using System.Globalization;
using System.Text.Json;
using Sistema.APP.DTOs;
using Sistema.APP.Services.Interfaces;
using Sistema.CORE.Common;
using Sistema.CORE.Entities;
using Sistema.CORE.Repositories.Interfaces;

namespace Sistema.APP.Services;

public class FinancasAppService(IUnitOfWork uow, IFinancasImportador importador, IFinancasMarketDataService marketData, ILogAppService log, IMensagemAppService mensagem, IExecutionContext execution) : IFinancasAppService
{
    private const decimal QuantidadeAbertaMinima = 0.000000001m;
    private const decimal ValorDustCriptoMaximoBrl = 10m;

    private readonly IUnitOfWork _uow = uow;
    private readonly IFinancasImportador _importador = importador;
    private readonly IFinancasMarketDataService _marketData = marketData;
    private readonly ILogAppService _log = log;
    private readonly IMensagemAppService _mensagem = mensagem;
    private readonly IExecutionContext _execution = execution;

    private string UsuarioAtual => string.IsNullOrWhiteSpace(_execution.Usuario) ? "sistema" : _execution.Usuario!;

    public Task PrepararDashboardAsync(CancellationToken cancellationToken = default)
        => _importador.GarantirCargaInicialAsync(cancellationToken);

    public async Task<FinancasPatrimonioDto> ObterPatrimonioDashboardAsync(CancellationToken cancellationToken = default)
    {
        var hoje = DateTime.UtcNow.Date;
        var inicio = hoje.AddYears(-1);
        var transacoes = await _uow.Financas.BuscarTodasTransacoesAsync(cancellationToken);
        var historico = await _uow.Financas.BuscarHistoricoPrecosAsync(inicio, cancellationToken);
        var carteiras = await _uow.Financas.BuscarCarteirasComAtivosAsync(cancellationToken);
        var cotacoes = await _uow.Financas.BuscarCotacoesAtuaisAsync(cancellationToken);
        var evolucao = CriarEvolucaoPatrimonio(transacoes, historico, carteiras, cotacoes, hoje, inicio);
        var posicoes = CalcularPosicoes(transacoes).Values.Where(p => p.Quantidade > QuantidadeAbertaMinima).ToList();
        var ativos = CriarAtivosCotadosDaTabela(posicoes, cotacoes).Where(EhAtivoCotadoVisivel).ToList();

        return new FinancasPatrimonioDto(
            ativos.Sum(x => x.ValorMercado),
            ativos.Sum(x => x.CustoEstimado),
            ativos.Sum(x => x.ResultadoNaoRealizado),
            evolucao);
    }

    public async Task<FinancasCarteirasDto> ObterCarteirasDashboardAsync(CancellationToken cancellationToken = default)
    {
        var transacoes = await _uow.Financas.BuscarTodasTransacoesAsync(cancellationToken);
        var cotacoes = await _uow.Financas.BuscarCotacoesAtuaisAsync(cancellationToken);
        var carteiras = await _uow.Financas.BuscarCarteirasComAtivosAsync(cancellationToken);
        var posicoes = CalcularPosicoes(transacoes).Values.Where(p => p.Quantidade > QuantidadeAbertaMinima).ToList();
        var ativos = CriarAtivosCotadosDaTabela(posicoes, cotacoes).Where(EhAtivoCotadoVisivel).ToList();
        var valorMercadoTotal = ativos.Sum(x => x.ValorMercado);

        return new FinancasCarteirasDto(CriarResumoCarteiras(carteiras, ativos, valorMercadoTotal));
    }

    public async Task<FinancasImportacaoDto> ObterImportacaoDashboardAsync(CancellationToken cancellationToken = default)
    {
        var carga = await _uow.Financas.ObterCargaMaisRecenteAsync(cancellationToken);
        var documentos = await _uow.Financas.BuscarDocumentosMonitoradosAsync(cancellationToken);
        var ultimaImportacao = await _uow.Financas.ObterUltimaImportacaoArquivoAsync(cancellationToken);
        var cotacoes = await _uow.Financas.BuscarCotacoesAtuaisAsync(cancellationToken);

        return new FinancasImportacaoDto(
            CriarKpis(carga),
            new ImportacaoFinanceiraResumoDto(
                ultimaImportacao?.FinishedAt ?? ultimaImportacao?.StartedAt,
                documentos.Count,
                documentos.Count(x => x.ParseStatus is StatusParseDocumentoFinanceiro.Processado or StatusParseDocumentoFinanceiro.ParcialmenteProcessado),
                documentos.Count(x => x.ParseStatus is StatusParseDocumentoFinanceiro.Falhou or StatusParseDocumentoFinanceiro.SemDadosEstruturados),
                ultimaImportacao?.SourceFolder),
            cotacoes.OrderByDescending(x => x.RetrievedAt).Select(x => (DateTime?)x.RetrievedAt).FirstOrDefault());
    }

    public async Task<FinancasOperacionalDto> ObterOperacionalDashboardAsync(CancellationToken cancellationToken = default)
    {
        var posicoes = await _uow.Financas.BuscarPosicoesAsync(true, cancellationToken);
        var alertas = await _uow.Financas.BuscarAlertasAsync(cancellationToken);

        return new FinancasOperacionalDto(
            posicoes.Where(EhPosicaoEstimativaAbertaVisivel).Take(12).Select(MapPosicao).ToList(),
            alertas.Take(8).Select(MapAlerta).ToList());
    }

    public async Task<FinancasDashboardDto> ObterDashboardAsync(CancellationToken cancellationToken = default)
    {
        await _importador.GarantirCargaInicialAsync(cancellationToken);
        var carga = await _uow.Financas.ObterCargaMaisRecenteAsync(cancellationToken);
        var alertas = await _uow.Financas.BuscarAlertasAsync(cancellationToken);
        var posicoes = await _uow.Financas.BuscarPosicoesAsync(null, cancellationToken);
        var cotacoes = await _uow.Financas.BuscarCotacoesAtuaisAsync(cancellationToken);
        var carteiras = await _uow.Financas.BuscarCarteirasComAtivosAsync(cancellationToken);
        var documentosMonitorados = await _uow.Financas.BuscarDocumentosMonitoradosAsync(cancellationToken);
        var ultimaImportacao = await _uow.Financas.ObterUltimaImportacaoArquivoAsync(cancellationToken);
        var transacoes = await _uow.Financas.BuscarTodasTransacoesAsync(cancellationToken);
        var posicoesTabela = CalcularPosicoes(transacoes).Values.Where(p => p.Quantidade > QuantidadeAbertaMinima).ToList();
        var ativosCotados = CriarAtivosCotadosDaTabela(posicoesTabela, cotacoes)
            .Where(EhAtivoCotadoVisivel)
            .ToList();
        var valorMercadoTotal = ativosCotados.Sum(x => x.ValorMercado);

        var hojeProv = DateTime.UtcNow.Date;
        var proventosMensais = CriarProventosMensais(
            await _uow.Financas.BuscarProventosPorPeriodoAsync(new DateTime(2000, 1, 1), hojeProv.AddYears(1), cancellationToken),
            hojeProv);

        var dashboard = new FinancasDashboardDto
        {
            GeradoEm = carga?.GeneratedAt?.ToString("dd/MM/yyyy HH:mm", CultureInfo.GetCultureInfo("pt-BR")) ?? string.Empty,
            Fonte = carga?.SourcePath ?? string.Empty,
            DashboardJson = carga?.DashboardJson,
            Kpis = CriarKpis(carga),
            B3PorAno = (await _uow.Financas.BuscarAgregadosAsync("b3-year", cancellationToken)).Select(MapSerie).ToList(),
            B3PorMes = (await _uow.Financas.BuscarAgregadosAsync("b3-month", cancellationToken)).Select(MapSerie).ToList(),
            B3PorClasse = (await _uow.Financas.BuscarAgregadosAsync("b3-class", cancellationToken)).Select(MapDistribuicao).ToList(),
            BinanceMoedas = (await _uow.Financas.BuscarAgregadosAsync("binance-coin", cancellationToken)).Select(MapDistribuicao).Take(12).ToList(),
            UltimasOperacoesB3 = (await _uow.Financas.BuscarUltimasOperacoesB3Async(12, cancellationToken)).Select(MapOperacaoB3).ToList(),
            UltimasTransacoesCripto = (await _uow.Financas.BuscarUltimasTransacoesCriptoAsync(12, cancellationToken)).Select(MapTransacaoCripto).ToList(),
            PosicoesAbertas = posicoes.Where(EhPosicaoEstimativaAbertaVisivel).Take(12).Select(MapPosicao).ToList(),
            PosicoesEncerradas = posicoes.Where(p => p.Status == StatusEstimativaPosicao.EncerradaPorOperacoes).Take(8).Select(MapPosicao).ToList(),
            Alertas = alertas.Take(8).Select(MapAlerta).ToList(),
            AtivosCotados = ativosCotados,
            Carteiras = CriarResumoCarteiras(carteiras, ativosCotados, valorMercadoTotal),
            Periodos = [],
            ImportacaoArquivos = new ImportacaoFinanceiraResumoDto(
                ultimaImportacao?.FinishedAt ?? ultimaImportacao?.StartedAt,
                documentosMonitorados.Count,
                documentosMonitorados.Count(x => x.ParseStatus == StatusParseDocumentoFinanceiro.Processado || x.ParseStatus == StatusParseDocumentoFinanceiro.ParcialmenteProcessado),
                documentosMonitorados.Count(x => x.ParseStatus is StatusParseDocumentoFinanceiro.Falhou or StatusParseDocumentoFinanceiro.SemDadosEstruturados),
                ultimaImportacao?.SourceFolder),
            CotacoesAtualizadasEm = cotacoes.OrderByDescending(x => x.RetrievedAt).Select(x => (DateTime?)x.RetrievedAt).FirstOrDefault(),
            ValorMercadoTotal = valorMercadoTotal,
            ProventosMensais = proventosMensais,
            CustoEstimadoTotal = ativosCotados.Sum(x => x.CustoEstimado),
            ResultadoNaoRealizadoTotal = ativosCotados.Sum(x => x.ResultadoNaoRealizado)
        };

        return dashboard;
    }

