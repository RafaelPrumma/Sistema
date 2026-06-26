using Sistema.APP.Services;

namespace Sistema.Tests;

// F-G — lógica pura de metas + rebalanceamento (sem DbContext). Cobre: desvio atual×alvo por carteira
// (p.p. e %), "falta aportar" no patrimônio atual, distribuição de um aporte hipotético proporcional ao
// déficit (acima do alvo não recebe), e a sanidade da soma dos alvos.
public class CalculadoraMetasCarteiraTests
{
    private static CalculadoraMetasCarteira.EntradaMeta Carteira(int id, string nome, decimal valor, decimal? alvo)
        => new(id, nome, valor, alvo);

    [Fact]
    public void SemAlvoDefinido_DevolveSemMetas()
    {
        var entradas = new[]
        {
            Carteira(1, "Bancos", 6000m, null),
            Carteira(2, "FIIs", 4000m, null)
        };

        var dto = CalculadoraMetasCarteira.Calcular(entradas);

        Assert.True(dto.SemMetas);
        Assert.Empty(dto.Carteiras);
        Assert.Equal(10000m, dto.PatrimonioTotal); // patrimônio ainda é somado, mesmo sem metas
    }

    [Fact]
    public void Desvio_AtualVersusAlvo_EmPontosEPercentual()
    {
        // Patrimônio 10.000. Bancos = 6.000 (60% atual) com alvo 50% → +10 p.p. (+20% relativo).
        // FIIs = 4.000 (40% atual) com alvo 50% → −10 p.p. (−20% relativo).
        var entradas = new[]
        {
            Carteira(1, "Bancos", 6000m, 50m),
            Carteira(2, "FIIs", 4000m, 50m)
        };

        var dto = CalculadoraMetasCarteira.Calcular(entradas);

        Assert.False(dto.SemMetas);
        Assert.False(dto.AlvoForaDeCem); // 50 + 50 = 100
        Assert.Equal(2, dto.Carteiras.Count);

        var bancos = dto.Carteiras.Single(c => c.Nome == "Bancos");
        Assert.Equal(60m, bancos.PesoAtual);
        Assert.Equal(50m, bancos.PesoAlvo);
        Assert.Equal(10m, bancos.DesvioPontos);
        Assert.Equal(20m, bancos.DesvioPercentual);
        Assert.Equal(0m, bancos.FaltaParaAlvo);        // está acima do alvo → nada a aportar
        Assert.Equal(1000m, bancos.SobraSobreAlvo);    // 6000 − 50%*10000 = 1000

        var fiis = dto.Carteiras.Single(c => c.Nome == "FIIs");
        Assert.Equal(40m, fiis.PesoAtual);
        Assert.Equal(-10m, fiis.DesvioPontos);
        Assert.Equal(-20m, fiis.DesvioPercentual);
        Assert.Equal(1000m, fiis.FaltaParaAlvo);       // 50%*10000 − 4000 = 1000
        Assert.Equal(0m, fiis.SobraSobreAlvo);
    }

    [Fact]
    public void CarteiraAbaixoDoAlvo_RecebeAporte_AcimaNaoRecebe()
    {
        // Bancos 60% (alvo 50%) acima; FIIs 40% (alvo 50%) abaixo. Aporte hipotético 1.000.
        // Todo o aporte vai para FIIs (único déficit), capado no déficit (1.000).
        var entradas = new[]
        {
            Carteira(1, "Bancos", 6000m, 50m),
            Carteira(2, "FIIs", 4000m, 50m)
        };

        var dto = CalculadoraMetasCarteira.Calcular(entradas, aporteHipotetico: 1000m);

        Assert.Equal(1000m, dto.AporteHipotetico);
        Assert.Equal(0m, dto.Carteiras.Single(c => c.Nome == "Bancos").AporteSugerido);
        Assert.Equal(1000m, dto.Carteiras.Single(c => c.Nome == "FIIs").AporteSugerido);
    }

