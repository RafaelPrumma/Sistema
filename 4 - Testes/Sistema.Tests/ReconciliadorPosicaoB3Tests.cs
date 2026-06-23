using Sistema.CORE.Entities;
using Sistema.INFRA.Importers;

namespace Sistema.Tests;

// F3 — lógica pura da reconciliação da posição pela aba Posição da B3 (sem DbContext):
// extração dos alvos da Posição e o cálculo dos ajustes (diff = alvo − calculado).
public class ReconciliadorPosicaoB3Tests
{
    [Fact]
    public void ExtrairAlvos_LeTickerEQuantidade_DaPosicao()
    {
        // Posição mais recente: NCHB11 30 cotas, BBAS3 102 ações.
        var linhas = new[]
        {
            LinhaPosicao("NCHB11", "30"),
            LinhaPosicao("BBAS3", "102"),
        };

        var alvos = ReconciliadorPosicaoB3.ExtrairAlvos(linhas);

        Assert.Equal(30m, alvos["NCHB11"]);
        Assert.Equal(102m, alvos["BBAS3"]);
        Assert.Equal(2, alvos.Count);
    }

    [Fact]
    public void ExtrairAlvos_SomaFracionarioNoLotePadrao()
    {
        // O fracionário (ITUB4F) é o MESMO ativo do lote-padrão (ITUB4): as quantidades somam no base.
        var linhas = new[]
        {
            LinhaPosicao("ITUB4", "100"),
            LinhaPosicao("ITUB4F", "7"),
            LinhaPosicao("IRIM11", "5"), // alias → IRDM11
        };

        var alvos = ReconciliadorPosicaoB3.ExtrairAlvos(linhas);

        Assert.Equal(107m, alvos["ITUB4"]);
        Assert.Equal(5m, alvos["IRDM11"]);
        Assert.False(alvos.ContainsKey("ITUB4F"));
        Assert.False(alvos.ContainsKey("IRIM11"));
    }

    [Fact]
    public void ExtrairAlvos_IgnoraLinhaSemTickerOuQuantidadeZero()
    {
        var linhas = new[]
        {
            LinhaPosicao("", "10"),       // sem ticker
            LinhaPosicao("MXRF11", "0"),  // quantidade zero
            LinhaPosicao("HGLG11", "12"),
        };

        var alvos = ReconciliadorPosicaoB3.ExtrairAlvos(linhas);

        var unico = Assert.Single(alvos);
        Assert.Equal("HGLG11", unico.Key);
        Assert.Equal(12m, unico.Value);
    }

    [Fact]
    public void ExtrairPrecosFechamento_LeTickerEPreco_DaPosicao()
    {
        // O xlsx da B3 grava número com "." (invariant), como ParseDecimal espera: NCHB11 9.87, BBAS3 23.45.
        var linhas = new[]
        {
            LinhaPosicaoComPreco("NCHB11", "30", "9.87"),
            LinhaPosicaoComPreco("BBAS3", "102", "23.45"),
        };

        var precos = ReconciliadorPosicaoB3.ExtrairPrecosFechamento(linhas);

        Assert.Equal(9.87m, precos["NCHB11"]);
        Assert.Equal(23.45m, precos["BBAS3"]);
        Assert.Equal(2, precos.Count);
    }

    [Fact]
    public void ExtrairPrecosFechamento_NormalizaTickerENaoSomaPrecos()
    {
        // Fracionário (ITUB4F) e lote-padrão (ITUB4) são o MESMO papel com o MESMO preço: não somar.
        // Alias IRIM11 → IRDM11. O primeiro preço positivo do ticker normalizado prevalece.
        var linhas = new[]
        {
            LinhaPosicaoComPreco("ITUB4", "100", "31.10"),
            LinhaPosicaoComPreco("ITUB4F", "7", "31.10"),
            LinhaPosicaoComPreco("IRIM11", "5", "88.40"),
        };

        var precos = ReconciliadorPosicaoB3.ExtrairPrecosFechamento(linhas);

        Assert.Equal(31.10m, precos["ITUB4"]);
        Assert.Equal(88.40m, precos["IRDM11"]);
        Assert.False(precos.ContainsKey("ITUB4F"));
        Assert.False(precos.ContainsKey("IRIM11"));
    }