    public async Task<PagedResult<DocumentoFinanceiroDto>> BuscarDocumentosAsync(int page, int pageSize, string? termo, CancellationToken cancellationToken = default)
    {
        await _importador.GarantirCargaInicialAsync(cancellationToken);
        var result = await _uow.Financas.BuscarDocumentosAsync(page, pageSize, termo, cancellationToken);
        return new PagedResult<DocumentoFinanceiroDto>(result.Items.Select(MapDocumento).ToList(), result.TotalCount, result.Page, result.PageSize);
    }

    public async Task<(DocumentoFinanceiroDto? Documento, IReadOnlyList<ConteudoBrutoFinanceiroDto> Conteudos)> ObterDocumentoAsync(int id, CancellationToken cancellationToken = default)
    {
        await _importador.GarantirCargaInicialAsync(cancellationToken);
        var documento = await _uow.Financas.ObterDocumentoAsync(id, cancellationToken);
        if (documento is null)
            return (null, []);

        var conteudos = await _uow.Financas.BuscarConteudosDocumentoAsync(id, cancellationToken);
        return (MapDocumento(documento), conteudos.Select(MapConteudo).ToList());
    }

    public async Task<PagedResult<OperacaoB3Dto>> BuscarOperacoesB3Async(int page, int pageSize, string? termo, int? ano, string? classe, CancellationToken cancellationToken = default)
    {
        await _importador.GarantirCargaInicialAsync(cancellationToken);
        var result = await _uow.Financas.BuscarOperacoesB3Async(page, pageSize, termo, ano, classe, cancellationToken);
        return new PagedResult<OperacaoB3Dto>(result.Items.Select(MapOperacaoB3).ToList(), result.TotalCount, result.Page, result.PageSize);
    }

    public async Task<PagedResult<TransacaoCriptoDto>> BuscarTransacoesCriptoAsync(int page, int pageSize, string? termo, CancellationToken cancellationToken = default)
    {
        await _importador.GarantirCargaInicialAsync(cancellationToken);
        var result = await _uow.Financas.BuscarTransacoesCriptoAsync(page, pageSize, termo, cancellationToken);
        return new PagedResult<TransacaoCriptoDto>(result.Items.Select(MapTransacaoCripto).ToList(), result.TotalCount, result.Page, result.PageSize);
    }

    public async Task<IReadOnlyList<PosicaoFinanceiraDto>> BuscarPosicoesAsync(bool? somenteAbertas, CancellationToken cancellationToken = default)
    {
        await _importador.GarantirCargaInicialAsync(cancellationToken);
        var result = await _uow.Financas.BuscarPosicoesAsync(somenteAbertas, cancellationToken);
        if (somenteAbertas == true)
            result = result.Where(EhPosicaoEstimativaAbertaVisivel).ToList();
        return result.Select(MapPosicao).ToList();
    }

    public async Task<IReadOnlyList<AlertaConfiabilidadeDto>> BuscarAlertasAsync(CancellationToken cancellationToken = default)
    {
        await _importador.GarantirCargaInicialAsync(cancellationToken);
        var result = await _uow.Financas.BuscarAlertasAsync(cancellationToken);
        return result.Select(MapAlerta).ToList();
    }

    public async Task ImportarPastaMonitoradaAsync(int? usuarioId = null, CancellationToken cancellationToken = default)
    {
        await _importador.ImportarPastaMonitoradaAsync(cancellationToken);

        await _log.RegistrarFinanceiroAsync(
            "Importacao", "ImportarPasta", true,
            "Importação da pasta monitorada concluída.",
            LogTipo.Sucesso, usuarioId?.ToString(CultureInfo.InvariantCulture) ?? "sistema", null, cancellationToken);
        await _uow.ConfirmarAsync(cancellationToken);

        // Notifica quem disparou a importação (aparece no badge de não-lidas / tela de avisos).
        if (usuarioId is > 0)
        {
            await _mensagem.EnviarAsync(
                null, usuarioId.Value,
                "Importação financeira concluída",
                "Seus relatórios foram importados e a carteira foi atualizada. Confira o dashboard de Finanças.",
                null, cancellationToken);
        }
    }

    public async Task AtualizarCotacoesAsync(CancellationToken cancellationToken = default)
        => await _marketData.AtualizarCotacoesAsync(force: true, cancellationToken);

    public async Task AtualizarProventosAsync(CancellationToken cancellationToken = default)
        => await _marketData.AtualizarProventosAsync(force: true, cancellationToken);

    public async Task<ProventosPaginaDto> BuscarProventosAsync(int page, int pageSize, string? termo, CancellationToken cancellationToken = default)
    {
        await _importador.GarantirCargaInicialAsync(cancellationToken);
        var paged = await _uow.Financas.BuscarProventosAsync(page, pageSize, termo, cancellationToken);

        var hoje = DateTime.UtcNow.Date;
        var todos = await _uow.Financas.BuscarProventosPorPeriodoAsync(new DateTime(2000, 1, 1), hoje.AddYears(5), cancellationToken);
        var recebidos = todos.Where(x => x.PaymentDate <= hoje).ToList();
        var resumo = new ProventosResumoDto(
            Math.Round(recebidos.Where(x => x.PaymentDate!.Value.Year == hoje.Year && x.PaymentDate!.Value.Month == hoje.Month).Sum(ValorLiquido), 2),
            Math.Round(recebidos.Where(x => x.PaymentDate!.Value.Year == hoje.Year).Sum(ValorLiquido), 2),
            Math.Round(recebidos.Sum(ValorLiquido), 2),
            Math.Round(todos.Where(x => x.PaymentDate > hoje).Sum(ValorLiquido), 2),
            todos.Count);

        var periodos = CriarPeriodosProventos(todos, hoje);
        var transacoes = await _uow.Financas.BuscarTodasTransacoesAsync(cancellationToken);
        var baldes = CriarBaldesProventos(transacoes, recebidos.Where(x => x.PaymentDate!.Value.Year == hoje.Year).ToList(), hoje);
        var mensais = CriarProventosMensais(todos, hoje);

        return new ProventosPaginaDto(paged.Items.Select(MapProvento).ToList(), paged.Page, paged.PageSize, paged.TotalCount, resumo, periodos, baldes, mensais);
    }

