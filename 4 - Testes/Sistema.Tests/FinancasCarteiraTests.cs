using Moq;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Sistema.APP.Services;
using Sistema.APP.Services.Interfaces;
using Sistema.CORE.Common;
using Sistema.CORE.Entities;
using Sistema.CORE.Repositories.Interfaces;
using Sistema.INFRA.Data;
using Sistema.INFRA.Importers;

namespace Sistema.Tests;

public class FinancasCarteiraTests
{
    [Theory]
    [InlineData("FII DEVANT CI", "FII DEVANT CI", "DEVA11")]
    [InlineData("FII FYTO CI", "FII FYTO CI", "FYTO11")]
    [InlineData("FII BC FFII CI", "FII BC FFII CI", "BCFF11")]
    [InlineData("FII CENESP CI", "FII CENESP CI", "CNES11")]
    [InlineData("FII IRIDIUM CI", "FII IRIDIUM CI", "IRDM11")]
    [InlineData("BRASIL ON EJ NM", "BRASIL ON", "BBAS3")]
    public void NormalizadorB3DeveResolverTickerCanonico(string especificacao, string chaveEsperada, string tickerEsperado)
    {
        Assert.Equal(chaveEsperada, NormalizadorAtivoB3.ChaveCanonica(especificacao));
        Assert.Equal(tickerEsperado, NormalizadorAtivoB3.Ticker(especificacao));
    }

    [Fact]
    public void ParserInformeRendimentosDeveExtrairLinhasComTickerTipoEValor()
    {
        var texto = """
            Rendimentos Isentos e Nao Tributaveis
            DEVA11 Rendimento 30/12/2025 15/01/2026 R$ 123,45
            BBAS3 Juros sobre Capital Proprio 20/12/2025 30/12/2025 R$ 45,67
            PETR4 Dividendos 10/11/2025 22/11/2025 R$ 89,10
            """;

        var linhas = InformeRendimentosParser.Extrair(texto);

        Assert.Contains(linhas, x => x.Ticker == "DEVA11" && x.Tipo == "Rendimento" && x.Valor == 123.45m);
        Assert.Contains(linhas, x => x.Ticker == "BBAS3" && x.Tipo == "JCP" && x.Valor == 45.67m);
        Assert.Contains(linhas, x => x.Ticker == "PETR4" && x.Tipo == "Dividendo" && x.Valor == 89.10m);
    }

