using Sistema.APP.Services;

namespace Sistema.Tests;

// F-H — lógica pura dos alertas de ESTADO: ativos detidos sem carteira + divergência custódia acima
// do limiar. Cobre os casos de borda (limiar desabilitado, percentual vs absoluto, divergência zero).
public class AvaliadorAlertaEstadoTests
{
    [Fact]
    public void SemCarteira_RetornaApenasDetidosForaDasCarteiras()
    {
        var detidos = new[] { 1, 2, 3, 4 };
        var emCarteira = new HashSet<int> { 2, 4 };

        var fora = AvaliadorAlertaEstado.AtivosSemCarteira(detidos, emCarteira);

        Assert.Equal(new[] { 1, 3 }, fora);
    }

    [Fact]
    public void SemCarteira_TodosEmCarteira_RetornaVazio()
    {
        var fora = AvaliadorAlertaEstado.AtivosSemCarteira(new[] { 1, 2 }, new HashSet<int> { 1, 2, 9 });
        Assert.Empty(fora);
    }

    [Fact]
    public void SemCarteira_NenhumaCarteira_RetornaTodos()
    {
        var fora = AvaliadorAlertaEstado.AtivosSemCarteira(new[] { 5, 6 }, new HashSet<int>());
        Assert.Equal(new[] { 5, 6 }, fora);
    }

    [Fact]
    public void Divergencia_AbsolutoUltrapassado_Dispara()
    {
        // |1500| >= limiar absoluto 1000 → dispara, mesmo sem regra de percentual.
        var disparar = AvaliadorAlertaEstado.DivergenciaAcimaDoLimiar(1500m, 100000m, limiarAbsoluto: 1000m, limiarPercentual: 0m);
        Assert.True(disparar);
    }

    [Fact]
    public void Divergencia_AbsolutoUsaModulo_DivergenciaNegativaDispara()
    {
        var disparar = AvaliadorAlertaEstado.DivergenciaAcimaDoLimiar(-1500m, 100000m, 1000m, 0m);
        Assert.True(disparar);
    }

    [Fact]
    public void Divergencia_AbaixoDoAbsolutoSemPercentual_NaoDispara()
    {
        var disparar = AvaliadorAlertaEstado.DivergenciaAcimaDoLimiar(500m, 100000m, limiarAbsoluto: 1000m, limiarPercentual: 0m);
        Assert.False(disparar);
    }

    [Fact]
    public void Divergencia_PercentualUltrapassado_Dispara()
    {
        // 600 / 10000 = 6% >= 5% → dispara (sem limiar absoluto habilitado).
        var disparar = AvaliadorAlertaEstado.DivergenciaAcimaDoLimiar(600m, 10000m, limiarAbsoluto: 0m, limiarPercentual: 5m);
        Assert.True(disparar);
    }

    [Fact]
    public void Divergencia_PercentualAbaixo_NaoDispara()
    {
        // 400 / 10000 = 4% < 5% → não dispara.
        var disparar = AvaliadorAlertaEstado.DivergenciaAcimaDoLimiar(400m, 10000m, 0m, 5m);
        Assert.False(disparar);
    }

    [Fact]
    public void Divergencia_QualquerLimiarSatisfeito_Dispara()
    {
        // Abaixo do absoluto (1000) mas acima do percentual (5% de 10000 = 500) → dispara pelo percentual.
        var disparar = AvaliadorAlertaEstado.DivergenciaAcimaDoLimiar(600m, 10000m, limiarAbsoluto: 1000m, limiarPercentual: 5m);
        Assert.True(disparar);
    }

    [Fact]
    public void Divergencia_DivergenciaZero_NuncaDispara()
    {
        var disparar = AvaliadorAlertaEstado.DivergenciaAcimaDoLimiar(0m, 10000m, limiarAbsoluto: 1m, limiarPercentual: 1m);
        Assert.False(disparar);
    }

    [Fact]
    public void Divergencia_NenhumLimiarHabilitado_NaoDispara()
    {
        var disparar = AvaliadorAlertaEstado.DivergenciaAcimaDoLimiar(9999m, 10000m, limiarAbsoluto: 0m, limiarPercentual: 0m);
        Assert.False(disparar);
    }

    [Fact]
    public void Divergencia_PercentualSemPatrimonio_NaoDispara()
    {
        // patrimônio de referência <= 0 desabilita a regra de percentual (evita divisão por zero/falso positivo).
        var disparar = AvaliadorAlertaEstado.DivergenciaAcimaDoLimiar(9999m, 0m, limiarAbsoluto: 0m, limiarPercentual: 1m);
        Assert.False(disparar);
    }
}