    // Série dos últimos 12 meses (estilo TradeMap): cada mês = soma líquida dos proventos pagos.
    // Meses futuros (data de pagamento à frente de hoje) vão como "a receber".
    private static IReadOnlyList<ProventoMensalDto> CriarProventosMensais(IReadOnlyList<RendimentoInvestimento> proventos, DateTime hoje)
    {
        var meses = Enumerable.Range(0, 24)
            .Select(i => new DateTime(hoje.Year, hoje.Month, 1).AddMonths(-23 + i))
            .ToList();

        var cultura = CultureInfo.GetCultureInfo("pt-BR");
        return meses.Select(m =>
        {
            var doMes = proventos.Where(x => x.PaymentDate.HasValue
                && x.PaymentDate.Value.Year == m.Year && x.PaymentDate.Value.Month == m.Month).ToList();
            var recebido = doMes.Where(x => x.PaymentDate!.Value.Date <= hoje).Sum(ValorLiquido);
            var aReceber = doMes.Where(x => x.PaymentDate!.Value.Date > hoje).Sum(ValorLiquido);
            return new ProventoMensalDto(
                $"{cultura.DateTimeFormat.GetAbbreviatedMonthName(m.Month)}/{m:yy}",
                m.Year, m.Month,
                Math.Round(recebido, 2),
                Math.Round(aReceber, 2));
        }).ToList();
    }

    private static decimal ValorLiquido(RendimentoInvestimento r) => r.Amount - r.TaxWithheld;

    private static IReadOnlyList<ProventoPeriodoDto> CriarPeriodosProventos(IReadOnlyList<RendimentoInvestimento> proventos, DateTime hoje)
    {
        var inicioSemana = hoje.AddDays(-((int)hoje.DayOfWeek));
        var inicioQuinzena = hoje.Day <= 15 ? new DateTime(hoje.Year, hoje.Month, 1) : new DateTime(hoje.Year, hoje.Month, 16);
        var fimQuinzena = hoje.Day <= 15 ? new DateTime(hoje.Year, hoje.Month, 15) : new DateTime(hoje.Year, hoje.Month, DateTime.DaysInMonth(hoje.Year, hoje.Month));
        var inicioMes = new DateTime(hoje.Year, hoje.Month, 1);
        var fimMes = inicioMes.AddMonths(1).AddDays(-1);
        var inicioAno = new DateTime(hoje.Year, 1, 1);
        var fimAno = new DateTime(hoje.Year, 12, 31);

        var specs = new (string Codigo, string Rotulo, DateTime Inicio, DateTime Fim)[]
        {
            ("SEMANA", "Semana", inicioSemana, inicioSemana.AddDays(6)),
            ("QUINZENA", "Quinzena", inicioQuinzena, fimQuinzena),
            ("MES", "Mes", inicioMes, fimMes),
            ("ANO", "Ano", inicioAno, fimAno)
        };

        var valores = specs
            .Select(s =>
            {
                var itens = proventos.Where(x => x.PaymentDate.HasValue && x.PaymentDate.Value.Date >= s.Inicio && x.PaymentDate.Value.Date <= s.Fim).ToList();
                var recebidos = itens.Where(x => x.PaymentDate!.Value.Date <= hoje).ToList();
                var recebido = recebidos.Sum(ValorLiquido);
                var aReceber = itens.Where(x => x.PaymentDate!.Value.Date > hoje).Sum(ValorLiquido);
                return (s.Codigo, s.Rotulo, Recebido: Math.Round(recebido, 2), AReceber: Math.Round(aReceber, 2), Quantidade: itens.Count);
            })
            .ToList();
        var maior = valores.Select(x => x.Recebido + x.AReceber).DefaultIfEmpty(0m).Max();

        return valores
            .Select(x => new ProventoPeriodoDto(
                x.Codigo,
                x.Rotulo,
                x.Recebido,
                x.AReceber,
                x.Quantidade,
                maior == 0m ? 0m : Math.Round((x.Recebido + x.AReceber) / maior * 100m, 2)))
            .ToList();
    }

    private static ProventoDto MapProvento(RendimentoInvestimento r)
        => new(
            r.Id,
            r.PaymentDate,
            r.ReferenceDate,
            r.Asset?.Ticker ?? r.Asset?.AssetKey ?? string.Empty,
            r.Asset?.Name ?? string.Empty,
            r.Asset?.AssetClass.ToString() ?? string.Empty,
            string.IsNullOrWhiteSpace(r.IncomeType) ? "Provento" : r.IncomeType,
            r.Quantity,
            r.RatePerShare,
            Math.Round(r.Amount, 2),
            Math.Round(r.TaxWithheld, 2),
            Math.Round(r.Amount - r.TaxWithheld, 2),
            r.Taxation,
            string.IsNullOrWhiteSpace(r.Fonte) ? r.Source : r.Fonte);

    private static IReadOnlyList<ProventoBaldeDto> CriarBaldesProventos(IReadOnlyList<TransacaoFinanceira> transacoes, IReadOnlyList<RendimentoInvestimento> proventosAno, DateTime hoje)
    {
        var inicioAno = new DateTime(hoje.Year, 1, 1);
        var fimAno = new DateTime(hoje.Year, 12, 31);
        var estado = new Dictionary<int, PosicaoAcumulada>();
        decimal resultadoTrade = 0m;
        var vendas = 0;

        foreach (var t in transacoes.Where(t => t.Asset is not null && t.Date.Date <= fimAno).OrderBy(t => t.Date).ThenBy(t => t.Id))
        {
            if (!estado.TryGetValue(t.AssetId, out var pos))
            {
                pos = new PosicaoAcumulada { Asset = t.Asset! };
                estado[t.AssetId] = pos;
            }

            var delta = DeltaQuantidade(t);
            if (delta > 0m)
            {
                pos.Custo += t.Quantity * t.UnitPrice + t.Fees;
                pos.Quantidade += t.Quantity;
            }
            else if (delta < 0m)
            {
                var precoMedio = pos.Quantidade > 0m ? pos.Custo / pos.Quantidade : 0m;
                if (t.OperationType == TipoOperacaoFinanceira.Venda && t.Date.Date >= inicioAno)
                {
                    resultadoTrade += t.Quantity * (t.UnitPrice - precoMedio) - t.Fees;
                    vendas++;
                }

                var reduz = Math.Min(t.Quantity, pos.Quantidade);
                pos.Custo -= reduz * precoMedio;
                pos.Quantidade -= t.Quantity;
                if (pos.Quantidade <= 0.000000000001m)
                {
                    pos.Quantidade = 0m;
                    pos.Custo = 0m;
                }
            }
        }

        var rendimentos = proventosAno.Sum(ValorLiquido);
        var totalAbs = Math.Abs(resultadoTrade) + Math.Abs(rendimentos);
        decimal Percent(decimal valor) => totalAbs == 0m ? 0m : Math.Round(Math.Abs(valor) / totalAbs * 100m, 2);

        return
        [
            new ProventoBaldeDto("TRADE", "Trade", Math.Round(resultadoTrade, 2), Percent(resultadoTrade), vendas, resultadoTrade >= 0m ? "Lucro realizado" : "Prejuizo realizado"),
            new ProventoBaldeDto("RENDIMENTOS", "Rendimentos", Math.Round(rendimentos, 2), Percent(rendimentos), proventosAno.Count, "Proventos recebidos")
        ];
    }

    public Task<ValidacaoAtivoResultado> ValidarAtivoAsync(string ticker, CancellationToken cancellationToken = default)
        => _marketData.ValidarAtivoAsync(ticker, cancellationToken);

    public async Task<EvolucaoPatrimonioDto> ObterEvolucaoPatrimonioAsync(CancellationToken cancellationToken = default)
    {
        await _importador.GarantirCargaInicialAsync(cancellationToken);

        var hoje = DateTime.UtcNow.Date;
        var inicio = hoje.AddYears(-1);
        var transacoes = await _uow.Financas.BuscarTodasTransacoesAsync(cancellationToken);
        var historico = await _uow.Financas.BuscarHistoricoPrecosAsync(inicio, cancellationToken);
        var carteiras = await _uow.Financas.BuscarCarteirasComAtivosAsync(cancellationToken);
        var cotacoes = await _uow.Financas.BuscarCotacoesAtuaisAsync(cancellationToken);

        return CriarEvolucaoPatrimonio(transacoes, historico, carteiras, cotacoes, hoje, inicio);
    }

    // Apuração de IR (cola): usa o carregador central (já com ajuste de split) + os proventos.
    public async Task<ApuracaoIrDto> ObterApuracaoIrAsync(int ano, CancellationToken cancellationToken = default)
    {
        await _importador.GarantirCargaInicialAsync(cancellationToken);
        var transacoes = await _uow.Financas.BuscarTodasTransacoesAsync(cancellationToken);
        var rendimentos = await _uow.Financas.BuscarRendimentosAsync(cancellationToken);
        return CalculadoraIr.Apurar(ano, transacoes, rendimentos);
    }

