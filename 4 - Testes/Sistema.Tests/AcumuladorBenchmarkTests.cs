using System.Globalization;
using Sistema.APP.Services;
using Sistema.CORE.Entities;

namespace Sistema.Tests;

/// <summary>
/// Testa a acumulação PURA dos benchmarks (CDI/IPCA por produtório de taxas; Ibov por nível) e a
/// série base 100 para o gráfico. Ver specs/investimentos.spec.md (F-B F2).
/// </summary>
public class AcumuladorBenchmarkTests
{
    private static DateTime D(string data) => DateTime.Parse(data, CultureInfo.InvariantCulture);

    private static AcumuladorBenchmark.PontoBenchmark P(IndiceBenchmark indice, string data, decimal valor)
        => new(indice, D(data), valor);

    [Fact]
    public void AcumularTaxa_Cdi_ProdutorioDosDias()
    {
        var pontos = new[]
        {
            P(IndiceBenchmark.Cdi, "2025-01-02", 0.04m), // % a.d.
            P(IndiceBenchmark.Cdi, "2025-01-03", 0.04m),
            P(IndiceBenchmark.Cdi, "2025-01-06", 0.04m),
        };

        // (1,0004)^3 - 1 ≈ 0,00120048
        var acc = AcumuladorBenchmark.Acumular(IndiceBenchmark.Cdi, pontos, D("2025-01-01"), D("2025-01-31"));
        Assert.Equal(0.00120048m, acc, 8);
    }

    [Fact]
    public void AcumularTaxa_RespeitaJanelaDoPeriodo()
    {
        var pontos = new[]
        {
            P(IndiceBenchmark.Cdi, "2024-12-31", 1.00m), // fora do período (anterior)
            P(IndiceBenchmark.Cdi, "2025-01-02", 0.10m),
            P(IndiceBenchmark.Cdi, "2025-02-10", 1.00m), // fora do período (posterior)
        };

        // só o ponto de 02/01 conta → 0,10%
        var acc = AcumuladorBenchmark.Acumular(IndiceBenchmark.Cdi, pontos, D("2025-01-01"), D("2025-01-31"));
        Assert.Equal(0.001m, acc, 8);
    }

    [Fact]
    public void AcumularTaxa_Ipca_ProdutorioMensal()
    {
        var pontos = new[]
        {
            P(IndiceBenchmark.Ipca, "2025-01-01", 0.50m), // % a.m.
            P(IndiceBenchmark.Ipca, "2025-02-01", 0.30m),
        };

        // 1,005 * 1,003 - 1 = 0,008015
        var acc = AcumuladorBenchmark.Acumular(IndiceBenchmark.Ipca, pontos, D("2025-01-01"), D("2025-03-31"));
        Assert.Equal(0.008015m, acc, 8);
    }

    [Fact]
    public void AcumularNivel_Ibov_FimSobreInicio()
    {
        var pontos = new[]
        {
            P(IndiceBenchmark.Ibov, "2025-01-02", 120000m),
            P(IndiceBenchmark.Ibov, "2025-06-30", 132000m),
        };

        // 132000/120000 - 1 = 0,10
        var acc = AcumuladorBenchmark.Acumular(IndiceBenchmark.Ibov, pontos, D("2025-01-01"), D("2025-12-31"));
        Assert.Equal(0.10m, acc, 6);
    }

    [Fact]
    public void Acumular_PeriodoVazio_RetornaZero()
    {
        var pontos = new[] { P(IndiceBenchmark.Cdi, "2025-06-01", 0.04m) };
        // período sem nenhum ponto dentro da janela
        Assert.Equal(0m, AcumuladorBenchmark.Acumular(IndiceBenchmark.Cdi, pontos, D("2025-01-01"), D("2025-01-31")));
    }

    [Fact]
    public void Acumular_IndiceAusente_RetornaZero()
    {
        // nenhum ponto do índice pedido → 0 (degrade gracioso "indisponível")
        Assert.Equal(0m, AcumuladorBenchmark.Acumular(IndiceBenchmark.Ibov, [], D("2025-01-01"), D("2025-12-31")));
    }

    [Fact]
    public void AcumularNivel_UmUnicoPonto_RetornaZero()
    {
        var pontos = new[] { P(IndiceBenchmark.Ibov, "2025-03-01", 120000m) };
        Assert.Equal(0m, AcumuladorBenchmark.Acumular(IndiceBenchmark.Ibov, pontos, D("2025-01-01"), D("2025-12-31")));
    }

    [Fact]
    public void SerieBase100_Taxa_AcumulaDiaADia()
    {
        var datas = new[] { D("2025-01-01"), D("2025-01-02"), D("2025-01-03") };
        var pontos = new[]
        {
            P(IndiceBenchmark.Cdi, "2025-01-02", 0.10m),
            P(IndiceBenchmark.Cdi, "2025-01-03", 0.10m),
        };

        var serie = AcumuladorBenchmark.SerieBase100(IndiceBenchmark.Cdi, datas, pontos);
        Assert.Equal(3, serie.Count);
        Assert.Equal(100m, serie[0], 6);          // base
        Assert.Equal(100.10m, serie[1], 4);       // +0,10%
        Assert.Equal(100.2001m, serie[2], 4);     // 100,10 * 1,001
    }

    [Fact]
    public void SerieBase100_Nivel_ForwardFillEBase100NoPrimeiroNivel()
    {
        var datas = new[] { D("2025-01-01"), D("2025-01-02"), D("2025-01-03") };
        var pontos = new[]
        {
            P(IndiceBenchmark.Ibov, "2025-01-02", 100000m), // primeiro nível conhecido = base
            // 03/01 sem ponto → forward-fill mantém 100000 → base 100
        };

        var serie = AcumuladorBenchmark.SerieBase100(IndiceBenchmark.Ibov, datas, pontos);
        Assert.Equal(100m, serie[0], 6); // antes do 1º nível → 100
        Assert.Equal(100m, serie[1], 6); // no nível base → 100
        Assert.Equal(100m, serie[2], 6); // forward-fill → 100
    }

    [Fact]
    public void SerieBase100_DatasVazias_RetornaVazio()
        => Assert.Empty(AcumuladorBenchmark.SerieBase100(IndiceBenchmark.Cdi, [], []));
}
