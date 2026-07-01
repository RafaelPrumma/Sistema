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

    // ─── Carteira (2ª fatia) ───────────────────────────────────────────────────────────────────────

    private static MontadorExplicacaoValor.EntradaCarteira EntradaCarteira(
        decimal valor = 1000m,
        decimal comCotacao = 600m,
        decimal comFechamentoB3 = 300m,
        decimal comCusto = 100m,
        int qCotacao = 3,
        int qFechamento = 1,
        int qCusto = 1,
        decimal pesoAtual = 25m,
        decimal? pesoAlvo = 30m,
        decimal ajuste = 0m,
        bool temAjuste = false)
        => new(
            Nome: "FIIs",
            Tipo: "Carteira",
            ValorMercado: valor,
            ComCotacao: comCotacao,
            ComFechamentoB3: comFechamentoB3,
            ComCusto: comCusto,
            QtdAtivos: qCotacao + qFechamento + qCusto,
            QtdComCotacao: qCotacao,
            QtdComFechamentoB3: qFechamento,
            QtdComCusto: qCusto,
            PesoAtual: pesoAtual,
            PesoAlvo: pesoAlvo,
            TemAjusteReconciliacao: temAjuste,
            ValorAjusteReconciliacao: ajuste);

    [Fact]
    public void Carteira_DecompoePorFonteEMostraPesoAtualVsAlvo()
    {
        var dto = MontadorExplicacaoValor.Carteira(EntradaCarteira());

        Assert.True(dto.Encontrada);
        Assert.Equal("FIIs", dto.Nome);
        Assert.Equal(1000m, dto.ValorMercado);
        Assert.Equal(600m, dto.ComCotacao);
        Assert.Equal(300m, dto.ComFechamentoB3);
        Assert.Equal(100m, dto.ComCusto);
        Assert.Equal(25m, dto.PesoAtual);
        Assert.Equal(30m, dto.PesoAlvo);
        // Linhas trazem valor + cada fonte + peso atual/alvo + desvio.
        Assert.Contains(dto.Linhas, l => l.Rotulo == "Valor da carteira");
        Assert.Contains(dto.Linhas, l => l.Rotulo.StartsWith("Cotação ao vivo", StringComparison.Ordinal));
        Assert.Contains(dto.Linhas, l => l.Rotulo.StartsWith("Fechamento B3", StringComparison.Ordinal));
        Assert.Contains(dto.Linhas, l => l.Rotulo.StartsWith("Custo / fallback", StringComparison.Ordinal));
        Assert.Contains(dto.Linhas, l => l.Rotulo == "Peso atual (no patrimônio)");
        Assert.Contains(dto.Linhas, l => l.Rotulo == "Peso-alvo" && l.Valor.Contains("30"));
        Assert.Contains(dto.Linhas, l => l.Rotulo.StartsWith("Desvio", StringComparison.Ordinal));
    }

    [Fact]
    public void Carteira_SemPesoAlvo_MostraNaoDefinidoESemDesvio()
    {
        var dto = MontadorExplicacaoValor.Carteira(EntradaCarteira(pesoAlvo: null));

        Assert.Null(dto.PesoAlvo);
        Assert.Contains(dto.Linhas, l => l.Rotulo == "Peso-alvo" && l.Valor.Contains("não definido"));
        Assert.DoesNotContain(dto.Linhas, l => l.Rotulo.StartsWith("Desvio", StringComparison.Ordinal));
    }

    [Fact]
    public void Carteira_ComAjusteReconciliacao_ExibeLinhaDeVariacao()
    {
        var dto = MontadorExplicacaoValor.Carteira(EntradaCarteira(ajuste: -80.25m, temAjuste: true));

        Assert.True(dto.TemAjusteReconciliacao);
        Assert.Equal(-80.25m, dto.ValorAjusteReconciliacao);
        Assert.Contains(dto.Linhas, l => l.Rotulo == "Ajuste de reconciliação (B3)");
    }

    [Fact]
    public void Carteira_SemCusto_NaoMarcaNotaDeFallback()
    {
        var dto = MontadorExplicacaoValor.Carteira(EntradaCarteira(comCusto: 0m, qCusto: 0, valor: 900m, comCotacao: 600m, comFechamentoB3: 300m));

        // Sem custo, não há a nota de atenção sobre fallback ao custo.
        Assert.DoesNotContain(dto.Linhas, l => l.Rotulo == "" && l.Valor.Contains("preço médio"));
    }

    [Fact]
    public void CarteiraNaoEncontrada_MarcaEncontradaFalse()
    {
        var dto = MontadorExplicacaoValor.CarteiraNaoEncontrada();
        Assert.False(dto.Encontrada);
        Assert.Empty(dto.Linhas);
    }

    // ─── Proventos (2ª fatia) ──────────────────────────────────────────────────────────────────────

    [Fact]
    public void Proventos_DecompoePorFonteEPorTipoComNotaDePrecedencia()
    {
        var dto = MontadorExplicacaoValor.Proventos(new MontadorExplicacaoValor.EntradaProventos(
            TotalRecebido: 1000m,
            Quantidade: 12,
            PeriodoInicio: new DateTime(2025, 7, 1),
            PeriodoFim: new DateTime(2026, 6, 1),
            PorFonte:
            [
                new MontadorExplicacaoValor.GrupoProvento("B3 Extrato", 700m, 8),
                new MontadorExplicacaoValor.GrupoProvento("Brapi", 200m, 3),
                new MontadorExplicacaoValor.GrupoProvento("Binance Earn", 100m, 1),
            ],
            PorTipo:
            [
                new MontadorExplicacaoValor.GrupoProvento("Rendimento FII", 600m, 6),
                new MontadorExplicacaoValor.GrupoProvento("Dividendo", 300m, 5),
                new MontadorExplicacaoValor.GrupoProvento("Earn", 100m, 1),
            ]));

        Assert.True(dto.TemDados);
        Assert.Equal(1000m, dto.TotalRecebido);
        Assert.Equal(12, dto.Quantidade);
        Assert.Equal("01/07/2025", dto.PeriodoInicio);
        Assert.Equal("01/06/2026", dto.PeriodoFim);
        // Cabeçalhos de seção + cada fonte + cada tipo + período + nota de precedência.
        Assert.Contains(dto.Linhas, l => l.Rotulo == "Recebido (12 meses)");
        Assert.Contains(dto.Linhas, l => l.Rotulo == "Período coberto");
        Assert.Contains(dto.Linhas, l => l.Rotulo == "Por fonte do dado");
        Assert.Contains(dto.Linhas, l => l.Rotulo.Contains("B3 Extrato"));
        Assert.Contains(dto.Linhas, l => l.Rotulo == "Por tipo");
        Assert.Contains(dto.Linhas, l => l.Rotulo.Contains("Rendimento FII"));
        // Nota de precedência (linha sem rótulo, tom atenção).
        Assert.Contains(dto.Linhas, l => l.Rotulo == "" && l.Valor.Contains("Precedência") && l.Tipo == "atencao");
    }

    [Fact]
    public void Proventos_SemFonteNemTipo_AindaTrazTotalENota()
    {
        var dto = MontadorExplicacaoValor.Proventos(new MontadorExplicacaoValor.EntradaProventos(
            TotalRecebido: 50m,
            Quantidade: 1,
            PeriodoInicio: null,
            PeriodoFim: null,
            PorFonte: [],
            PorTipo: []));

        Assert.True(dto.TemDados);
        Assert.Null(dto.PeriodoInicio);
        Assert.DoesNotContain(dto.Linhas, l => l.Rotulo == "Período coberto");
        Assert.DoesNotContain(dto.Linhas, l => l.Rotulo == "Por fonte do dado");
        Assert.Contains(dto.Linhas, l => l.Rotulo == "Recebido (12 meses)");
        Assert.Contains(dto.Linhas, l => l.Valor.Contains("Precedência"));
    }

    [Fact]
    public void ProventosVazio_MarcaTemDadosFalse()
    {
        var dto = MontadorExplicacaoValor.ProventosVazio();
        Assert.False(dto.TemDados);
        Assert.Empty(dto.Linhas);
    }
}
