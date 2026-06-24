using Sistema.APP.Services;
using Sistema.CORE.Entities;

namespace Sistema.Tests;

public class CalculadoraPosicaoAtivoTests
{
    [Fact]
    public void CompraUnica_MaterializaQuantidadePrecoMedioECusto()
    {
        var ativo = Ativo(1, "BBAS3");

        var posicao = Assert.Single(CalculadoraPosicaoAtivo.Calcular([
            Transacao(ativo, TipoOperacaoFinanceira.Compra, 10m, 20m)
        ]));

        Assert.Equal(10m, posicao.Quantidade);
        Assert.Equal(20m, posicao.PrecoMedio);
        Assert.Equal(200m, posicao.CustoTotal);
        Assert.Equal(StatusPosicaoAtivo.Aberta, posicao.Status);
    }

    [Fact]
    public void CompraEVendaParcial_BaixaCustoPeloPrecoMedioVigente()
    {
        var ativo = Ativo(1, "BBAS3");

        var posicao = Assert.Single(CalculadoraPosicaoAtivo.Calcular([
            Transacao(ativo, TipoOperacaoFinanceira.Compra, 10m, 20m),
            Transacao(ativo, TipoOperacaoFinanceira.Compra, 10m, 30m, data: new DateTime(2026, 1, 2)),
            Transacao(ativo, TipoOperacaoFinanceira.Venda, 5m, 40m, data: new DateTime(2026, 1, 3))
        ]));

        Assert.Equal(15m, posicao.Quantidade);
        Assert.Equal(25m, posicao.PrecoMedio);
        Assert.Equal(375m, posicao.CustoTotal);
        Assert.Equal(200m, posicao.TotalVendido);
        Assert.Equal(75m, posicao.ResultadoRealizado);
    }

    [Fact]
    public void VendaTotal_ZeraQuantidadePrecoMedioECusto()
    {
        var ativo = Ativo(1, "BBAS3");

        var posicao = Assert.Single(CalculadoraPosicaoAtivo.Calcular([
            Transacao(ativo, TipoOperacaoFinanceira.Compra, 10m, 20m),
            Transacao(ativo, TipoOperacaoFinanceira.Venda, 10m, 25m, data: new DateTime(2026, 1, 2))
        ]));

        Assert.Equal(0m, posicao.Quantidade);
        Assert.Equal(0m, posicao.PrecoMedio);
        Assert.Equal(0m, posicao.CustoTotal);
        Assert.Equal(50m, posicao.ResultadoRealizado);
        Assert.Equal(StatusPosicaoAtivo.Encerrada, posicao.Status);
    }

    [Fact]
    public void Taxa_ReduzQuantidadeERealizaPeloPrecoMedio()
    {
        var ativo = Ativo(1, "BBAS3");

        var posicao = Assert.Single(CalculadoraPosicaoAtivo.Calcular([
            Transacao(ativo, TipoOperacaoFinanceira.Compra, 10m, 20m),
            Transacao(ativo, TipoOperacaoFinanceira.Taxa, 1m, 0m, data: new DateTime(2026, 1, 2))
        ]));

        Assert.Equal(9m, posicao.Quantidade);
        Assert.Equal(20m, posicao.PrecoMedio);
        Assert.Equal(180m, posicao.CustoTotal);
        Assert.Equal(-20m, posicao.ResultadoRealizado);
    }

    [Fact]
    public void Cripto_PreservaCasasDecimais()
    {
        var ativo = Ativo(1, "BTC", ClasseAtivo.Cripto, true);

        var posicao = Assert.Single(CalculadoraPosicaoAtivo.Calcular([
            Transacao(ativo, TipoOperacaoFinanceira.Compra, 0.123456789123m, 100000m)
        ]));

        Assert.Equal(0.123456789123m, posicao.Quantidade);
        Assert.Equal(100000m, posicao.PrecoMedio);
    }

    [Fact]
    public void EventoCorporativo_UsaTransacoesJaAjustadas()
    {
        var ativo = Ativo(1, "BCFF11", ClasseAtivo.FII);

        var posicao = Assert.Single(CalculadoraPosicaoAtivo.Calcular([
            Transacao(ativo, TipoOperacaoFinanceira.Compra, 800m, 10m),
            Transacao(ativo, TipoOperacaoFinanceira.Venda, 200m, 10m, data: new DateTime(2026, 1, 2))
        ]));

        Assert.Equal(600m, posicao.Quantidade);
        Assert.Equal(10m, posicao.PrecoMedio);
        Assert.Equal(6000m, posicao.CustoTotal);
    }

    private static AtivoFinanceiro Ativo(int id, string ticker, ClasseAtivo classe = ClasseAtivo.Acao, bool cripto = false)
        => new()
        {
            Id = id,
            Chave = ticker,
            Sigla = ticker,
            Nome = ticker,
            Classe = classe,
            Mercado = cripto ? "Binance" : "B3",
            Moeda = cripto ? "USD/BRL" : "BRL",
            EhCripto = cripto,
            Ativo = true
        };

    private static TransacaoFinanceira Transacao(
        AtivoFinanceiro ativo,
        TipoOperacaoFinanceira tipo,
        decimal quantidade,
        decimal preco,
        decimal taxas = 0m,
        DateTime? data = null)
        => new()
        {
            AssetId = ativo.Id,
            Asset = ativo,
            Date = data ?? new DateTime(2026, 1, 1),
            OperationType = tipo,
            Quantity = quantidade,
            UnitPrice = preco,
            Fees = taxas,
            GrossAmount = quantidade * preco,
            IsCanonical = true,
            RawJson = "{}"
        };
}
