using Sistema.APP.DTOs;
using Sistema.APP.Services;
using Sistema.CORE.Entities;

namespace Sistema.Tests;

// F-Q — lógica pura de montagem do "Explique este valor" (Posições + Patrimônio). Cobre a fonte do
// preço (cotação ao vivo × fechamento B3 × custo/fallback), o resultado, o ajuste de reconciliação e
// a composição do patrimônio. Não toca DbContext nem API — só monta os DTOs a partir de sinais.
public class MontadorExplicacaoValorTests
{
    private static MontadorExplicacaoValor.EntradaPosicao Entrada(
        decimal qtd = 100m,
        decimal pm = 10m,
        decimal custo = 1000m,
        decimal valorMercado = 1200m,
        decimal? precoUsado = 12m,
        bool temPreco = true,
        ProvedorCotacao provedor = ProvedorCotacao.Brapi,
        StatusCotacao status = StatusCotacao.Atual,
        bool vencida = false,
        decimal ajuste = 0m,
        bool temAjuste = false)
        => new(
            Ticker: "BBAS3",
            Nome: "Banco do Brasil",
            Classe: "Acao",
            EhCripto: false,
            Quantidade: qtd,
            PrecoMedio: pm,
            Custo: custo,
            ValorMercado: valorMercado,
            PrecoUsado: precoUsado,
            TemPrecoUtilizavel: temPreco,
            Provedor: provedor,
            StatusCotacao: status,
            Vencida: vencida,
            CotacaoEm: new DateTime(2026, 6, 29, 12, 0, 0, DateTimeKind.Utc),
            Simbolo: "BBAS3.SA",
            ValorAjusteReconciliacao: ajuste,
            TemAjusteReconciliacao: temAjuste);

    [Fact]
    public void Posicao_ComCotacaoBrapi_FonteAtualEResultadoPositivo()
    {
        var dto = MontadorExplicacaoValor.Posicao(Entrada());

        Assert.True(dto.Encontrada);
        Assert.Equal("BBAS3", dto.Ticker);
        Assert.Equal("Cotação (Brapi)", dto.FontePreco);
        Assert.Equal("Atual", dto.FonteStatus);
        Assert.Equal("ok", dto.FonteSeveridade);
        Assert.False(dto.ValoradoPeloCusto);
        Assert.Equal(1200m, dto.ValorMercado);
        Assert.Equal(1000m, dto.Custo);
        Assert.Equal(200m, dto.Resultado);
        Assert.Equal(20m, dto.ResultadoPercentual);
        Assert.Equal("BBAS3", dto.BuscaTransacoes);
        // Mostra qtd, PM e o preço usado (R$ 12,00) explicitamente.
        Assert.Contains(dto.Linhas, l => l.Rotulo == "Quantidade");
        Assert.Contains(dto.Linhas, l => l.Rotulo == "Preço médio (PM)");
        Assert.Contains(dto.Linhas, l => l.Rotulo == "Preço usado" && l.Valor.Contains("12"));
    }

    [Fact]
    public void Posicao_SemCotacao_ValoraPeloCustoComFallbackEAtencao()
    {
        // Sem preço utilizável → valorado pelo custo; fonte vira "Custo" e a severidade não é "ok".
        var dto = MontadorExplicacaoValor.Posicao(Entrada(
            valorMercado: 1000m, precoUsado: null, temPreco: false,
            provedor: ProvedorCotacao.Brapi, status: StatusCotacao.SemToken));

        Assert.True(dto.ValoradoPeloCusto);
        Assert.Equal("Custo", dto.FontePreco);
        Assert.Equal("Sem token", dto.FonteStatus);
        Assert.NotEqual("ok", dto.FonteSeveridade);
        Assert.Equal(0m, dto.Resultado); // valorMercado == custo no fallback
        Assert.Contains(dto.Linhas, l => l.Rotulo == "Preço usado" && l.Valor.Contains("sem cotação"));
        Assert.Contains(dto.Linhas, l => l.Rotulo == "Observação");
    }

    [Fact]
    public void Posicao_FechamentoB3_FonteEhB3CustodiaComAtencao()
    {
        var dto = MontadorExplicacaoValor.Posicao(Entrada(provedor: ProvedorCotacao.B3Custodia));

        Assert.Equal("Fechamento B3", dto.FontePreco);
        Assert.Equal("B3 Custódia", dto.FonteStatus);
        Assert.Equal("atencao", dto.FonteSeveridade);
    }

