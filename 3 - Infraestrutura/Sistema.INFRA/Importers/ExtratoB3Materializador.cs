using System.Globalization;
using Sistema.CORE.Entities;

namespace Sistema.INFRA.Importers;

/// <summary>
/// Lógica PURA (sem banco) da materialização do extrato consolidado da B3 (F2):
/// parsing das linhas de Negociações/Proventos e a regra de precedência vs notas.
/// Mantida separada do <see cref="FinancasImportador"/> para ser testável sem DbContext.
/// </summary>
public static class ExtratoB3Materializador
{
    public const string Fonte = "B3 Extrato";

    /// <summary>
    /// Precedência INVERTIDA (§3.1, revista jun/2026): a B3 é a fonte de verdade e SEMPRE materializa
    /// suas Negociações; as notas Nubank só materializam onde a B3 NÃO cobre aquele ticker×mês
    /// (meses < set/2021, outras corretoras). <paramref name="cobertosPorB3"/> é o conjunto de
    /// (assetId, ano, mês) que tem Negociação da B3 → a nota daquele ticker×mês é pulada (a B3 manda).
    /// </summary>
    public static bool DeveMaterializarNotaB3(int assetId, int ano, int mes, ISet<(int AssetId, int Ano, int Mes)> cobertosPorB3)
        => !cobertosPorB3.Contains((assetId, ano, mes));

    /// <summary>Chave natural do agregado mensal: fonte + ticker + ano-mês + sentido + corretora.</summary>
    public static string ChaveNegociacao(string assetKey, int anoMes, TipoOperacaoFinanceira tipo, string? broker)
        => $"{Fonte}|{assetKey}|{anoMes:D6}|{(int)tipo}|{(broker ?? string.Empty).Trim().ToUpperInvariant()}";

    /// <summary>Ano-mês no formato yyyyMM (ex.: 2022/9 → 202209).</summary>
    public static int AnoMes(int ano, int mes) => ano * 100 + mes;

    /// <summary>
    /// Precedência de PROVENTOS (mesma regra "B3 manda" das Negociações, §3.1): a fonte
    /// <see cref="Fonte"/> (extrato consolidado mensal, custódia oficial) é primária; a Brapi
    /// (estimada por ticker) é apenas FALLBACK. Um provento candidato da Brapi deve ser
    /// SUPRIMIDO se a B3 já cobre o mesmo ativo no mesmo ano-mês de pagamento.
    /// </summary>
    /// <param name="assetId">
    /// AssetId do candidato Brapi. Null = não dá pra casar com a cobertura → NÃO suprime
    /// (não dá pra afirmar que a B3 cobre; mantém como fallback).
    /// </param>
    /// <param name="anoMesPagamento">
    /// Ano-mês de pagamento do candidato (yyyyMM). Quando o PaymentDate da Brapi for nulo, o
    /// chamador deriva o mês do ReferenceDate (data-com) — documentado no caminho de inserção.
    /// </param>
    /// <param name="cobertosPorB3">Conjunto (AssetId × ano-mês de pagamento) com provento da B3.</param>
    /// <returns>true = suprimir o candidato Brapi (a B3 manda nesse ativo×mês).</returns>
    public static bool DeveSuprimirProventoBrapi(int? assetId, int anoMesPagamento, ISet<(int AssetId, int AnoMes)> cobertosPorB3)
        => assetId is int id && cobertosPorB3.Contains((id, anoMesPagamento));

    /// <summary>Ano-mês (yyyyMM) de uma data; usa PaymentDate, com fallback no ReferenceDate (data-com).</summary>
    public static int? AnoMesPagamento(DateTime? pagamento, DateTime? referencia)
    {
        var data = pagamento ?? referencia;
        return data is null ? null : data.Value.Year * 100 + data.Value.Month;
    }

