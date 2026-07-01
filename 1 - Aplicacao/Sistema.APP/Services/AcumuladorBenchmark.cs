using Sistema.CORE.Entities;

namespace Sistema.APP.Services;

/// <summary>
/// Acumulação PURA (sem banco) do retorno de cada benchmark (CDI/IPCA/Ibov) no período da carteira,
/// a partir da série persistida em FinanceiroSerieBenchmark. Ver specs/investimentos.spec.md (F-B F2).
///  - CDI  (% a.d.):  retorno = ∏(1 + valor/100) sobre os dias úteis dentro de [inicio, fim] − 1.
///  - IPCA (% a.m.):  retorno = ∏(1 + valor/100) sobre os meses cujo ponto cai em [inicio, fim] − 1.
///  - Ibov (nível):   retorno = nível_no_fim / nível_no_inicio − 1 (primeiro e último ponto no período).
/// Período vazio / índice ausente → 0 (degrade gracioso; o chamador marca "indisponível").
/// </summary>
public static class AcumuladorBenchmark
{
    public readonly record struct PontoBenchmark(IndiceBenchmark Indice, DateTime Data, decimal Valor);

    /// <summary>CDI/IPCA: produtório de (1 + taxa%/100) sobre os pontos no intervalo [inicio, fim].</summary>
    public static decimal AcumularTaxa(IEnumerable<PontoBenchmark> pontos, DateTime inicio, DateTime fim)
    {
        var fator = 1m;
        var temAlgum = false;
        foreach (var p in pontos)
        {
            if (p.Data.Date < inicio.Date || p.Data.Date > fim.Date)
                continue;
            temAlgum = true;
            fator *= 1m + p.Valor / 100m;
        }

        return temAlgum ? fator - 1m : 0m;
    }

    /// <summary>Ibovespa (nível): fim/início − 1, usando o 1º e o último ponto dentro de [inicio, fim].</summary>
    public static decimal AcumularNivel(IEnumerable<PontoBenchmark> pontos, DateTime inicio, DateTime fim)
    {
        var noPeriodo = pontos
            .Where(p => p.Data.Date >= inicio.Date && p.Data.Date <= fim.Date && p.Valor > 0m)
            .OrderBy(p => p.Data)
            .ToList();
        if (noPeriodo.Count < 2)
            return 0m;

        var nivelInicio = noPeriodo[0].Valor;
        var nivelFim = noPeriodo[^1].Valor;
        return nivelInicio <= 0m ? 0m : nivelFim / nivelInicio - 1m;
    }

    /// <summary>Acumula o índice no período conforme a natureza da série (taxa diária/mensal ou nível).</summary>
    public static decimal Acumular(IndiceBenchmark indice, IEnumerable<PontoBenchmark> pontos, DateTime inicio, DateTime fim)
        => indice switch
        {
            IndiceBenchmark.Cdi or IndiceBenchmark.Ipca => AcumularTaxa(pontos, inicio, fim),
            IndiceBenchmark.Ibov => AcumularNivel(pontos, inicio, fim),
            _ => 0m
        };

    /// <summary>
    /// Série base 100 alinhada ao eixo <paramref name="datas"/>: para cada data d, o valor é
    /// 100 × (1 + retorno acumulado de [datas[0], d]). Para taxa (CDI/IPCA) acumula o produtório até d;
    /// para nível (Ibov) usa nivel(d)/nivel(início) com forward-fill (mantém o último nível conhecido nos
    /// dias sem ponto). Vazio se não houver datas; começa em 100. Usado para o gráfico sobreposto.
    /// </summary>
    public static IReadOnlyList<decimal> SerieBase100(IndiceBenchmark indice, IReadOnlyList<DateTime> datas, IReadOnlyList<PontoBenchmark> pontos)
    {
        if (datas.Count == 0)
            return [];

        return indice == IndiceBenchmark.Ibov
            ? SerieBase100Nivel(datas, pontos)
            : SerieBase100Taxa(datas, pontos);
    }

    private static IReadOnlyList<decimal> SerieBase100Taxa(IReadOnlyList<DateTime> datas, IReadOnlyList<PontoBenchmark> pontos)
    {
        // Soma das taxas por dia → acumula o produtório dia a dia ao varrer o eixo de datas.
        var taxaPorDia = pontos
            .GroupBy(p => p.Data.Date)
            .ToDictionary(g => g.Key, g => g.Sum(x => x.Valor));

        var serie = new decimal[datas.Count];
        var fator = 1m;
        for (var i = 0; i < datas.Count; i++)
        {
            if (taxaPorDia.TryGetValue(datas[i].Date, out var taxa))
                fator *= 1m + taxa / 100m;
            serie[i] = 100m * fator;
        }

        return serie;
    }

    private static IReadOnlyList<decimal> SerieBase100Nivel(IReadOnlyList<DateTime> datas, IReadOnlyList<PontoBenchmark> pontos)
    {
        var nivelPorDia = pontos
            .Where(p => p.Valor > 0m)
            .GroupBy(p => p.Data.Date)
            .ToDictionary(g => g.Key, g => g.OrderBy(x => x.Data).Last().Valor);

        var serie = new decimal[datas.Count];
        decimal? nivelBase = null;
        decimal? ultimoNivel = null;
        for (var i = 0; i < datas.Count; i++)
        {
            if (nivelPorDia.TryGetValue(datas[i].Date, out var nivel))
            {
                ultimoNivel = nivel;
                nivelBase ??= nivel; // primeiro nível conhecido = base do índice.
            }
            serie[i] = nivelBase is > 0m && ultimoNivel is not null
                ? 100m * ultimoNivel.Value / nivelBase.Value
                : 100m;
        }

        return serie;
    }
}
