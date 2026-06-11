using System.Globalization;
using System.Text.Json;
using Sistema.APP.DTOs;
using Sistema.APP.Services.Interfaces;
using Sistema.CORE.Common;
using Sistema.CORE.Entities;
using Sistema.CORE.Repositories.Interfaces;

namespace Sistema.APP.Services;

public class MinhasFinancasAppService(IUnitOfWork uow, IMinhasFinancasImportador importador, IMinhasFinancasMarketDataService marketData) : IMinhasFinancasAppService
{
    private readonly IUnitOfWork _uow = uow;
    private readonly IMinhasFinancasImportador _importador = importador;
    private readonly IMinhasFinancasMarketDataService _marketData = marketData;

    public async Task<MinhasFinancasDashboardDto> ObterDashboardAsync(CancellationToken cancellationToken = default)
    {
        await _importador.GarantirCargaInicialAsync(cancellationToken);
        var carga = await _uow.MinhasFinancas.ObterCargaMaisRecenteAsync(cancellationToken);
        var alertas = await _uow.MinhasFinancas.BuscarAlertasAsync(cancellationToken);
        var posicoes = await _uow.MinhasFinancas.BuscarPosicoesAsync(null, cancellationToken);
        var cotacoes = await _uow.MinhasFinancas.BuscarCotacoesAtuaisAsync(cancellationToken);
        var carteiras = await _uow.MinhasFinancas.BuscarCarteirasComAtivosAsync(cancellationToken);
        var historico = await _uow.MinhasFinancas.BuscarHistoricoPrecosAsync(DateTime.UtcNow.Date.AddYears(-1), cancellationToken);
        var documentosMonitorados = await _uow.MinhasFinancas.BuscarDocumentosMonitoradosAsync(cancellationToken);
        var ultimaImportacao = await _uow.MinhasFinancas.ObterUltimaImportacaoArquivoAsync(cancellationToken);
        var ativosCotados = CriarAtivosCotados(posicoes, cotacoes);

        var dashboard = new MinhasFinancasDashboardDto
        {
            GeradoEm = carga?.GeneratedAt?.ToString("dd/MM/yyyy HH:mm", CultureInfo.GetCultureInfo("pt-BR")) ?? string.Empty,
            Fonte = carga?.SourcePath ?? string.Empty,
            DashboardJson = carga?.DashboardJson,
            Kpis = CriarKpis(carga),
            B3PorAno = (await _uow.MinhasFinancas.BuscarAgregadosAsync("b3-year", cancellationToken)).Select(MapSerie).ToList(),
            B3PorMes = (await _uow.MinhasFinancas.BuscarAgregadosAsync("b3-month", cancellationToken)).Select(MapSerie).ToList(),
            B3PorClasse = (await _uow.MinhasFinancas.BuscarAgregadosAsync("b3-class", cancellationToken)).Select(MapDistribuicao).ToList(),
            BinanceMoedas = (await _uow.MinhasFinancas.BuscarAgregadosAsync("binance-coin", cancellationToken)).Select(MapDistribuicao).Take(12).ToList(),
            UltimasOperacoesB3 = (await _uow.MinhasFinancas.BuscarUltimasOperacoesB3Async(12, cancellationToken)).Select(MapOperacaoB3).ToList(),
            UltimasTransacoesCripto = (await _uow.MinhasFinancas.BuscarUltimasTransacoesCriptoAsync(12, cancellationToken)).Select(MapTransacaoCripto).ToList(),
            PosicoesAbertas = posicoes.Where(p => p.Status == StatusEstimativaPosicao.AbertaOuResidual).Take(12).Select(MapPosicao).ToList(),
            PosicoesEncerradas = posicoes.Where(p => p.Status == StatusEstimativaPosicao.EncerradaPorOperacoes).Take(8).Select(MapPosicao).ToList(),
            Alertas = alertas.Take(8).Select(MapAlerta).ToList(),
            AtivosCotados = ativosCotados,
            Carteiras = CriarResumoCarteiras(carteiras, ativosCotados),
            Periodos = CriarPeriodos(posicoes, cotacoes, historico),
            ImportacaoArquivos = new ImportacaoFinanceiraResumoDto(
                ultimaImportacao?.FinishedAt ?? ultimaImportacao?.StartedAt,
                documentosMonitorados.Count,
                documentosMonitorados.Count(x => x.ParseStatus == StatusParseDocumentoFinanceiro.Processado || x.ParseStatus == StatusParseDocumentoFinanceiro.ParcialmenteProcessado),
                documentosMonitorados.Count(x => x.ParseStatus is StatusParseDocumentoFinanceiro.Falhou or StatusParseDocumentoFinanceiro.SemDadosEstruturados),
                ultimaImportacao?.SourceFolder),
            CotacoesAtualizadasEm = cotacoes.OrderByDescending(x => x.RetrievedAt).Select(x => (DateTime?)x.RetrievedAt).FirstOrDefault(),
            ValorMercadoTotal = ativosCotados.Sum(x => x.ValorMercado),
            CustoEstimadoTotal = ativosCotados.Sum(x => x.CustoEstimado),
            ResultadoNaoRealizadoTotal = ativosCotados.Sum(x => x.ResultadoNaoRealizado)
        };

        return dashboard;
    }

