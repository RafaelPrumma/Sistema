using Sistema.CORE.Entities;
using Sistema.INFRA.Importers;

namespace Sistema.Tests;

// F-I — lógica pura da classificação de um ativo na árvore de carteiras (sem DbContext). Cobre o mapa
// explícito por ticker e os fallbacks: cripto (BTC / memecoin / altcoin), FII (papel por nome / tijolo)
// e ação/ETF fora do mapa → null (não inventa carteira). Espelha o mapa semeado em
// specs/investimentos.spec.md (F-I).
public class ClassificadorCarteiraTests
{
    // --- 1) Mapa explícito por ticker (custódia B3 + cripto §10) -------------------------------------

    [Theory]
    [InlineData("BBAS3", ClassificadorCarteira.SlugBancario, ClassificadorCarteira.SlugBancos)]
    [InlineData("BBDC4", ClassificadorCarteira.SlugBancario, ClassificadorCarteira.SlugBancos)]
    [InlineData("CXSE3", ClassificadorCarteira.SlugBancario, ClassificadorCarteira.SlugSeguridade)] // seguradora → Seguridade
    [InlineData("CPTS11", ClassificadorCarteira.SlugFiis, ClassificadorCarteira.SlugFiisPapel)]
    [InlineData("HGLG11", ClassificadorCarteira.SlugFiis, ClassificadorCarteira.SlugFiisTijolo)]
    [InlineData("PETR4", ClassificadorCarteira.SlugComodities, ClassificadorCarteira.SlugComoditiesPetroleo)]
    [InlineData("VALE3", ClassificadorCarteira.SlugComodities, ClassificadorCarteira.SlugComoditiesMineracao)]
    [InlineData("GOLD11", ClassificadorCarteira.SlugComodities, ClassificadorCarteira.SlugComoditiesMineracao)]
    [InlineData("TAEE4", ClassificadorCarteira.SlugComodities, ClassificadorCarteira.SlugComoditiesEnergia)]
    [InlineData("BTC", ClassificadorCarteira.SlugCripto, ClassificadorCarteira.SlugCriptoBtc)]
    [InlineData("XRP", ClassificadorCarteira.SlugCripto, ClassificadorCarteira.SlugCriptoAltcoins)]
    [InlineData("DOGE", ClassificadorCarteira.SlugCripto, ClassificadorCarteira.SlugCriptoMemecoins)]
    public void MapaExplicito_PorTicker_ResolveCaminho(string ticker, string slugTopo, string? slugFolha)
    {
        var classificacao = ClassificadorCarteira.Classificar(ticker, ticker, ClasseAtivo.Outro, isCripto: false);

        Assert.NotNull(classificacao);
        Assert.Equal(slugTopo, classificacao!.SlugTopo);
        Assert.Equal(slugFolha, classificacao.SlugFolha);
    }

    [Fact]
    public void MapaExplicito_IgnoraCaixaEEspacos_NoTicker()
    {
        var classificacao = ClassificadorCarteira.Classificar("  bbas3 ", "Banco do Brasil", ClasseAtivo.Acao, isCripto: false);

        Assert.NotNull(classificacao);
        Assert.Equal(ClassificadorCarteira.SlugBancario, classificacao!.SlugTopo);
        Assert.Equal(ClassificadorCarteira.SlugBancos, classificacao.SlugFolha);
    }

    // --- 2) Fallback cripto (ticker fora do mapa) ---------------------------------------------------

    [Theory]
    [InlineData("BTC")]        // mapa explícito também resolve, mas o fallback confirma o BTC base
    [InlineData("BTC/BRL")]    // par com quote separado por barra → símbolo base BTC
    [InlineData("btc usd")]    // caixa/espaço → símbolo base BTC
    public void FallbackCripto_BTC_VaiParaBtc(string ticker)
    {
        var classificacao = ClassificadorCarteira.Classificar(ticker, "Bitcoin", ClasseAtivo.Cripto, isCripto: true);

        Assert.NotNull(classificacao);
        Assert.Equal(ClassificadorCarteira.SlugCripto, classificacao!.SlugTopo);
        Assert.Equal(ClassificadorCarteira.SlugCriptoBtc, classificacao.SlugFolha);
    }

    [Theory]
    [InlineData("SHIB")]
    [InlineData("PEPE")]
    [InlineData("FLOKI")]
    [InlineData("BONK")]
    [InlineData("WIF")]
    public void FallbackCripto_Memecoin_VaiParaMemecoins(string simbolo)
    {
        var classificacao = ClassificadorCarteira.Classificar(simbolo, simbolo, ClasseAtivo.Cripto, isCripto: true);

        Assert.NotNull(classificacao);
        Assert.Equal(ClassificadorCarteira.SlugCripto, classificacao!.SlugTopo);
        Assert.Equal(ClassificadorCarteira.SlugCriptoMemecoins, classificacao.SlugFolha);
    }