    [Fact]
    public void Posicao_CotacaoVencida_FonteVencidaComAtencao()
    {
        var dto = MontadorExplicacaoValor.Posicao(Entrada(vencida: true));

        Assert.Equal("Vencida", dto.FonteStatus);
        Assert.Equal("atencao", dto.FonteSeveridade);
        Assert.False(dto.ValoradoPeloCusto);
    }

    [Fact]
    public void Posicao_ComAjusteReconciliacao_ExibeLinhaDeVariacao()
    {
        var dto = MontadorExplicacaoValor.Posicao(Entrada(ajuste: -150.50m, temAjuste: true));

        Assert.True(dto.TemAjusteReconciliacao);
        Assert.Equal(-150.50m, dto.ValorAjusteReconciliacao);
        Assert.Contains(dto.Linhas, l => l.Rotulo == "Ajuste de reconciliação (B3)");
    }

    [Fact]
    public void Posicao_ResultadoNegativo_MarcaTipoNegativo()
    {
        var dto = MontadorExplicacaoValor.Posicao(Entrada(valorMercado: 800m, precoUsado: 8m));

        Assert.Equal(-200m, dto.Resultado);
        Assert.Contains(dto.Linhas, l => l.Rotulo.StartsWith("Resultado", StringComparison.Ordinal) && l.Tipo == "negativo");
    }

    [Fact]
    public void PosicaoNaoEncontrada_MarcaEncontradaFalse()
    {
        var dto = MontadorExplicacaoValor.PosicaoNaoEncontrada();
        Assert.False(dto.Encontrada);
        Assert.Empty(dto.Linhas);
    }

    [Fact]
    public void Patrimonio_DecompoeTotalPorFonteEReconciliacao()
    {
        var dto = MontadorExplicacaoValor.Patrimonio(new MontadorExplicacaoValor.EntradaPatrimonio(
            Total: 1000m,
            ComCotacao: 600m,
            ComFechamentoB3: 300m,
            ComCusto: 100m,
            QtdAtivos: 5,
            QtdComCotacao: 3,
            QtdComFechamentoB3: 1,
            QtdComCusto: 1,
            TemReconciliacao: true,
            ValorReconciliacao: 50m,
            QtdAjustesReconciliacao: 2));

        Assert.True(dto.TemDados);
        Assert.Equal(1000m, dto.Total);
        Assert.Equal(600m, dto.ComCotacao);
        Assert.Equal(300m, dto.ComFechamentoB3);
        Assert.Equal(100m, dto.ComCusto);
        Assert.True(dto.TemReconciliacao);
        Assert.Equal(50m, dto.ValorReconciliacao);
        // Linhas trazem total + cada fonte + reconciliação.
        Assert.Contains(dto.Linhas, l => l.Rotulo == "Patrimônio total");
        Assert.Contains(dto.Linhas, l => l.Rotulo.StartsWith("Cotação ao vivo", StringComparison.Ordinal));
        Assert.Contains(dto.Linhas, l => l.Rotulo.StartsWith("Fechamento B3", StringComparison.Ordinal));
        Assert.Contains(dto.Linhas, l => l.Rotulo.StartsWith("Custo / fallback", StringComparison.Ordinal));
        Assert.Contains(dto.Linhas, l => l.Rotulo.StartsWith("Reconciliação B3", StringComparison.Ordinal));
    }

    [Fact]
    public void Patrimonio_SemCusto_NaoMarcaAtencaoNoFallback()
    {
        var dto = MontadorExplicacaoValor.Patrimonio(new MontadorExplicacaoValor.EntradaPatrimonio(
            Total: 900m,
            ComCotacao: 600m,
            ComFechamentoB3: 300m,
            ComCusto: 0m,
            QtdAtivos: 4,
            QtdComCotacao: 3,
            QtdComFechamentoB3: 1,
            QtdComCusto: 0,
            TemReconciliacao: false,
            ValorReconciliacao: 0m,
            QtdAjustesReconciliacao: 0));

        Assert.True(dto.TemDados);
        Assert.False(dto.TemReconciliacao);
        // Sem custo e sem reconciliação, não há nota de atenção de fallback nem linha de reconciliação.
        Assert.DoesNotContain(dto.Linhas, l => l.Rotulo.StartsWith("Reconciliação B3", StringComparison.Ordinal));
    }

    [Fact]
    public void PatrimonioVazio_MarcaTemDadosFalse()
    {
        var dto = MontadorExplicacaoValor.PatrimonioVazio();
        Assert.False(dto.TemDados);
        Assert.Empty(dto.Linhas);
    }
}