    [Fact]
    public void ExtrairPrecosFechamento_IgnoraLinhaSemTickerOuPrecoZero()
    {
        var linhas = new[]
        {
            LinhaPosicaoComPreco("", "10", "12.00"),     // sem ticker
            LinhaPosicaoComPreco("MXRF11", "100", "0"),  // preço zero (sem fechamento utilizável)
            LinhaPosicaoComPreco("HGLG11", "12", "158.30"),
        };

        var precos = ReconciliadorPosicaoB3.ExtrairPrecosFechamento(linhas);

        var unico = Assert.Single(precos);
        Assert.Equal("HGLG11", unico.Key);
        Assert.Equal(158.30m, unico.Value);
    }

    [Fact]
    public void CalcularAjustes_FantasmaForaDaCustodia_ZeraComVenda()
    {
        // NCHB11: o cálculo tem +30 (compra sem a venda), mas NÃO está na Posição → alvo 0.
        // diff = 0 − 30 = −30 → ajuste de Venda de 30 ao PM corrente (realizado ≈ 0).
        var ativos = new[] { new AtivoReconciliavel(10, "NCHB11") };
        var calculado = new Dictionary<int, decimal> { [10] = 30m };
        var pm = new Dictionary<int, decimal> { [10] = 9.50m };
        var alvo = new Dictionary<string, decimal>(); // ausente → 0

        var ajustes = ReconciliadorPosicaoB3.CalcularAjustes(ativos, calculado, pm, alvo);

        var ajuste = Assert.Single(ajustes);
        Assert.Equal(10, ajuste.AssetId);
        Assert.Equal(TipoOperacaoFinanceira.Venda, ajuste.OperationType);
        Assert.Equal(30m, ajuste.Quantidade);
        Assert.Equal(9.50m, ajuste.PrecoMedio);
        Assert.Equal(285m, ajuste.ValorContrapartida); // 30 × 9,50
        Assert.Equal(0m, ajuste.Alvo);
        Assert.Equal(30m, ajuste.Calculado);
    }

    [Fact]
    public void CalcularAjustes_AlvoBateComCalculado_NaoGeraAjuste()
    {
        // BBAS3: cálculo = 102, Posição = 102 → diff 0 → sem ajuste (não cria fantasma).
        var ativos = new[] { new AtivoReconciliavel(1, "BBAS3") };
        var calculado = new Dictionary<int, decimal> { [1] = 102m };
        var pm = new Dictionary<int, decimal> { [1] = 25m };
        var alvo = new Dictionary<string, decimal> { ["BBAS3"] = 102m };

        var ajustes = ReconciliadorPosicaoB3.CalcularAjustes(ativos, calculado, pm, alvo);

        Assert.Empty(ajustes);
    }

    [Fact]
    public void CalcularAjustes_DiferencaAbaixoDoEpsilon_NaoGeraAjuste()
    {
        var ativos = new[] { new AtivoReconciliavel(1, "BBAS3") };
        var calculado = new Dictionary<int, decimal> { [1] = 102.0000001m };
        var pm = new Dictionary<int, decimal> { [1] = 25m };
        var alvo = new Dictionary<string, decimal> { ["BBAS3"] = 102m };

        Assert.Empty(ReconciliadorPosicaoB3.CalcularAjustes(ativos, calculado, pm, alvo));
    }

    [Fact]
    public void CalcularAjustes_CustodiaMaiorQueCalculado_CompraParaAlvo()
    {
        // Posição tem 15 mas o cálculo só tem 10 (compra antiga faltando) → diff +5 → Compra de 5.
        var ativos = new[] { new AtivoReconciliavel(7, "HGLG11") };
        var calculado = new Dictionary<int, decimal> { [7] = 10m };
        var pm = new Dictionary<int, decimal> { [7] = 160m };
        var alvo = new Dictionary<string, decimal> { ["HGLG11"] = 15m };

        var ajuste = Assert.Single(ReconciliadorPosicaoB3.CalcularAjustes(ativos, calculado, pm, alvo));

        Assert.Equal(TipoOperacaoFinanceira.Compra, ajuste.OperationType);
        Assert.Equal(5m, ajuste.Quantidade);
        Assert.Equal(800m, ajuste.ValorContrapartida); // 5 × 160
    }

