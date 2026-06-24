using Sistema.CORE.Entities;
using Sistema.INFRA.Importers;

namespace Sistema.Tests;

// F2 — valoração em BRL na data (specs/cripto.spec.md §4/§5). Lógica pura (sem DbContext): dada a perna
// netada + o preço de mercado BRL na data + o BRL pago (compra fiat) + o PM corrente, define o UnitPrice,
// a confiança e se faltou preço (→ a persistência registra AlertaConfiabilidade). Espelha as regras por
// tipo de perna que o IR (F3) consome.
public class CriptoValoradorTests
{
    private static readonly DateTime T = new(2025, 8, 17, 7, 49, 4, DateTimeKind.Utc);

    private static MovimentoCriptoCanonico Perna(TipoOperacaoFinanceira tipo, decimal qtd, bool precoZero = false)
        => new("BTC", T, tipo, qtd, DefinirPrecoNaPersistencia: true, PrecoZero: precoZero, "op", 1);

    [Fact]
    public void Venda_ValoraAoPrecoBrlDaData()
    {
        // Perna de saída / permuta-origem: UnitPrice = preço de mercado BRL na data (valor de alienação).
        var mov = Perna(TipoOperacaoFinanceira.Venda, 0.5m);
        var r = CriptoValorador.Valorar(mov, precoMercadoNaData: 350000m, totalFiatBrl: null, pmCorrente: 100000m);

        Assert.Equal(350000m, r.UnitPrice);
        Assert.False(r.PrecoFaltante);
        Assert.Equal(NivelConfianca.Media, r.Confianca);
    }

    [Fact]
    public void PermutaDestino_ValoraAoPrecoBrlDaData()
    {
        // Perna de entrada de troca (sem fiat): custo = preço de mercado BRL na data.
        var mov = Perna(TipoOperacaoFinanceira.Compra, 1m);
        var r = CriptoValorador.Valorar(mov, precoMercadoNaData: 1200m, totalFiatBrl: null, pmCorrente: 0m);

        Assert.Equal(1200m, r.UnitPrice);
        Assert.False(r.PrecoFaltante);
    }

    [Fact]
    public void CompraComFiat_UsaOBrlPago_NaoOClose()
    {
        // Compra com fiat: o BRL pago manda (mais fiel que o close diário). 250 BRL / 2 = 125 por unidade.
        var mov = Perna(TipoOperacaoFinanceira.Compra, 2m);
        var r = CriptoValorador.Valorar(mov, precoMercadoNaData: 130m, totalFiatBrl: 250m, pmCorrente: 0m);

        Assert.Equal(125m, r.UnitPrice);
        Assert.Equal(NivelConfianca.Alta, r.Confianca);
        Assert.False(r.PrecoFaltante);
    }

    [Fact]
    public void Earn_CustoIgualMercadoNaData_NaoZero()
    {
        // §4: rendimento entra com custo de aquisição = mercado na data (NÃO 0).
        var mov = Perna(TipoOperacaoFinanceira.Rendimento, 0.00000006m, precoZero: true);
        var r = CriptoValorador.Valorar(mov, precoMercadoNaData: 600000m, totalFiatBrl: null, pmCorrente: 0m);

        Assert.Equal(600000m, r.UnitPrice);
        Assert.False(r.PrecoFaltante);
        Assert.Equal(NivelConfianca.Media, r.Confianca);
    }

    [Fact]
    public void Earn_SemPreco_Custo0_ConfiancaBaixa_EAlerta()
    {
        var mov = Perna(TipoOperacaoFinanceira.Rendimento, 1m, precoZero: true);
        var r = CriptoValorador.Valorar(mov, precoMercadoNaData: null, totalFiatBrl: null, pmCorrente: 0m);

        Assert.Equal(0m, r.UnitPrice);
        Assert.True(r.PrecoFaltante);
        Assert.Equal(NivelConfianca.Baixa, r.Confianca);
    }

    [Fact]
    public void Venda_SemPreco_UsaPmCorrente_RealizadoZero_EAlerta()
    {
        // Sem preço: não chuta ganho — usa o PM corrente (realizado ≈ 0) e sinaliza alerta/confiança baixa.
        var mov = Perna(TipoOperacaoFinanceira.Venda, 1m);
        var r = CriptoValorador.Valorar(mov, precoMercadoNaData: null, totalFiatBrl: null, pmCorrente: 90000m);

        Assert.Equal(90000m, r.UnitPrice);
        Assert.True(r.PrecoFaltante);
        Assert.Equal(NivelConfianca.Baixa, r.Confianca);
    }

    [Fact]
    public void PermutaDestino_SemPreco_UsaPmCorrente_EAlerta()
    {
        var mov = Perna(TipoOperacaoFinanceira.Compra, 1m);
        var r = CriptoValorador.Valorar(mov, precoMercadoNaData: null, totalFiatBrl: null, pmCorrente: 80m);

        Assert.Equal(80m, r.UnitPrice);
        Assert.True(r.PrecoFaltante);
        Assert.Equal(NivelConfianca.Baixa, r.Confianca);
    }

    [Fact]
    public void PermutaDestino_SemPreco_SemPm_UsaNeutro1_EAlerta()
    {
        var mov = Perna(TipoOperacaoFinanceira.Compra, 3m);
        var r = CriptoValorador.Valorar(mov, precoMercadoNaData: null, totalFiatBrl: null, pmCorrente: 0m);

        Assert.Equal(1m, r.UnitPrice);
        Assert.True(r.PrecoFaltante);
    }

    [Fact]
    public void PrecoZeroOuNegativo_NaoConta_ComoPreco()
    {
        // Um close <= 0 (ruído) não é preço: cai no fallback (PM corrente) e sinaliza faltante.
        var mov = Perna(TipoOperacaoFinanceira.Venda, 1m);
        var r = CriptoValorador.Valorar(mov, precoMercadoNaData: 0m, totalFiatBrl: null, pmCorrente: 50m);

        Assert.Equal(50m, r.UnitPrice);
        Assert.True(r.PrecoFaltante);
    }
}
