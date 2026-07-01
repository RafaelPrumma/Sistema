using System.Globalization;
using System.Text.RegularExpressions;
using Sistema.CORE.Entities;

namespace Sistema.INFRA.Importers;

/// <summary>
/// Lógica PURA (sem banco) da materialização de gastos (G1): texto bruto de uma fatura Nubank
/// (cartão) ou de um extrato da NuConta (conta corrente) → lista de <see cref="LancamentoGastoParseado"/>.
/// Mantida separada do importador para ser testável sem DbContext, no mesmo estilo do
/// <see cref="ExtratoB3Materializador"/>.
///
/// O texto vem do <c>page.Text</c> do PdfPig (mesma fonte já persistida em ConteudoBrutoFinanceiro):
/// uma ÚNICA string concatenada por página, sem quebras de linha entre os itens. Nas faturas o
/// número final do cartão vem depois de quatro bullets U+2022 ("•••• 2115") e o crédito/estorno
/// usa o sinal de menos tipográfico U+2212 ("−R$"). Por isso o parsing é por âncoras/regex, não por
/// linha.
///
/// Escopo consciente (validado contra os arquivos reais):
///  - FATURA: as compras têm data + (bullets + final do cartão) + descrição + valor bem delimitados
///    → parse item a item (confiável).
///  - EXTRATO: os itens vêm com o valor COLADO no fim da descrição, que muitas vezes traz nº de
///    conta/agência ("... Conta: 26665654-47.800,00" → o valor real é 7.800,00, mas o nº da conta
///    gruda e vira 47.800,00). Logo, Pix/transferências avulsas NÃO são parseadas (o valor é
///    ambíguo e corromperia os totais). Só materializamos os itens com PREFIXO alfabético seguro e
///    valor imediatamente após ("Compra de Ações/ETF/FII TICKER", "Transferência de saldo NuInvest",
///    "Aplicação/Resgate RDB", "Pagamento de fatura", "Crédito em conta"). Assim o extrato contribui
///    aportes/transferências/créditos confiáveis, e a fatura carrega as despesas reais — sem números
///    inventados.
///
/// A separação aporte × despesa segue a regra do §3 da spec (não duplicar com Investimentos):
/// "Compra de Ações/ETF/FII", "Aplicação RDB", "Transferência de saldo NuInvest" NÃO são despesa
/// (Aporte); "Resgate RDB" e "Pagamento de fatura" são Transferência; "Crédito em conta" é Receita.
/// </summary>
public static class GastosMaterializador
{
    /// <summary>Caractere de substituição (U+FFFD) que o PdfPig às vezes produz no lugar de acentos perdidos.</summary>
    private const char Replacement = '�';

    private static readonly IReadOnlyDictionary<string, int> MesesPt = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
    {
        ["JAN"] = 1, ["FEV"] = 2, ["MAR"] = 3, ["ABR"] = 4, ["MAI"] = 5, ["JUN"] = 6,
        ["JUL"] = 7, ["AGO"] = 8, ["SET"] = 9, ["OUT"] = 10, ["NOV"] = 11, ["DEZ"] = 12
    };

    // --- FATURA (cartão) ---------------------------------------------------------------------

