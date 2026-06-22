using Sistema.CORE.Entities;
using Sistema.INFRA.Importers;

namespace Sistema.Tests;

// F2 — lógica pura da materialização do extrato consolidado da B3 (sem DbContext):
// regra de precedência vs notas e o mapeamento das linhas de Negociações/Proventos.
public class ExtratoB3MaterializadorTests
{
    // Mercado fracionário (sufixo "F") = mesmo ativo do lote-padrão → unifica no ticker base.
    [Theory]
    [InlineData("ITUB4F", "ITUB4")]
    [InlineData("PETR4F", "PETR4")]
    [InlineData("GOLD11F", "GOLD11")]
    [InlineData("GOGL34F", "GOGL34")]
    [InlineData("ITUB4", "ITUB4")]   // base não muda
    [InlineData("HGLG11", "HGLG11")] // FII base não muda
    [InlineData("itub4f", "ITUB4")]  // normaliza caixa
    public void NormalizarTicker_RemoveSufixoFracionario(string entrada, string esperado)
        => Assert.Equal(esperado, ExtratoB3Materializador.NormalizarTicker(entrada));

    [Fact]
    public void PrecedenciaInvertida_B3PresenteNoTickerMes_NotaNaoMaterializa()
    {
        // B3 é a fonte de verdade: BBAS3 (assetId 1) em 2022-09 tem Negociação da B3 →
        // a NOTA daquele ticker×mês é pulada (a B3 manda).
        var cobertosPorB3 = new HashSet<(int AssetId, int Ano, int Mes)> { (1, 2022, 9) };

        Assert.False(ExtratoB3Materializador.DeveMaterializarNotaB3(1, 2022, 9, cobertosPorB3));
    }

    [Fact]
    public void PrecedenciaInvertida_B3AusenteNoTickerMes_NotaMaterializa()
    {
        // A B3 cobre BBAS3/2022-09; onde a B3 NÃO cobre, a nota Nubank complementa:
        // outro ticker no mesmo mês (PETR4/2022-09, assetId 2) e o MESMO ticker em outro mês
        // (BBAS3/2022-10) → a nota materializa.
        var cobertosPorB3 = new HashSet<(int AssetId, int Ano, int Mes)> { (1, 2022, 9) };

        Assert.True(ExtratoB3Materializador.DeveMaterializarNotaB3(2, 2022, 9, cobertosPorB3));
        Assert.True(ExtratoB3Materializador.DeveMaterializarNotaB3(1, 2022, 10, cobertosPorB3));
    }

    [Fact]
    public void InterpretarNegociacao_LinhaSoCompra_GeraUmaCompra()
    {
        // Linha real de 2022-setembro: AFHI11 compra 2 a 96.9 (decimal com ".").
        var row = LinhaNegociacao(
            "AFHI11", "29/09/2022", "-", "NU INVEST CORRETORA DE VALORES S.A.",
            qtdCompra: "2", qtdVenda: "0", qtdLiquida: "2", pmCompra: "96.9", pmVenda: "0");

        var movimentos = ExtratoB3Materializador.InterpretarNegociacao(row);

        var mov = Assert.Single(movimentos);
        Assert.Equal("AFHI11", mov.Ticker);
        Assert.Equal(TipoOperacaoFinanceira.Compra, mov.OperationType);
        Assert.Equal(2m, mov.Quantity);
        Assert.Equal(96.9m, mov.UnitPrice);
        Assert.Equal(new DateTime(2022, 9, 29), mov.PeriodoInicial);
        Assert.Null(mov.PeriodoFinal); // "-" vira null
    }

    [Fact]
    public void InterpretarNegociacao_LinhaSoVenda_GeraUmaVenda()
    {
        // Linha real: AMER3F vende 8 a 17.28.
        var row = LinhaNegociacao(
            "AMER3F", "20/09/2022", "-", "NU INVEST CORRETORA DE VALORES S.A.",
            qtdCompra: "0", qtdVenda: "8", qtdLiquida: "-8", pmCompra: "0", pmVenda: "17.28");

        var movimentos = ExtratoB3Materializador.InterpretarNegociacao(row);

        var mov = Assert.Single(movimentos);
        Assert.Equal(TipoOperacaoFinanceira.Venda, mov.OperationType);
        Assert.Equal(8m, mov.Quantity);
        Assert.Equal(17.28m, mov.UnitPrice);
    }

