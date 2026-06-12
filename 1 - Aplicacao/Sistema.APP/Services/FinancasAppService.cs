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
    private readonly IUnitOfWork _uow = uow;
    private readonly IFinancasImportador _importador = importador;
    private readonly IFinancasMarketDataService _marketData = marketData;
    private readonly ILogAppService _log = log;
    private readonly IMensagemAppService _mensagem = mensagem;
    private readonly IExecutionContext _execution = execution;

    private string UsuarioAtual => string.IsNullOrWhiteSpace(_execution.Usuario) ? "sistema" : _execution.Usuario!;

    public async Task<FinancasDashboardDto> ObterDashboardAsync(CancellationToken cancellationToken = default)
    {
        await _importador.GarantirCargaInicialAsync(cancellationToken);
        var carga = await _uow.Financas.ObterCargaMaisRecenteAsync(cancellationToken);
        var alertas = await _uow.Financas.BuscarAlertasAsync(cancellationToken);
        var posicoes = await _uow.Financas.BuscarPosicoesAsync(null, cancellationToken);
        var cotacoes = await _uow.Financas.BuscarCotacoesAtuaisAsync(cancellationToken);
        var carteiras = await _uow.Financas.BuscarCarteirasComAtivosAsync(cancellationToken);
        var historico = await _uow.Financas.BuscarHistoricoPrecosAsync(DateTime.UtcNow.Date.AddYears(-1), cancellationToken);
        var documentosMonitorados = await _uow.Financas.BuscarDocumentosMonitoradosAsync(cancellationToken);
        var ultimaImportacao = await _uow.Financas.ObterUltimaImportacaoArquivoAsync(cancellationToken);
        var transacoes = await _uow.Financas.BuscarTodasTransacoesAsync(cancellationToken);
        var posicoesTabela = CalcularPosicoes(transacoes).Values.Where(p => p.Quantidade > 0.000000001m).ToList();
        var ativosCotados = CriarAtivosCotadosDaTabela(posicoesTabela, cotacoes);

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

    public Task<ValidacaoAtivoResultado> ValidarAtivoAsync(string ticker, CancellationToken cancellationToken = default)
        => _marketData.ValidarAtivoAsync(ticker, cancellationToken);

    public async Task<EvolucaoPatrimonioDto> ObterEvolucaoPatrimonioAsync(CancellationToken cancellationToken = default)
    {
        await _importador.GarantirCargaInicialAsync(cancellationToken);

        var hoje = DateTime.UtcNow.Date;
        var inicio = hoje.AddYears(-1);
        var totalDias = (hoje - inicio).Days + 1;
        var datas = Enumerable.Range(0, totalDias).Select(i => inicio.AddDays(i)).ToList();

        var transacoes = await _uow.Financas.BuscarTodasTransacoesAsync(cancellationToken);
        var historico = await _uow.Financas.BuscarHistoricoPrecosAsync(inicio, cancellationToken);
        var carteiras = await _uow.Financas.BuscarCarteirasComAtivosAsync(cancellationToken);

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
            precosPorAtivo.TryGetValue(grupo.Key, out var candles);
            var setor = setorPorAtivo.TryGetValue(grupo.Key, out var s) ? s : "Outros";
            if (!seriesSetor.TryGetValue(setor, out var arrSetor))
            {
                arrSetor = new decimal[totalDias];
                seriesSetor[setor] = arrSetor;
            }

            decimal quantidade = 0m;
            decimal ultimoPreco = 0m;
            int ti = 0, ci = 0;

            for (int di = 0; di < totalDias; di++)
            {
                var d = datas[di];
                while (ti < txs.Count && txs[ti].Date.Date <= d)
                {
                    quantidade += DeltaQuantidade(txs[ti]);
                    ti++;
                }
                if (candles is not null)
                    while (ci < candles.Count && candles[ci].Date <= d)
                    {
                        ultimoPreco = candles[ci].Close;
                        ci++;
                    }

                var valor = quantidade > 0.000000000001m ? quantidade * ultimoPreco : 0m;
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
        var cotacoes = await _uow.Financas.BuscarCotacoesAtuaisAsync(cancellationToken);
        var cotacaoPorAtivo = cotacoes
            .GroupBy(c => c.AtivoFinanceiroId)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(c => c.RetrievedAt).First());
        var posicoesAtuais = CalcularPosicoes(transacoes);

        var diaPorSetor = new Dictionary<string, (decimal Valor, decimal VarValor)>(StringComparer.OrdinalIgnoreCase);
        decimal valorVivoTotal = 0m, varDiaValorTotal = 0m;
        foreach (var pos in posicoesAtuais.Values.Where(p => p.Quantidade > 0m))
        {
            if (!cotacaoPorAtivo.TryGetValue(pos.Asset.Id, out var cot) || cot.PriceBRL <= 0m)
                continue;

            var valorAtual = pos.Quantidade * cot.PriceBRL;
            var varValor = valorAtual * ((cot.ChangePercent ?? 0m) / 100m);
            var setorAtivo = setorPorAtivo.TryGetValue(pos.Asset.Id, out var s) ? s : "Outros";
            diaPorSetor.TryGetValue(setorAtivo, out var acc);
            diaPorSetor[setorAtivo] = (acc.Valor + valorAtual, acc.VarValor + varValor);
            valorVivoTotal += valorAtual;
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

        var variacaoDiaTotal = valorVivoTotal == 0m ? 0m : Math.Round(varDiaValorTotal / valorVivoTotal * 100m, 2);

        return new EvolucaoPatrimonioDto(
            datas.Select(d => d.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)).ToList(),
            total.Select(v => Math.Round(v, 2)).ToList(),
            variacaoDiaTotal,
            Math.Round(valorVivoTotal, 2),
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
        decimal custoTotal = 0m, valorTotal = 0m;
        foreach (var (ativoId, pos) in estado)
        {
            if (pos.Quantidade <= 0m && pos.RealizadoPeriodo == 0m)
                continue;

            var precoMedio = pos.Quantidade > 0m ? pos.Custo / pos.Quantidade : 0m;
            precoAtualPorAtivo.TryGetValue(ativoId, out var preco);
            decimal? precoAtual = preco > 0m ? preco : null;
            var valorMercado = pos.Quantidade * (precoAtual ?? precoMedio);
            var pl = valorMercado - pos.Custo;
            var plPercentual = pos.Custo > 0m ? pl / pos.Custo * 100m : 0m;
            custoTotal += pos.Custo;
            valorTotal += valorMercado;

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
                Math.Round(pos.RealizadoPeriodo, 2)));
        }

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
            Math.Round(valorTotal - custoTotal, 2),
            ativos.OrderByDescending(a => a.ValorMercado).ToList(),
            vendas.OrderByDescending(v => v.Data).ToList());
    }

    private static string TickerDe(AtivoFinanceiro a) => a.Ticker ?? a.AssetKey ?? a.Name ?? string.Empty;

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