    // Uma transação na fatura: "08 FEV•••• 2115Mix Bairro MerceariaR$ 125,00" ou, no crédito/estorno,
    // o sinal de menos tipográfico (U+2212) antes de "R$": "28 FEVEstorno de "Dl*99 Ride"−R$ 22,70".
    // O final do cartão vem depois de quatro bullets U+2022 e um espaço: "•••• 2115"; itens sem cartão
    // (estorno/IOF) não têm esse marcador. Os bullets são OBRIGATÓRIOS no marcador do cartão para não
    // confundir o ano do cabeçalho ("16 MAR 2026") com um final de cartão. Grupos: dia | mês | (final
    // do cartão, opcional) | descrição | sinal (- ASCII ou − U+2212) | valor.
    // O lookbehind (?<!=\s) no "R$" do valor pula a COTAÇÃO de compras internacionais
    // ("Conversão: USD 1 = R$ 5,38R$ 107,64"): o "R$ 5,38" (precedido por "= ") é ignorado e o valor
    // recai sobre o "R$ 107,64" real. Compras normais ("MerceariaR$ 125,00") e créditos ("−R$ 22,70")
    // não são afetados.
    // O lookahead (?!\s+A\s+\d) após o mês rejeita o CABEÇALHO de período ("DE 08 FEV A 08 MAR"),
    // que senão iniciaria um "lançamento" espúrio e engoliria a 1ª transação real logo em seguida.
    private static readonly Regex FaturaTransacao = new(
        @"(?<dia>\d{1,2})\s+(?<mes>[A-Z]{3})(?!\s+A\s+\d)(?:•+\s*(?<cartao>\d{4}))?(?<desc>.+?)(?<sinal>[-−])?(?<!=\s)R\$\s*(?<valor>\d{1,3}(?:\.\d{3})*,\d{2})",
        RegexOptions.Compiled);

    // "- Parcela 2/12" no fim da descrição.
    private static readonly Regex ParcelaRegex = new(@"-\s*Parcela\s+(?<atual>\d{1,2})\s*/\s*(?<total>\d{1,2})", RegexOptions.Compiled);

    // Rabo de compra internacional grudado na descrição: "USD 20.00Conversão: USD 1 = R$ 5,38".
    // Corta do valor em moeda estrangeira (ou do "Conversão:") em diante.
    private static readonly Regex ConversaoInternacionalTail = new(@"(?:USD|EUR|GBP|Conversão:).*$", RegexOptions.Compiled);

    // Período da fatura no cabeçalho ("FATURA 16 MAR 2026"): dá o ano de referência das transações.
    private static readonly Regex FaturaAno = new(@"FATURA\s+\d{1,2}\s+[A-Z]{3}\s+(?<ano>\d{4})", RegexOptions.Compiled);

    /// <summary>
    /// Materializa as transações de uma fatura Nubank (cartão de crédito). Recebe o texto de TODAS
    /// as páginas (concatenado por '\n' entre páginas) e o ano-base resolvido a partir do nome do
    /// arquivo (Nubank_yyyy-MM-dd).
    /// </summary>
    public static IReadOnlyList<LancamentoGastoParseado> ParsearFatura(string textoCompleto, int anoBase, IReadOnlyList<RegraTexto>? regras = null)
    {
        var resultado = new List<LancamentoGastoParseado>();
        if (string.IsNullOrWhiteSpace(textoCompleto))
            return resultado;

        // Sem quebra de linha confiável, varremos o texto inteiro e filtramos itens válidos por regex.
        // A validade (cartão presente OU descrição "Estorno"/"IOF de volta") descarta valores de
        // resumo ("Total a pagar R$ 9.578,25", limites, alternativas de pagamento etc.).
        var ano = ResolverAnoFatura(textoCompleto, anoBase);
        foreach (Match m in FaturaTransacao.Matches(textoCompleto))
        {
            if (!MesesPt.TryGetValue(m.Groups["mes"].Value, out var mes))
                continue;

            var temCartao = m.Groups["cartao"].Success;
            // Em compra internacional a descrição arrasta a cotação ("...USD 20.00Conversão: USD 1 =");
            // tiramos esse rabo (do 1º valor em moeda estrangeira em diante) para ficar só o estabelecimento.
            var descricaoBruta = Limpar(ConversaoInternacionalTail.Replace(m.Groups["desc"].Value, string.Empty));
            var ehEstorno = descricaoBruta.StartsWith("Estorno", StringComparison.OrdinalIgnoreCase);
            var ehIofDevolvido = descricaoBruta.StartsWith("IOF de volta", StringComparison.OrdinalIgnoreCase);

            // Validade: uma transação real tem o final do cartão ("•••• 2115") OU é um Estorno / IOF
            // devolvido (crédito sem cartão). Isso descarta os valores de resumo da fatura.
            if (!temCartao && !ehEstorno && !ehIofDevolvido)
                continue;

            var dia = int.Parse(m.Groups["dia"].Value, CultureInfo.InvariantCulture);
            if (!TentarData(ano, mes, dia, out var data))
                continue;

            var valor = ParseValor(m.Groups["valor"].Value);
            if (valor <= 0m)
                continue;

            // sinal "-"/"−" antes de R$, OU estorno/IOF-de-volta = crédito (devolução) → Receita.
            var credito = m.Groups["sinal"].Success || ehEstorno || ehIofDevolvido;

            int? parcelaAtual = null, parcelaTotal = null;
            var descricao = descricaoBruta;
            var parcela = ParcelaRegex.Match(descricaoBruta);
            if (parcela.Success)
            {
                parcelaAtual = int.Parse(parcela.Groups["atual"].Value, CultureInfo.InvariantCulture);
                parcelaTotal = int.Parse(parcela.Groups["total"].Value, CultureInfo.InvariantCulture);
                descricao = Limpar(descricaoBruta[..parcela.Index]);
            }

            if (string.IsNullOrWhiteSpace(descricao))
                continue;

            var tipo = credito ? TipoLancamentoGasto.Receita : TipoLancamentoGasto.Despesa;
            var estabelecimento = (credito || descricao.StartsWith("IOF", StringComparison.OrdinalIgnoreCase)) ? null : descricao;

            resultado.Add(Montar(FonteLancamentoGasto.Cartao, data, descricao, valor, tipo, estabelecimento, parcelaAtual, parcelaTotal, regras));
        }

        return resultado;
    }