    public async Task<byte[]> ExportarApuracaoIrExcelAsync(int ano, CancellationToken cancellationToken = default)
        => ExcelApuracaoIr.Gerar(await ObterApuracaoIrAsync(ano, cancellationToken));

    private static EvolucaoPatrimonioDto CriarEvolucaoPatrimonio(
        IReadOnlyList<TransacaoFinanceira> transacoes,
        IReadOnlyList<PrecoHistoricoAtivoFinanceiro> historico,
        IReadOnlyList<CarteiraFinanceira> carteiras,
        IReadOnlyList<CotacaoAtivoFinanceiro> cotacoes,
        DateTime hoje,
        DateTime inicio)
    {
        var totalDias = (hoje - inicio).Days + 1;
        var datas = Enumerable.Range(0, totalDias).Select(i => inicio.AddDays(i)).ToList();
        var cotacaoPorAtivo = cotacoes
            .GroupBy(c => c.AtivoFinanceiroId)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(c => c.RetrievedAt).First());

        // Agrupa por carteira/grupo (Setor, Tese de cripto, Classe de FII...). Cada ativo cai na
        // primeira carteira que o contém, na ordem definida. Ativos sem grupo vão para "Outros".
        var setorPorAtivo = new Dictionary<int, string>();
        foreach (var carteira in carteiras.OrderBy(c => c.Ordem).ThenBy(c => c.Nome))
            foreach (var item in carteira.Ativos.Where(a => a.AtivoFinanceiroId > 0))
                setorPorAtivo.TryAdd(item.AtivoFinanceiroId, carteira.Nome);

        // closeBRL por (ativo, data), já ordenado, para forward-fill.
        var precosPorAtivo = historico
            .GroupBy(h => h.AtivoFinanceiroId)
            .ToDictionary(
                g => g.Key,
                g => g.GroupBy(h => h.Date.Date)
                      .Select(d => (Date: d.Key, Close: d.OrderByDescending(x => x.CloseBRL).First().CloseBRL))
                      .OrderBy(x => x.Date)
                      .ToList());

        var total = new decimal[totalDias];
        var seriesSetor = new Dictionary<string, decimal[]>();

        foreach (var grupo in transacoes.GroupBy(t => t.AssetId))
        {
            var txs = grupo.OrderBy(t => t.Date).ToList();
            var assetGrupo = txs.FirstOrDefault(t => t.Asset is not null)?.Asset;
            if (assetGrupo is null)
                continue;
            precosPorAtivo.TryGetValue(grupo.Key, out var candles);
            var setor = setorPorAtivo.TryGetValue(grupo.Key, out var s) ? s : "Outros";
            if (!seriesSetor.TryGetValue(setor, out var arrSetor))
            {
                arrSetor = new decimal[totalDias];
                seriesSetor[setor] = arrSetor;
            }

            decimal quantidade = 0m;
            decimal custo = 0m;
            decimal ultimoPreco = 0m;
            int ti = 0, ci = 0;

            for (int di = 0; di < totalDias; di++)
            {
                var d = datas[di];
                while (ti < txs.Count && txs[ti].Date.Date <= d)
                {
                    var tx = txs[ti];
                    var delta = DeltaQuantidade(tx);
                    if (delta > 0m)
                    {
                        custo += tx.Quantity * tx.UnitPrice + tx.Fees;
                        quantidade += tx.Quantity;
                    }
                    else if (delta < 0m)
                    {
                        var precoMedioVenda = quantidade > 0m ? custo / quantidade : 0m;
                        var reduz = Math.Min(tx.Quantity, quantidade);
                        custo -= reduz * precoMedioVenda;
                        quantidade -= tx.Quantity;
                        if (quantidade <= 0.000000000001m)
                        {
                            quantidade = 0m;
                            custo = 0m;
                        }
                    }
                    ti++;
                }
                if (candles is not null)
                    while (ci < candles.Count && candles[ci].Date <= d)
                    {
                        ultimoPreco = candles[ci].Close;
                        ci++;
                    }

                var precoMedio = quantidade > 0m ? custo / quantidade : 0m;
                var preco = ultimoPreco > 0m ? ultimoPreco : precoMedio;
                if (di == totalDias - 1
                    && cotacaoPorAtivo.TryGetValue(grupo.Key, out var cotAtual)
                    && cotAtual.PriceBRL > 0m)
                    preco = cotAtual.PriceBRL;

                var valor = quantidade > QuantidadeAbertaMinima ? quantidade * preco : 0m;
                if (!EhValorPosicaoVisivel(assetGrupo, quantidade, valor))
                    valor = 0m;
                total[di] += valor;
                arrSetor[di] += valor;
            }
        }

        decimal atual = total.Length > 0 ? total[^1] : 0m;
        PeriodoPerformanceDto Periodo(string codigo, string label, DateTime baseData)
        {
            var idx = datas.FindIndex(x => x >= baseData.Date);
            if (idx < 0) idx = 0;
            var baseValor = total[idx];
            var variacao = atual - baseValor;
            return new PeriodoPerformanceDto(codigo, label, baseValor == 0 ? 0 : variacao / baseValor * 100m, variacao);
        }

        var periodos = new List<PeriodoPerformanceDto>
        {
            Periodo("1D", "1 dia", hoje.AddDays(-1)),
            Periodo("5D", "5 dias", hoje.AddDays(-5)),
            Periodo("1M", "1 mês", hoje.AddMonths(-1)),
            Periodo("6M", "6 meses", hoje.AddMonths(-6)),
            Periodo("YTD", "No ano", new DateTime(hoje.Year, 1, 1)),
            Periodo("1A", "1 ano", inicio)
        };

        // Variação do dia por setor e total, a partir das cotações ao vivo (não do histórico diário).
        var posicoesAtuais = CalcularPosicoes(transacoes);

        var diaPorSetor = new Dictionary<string, (decimal Valor, decimal VarValor)>(StringComparer.OrdinalIgnoreCase);
        decimal valorCotadoDiaTotal = 0m, varDiaValorTotal = 0m;
        foreach (var pos in posicoesAtuais.Values.Where(p => p.Quantidade > QuantidadeAbertaMinima))
        {
            if (!cotacaoPorAtivo.TryGetValue(pos.Asset.Id, out var cot) || cot.PriceBRL <= 0m)
                continue;

            var valorAtual = pos.Quantidade * cot.PriceBRL;
            if (!EhValorPosicaoVisivel(pos.Asset, pos.Quantidade, valorAtual))
                continue;
            var varValor = valorAtual * ((cot.ChangePercent ?? 0m) / 100m);
            var setorAtivo = setorPorAtivo.TryGetValue(pos.Asset.Id, out var s) ? s : "Outros";
            diaPorSetor.TryGetValue(setorAtivo, out var acc);
            diaPorSetor[setorAtivo] = (acc.Valor + valorAtual, acc.VarValor + varValor);
            valorCotadoDiaTotal += valorAtual;
            varDiaValorTotal += varValor;
        }

        decimal VarDiaSetor(string setor)
            => diaPorSetor.TryGetValue(setor, out var acc) && acc.Valor != 0m ? Math.Round(acc.VarValor / acc.Valor * 100m, 2) : 0m;
        decimal ValorVivoSetor(string setor)
            => diaPorSetor.TryGetValue(setor, out var acc) ? Math.Round(acc.Valor, 2) : 0m;

        var setores = seriesSetor
            .OrderByDescending(x => x.Value.Length > 0 ? x.Value[^1] : 0m)
            .Select(x => new SerieEvolucaoDto(Slug(x.Key), x.Key, x.Value.Select(v => Math.Round(v, 2)).ToList(), VarDiaSetor(x.Key), ValorVivoSetor(x.Key)))
            .Where(x => x.Valores.Any(v => v != 0m))
            .ToList();

        var variacaoDiaTotal = valorCotadoDiaTotal == 0m ? 0m : Math.Round(varDiaValorTotal / valorCotadoDiaTotal * 100m, 2);

