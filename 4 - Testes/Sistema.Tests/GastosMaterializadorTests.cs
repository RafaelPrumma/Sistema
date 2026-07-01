using Sistema.CORE.Entities;
using Sistema.INFRA.Importers;
using UglyToad.PdfPig;

namespace Sistema.Tests;

// G1 — lógica PURA da materialização de gastos (sem DbContext): parser da fatura Nubank (cartão) e
// do extrato da NuConta (conta). As fixtures reproduzem os QUIRKS REAIS do texto do PdfPig
// (bullets U+2022 antes do final do cartão, sinal U+2212 no crédito, valor colado no fim da
// descrição, ticker B3 grudado no valor) SEM copiar dado sensível dos arquivos do usuário
// (nomes/CPF fictícios). Um teste extra roda contra os arquivos reais SE eles existirem localmente
// (privacidade: não versionados) — no CI/checkout limpo ele é pulado, não falha.
public class GastosMaterializadorTests
{
    private const char Bullet = '•'; // • marcador antes do final do cartão
    private const char Menos = '−';  // − sinal de crédito/estorno na fatura
    private static readonly string Cartao = new string(Bullet, 4) + " 2115";

    // ---------------- FATURA (cartão) ----------------

    private static string FaturaFixture() =>
        "RAFAEL SILVAFATURA 16 MAR 2026EMISSÃO E ENVIO 08 MAR 2026" +
        "Total a pagarR$ 9.578,25" +                                  // resumo: NÃO deve virar lançamento
        "TRANSAÇÕESDE 08 FEV A 08 MARRafael F M SilvaR$ 9.578,25" +  // cabeçalho de período + nome/total
        $"08 FEV{Cartao}Mix Bairro MerceariaR$ 125,00" +              // despesa simples
        $"08 FEV{Cartao}Mercadolivre*2produtos - Parcela 2/2R$ 59,39" + // parcela
        $"09 FEV{Cartao}Uber Uber *Trip Help.UR$ 42,94" +             // transporte (regra)
        $"28 FEVEstorno de \"Dl*99 Ride\"{Menos}R$ 22,70" +          // crédito/estorno (U+2212, sem cartão)
        $"03 MAR{Cartao}Openai *Chatgpt SubscrUSD 20.00Conversão: USD 1 = R$ 5,38R$ 107,64"; // internacional

    [Fact]
    public void ParsearFatura_ExtraiTransacoesEIgnoraResumo()
    {
        var itens = GastosMaterializador.ParsearFatura(FaturaFixture(), 2026);

        // 5 transações reais; o "Total a pagar" do resumo é descartado (sem cartão/estorno).
        Assert.Equal(5, itens.Count);
        Assert.All(itens, i => Assert.Equal(FonteLancamentoGasto.Cartao, i.Fonte));

        var mix = Assert.Single(itens, i => i.Descricao == "Mix Bairro Mercearia");
        Assert.Equal(new DateTime(2026, 2, 8), mix.Data);
        Assert.Equal(125.00m, mix.Valor);
        Assert.Equal(TipoLancamentoGasto.Despesa, mix.Tipo);
    }

