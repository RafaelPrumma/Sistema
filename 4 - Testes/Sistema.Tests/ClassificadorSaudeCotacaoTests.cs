using Sistema.APP.Services;
using Sistema.CORE.Entities;

namespace Sistema.Tests;

// F-S — lógica pura da classificação da saúde da cotação de um ativo detido.
// Cobre cada status amigável (Atual / Vencida / Falhou / Sem token / Fallback custo / B3 Custódia /
// Não suportada), a severidade resultante (ok/atencao/critico) e o agrupamento visual.
public class ClassificadorSaudeCotacaoTests
{
    [Fact]
    public void PrecoVivoValido_EhAtualEOk()
    {
        var r = ClassificadorSaudeCotacao.Classificar(
            temPrecoUtilizavel: true, ProvedorCotacao.Brapi, StatusCotacao.Atual, vencida: false);
        Assert.Equal("Atual", r.Status);
        Assert.Equal(ClassificadorSaudeCotacao.Severidade.Ok, r.Nivel);
    }

    [Fact]
    public void PrecoVivoBinance_EhAtual()
    {
        var r = ClassificadorSaudeCotacao.Classificar(true, ProvedorCotacao.Binance, StatusCotacao.Atual, false);
        Assert.Equal("Atual", r.Status);
        Assert.Equal(ClassificadorSaudeCotacao.Severidade.Ok, r.Nivel);
    }

    [Fact]
    public void FechamentoB3_EhB3CustodiaEAtencao()
    {
        // Preço utilizável vindo do fechamento da custódia B3 (sem token Brapi) — bom, mas não ao vivo.
        var r = ClassificadorSaudeCotacao.Classificar(true, ProvedorCotacao.B3Custodia, StatusCotacao.Atual, false);
        Assert.Equal("B3 Custódia", r.Status);
        Assert.Equal(ClassificadorSaudeCotacao.Severidade.Atencao, r.Nivel);
    }

    [Fact]
    public void PrecoUtilizavelMasVencido_EhVencida()
    {
        var r = ClassificadorSaudeCotacao.Classificar(true, ProvedorCotacao.Brapi, StatusCotacao.Atual, vencida: true);
        Assert.Equal("Vencida", r.Status);
        Assert.Equal(ClassificadorSaudeCotacao.Severidade.Atencao, r.Nivel);
    }

    [Fact]
    public void CotacaoMarcadaDesatualizada_EhVencida()
    {
        var r = ClassificadorSaudeCotacao.Classificar(true, ProvedorCotacao.Brapi, StatusCotacao.Desatualizada, vencida: false);
        Assert.Equal("Vencida", r.Status);
        Assert.Equal(ClassificadorSaudeCotacao.Severidade.Atencao, r.Nivel);
    }

    [Fact]
    public void B3CustodiaTemPrecedenciaSobreVencida()
    {
        // B3Custódia é classificada como tal mesmo que a flag vencida esteja ligada (preço de fechamento).
        var r = ClassificadorSaudeCotacao.Classificar(true, ProvedorCotacao.B3Custodia, StatusCotacao.Atual, vencida: true);
        Assert.Equal("B3 Custódia", r.Status);
    }

    [Fact]
    public void SemPrecoComSemToken_EhSemTokenEAtencao()
    {
        var r = ClassificadorSaudeCotacao.Classificar(false, ProvedorCotacao.Brapi, StatusCotacao.SemToken, false);
        Assert.Equal("Sem token", r.Status);
        Assert.Equal(ClassificadorSaudeCotacao.Severidade.Atencao, r.Nivel);
    }

    [Fact]
    public void SemPrecoComFalha_EhFalhouECritico()
    {
        var r = ClassificadorSaudeCotacao.Classificar(false, ProvedorCotacao.Brapi, StatusCotacao.Falhou, false);
        Assert.Equal("Falhou", r.Status);
        Assert.Equal(ClassificadorSaudeCotacao.Severidade.Critico, r.Nivel);
    }

    [Fact]
    public void SemPrecoNaoSuportada_EhNaoSuportadaECritico()
    {
        var r = ClassificadorSaudeCotacao.Classificar(false, ProvedorCotacao.Binance, StatusCotacao.NaoSuportada, false);
        Assert.Equal("Não suportada", r.Status);
        Assert.Equal(ClassificadorSaudeCotacao.Severidade.Critico, r.Nivel);
    }

    [Fact]
    public void SemPrecoSemMotivoConhecido_CaiNoFallbackCusto()
    {
        // Sem cotação utilizável e status "Atual" (nunca tentou cotar de verdade) → valora pelo custo.
        var r = ClassificadorSaudeCotacao.Classificar(false, ProvedorCotacao.Manual, StatusCotacao.Atual, false);
        Assert.Equal("Fallback custo", r.Status);
        Assert.Equal(ClassificadorSaudeCotacao.Severidade.Critico, r.Nivel);
    }

    [Theory]
    [InlineData(ClassificadorSaudeCotacao.Severidade.Ok, "ok")]
    [InlineData(ClassificadorSaudeCotacao.Severidade.Atencao, "atencao")]
    [InlineData(ClassificadorSaudeCotacao.Severidade.Critico, "critico")]
    public void RotuloSeveridade_MapeiaParaClasseDoBadge(ClassificadorSaudeCotacao.Severidade nivel, string esperado)
        => Assert.Equal(esperado, ClassificadorSaudeCotacao.RotuloSeveridade(nivel));

    [Fact]
    public void Grupo_CriptoSemprePrevalece()
    {
        Assert.Equal("Cripto", ClassificadorSaudeCotacao.Grupo(ehCripto: true, temPrecoUtilizavel: true, ProvedorCotacao.Binance));
        Assert.Equal("Cripto", ClassificadorSaudeCotacao.Grupo(ehCripto: true, temPrecoUtilizavel: false, ProvedorCotacao.Brapi));
    }

    [Fact]
    public void Grupo_AtivoCotadoPelaCustodiaB3_VaiParaGrupoProprio()
    {
        var g = ClassificadorSaudeCotacao.Grupo(ehCripto: false, temPrecoUtilizavel: true, ProvedorCotacao.B3Custodia);
        Assert.Equal("B3 Custódia", g);
    }

    [Fact]
    public void Grupo_AcaoComBrapiOuSemPreco_VaiParaB3()
    {
        Assert.Equal("B3", ClassificadorSaudeCotacao.Grupo(ehCripto: false, temPrecoUtilizavel: true, ProvedorCotacao.Brapi));
        // Sem preço (caiu no custo) o provedor B3Custódia não vale para o agrupamento → B3.
        Assert.Equal("B3", ClassificadorSaudeCotacao.Grupo(ehCripto: false, temPrecoUtilizavel: false, ProvedorCotacao.B3Custodia));
    }
}
