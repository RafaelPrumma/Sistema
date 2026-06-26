using Sistema.APP.Services;
using Sistema.CORE.Entities;

namespace Sistema.Tests;

// F-H — lógica pura do "o preço cruzou o limiar?" com histerese de re-disparo (sem DbContext).
// Cobre: dispara ao cruzar (acima/abaixo), não redispara enquanto armado, re-arma ao voltar do outro
// lado, ignora preço inválido (<= 0) e a sobrecarga que lê o estado da entidade.
public class AvaliadorAlertaPrecoTests
{
    [Fact]
    public void Acima_DisparaQuandoPrecoAtingeOuUltrapassaLimiar()
    {
        var d = AvaliadorAlertaPreco.Avaliar(DirecaoAlertaPreco.Acima, limiar: 30m, precoAtual: 31m, jaDisparado: false);
        Assert.True(d.Disparar);
        Assert.False(d.Rearmar);
    }

    [Fact]
    public void Acima_DisparaExatamenteNoLimiar()
    {
        var d = AvaliadorAlertaPreco.Avaliar(DirecaoAlertaPreco.Acima, 30m, 30m, false);
        Assert.True(d.Disparar);
    }

    [Fact]
    public void Acima_NaoDisparaAbaixoDoLimiar()
    {
        var d = AvaliadorAlertaPreco.Avaliar(DirecaoAlertaPreco.Acima, 30m, 29.99m, false);
        Assert.False(d.Disparar);
        Assert.False(d.Rearmar); // não estava disparado → nada a re-armar
    }

    [Fact]
    public void Abaixo_DisparaQuandoPrecoCaiAteOuAbaixoDoLimiar()
    {
        var d = AvaliadorAlertaPreco.Avaliar(DirecaoAlertaPreco.Abaixo, 20m, 19.5m, false);
        Assert.True(d.Disparar);
    }

    [Fact]
    public void Abaixo_NaoDisparaAcimaDoLimiar()
    {
        var d = AvaliadorAlertaPreco.Avaliar(DirecaoAlertaPreco.Abaixo, 20m, 20.01m, false);
        Assert.False(d.Disparar);
        Assert.False(d.Rearmar);
    }

    [Fact]
    public void NaoRedispara_EnquantoJaDisparadoEAindaCruzado()
    {
        // Já disparou (armado) e o preço continua acima → não notifica de novo, nem re-arma.
        var d = AvaliadorAlertaPreco.Avaliar(DirecaoAlertaPreco.Acima, 30m, 35m, jaDisparado: true);
        Assert.False(d.Disparar);
        Assert.False(d.Rearmar);
    }

    [Fact]
    public void Rearma_QuandoPrecoVoltaParaOOutroLado()
    {
        // Estava disparado (acima) e o preço caiu abaixo do limiar → re-arma (sem notificar).
        var d = AvaliadorAlertaPreco.Avaliar(DirecaoAlertaPreco.Acima, 30m, 28m, jaDisparado: true);
        Assert.False(d.Disparar);
        Assert.True(d.Rearmar);
    }

    [Fact]
    public void Rearma_DirecaoAbaixo_QuandoPrecoSobeAcimaDoLimiar()
    {
        var d = AvaliadorAlertaPreco.Avaliar(DirecaoAlertaPreco.Abaixo, 20m, 22m, jaDisparado: true);
        Assert.False(d.Disparar);
        Assert.True(d.Rearmar);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void PrecoInvalido_NaoDisparaNemRearma(decimal preco)
    {
        var d = AvaliadorAlertaPreco.Avaliar(DirecaoAlertaPreco.Acima, 30m, preco, jaDisparado: true);
        Assert.False(d.Disparar);
        Assert.False(d.Rearmar);
    }

    [Fact]
    public void Sobrecarga_DaEntidade_UsaEstadoDispararadoEm()
    {
        var armado = new AlertaPreco { Direcao = DirecaoAlertaPreco.Acima, Limiar = 30m, DispararadoEm = DateTime.UtcNow };
        var naoArmado = new AlertaPreco { Direcao = DirecaoAlertaPreco.Acima, Limiar = 30m, DispararadoEm = null };

        // Mesmo preço (cruzado): o não-armado dispara, o armado não.
        Assert.True(AvaliadorAlertaPreco.Avaliar(naoArmado, 31m).Disparar);
        Assert.False(AvaliadorAlertaPreco.Avaliar(armado, 31m).Disparar);

        // Preço volta para baixo: o armado re-arma; o não-armado nada faz.
        Assert.True(AvaliadorAlertaPreco.Avaliar(armado, 25m).Rearmar);
        Assert.False(AvaliadorAlertaPreco.Avaliar(naoArmado, 25m).Rearmar);
    }
}