    /// <summary>Sigla = prefixo antes de " - " no campo Produto (ex.: "BBAS3 - BANCO DO BRASIL S/A").</summary>
    public static string? ExtrairTickerProduto(string? produto)
    {
        if (string.IsNullOrWhiteSpace(produto))
            return null;

        var idx = produto.IndexOf(" - ", StringComparison.Ordinal);
        var ticker = NormalizarTicker((idx >= 0 ? produto[..idx] : produto).Trim()); // fracionário → base
        return string.IsNullOrWhiteSpace(ticker) ? null : ticker;
    }

    /// <summary>
    /// Mercado fracionário da B3 (ex.: ITUB4F, PETR4F, GOLD11F) é o MESMO ativo do lote-padrão
    /// (ITUB4, PETR4, GOLD11): remove o sufixo "F". Padrão = 4 letras + 1–2 dígitos (+ "F" no fracionário).
    /// Sem isso o extrato cria ativos duplicados (ITUB4 e ITUB4F) e racha a posição.
    /// </summary>
    public static string NormalizarTicker(string? ticker)
    {
        var t = (ticker ?? string.Empty).Trim().ToUpperInvariant();
        if (System.Text.RegularExpressions.Regex.IsMatch(t, @"^[A-Z]{4}\d{1,2}F$"))
            t = t[..^1]; // fracionário → base (ITUB4F → ITUB4)
        return Aliases.TryGetValue(t, out var canonico) ? canonico : t;
    }

