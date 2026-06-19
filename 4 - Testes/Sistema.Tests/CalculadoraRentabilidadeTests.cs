using System.Globalization;
using Sistema.APP.DTOs;
using Sistema.APP.Services;

namespace Sistema.Tests;

/// <summary>
/// Testa o motor puro de rentabilidade: TWR (sistema de cotas), MWR/TIR, comparação com benchmark
/// e rentabilidade real.
/// </summary>
public class CalculadoraRentabilidadeTests
{
    private static PontoRentabilidadeDto P(string data, decimal valor, decimal fluxo)
        => new(DateTime.Parse(data, CultureInfo.InvariantCulture), valor, fluxo);

    [Fact]
    public void Twr_CrescimentoSimples_RetornaVariacaoDoValor()
    {
        var serie = new[]
        {
            P("2025-01-01", 100m, 100m), // aporte inicial de 100
            P("2025-01-02", 110m, 0m),   // sobe para 110
        };

        Assert.Equal(0.10m, CalculadoraRentabilidade.CalcularTwr(serie), 6); // +10%
    }

    [Fact]
    public void Twr_NeutralizaAporte_NaoInflaPelaEntradaDeCapital()
    {
        var serie = new[]
        {
            P("2025-01-01", 100m, 100m), // aporte 100
            P("2025-01-02", 200m, 100m), // aporte +100 no início (mercado flat no dia)
            P("2025-01-03", 220m, 0m),   // sobe 10% sobre 200
        };

        // TWR só captura os 10% de performance, ignorando que o aporte dobrou o capital.
        Assert.Equal(0.10m, CalculadoraRentabilidade.CalcularTwr(serie), 6);
    }

    [Fact]
    public void Mwr_AporteUnicoUmAno_RetornaTir()
    {
        var fluxos = new[]
        {
            (Data: DateTime.Parse("2025-01-01", CultureInfo.InvariantCulture), Valor: -1000m), // aporte (saída)
            (Data: DateTime.Parse("2026-01-01", CultureInfo.InvariantCulture), Valor: 1100m),  // valor final (365 dias)
        };

        Assert.Equal(0.10m, CalculadoraRentabilidade.CalcularMwr(fluxos), 3); // TIR ~10% a.a.
    }

    [Fact]
    public void CompararBenchmark_CalculaExcessoERelativo()
    {
        var c = CalculadoraRentabilidade.CompararBenchmark("CDI", retornoCarteira: 0.20m, retornoBenchmark: 0.10m);

        Assert.Equal(0.10m, c.ExcessoAbsoluto, 6);            // 20% - 10%
        Assert.Equal(0.090909m, c.RetornoRelativo, 5);       // 1,20/1,10 - 1 (≈109% do CDI)
    }

    [Fact]
    public void RetornoReal_DescontaInflacao()
    {
        // nominal 20%, IPCA 5% → real = 1,20/1,05 - 1 ≈ 14,2857%
        Assert.Equal(0.142857m, CalculadoraRentabilidade.RetornoReal(0.20m, 0.05m), 5);
    }

    [Fact]
    public void Apurar_SerieVazia_RetornaZeroSemErro()
    {
        var r = CalculadoraRentabilidade.Apurar([], []);
        Assert.Equal(0m, r.Twr);
        Assert.Equal(0m, r.Mwr);
        Assert.Empty(r.Benchmarks);
    }

    [Fact]
    public void Apurar_IntegraTwrMwrEBenchmarks()
    {
        var serie = new[]
        {
            P("2025-01-01", 1000m, 1000m),
            P("2026-01-01", 1200m, 0m), // +20% em 365 dias, sem novos aportes
        };

        var r = CalculadoraRentabilidade.Apurar(serie, [("CDI", 0.11m)]);

        Assert.Equal(0.20m, r.Twr, 6);
        Assert.Equal(365, r.Dias);
        Assert.Equal(0.20m, r.Mwr, 3); // sem aportes intermediários, MWR ≈ TWR
        var cdi = Assert.Single(r.Benchmarks);
        Assert.Equal(0.09m, cdi.ExcessoAbsoluto, 6); // 20% - 11%
    }
}
