using Sistema.CORE.Entities;
using Sistema.INFRA.Importers;

namespace Sistema.Tests;

// F1 — lógica pura do netting cripto (sem DbContext): classificação das operações do ledger da Binance
// e a regra de netting (permuta abate origem; BRL/fiat fora; earn entra como rendimento; transferência
// interna ignorada). Espelha o cenário real do "Histórico de Transações".
public class CriptoNettingTests
{
    private static readonly DateTime T = new(2025, 8, 17, 7, 49, 4, DateTimeKind.Utc);

    private static MovimentoCriptoBruto Bruto(string symbol, string op, decimal change, int id = 1)
        => new(symbol, T, op, change, id);

    [Fact]
    public void Permuta_DuasPernas_AbateOrigemESomaDestino()
    {
        // SOL Staking - Purchase: SOL -13.99 (sai) + BNSOL +13.09 (entra), mesmo timestamp.
        var movimentos = CriptoNetting.Netar(new[]
        {
            Bruto("SOL", "SOL Staking - Purchase", -13.99160182m, 1),
            Bruto("BNSOL", "SOL Staking - Purchase", 13.09265767m, 2)
        });

        Assert.Equal(2, movimentos.Count);
        var saida = Assert.Single(movimentos, m => m.AssetSymbol == "SOL");
        var entrada = Assert.Single(movimentos, m => m.AssetSymbol == "BNSOL");
        Assert.Equal(TipoOperacaoFinanceira.Venda, saida.OperationType);   // abate a origem
        Assert.Equal(13.99160182m, saida.Quantity);
        Assert.Equal(TipoOperacaoFinanceira.Compra, entrada.OperationType); // soma o destino
        Assert.Equal(13.09265767m, entrada.Quantity);
        Assert.True(saida.DefinirPrecoNaPersistencia); // preço = PM corrente (realizado ≈ 0)
    }

    [Fact]
    public void Convert_PernaBRL_FicaForaDaPosicao()
    {
        // Binance Convert: USDT -55.35 (sai) + BRL +300.22 (caixa, NÃO é posição).
        var movimentos = CriptoNetting.Netar(new[]
        {
            Bruto("USDT", "Binance Convert", -55.3525m, 1),
            Bruto("BRL", "Binance Convert", 300.22720103m, 2)
        });

        var mov = Assert.Single(movimentos);
        Assert.Equal("USDT", mov.AssetSymbol);            // a stablecoin abate normalmente
        Assert.Equal(TipoOperacaoFinanceira.Venda, mov.OperationType);
        Assert.DoesNotContain(movimentos, m => m.AssetSymbol == "BRL"); // BRL fora
    }

    [Fact]
    public void BuyCryptoWithFiat_SoOAtivoEntra_BRLForaDaPosicao()
    {
        // Buy Crypto With Fiat: DOGE +22.74 (entra) + BRL -25 (caixa).
        var movimentos = CriptoNetting.Netar(new[]
        {
            Bruto("DOGE", "Buy Crypto With Fiat", 22.74000169m, 1),
            Bruto("BRL", "Buy Crypto With Fiat", -25m, 2)
        });

        var mov = Assert.Single(movimentos);
        Assert.Equal("DOGE", mov.AssetSymbol);
        Assert.Equal(TipoOperacaoFinanceira.Compra, mov.OperationType);
        Assert.Equal(22.74000169m, mov.Quantity);
    }

    [Fact]
    public void Earn_FlexibleInterest_EntraComoRendimentoCusto0()
    {
        var movimentos = CriptoNetting.Netar(new[]
        {
            Bruto("BTC", "Simple Earn Flexible Interest", 0.00000006m)
        });

        var mov = Assert.Single(movimentos);
        Assert.Equal("BTC", mov.AssetSymbol);
        Assert.Equal(TipoOperacaoFinanceira.Rendimento, mov.OperationType);
        Assert.Equal(0.00000006m, mov.Quantity);
        Assert.True(mov.PrecoZero); // earn não tem custo de compra nesta fase
    }

    [Theory]
    [InlineData("HODLer Airdrops Distribution", "SOMI")]
    [InlineData("Earn - Airdrop Distribution", "ZZZ")]
    [InlineData("BNSOL Staking - Extra Rewards", "SIGN")]
    [InlineData("Crypto Box", "FDUSD")]
    [InlineData("Token Swap - Distribution", "POL")]
    [InlineData("Launchpool Airdrop - System Distribution", "AAA")]
    public void Earn_VariosRotulos_EntramComoRendimento(string op, string symbol)
    {
        var mov = Assert.Single(CriptoNetting.Netar(new[] { Bruto(symbol, op, 1.23m) }));
        Assert.Equal(TipoOperacaoFinanceira.Rendimento, mov.OperationType);
        Assert.True(mov.PrecoZero);
    }