    /// <summary>Aliases de ticker: tickers diferentes que são o MESMO ativo (troca de código/fundo).</summary>
    private static readonly IReadOnlyDictionary<string, string> Aliases = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        // Mesmo fundo (Iridium): a B3 usou "IRIM11", a Nubank "IRDM11" → a venda saía contada 2×.
        ["IRIM11"] = "IRDM11",
    };

    /// <summary>Mapeia o "Tipo de Evento" do extrato para o vocabulário interno (JCP/Rendimento/Dividendo).</summary>
    public static string MapTipoProvento(string? tipoEvento)
    {
        var texto = (tipoEvento ?? string.Empty).Trim().ToUpperInvariant();
        if (texto.Contains("JUROS") || texto.Contains("JCP") || texto.Contains("CAPITAL PR"))
            return "JCP";
        if (texto.Contains("RENDIMENTO"))
            return "Rendimento";
        if (texto.Contains("DIVIDENDO"))
            return "Dividendo";
        return string.IsNullOrWhiteSpace(tipoEvento) ? "Provento" : tipoEvento!.Trim();
    }

    /// <summary>FII se o ticker termina em 11; senão Ação. (Ticker já vem canônico do extrato.)</summary>
    public static ClasseAtivo ClassePorTicker(string ticker)
        => ticker.EndsWith("11", StringComparison.OrdinalIgnoreCase) ? ClasseAtivo.FII : ClasseAtivo.Acao;

    /// <summary>Datas do extrato vêm em dd/MM/yyyy; "-" (Período Final vazio) e branco viram null.</summary>
    public static DateTime? ParseData(string? value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Trim() == "-")
            return null;

        return DateTime.TryParseExact(value.Trim(), "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed)
            ? parsed.Date
            : null;
    }

    /// <summary>Decimais do extrato usam "." como separador decimal (ex.: "96.9", "0.27"). InvariantCulture.</summary>
    public static decimal ParseDecimal(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return 0m;

        var texto = value.Trim().Replace("R$", string.Empty, StringComparison.OrdinalIgnoreCase).Trim();
        if (texto.Length == 0 || texto == "-")
            return 0m;

        return decimal.TryParse(texto, NumberStyles.Number, CultureInfo.InvariantCulture, out var parsed) ? parsed : 0m;
    }

    /// <summary>Indexa a linha pelo cabeçalho (case-insensitive); ignora colunas sem header.</summary>
    public static Dictionary<string, string> MapearLinha(IReadOnlyList<string> headers, IReadOnlyList<string> celulas)
    {
        var row = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < celulas.Count; i++)
        {
            if (i >= headers.Count)
                break;

            var header = headers[i];
            if (string.IsNullOrWhiteSpace(header) || row.ContainsKey(header))
                continue;

            row[header] = (celulas[i] ?? string.Empty).Trim();
        }

        return row;
    }

    /// <summary>
    /// Interpreta uma linha da aba "Negociações" (agregado mensal). Devolve até dois movimentos:
    /// uma Compra (se Quantidade (Compra) &gt; 0) e/ou uma Venda (se Quantidade (Venda) &gt; 0).
    /// Linha sem ticker ou sem quantidade não gera movimento.
    /// </summary>
    public static IReadOnlyList<MovimentoNegociacaoB3> InterpretarNegociacao(IReadOnlyDictionary<string, string> row)
    {
        var ticker = NormalizarTicker(Campo(row, "Código de Negociação")); // ITUB4F → ITUB4 (fracionário)
        var movimentos = new List<MovimentoNegociacaoB3>(2);
        if (string.IsNullOrWhiteSpace(ticker))
            return movimentos;

        var periodoInicial = ParseData(Campo(row, "Período (Inicial)"));
        var periodoFinal = ParseData(Campo(row, "Período (Final)"));
        var broker = Campo(row, "Instituição");

        var qtdCompra = ParseDecimal(Campo(row, "Quantidade (Compra)"));
        if (qtdCompra > 0m)
            movimentos.Add(new MovimentoNegociacaoB3(ticker, TipoOperacaoFinanceira.Compra, qtdCompra,
                ParseDecimal(Campo(row, "Preço Médio (Compra)")), periodoInicial, periodoFinal, broker));

        var qtdVenda = ParseDecimal(Campo(row, "Quantidade (Venda)"));
        if (qtdVenda > 0m)
            movimentos.Add(new MovimentoNegociacaoB3(ticker, TipoOperacaoFinanceira.Venda, qtdVenda,
                ParseDecimal(Campo(row, "Preço Médio (Venda)")), periodoInicial, periodoFinal, broker));

        return movimentos;
    }

    /// <summary>Interpreta uma linha da aba "Proventos Recebidos". Devolve null se inválida.</summary>
    public static ProventoB3? InterpretarProvento(IReadOnlyDictionary<string, string> row)
    {
        var ticker = ExtrairTickerProduto(Campo(row, "Produto"));
        if (string.IsNullOrWhiteSpace(ticker))
            return null;

        var pagamento = ParseData(Campo(row, "Pagamento"));
        if (pagamento is null)
            return null;

        var valor = ParseDecimal(Campo(row, "Valor líquido"));
        if (valor <= 0m)
            return null;

        return new ProventoB3(
            ticker,
            pagamento.Value,
            MapTipoProvento(Campo(row, "Tipo de Evento")),
            valor,
            ParseDecimal(Campo(row, "Quantidade")),
            ParseDecimal(Campo(row, "Preço unitário")),
            Campo(row, "Produto"));
    }

    private static string? Campo(IReadOnlyDictionary<string, string> row, string header)
        => row.TryGetValue(header, out var value) && !string.IsNullOrWhiteSpace(value) ? value.Trim() : null;
}

/// <summary>Um movimento (compra ou venda) extraído de uma linha agregada de Negociações.</summary>
public sealed record MovimentoNegociacaoB3(
    string Ticker,
    TipoOperacaoFinanceira OperationType,
    decimal Quantity,
    decimal UnitPrice,
    DateTime? PeriodoInicial,
    DateTime? PeriodoFinal,
    string? Broker);

/// <summary>Um provento extraído de uma linha de Proventos Recebidos.</summary>
public sealed record ProventoB3(
    string Ticker,
    DateTime Pagamento,
    string Tipo,
    decimal Valor,
    decimal Quantidade,
    decimal ValorPorAcao,
    string? Produto);