    [Fact]
    public async Task DashboardDeveIgnorarPosicaoZeradaEAgruparCarteirasComFallback()
    {
        var banco = new AtivoFinanceiro
        {
            Id = 1,
            Chave = "ITUB4",
            Sigla = "ITUB4",
            Nome = "Itau",
            Classe = ClasseAtivo.Acao,
            Mercado = "B3"
        };
        var criptoZerada = new AtivoFinanceiro
        {
            Id = 2,
            Chave = "MATIC",
            Sigla = "MATIC",
            Nome = "MATIC",
            Classe = ClasseAtivo.Cripto,
            EhCripto = true,
            Mercado = "Binance"
        };
        var fiiSemCotacao = new AtivoFinanceiro
        {
            Id = 3,
            Chave = "DEVA11",
            Sigla = "DEVA11",
            Nome = "FII DEVANT CI",
            Classe = ClasseAtivo.FII,
            Mercado = "B3"
        };
        var maticDust = new AtivoFinanceiro
        {
            Id = 4,
            Chave = "MATIC",
            Sigla = "MATIC",
            Nome = "MATIC",
            Classe = ClasseAtivo.Cripto,
            EhCripto = true,
            Mercado = "Binance"
        };

        var transacoes = new List<TransacaoFinanceira>
        {
            Compra(banco, 10m, 20m, new DateTime(2026, 1, 10)),
            Compra(criptoZerada, 5m, 4m, new DateTime(2026, 1, 10)),
            Venda(criptoZerada, 5m, 5m, new DateTime(2026, 2, 10)),
            Compra(fiiSemCotacao, 10m, 100m, new DateTime(2026, 3, 10)),
            Compra(maticDust, 0.3m, 1m, new DateTime(2026, 3, 10))
        };
        var cotacoes = new List<CotacaoAtivoFinanceiro>
        {
            new()
            {
                AtivoFinanceiroId = banco.Id,
                AtivoFinanceiro = banco,
                Provedor = ProvedorCotacao.Brapi,
                Simbolo = "ITUB4",
                PrecoBRL = 25m,
                Preco = 25m,
                VariacaoPercentual = 1.5m,
                ConsultadoEm = DateTime.UtcNow,
                RawJson = "{}"
            },
            new()
            {
                AtivoFinanceiroId = maticDust.Id,
                AtivoFinanceiro = maticDust,
                Provedor = ProvedorCotacao.Binance,
                Simbolo = "MATIC",
                PrecoBRL = 2m,
                Preco = 2m,
                ConsultadoEm = DateTime.UtcNow,
                RawJson = "{}"
            }
        };
        var carteiras = new List<CarteiraFinanceira>
        {
            Carteira(10, "Bancos", "Setor", banco),
            Carteira(20, "FIIs de papel", "Classe", fiiSemCotacao),
            Carteira(30, "Cripto", "Tese", criptoZerada)
        };

        var repo = new Mock<IFinancasRepository>();
        repo.Setup(r => r.ObterCargaMaisRecenteAsync(It.IsAny<CancellationToken>())).ReturnsAsync((CargaFinanceira?)null);
        repo.Setup(r => r.BuscarAlertasAsync(It.IsAny<CancellationToken>())).ReturnsAsync([]);
        repo.Setup(r => r.BuscarPosicoesAsync(null, It.IsAny<CancellationToken>())).ReturnsAsync([]);
        repo.Setup(r => r.BuscarCotacoesAtuaisAsync(It.IsAny<CancellationToken>())).ReturnsAsync(cotacoes);
        repo.Setup(r => r.BuscarCarteirasComAtivosAsync(It.IsAny<CancellationToken>())).ReturnsAsync(carteiras);
        repo.Setup(r => r.BuscarDocumentosMonitoradosAsync(It.IsAny<CancellationToken>())).ReturnsAsync([]);
        repo.Setup(r => r.ObterUltimaImportacaoArquivoAsync(It.IsAny<CancellationToken>())).ReturnsAsync((ImportacaoFinanceiraArquivo?)null);
        repo.Setup(r => r.BuscarTodasTransacoesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(transacoes);
        repo.Setup(r => r.BuscarProventosPorPeriodoAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>())).ReturnsAsync([]);
        repo.Setup(r => r.BuscarAgregadosAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync([]);
        repo.Setup(r => r.BuscarUltimasOperacoesB3Async(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync([]);
        repo.Setup(r => r.BuscarUltimasTransacoesCriptoAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync([]);

        var service = CriarService(repo.Object);

        var dashboard = await service.ObterDashboardAsync();

        Assert.Equal(1250m, dashboard.ValorMercadoTotal);
        Assert.DoesNotContain(dashboard.AtivosCotados, a => a.AtivoId == criptoZerada.Id);
        Assert.DoesNotContain(dashboard.AtivosCotados, a => a.AtivoId == maticDust.Id);
        Assert.Contains(dashboard.AtivosCotados, a => a.AtivoId == fiiSemCotacao.Id && a.ValorMercado == 1000m && a.Status == "SemCotacao");
        Assert.Equal(2, dashboard.Carteiras.Count);
        Assert.DoesNotContain(dashboard.Carteiras, c => c.Nome == "Cripto");
        Assert.Contains(dashboard.Carteiras, c => c.Nome == "Bancos" && c.Itens.Single().Ativo == "ITUB4");
        Assert.Contains(dashboard.Carteiras, c => c.Nome == "FIIs de papel" && c.Itens.Single().Ativo == "DEVA11");
    }

    [Fact]
    public async Task ReparoDeveRepontarFragmentosParaAtivoCanonico()
    {
        await using var context = CriarContexto();
        var carga = new CargaFinanceira
        {
            Id = 1,
            SchemaVersion = "test",
            JsonSha256 = Guid.NewGuid().ToString("N"),
            SourcePath = "test",
            SummaryJson = "{}",
            UsuarioInclusao = "teste"
        };
        var canonico = new AtivoFinanceiro
        {
            Id = 1,
            Chave = "BBDC4",
            Sigla = "BBDC4",
            Nome = "BRADESCO PN",
            Classe = ClasseAtivo.Acao,
            Mercado = "B3",
            UsuarioInclusao = "teste"
        };
        var fragmento = new AtivoFinanceiro
        {
            Id = 2,
            Chave = "BRADESCO PN EJ N1",
            Nome = "BRADESCO PN EJ N1",
            Classe = ClasseAtivo.Acao,
            Mercado = "B3",
            DataExclusao = DateTime.UtcNow,
            UsuarioInclusao = "teste"
        };
        var carteira = new CarteiraFinanceira { Id = 1, Nome = "Bancario", Slug = "bancario", UsuarioInclusao = "teste" };
        context.AddRange(carga, canonico, fragmento, carteira);
        context.CarteirasAtivosFinanceiros.Add(new CarteiraAtivoFinanceiro { Id = 1, CarteiraFinanceiraId = 1, AtivoFinanceiroId = 2, UsuarioInclusao = "teste" });
        context.CotacoesAtivosFinanceiros.AddRange(
            new CotacaoAtivoFinanceiro { Id = 1, AtivoFinanceiroId = 1, Provedor = ProvedorCotacao.Brapi, Simbolo = "BBDC4", Preco = 10m, PrecoBRL = 10m, ConsultadoEm = DateTime.UtcNow.AddDays(-1), RawJson = "{}", UsuarioInclusao = "teste" },
            new CotacaoAtivoFinanceiro { Id = 2, AtivoFinanceiroId = 2, Provedor = ProvedorCotacao.Brapi, Simbolo = "BRADESCO PN", Preco = 12m, PrecoBRL = 12m, ConsultadoEm = DateTime.UtcNow, RawJson = "{}", UsuarioInclusao = "teste" });
        context.PrecosHistoricosAtivosFinanceiros.AddRange(
            new PrecoHistoricoAtivoFinanceiro { Id = 1, AtivoFinanceiroId = 1, Provedor = ProvedorCotacao.Brapi, Symbol = "BBDC4", Date = new DateTime(2026, 1, 1), Interval = "1d", Close = 10m, CloseBRL = 10m, RawJson = "{}", UsuarioInclusao = "teste" },
            new PrecoHistoricoAtivoFinanceiro { Id = 2, AtivoFinanceiroId = 2, Provedor = ProvedorCotacao.Brapi, Symbol = "BRADESCO PN", Date = new DateTime(2026, 1, 2), Interval = "1d", Close = 11m, CloseBRL = 11m, RawJson = "{}", UsuarioInclusao = "teste" });
        context.OperacoesB3.Add(new OperacaoB3 { Id = 1, CargaFinanceiraId = 1, AssetId = 2, OriginalAssetName = "BRADESCO PN EJ N1", RawJson = "{}", UsuarioInclusao = "teste" });
        context.TransacoesFinanceiras.Add(new TransacaoFinanceira { Id = 1, AssetId = 2, Date = DateTime.UtcNow, OperationType = TipoOperacaoFinanceira.Compra, Quantity = 10m, UnitPrice = 10m, GrossAmount = 100m, RawJson = "{}", UsuarioInclusao = "teste" });
        context.EstimativasPosicaoCarteira.Add(new EstimativaPosicaoCarteira { Id = 1, CargaFinanceiraId = 1, AtivoFinanceiroId = 2, RawJson = "{}", UsuarioInclusao = "teste" });
        context.RendimentosInvestimento.Add(new RendimentoInvestimento { Id = 1, AssetId = 2, IncomeType = "Dividendo", Source = "Teste", Fonte = "Teste", RawJson = "{}", UsuarioInclusao = "teste" });
        await context.SaveChangesAsync();

        var repair = new FinancasDataRepairService(context, NullLogger<FinancasDataRepairService>.Instance);
        await repair.RepararAsync();

        Assert.All(context.CarteirasAtivosFinanceiros.IgnoreQueryFilters(), x => Assert.Equal(1, x.AtivoFinanceiroId));
        Assert.All(context.OperacoesB3.IgnoreQueryFilters(), x => Assert.Equal(1, x.AssetId));
        Assert.All(context.TransacoesFinanceiras.IgnoreQueryFilters(), x => Assert.Equal(1, x.AssetId));
        Assert.All(context.EstimativasPosicaoCarteira.IgnoreQueryFilters(), x => Assert.Equal(1, x.AtivoFinanceiroId));
        Assert.All(context.RendimentosInvestimento.IgnoreQueryFilters(), x => Assert.Equal(1, x.AssetId));
        Assert.Equal(12m, context.CotacoesAtivosFinanceiros.Single(x => x.AtivoFinanceiroId == 1 && x.DataExclusao == null).PrecoBRL);
        Assert.Contains(context.PrecosHistoricosAtivosFinanceiros, x => x.AtivoFinanceiroId == 1 && x.Date == new DateTime(2026, 1, 2));
        Assert.Equal("5", context.Configuracoes.Single(x => x.Chave == "ReparoAtivosVersao").Valor);
    }

    [Fact]
    public async Task ReparoDeveReclassificarDocumentKindDesconhecido()
    {
        await using var context = CriarContexto();
        context.DocumentosFinanceiros.AddRange(
            new DocumentoFinanceiro { Id = 1, FileName = "Binance-Histórico-de-Transações-202606051619(UTC--3)_f8ea955c.xlsx", DocumentKind = TipoDocumentoFinanceiro.Desconhecido, UsuarioInclusao = "teste" },
            new DocumentoFinanceiro { Id = 2, FileName = "movimentacaobinance.csv", DocumentKind = TipoDocumentoFinanceiro.Desconhecido, UsuarioInclusao = "teste" },
            new DocumentoFinanceiro { Id = 3, FileName = "Binance-Histórico-de-Ordens-Spot-202606051616(UTC--3)_71523b31.xlsx", DocumentKind = TipoDocumentoFinanceiro.BinanceSpotOrders, UsuarioInclusao = "teste" });
        await context.SaveChangesAsync();

        var repair = new FinancasDataRepairService(context, NullLogger<FinancasDataRepairService>.Instance);
        await repair.RepararAsync();

        // O export .xlsx "Histórico de Transações" deixa de ser Desconhecido e vira ledger (BinanceTransactions)
        // → o netting passa a enxergá-lo (causa-raiz do BTC subcontado).
        Assert.Equal(TipoDocumentoFinanceiro.BinanceTransactions, context.DocumentosFinanceiros.Single(x => x.Id == 1).DocumentKind);
        // CSV antigo também é classificado (CsvBinance) — fica como fallback do ledger.
        Assert.Equal(TipoDocumentoFinanceiro.CsvBinance, context.DocumentosFinanceiros.Single(x => x.Id == 2).DocumentKind);
        // Documento que já tinha kind definido não é alterado.
        Assert.Equal(TipoDocumentoFinanceiro.BinanceSpotOrders, context.DocumentosFinanceiros.Single(x => x.Id == 3).DocumentKind);
    }

    [Fact]
    public async Task ReparoDeveReclassificarFiiGravadoComoEtf()
    {
        await using var context = CriarContexto();
        // FIIs gravados erroneamente como ETF (causa do IR a 15% em vez de 20%).
        context.AtivosFinanceiros.AddRange(
            new AtivoFinanceiro { Id = 1, Chave = "AFHI11", Sigla = "AFHI11", Nome = "FII AFHI CRI CI", Classe = ClasseAtivo.ETF, Mercado = "B3", UsuarioInclusao = "teste" },
            new AtivoFinanceiro { Id = 2, Chave = "CPTS11", Sigla = "CPTS11", Nome = "FII CAPITANIA CI", Classe = ClasseAtivo.ETF, Mercado = "B3", UsuarioInclusao = "teste" },
            // GOLD11 é ETF de verdade (TREND OURO) → permanece ETF.
            new AtivoFinanceiro { Id = 3, Chave = "GOLD11", Sigla = "GOLD11", Nome = "TREND OURO CI", Classe = ClasseAtivo.ETF, Mercado = "B3", UsuarioInclusao = "teste" },
            // Cripto não pode ser tocado pelo reparo B3.
            new AtivoFinanceiro { Id = 4, Chave = "BTC", Sigla = "BTC", Nome = "Bitcoin", Classe = ClasseAtivo.Cripto, EhCripto = true, Mercado = "Binance", UsuarioInclusao = "teste" });
        await context.SaveChangesAsync();

        var repair = new FinancasDataRepairService(context, NullLogger<FinancasDataRepairService>.Instance);
        await repair.RepararAsync();

        Assert.Equal(ClasseAtivo.FII, context.AtivosFinanceiros.Single(x => x.Id == 1).Classe);
        Assert.Equal(ClasseAtivo.FII, context.AtivosFinanceiros.Single(x => x.Id == 2).Classe);
        Assert.Equal(ClasseAtivo.ETF, context.AtivosFinanceiros.Single(x => x.Id == 3).Classe);
        Assert.Equal(ClasseAtivo.Cripto, context.AtivosFinanceiros.IgnoreQueryFilters().Single(x => x.Id == 4).Classe);
    }

    [Fact]
    public async Task ProventosDashboardDeveAgruparRecebidoPorFonte()
    {
        var fii = new AtivoFinanceiro { Id = 1, Chave = "DEVA11", Sigla = "DEVA11", Nome = "FII DEVANT", Classe = ClasseAtivo.FII, Mercado = "B3" };
        var acao = new AtivoFinanceiro { Id = 2, Chave = "BBAS3", Sigla = "BBAS3", Nome = "Banco do Brasil", Classe = ClasseAtivo.Acao, Mercado = "B3" };
        var cripto = new AtivoFinanceiro { Id = 3, Chave = "BTC", Sigla = "BTC", Nome = "Bitcoin", Classe = ClasseAtivo.Cripto, EhCripto = true, Mercado = "Binance" };

        var hoje = DateTime.UtcNow.Date;
        var proventos = new List<RendimentoInvestimento>
        {
            Provento(fii, 100m, "B3 Extrato", hoje.AddMonths(-1)),
            Provento(acao, 40m, "InformeIR2025", hoje.AddMonths(-2)),
            Provento(acao, 60m, "B3 Extrato+Brapi", hoje.AddMonths(-3)),  // fonte combinada → primeira manda (B3)
            Provento(cripto, 10m, "Binance", hoje.AddMonths(-1)),
            Provento(fii, 999m, "Brapi", hoje.AddYears(-2))               // fora dos 12M → ignorado
        };

        var repo = new Mock<IFinancasRepository>();
        repo.Setup(r => r.BuscarProventosPorPeriodoAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>())).ReturnsAsync(proventos);
        var service = CriarService(repo.Object);

        var dto = await service.ObterProventosDashboardAsync();

        var porFonte = dto.PorFonte.ToDictionary(x => x.Fonte, x => x.Valor);
        Assert.Equal(160m, porFonte["B3 Extrato"]);   // 100 (FII) + 60 (combinada B3+Brapi)
        Assert.Equal(40m, porFonte["Informe IR"]);
        Assert.Equal(10m, porFonte["Binance Earn"]);
        Assert.DoesNotContain("Brapi", porFonte.Keys); // a linha pura Brapi está fora dos 12M
        // Soma dos percentuais ~100 e ordenado por valor desc.
        Assert.Equal("B3 Extrato", dto.PorFonte[0].Fonte);
        Assert.Equal(100m, dto.PorFonte.Sum(x => x.Percentual), 0);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task CarteirasDashboardSinalizaCriptoParcialmenteReconciliadaQuandoHaPosicaoCripto(bool comCripto)
    {
        var acao = new AtivoFinanceiro { Id = 1, Chave = "BBAS3", Sigla = "BBAS3", Nome = "BB", Classe = ClasseAtivo.Acao, Mercado = "B3" };
        var btc = new AtivoFinanceiro { Id = 2, Chave = "BTC", Sigla = "BTC", Nome = "Bitcoin", Classe = ClasseAtivo.Cripto, EhCripto = true, Mercado = "Binance" };

        var transacoes = new List<TransacaoFinanceira> { Compra(acao, 10m, 20m, new DateTime(2026, 1, 10)) };
        if (comCripto)
            transacoes.Add(Compra(btc, 0.05m, 200000m, new DateTime(2026, 1, 10)));

        var repo = new Mock<IFinancasRepository>();
        repo.Setup(r => r.BuscarTodasTransacoesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(transacoes);
        repo.Setup(r => r.BuscarCotacoesAtuaisAsync(It.IsAny<CancellationToken>())).ReturnsAsync([]);
        repo.Setup(r => r.BuscarCarteirasComAtivosAsync(It.IsAny<CancellationToken>())).ReturnsAsync([]);
        var service = CriarService(repo.Object);

        var dto = await service.ObterCarteirasDashboardAsync();

        Assert.Equal(comCripto, dto.CriptoParcialmenteReconciliada);
    }

    [Fact]
    public async Task ReconciliacaoDashboardDeveResumirAjustesEValorNaVariacao()
    {
        var bbas = new AtivoFinanceiro { Id = 1, Chave = "BBAS3", Sigla = "BBAS3", Nome = "Banco do Brasil", Classe = ClasseAtivo.Acao, Mercado = "B3" };
        var cpts = new AtivoFinanceiro { Id = 2, Chave = "CPTS11", Sigla = "CPTS11", Nome = "FII Capitania", Classe = ClasseAtivo.FII, Mercado = "B3" };
        var variacao = new AtivoFinanceiro { Id = 9, Chave = "VARIACAO", Sigla = "VARIACAO", Nome = "Ajuste de Reconciliação", Classe = ClasseAtivo.Outro, Mercado = "B3" };
        var hoje = DateTime.UtcNow.Date;

        var transacoes = new List<TransacaoFinanceira>
        {
            // BBAS3: alvo 100, calculado 80 → faltam 20 cotas (Compra) ao PM 25 = R$ 500.
            Reconciliacao(bbas, TipoOperacaoFinanceira.Compra, 20m, 25m, hoje, alvo: 100m, calculado: 80m),
            // CPTS11: alvo 0, calculado 50 → sobram 50 (Venda) ao PM 9 = R$ 450 (fantasma zerado).
            Reconciliacao(cpts, TipoOperacaoFinanceira.Venda, 50m, 9m, hoje, alvo: 0m, calculado: 50m),
            // Contrapartidas no VARIACAO (inverso): BBAS3 deu compra → VARIACAO vende 500; CPTS11 venda → VARIACAO compra 450.
            Reconciliacao(variacao, TipoOperacaoFinanceira.Venda, 500m, 1m, hoje, alvo: 0m, calculado: 0m),
            Reconciliacao(variacao, TipoOperacaoFinanceira.Compra, 450m, 1m, hoje, alvo: 0m, calculado: 0m)
        };

        var repo = new Mock<IFinancasRepository>();
        repo.Setup(r => r.BuscarTodasTransacoesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(transacoes);
        var service = CriarService(repo.Object);

        var dto = await service.ObterReconciliacaoDashboardAsync();

        Assert.True(dto.TemDados);
        Assert.Equal(2, dto.NumeroAjustes); // só os ativos reais (VARIACAO não conta)
        Assert.Equal(-50m, dto.ValorTotalVariacao); // VARIACAO: Compra 450 − Venda 500
        Assert.Equal(100m, dto.AlvoTotalCustodia);  // 100 (BBAS3) + 0 (CPTS11)
        Assert.Equal(130m, dto.CalculadoTotal);     // 80 + 50
        // Maior ajuste em valor primeiro: BBAS3 (R$500) antes de CPTS11 (R$450).
        Assert.Equal("BBAS3", dto.PrincipaisAtivos[0].Ticker);
        Assert.Equal(20m, dto.PrincipaisAtivos[0].Diferenca);
        Assert.Equal(500m, dto.PrincipaisAtivos[0].ValorAjuste);
        Assert.DoesNotContain(dto.PrincipaisAtivos, x => x.Ticker == "VARIACAO");
    }

    [Fact]
    public async Task ReconciliacaoDashboardSemAjustesDevolveVazio()
    {
        var repo = new Mock<IFinancasRepository>();
        repo.Setup(r => r.BuscarTodasTransacoesAsync(It.IsAny<CancellationToken>())).ReturnsAsync([]);
        var service = CriarService(repo.Object);

        var dto = await service.ObterReconciliacaoDashboardAsync();

        Assert.False(dto.TemDados);
        Assert.Equal(0, dto.NumeroAjustes);
        Assert.Empty(dto.PrincipaisAtivos);
    }

    [Fact]
    public async Task PosicoesDashboardDeveComporValorPorFonteEDiferencaVsB3()
    {
        // BBAS3: tem cotação Brapi (ao vivo) E fechamento B3 → valora pela Brapi, mas mostra dif vs B3.
        var bbas = new AtivoFinanceiro { Id = 1, Chave = "BBAS3", Sigla = "BBAS3", Nome = "BB", Classe = ClasseAtivo.Acao, Mercado = "B3" };
        // DEVA11: só tem fechamento B3Custódia → valora por fechamento B3, dif = 0.
        var deva = new AtivoFinanceiro { Id = 2, Chave = "DEVA11", Sigla = "DEVA11", Nome = "FII DEVANT", Classe = ClasseAtivo.FII, Mercado = "B3" };
        // PETR4: sem cotação alguma → cai no custo (fallback).
        var petr = new AtivoFinanceiro { Id = 3, Chave = "PETR4", Sigla = "PETR4", Nome = "Petrobras", Classe = ClasseAtivo.Acao, Mercado = "B3" };

        var transacoes = new List<TransacaoFinanceira>
        {
            Compra(bbas, 100m, 20m, new DateTime(2026, 1, 10)), // custo 2000
            Compra(deva, 10m, 100m, new DateTime(2026, 1, 10)), // custo 1000
            Compra(petr, 50m, 30m, new DateTime(2026, 1, 10))   // custo 1500 (fallback)
        };
        var agora = DateTime.UtcNow;
        var cotacoes = new List<CotacaoAtivoFinanceiro>
        {
            new() { AtivoFinanceiroId = bbas.Id, AtivoFinanceiro = bbas, Provedor = ProvedorCotacao.Brapi, Simbolo = "BBAS3", PrecoBRL = 25m, Preco = 25m, ConsultadoEm = agora, RawJson = "{}" },
            new() { AtivoFinanceiroId = bbas.Id, AtivoFinanceiro = bbas, Provedor = ProvedorCotacao.B3Custodia, Simbolo = "BBAS3", PrecoBRL = 24m, Preco = 24m, ConsultadoEm = agora.AddDays(-1), RawJson = "{}" },
            new() { AtivoFinanceiroId = deva.Id, AtivoFinanceiro = deva, Provedor = ProvedorCotacao.B3Custodia, Simbolo = "DEVA11", PrecoBRL = 110m, Preco = 110m, ConsultadoEm = agora, RawJson = "{}" }
        };

        var repo = new Mock<IFinancasRepository>();
        repo.Setup(r => r.BuscarTodasTransacoesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(transacoes);
        repo.Setup(r => r.BuscarCotacoesAtuaisAsync(It.IsAny<CancellationToken>())).ReturnsAsync(cotacoes);
        var service = CriarService(repo.Object);

        var dto = await service.ObterPosicoesDashboardAsync();

        // Composição: BBAS3 (100×25=2500) cotação; DEVA11 (10×110=1100) fechamento B3; PETR4 (1500) custo.
        Assert.Equal(2500m, dto.Composicao.ComCotacao);
        Assert.Equal(1100m, dto.Composicao.ComFechamentoB3);
        Assert.Equal(1500m, dto.Composicao.ComCusto);
        Assert.Equal(5100m, dto.Composicao.Total);

        var bbasDto = dto.Posicoes.Single(x => x.Ticker == "BBAS3");
        Assert.Equal("Cotação (Brapi)", bbasDto.FontePreco);
        Assert.Equal(24m, bbasDto.PrecoB3);
        Assert.Equal(100m, bbasDto.DiferencaB3); // 2500 − 100×24 = 100

        var petrDto = dto.Posicoes.Single(x => x.Ticker == "PETR4");
        Assert.Equal("Custo", petrDto.FontePreco);
        Assert.Null(petrDto.PrecoB3);
        Assert.Null(petrDto.DiferencaB3);
    }

    [Fact]
    public async Task ImportacaoDashboardDeveAgruparArquivosPorFonteEStatus()
    {
        // 2 extratos B3 (jan e mar/2026, com fev faltando → lacuna), 1 nota Nubank parcial, 1 Binance falho com alerta,
        // 1 informe IR. RawMetadata do B3 carrega referencePeriod; demais usam ReferenceYear.
        string Meta(string periodo) => System.Text.Json.JsonSerializer.Serialize(new { referencePeriod = periodo });
        var docs = new List<RastreabilidadeDocumentoProjecao>
        {
            new(1, "b3-2026-01.xlsx", TipoDocumentoFinanceiro.ExtratoConsolidadoB3, StatusParseDocumentoFinanceiro.Processado, StatusDocumentoFinanceiro.Processado, 2026, Meta("2026-01"), LinhasLidas: 120, Abas: 6, Alertas: 0),
            new(2, "b3-2026-03.xlsx", TipoDocumentoFinanceiro.ExtratoConsolidadoB3, StatusParseDocumentoFinanceiro.Processado, StatusDocumentoFinanceiro.Processado, 2026, Meta("2026-03"), LinhasLidas: 130, Abas: 6, Alertas: 0),
            new(3, "nota-nubank.pdf", TipoDocumentoFinanceiro.ExtratoInvestimentosNubank, StatusParseDocumentoFinanceiro.ParcialmenteProcessado, StatusDocumentoFinanceiro.ParcialmenteProcessado, 2025, "{}", LinhasLidas: 8, Abas: 0, Alertas: 0),
            new(4, "binance.xlsx", TipoDocumentoFinanceiro.BinanceTransactions, StatusParseDocumentoFinanceiro.Falhou, StatusDocumentoFinanceiro.Falhou, 2024, "{}", LinhasLidas: 0, Abas: 0, Alertas: 2),
            new(5, "informe-ir.pdf", TipoDocumentoFinanceiro.InformeRendimentos, StatusParseDocumentoFinanceiro.Processado, StatusDocumentoFinanceiro.Processado, 2025, "{}", LinhasLidas: 15, Abas: 0, Alertas: 0)
        };

        var repo = new Mock<IFinancasRepository>();
        repo.Setup(r => r.ObterCargaMaisRecenteAsync(It.IsAny<CancellationToken>())).ReturnsAsync((CargaFinanceira?)null);
        repo.Setup(r => r.BuscarDocumentosMonitoradosAsync(It.IsAny<CancellationToken>())).ReturnsAsync([]);
        repo.Setup(r => r.ObterUltimaImportacaoArquivoAsync(It.IsAny<CancellationToken>())).ReturnsAsync((ImportacaoFinanceiraArquivo?)null);
        repo.Setup(r => r.BuscarCotacoesAtuaisAsync(It.IsAny<CancellationToken>())).ReturnsAsync([]);
        repo.Setup(r => r.BuscarRastreabilidadeDocumentosAsync(It.IsAny<CancellationToken>())).ReturnsAsync(docs);
        var service = CriarService(repo.Object);

        var dto = await service.ObterImportacaoDashboardAsync();

        var fontes = dto.RastreabilidadeFontes.ToDictionary(x => x.Fonte);
        Assert.Equal(4, dto.RastreabilidadeFontes.Count); // B3, Nubank, Binance, Informe IR
        // B3 vem primeiro (ordem de fonte) e soma 2 docs/250 linhas processados.
        Assert.Equal("B3", dto.RastreabilidadeFontes[0].Fonte);
        Assert.Equal(2, fontes["B3"].Documentos);
        Assert.Equal(2, fontes["B3"].Processados);
        Assert.Equal(250, fontes["B3"].LinhasLidas);
        // Nubank: 1 parcial.
        Assert.Equal(1, fontes["Nubank"].Parciais);
        Assert.Equal(0, fontes["Nubank"].Processados);
        // Binance: 1 falho com 2 alertas.
        Assert.Equal(1, fontes["Binance"].Falhos);
        Assert.Equal(2, fontes["Binance"].Alertas);
        // Período do extrato B3 sai como referencePeriod; nota Nubank cai no ano.
        Assert.Contains(fontes["B3"].Itens, i => i.Periodo == "2026-03" && i.Tipo == "B3 Extrato");
        Assert.Contains(fontes["Nubank"].Itens, i => i.Periodo == "2025");

        // Custódia B3: última posição = 2026-03; fev/2026 faltando entre jan e mar.
        Assert.NotNull(dto.RastreabilidadeB3);
        Assert.Equal("2026-03", dto.RastreabilidadeB3!.UltimoPeriodoPosicao);
        Assert.Equal("2026-01", dto.RastreabilidadeB3.PrimeiroPeriodoExtrato);
        Assert.Equal(2, dto.RastreabilidadeB3.ExtratosImportados);
        Assert.Equal(["2026-02"], dto.RastreabilidadeB3.MesesFaltantes);
    }

    [Fact]
    public async Task EvolucaoDeveExporCustoAcumuladoQueCresceNasComprasEReduzNasVendas()
    {
        var acao = new AtivoFinanceiro { Id = 1, Chave = "BBAS3", Sigla = "BBAS3", Nome = "BB", Classe = ClasseAtivo.Acao, Mercado = "B3" };
        var hoje = DateTime.UtcNow.Date;
        // Dentro da janela de 1 ano do gráfico. Compra 2.000, +1.000 num dia seguinte, venda de 50 cotas (−1.000).
        var compra1 = hoje.AddMonths(-6);   // 100 × 20 = 2.000
        var compra2 = hoje.AddMonths(-4);   //  50 × 20 = 1.000 (acumulado 3.000)
        var venda = hoje.AddMonths(-2);     //  50 × 20 = 1.000 a menos (acumulado 2.000)

        var transacoes = new List<TransacaoFinanceira>
        {
            Compra(acao, 100m, 20m, compra1),
            Compra(acao, 50m, 20m, compra2),
            Venda(acao, 50m, 20m, venda)
        };

        var repo = new Mock<IFinancasRepository>();
        repo.Setup(r => r.BuscarTodasTransacoesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(transacoes);
        repo.Setup(r => r.BuscarHistoricoPrecosAsync(It.IsAny<DateTime>(), It.IsAny<CancellationToken>())).ReturnsAsync([]);
        repo.Setup(r => r.BuscarCarteirasComAtivosAsync(It.IsAny<CancellationToken>())).ReturnsAsync([]);
        repo.Setup(r => r.BuscarCotacoesAtuaisAsync(It.IsAny<CancellationToken>())).ReturnsAsync([]);
        var service = CriarService(repo.Object);

        var dto = await service.ObterEvolucaoPatrimonioAsync();

        // Série alinhada ao eixo de datas, monotônica não-decrescente até a venda.
        Assert.Equal(dto.Datas.Count, dto.CustoAcumulado.Count);
        decimal Em(DateTime data)
        {
            var idx = dto.Datas.ToList().FindIndex(d => d == data.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture));
            return dto.CustoAcumulado[idx];
        }

        Assert.Equal(0m, dto.CustoAcumulado[0]);            // nada antes da 1ª compra
        Assert.Equal(2000m, Em(compra1));                   // sobe na compra
        Assert.Equal(3000m, Em(compra2));                   // continua subindo no aporte seguinte
        Assert.Equal(2000m, Em(venda));                     // reduz na venda
        Assert.Equal(2000m, dto.CustoAcumulado[^1]);        // forward-fill até hoje
        // Não-decrescente entre 1ª compra e véspera da venda.
        var iC1 = dto.Datas.ToList().FindIndex(d => d == compra1.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture));
        var iV = dto.Datas.ToList().FindIndex(d => d == venda.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture));
        for (var i = iC1 + 1; i < iV; i++)
            Assert.True(dto.CustoAcumulado[i] >= dto.CustoAcumulado[i - 1]);
    }

    [Fact]
    public async Task EvolucaoDeveSomarComprasAnterioresAoInicioNaBaseDoCustoAcumulado()
    {
        var acao = new AtivoFinanceiro { Id = 1, Chave = "BBAS3", Sigla = "BBAS3", Nome = "BB", Classe = ClasseAtivo.Acao, Mercado = "B3" };
        var hoje = DateTime.UtcNow.Date;
        // Compra há 2 anos (antes da janela de 1 ano) deve entrar na base (1º dia) do custo acumulado.
        var transacoes = new List<TransacaoFinanceira>
        {
            Compra(acao, 10m, 50m, hoje.AddYears(-2)) // 500
        };

        var repo = new Mock<IFinancasRepository>();
        repo.Setup(r => r.BuscarTodasTransacoesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(transacoes);
        repo.Setup(r => r.BuscarHistoricoPrecosAsync(It.IsAny<DateTime>(), It.IsAny<CancellationToken>())).ReturnsAsync([]);
        repo.Setup(r => r.BuscarCarteirasComAtivosAsync(It.IsAny<CancellationToken>())).ReturnsAsync([]);
        repo.Setup(r => r.BuscarCotacoesAtuaisAsync(It.IsAny<CancellationToken>())).ReturnsAsync([]);
        var service = CriarService(repo.Object);

        var dto = await service.ObterEvolucaoPatrimonioAsync();

        Assert.Equal(500m, dto.CustoAcumulado[0]);
        Assert.Equal(500m, dto.CustoAcumulado[^1]);
    }

    private static TransacaoFinanceira Reconciliacao(AtivoFinanceiro ativo, TipoOperacaoFinanceira tipo, decimal qtd, decimal preco, DateTime data, decimal alvo, decimal calculado)
        => new()
        {
            AssetId = ativo.Id,
            Asset = ativo,
            OperationType = tipo,
            Quantity = qtd,
            UnitPrice = preco,
            GrossAmount = qtd * preco,
            Date = data,
            Fonte = "Reconciliação",
            IsCanonical = true,
            RawJson = System.Text.Json.JsonSerializer.Serialize(new { Alvo = alvo, Calculado = calculado, PrecoMedio = preco })
        };

    private static RendimentoInvestimento Provento(AtivoFinanceiro ativo, decimal valor, string fonte, DateTime pagamento)
        => new()
        {
            AssetId = ativo.Id,
            Asset = ativo,
            Amount = valor,
            TaxWithheld = 0m,
            PaymentDate = pagamento,
            IncomeType = "Rendimento",
            Source = fonte,
            Fonte = fonte,
            RawJson = "{}"
        };

    private static FinancasAppService CriarService(IFinancasRepository repo)
    {
        var uow = new Mock<IUnitOfWork>();
        uow.SetupGet(x => x.Financas).Returns(repo);

        var importador = new Mock<IFinancasImportador>();
        importador.Setup(x => x.GarantirCargaInicialAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var marketData = new Mock<IFinancasMarketDataService>();
        var projection = new Mock<IPosicaoAtivoProjectionService>();
        projection.Setup(x => x.RecalcularAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        var log = new Mock<ILogAppService>();
        var mensagem = new Mock<IMensagemAppService>();
        var execution = new Mock<IExecutionContext>();
        execution.SetupGet(x => x.Usuario).Returns("teste");

        return new FinancasAppService(uow.Object, importador.Object, marketData.Object, projection.Object, log.Object, mensagem.Object, execution.Object);
    }

    private static AppDbContext CriarContexto()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        var execution = new Mock<IExecutionContext>();
        execution.SetupGet(x => x.Usuario).Returns("teste");
        return new AppDbContext(options, execution.Object);
    }

    private static TransacaoFinanceira Compra(AtivoFinanceiro ativo, decimal quantidade, decimal preco, DateTime data)
        => Transacao(ativo, TipoOperacaoFinanceira.Compra, quantidade, preco, data);

    private static TransacaoFinanceira Venda(AtivoFinanceiro ativo, decimal quantidade, decimal preco, DateTime data)
        => Transacao(ativo, TipoOperacaoFinanceira.Venda, quantidade, preco, data);

    private static TransacaoFinanceira Transacao(AtivoFinanceiro ativo, TipoOperacaoFinanceira tipo, decimal quantidade, decimal preco, DateTime data)
        => new()
        {
            AssetId = ativo.Id,
            Asset = ativo,
            OperationType = tipo,
            Quantity = quantidade,
            UnitPrice = preco,
            GrossAmount = quantidade * preco,
            Date = data,
            IsCanonical = true,
            RawJson = "{}"
        };

    private static CarteiraFinanceira Carteira(int id, string nome, string tipo, AtivoFinanceiro ativo)
        => new()
        {
            Id = id,
            Nome = nome,
            Slug = nome.ToLowerInvariant().Replace(' ', '-'),
            Tipo = tipo,
            Ativos =
            [
                new CarteiraAtivoFinanceiro
                {
                    CarteiraFinanceiraId = id,
                    AtivoFinanceiroId = ativo.Id,
                    AtivoFinanceiro = ativo
                }
            ]
        };
}