    private static int ResolverAnoFatura(string texto, int anoBase)
    {
        var m = FaturaAno.Match(texto);
        return m.Success && int.TryParse(m.Groups["ano"].Value, out var ano) ? ano : anoBase;
    }

    // --- CONTA (extrato NuConta) -------------------------------------------------------------

    // Cabeçalho de um dia: "01 AGO 2025". Âncora dos itens daquele dia. O regex de dia não casa o
    // "01 DE MARÇO DE 2026" por extenso do cabeçalho da página → só pega cabeçalhos reais de dia.
    private static readonly Regex ContaDia = new(@"(?<dia>\d{1,2})\s+(?<mes>[A-Z]{3})\s+(?<ano>\d{4})", RegexOptions.Compiled);

    // Itens do extrato com prefixo alfabético SEGURO e valor imediatamente após (sem nº de conta
    // colado). Cada alternativa termina no início do valor "1.234,56". Deliberadamente NÃO cobre
    // Pix/transferências avulsas (valor ambíguo — vem colado no nº da conta/CPF).
    //  - "Compra de Ações/ETF/FII<TICKER>": TICKER = 4 letras + o sufixo numérico "11" (FII/ETF) OU
    //    um único dígito (ação: BBAS3, PETR4). Fixar o sufixo em "(?:11|\d)" evita roubar um dígito do
    //    valor colado (ex.: "BBAS3221,28" = BBAS3 + 221,28, não BBAS32 + 21,28).
    //  - "Transferência de saldo NuInvest", "Aplicação/Resgate RDB", "Pagamento de fatura",
    //    "Crédito em conta": prefixo fixo, valor logo em seguida.
    private static readonly Regex ItemTipadoConta = new(
        @"(?<desc>Compra de (?:A\S{0,3}es|ETF|FII)[A-Z]{4}(?:11|\d)|Transfer\S{0,3}ncia de saldo NuInvest|Aplica\S{0,3}o RDB|Resgate RDB|Pagamento de fatura|Cr\S{0,2}dito em conta)(?<valor>\d{1,3}(?:\.\d{3})*,\d{2})",
        RegexOptions.Compiled);