    [Fact]
    public void CalcularAjustes_SemPmConhecido_UsaZero()
    {
        // Ativo sem PM (estoque já zerado): zera a quantidade fantasma com valor 0 (sem PM, sem $).
        var ativos = new[] { new AtivoReconciliavel(10, "EQIN11") };
        var calculado = new Dictionary<int, decimal> { [10] = 6m };
        var pm = new Dictionary<int, decimal>(); // sem PM
        var alvo = new Dictionary<string, decimal>();

        var ajuste = Assert.Single(ReconciliadorPosicaoB3.CalcularAjustes(ativos, calculado, pm, alvo));

        Assert.Equal(TipoOperacaoFinanceira.Venda, ajuste.OperationType);
        Assert.Equal(6m, ajuste.Quantidade);
        Assert.Equal(0m, ajuste.PrecoMedio);
        Assert.Equal(0m, ajuste.ValorContrapartida);
    }

    [Fact]
    public void CalcularAjustes_Idempotente_MesmaEntradaMesmoResultado()
    {
        // O ajuste depende só do "calculado" SEM os próprios ajustes (a camada de persistência apaga
        // os ajustes antes de recalcular). Rodar a lógica 2× sobre a mesma entrada dá o mesmo resultado.
        var ativos = new[] { new AtivoReconciliavel(10, "IRDM11") };
        var calculado = new Dictionary<int, decimal> { [10] = 7m };
        var pm = new Dictionary<int, decimal> { [10] = 80m };
        var alvo = new Dictionary<string, decimal>();

        var primeira = ReconciliadorPosicaoB3.CalcularAjustes(ativos, calculado, pm, alvo);
        var segunda = ReconciliadorPosicaoB3.CalcularAjustes(ativos, calculado, pm, alvo);

        Assert.Equal(primeira.Count, segunda.Count);
        Assert.Equal(primeira[0].Quantidade, segunda[0].Quantidade);
        Assert.Equal(primeira[0].OperationType, segunda[0].OperationType);
        Assert.Equal(primeira[0].ValorContrapartida, segunda[0].ValorContrapartida);
    }

    [Fact]
    public void CalcularAjustes_ZeraOsQuatroFantasmasConhecidos()
    {
        // NCHB11 +30, EQIN11 +6, CMIG3 +10, IRDM11 +7 — todos vendidos sem a venda nos relatórios e
        // ausentes na Posição → todos devem virar Venda da quantidade fantasma (alvo 0).
        var ativos = new[]
        {
            new AtivoReconciliavel(1, "NCHB11"),
            new AtivoReconciliavel(2, "EQIN11"),
            new AtivoReconciliavel(3, "CMIG3"),
            new AtivoReconciliavel(4, "IRDM11"),
        };
        var calculado = new Dictionary<int, decimal> { [1] = 30m, [2] = 6m, [3] = 10m, [4] = 7m };
        var pm = new Dictionary<int, decimal> { [1] = 1m, [2] = 1m, [3] = 1m, [4] = 1m };
        var alvo = new Dictionary<string, decimal>(); // nenhum na custódia

        var ajustes = ReconciliadorPosicaoB3.CalcularAjustes(ativos, calculado, pm, alvo);

        Assert.Equal(4, ajustes.Count);
        Assert.All(ajustes, a => Assert.Equal(TipoOperacaoFinanceira.Venda, a.OperationType));
        Assert.Equal(30m, ajustes.Single(a => a.AssetId == 1).Quantidade);
        Assert.Equal(6m, ajustes.Single(a => a.AssetId == 2).Quantidade);
        Assert.Equal(10m, ajustes.Single(a => a.AssetId == 3).Quantidade);
        Assert.Equal(7m, ajustes.Single(a => a.AssetId == 4).Quantidade);
    }

    // Monta uma linha de Posição (header→valor) como a gravada no ConteudoBruto (campos Código de
    // Negociação + Quantidade são os que a reconciliação lê).
    private static Dictionary<string, string> LinhaPosicao(string ticker, string quantidade)
        => new(StringComparer.OrdinalIgnoreCase)
        {
            ["Código de Negociação"] = ticker,
            ["Quantidade"] = quantidade,
        };

    // Linha de Posição com o Preço de Fechamento (custódia) — fonte da cotação B3Custódia.
    private static Dictionary<string, string> LinhaPosicaoComPreco(string ticker, string quantidade, string precoFechamento)
        => new(StringComparer.OrdinalIgnoreCase)
        {
            ["Código de Negociação"] = ticker,
            ["Quantidade"] = quantidade,
            ["Preço de Fechamento"] = precoFechamento,
        };
}