        return new EvolucaoPatrimonioDto(
            datas.Select(d => d.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)).ToList(),
            total.Select(v => Math.Round(v, 2)).ToList(),
            variacaoDiaTotal,
            Math.Round(atual, 2),
            setores,
            periodos);
    }

    public async Task<ResumoAnaliticoDto> ObterResumoAnaliticoAsync(DateTime? inicio, DateTime? fim, CancellationToken cancellationToken = default)
    {
        await _importador.GarantirCargaInicialAsync(cancellationToken);

        var fimPeriodo = (fim ?? DateTime.UtcNow.Date).Date;
        var inicioPeriodo = (inicio ?? new DateTime(fimPeriodo.Year, fimPeriodo.Month, 1)).Date;

        var transacoes = (await _uow.Financas.BuscarTodasTransacoesAsync(cancellationToken))
            .Where(t => t.Date.Date <= fimPeriodo && t.Asset is not null)
            .OrderBy(t => t.Date)
            .ThenBy(t => t.Id)
            .ToList();

        var cotacoes = await _uow.Financas.BuscarCotacoesAtuaisAsync(cancellationToken);
        var precoAtualPorAtivo = cotacoes
            .GroupBy(c => c.AtivoFinanceiroId)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(c => c.RetrievedAt).First().PriceBRL);

        // Proventos líquidos recebidos no período, por ativo — entram no retorno total (ganho de capital
        // sozinho ignora dividendos/JCP/rendimentos de FII).
        var proventosPorAtivo = (await _uow.Financas.BuscarProventosPorPeriodoAsync(inicioPeriodo, fimPeriodo, cancellationToken))
            .Where(x => x.AssetId.HasValue)
            .GroupBy(x => x.AssetId!.Value)
            .ToDictionary(g => g.Key, g => g.Sum(ValorLiquido));

        var estado = new Dictionary<int, PosicaoAcumulada>();
        decimal totalComprado = 0m, totalVendido = 0m, resultadoRealizado = 0m;
        var numeroOperacoes = 0;
        var vendas = new List<VendaRealizadaDto>();

        foreach (var t in transacoes)
        {
            if (!estado.TryGetValue(t.AssetId, out var pos))
            {
                pos = new PosicaoAcumulada { Asset = t.Asset! };
                estado[t.AssetId] = pos;
            }

            var noPeriodo = t.Date.Date >= inicioPeriodo && t.Date.Date <= fimPeriodo;
            var delta = DeltaQuantidade(t);

            if (delta > 0)
            {
                pos.Custo += t.Quantity * t.UnitPrice + t.Fees;
                pos.Quantidade += t.Quantity;
                if (noPeriodo && t.OperationType == TipoOperacaoFinanceira.Compra)
                {
                    totalComprado += t.GrossAmount;
                    numeroOperacoes++;
                }
            }
            else if (delta < 0)
            {
                var precoMedio = pos.Quantidade > 0m ? pos.Custo / pos.Quantidade : 0m;
                if (t.OperationType == TipoOperacaoFinanceira.Venda)
                {
                    var resultado = t.Quantity * (t.UnitPrice - precoMedio) - t.Fees;
                    if (noPeriodo)
                    {
                        resultadoRealizado += resultado;
                        totalVendido += t.GrossAmount;
                        numeroOperacoes++;
                        pos.RealizadoPeriodo += resultado;
                        vendas.Add(new VendaRealizadaDto(t.Date, TickerDe(pos.Asset), t.Quantity, t.UnitPrice, precoMedio, resultado, t.UnitPrice >= precoMedio));
                    }
                }

                var reduz = Math.Min(t.Quantity, pos.Quantidade);
                pos.Custo -= reduz * precoMedio;
                pos.Quantidade -= t.Quantity;
                if (pos.Quantidade <= 0.000000000001m)
                {
                    pos.Quantidade = 0m;
                    pos.Custo = 0m;
                }
            }
        }

        var ativos = new List<ResumoAtivoDto>();
        decimal custoTotal = 0m, valorTotal = 0m, proventosTotal = 0m;
        foreach (var (ativoId, pos) in estado)
        {
            proventosPorAtivo.TryGetValue(ativoId, out var proventoAtivo);
            if (pos.Quantidade <= 0m && pos.RealizadoPeriodo == 0m && proventoAtivo == 0m)
                continue;

            var precoMedio = pos.Quantidade > 0m ? pos.Custo / pos.Quantidade : 0m;
            precoAtualPorAtivo.TryGetValue(ativoId, out var preco);
            decimal? precoAtual = preco > 0m ? preco : null;
            var valorMercado = pos.Quantidade * (precoAtual ?? precoMedio);
            var pl = valorMercado - pos.Custo;
            var plPercentual = pos.Custo > 0m ? pl / pos.Custo * 100m : 0m;
            var retornoTotalAtivo = pl + pos.RealizadoPeriodo + proventoAtivo;
            custoTotal += pos.Custo;
            valorTotal += valorMercado;
            proventosTotal += proventoAtivo;

            ativos.Add(new ResumoAtivoDto(
                TickerDe(pos.Asset),
                pos.Asset.Name,
                pos.Asset.AssetClass.ToString(),
                Math.Round(pos.Quantidade, 8),
                Math.Round(precoMedio, 4),
                Math.Round(pos.Custo, 2),
                precoAtual,
                Math.Round(valorMercado, 2),
                Math.Round(pl, 2),
                Math.Round(plPercentual, 2),
                Math.Round(pos.RealizadoPeriodo, 2),
                Math.Round(proventoAtivo, 2),
                Math.Round(retornoTotalAtivo, 2)));
        }

        var plNaoRealizadoTotal = valorTotal - custoTotal;
        return new ResumoAnaliticoDto(
            $"{inicioPeriodo:dd/MM/yyyy} a {fimPeriodo:dd/MM/yyyy}",
            inicioPeriodo,
            fimPeriodo,
            Math.Round(totalComprado, 2),
            Math.Round(totalVendido, 2),
            Math.Round(totalComprado - totalVendido, 2),
            Math.Round(resultadoRealizado, 2),
            numeroOperacoes,
            Math.Round(custoTotal, 2),
            Math.Round(valorTotal, 2),
            Math.Round(plNaoRealizadoTotal, 2),
            Math.Round(proventosTotal, 2),
            Math.Round(plNaoRealizadoTotal + resultadoRealizado + proventosTotal, 2),
            ativos.OrderByDescending(a => a.ValorMercado).ToList(),
            vendas.OrderByDescending(v => v.Data).ToList());
    }

    private static string TickerDe(AtivoFinanceiro a) => a.Ticker ?? a.AssetKey ?? a.Name ?? string.Empty;

    private static bool EhAtivoCotadoVisivel(CotacaoAtivoDto ativo)
    {
        if (ativo.Quantidade <= QuantidadeAbertaMinima)
            return false;

        return !string.Equals(ativo.Classe, ClasseAtivo.Cripto.ToString(), StringComparison.OrdinalIgnoreCase)
            || ativo.ValorMercado >= ValorDustCriptoMaximoBrl;
    }

    private static bool EhValorPosicaoVisivel(AtivoFinanceiro ativo, decimal quantidade, decimal valorMercado)
    {
        if (quantidade <= QuantidadeAbertaMinima)
            return false;

        return (!ativo.IsCrypto && ativo.AssetClass != ClasseAtivo.Cripto)
            || valorMercado >= ValorDustCriptoMaximoBrl;
    }

    private static bool EhPosicaoEstimativaAbertaVisivel(EstimativaPosicaoCarteira posicao)
    {
        if (posicao.Status != StatusEstimativaPosicao.AbertaOuResidual || posicao.Quantity <= QuantidadeAbertaMinima)
            return false;

        var ativo = posicao.Asset;
        if (ativo is null || (!ativo.IsCrypto && ativo.AssetClass != ClasseAtivo.Cripto))
            return true;

        var valor = posicao.EstimatedCurrentPosition != 0m
            ? Math.Abs(posicao.EstimatedCurrentPosition)
            : Math.Abs(posicao.Quantity * posicao.AveragePrice);
        return valor >= ValorDustCriptoMaximoBrl;
    }

    private sealed class PosicaoAcumulada
    {
        public decimal Quantidade;
        public decimal Custo;
        public decimal RealizadoPeriodo;
        public AtivoFinanceiro Asset = null!;
    }

    // Efeito de uma transação no estoque do ativo (Quantity é sempre positiva; o tipo dá o sentido).
    private static decimal DeltaQuantidade(TransacaoFinanceira t) => t.OperationType switch
    {
        TipoOperacaoFinanceira.Compra or TipoOperacaoFinanceira.Deposito or TipoOperacaoFinanceira.Rendimento => t.Quantity,
        TipoOperacaoFinanceira.Venda or TipoOperacaoFinanceira.Saque or TipoOperacaoFinanceira.Taxa => -t.Quantity,
        _ => 0m
    };

    private static string Slug(string valor)
        => new string((valor ?? string.Empty).ToLowerInvariant().Select(c => char.IsLetterOrDigit(c) ? c : '-').ToArray()).Trim('-');

    public async Task<PagedResult<TransacaoFinanceiraDto>> BuscarTransacoesAsync(int page, int pageSize, string? termo, string? origem, CancellationToken cancellationToken = default)
    {
        await _importador.GarantirCargaInicialAsync(cancellationToken);
        OrigemTransacao? origemFiltro = Enum.TryParse<OrigemTransacao>(origem, true, out var o) ? o : null;
        var result = await _uow.Financas.BuscarTransacoesAsync(page, pageSize, termo, origemFiltro, cancellationToken);
        return new PagedResult<TransacaoFinanceiraDto>(result.Items.Select(MapTransacao).ToList(), result.TotalCount, result.Page, result.PageSize);
    }

    public async Task<DataTablesResponse<TransacaoFinanceiraDto>> BuscarTransacoesDataTableAsync(DataTablesRequest request, string? origem, CancellationToken cancellationToken = default)
    {
        await _importador.GarantirCargaInicialAsync(cancellationToken);
        OrigemTransacao? origemFiltro = Enum.TryParse<OrigemTransacao>(origem, true, out var o) ? o : null;
        var resposta = await _uow.Financas.BuscarTransacoesDataTableAsync(request, origemFiltro, cancellationToken);
        return resposta.Map(MapTransacao);
    }

    public async Task<ResultadoOperacao> RegistrarTransacaoManualAsync(NovaTransacaoInput input, CancellationToken cancellationToken = default)
    {
        if (input is null || string.IsNullOrWhiteSpace(input.Ticker))
            return new ResultadoOperacao(false, "Informe o ticker do ativo.");
        if (input.Quantidade <= 0)
            return new ResultadoOperacao(false, "Quantidade deve ser maior que zero.");
        if (input.PrecoUnitario < 0)
            return new ResultadoOperacao(false, "Preço inválido.");
        if (!Enum.TryParse<TipoOperacaoFinanceira>(input.Tipo, true, out var tipo) || tipo is not (TipoOperacaoFinanceira.Compra or TipoOperacaoFinanceira.Venda))
            return new ResultadoOperacao(false, "Tipo deve ser Compra ou Venda.");

        var ticker = input.Ticker.Trim().ToUpperInvariant();
        var ativo = await _uow.Financas.ObterAtivoPorChaveOuTickerAsync(ticker, cancellationToken);
        if (ativo is null)
        {
            var validacao = await _marketData.ValidarAtivoAsync(ticker, cancellationToken);
            if (!validacao.Valido)
                return new ResultadoOperacao(false, validacao.Mensagem ?? "Ativo inválido.");

            var classe = Enum.TryParse<ClasseAtivo>(validacao.Classe, true, out var c) ? c : ClasseAtivo.Outro;
            ativo = new AtivoFinanceiro
            {
                AssetKey = ticker,
                Ticker = ticker,
                Name = string.IsNullOrWhiteSpace(validacao.Nome) ? ticker : validacao.Nome,
                AssetClass = classe,
                Market = validacao.IsCrypto ? "Binance" : "B3",
                Currency = "BRL",
                IsCrypto = validacao.IsCrypto,
                IsActive = true,
                UsuarioInclusao = "financas-manual"
            };
            await _uow.Financas.AdicionarAtivoAsync(ativo, cancellationToken);
            await _uow.ConfirmarAsync(cancellationToken);
            await _marketData.GarantirCotacaoAtivoAsync(ativo.Id, cancellationToken);
        }

        var transacao = new TransacaoFinanceira
        {
            Origem = OrigemTransacao.Manual,
            AssetId = ativo.Id,
            Date = input.Data.Date,
            OperationType = tipo,
            Quantity = input.Quantidade,
            UnitPrice = input.PrecoUnitario,
            GrossAmount = input.Quantidade * input.PrecoUnitario,
            Fees = input.Taxas,
            Currency = "BRL",
            Broker = string.IsNullOrWhiteSpace(input.Corretora) ? "Manual" : input.Corretora!.Trim(),
            Fonte = "Manual",
            Observacao = input.Observacao,
            IsCanonical = true,
            ConfidenceLevel = NivelConfianca.Alta,
            RawJson = "{}",
            UsuarioInclusao = "financas-manual"
        };
        await _uow.Financas.AdicionarTransacaoAsync(transacao, cancellationToken);
        await _log.RegistrarFinanceiroAsync(
            "TransacaoFinanceira", "CriarManual", true,
            $"Transação manual {tipo} de {input.Quantidade} {ticker} a {input.PrecoUnitario}",
            LogTipo.Sucesso, UsuarioAtual, null, cancellationToken);
        await _uow.ConfirmarAsync(cancellationToken);
        return new ResultadoOperacao(true, "Transação registrada.", transacao.Id);
    }

    public async Task<ResultadoOperacao> EditarTransacaoAsync(int id, NovaTransacaoInput input, CancellationToken cancellationToken = default)
    {
        var transacao = await _uow.Financas.ObterTransacaoAsync(id, cancellationToken);
        if (transacao is null)
            return new ResultadoOperacao(false, "Transação não encontrada.");
        if (input.Quantidade <= 0)
            return new ResultadoOperacao(false, "Quantidade deve ser maior que zero.");
        if (!Enum.TryParse<TipoOperacaoFinanceira>(input.Tipo, true, out var tipo) || tipo is not (TipoOperacaoFinanceira.Compra or TipoOperacaoFinanceira.Venda))
            return new ResultadoOperacao(false, "Tipo deve ser Compra ou Venda.");

        transacao.OperationType = tipo;
        transacao.Quantity = input.Quantidade;
        transacao.UnitPrice = input.PrecoUnitario;
        transacao.GrossAmount = input.Quantidade * input.PrecoUnitario;
        transacao.Fees = input.Taxas;
        transacao.Date = input.Data.Date;
        transacao.Broker = string.IsNullOrWhiteSpace(input.Corretora) ? transacao.Broker : input.Corretora!.Trim();
        transacao.Observacao = input.Observacao;
        _uow.Financas.AtualizarTransacao(transacao);
        await _log.RegistrarFinanceiroAsync(
            "TransacaoFinanceira", "Editar", true,
            $"Transação #{transacao.Id} editada ({tipo} {input.Quantidade})",
            LogTipo.Informacao, UsuarioAtual, null, cancellationToken);
        await _uow.ConfirmarAsync(cancellationToken);
        return new ResultadoOperacao(true, "Transação atualizada.", transacao.Id);
    }

    public async Task<ResultadoOperacao> ExcluirTransacaoAsync(int id, CancellationToken cancellationToken = default)
    {
        var transacao = await _uow.Financas.ObterTransacaoAsync(id, cancellationToken);
        if (transacao is null)
            return new ResultadoOperacao(false, "Transação não encontrada.");

        _uow.Financas.RemoverTransacao(transacao);
        await _log.RegistrarFinanceiroAsync(
            "TransacaoFinanceira", "Excluir", true,
            $"Transação #{transacao.Id} excluída ({transacao.OperationType} {transacao.Quantity})",
            LogTipo.Informacao, UsuarioAtual, null, cancellationToken);
        await _uow.ConfirmarAsync(cancellationToken);
        return new ResultadoOperacao(true, "Transação excluída.");
    }

    public async Task<PagedResult<EventoCorporativoDto>> BuscarEventosCorporativosAsync(int page, int pageSize, string? termo, CancellationToken cancellationToken = default)
    {
        var result = await _uow.Financas.BuscarEventosCorporativosAsync(page, pageSize, termo, cancellationToken);
        return new PagedResult<EventoCorporativoDto>(result.Items.Select(MapEventoCorporativo).ToList(), result.TotalCount, result.Page, result.PageSize);
    }

    public async Task<ResultadoOperacao> RegistrarEventoCorporativoManualAsync(NovoEventoCorporativoInput input, CancellationToken cancellationToken = default)
    {
        if (input is null || string.IsNullOrWhiteSpace(input.Ticker))
            return new ResultadoOperacao(false, "Informe o ticker do ativo.");
        if (input.Fator <= 0m)
            return new ResultadoOperacao(false, "Fator deve ser maior que zero.");
        if (!Enum.TryParse<TipoEventoCorporativo>(input.Tipo, true, out var tipo))
            return new ResultadoOperacao(false, "Tipo de evento inválido.");

        var ticker = input.Ticker.Trim().ToUpperInvariant();
        var ativo = await _uow.Financas.ObterAtivoPorChaveOuTickerAsync(ticker, cancellationToken);
        if (ativo is null)
        {
            var validacao = await _marketData.ValidarAtivoAsync(ticker, cancellationToken);
            if (!validacao.Valido)
                return new ResultadoOperacao(false, validacao.Mensagem ?? "Ativo inválido.");

            var classe = Enum.TryParse<ClasseAtivo>(validacao.Classe, true, out var c) ? c : ClasseAtivo.Outro;
            ativo = new AtivoFinanceiro
            {
                AssetKey = ticker,
                Ticker = ticker,
                Name = string.IsNullOrWhiteSpace(validacao.Nome) ? ticker : validacao.Nome,
                AssetClass = classe,
                Market = validacao.IsCrypto ? "Binance" : "B3",
                Currency = "BRL",
                IsCrypto = validacao.IsCrypto,
                IsActive = true,
                UsuarioInclusao = "financas-manual"
            };
            await _uow.Financas.AdicionarAtivoAsync(ativo, cancellationToken);
            await _uow.ConfirmarAsync(cancellationToken);
        }

        var fonte = string.IsNullOrWhiteSpace(input.Fonte) ? "Manual" : input.Fonte!.Trim();
        // Chave canônica (independe da fonte) → dedup contra seed e Brapi: o mesmo split não entra 2×.
        var chaveNatural = EventoCorporativo.GerarChaveNatural(ticker, input.Data, input.Fator);

        var evento = new EventoCorporativo
        {
            AtivoFinanceiroId = ativo.Id,
            Tipo = tipo,
            Data = input.Data.Date,
            Fator = input.Fator,
            Fonte = fonte,
            ChaveNatural = chaveNatural,
            UsuarioInclusao = UsuarioAtual
        };
        await _uow.Financas.AdicionarEventoCorporativoAsync(evento, cancellationToken);
        await _log.RegistrarFinanceiroAsync(
            "EventoCorporativo", "CriarManual", true,
            $"Evento {tipo} de {ticker} em {input.Data:yyyy-MM-dd} fator {input.Fator}",
            LogTipo.Sucesso, UsuarioAtual, null, cancellationToken);
        await _uow.ConfirmarAsync(cancellationToken);
        return new ResultadoOperacao(true, "Evento corporativo registrado.", evento.Id);
    }

    public async Task<ResultadoOperacao> EditarEventoCorporativoAsync(int id, NovoEventoCorporativoInput input, CancellationToken cancellationToken = default)
    {
        var evento = await _uow.Financas.ObterEventoCorporativoAsync(id, cancellationToken);
        if (evento is null)
            return new ResultadoOperacao(false, "Evento corporativo não encontrado.");
        if (input.Fator <= 0m)
            return new ResultadoOperacao(false, "Fator deve ser maior que zero.");
        if (!Enum.TryParse<TipoEventoCorporativo>(input.Tipo, true, out var tipo))
            return new ResultadoOperacao(false, "Tipo de evento inválido.");

        evento.Tipo = tipo;
        evento.Data = input.Data.Date;
        evento.Fator = input.Fator;
        evento.Fonte = string.IsNullOrWhiteSpace(input.Fonte) ? evento.Fonte : input.Fonte!.Trim();
        _uow.Financas.AtualizarEventoCorporativo(evento);
        await _log.RegistrarFinanceiroAsync(
            "EventoCorporativo", "Editar", true,
            $"Evento #{evento.Id} editado ({tipo} fator {input.Fator})",
            LogTipo.Informacao, UsuarioAtual, null, cancellationToken);
        await _uow.ConfirmarAsync(cancellationToken);
        return new ResultadoOperacao(true, "Evento corporativo atualizado.", evento.Id);
    }

    public async Task<ResultadoOperacao> ExcluirEventoCorporativoAsync(int id, CancellationToken cancellationToken = default)
    {
        var evento = await _uow.Financas.ObterEventoCorporativoAsync(id, cancellationToken);
        if (evento is null)
            return new ResultadoOperacao(false, "Evento corporativo não encontrado.");

        _uow.Financas.RemoverEventoCorporativo(evento);
        await _log.RegistrarFinanceiroAsync(
            "EventoCorporativo", "Excluir", true,
            $"Evento #{evento.Id} excluído ({evento.Tipo} fator {evento.Fator})",
            LogTipo.Informacao, UsuarioAtual, null, cancellationToken);
        await _uow.ConfirmarAsync(cancellationToken);
        return new ResultadoOperacao(true, "Evento corporativo excluído.");
    }

    private static EventoCorporativoDto MapEventoCorporativo(EventoCorporativo e)
        => new(
            e.Id,
            e.AtivoFinanceiro?.Ticker ?? e.AtivoFinanceiro?.AssetKey ?? string.Empty,
            e.AtivoFinanceiro?.Name ?? string.Empty,
            e.Tipo.ToString(),
            e.Data,
            e.Fator,
            e.Fonte);

    private static IReadOnlyList<FinanceiroKpiDto> CriarKpis(CargaFinanceira? carga)
    {
        var summary = TryParseObject(carga?.DashboardJson, "summary") ?? TryParseObject(carga?.SummaryJson);
        decimal Get(string key)
        {
            if (!summary.HasValue || summary.Value.ValueKind != JsonValueKind.Object || !summary.Value.TryGetProperty(key, out var v))
                return 0m;

            return v.ValueKind == JsonValueKind.Number && v.TryGetDecimal(out var value) ? value : 0m;
        }

        return
        [
            new("Patrimônio IR 2025", Get("assetBalance2025"), "Bens/direitos do dashboard de referência", "bi-wallet2"),
            new("Compras B3", Get("b3BuyTotal"), "Operações canônicas importadas", "bi-arrow-down-circle"),
            new("Vendas B3", Get("b3SellTotal"), "Total bruto vendido no período", "bi-arrow-up-circle"),
            new("Aportes Binance", Get("binanceDepositsBRL"), "Histórico consolidado Binance", "bi-currency-bitcoin")
        ];
    }

    private static JsonElement? TryParseObject(string? json, string? child = null)
    {
        if (string.IsNullOrWhiteSpace(json))
            return null;

        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            if (child is not null && root.ValueKind == JsonValueKind.Object && root.TryGetProperty(child, out var selected))
                return selected.Clone();
            return root.Clone();
        }
        catch
        {
            return null;
        }
    }

    private static FinanceiroSerieDto MapSerie(AgregadoFinanceiro agregado)
        => new(agregado.Mes ?? agregado.Ano?.ToString(CultureInfo.InvariantCulture) ?? agregado.Chave, agregado.ValorCompra ?? 0m, agregado.ValorVenda ?? 0m, agregado.Saldo ?? 0m, agregado.Contagem ?? 0);

    private static FinanceiroDistribuicaoDto MapDistribuicao(AgregadoFinanceiro agregado)
        => new(agregado.Chave, Math.Abs(agregado.Saldo ?? agregado.Quantidade ?? 0m), agregado.Contagem ?? 0);

    private static DocumentoFinanceiroDto MapDocumento(DocumentoFinanceiro documento)
        => new(documento.Id, documento.FileName, documento.FileType, documento.Source, documento.Sha256, documento.SizeBytes, documento.ReferenceYear, documento.PageCount, documento.Status.ToString());

    private static ConteudoBrutoFinanceiroDto MapConteudo(ConteudoBrutoFinanceiro conteudo)
        => new(conteudo.Id, conteudo.ContentType.ToString(), conteudo.PageNumber, conteudo.SheetName, conteudo.RowNumber, conteudo.RawText, conteudo.RawJson);

    private static OperacaoB3Dto MapOperacaoB3(OperacaoB3 op)
        => new(op.Id, op.TradeDate, op.OperationType.ToString(), op.Asset?.Name ?? op.OriginalAssetName, op.Asset?.AssetClass.ToString() ?? string.Empty, op.Quantity, op.UnitPrice, op.GrossAmount, op.Market, op.SourceFile, op.PageNumber, op.IsCanonical, op.ConfidenceLevel.ToString());

    private static TransacaoCriptoDto MapTransacaoCripto(TransacaoCripto tx)
        => new(tx.Id, tx.TransactionDate, tx.OperationType.ToString(), tx.AssetSymbol, tx.Pair, tx.Amount, tx.Price, tx.Total, tx.SourceFile, tx.RawType);

    private static PosicaoFinanceiraDto MapPosicao(EstimativaPosicaoCarteira posicao)
        => new(posicao.Id, posicao.Asset?.Name ?? string.Empty, posicao.Asset?.AssetClass.ToString() ?? string.Empty, posicao.Quantity, posicao.AveragePrice, posicao.TotalInvested, posicao.TotalSold, posicao.RealizedResult, posicao.Status.ToString(), posicao.ConfidenceLevel.ToString(), posicao.LastOperationDate);

    private static AlertaConfiabilidadeDto MapAlerta(AlertaConfiabilidade alerta)
        => new(alerta.Id, alerta.EntityType, alerta.Severity.ToString(), alerta.Code, alerta.Message, alerta.Details, alerta.CreatedAt);

    private static TransacaoFinanceiraDto MapTransacao(TransacaoFinanceira t)
        => new(
            t.Id,
            t.Origem.ToString(),
            string.IsNullOrWhiteSpace(t.Fonte) ? t.Origem.ToString() : t.Fonte,
            t.Asset?.Name ?? t.Asset?.Ticker ?? string.Empty,
            t.Asset?.Ticker ?? t.Asset?.AssetKey ?? string.Empty,
            t.Asset?.AssetClass.ToString() ?? string.Empty,
            t.Date,
            t.OperationType.ToString(),
            t.Quantity,
            t.UnitPrice,
            t.GrossAmount,
            t.Fees,
            t.Broker,
            t.Observacao);

    // Posições atuais (qtd, custo, preço médio) por ativo, derivadas da tabela única de transações.
    private static Dictionary<int, PosicaoAcumulada> CalcularPosicoes(IReadOnlyList<TransacaoFinanceira> transacoes)
    {
        var estado = new Dictionary<int, PosicaoAcumulada>();
        foreach (var t in transacoes.Where(t => t.Asset is not null).OrderBy(t => t.Date).ThenBy(t => t.Id))
        {
            if (!estado.TryGetValue(t.AssetId, out var pos))
            {
                pos = new PosicaoAcumulada { Asset = t.Asset! };
                estado[t.AssetId] = pos;
            }

            var delta = DeltaQuantidade(t);
            if (delta > 0)
            {
                pos.Custo += t.Quantity * t.UnitPrice + t.Fees;
                pos.Quantidade += t.Quantity;
            }
            else if (delta < 0)
            {
                var precoMedio = pos.Quantidade > 0m ? pos.Custo / pos.Quantidade : 0m;
                var reduz = Math.Min(t.Quantity, pos.Quantidade);
                pos.Custo -= reduz * precoMedio;
                pos.Quantidade -= t.Quantity;
                if (pos.Quantidade <= 0.000000000001m)
                {
                    pos.Quantidade = 0m;
                    pos.Custo = 0m;
                }
            }
        }

        return estado;
    }

    private static IReadOnlyList<CotacaoAtivoDto> CriarAtivosCotadosDaTabela(IReadOnlyList<PosicaoAcumulada> posicoes, IReadOnlyList<CotacaoAtivoFinanceiro> cotacoes)
    {
        var cotacaoPorAtivo = cotacoes
            .GroupBy(x => x.AtivoFinanceiroId)
            .ToDictionary(x => x.Key, x => x.OrderByDescending(c => c.RetrievedAt).First());

        return posicoes
            .Select(pos =>
            {
                var asset = pos.Asset;
                cotacaoPorAtivo.TryGetValue(asset.Id, out var cotacao);
                var precoMedio = pos.Quantidade > 0m ? pos.Custo / pos.Quantidade : 0m;
                var precoAtual = cotacao is { PriceBRL: > 0m } ? cotacao.PriceBRL : (decimal?)null;
                var custo = pos.Custo;
                var valorMercado = precoAtual.HasValue ? pos.Quantidade * precoAtual.Value : custo;
                var resultado = valorMercado - custo;
                var percentual = custo == 0m ? 0m : resultado / custo * 100m;

                return new CotacaoAtivoDto(
                    asset.Id,
                    asset.Ticker ?? asset.AssetKey ?? asset.Name ?? string.Empty,
                    asset.AssetClass.ToString(),
                    cotacao?.Symbol ?? asset.Ticker ?? asset.AssetKey ?? string.Empty,
                    pos.Quantidade,
                    precoMedio,
                    precoAtual,
                    valorMercado,
                    custo,
                    resultado,
                    percentual,
                    cotacao?.ChangePercent,
                    cotacao?.RetrievedAt,
                    cotacao?.Status.ToString() ?? "SemCotacao",
                    "Calculada");
            })
            .OrderByDescending(x => x.ValorMercado)
            .ToList();
    }

    private static IReadOnlyList<CotacaoAtivoDto> CriarAtivosCotados(IReadOnlyList<EstimativaPosicaoCarteira> posicoes, IReadOnlyList<CotacaoAtivoFinanceiro> cotacoes)
    {
        var cotacaoPorAtivo = cotacoes
            .GroupBy(x => x.AtivoFinanceiroId)
            .ToDictionary(x => x.Key, x => x.OrderByDescending(c => c.RetrievedAt).First());

        return posicoes
            .Where(x => x.Status == StatusEstimativaPosicao.AbertaOuResidual && x.Asset is not null)
            .Select(posicao =>
            {
                cotacaoPorAtivo.TryGetValue(posicao.AssetId, out var cotacao);
                var precoAtual = cotacao?.PriceBRL;
                var custo = posicao.Quantity * posicao.AveragePrice;
                var valorMercado = precoAtual.HasValue ? posicao.Quantity * precoAtual.Value : posicao.EstimatedCurrentPosition;
                var resultado = valorMercado - custo;
                var percentual = custo == 0 ? 0 : resultado / custo * 100m;

                return new CotacaoAtivoDto(
                    posicao.AssetId,
                    posicao.Asset?.Ticker ?? posicao.Asset?.AssetKey ?? posicao.Asset?.Name ?? string.Empty,
                    posicao.Asset?.AssetClass.ToString() ?? string.Empty,
                    cotacao?.Symbol ?? posicao.Asset?.Ticker ?? posicao.Asset?.AssetKey ?? string.Empty,
                    posicao.Quantity,
                    posicao.AveragePrice,
                    precoAtual,
                    valorMercado,
                    custo,
                    resultado,
                    percentual,
                    cotacao?.ChangePercent,
                    cotacao?.RetrievedAt,
                    cotacao?.Status.ToString() ?? "SemCotacao",
                    posicao.ConfidenceLevel.ToString());
            })
            .OrderByDescending(x => x.ValorMercado)
            .ToList();
    }

    private static IReadOnlyList<CarteiraFinanceiraResumoDto> CriarResumoCarteiras(IReadOnlyList<CarteiraFinanceira> carteiras, IReadOnlyList<CotacaoAtivoDto> ativos, decimal valorPatrimonio)
    {
        var ativosPorId = ativos.ToDictionary(x => x.AtivoId);
        return carteiras
            .Select(carteira =>
            {
                var itens = carteira.Ativos
                    .Where(x => x.AtivoFinanceiroId > 0 && ativosPorId.ContainsKey(x.AtivoFinanceiroId))
                    .Select(x => ativosPorId[x.AtivoFinanceiroId])
                    .ToList();
                var valor = itens.Sum(x => x.ValorMercado);
                var custo = itens.Sum(x => x.CustoEstimado);
                var resultado = valor - custo;
                var variacaoDiaValor = itens.Sum(x => x.ValorMercado * ((x.VariacaoDiaPercentual ?? 0m) / 100m));
                var itensResumo = itens
                    .OrderByDescending(x => x.ValorMercado)
                    .Select(x => new CarteiraAtivoResumoDto(
                        x.AtivoId,
                        x.Ativo,
                        x.Classe,
                        x.Symbol,
                        x.Quantidade,
                        Math.Round(x.ValorMercado, 2),
                        valor == 0m ? 0m : Math.Round(x.ValorMercado / valor * 100m, 2),
                        x.VariacaoDiaPercentual,
                        Math.Round(x.ResultadoNaoRealizado, 2),
                        Math.Round(x.ResultadoNaoRealizadoPercentual, 2),
                        x.Status))
                    .ToList();

                return new CarteiraFinanceiraResumoDto(
                    carteira.Id,
                    carteira.Nome,
                    carteira.Tipo,
                    valor,
                    custo,
                    resultado,
                    custo == 0 ? 0 : resultado / custo * 100m,
                    valor == 0 ? 0 : variacaoDiaValor / valor * 100m,
                    valorPatrimonio == 0m ? 0m : valor / valorPatrimonio * 100m,
                    itens.Count,
                    itensResumo);
            })
            .Where(x => x.Ativos > 0)
            .OrderByDescending(x => x.ValorMercado)
            .ToList();
    }

}