    /// <summary>
    /// Materializa os lançamentos SEGUROS de um extrato da NuConta (conta corrente). Texto de TODAS
    /// as páginas concatenado. Cada dia é aberto por "DD MMM YYYY"; dentro dele varremos só os itens
    /// tipados de <see cref="ItemTipadoConta"/> (ver a nota de escopo na doc da classe). A natureza
    /// (Aporte/Transferência/Receita) sai de <see cref="ClassificarItemConta"/>.
    /// </summary>
    public static IReadOnlyList<LancamentoGastoParseado> ParsearExtratoConta(string textoCompleto, IReadOnlyList<RegraTexto>? regras = null)
    {
        var resultado = new List<LancamentoGastoParseado>();
        if (string.IsNullOrWhiteSpace(textoCompleto))
            return resultado;

        var dias = ContaDia.Matches(textoCompleto).Cast<Match>().ToList();
        for (var i = 0; i < dias.Count; i++)
        {
            var diaMatch = dias[i];
            if (!MesesPt.TryGetValue(diaMatch.Groups["mes"].Value, out var mes))
                continue;
            if (!int.TryParse(diaMatch.Groups["ano"].Value, out var ano))
                continue;
            var dia = int.Parse(diaMatch.Groups["dia"].Value, CultureInfo.InvariantCulture);
            if (!TentarData(ano, mes, dia, out var data))
                continue;

            var inicio = diaMatch.Index + diaMatch.Length;
            var fim = i + 1 < dias.Count ? dias[i + 1].Index : textoCompleto.Length;
            ExtrairItensConta(textoCompleto[inicio..fim], data, regras, resultado);
        }

        return resultado;
    }

    private static void ExtrairItensConta(string bloco, DateTime data, IReadOnlyList<RegraTexto>? regras, List<LancamentoGastoParseado> destino)
    {
        foreach (Match m in ItemTipadoConta.Matches(bloco))
        {
            var valor = ParseValor(m.Groups["valor"].Value);
            if (valor <= 0m)
                continue;

            var descricao = Limpar(m.Groups["desc"].Value);
            if (string.IsNullOrWhiteSpace(descricao))
                continue;

            var (tipo, estabelecimento) = ClassificarItemConta(descricao);
            destino.Add(Montar(FonteLancamentoGasto.Conta, data, descricao, valor, tipo, estabelecimento, null, null, regras));
        }
    }

    /// <summary>
    /// Natureza de um item TIPADO do extrato de conta. Regra §3 (não duplicar com Investimentos):
    ///  - "Compra de Ações/ETF/FII", "Aplicação RDB" → Aporte;
    ///  - "Resgate RDB", "Transferência de saldo NuInvest", "Pagamento de fatura" → Transferência;
    ///  - "Crédito em conta" → Receita.
    /// O texto pode ter acentos perdidos (U+FFFD) → casamos por trechos ASCII e ignoramos caixa.
    /// </summary>
    public static (TipoLancamentoGasto Tipo, string? Estabelecimento) ClassificarItemConta(string descricao)
    {
        var d = NormalizarAscii(descricao);

        if (d.StartsWith("COMPRA DE A", StringComparison.Ordinal) // Compra de Ações
            || d.StartsWith("COMPRA DE ETF", StringComparison.Ordinal)
            || d.StartsWith("COMPRA DE FII", StringComparison.Ordinal)
            || (d.StartsWith("APLICA", StringComparison.Ordinal) && d.Contains("RDB", StringComparison.Ordinal)))
            return (TipoLancamentoGasto.Aporte, null);

        if (d.StartsWith("RESGATE", StringComparison.Ordinal)
            || d.Contains("SALDO NUINVEST", StringComparison.Ordinal)
            || d.Contains("PAGAMENTO DE FATURA", StringComparison.Ordinal))
            return (TipoLancamentoGasto.Transferencia, null);

        if (d.Contains("DITO EM CONTA", StringComparison.Ordinal)) // Crédito em conta (Cr[é]dito)
            return (TipoLancamentoGasto.Receita, null);

        // Item tipado desconhecido: por segurança, Transferência (não infla despesa).
        return (TipoLancamentoGasto.Transferencia, null);
    }

    // --- Categorização (aplica a 1ª regra que casa, em ordem de prioridade) -------------------

