using System.Globalization;
using System.Text.Json;
using Sistema.CORE.Entities;

namespace Sistema.INFRA.Services;

/// <summary>
/// Mapeia uma entrada de <c>dividendsData.stockDividends</c> da Brapi para um evento corporativo
/// (desdobramento/grupamento). Puro/testável: não toca em banco nem HTTP.
/// Brapi: <c>factor</c> = multiplicador (10 = 1:10); <c>label</c> = DESDOBRAMENTO/GRUPAMENTO;
/// <c>lastDatePrior</c> = data de corte (último dia pré-split).
/// </summary>
public static class EventoCorporativoBrapiParser
{
    public sealed record EventoBrapi(TipoEventoCorporativo Tipo, decimal Fator, DateTime Data, string Raw);

    public static EventoBrapi? Mapear(JsonElement ev)
    {
        var label = (Str(ev, "label") ?? string.Empty).ToUpperInvariant();
        var factor = Dec(ev, "factor");
        if (factor is null || factor <= 0m)
            return null;

        // Data-ex = data de corte (último dia pré-split) + 1 dia. Sem corte, cai no approvedOn (aprox.).
        var corte = ParseData(Str(ev, "lastDatePrior"));
        var data = corte is not null ? corte.Value.Date.AddDays(1) : ParseData(Str(ev, "approvedOn"));
        if (data is null)
            return null;

        TipoEventoCorporativo tipo;
        decimal fator;
        if (label.Contains("DESDOBR"))
        {
            tipo = TipoEventoCorporativo.Desdobramento;
            fator = factor.Value;            // 10 = 1:10
        }
        else if (label.Contains("GRUPAM"))
        {
            tipo = TipoEventoCorporativo.Grupamento;
            fator = 1m / factor.Value;       // grupamento 10:1 → fator 0,1
        }
        else
        {
            // Bonificação / tipo ambíguo: a semântica do fator varia → não auto-insere (caller alerta).
            return null;
        }

        return new EventoBrapi(tipo, fator, data.Value, ev.GetRawText());
    }

    /// <summary>
    /// Já coberto = existe evento do mesmo ativo numa janela de poucos dias. Trata a diferença de
    /// convenção entre a data de corte da Brapi e a data-ex já semeada/manual (não duplica nem reaplica).
    /// </summary>
    public static bool JaCoberto(int assetId, DateTime data, IEnumerable<(int AssetId, DateTime Data)> existentes, int janelaDias = 7)
        => existentes.Any(e => e.AssetId == assetId && Math.Abs((e.Data.Date - data.Date).TotalDays) <= janelaDias);

    private static string? Str(JsonElement el, string name)
        => el.TryGetProperty(name, out var p) && p.ValueKind == JsonValueKind.String ? p.GetString() : null;

    private static decimal? Dec(JsonElement el, string name)
    {
        if (!el.TryGetProperty(name, out var p))
            return null;
        if (p.ValueKind == JsonValueKind.Number && p.TryGetDecimal(out var d))
            return d;
        if (p.ValueKind == JsonValueKind.String && decimal.TryParse(p.GetString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var ds))
            return ds;
        return null;
    }

    private static DateTime? ParseData(string? s)
        => DateTime.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var dt)
            ? dt
            : null;
}
