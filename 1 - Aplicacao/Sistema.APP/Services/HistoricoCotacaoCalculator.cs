namespace Sistema.APP.Services;

/// <summary>
/// OHLC puro de um bucket de preço (sem EF), usado pelo bucketing intradiário (30m) e pela
/// consolidação diária (1d). Date é o início do bucket em UTC.
/// </summary>
public readonly record struct CandleOhlc(
    DateTime Date,
    decimal Open,
    decimal High,
    decimal Low,
    decimal Close,
    decimal CloseBRL);

/// <summary>
/// Motor PURO (sem banco) do modelo de cotações/histórico (specs/investimentos.spec.md, F-P):
/// cálculo do início do bucket de 30 min em UTC, merge OHLC dentro do mesmo bucket, consolidação
/// diária (1d) a partir dos buckets 30m e a regra de retenção do intradiário.
/// </summary>
public static class HistoricoCotacaoCalculator
{
    public const string Intervalo30m = "30m";
    public const string Intervalo1d = "1d";

    /// <summary>Duração da retenção do bucket 30m após o fim do dia (24h, conforme a spec).</summary>
    public static readonly TimeSpan RetencaoIntradiario = TimeSpan.FromHours(24);

    /// <summary>
    /// Início do bucket de 30 min em UTC: trunca ao múltiplo de 30 minutos (segundos/ms zerados).
    /// Ex.: 14:37:12 → 14:30:00; 14:05 → 14:00; 15:30 → 15:30.
    /// </summary>
    public static DateTime InicioBucket30m(DateTime instante)
    {
        var utc = instante.Kind == DateTimeKind.Utc ? instante : instante.ToUniversalTime();
        var minutoBucket = (utc.Minute / 30) * 30;
        return new DateTime(utc.Year, utc.Month, utc.Day, utc.Hour, minutoBucket, 0, DateTimeKind.Utc);
    }

    /// <summary>
    /// OHLC de um bucket 30m NOVO: Open=High=Low=Close=preço atual; CloseBRL=preço atual em BRL.
    /// </summary>
    public static CandleOhlc NovoBucket30m(DateTime instante, decimal preco, decimal precoBrl)
    {
        var date = InicioBucket30m(instante);
        return new CandleOhlc(date, preco, preco, preco, preco, precoBrl);
    }

    /// <summary>
    /// Merge de uma nova cotação num bucket 30m já existente: preserva Open, estende High/Low
    /// (máx/mín) e atualiza Close/CloseBRL com a cotação mais recente do bucket.
    /// </summary>
    public static CandleOhlc Merge30m(CandleOhlc existente, decimal preco, decimal precoBrl)
        => existente with
        {
            High = Math.Max(existente.High, preco),
            Low = Math.Min(existente.Low, preco),
            Close = preco,
            CloseBRL = precoBrl
        };

    /// <summary>
    /// Consolida o candle diário (1d) a partir dos buckets 30m de um mesmo dia/ativo/provedor:
    /// Open=primeiro bucket, High/Low=máx/mín, Close/CloseBRL=último bucket válido (mais recente).
    /// "Válido" = Close &gt; 0 (ignora buckets zerados de falha). A Date do 1d é a data (00:00 UTC).
    /// Retorna null se não houver bucket válido.
    /// </summary>
    public static CandleOhlc? ConsolidarDiario(IEnumerable<CandleOhlc> buckets30m)
    {
        var ordenados = buckets30m
            .Where(b => b.Close > 0m)
            .OrderBy(b => b.Date)
            .ToList();
        if (ordenados.Count == 0)
            return null;

        var primeiro = ordenados[0];
        var ultimo = ordenados[^1];
        return new CandleOhlc(
            primeiro.Date.Date,
            primeiro.Open,
            ordenados.Max(b => b.High),
            ordenados.Min(b => b.Low),
            ultimo.Close,
            ultimo.CloseBRL);
    }

    /// <summary>
    /// Regra de retenção: um bucket 30m pode ser apagado quando (a) já passou de 24h do fim do
    /// dia do bucket E (b) o candle 1d daquele ativo/provedor/dia já existe (fechamento persistido).
    /// </summary>
    public static bool PodeApagarBucket30m(DateTime dataBucketUtc, DateTime agoraUtc, bool fechamentoDiarioExiste)
    {
        if (!fechamentoDiarioExiste)
            return false;
        var fimDoDia = dataBucketUtc.Date.AddDays(1);
        return agoraUtc - fimDoDia >= RetencaoIntradiario;
    }
}