    public async Task<PagedResult<DocumentoFinanceiroDto>> BuscarDocumentosAsync(int page, int pageSize, string? termo, CancellationToken cancellationToken = default)
    {
        await _importador.GarantirCargaInicialAsync(cancellationToken);
        var result = await _uow.MinhasFinancas.BuscarDocumentosAsync(page, pageSize, termo, cancellationToken);
        return new PagedResult<DocumentoFinanceiroDto>(result.Items.Select(MapDocumento).ToList(), result.TotalCount, result.Page, result.PageSize);
    }

    public async Task<(DocumentoFinanceiroDto? Documento, IReadOnlyList<ConteudoBrutoFinanceiroDto> Conteudos)> ObterDocumentoAsync(int id, CancellationToken cancellationToken = default)
    {
        await _importador.GarantirCargaInicialAsync(cancellationToken);
        var documento = await _uow.MinhasFinancas.ObterDocumentoAsync(id, cancellationToken);
        if (documento is null)
            return (null, []);

        var conteudos = await _uow.MinhasFinancas.BuscarConteudosDocumentoAsync(id, cancellationToken);
        return (MapDocumento(documento), conteudos.Select(MapConteudo).ToList());
    }

    public async Task<PagedResult<OperacaoB3Dto>> BuscarOperacoesB3Async(int page, int pageSize, string? termo, int? ano, string? classe, CancellationToken cancellationToken = default)
    {
        await _importador.GarantirCargaInicialAsync(cancellationToken);
        var result = await _uow.MinhasFinancas.BuscarOperacoesB3Async(page, pageSize, termo, ano, classe, cancellationToken);
        return new PagedResult<OperacaoB3Dto>(result.Items.Select(MapOperacaoB3).ToList(), result.TotalCount, result.Page, result.PageSize);
    }

    public async Task<PagedResult<TransacaoCriptoDto>> BuscarTransacoesCriptoAsync(int page, int pageSize, string? termo, CancellationToken cancellationToken = default)
    {
        await _importador.GarantirCargaInicialAsync(cancellationToken);
        var result = await _uow.MinhasFinancas.BuscarTransacoesCriptoAsync(page, pageSize, termo, cancellationToken);
        return new PagedResult<TransacaoCriptoDto>(result.Items.Select(MapTransacaoCripto).ToList(), result.TotalCount, result.Page, result.PageSize);
    }

    public async Task<IReadOnlyList<PosicaoFinanceiraDto>> BuscarPosicoesAsync(bool? somenteAbertas, CancellationToken cancellationToken = default)
    {
        await _importador.GarantirCargaInicialAsync(cancellationToken);
        var result = await _uow.MinhasFinancas.BuscarPosicoesAsync(somenteAbertas, cancellationToken);
        return result.Select(MapPosicao).ToList();
    }