    [Fact]
    public void AporteHipotetico_DistribuiProporcionalAoDeficit()
    {
        // Patrimônio 10.000. A: 1.000 (alvo 40% → déficit 3.000). B: 2.000 (alvo 40% → déficit 2.000).
        // C: 7.000 (alvo 20% → sobra, sem aporte). Déficit total 5.000. Aporte 5.000 cobre os dois déficits.
        var entradas = new[]
        {
            Carteira(1, "A", 1000m, 40m),
            Carteira(2, "B", 2000m, 40m),
            Carteira(3, "C", 7000m, 20m)
        };

        var dto = CalculadoraMetasCarteira.Calcular(entradas, aporteHipotetico: 5000m);

        Assert.Equal(3000m, dto.Carteiras.Single(c => c.Nome == "A").AporteSugerido);
        Assert.Equal(2000m, dto.Carteiras.Single(c => c.Nome == "B").AporteSugerido);
        Assert.Equal(0m, dto.Carteiras.Single(c => c.Nome == "C").AporteSugerido);
        // O total sugerido não ultrapassa o aporte disponível.
        Assert.True(dto.Carteiras.Sum(c => c.AporteSugerido) <= dto.AporteHipotetico);
    }

    [Fact]
    public void AporteParcial_NaoUltrapassaDeficitNemAporteDisponivel()
    {
        // Mesmo cenário, mas aporte 1.000 < déficit total 5.000 → distribuído 3/5 e 2/5.
        var entradas = new[]
        {
            Carteira(1, "A", 1000m, 40m),
            Carteira(2, "B", 2000m, 40m),
            Carteira(3, "C", 7000m, 20m)
        };

        var dto = CalculadoraMetasCarteira.Calcular(entradas, aporteHipotetico: 1000m);

        Assert.Equal(600m, dto.Carteiras.Single(c => c.Nome == "A").AporteSugerido); // 1000 * 3/5
        Assert.Equal(400m, dto.Carteiras.Single(c => c.Nome == "B").AporteSugerido); // 1000 * 2/5
        Assert.Equal(1000m, dto.Carteiras.Sum(c => c.AporteSugerido));
    }

    [Fact]
    public void SomaDosAlvos_DiferenteDeCem_SinalizaAviso()
    {
        // 50 + 30 = 80 (≠ 100) → AlvoForaDeCem; carteira sem alvo (null) é ignorada na soma e nas linhas.
        var entradas = new[]
        {
            Carteira(1, "Bancos", 5000m, 50m),
            Carteira(2, "FIIs", 3000m, 30m),
            Carteira(3, "Cripto", 2000m, null)
        };

        var dto = CalculadoraMetasCarteira.Calcular(entradas);

        Assert.True(dto.AlvoForaDeCem);
        Assert.Equal(80m, dto.SomaPesoAlvo);
        Assert.Equal(2, dto.Carteiras.Count); // Cripto (sem alvo) não vira linha de meta
        Assert.DoesNotContain(dto.Carteiras, c => c.Nome == "Cripto");
    }

    [Fact]
    public void PatrimonioZero_NaoEstoura()
    {
        var entradas = new[] { Carteira(1, "Bancos", 0m, 100m) };

        var dto = CalculadoraMetasCarteira.Calcular(entradas);

        Assert.False(dto.SemMetas);
        Assert.Equal(0m, dto.PatrimonioTotal);
        Assert.Equal(0m, dto.Carteiras[0].PesoAtual);
        Assert.Equal(-100m, dto.Carteiras[0].DesvioPontos);
    }

    [Fact]
    public void OrdenaPorMaiorDesvioAbsoluto()
    {
        // Patrimônio 10.000. Bancos +5 p.p., FIIs +10 p.p., Cripto −15 p.p. → ordem Cripto, FIIs, Bancos.
        var entradas = new[]
        {
            Carteira(1, "Bancos", 3500m, 30m),  // 35% − 30% = +5
            Carteira(2, "FIIs", 4000m, 30m),    // 40% − 30% = +10
            Carteira(3, "Cripto", 2500m, 40m)   // 25% − 40% = −15
        };

        var dto = CalculadoraMetasCarteira.Calcular(entradas);

        Assert.Equal("Cripto", dto.Carteiras[0].Nome);
        Assert.Equal("FIIs", dto.Carteiras[1].Nome);
        Assert.Equal("Bancos", dto.Carteiras[2].Nome);
    }
}
