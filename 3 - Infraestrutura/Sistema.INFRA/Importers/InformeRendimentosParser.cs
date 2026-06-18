using System.Globalization;
using System.Text.RegularExpressions;
using Sistema.CORE.Entities;

namespace Sistema.INFRA.Importers;

public static partial class InformeRendimentosParser
{
    private static readonly CultureInfo Br = CultureInfo.GetCultureInfo("pt-BR");

    public static IReadOnlyList<InformeRendimentoLinha> Extrair(string texto)
    {
        var linhas = (texto ?? string.Empty)
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(NormalizarEspacos)
            .Where(l => l.Length > 0)
            .ToList();
        var resultados = new List<InformeRendimentoLinha>();

        foreach (var linha in linhas)
        {
            var tipo = ClassificarTipo(linha);
            if (tipo is null)
                continue;

            var ticker = TickerRegex().Match(linha).Value;
            if (string.IsNullOrWhiteSpace(ticker))
                continue;

            var valores = MoneyRegex().Matches(linha)
                .Select(m => ParseMoeda(m.Value))
                .Where(v => v > 0m)
                .ToList();
            if (valores.Count == 0)
                continue;

            var datas = DateRegex().Matches(linha)
                .Select(m => ParseData(m.Value))
                .Where(d => d.HasValue)
                .Select(d => d!.Value.Date)
                .ToList();
            var pagamento = datas.LastOrDefault(DateTime.MinValue);
            var referencia = datas.Count > 1 ? datas[^2] : pagamento;
            if (pagamento == DateTime.MinValue)
                pagamento = new DateTime(ExtrairAno(linha) ?? DateTime.UtcNow.Year, 12, 31);
            if (referencia == DateTime.MinValue)
                referencia = pagamento;

            resultados.Add(new InformeRendimentoLinha(
                ticker.ToUpperInvariant(),
                tipo.Value.Tipo,
                tipo.Value.Tributacao,
                pagamento,
                referencia,
                valores.Last(),
                linha));
        }

        return resultados
            .GroupBy(x => x.Chave, StringComparer.OrdinalIgnoreCase)
            .Select(g => g.OrderByDescending(x => x.Valor).First())
            .ToList();
    }

    private static (string Tipo, string Tributacao)? ClassificarTipo(string linha)
    {
        var upper = RemoverAcentos(linha).ToUpperInvariant();
        if (upper.Contains("JCP") || upper.Contains("JUROS SOBRE CAPITAL") || upper.Contains("CAPITAL PROPRIO"))
            return ("JCP", "JCP (15% IRRF)");
        if (upper.Contains("DIVIDENDO"))
            return ("Dividendo", "Isento");
        if (upper.Contains("RENDIMENTO"))
            return ("Rendimento", "Isento (FII)");
        return null;
    }

    private static int? ExtrairAno(string linha)
    {
        var match = Regex.Match(linha, @"\b20\d{2}\b");
        return match.Success ? int.Parse(match.Value, CultureInfo.InvariantCulture) : null;
    }

    private static string NormalizarEspacos(string value)
        => Regex.Replace(value, @"\s+", " ").Trim();

    private static string RemoverAcentos(string value)
        => string.Concat(value.Normalize(System.Text.NormalizationForm.FormD)
            .Where(ch => CharUnicodeInfo.GetUnicodeCategory(ch) != UnicodeCategory.NonSpacingMark));

    private static decimal ParseMoeda(string value)
    {
        value = value.Replace("R$", string.Empty, StringComparison.OrdinalIgnoreCase).Trim();
        return decimal.TryParse(value, NumberStyles.Number, Br, out var parsed) ? parsed : 0m;
    }

    private static DateTime? ParseData(string value)
        => DateTime.TryParseExact(value, "dd/MM/yyyy", Br, DateTimeStyles.None, out var parsed) ? parsed : null;

    [GeneratedRegex(@"\b[A-Z]{4}\d{1,2}\b", RegexOptions.IgnoreCase)]
    private static partial Regex TickerRegex();

    [GeneratedRegex(@"\b\d{2}/\d{2}/\d{4}\b")]
    private static partial Regex DateRegex();

    [GeneratedRegex(@"(?:R\$\s*)?\d{1,3}(?:\.\d{3})*,\d{2,8}")]
    private static partial Regex MoneyRegex();
}

public sealed record InformeRendimentoLinha(
    string Ticker,
    string Tipo,
    string Tributacao,
    DateTime DataPagamento,
    DateTime DataReferencia,
    decimal Valor,
    string RawText)
{
    public string Chave => $"{Ticker}|{DataReferencia:yyyyMMdd}|{DataPagamento:yyyyMMdd}|{Tipo}|{Valor:0.00}";
}