    /// <summary>
    /// Aplica a primeira <see cref="RegraTexto"/> (já ordenada por prioridade) cuja descrição casar.
    /// Devolve o CategoriaId da regra ou null se nenhuma casar. Regex inválida é ignorada (não estoura).
    /// </summary>
    public static int? Categorizar(string descricao, IReadOnlyList<RegraTexto>? regras)
    {
        if (regras is null || regras.Count == 0 || string.IsNullOrWhiteSpace(descricao))
            return null;

        var alvo = NormalizarAscii(descricao);
        foreach (var regra in regras)
        {
            try
            {
                var casou = regra.TipoMatch == TipoMatchRegra.Regex
                    ? Regex.IsMatch(alvo, NormalizarAscii(regra.Padrao), RegexOptions.IgnoreCase)
                    : alvo.Contains(NormalizarAscii(regra.Padrao), StringComparison.OrdinalIgnoreCase);
                if (casou)
                    return regra.CategoriaId;
            }
            catch (ArgumentException)
            {
                // Regex inválida na regra: ignora e segue (categorização é best-effort).
            }
        }

        return null;
    }

    // --- helpers ------------------------------------------------------------------------------

    private static LancamentoGastoParseado Montar(
        FonteLancamentoGasto fonte, DateTime data, string descricao, decimal valor, TipoLancamentoGasto tipo,
        string? estabelecimento, int? parcelaAtual, int? parcelaTotal, IReadOnlyList<RegraTexto>? regras)
        => new(
            fonte, data, descricao, valor, tipo, estabelecimento, parcelaAtual, parcelaTotal,
            Categorizar(descricao, regras),
            LancamentoGasto.GerarChaveNatural(fonte, data, descricao, valor, tipo));

    /// <summary>Tira espaços duplicados e o caractere de substituição (acentos perdidos) das pontas.</summary>
    private static string Limpar(string? texto)
    {
        if (string.IsNullOrEmpty(texto))
            return string.Empty;

        var s = texto.Replace(Replacement.ToString(), string.Empty, StringComparison.Ordinal);
        s = Regex.Replace(s, @"\s+", " ").Trim();
        return s;
    }

    /// <summary>Maiúsculas + remove o caractere de substituição, para casar trechos ASCII de forma estável.</summary>
    private static string NormalizarAscii(string? texto)
        => (texto ?? string.Empty).Replace(Replacement.ToString(), string.Empty, StringComparison.Ordinal).ToUpperInvariant();

    private static decimal ParseValor(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return 0m;

        var texto = value.Trim().Replace(".", string.Empty, StringComparison.Ordinal).Replace(",", ".", StringComparison.Ordinal);
        return decimal.TryParse(texto, NumberStyles.Number, CultureInfo.InvariantCulture, out var v) ? v : 0m;
    }

    private static bool TentarData(int ano, int mes, int dia, out DateTime data)
    {
        data = default;
        if (ano is < 2000 or > 2100 || mes is < 1 or > 12 || dia < 1 || dia > DateTime.DaysInMonth(ano, mes))
            return false;
        data = new DateTime(ano, mes, dia);
        return true;
    }

    /// <summary>Ano-base de uma fatura a partir do nome "Nubank_yyyy-MM-dd.pdf" (fallback do cabeçalho).</summary>
    public static int AnoBaseDoNomeFatura(string? fileName, int fallback)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            return fallback;
        var m = Regex.Match(fileName, @"(?<ano>\d{4})-\d{2}-\d{2}");
        return m.Success && int.TryParse(m.Groups["ano"].Value, out var ano) ? ano : fallback;
    }
}

/// <summary>Um lançamento de gasto extraído de uma fatura/extrato (DTO puro, sem EF).</summary>
public sealed record LancamentoGastoParseado(
    FonteLancamentoGasto Fonte,
    DateTime Data,
    string Descricao,
    decimal Valor,
    TipoLancamentoGasto Tipo,
    string? Estabelecimento,
    int? ParcelaAtual,
    int? ParcelaTotal,
    int? CategoriaId,
    string ChaveNatural);

/// <summary>
/// Projeção mínima de <see cref="RegraCategorizacao"/> para a categorização pura (sem EF):
/// padrão, tipo de match, categoria e prioridade (a lista deve vir ordenada por prioridade).
/// </summary>
public sealed record RegraTexto(string Padrao, TipoMatchRegra TipoMatch, int CategoriaId, int Prioridade);
