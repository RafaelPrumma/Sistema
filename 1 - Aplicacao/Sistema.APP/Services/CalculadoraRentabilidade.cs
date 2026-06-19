using Sistema.APP.DTOs;

namespace Sistema.APP.Services;

/// <summary>
/// Motor PURO de rentabilidade (sem banco): TWR (time-weighted, sistema de cotas), MWR/TIR
/// (money-weighted) e comparação com benchmark + rentabilidade real. Consome a curva diária
/// (valor + fluxo) que o app monta a partir de CriarEvolucaoPatrimonio. Ver specs/investimentos.spec.md (F-B).
/// </summary>
public static class CalculadoraRentabilidade
{
    private const decimal CotaInicial = 100m;

    /// <summary>
    /// TWR pelo **sistema de cotas**: aportes/resgates emitem/resgatam cotas pelo valor da cota do dia,
    /// então o valor da cota reflete só a performance (neutraliza timing e tamanho dos aportes).
    /// </summary>
    public static decimal CalcularTwr(IReadOnlyList<PontoRentabilidadeDto> serie)
    {
        decimal cotas = 0m, valorAnterior = 0m, valorCota = CotaInicial;

        foreach (var p in serie.OrderBy(x => x.Data))
        {
            var baseInicio = valorAnterior + p.FluxoLiquido;
            if (cotas <= 0m)
            {
                if (baseInicio <= 0m)
                {
                    valorAnterior = p.Valor > 0m ? p.Valor : 0m;
                    continue;
                }
                cotas = baseInicio / CotaInicial; // primeira capitalização: cota vale 100.
            }
            else if (p.FluxoLiquido != 0m)
            {
                var cotaAntesFluxo = valorAnterior / cotas; // valor da cota no início do dia (pré-fluxo)
                if (cotaAntesFluxo > 0m)
                    cotas += p.FluxoLiquido / cotaAntesFluxo;
            }

            if (cotas > 0m)
                valorCota = p.Valor / cotas;
            valorAnterior = p.Valor;
        }

        return valorCota / CotaInicial - 1m;
    }

    /// <summary>Anualiza um retorno total acumulado em <paramref name="dias"/> corridos.</summary>
    public static decimal Anualizar(decimal retornoTotal, int dias)
        => dias <= 0 ? 0m : (decimal)(Math.Pow((double)(1m + retornoTotal), 365.0 / dias) - 1.0);

    /// <summary>
    /// MWR/TIR anualizada: taxa que zera o VPL dos fluxos (aporte negativo, resgate/valor final positivo).
    /// Resolvida por bissecção. Reflete a experiência real (ponderada pelo capital investido).
    /// </summary>
    public static decimal CalcularMwr(IReadOnlyList<(DateTime Data, decimal Valor)> fluxos)
    {
        var ordenados = fluxos.OrderBy(f => f.Data).ToList();
        if (ordenados.Count < 2)
            return 0m;
        if (ordenados.All(f => f.Valor >= 0m) || ordenados.All(f => f.Valor <= 0m))
            return 0m; // sem sinais opostos → TIR indefinida.

        var d0 = ordenados[0].Data;
        double Vpl(double taxa) => ordenados.Sum(f =>
            (double)f.Valor / Math.Pow(1.0 + taxa, (f.Data - d0).Days / 365.0));

        double lo = -0.9999, hi = 10.0;
        double fLo = Vpl(lo), fHi = Vpl(hi);
        if (fLo * fHi > 0)
            return 0m; // sem raiz no intervalo testado.

        for (var i = 0; i < 200; i++)
        {
            var mid = (lo + hi) / 2.0;
            var fMid = Vpl(mid);
            if (Math.Abs(fMid) < 1e-9)
                return (decimal)mid;
            if (fLo * fMid < 0) { hi = mid; }
            else { lo = mid; fLo = fMid; }
        }

        return (decimal)((lo + hi) / 2.0);
    }

    /// <summary>Compara a carteira com um benchmark acumulado no mesmo período.</summary>
    public static ComparativoBenchmarkDto CompararBenchmark(string nome, decimal retornoCarteira, decimal retornoBenchmark)
        => new(
            nome,
            retornoBenchmark,
            retornoCarteira - retornoBenchmark,
            retornoBenchmark == -1m ? 0m : (1m + retornoCarteira) / (1m + retornoBenchmark) - 1m);

    /// <summary>Rentabilidade real: desconta a inflação (IPCA) do retorno nominal.</summary>
    public static decimal RetornoReal(decimal nominal, decimal inflacao)
        => inflacao == -1m ? 0m : (1m + nominal) / (1m + inflacao) - 1m;

    /// <summary>Apura TWR + MWR + comparação com benchmarks já acumulados no período da série.</summary>
    public static RentabilidadeDto Apurar(
        IReadOnlyList<PontoRentabilidadeDto> serie,
        IReadOnlyList<(string Nome, decimal RetornoAcumulado)> benchmarks)
    {
        if (serie.Count == 0)
            return new RentabilidadeDto(0m, 0m, 0m, 0, []);

        var ordenada = serie.OrderBy(x => x.Data).ToList();
        var dias = (ordenada[^1].Data.Date - ordenada[0].Data.Date).Days;
        var twr = CalcularTwr(ordenada);

        // Fluxos para a TIR: aporte = saída do bolso (negativo); valor final = resgate hipotético (positivo).
        var fluxos = ordenada
            .Where(p => p.FluxoLiquido != 0m)
            .Select(p => (p.Data, Valor: -p.FluxoLiquido))
            .ToList();
        fluxos.Add((ordenada[^1].Data, ordenada[^1].Valor));
        var mwr = CalcularMwr(fluxos);

        var comparativos = benchmarks
            .Select(b => CompararBenchmark(b.Nome, twr, b.RetornoAcumulado))
            .ToList();

        return new RentabilidadeDto(twr, Anualizar(twr, dias), mwr, dias, comparativos);
    }
}
