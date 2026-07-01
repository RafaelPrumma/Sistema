using System.Globalization;

namespace Sistema.APP.Services;

/// <summary>
/// Parser PURO (sem HTTP) da resposta do BCB SGS: cada item é { "data":"dd/MM/yyyy", "valor":"x,xx"|"x.xx" }.
/// O número pode vir com vírgula (pt-BR) ou ponto (en-US) como separador decimal — aceita ambos.
/// Mantido separado do HttpClient para ser testável. Ver specs/investimentos.spec.md (F-B F2).
/// </summary>
public static class BenchmarkSgsParser
{
    public readonly record struct PontoSgs(DateTime Data, decimal Valor);

    /// <summary>Converte (data, valor) crus do SGS num ponto tipado; null se qualquer campo for inválido.</summary>
    public static PontoSgs? Parse(string? data, string? valor)
    {
        var d = ParseData(data);
        var v = ParseValor(valor);
        if (d is null || v is null)
            return null;
        return new PontoSgs(d.Value, v.Value);
    }

    /// <summary>Mapeia a lista crua do SGS para pontos válidos (descarta os malformados — à prova de falha).</summary>
    public static IReadOnlyList<PontoSgs> ParseMuitos(IEnumerable<(string? Data, string? Valor)> itens)
    {
        var pontos = new List<PontoSgs>();
        foreach (var (data, valor) in itens)
            if (Parse(data, valor) is PontoSgs p)
                pontos.Add(p);
        return pontos;
    }

    private static DateTime? ParseData(string? data)
        => DateTime.TryParseExact(data, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed)
            ? DateTime.SpecifyKind(parsed.Date, DateTimeKind.Utc)
            : null;

    // O SGS pode mandar "0,043739" (pt-BR) ou "0.043739" (en-US). Normaliza para ponto e parseia invariante.
    private static decimal? ParseValor(string? valor)
    {
        if (string.IsNullOrWhiteSpace(valor))
            return null;
        var normalizado = valor.Trim().Replace(",", ".", StringComparison.Ordinal);
        return decimal.TryParse(normalizado, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed) ? parsed : null;
    }
}