    [Fact]
    public void Earn_Subscription_E_Redemption_SaoTransferenciaInterna_Ignoradas()
    {
        // Mesma moeda sai (subscription) e volta (redemption) → net-zero na posição.
        var movimentos = CriptoNetting.Netar(new[]
        {
            Bruto("XRP", "Simple Earn Flexible Subscription", -208.7m, 1),
            Bruto("SOL", "Simple Earn Flexible Redemption", 13.65682751m, 2)
        });

        Assert.Empty(movimentos);
    }

    [Fact]
    public void TransferEntreCarteiras_Ignorada()
    {
        var movimentos = CriptoNetting.Netar(new[]
        {
            Bruto("SOL", "Transfer Between Main and Funding Wallet", 0.00110831m, 1),
            Bruto("SOL", "Transfer Between Main and Funding Wallet", -0.00110831m, 2)
        });

        Assert.Empty(movimentos);
    }

    [Fact]
    public void DepositoEFiatWithdraw_DeBRL_FicamForaDaPosicao()
    {
        var movimentos = CriptoNetting.Netar(new[]
        {
            Bruto("BRL", "Deposit", 100m, 1),
            Bruto("BRL", "Fiat Withdraw", -30m, 2)
        });

        Assert.Empty(movimentos);
    }

    [Fact]
    public void TransactionFee_EmCripto_AbateAPosicao()
    {
        // Transaction Fee em SOL (taxa paga em cripto) reduz a posição.
        var mov = Assert.Single(CriptoNetting.Netar(new[] { Bruto("SOL", "Transaction Fee", -0.0001m) }));
        Assert.Equal(TipoOperacaoFinanceira.Venda, mov.OperationType);
        Assert.Equal(0.0001m, mov.Quantity);
    }

    [Fact]
    public void SmallAssetsExchangeBNB_CadaPernaNetaIndependente()
    {
        // Small Assets Exchange BNB: várias moedas viram BNB no mesmo instante (linhas separadas).
        var movimentos = CriptoNetting.Netar(new[]
        {
            Bruto("LTC", "Small Assets Exchange BNB", -0.00065904m, 1),
            Bruto("USDT", "Small Assets Exchange BNB", -0.1314443m, 2),
            Bruto("BNB", "Small Assets Exchange BNB", 0.00020722m, 3),
            Bruto("BRL", "Small Assets Exchange BNB", -0.86805834m, 4) // fiat → fora
        });

        Assert.Equal(3, movimentos.Count);
        Assert.Equal(TipoOperacaoFinanceira.Venda, Assert.Single(movimentos, m => m.AssetSymbol == "LTC").OperationType);
        Assert.Equal(TipoOperacaoFinanceira.Venda, Assert.Single(movimentos, m => m.AssetSymbol == "USDT").OperationType);
        Assert.Equal(TipoOperacaoFinanceira.Compra, Assert.Single(movimentos, m => m.AssetSymbol == "BNB").OperationType);
        Assert.DoesNotContain(movimentos, m => m.AssetSymbol == "BRL");
    }

    [Fact]
    public void LinhasLegitimasDuplicadas_DoLedger_NaoColapsam()
    {
        // §11: o ledger da Binance pode ter linhas LEGÍTIMAS iguais — mesmo timestamp, moeda, operação e
        // quantidade (caso real: USDT). Cada linha é um staging distinto (id). O netting NÃO deduplica por
        // ativo/qtd/op → as duas linhas viram dois movimentos canônicos (com SourceStagingId distintos,
        // que alimentam a chave natural por staging "BinanceLedger|TransacaoCripto|{StagingId}").
        var movimentos = CriptoNetting.Netar(new[]
        {
            Bruto("USDT", "Binance Convert", -55.3525m, 1),
            Bruto("USDT", "Binance Convert", -55.3525m, 2)
        });

        Assert.Equal(2, movimentos.Count);
        Assert.All(movimentos, m => Assert.Equal(TipoOperacaoFinanceira.Venda, m.OperationType));
        Assert.All(movimentos, m => Assert.Equal(55.3525m, m.Quantity));
        // Os dois movimentos têm SourceStagingId distintos → chaves naturais distintas (não colapsam).
        Assert.Equal(2, movimentos.Select(m => m.SourceStagingId).Distinct().Count());
    }