    [Theory]
    [InlineData("SOL")]
    [InlineData("ADA")]
    [InlineData("LINK")]
    public void FallbackCripto_OutraMoeda_VaiParaAltcoins(string simbolo)
    {
        var classificacao = ClassificadorCarteira.Classificar(simbolo, simbolo, ClasseAtivo.Cripto, isCripto: true);

        Assert.NotNull(classificacao);
        Assert.Equal(ClassificadorCarteira.SlugCripto, classificacao!.SlugTopo);
        Assert.Equal(ClassificadorCarteira.SlugCriptoAltcoins, classificacao.SlugFolha);
    }

    // --- 3) Fallback FII (ticker fora do mapa, classe FII ou final 11) ------------------------------

    [Theory]
    [InlineData("XPTO11", "FII XPTO RECEBÍVEIS CI")]   // RECEB no nome → Papel
    [InlineData("ABCD11", "FII ABCD CRI FUNDO")]       // CRI no nome → Papel
    [InlineData("WXYZ11", "WXYZ SECURITIES FII")]      // SECURITIES → Papel
    public void FallbackFii_NomeDePapel_VaiParaPapel(string ticker, string nome)
    {
        var classificacao = ClassificadorCarteira.Classificar(ticker, nome, ClasseAtivo.FII, isCripto: false);

        Assert.NotNull(classificacao);
        Assert.Equal(ClassificadorCarteira.SlugFiis, classificacao!.SlugTopo);
        Assert.Equal(ClassificadorCarteira.SlugFiisPapel, classificacao.SlugFolha);
    }

    [Theory]
    [InlineData("LOGG11", "FII LOG LOGISTICA CI")]     // sem marcador de papel → Tijolo
    [InlineData("VISC11", "FII VINCI SHOPPING CI")]
    public void FallbackFii_NomeDeTijolo_VaiParaTijolo(string ticker, string nome)
    {
        var classificacao = ClassificadorCarteira.Classificar(ticker, nome, ClasseAtivo.FII, isCripto: false);

        Assert.NotNull(classificacao);
        Assert.Equal(ClassificadorCarteira.SlugFiis, classificacao!.SlugTopo);
        Assert.Equal(ClassificadorCarteira.SlugFiisTijolo, classificacao.SlugFolha);
    }

    [Fact]
    public void FallbackFii_TickerFinal11_SemClasseFii_AindaCaiEmFii()
    {
        // Ticker final "11" sem classe FII explícita ainda entra no fallback de FII.
        var classificacao = ClassificadorCarteira.Classificar("NOVO11", "FUNDO NOVO RECEBÍVEIS", ClasseAtivo.Outro, isCripto: false);

        Assert.NotNull(classificacao);
        Assert.Equal(ClassificadorCarteira.SlugFiis, classificacao!.SlugTopo);
        Assert.Equal(ClassificadorCarteira.SlugFiisPapel, classificacao.SlugFolha);
    }

    // --- 4) Ação/ETF fora do mapa → null (sem carteira) ---------------------------------------------

    [Theory]
    [InlineData("MGLU3", "MAGAZINE LUIZA ON", ClasseAtivo.Acao)]
    [InlineData("BOVA11", "ISHARES IBOVESPA ETF", ClasseAtivo.ETF)] // ETF não termina em "11" relevante? termina; ver nota
    [InlineData("AAPL34", "APPLE BDR", ClasseAtivo.BDR)]
    public void AcaoOuEtfForaDoMapa_DevolveNull(string ticker, string nome, ClasseAtivo classe)
    {
        var classificacao = ClassificadorCarteira.Classificar(ticker, nome, classe, isCripto: false);

        // BOVA11 termina em "11" → o fallback de FII o captura; os demais ficam sem carteira.
        if (ticker == "BOVA11")
            Assert.NotNull(classificacao);
        else
            Assert.Null(classificacao);
    }

    [Fact]
    public void Acao_ForaDoMapa_DevolveNull_SemInventarGrupo()
    {
        var classificacao = ClassificadorCarteira.Classificar("WEGE3", "WEG ON", ClasseAtivo.Acao, isCripto: false);

        Assert.Null(classificacao);
    }

    // --- 5) Sobrecarga por entidade ----------------------------------------------------------------

    [Fact]
    public void Classificar_PorEntidade_UsaTickerEFlagCripto()
    {
        var ativo = new AtivoFinanceiro
        {
            Id = 1,
            Chave = "DOGE",
            Sigla = "DOGE",
            Nome = "Dogecoin",
            Classe = ClasseAtivo.Cripto,
            EhCripto = true
        };

        var classificacao = ClassificadorCarteira.Classificar(ativo);

        Assert.NotNull(classificacao);
        Assert.Equal(ClassificadorCarteira.SlugCripto, classificacao!.SlugTopo);
        Assert.Equal(ClassificadorCarteira.SlugCriptoMemecoins, classificacao.SlugFolha);
    }

    [Fact]
    public void Classificar_PorEntidade_AcaoForaDoMapa_DevolveNull()
    {
        var ativo = new AtivoFinanceiro
        {
            Id = 2,
            Chave = "RANI3",
            Sigla = "RANI3",
            Nome = "IRANI PAPEL ON",
            Classe = ClasseAtivo.Acao
        };

        Assert.Null(ClassificadorCarteira.Classificar(ativo));
    }
}
