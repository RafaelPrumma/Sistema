using System.Globalization;
using Sistema.APP.Services;
using Sistema.CORE.Entities;

namespace Sistema.Tests;

/// <summary>
/// Testa o motor puro de apuração de IR (CalculadoraIr): ganho de capital mensal por natureza
/// (isenção ações R$20k, FII 20%, cripto R$35k), compensação de prejuízo, Bens e Direitos e proventos.
/// </summary>
public class CalculadoraIrTests
{
    private static AtivoFinanceiro Ativo(int id, string ticker, ClasseAtivo classe, bool cripto = false)
        => new() { Id = id, Chave = ticker, Sigla = ticker, Nome = ticker, Classe = classe, EhCripto = cripto };

    private static TransacaoFinanceira Tx(AtivoFinanceiro a, string data, TipoOperacaoFinanceira tipo, decimal qtd, decimal preco)
        => new()
        {
            AssetId = a.Id,
            Asset = a,
            Date = DateTime.Parse(data, CultureInfo.InvariantCulture),
            OperationType = tipo,
            Quantity = qtd,
            UnitPrice = preco,
            GrossAmount = qtd * preco
        };

    [Fact]
    public void Acoes_VendasAteVinteMilNoMes_GanhoIsento()
    {
        var petr = Ativo(1, "PETR4", ClasseAtivo.Acao);
        var txs = new[]
        {
            Tx(petr, "2025-01-05", TipoOperacaoFinanceira.Compra, 100, 30m), // custo 3000
            Tx(petr, "2025-02-10", TipoOperacaoFinanceira.Venda, 100, 35m),  // vendas 3500 (<=20k), lucro 500
        };

        var r = CalculadoraIr.Apurar(2025, txs, []);
        var fev = Assert.Single(r.GanhosMensais);
        Assert.True(fev.Isento);
        Assert.Equal(0m, fev.Imposto);
        Assert.Equal(0m, r.TotalImpostoDevido);
    }

    [Fact]
    public void Acoes_VendasAcimaDeVinteMil_Tributa15PorCento()
    {
        var petr = Ativo(1, "PETR4", ClasseAtivo.Acao);
        var txs = new[]
        {
            Tx(petr, "2025-01-05", TipoOperacaoFinanceira.Compra, 1000, 30m), // custo 30000
            Tx(petr, "2025-02-10", TipoOperacaoFinanceira.Venda, 1000, 35m),  // vendas 35000 (>20k), lucro 5000
        };

        var fev = Assert.Single(CalculadoraIr.Apurar(2025, txs, []).GanhosMensais);
        Assert.False(fev.Isento);
        Assert.Equal(0.15m, fev.Aliquota);
        Assert.Equal(750m, fev.Imposto); // 15% de 5000
    }

    [Fact]
    public void Fii_NaoTemIsencao_Tributa20PorCento()
    {
        var fii = Ativo(2, "HGLG11", ClasseAtivo.FII);
        var txs = new[]
        {
            Tx(fii, "2025-01-05", TipoOperacaoFinanceira.Compra, 100, 100m), // custo 10000
            Tx(fii, "2025-03-10", TipoOperacaoFinanceira.Venda, 100, 120m),  // vendas 12000 (<20k mas FII não isenta), lucro 2000
        };

        var mar = Assert.Single(CalculadoraIr.Apurar(2025, txs, []).GanhosMensais);
        Assert.False(mar.Isento);
        Assert.Equal(0.20m, mar.Aliquota);
        Assert.Equal(400m, mar.Imposto); // 20% de 2000
    }