    [Fact]
    public void ParsearFatura_ParcelaExtraiAtualETotalELimpaDescricao()
    {
        var itens = GastosMaterializador.ParsearFatura(FaturaFixture(), 2026);

        var parcelado = Assert.Single(itens, i => i.Descricao.StartsWith("Mercadolivre", StringComparison.Ordinal));
        Assert.Equal(2, parcelado.ParcelaAtual);
        Assert.Equal(2, parcelado.ParcelaTotal);
        Assert.Equal(59.39m, parcelado.Valor);
        Assert.DoesNotContain("Parcela", parcelado.Descricao, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ParsearFatura_EstornoComSinalTipografico_ViraReceita()
    {
        var itens = GastosMaterializador.ParsearFatura(FaturaFixture(), 2026);

        var estorno = Assert.Single(itens, i => i.Descricao.StartsWith("Estorno", StringComparison.Ordinal));
        Assert.Equal(TipoLancamentoGasto.Receita, estorno.Tipo);
        Assert.Equal(22.70m, estorno.Valor);
        Assert.Equal(new DateTime(2026, 2, 28), estorno.Data);
    }

    [Fact]
    public void ParsearFatura_CompraInternacional_UsaValorEmReaisNaoACotacao()
    {
        var itens = GastosMaterializador.ParsearFatura(FaturaFixture(), 2026);

        // "...= R$ 5,38R$ 107,64": o valor REAL é 107,64 (o 5,38 é a cotação e é ignorado); a
        // descrição não arrasta o "USD 20.00Conversão...".
        var intl = Assert.Single(itens, i => i.Descricao.StartsWith("Openai", StringComparison.Ordinal));
        Assert.Equal(107.64m, intl.Valor);
        Assert.Equal(TipoLancamentoGasto.Despesa, intl.Tipo);
        Assert.DoesNotContain("Conversão", intl.Descricao, StringComparison.Ordinal);
        Assert.DoesNotContain("USD", intl.Descricao, StringComparison.Ordinal);
    }

    [Fact]
    public void ParsearFatura_AnoDoCabecalhoNaoViraLancamento()
    {
        // Regressão: "16 MAR 2026" do cabeçalho não pode ser confundido com "dia mês <cartão 2026>".
        var itens = GastosMaterializador.ParsearFatura(FaturaFixture(), 2026);
        Assert.DoesNotContain(itens, i => i.Valor == 9578.25m);
    }

    [Fact]
    public void ParsearFatura_TextoVazio_RetornaVazio()
    {
        Assert.Empty(GastosMaterializador.ParsearFatura("", 2026));
        Assert.Empty(GastosMaterializador.ParsearFatura("   ", 2026));
    }

    // ---------------- EXTRATO (conta) ----------------

    private static string ExtratoFixture() =>
        "Extrato da conta VALORES EM R$" +
        "01 MAR 2026Total de saídas- 25,00" +
        "Transferência enviada pelo PixFULANO DE TAL (Transferência enviada)5,00" + // Pix: NÃO parseado (valor ambíguo)
        "14 MAR 2026Total de entradas+ 7.973,26" +
        "Crédito em conta173,26" +                                                  // Receita
        "16 ABR 2026Total de saídas- 900,00" +
        "Compra de AçõesBBAS3221,28" +                                              // Aporte (ticker BBAS3 + 221,28)
        "Compra de ETFGOLD11251,79" +                                              // Aporte (GOLD11 + 251,79)
        "Compra de FIICPTS11121,09" +                                              // Aporte (CPTS11 + 121,09)
        "Aplicação RDB600,00" +                                                     // Aporte
        "Resgate RDB3.000,00" +                                                     // Transferência
        "Transferência de saldo NuInvest26,40" +                                    // Transferência
        "Pagamento de fatura9.578,25";                                              // Transferência

    [Fact]
    public void ParsearExtratoConta_TipaAporteTransferenciaEReceita()
    {
        var itens = GastosMaterializador.ParsearExtratoConta(ExtratoFixture());

        Assert.All(itens, i => Assert.Equal(FonteLancamentoGasto.Conta, i.Fonte));

        // Pix avulso não é materializado (valor colado no nº de conta é ambíguo) → nenhuma despesa.
        Assert.DoesNotContain(itens, i => i.Tipo == TipoLancamentoGasto.Despesa);

        var credito = Assert.Single(itens, i => i.Descricao.StartsWith("Crédito", StringComparison.Ordinal));
        Assert.Equal(TipoLancamentoGasto.Receita, credito.Tipo);
        Assert.Equal(173.26m, credito.Valor);
        Assert.Equal(new DateTime(2026, 3, 14), credito.Data);
    }

    [Fact]
    public void ParsearExtratoConta_CompraDeAtivos_ViraAporteComTickerEValorCorretos()
    {
        var itens = GastosMaterializador.ParsearExtratoConta(ExtratoFixture());

        var acao = Assert.Single(itens, i => i.Descricao.Contains("BBAS3", StringComparison.Ordinal));
        Assert.Equal(TipoLancamentoGasto.Aporte, acao.Tipo);
        Assert.Equal(221.28m, acao.Valor); // ticker não rouba dígito do valor
        Assert.Equal(new DateTime(2026, 4, 16), acao.Data);

        var etf = Assert.Single(itens, i => i.Descricao.Contains("GOLD11", StringComparison.Ordinal));
        Assert.Equal(TipoLancamentoGasto.Aporte, etf.Tipo);
        Assert.Equal(251.79m, etf.Valor);

        var fii = Assert.Single(itens, i => i.Descricao.Contains("CPTS11", StringComparison.Ordinal));
        Assert.Equal(TipoLancamentoGasto.Aporte, fii.Tipo);
        Assert.Equal(121.09m, fii.Valor);

        var rdb = Assert.Single(itens, i => i.Descricao.StartsWith("Aplicação RDB", StringComparison.Ordinal));
        Assert.Equal(TipoLancamentoGasto.Aporte, rdb.Tipo);
        Assert.Equal(600.00m, rdb.Valor);
    }

    [Fact]
    public void ParsearExtratoConta_ResgateNuInvestEFatura_ViraTransferencia()
    {
        var itens = GastosMaterializador.ParsearExtratoConta(ExtratoFixture());

        Assert.Equal(TipoLancamentoGasto.Transferencia,
            Assert.Single(itens, i => i.Descricao.StartsWith("Resgate", StringComparison.Ordinal)).Tipo);
        Assert.Equal(TipoLancamentoGasto.Transferencia,
            Assert.Single(itens, i => i.Descricao.Contains("NuInvest", StringComparison.Ordinal)).Tipo);

        var fatura = Assert.Single(itens, i => i.Descricao.StartsWith("Pagamento de fatura", StringComparison.Ordinal));
        Assert.Equal(TipoLancamentoGasto.Transferencia, fatura.Tipo); // não vira despesa (evita duplicar com a fatura)
        Assert.Equal(9578.25m, fatura.Valor);
    }

    [Fact]
    public void ParsearExtratoConta_TextoVazio_RetornaVazio()
        => Assert.Empty(GastosMaterializador.ParsearExtratoConta(""));

    // ---------------- Idempotência (chave natural) ----------------

    [Fact]
    public void ChaveNatural_MesmoLancamento_EhIgualEntreExecucoes()
    {
        var a = GastosMaterializador.ParsearFatura(FaturaFixture(), 2026);
        var b = GastosMaterializador.ParsearFatura(FaturaFixture(), 2026);

        // Reparsear a mesma fatura gera exatamente as mesmas chaves (dedup cross-arquivo no serviço).
        var chavesA = a.Select(x => x.ChaveNatural).OrderBy(x => x, StringComparer.Ordinal).ToList();
        var chavesB = b.Select(x => x.ChaveNatural).OrderBy(x => x, StringComparer.Ordinal).ToList();
        Assert.Equal(chavesA, chavesB);

        // E não há chaves duplicadas dentro do mesmo arquivo.
        Assert.Equal(chavesA.Count, chavesA.Distinct(StringComparer.Ordinal).Count());
    }

    // ---------------- Categorização por regra ----------------

    [Fact]
    public void Categorizar_AplicaPrimeiraRegraQueCasaEmOrdemDePrioridade()
    {
        var regras = new List<RegraTexto>
        {
            new("UBER", TipoMatchRegra.Contem, CategoriaId: 7, Prioridade: 10),
            new("MERCEARIA", TipoMatchRegra.Contem, CategoriaId: 2, Prioridade: 10),
        };

        Assert.Equal(7, GastosMaterializador.Categorizar("Uber Uber *Trip Help.U", regras));
        Assert.Equal(2, GastosMaterializador.Categorizar("Mix Bairro Mercearia", regras));
        Assert.Null(GastosMaterializador.Categorizar("Padaria do Zé", regras));
        Assert.Null(GastosMaterializador.Categorizar("qualquer", regras: null));
    }

    [Fact]
    public void Categorizar_RegexInvalida_NaoEstoura()
    {
        var regras = new List<RegraTexto> { new("[", TipoMatchRegra.Regex, CategoriaId: 1, Prioridade: 1) };
        Assert.Null(GastosMaterializador.Categorizar("qualquer coisa", regras));
    }

    // ---------------- Smoke test com os ARQUIVOS REAIS (só roda se existirem localmente) ----------------

    [Fact]
    public void ParsearArquivosReais_SeExistirem_ExtraiLancamentosCoerentes()
    {
        var pasta = LocalizarPastaFinanceiro();
        if (pasta is null)
            return; // arquivos não versionados (privacidade): pulado em CI/checkout limpo.

        var fatura = Directory.GetFiles(pasta, "Nubank_2026-*.pdf")
            .FirstOrDefault(f => !Path.GetFileName(f).Contains("nota", StringComparison.OrdinalIgnoreCase));
        if (fatura is not null)
        {
            var itens = GastosMaterializador.ParsearFatura(
                LerTextoPdf(fatura), GastosMaterializador.AnoBaseDoNomeFatura(Path.GetFileName(fatura), 2026));
            Assert.NotEmpty(itens);
            Assert.All(itens, i => Assert.True(i.Valor > 0m));
            // Reparse é idempotente (mesmas chaves, sem duplicata).
            var chaves = itens.Select(i => i.ChaveNatural).ToList();
            Assert.Equal(chaves.Count, chaves.Distinct(StringComparer.Ordinal).Count());
        }

        var extrato = Directory.GetFiles(pasta, "NU_40648231_*.pdf").FirstOrDefault();
        if (extrato is not null)
        {
            var itens = GastosMaterializador.ParsearExtratoConta(LerTextoPdf(extrato));
            // Extrato só materializa aporte/transferência/receita (Pix avulso não): nada de despesa.
            Assert.DoesNotContain(itens, i => i.Tipo == TipoLancamentoGasto.Despesa);
            Assert.All(itens, i => Assert.True(i.Valor > 0m));
        }
    }

    private static string LerTextoPdf(string caminho)
    {
        using var pdf = PdfDocument.Open(caminho);
        return string.Join('\n', pdf.GetPages().Select(p => p.Text));
    }

    // Sobe da pasta de execução do teste até achar "arquivos/financeiro" na raiz do repositório.
    private static string? LocalizarPastaFinanceiro()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            var candidata = Path.Combine(dir.FullName, "arquivos", "financeiro");
            if (Directory.Exists(candidata))
                return candidata;
            dir = dir.Parent;
        }
        return null;
    }
}