    [Fact]
    public void InterpretarNegociacao_LinhaComCompraEVenda_GeraDoisMovimentos()
    {
        var row = LinhaNegociacao(
            "PETR4", "01/09/2022", "30/09/2022", "NU INVEST",
            qtdCompra: "100", qtdVenda: "40", qtdLiquida: "60", pmCompra: "30.5", pmVenda: "33.2");

        var movimentos = ExtratoB3Materializador.InterpretarNegociacao(row);

        Assert.Equal(2, movimentos.Count);
        var compra = Assert.Single(movimentos, m => m.OperationType == TipoOperacaoFinanceira.Compra);
        var venda = Assert.Single(movimentos, m => m.OperationType == TipoOperacaoFinanceira.Venda);
        Assert.Equal(100m, compra.Quantity);
        Assert.Equal(30.5m, compra.UnitPrice);
        Assert.Equal(40m, venda.Quantity);
        Assert.Equal(33.2m, venda.UnitPrice);
        Assert.Equal(new DateTime(2022, 9, 30), compra.PeriodoFinal);
    }

    [Fact]
    public void InterpretarProvento_LinhaFii_ExtraiTickerTipoValor()
    {
        // Linha real de FII (rendimento que não vem em informe de IR): AFHI11.
        var row = LinhaProvento(
            "AFHI11 - AF INVEST CRI FDO. INV. IMOB - RECEBÍVEIS IMOB.",
            "22/09/2022", "Rendimento", "NU INVEST", quantidade: "10", precoUnitario: "1.15", valorLiquido: "11.5");

        var provento = ExtratoB3Materializador.InterpretarProvento(row);

        Assert.NotNull(provento);
        Assert.Equal("AFHI11", provento!.Ticker); // prefixo antes do PRIMEIRO " - "
        Assert.Equal("Rendimento", provento.Tipo);
        Assert.Equal(11.5m, provento.Valor);
        Assert.Equal(10m, provento.Quantidade);
        Assert.Equal(1.15m, provento.ValorPorAcao);
        Assert.Equal(new DateTime(2022, 9, 22), provento.Pagamento);
    }

    [Fact]
    public void InterpretarProvento_LinhaAcao_MapeiaJcp()
    {
        // "Juros Sobre Capital Próprio" → JCP.
        var row = LinhaProvento(
            "BBAS3 - BANCO DO BRASIL S/A",
            "30/09/2022", "Juros Sobre Capital Próprio", "NU INVEST", quantidade: "102", precoUnitario: "0.27", valorLiquido: "23.73");

        var provento = ExtratoB3Materializador.InterpretarProvento(row);

        Assert.NotNull(provento);
        Assert.Equal("BBAS3", provento!.Ticker);
        Assert.Equal("JCP", provento.Tipo);
        Assert.Equal(23.73m, provento.Valor);
    }

    [Theory]
    [InlineData("Dividendo", "Dividendo")]
    [InlineData("Rendimento", "Rendimento")]
    [InlineData("Juros Sobre Capital Próprio", "JCP")]
    public void MapTipoProvento_ReconheceOsTresTipos(string evento, string esperado)
        => Assert.Equal(esperado, ExtratoB3Materializador.MapTipoProvento(evento));

    [Fact]
    public void ChaveNegociacao_IncluiTickerAnoMesSentidoCorretora()
    {
        var chave = ExtratoB3Materializador.ChaveNegociacao("PETR4", 202209, TipoOperacaoFinanceira.Compra, "NU Invest");

        Assert.Equal("B3 Extrato|PETR4|202209|1|NU INVEST", chave);
    }

    [Fact]
    public void ClassePorTicker_TickerComFinal11EhFii()
    {
        Assert.Equal(ClasseAtivo.FII, ExtratoB3Materializador.ClassePorTicker("AFHI11"));
        Assert.Equal(ClasseAtivo.Acao, ExtratoB3Materializador.ClassePorTicker("BBAS3"));
    }

    private static Dictionary<string, string> LinhaNegociacao(
        string ticker, string periodoInicial, string periodoFinal, string instituicao,
        string qtdCompra, string qtdVenda, string qtdLiquida, string pmCompra, string pmVenda)
    {
        var headers = new[]
        {
            "Código de Negociação", "Período (Inicial)", "Período (Final)", "Instituição",
            "Quantidade (Compra)", "Quantidade (Venda)", "Quantidade (Líquida)",
            "Preço Médio (Compra)", "Preço Médio (Venda)"
        };
        var cells = new[] { ticker, periodoInicial, periodoFinal, instituicao, qtdCompra, qtdVenda, qtdLiquida, pmCompra, pmVenda };
        return ExtratoB3Materializador.MapearLinha(headers, cells);
    }

    private static Dictionary<string, string> LinhaProvento(
        string produto, string pagamento, string tipoEvento, string instituicao,
        string quantidade, string precoUnitario, string valorLiquido)
    {
        var headers = new[] { "Produto", "Pagamento", "Tipo de Evento", "Instituição", "Quantidade", "Preço unitário", "Valor líquido" };
        var cells = new[] { produto, pagamento, tipoEvento, instituicao, quantidade, precoUnitario, valorLiquido };
        return ExtratoB3Materializador.MapearLinha(headers, cells);
    }
}