    public async Task<IReadOnlyList<AlertaConfiabilidadeDto>> BuscarAlertasAsync(CancellationToken cancellationToken = default)
    {
        await _importador.GarantirCargaInicialAsync(cancellationToken);
        var result = await _uow.MinhasFinancas.BuscarAlertasAsync(cancellationToken);
        return result.Select(MapAlerta).ToList();
    }

    public async Task ImportarPastaMonitoradaAsync(CancellationToken cancellationToken = default)
        => await _importador.ImportarPastaMonitoradaAsync(cancellationToken);

    public async Task AtualizarCotacoesAsync(CancellationToken cancellationToken = default)
        => await _marketData.AtualizarCotacoesAsync(force: true, cancellationToken);

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

    private static IReadOnlyList<CarteiraFinanceiraResumoDto> CriarResumoCarteiras(IReadOnlyList<CarteiraFinanceira> carteiras, IReadOnlyList<CotacaoAtivoDto> ativos)
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

                return new CarteiraFinanceiraResumoDto(
                    carteira.Id,
                    carteira.Nome,
                    carteira.Tipo,
                    valor,
                    custo,
                    resultado,
                    custo == 0 ? 0 : resultado / custo * 100m,
                    valor == 0 ? 0 : variacaoDiaValor / valor * 100m,
                    itens.Count);
            })
            .Where(x => x.Ativos > 0)
            .OrderByDescending(x => x.ValorMercado)
            .ToList();
    }

    private static IReadOnlyList<PeriodoPerformanceDto> CriarPeriodos(
        IReadOnlyList<EstimativaPosicaoCarteira> posicoes,
        IReadOnlyList<CotacaoAtivoFinanceiro> cotacoes,
        IReadOnlyList<PrecoHistoricoAtivoFinanceiro> historico)
    {
        var cotacaoPorAtivo = cotacoes
            .GroupBy(x => x.AtivoFinanceiroId)
            .ToDictionary(x => x.Key, x => x.OrderByDescending(c => c.RetrievedAt).First());
        var historicoPorAtivo = historico
            .GroupBy(x => x.AtivoFinanceiroId)
            .ToDictionary(x => x.Key, x => x.OrderBy(h => h.Date).ToList());

        var periodos = new (string Codigo, string Label, DateTime Inicio)[]
        {
            ("1D", "1 dia", DateTime.UtcNow.Date.AddDays(-1)),
            ("5D", "5 dias", DateTime.UtcNow.Date.AddDays(-5)),
            ("1W", "1 semana", DateTime.UtcNow.Date.AddDays(-7)),
            ("1M", "1 mês", DateTime.UtcNow.Date.AddMonths(-1)),
            ("3M", "3 meses", DateTime.UtcNow.Date.AddMonths(-3)),
            ("YTD", "No ano", new DateTime(DateTime.UtcNow.Year, 1, 1)),
            ("1Y", "1 ano", DateTime.UtcNow.Date.AddYears(-1))
        };

        var abertas = posicoes.Where(x => x.Status == StatusEstimativaPosicao.AbertaOuResidual).ToList();
        var atual = abertas.Sum(x => cotacaoPorAtivo.TryGetValue(x.AssetId, out var c) ? x.Quantity * c.PriceBRL : x.EstimatedCurrentPosition);

        return periodos.Select(periodo =>
        {
            var baseValor = abertas.Sum(posicao =>
            {
                if (!historicoPorAtivo.TryGetValue(posicao.AssetId, out var serie))
                    return 0m;

                var precoBase = serie.LastOrDefault(x => x.Date.Date <= periodo.Inicio.Date) ?? serie.FirstOrDefault();
                return precoBase is null ? 0m : posicao.Quantity * precoBase.CloseBRL;
            });

            var variacao = baseValor == 0 ? 0 : atual - baseValor;
            return new PeriodoPerformanceDto(periodo.Codigo, periodo.Label, baseValor == 0 ? 0 : variacao / baseValor * 100m, variacao);
        }).ToList();
    }
}
