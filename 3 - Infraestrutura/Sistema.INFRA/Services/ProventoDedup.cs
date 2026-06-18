using System.Globalization;

namespace Sistema.INFRA.Services;

internal static class ProventoDedup
{
    public static string ChaveEconomica(int assetId, DateTime? dataCom, DateTime? pagamento, string tipo)
    {
        var tipoCanonico = string.IsNullOrWhiteSpace(tipo) ? "PROVENTO" : tipo.Trim().ToUpperInvariant();
        var dataComKey = (dataCom ?? pagamento)?.Date.ToString("yyyyMMdd", CultureInfo.InvariantCulture) ?? "00000000";
        var pagamentoKey = pagamento?.Date.ToString("yyyyMMdd", CultureInfo.InvariantCulture) ?? "00000000";
        return $"Provento|{assetId}|{dataComKey}|{pagamentoKey}|{tipoCanonico}";
    }

    public static bool MesmoValor(decimal atual, decimal novo)
        => Math.Abs(decimal.Round(atual, 2) - decimal.Round(novo, 2)) <= 0.01m;
}
