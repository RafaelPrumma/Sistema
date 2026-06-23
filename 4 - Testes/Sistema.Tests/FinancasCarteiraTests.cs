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
            AssetKey = "ITUB4",
            Ticker = "ITUB4",
            Name = "Itau",
            AssetClass = ClasseAtivo.Acao,
            Market = "B3"
        };
        var criptoZerada = new AtivoFinanceiro
        {
            Id = 2,
            AssetKey = "MATIC",
            Ticker = "MATIC",
            Name = "MATIC",
            AssetClass = ClasseAtivo.Cripto,
            IsCrypto = true,
            Market = "Binance"
        };
        var fiiSemCotacao = new AtivoFinanceiro
        {
            Id = 3,
            AssetKey = "DEVA11",
            Ticker = "DEVA11",
            Name = "FII DEVANT CI",
            AssetClass = ClasseAtivo.FII,
            Market = "B3"
        };
        var maticDust = new AtivoFinanceiro
        {
            Id = 4,
            AssetKey = "MATIC",
            Ticker = "MATIC",
            Name = "MATIC",
            AssetClass = ClasseAtivo.Cripto,
            IsCrypto = true,
            Market = "Binance"
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
                Symbol = "ITUB4",
                PriceBRL = 25m,
                Price = 25m,
                ChangePercent = 1.5m,
                RetrievedAt = DateTime.UtcNow,
                RawJson = "{}"
            },
            new()
            {
                AtivoFinanceiroId = maticDust.Id,
                AtivoFinanceiro = maticDust,
                Provedor = ProvedorCotacao.Binance,
                Symbol = "MATIC",
                PriceBRL = 2m,
                Price = 2m,
                RetrievedAt = DateTime.UtcNow,
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
            AssetKey = "BBDC4",
            Ticker = "BBDC4",
            Name = "BRADESCO PN",
            AssetClass = ClasseAtivo.Acao,
            Market = "B3",
            UsuarioInclusao = "teste"
        };
        var fragmento = new AtivoFinanceiro
        {
            Id = 2,
            AssetKey = "BRADESCO PN EJ N1",
            Name = "BRADESCO PN EJ N1",
            AssetClass = ClasseAtivo.Acao,
            Market = "B3",
            DataExclusao = DateTime.UtcNow,
            UsuarioInclusao = "teste"
        };
        var carteira = new CarteiraFinanceira { Id = 1, Nome = "Bancario", Slug = "bancario", UsuarioInclusao = "teste" };
        context.AddRange(carga, canonico, fragmento, carteira);
        context.CarteirasAtivosFinanceiros.Add(new CarteiraAtivoFinanceiro { Id = 1, CarteiraFinanceiraId = 1, AtivoFinanceiroId = 2, UsuarioInclusao = "teste" });
        context.CotacoesAtivosFinanceiros.AddRange(
            new CotacaoAtivoFinanceiro { Id = 1, AtivoFinanceiroId = 1, Provedor = ProvedorCotacao.Brapi, Symbol = "BBDC4", Price = 10m, PriceBRL = 10m, RetrievedAt = DateTime.UtcNow.AddDays(-1), RawJson = "{}", UsuarioInclusao = "teste" },
            new CotacaoAtivoFinanceiro { Id = 2, AtivoFinanceiroId = 2, Provedor = ProvedorCotacao.Brapi, Symbol = "BRADESCO PN", Price = 12m, PriceBRL = 12m, RetrievedAt = DateTime.UtcNow, RawJson = "{}", UsuarioInclusao = "teste" });
        context.PrecosHistoricosAtivosFinanceiros.AddRange(
            new PrecoHistoricoAtivoFinanceiro { Id = 1, AtivoFinanceiroId = 1, Provedor = ProvedorCotacao.Brapi, Symbol = "BBDC4", Date = new DateTime(2026, 1, 1), Interval = "1d", Close = 10m, CloseBRL = 10m, RawJson = "{}", UsuarioInclusao = "teste" },
            new PrecoHistoricoAtivoFinanceiro { Id = 2, AtivoFinanceiroId = 2, Provedor = ProvedorCotacao.Brapi, Symbol = "BRADESCO PN", Date = new DateTime(2026, 1, 2), Interval = "1d", Close = 11m, CloseBRL = 11m, RawJson = "{}", UsuarioInclusao = "teste" });
        context.OperacoesB3.Add(new OperacaoB3 { Id = 1, CargaFinanceiraId = 1, AssetId = 2, OriginalAssetName = "BRADESCO PN EJ N1", RawJson = "{}", UsuarioInclusao = "teste" });
        context.TransacoesFinanceiras.Add(new TransacaoFinanceira { Id = 1, AssetId = 2, Date = DateTime.UtcNow, OperationType = TipoOperacaoFinanceira.Compra, Quantity = 10m, UnitPrice = 10m, GrossAmount = 100m, RawJson = "{}", UsuarioInclusao = "teste" });
        context.EstimativasPosicaoCarteira.Add(new EstimativaPosicaoCarteira { Id = 1, CargaFinanceiraId = 1, AssetId = 2, RawJson = "{}", UsuarioInclusao = "teste" });
        context.RendimentosInvestimento.Add(new RendimentoInvestimento { Id = 1, AssetId = 2, IncomeType = "Dividendo", Source = "Teste", Fonte = "Teste", RawJson = "{}", UsuarioInclusao = "teste" });
        await context.SaveChangesAsync();

        var repair = new FinancasDataRepairService(context, NullLogger<FinancasDataRepairService>.Instance);
        await repair.RepararAsync();

        Assert.All(context.CarteirasAtivosFinanceiros.IgnoreQueryFilters(), x => Assert.Equal(1, x.AtivoFinanceiroId));
        Assert.All(context.OperacoesB3.IgnoreQueryFilters(), x => Assert.Equal(1, x.AssetId));
        Assert.All(context.TransacoesFinanceiras.IgnoreQueryFilters(), x => Assert.Equal(1, x.AssetId));
        Assert.All(context.EstimativasPosicaoCarteira.IgnoreQueryFilters(), x => Assert.Equal(1, x.AssetId));
        Assert.All(context.RendimentosInvestimento.IgnoreQueryFilters(), x => Assert.Equal(1, x.AssetId));
        Assert.Equal(12m, context.CotacoesAtivosFinanceiros.Single(x => x.AtivoFinanceiroId == 1 && x.DataExclusao == null).PriceBRL);
        Assert.Contains(context.PrecosHistoricosAtivosFinanceiros, x => x.AtivoFinanceiroId == 1 && x.Date == new DateTime(2026, 1, 2));
        Assert.Equal("4", context.Configuracoes.Single(x => x.Chave == "ReparoAtivosVersao").Valor);
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
    public async Task ProventosDashboardDeveAgruparRecebidoPorFonte()
    {
        var fii = new AtivoFinanceiro { Id = 1, AssetKey = "DEVA11", Ticker = "DEVA11", Name = "FII DEVANT", AssetClass = ClasseAtivo.FII, Market = "B3" };
        var acao = new AtivoFinanceiro { Id = 2, AssetKey = "BBAS3", Ticker = "BBAS3", Name = "Banco do Brasil", AssetClass = ClasseAtivo.Acao, Market = "B3" };
        var cripto = new AtivoFinanceiro { Id = 3, AssetKey = "BTC", Ticker = "BTC", Name = "Bitcoin", AssetClass = ClasseAtivo.Cripto, IsCrypto = true, Market = "Binance" };

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
        Assert.Equal("B3 Extrato", dto.PorFonte.First().Fonte);
        Assert.Equal(100m, dto.PorFonte.Sum(x => x.Percentual), 0);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task CarteirasDashboardSinalizaCriptoParcialmenteReconciliadaQuandoHaPosicaoCripto(bool comCripto)
    {
        var acao = new AtivoFinanceiro { Id = 1, AssetKey = "BBAS3", Ticker = "BBAS3", Name = "BB", AssetClass = ClasseAtivo.Acao, Market = "B3" };
        var btc = new AtivoFinanceiro { Id = 2, AssetKey = "BTC", Ticker = "BTC", Name = "Bitcoin", AssetClass = ClasseAtivo.Cripto, IsCrypto = true, Market = "Binance" };

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
        var bbas = new AtivoFinanceiro { Id = 1, AssetKey = "BBAS3", Ticker = "BBAS3", Name = "Banco do Brasil", AssetClass = ClasseAtivo.Acao, Market = "B3" };
        var cpts = new AtivoFinanceiro { Id = 2, AssetKey = "CPTS11", Ticker = "CPTS11", Name = "FII Capitania", AssetClass = ClasseAtivo.FII, Market = "B3" };
        var variacao = new AtivoFinanceiro { Id = 9, AssetKey = "VARIACAO", Ticker = "VARIACAO", Name = "Ajuste de Reconciliação", AssetClass = ClasseAtivo.Outro, Market = "B3" };
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
        Assert.Equal("BBAS3", dto.PrincipaisAtivos.First().Ticker);
        Assert.Equal(20m, dto.PrincipaisAtivos.First().Diferenca);
        Assert.Equal(500m, dto.PrincipaisAtivos.First().ValorAjuste);
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
        var log = new Mock<ILogAppService>();
        var mensagem = new Mock<IMensagemAppService>();
        var execution = new Mock<IExecutionContext>();
        execution.SetupGet(x => x.Usuario).Returns("teste");

        return new FinancasAppService(uow.Object, importador.Object, marketData.Object, log.Object, mensagem.Object, execution.Object);
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