    [Fact]
    public void Cripto_Exterior_TributaGanhoAnual15pct_SemIsencaoNacional()
    {
        // Cripto (Binance) = aplicação no EXTERIOR (Lei 14.754/2023): NÃO entra no ganho de capital
        // mensal nacional nem usa a isenção de R$35k/mês; é 15% sobre o ganho líquido ANUAL.
        var btc = Ativo(3, "BTC", ClasseAtivo.Cripto, cripto: true);

        // Alienação de R$20.000 — seria isenta no modelo nacional (<=35k), mas no exterior tributa.
        var pequena = new[]
        {
            Tx(btc, "2025-01-05", TipoOperacaoFinanceira.Compra, 1m, 100000m),
            Tx(btc, "2025-02-10", TipoOperacaoFinanceira.Venda, 0.05m, 400000m), // aliena 20000; custo 5000; ganho 15000
        };
        var apurP = CalculadoraIr.Apurar(2025, pequena, []);
        Assert.Empty(apurP.GanhosMensais);                              // cripto não entra no ganho mensal nacional
        Assert.Equal(15000m, apurP.CriptoExterior.GanhoCapitalLiquido);
        Assert.Equal(0.15m, apurP.CriptoExterior.Aliquota);
        Assert.Equal(2250m, apurP.CriptoExterior.ImpostoGanhoCapital);  // 15% de 15000 — sem isenção

        // Lucro 30000 → 15% = 4500; uma linha de alienação valorada em BRL.
        var maior = new[]
        {
            Tx(btc, "2025-01-05", TipoOperacaoFinanceira.Compra, 1m, 100000m),  // PM 100000
            Tx(btc, "2025-02-10", TipoOperacaoFinanceira.Venda, 0.1m, 400000m), // aliena 40000; custo 10000; ganho 30000
        };
        var apur = CalculadoraIr.Apurar(2025, maior, []);
        var alienacao = Assert.Single(apur.CriptoExterior.Alienacoes);
        Assert.Equal(40000m, alienacao.ValorAlienacao);
        Assert.Equal(10000m, alienacao.Custo);
        Assert.Equal(30000m, alienacao.Ganho);
        Assert.Equal(4500m, apur.CriptoExterior.ImpostoGanhoCapital);   // 15% de 30000
    }

    [Fact]
    public void Prejuizo_DeUmMesCompensaGanhoDeMesPosterior()
    {
        var fii = Ativo(2, "KNCR11", ClasseAtivo.FII);
        var txs = new[]
        {
            Tx(fii, "2025-01-05", TipoOperacaoFinanceira.Compra, 100, 100m), // PM 100
            Tx(fii, "2025-02-10", TipoOperacaoFinanceira.Venda, 50, 80m),    // lucro 4000-5000 = -1000 (prejuízo)
            Tx(fii, "2025-03-10", TipoOperacaoFinanceira.Venda, 50, 160m),   // lucro 8000-5000 = 3000
        };

        var ganhos = CalculadoraIr.Apurar(2025, txs, []).GanhosMensais;
        var mar = ganhos.Single(g => g.Mes == 3);
        Assert.Equal(1000m, mar.PrejuizoCompensado);
        Assert.Equal(2000m, mar.BaseCalculo);
        Assert.Equal(400m, mar.Imposto); // 20% de 2000
    }

    [Fact]
    public void BensEDireitos_TrazPosicaoEm3112AoCusto_IgnoraZerados()
    {
        var petr = Ativo(1, "PETR4", ClasseAtivo.Acao);
        var vale = Ativo(4, "VALE3", ClasseAtivo.Acao);
        var txs = new[]
        {
            Tx(petr, "2025-06-01", TipoOperacaoFinanceira.Compra, 100, 30m), // mantido: 100 @ custo 3000
            Tx(vale, "2025-06-01", TipoOperacaoFinanceira.Compra, 50, 60m),
            Tx(vale, "2025-07-01", TipoOperacaoFinanceira.Venda, 50, 70m),   // zerado → fora do B&D
        };

        var bd = CalculadoraIr.Apurar(2025, txs, []).BensEDireitos;
        var item = Assert.Single(bd);
        Assert.Equal("PETR4", item.Ticker);
        Assert.Equal(100m, item.Quantidade);
        Assert.Equal(3000m, item.Custo);
    }

    [Fact]
    public void Proventos_SeparaIsentosDeJcp()
    {
        var proventos = new[]
        {
            new RendimentoInvestimento { IncomeType = "JCP", Amount = 100m, PaymentDate = new DateTime(2025, 5, 1) },
            new RendimentoInvestimento { IncomeType = "Dividendo", Amount = 50m, PaymentDate = new DateTime(2025, 6, 1) },
            new RendimentoInvestimento { IncomeType = "Rendimento", Amount = 200m, PaymentDate = new DateTime(2025, 7, 1) },
            new RendimentoInvestimento { IncomeType = "Dividendo", Amount = 9m, PaymentDate = new DateTime(2024, 7, 1) }, // outro ano → fora
        };

        var r = CalculadoraIr.Apurar(2025, [], proventos);
        Assert.Equal(100m, Assert.Single(r.TributacaoExclusiva).Valor);
        Assert.Equal(250m, r.RendimentosIsentos.Sum(x => x.Valor)); // 50 + 200
    }
}