    [Fact]
    public void Usdt_QueFechaEmZero_MaterializaTodasAsPernas_SemSaldoFantasma()
    {
        // Cenário USDT do §11: o ledger bruto fecha em zero (entradas = saídas), inclusive com pernas
        // LEGÍTIMAS idênticas. Todas as pernas precisam materializar (entradas e saídas) para o saldo
        // líquido bater em zero — colapsar as duplicatas deixaria saldo fantasma.
        var movimentos = CriptoNetting.Netar(new[]
        {
            Bruto("USDT", "Buy Crypto With Fiat", 100m, 1),
            Bruto("USDT", "Binance Convert", -55.3525m, 2),
            Bruto("USDT", "Binance Convert", -55.3525m, 3), // duplicata legítima da anterior
            Bruto("USDT", "Simple Earn Flexible Interest", 10.705m, 4)
        });

        // Todas as 4 pernas viram movimento (nenhuma é colapsada/descartada por chave natural).
        Assert.Equal(4, movimentos.Count);
        Assert.Equal(4, movimentos.Select(m => m.SourceStagingId).Distinct().Count());

        // Saldo líquido: +100 -55.3525 -55.3525 +10.705 = 0 → sem fantasma e sem negativo.
        var saldo = movimentos.Sum(m => m.OperationType == TipoOperacaoFinanceira.Venda ? -m.Quantity : m.Quantity);
        Assert.Equal(0m, saldo);
    }

    [Fact]
    public void StablecoinsUsdtUsdcFdusd_SaoCripto_NaoFiat()
    {
        // §11: USDT/USDC/FDUSD continuam sendo cripto (netam normalmente), não fiat — só somem da
        // carteira quando a posição líquida zera (coberto pelo teste acima).
        Assert.False(CriptoNetting.EhFiat("USDT"));
        Assert.False(CriptoNetting.EhFiat("USDC"));
        Assert.False(CriptoNetting.EhFiat("FDUSD"));

        var movimentos = CriptoNetting.Netar(new[]
        {
            Bruto("USDC", "Binance Convert", -10m, 1),
            Bruto("FDUSD", "Binance Convert", 10m, 2)
        });
        Assert.Equal(TipoOperacaoFinanceira.Venda, Assert.Single(movimentos, m => m.AssetSymbol == "USDC").OperationType);
        Assert.Equal(TipoOperacaoFinanceira.Compra, Assert.Single(movimentos, m => m.AssetSymbol == "FDUSD").OperationType);
    }

    [Fact]
    public void StakingPurchase_NaoEhRendimento_EhPermuta()
    {
        // §11: WBETH2.0 - Staking e SOL Staking - Purchase são permutas de staking (abatem ETH/SOL,
        // aumentam WBETH/BNSOL), NÃO rendimento.
        Assert.Equal(CategoriaCripto.Permuta, CriptoNetting.Classificar("WBETH2.0 - Staking"));
        Assert.Equal(CategoriaCripto.Permuta, CriptoNetting.Classificar("SOL Staking - Purchase"));
    }

    [Fact]
    public void ChangeZeroOuSimboloVazio_NaoGeramMovimento()
    {
        var movimentos = CriptoNetting.Netar(new[]
        {
            Bruto("BTC", "Transaction Buy", 0m, 1),
            Bruto("", "Transaction Buy", 1m, 2)
        });

        Assert.Empty(movimentos);
    }

    [Fact]
    public void Classificar_ReconheceCategorias()
    {
        Assert.Equal(CategoriaCripto.Fiat, CriptoNetting.Classificar("Deposit"));
        Assert.Equal(CategoriaCripto.Fiat, CriptoNetting.Classificar("Fiat Withdraw"));
        Assert.Equal(CategoriaCripto.TransferenciaInterna, CriptoNetting.Classificar("Simple Earn Flexible Subscription"));
        Assert.Equal(CategoriaCripto.TransferenciaInterna, CriptoNetting.Classificar("Simple Earn Flexible Redemption"));
        Assert.Equal(CategoriaCripto.Rendimento, CriptoNetting.Classificar("Simple Earn Flexible Interest"));
        Assert.Equal(CategoriaCripto.Rendimento, CriptoNetting.Classificar("HODLer Airdrops Distribution"));
        Assert.Equal(CategoriaCripto.Permuta, CriptoNetting.Classificar("Binance Convert"));
        Assert.Equal(CategoriaCripto.Permuta, CriptoNetting.Classificar("Transaction Buy"));
        Assert.Equal(CategoriaCripto.Permuta, CriptoNetting.Classificar("WBETH2.0 - Staking"));
    }

    [Fact]
    public void EhFiat_ReconheceBRLeFiat_MasNaoStablecoin()
    {
        Assert.True(CriptoNetting.EhFiat("BRL"));
        Assert.True(CriptoNetting.EhFiat("USD"));
        Assert.False(CriptoNetting.EhFiat("USDT"));  // stablecoin é cripto, só neta
        Assert.False(CriptoNetting.EhFiat("BTC"));
    }
}
