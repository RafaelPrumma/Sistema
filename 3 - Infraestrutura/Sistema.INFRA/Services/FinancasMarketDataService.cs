using System.Globalization;
using System.Net;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Sistema.APP.DTOs;
using Sistema.APP.Services.Interfaces;
using Sistema.CORE.Entities;
using Sistema.INFRA.Data;

namespace Sistema.INFRA.Services;

public class FinancasMarketDataService(
    AppDbContext context,
    IHttpClientFactory httpClientFactory,
    IConfiguracaoLeitura config,
    ILogger<FinancasMarketDataService> logger) : IFinancasMarketDataService
{
    private const string UsuarioSistema = "market-data";
    private readonly AppDbContext _context = context;
    private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;
    private readonly IConfiguracaoLeitura _config = config;
    private readonly ILogger<FinancasMarketDataService> _logger = logger;

    public async Task AtualizarCotacoesAsync(bool force = false, CancellationToken cancellationToken = default)
    {
        // Cota os ativos com posição líquida > 0 na tabela única de transações (B3 + cripto + manuais).
        var movimentos = await _context.TransacoesFinanceiras
            .Where(x => x.IsCanonical && x.Asset != null)
            .Select(x => new { x.AssetId, x.OperationType, x.Quantity, Asset = x.Asset! })
            .ToListAsync(cancellationToken);

        if (movimentos.Count == 0)
            return;

        var saldos = new Dictionary<int, (decimal Saldo, AtivoFinanceiro Asset)>();
        foreach (var m in movimentos)
        {
            var delta = m.OperationType switch
            {
                TipoOperacaoFinanceira.Compra or TipoOperacaoFinanceira.Deposito or TipoOperacaoFinanceira.Rendimento => m.Quantity,
                TipoOperacaoFinanceira.Venda or TipoOperacaoFinanceira.Saque or TipoOperacaoFinanceira.Taxa => -m.Quantity,
                _ => 0m
            };
            saldos.TryGetValue(m.AssetId, out var atual);
            saldos[m.AssetId] = (atual.Saldo + delta, m.Asset);
        }

        var ativos = saldos.Values.Where(x => x.Saldo > 0.000000001m).Select(x => x.Asset).ToList();
        if (ativos.Count == 0)
            return;

        await AtualizarB3Async(ativos.Where(x => !x.IsCrypto).ToList(), force, cancellationToken);
        await AtualizarCriptoAsync(ativos.Where(x => x.IsCrypto).ToList(), force, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task AtualizarProventosAsync(bool force = false, CancellationToken cancellationToken = default)
    {
        var algumGravado = await AtualizarProventosB3Async(cancellationToken);
        algumGravado |= await AtualizarProventosCriptoEarnAsync(cancellationToken);
        if (algumGravado)
            await _context.SaveChangesAsync(cancellationToken);
    }

    // Proventos de renda variável B3 (dividendo/JCP/rendimento) via Brapi, cruzados com a posição na data-com.
    private async Task<bool> AtualizarProventosB3Async(CancellationToken cancellationToken)
    {
        // Reconstrói a linha do tempo de quantidade por ativo (a partir da tabela única) para saber
        // quanto eu detinha na data-com de cada provento. Só renda variável B3 (Brapi paga proventos).
        var movimentos = await _context.TransacoesFinanceiras
            .Where(x => x.IsCanonical && x.Asset != null && !x.Asset!.IsCrypto)
            .Select(x => new { x.AssetId, x.OperationType, x.Quantity, x.Date, Asset = x.Asset! })
            .ToListAsync(cancellationToken);
        if (movimentos.Count == 0)
            return false;

        var timelines = new Dictionary<int, (AtivoFinanceiro Asset, List<(DateTime Date, decimal Delta)> Movs)>();
        foreach (var m in movimentos)
        {
            var delta = m.OperationType switch
            {
                TipoOperacaoFinanceira.Compra or TipoOperacaoFinanceira.Deposito or TipoOperacaoFinanceira.Rendimento => m.Quantity,
                TipoOperacaoFinanceira.Venda or TipoOperacaoFinanceira.Saque or TipoOperacaoFinanceira.Taxa => -m.Quantity,
                _ => 0m
            };
            if (!timelines.TryGetValue(m.AssetId, out var t))
            {
                t = (m.Asset, new List<(DateTime, decimal)>());
                timelines[m.AssetId] = t;
            }
            t.Movs.Add((m.Date.Date, delta));
        }

        // Busca proventos de quem já esteve em carteira com saldo positivo; vender depois da
        // data-com não elimina o direito ao provento.
        var emCarteira = timelines.Where(kv => TeveSaldoPositivo(kv.Value.Movs)).ToList();
        if (emCarteira.Count == 0)
            return false;

        var token = await _config.ObterTextoAsync("Financas", "MarketData:BrapiToken", null, cancellationToken);
        var client = _httpClientFactory.CreateClient("Brapi");
        var limiteInferior = DateTime.UtcNow.Date.AddYears(-3);
        var algumGravado = false;

        foreach (var (assetId, dados) in emCarteira)
        {
            var symbol = ResolverTickerB3(dados.Asset);
            if (string.IsNullOrWhiteSpace(symbol))
                continue;
            // Sem token, a Brapi não devolve proventos da maioria dos tickers (mesma limitação da cotação).
            if (string.IsNullOrWhiteSpace(token) && !IsBrapiFreeTicker(symbol))
                continue;

            var movs = dados.Movs.OrderBy(x => x.Date).ToList();
            decimal QtdEm(DateTime data) => movs.Where(x => x.Date <= data.Date).Sum(x => x.Delta);

            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, $"api/quote/{WebUtility.UrlEncode(symbol)}?dividends=true");
                if (!string.IsNullOrWhiteSpace(token))
                    request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

                using var response = await client.SendAsync(request, cancellationToken);
                if (!response.IsSuccessStatusCode)
                    continue;

                using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
                if (!doc.RootElement.TryGetProperty("results", out var results) || results.ValueKind != JsonValueKind.Array || results.GetArrayLength() == 0)
                    continue;
                if (!results[0].TryGetProperty("dividendsData", out var dividendsData) || dividendsData.ValueKind != JsonValueKind.Object)
                    continue;
                if (!dividendsData.TryGetProperty("cashDividends", out var cash) || cash.ValueKind != JsonValueKind.Array)
                    continue;

                foreach (var ev in cash.EnumerateArray())
                {
                    var rate = GetDecimal(ev, "rate") ?? 0m;
                    if (rate <= 0m)
                        continue;
                    var pagamento = TryParseDateTime(GetString(ev, "paymentDate"));
                    if (pagamento is null || pagamento.Value.Date < limiteInferior)
                        continue;

                    var dataCom = TryParseDateTime(GetString(ev, "lastDatePrior")) ?? pagamento;
                    var qtd = QtdEm(dataCom.Value);
                    if (qtd <= 0.000000001m)
                        continue; // não detinha o ativo na data-com → não recebeu.

                    var label = (GetString(ev, "label") ?? GetString(ev, "relatedTo") ?? "Provento").Trim();
                    var bruto = Math.Round(qtd * rate, 2);
                    var (tipo, tributacao, irrf) = ClassificarProvento(label, bruto);
                    algumGravado |= UpsertProvento(
                        assetId,
                        pagamento.Value.Date,
                        dataCom.Value.Date,
                        tipo,
                        "Brapi",
                        "Brapi",
                        qtd,
                        rate,
                        bruto,
                        irrf,
                        "BRL",
                        tributacao,
                        ev.GetRawText());
                }
            }
            catch (Exception ex)
            {
                FinancasMarketDataLogMessages.FalhaCotacao(_logger, "Brapi/proventos", ex.Message);
            }
        }

        return algumGravado;
    }

    private static bool TeveSaldoPositivo(IEnumerable<(DateTime Date, decimal Delta)> movimentos)
    {
        var saldo = 0m;
        foreach (var mov in movimentos.OrderBy(x => x.Date))
        {
            saldo += mov.Delta;
            if (saldo > 0.000000001m)
                return true;
        }

        return false;
    }

    // Earn/staking/rewards da Binance: ficam no staging cripto (sem preço → não entram como trade).
    // Aqui entram como PROVENTO, valorados em BRL pelo preço da data do recebimento (histórico diário,
    // com fallback na cotação atual). Não alteram a quantidade da posição — é renda, como o dividendo.
    private async Task<bool> AtualizarProventosCriptoEarnAsync(CancellationToken cancellationToken)
    {
        var earns = await _context.TransacoesCripto
            .Where(x => x.Amount > 0m && x.TransactionDate != null &&
                (x.OperationType == TipoOperacaoFinanceira.Rendimento ||
                 x.RawType.Contains("EARN") || x.RawType.Contains("STAK") || x.RawType.Contains("REWARD") ||
                 x.RawType.Contains("INTEREST") || x.RawType.Contains("DISTRIBUTION") || x.RawType.Contains("AIRDROP") ||
                 x.RawType.Contains("SAVINGS")))
            .ToListAsync(cancellationToken);
        if (earns.Count == 0)
            return false;

        var ativos = (await _context.AtivosFinanceiros.Where(a => a.IsCrypto).ToListAsync(cancellationToken))
            .GroupBy(a => a.AssetKey, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);
        if (ativos.Count == 0)
            return false;

        var assetIds = ativos.Values.Select(a => a.Id).ToHashSet();

        // Earn que já virou trade canônico (raro: tinha preço) é ignorado aqui para não contar em dobro.
        var materializados = (await _context.TransacoesFinanceiras
                .Where(x => x.StagingTipo == "TransacaoCripto" && x.StagingId != null)
                .Select(x => x.StagingId!.Value)
                .ToListAsync(cancellationToken))
            .ToHashSet();

        // Preço histórico diário (BRL) por ativo para valorar o earn na data; fallback na cotação atual.
        var historico = (await _context.PrecosHistoricosAtivosFinanceiros
                .Where(x => x.Provedor == ProvedorCotacao.Binance && x.Interval == "1d" && assetIds.Contains(x.AtivoFinanceiroId))
                .Select(x => new { x.AtivoFinanceiroId, x.Date, x.CloseBRL })
                .ToListAsync(cancellationToken))
            .GroupBy(x => x.AtivoFinanceiroId)
            .ToDictionary(g => g.Key, g => g.OrderBy(x => x.Date).Select(x => (x.Date, x.CloseBRL)).ToList());

        var cotacaoAtual = (await _context.CotacoesAtivosFinanceiros
                .Where(x => x.Provedor == ProvedorCotacao.Binance && x.PriceBRL > 0m && assetIds.Contains(x.AtivoFinanceiroId))
                .Select(x => new { x.AtivoFinanceiroId, x.PriceBRL, x.RetrievedAt })
                .ToListAsync(cancellationToken))
            .GroupBy(x => x.AtivoFinanceiroId)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(x => x.RetrievedAt).First().PriceBRL);

        decimal? PrecoEm(int assetId, DateTime data)
        {
            if (historico.TryGetValue(assetId, out var serie) && serie.Count > 0)
            {
                decimal? close = null;
                foreach (var (d, c) in serie)
                {
                    if (d.Date <= data.Date) close = c;
                    else break;
                }
                if (close is > 0m) return close;
                if (serie[0].CloseBRL > 0m) return serie[0].CloseBRL; // earn anterior ao 1º candle: usa o mais antigo.
            }
            return cotacaoAtual.TryGetValue(assetId, out var atual) ? atual : (decimal?)null;
        }

        var algum = false;
        foreach (var tx in earns)
        {
            if (materializados.Contains(tx.Id))
                continue;
            if (string.IsNullOrWhiteSpace(tx.AssetSymbol) || !ativos.TryGetValue(tx.AssetSymbol, out var ativo))
                continue;

            var data = tx.TransactionDate!.Value.Date;
            var qtd = tx.Amount;
            var preco = tx.Total is > 0m ? tx.Total!.Value / qtd : PrecoEm(ativo.Id, data);
            var valorBrl = tx.Total is > 0m ? Math.Round(tx.Total!.Value, 2) : (preco.HasValue ? Math.Round(qtd * preco.Value, 2) : 0m);
            if (valorBrl <= 0m)
                continue; // sem preço para valorar — não dá pra somar no retorno; fica fora.
            // Trava de sanidade: nenhum earn diário de varejo chega a R$50k. Acima disso é parsing
            // suspeito (ex.: notação científica mal lida) — ignora para não poluir o retorno.
            if (valorBrl > 50000m)
            {
                FinancasMarketDataLogMessages.FalhaCotacao(_logger, "Binance/earn", $"valor suspeito ignorado: {tx.AssetSymbol} {qtd} = R$ {valorBrl}");
                continue;
            }

            algum |= UpsertProvento(
                ativo.Id,
                data,
                data,
                "Rendimento (Earn)",
                "Binance",
                "Binance",
                qtd,
                preco,
                valorBrl,
                0m,
                "BRL",
                "Tributável (cripto)",
                tx.RawJson);
        }

        return algum;
    }

    private bool UpsertProvento(
        int assetId,
        DateTime? pagamento,
        DateTime? referencia,
        string tipo,
        string source,
        string fonte,
        decimal? quantidade,
        decimal? valorPorAcao,
        decimal valor,
        decimal irrf,
        string currency,
        string tributacao,
        string rawJson)
    {
        var chave = ProventoDedup.ChaveEconomica(assetId, referencia, pagamento, tipo);
        var provento = _context.RendimentosInvestimento.Local.FirstOrDefault(x => x.ChaveNatural == chave)
            ?? _context.RendimentosInvestimento.FirstOrDefault(x => x.ChaveNatural == chave);
        if (provento is not null)
        {
            if (!ProventoDedup.MesmoValor(provento.Amount, valor))
            {
                FinancasMarketDataLogMessages.ProventoDivergente(_logger, chave, provento.Amount, valor, fonte);
                return false;
            }

            provento.Fonte = provento.Fonte.Contains(fonte, StringComparison.OrdinalIgnoreCase)
                ? provento.Fonte
                : string.IsNullOrWhiteSpace(provento.Fonte) ? fonte : $"{provento.Fonte}+{fonte}";
            return false;
        }

        _context.RendimentosInvestimento.Add(new RendimentoInvestimento
        {
            AssetId = assetId,
            PaymentDate = pagamento?.Date,
            ReferenceDate = referencia?.Date,
            IncomeType = tipo,
            Source = source,
            Fonte = fonte,
            Quantity = quantidade,
            RatePerShare = valorPorAcao,
            Amount = valor,
            TaxWithheld = irrf,
            Currency = currency,
            Taxation = tributacao,
            ChaveNatural = chave,
            RawJson = rawJson,
            UsuarioInclusao = UsuarioSistema
        });
        return true;
    }

    // Classifica o provento pela descrição da Brapi e devolve (tipo, tributação, IRRF retido).
    // Dividendo é isento p/ PF; JCP tem 15% retido na fonte; rendimento de FII é isento p/ PF.
    private static (string Tipo, string Tributacao, decimal Irrf) ClassificarProvento(string label, decimal bruto)
    {
        var l = (label ?? string.Empty).ToUpperInvariant();
        if (l.Contains("JCP") || l.Contains("JRS") || l.Contains("JURO") || l.Contains("CAPITAL PROPRIO") || l.Contains("CAPITAL PRÓPRIO"))
            return ("JCP", "JCP (15% IRRF)", Math.Round(bruto * 0.15m, 2));
        if (l.Contains("RENDIMENTO"))
            return ("Rendimento", "Isento (FII)", 0m);
        if (l.Contains("DIVIDENDO") || l.Contains("DIVIDEND"))
            return ("Dividendo", "Isento", 0m);
        return (string.IsNullOrWhiteSpace(label) ? "Provento" : label!, string.Empty, 0m);
    }

    public async Task<ValidacaoAtivoResultado> ValidarAtivoAsync(string ticker, CancellationToken cancellationToken = default)
    {
        var symbol = (ticker ?? string.Empty).Trim().ToUpperInvariant();
        if (string.IsNullOrWhiteSpace(symbol))
            return new ValidacaoAtivoResultado(false, symbol, string.Empty, string.Empty, string.Empty, false, null, "Informe um ticker.");

        var pareceB3 = System.Text.RegularExpressions.Regex.IsMatch(symbol, @"^[A-Z]{4}\d{1,2}$");

        if (pareceB3)
            return await ValidarB3Async(symbol, cancellationToken)
                ?? await ValidarCriptoAsync(symbol, cancellationToken)
                ?? Invalido(symbol);

        return await ValidarCriptoAsync(symbol, cancellationToken)
            ?? await ValidarB3Async(symbol, cancellationToken)
            ?? Invalido(symbol);
    }

    private static ValidacaoAtivoResultado Invalido(string symbol)
        => new(false, symbol, string.Empty, string.Empty, string.Empty, false, null,
            "Ativo não encontrado na B3 (Brapi) nem na Binance. Verifique o ticker.");

    private async Task<ValidacaoAtivoResultado?> ValidarB3Async(string symbol, CancellationToken cancellationToken)
    {
        var pareceB3 = System.Text.RegularExpressions.Regex.IsMatch(symbol, @"^[A-Z]{4}\d{1,2}$");
        var token = await _config.ObterTextoAsync("Financas", "MarketData:BrapiToken", null, cancellationToken);
        var client = _httpClientFactory.CreateClient("Brapi");

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, $"api/quote/{WebUtility.UrlEncode(symbol)}?range=1d&interval=1d");
            if (!string.IsNullOrWhiteSpace(token))
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            using var response = await client.SendAsync(request, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
                if (doc.RootElement.TryGetProperty("results", out var results) && results.ValueKind == JsonValueKind.Array && results.GetArrayLength() > 0)
                {
                    var r = results[0];
                    var temErro = r.TryGetProperty("error", out var erro) && erro.ValueKind == JsonValueKind.True;
                    var sym = GetString(r, "symbol");
                    if (!temErro && !string.IsNullOrWhiteSpace(sym))
                    {
                        var nome = GetString(r, "longName") ?? GetString(r, "shortName") ?? sym;
                        var preco = GetDecimal(r, "regularMarketPrice");
                        return new ValidacaoAtivoResultado(true, sym, nome, ClassificarB3(sym, nome).ToString(), "Brapi", false, preco, null);
                    }
                }
            }

            // Quote indisponível (sem token / ticker pago): confirma a existência na lista pública da B3.
            var existe = await ExisteNaListaBrapiAsync(symbol, client, cancellationToken);
            if (existe == true)
                return new ValidacaoAtivoResultado(true, symbol, symbol, ClassificarB3(symbol, symbol).ToString(), "Brapi", false, null,
                    "Validado pela lista da B3. Configure o token Brapi para preço e histórico completos.");
            if (existe == false)
                return null;

            // Lista indisponível agora: não bloqueia um ticker no formato estrito da B3.
            if (pareceB3)
                return new ValidacaoAtivoResultado(true, symbol, symbol, ClassificarB3(symbol, symbol).ToString(), "Brapi", false, null,
                    "Cadastrado pelo padrão B3 (não foi possível confirmar na API agora).");
            return null;
        }
        catch (Exception ex)
        {
            FinancasMarketDataLogMessages.FalhaCotacao(_logger, "Brapi/validação", ex.Message);
            return null;
        }
    }

    // true = existe na B3; false = a lista respondeu e não contém; null = não foi possível consultar.
    private async Task<bool?> ExisteNaListaBrapiAsync(string symbol, HttpClient client, CancellationToken cancellationToken)
    {
        try
        {
            using var resp = await client.GetAsync($"api/available?search={WebUtility.UrlEncode(symbol)}", cancellationToken);
            if (!resp.IsSuccessStatusCode)
                return null;

            using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync(cancellationToken));
            if (doc.RootElement.TryGetProperty("stocks", out var stocks) && stocks.ValueKind == JsonValueKind.Array)
            {
                foreach (var s in stocks.EnumerateArray())
                    if (string.Equals(s.GetString(), symbol, StringComparison.OrdinalIgnoreCase))
                        return true;
                return false;
            }

            return null;
        }
        catch
        {
            return null;
        }
    }

    private async Task<ValidacaoAtivoResultado?> ValidarCriptoAsync(string symbol, CancellationToken cancellationToken)
    {
        foreach (var quote in new[] { "BRL", "USDT" })
        {
            var ticker = await ObterTickerBinanceAsync($"{symbol}{quote}", cancellationToken);
            if (ticker is null || ticker.Price <= 0)
                continue;

            decimal? precoBrl = ticker.Price;
            if (!string.Equals(quote, "BRL", StringComparison.OrdinalIgnoreCase))
            {
                var usdtBrl = await ObterTickerBinanceAsync("USDTBRL", cancellationToken);
                precoBrl = usdtBrl?.Price > 0 ? ticker.Price * usdtBrl.Price : null;
            }

            return new ValidacaoAtivoResultado(true, symbol, symbol, ClasseAtivo.Cripto.ToString(), "Binance", true, precoBrl, null);
        }

        return null;
    }

    private static ClasseAtivo ClassificarB3(string ticker, string? nome)
    {
        var t = (ticker ?? string.Empty).ToUpperInvariant();
        var n = (nome ?? string.Empty).ToUpperInvariant();

        if (t.EndsWith("34", StringComparison.Ordinal) ||
            t.EndsWith("35", StringComparison.Ordinal) ||
            t.EndsWith("32", StringComparison.Ordinal) ||
            t.EndsWith("33", StringComparison.Ordinal) ||
            t.EndsWith("39", StringComparison.Ordinal))
            return ClasseAtivo.BDR;

        if (t.EndsWith("11", StringComparison.Ordinal))
        {
            if (n.Contains("FII") || n.Contains("FDO") || n.Contains("IMOB") || n.Contains("RECEB") || n.Contains("FUNDO") && n.Contains("INVEST IMOB"))
                return ClasseAtivo.FII;
            if (n.Contains("ETF") || n.Contains("INDEX") || n.Contains("ISHARES") || n.Contains("TREND") || n.Contains("IT NOW"))
                return ClasseAtivo.ETF;
            return ClasseAtivo.FII;
        }

        return ClasseAtivo.Acao;
    }

    public async Task GarantirCotacaoAtivoAsync(int ativoId, CancellationToken cancellationToken = default)
    {
        var ativo = await _context.AtivosFinanceiros.FirstOrDefaultAsync(x => x.Id == ativoId, cancellationToken);
        if (ativo is null)
            return;

        if (ativo.IsCrypto)
            await AtualizarCriptoAsync(new[] { ativo }, force: true, cancellationToken);
        else
            await AtualizarB3Async(new[] { ativo }, force: true, cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);
    }

    private async Task AtualizarB3Async(IReadOnlyList<AtivoFinanceiro> ativos, bool force, CancellationToken cancellationToken)
    {
        var symbols = ativos
            .Select(ResolverTickerB3)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (symbols.Count == 0)
            return;

        var staleSymbols = await FiltrarSymbolsStaleAsync(symbols, ProvedorCotacao.Brapi, TimeSpan.FromMinutes(5), force, cancellationToken);
        if (staleSymbols.Count == 0)
            return;

        var token = await _config.ObterTextoAsync("Financas", "MarketData:BrapiToken", null, cancellationToken);
        if (string.IsNullOrWhiteSpace(token) && staleSymbols.Any(x => !IsBrapiFreeTicker(x)))
        {
            await MarcarSemTokenAsync(ativos, ProvedorCotacao.Brapi, staleSymbols, cancellationToken);
            return;
        }

        var client = _httpClientFactory.CreateClient("Brapi");
        using var request = new HttpRequestMessage(HttpMethod.Get, $"api/quote/{string.Join(',', staleSymbols)}?range=1mo&interval=1d");
        if (!string.IsNullOrWhiteSpace(token))
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        try
        {
            using var response = await client.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                await MarcarFalhaAsync(ativos, ProvedorCotacao.Brapi, staleSymbols, $"Brapi HTTP {(int)response.StatusCode}", cancellationToken);
                return;
            }

            using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
            if (!doc.RootElement.TryGetProperty("results", out var results) || results.ValueKind != JsonValueKind.Array)
                return;

            foreach (var result in results.EnumerateArray())
            {
                var symbol = GetString(result, "symbol");
                if (string.IsNullOrWhiteSpace(symbol))
                    continue;

                var ativo = ativos.FirstOrDefault(x => string.Equals(ResolverTickerB3(x), symbol, StringComparison.OrdinalIgnoreCase));
                if (ativo is null)
                    continue;

                var price = GetDecimal(result, "regularMarketPrice") ?? 0m;
                var marketTime = TryParseDateTime(GetString(result, "regularMarketTime"));
                UpsertCotacao(ativo.Id, ProvedorCotacao.Brapi, symbol, GetString(result, "currency") ?? "BRL", price, price, GetDecimal(result, "regularMarketChange"), GetDecimal(result, "regularMarketChangePercent"), marketTime, StatusCotacao.Atual, null, result.GetRawText(), TimeSpan.FromMinutes(5));
                await ImportarHistoricoBrapiAsync(ativo.Id, symbol, result, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            FinancasMarketDataLogMessages.FalhaCotacao(_logger, "Brapi", ex.Message);
            await MarcarFalhaAsync(ativos, ProvedorCotacao.Brapi, staleSymbols, ex.Message, cancellationToken);
        }
    }

    private async Task AtualizarCriptoAsync(IReadOnlyList<AtivoFinanceiro> ativos, bool force, CancellationToken cancellationToken)
    {
        if (ativos.Count == 0)
            return;

        var symbols = ativos.Select(x => x.AssetKey.ToUpperInvariant()).Distinct().ToList();
        var staleSymbols = await FiltrarSymbolsStaleAsync(symbols, ProvedorCotacao.Binance, TimeSpan.FromSeconds(45), force, cancellationToken);
        if (staleSymbols.Count == 0)
            return;

        var usdtBrl = await ObterTickerBinanceAsync("USDTBRL", cancellationToken);
        var brlRate = usdtBrl?.Price > 0 ? usdtBrl.Price : 1m;

        foreach (var assetSymbol in staleSymbols)
        {
            var ativo = ativos.FirstOrDefault(x => string.Equals(x.AssetKey, assetSymbol, StringComparison.OrdinalIgnoreCase));
            if (ativo is null)
                continue;

            var quoteSymbol = $"{assetSymbol}BRL";
            var ticker = await ObterTickerBinanceAsync(quoteSymbol, cancellationToken);
            var currency = "BRL";
            var priceBrl = ticker?.Price ?? 0m;

            if (ticker is null || ticker.Price <= 0)
            {
                quoteSymbol = $"{assetSymbol}USDT";
                ticker = await ObterTickerBinanceAsync(quoteSymbol, cancellationToken);
                currency = "USDT";
                priceBrl = (ticker?.Price ?? 0m) * brlRate;
            }

            if (ticker is null || ticker.Price <= 0)
            {
                UpsertCotacao(ativo.Id, ProvedorCotacao.Binance, quoteSymbol, currency, 0m, 0m, null, null, null, StatusCotacao.NaoSuportada, "Par não encontrado na Binance", "{}", TimeSpan.FromSeconds(45));
                continue;
            }

            UpsertCotacao(ativo.Id, ProvedorCotacao.Binance, quoteSymbol, currency, ticker.Price, priceBrl, ticker.Change, ticker.ChangePercent, ticker.MarketTime, StatusCotacao.Atual, null, ticker.RawJson, TimeSpan.FromSeconds(45));
            await ImportarHistoricoBinanceAsync(ativo.Id, quoteSymbol, currency, brlRate, cancellationToken);
        }
    }

    private async Task<IReadOnlyList<string>> FiltrarSymbolsStaleAsync(IReadOnlyList<string> symbols, ProvedorCotacao provedor, TimeSpan freshness, bool force, CancellationToken cancellationToken)
    {
        if (force)
            return symbols;

        var limite = DateTime.UtcNow.Subtract(freshness);
        var frescos = await _context.CotacoesAtivosFinanceiros
            .Where(x => x.Provedor == provedor && x.RetrievedAt >= limite && symbols.Contains(x.Symbol))
            .Select(x => x.Symbol)
            .ToListAsync(cancellationToken);

        return symbols.Except(frescos, StringComparer.OrdinalIgnoreCase).ToList();
    }

    private async Task<BinanceTicker?> ObterTickerBinanceAsync(string symbol, CancellationToken cancellationToken)
    {
        var client = _httpClientFactory.CreateClient("Binance");
        using var response = await client.GetAsync($"api/v3/ticker/24hr?symbol={WebUtility.UrlEncode(symbol)}", cancellationToken);
        if (!response.IsSuccessStatusCode)
            return null;

        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        return new BinanceTicker(
            symbol,
            GetDecimal(root, "lastPrice") ?? 0m,
            GetDecimal(root, "priceChange"),
            GetDecimal(root, "priceChangePercent"),
            DateTimeOffset.FromUnixTimeMilliseconds((long)(GetDecimal(root, "closeTime") ?? 0m)).UtcDateTime,
            json);
    }

    private async Task ImportarHistoricoBinanceAsync(int ativoId, string symbol, string currency, decimal brlRate, CancellationToken cancellationToken)
    {
        const string interval = "1d";
        if (await HistoricoSincronizadoHojeAsync(ativoId, ProvedorCotacao.Binance, interval, cancellationToken))
            return;

        var client = _httpClientFactory.CreateClient("Binance");
        using var response = await client.GetAsync($"api/v3/klines?symbol={WebUtility.UrlEncode(symbol)}&interval={interval}&limit=370", cancellationToken);
        if (!response.IsSuccessStatusCode)
            return;

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
        if (doc.RootElement.ValueKind != JsonValueKind.Array)
            return;

        var candles = new List<HistoricoCandle>();
        foreach (var row in doc.RootElement.EnumerateArray())
        {
            if (row.ValueKind != JsonValueKind.Array || row.GetArrayLength() < 6)
                continue;

            var date = DateTimeOffset.FromUnixTimeMilliseconds(row[0].GetInt64()).UtcDateTime.Date;
            var open = ParseDecimal(row[1].GetString()) ?? 0m;
            var high = ParseDecimal(row[2].GetString()) ?? 0m;
            var low = ParseDecimal(row[3].GetString()) ?? 0m;
            var close = ParseDecimal(row[4].GetString()) ?? 0m;
            var multiplier = string.Equals(currency, "BRL", StringComparison.OrdinalIgnoreCase) ? 1m : brlRate;
            candles.Add(new HistoricoCandle(ativoId, ProvedorCotacao.Binance, symbol, date, interval, open, high, low, close, close * multiplier, ParseDecimal(row[5].GetString()), row.GetRawText()));
        }

        await UpsertHistoricoEmLoteAsync(candles, cancellationToken);
    }

    private async Task ImportarHistoricoBrapiAsync(int ativoId, string symbol, JsonElement result, CancellationToken cancellationToken)
    {
        const string interval = "1d";
        if (await HistoricoSincronizadoHojeAsync(ativoId, ProvedorCotacao.Brapi, interval, cancellationToken))
            return;

        if (!result.TryGetProperty("historicalDataPrice", out var historical) || historical.ValueKind != JsonValueKind.Array)
            return;

        var candles = new List<HistoricoCandle>();
        foreach (var item in historical.EnumerateArray())
        {
            var unix = GetDecimal(item, "date");
            if (!unix.HasValue)
                continue;

            var date = DateTimeOffset.FromUnixTimeSeconds((long)unix.Value).UtcDateTime.Date;
            var close = GetDecimal(item, "adjustedClose") ?? GetDecimal(item, "close") ?? 0m;
            candles.Add(new HistoricoCandle(
                ativoId,
                ProvedorCotacao.Brapi,
                symbol,
                date,
                interval,
                GetDecimal(item, "open") ?? close,
                GetDecimal(item, "high") ?? close,
                GetDecimal(item, "low") ?? close,
                close,
                close,
                GetDecimal(item, "volume"),
                item.GetRawText()));
        }

        await UpsertHistoricoEmLoteAsync(candles, cancellationToken);
    }

    private void UpsertCotacao(int ativoId, ProvedorCotacao provedor, string symbol, string currency, decimal price, decimal priceBrl, decimal? change, decimal? changePercent, DateTime? marketTime, StatusCotacao status, string? error, string rawJson, TimeSpan ttl)
    {
        var cotacao = _context.CotacoesAtivosFinanceiros.FirstOrDefault(x => x.AtivoFinanceiroId == ativoId && x.Provedor == provedor);
        if (cotacao is null)
        {
            cotacao = new CotacaoAtivoFinanceiro
            {
                AtivoFinanceiroId = ativoId,
                Provedor = provedor,
                UsuarioInclusao = UsuarioSistema
            };
            _context.CotacoesAtivosFinanceiros.Add(cotacao);
        }

        cotacao.Symbol = symbol;
        cotacao.Currency = currency;
        cotacao.Price = price;
        cotacao.PriceBRL = priceBrl;
        cotacao.Change = change;
        cotacao.ChangePercent = changePercent;
        cotacao.MarketTime = marketTime;
        cotacao.RetrievedAt = DateTime.UtcNow;
        cotacao.ExpiresAt = DateTime.UtcNow.Add(ttl);
        cotacao.Status = status;
        cotacao.ErrorMessage = error;
        cotacao.RawJson = rawJson;
    }

    private async Task<bool> HistoricoSincronizadoHojeAsync(int ativoId, ProvedorCotacao provedor, string interval, CancellationToken cancellationToken)
    {
        var hoje = DateTime.UtcNow.Date;
        return await _context.PrecosHistoricosAtivosFinanceiros.AnyAsync(x =>
            x.AtivoFinanceiroId == ativoId &&
            x.Provedor == provedor &&
            x.Interval == interval &&
            ((x.DataAlteracao ?? x.DataInclusao) >= hoje),
            cancellationToken);
    }

    private async Task UpsertHistoricoEmLoteAsync(IReadOnlyList<HistoricoCandle> candles, CancellationToken cancellationToken)
    {
        if (candles.Count == 0)
            return;

        foreach (var grupo in candles.GroupBy(x => new { x.AtivoFinanceiroId, x.Provedor, x.Interval }))
        {
            var inicio = grupo.Min(x => x.Date);
            var fim = grupo.Max(x => x.Date);
            var existentes = await _context.PrecosHistoricosAtivosFinanceiros
                .Where(x =>
                    x.AtivoFinanceiroId == grupo.Key.AtivoFinanceiroId &&
                    x.Provedor == grupo.Key.Provedor &&
                    x.Interval == grupo.Key.Interval &&
                    x.Date >= inicio &&
                    x.Date <= fim)
                .ToListAsync(cancellationToken);

            var existentesPorData = existentes.ToDictionary(x => x.Date.Date);
            foreach (var candle in grupo.OrderBy(x => x.Date))
            {
                if (!existentesPorData.TryGetValue(candle.Date, out var historico))
                {
                    historico = new PrecoHistoricoAtivoFinanceiro
                    {
                        AtivoFinanceiroId = candle.AtivoFinanceiroId,
                        Provedor = candle.Provedor,
                        Interval = candle.Interval,
                        Date = candle.Date,
                        UsuarioInclusao = UsuarioSistema
                    };
                    existentesPorData[candle.Date] = historico;
                    _context.PrecosHistoricosAtivosFinanceiros.Add(historico);
                }

                AplicarHistorico(historico, candle);
            }
        }
    }

    private static void AplicarHistorico(PrecoHistoricoAtivoFinanceiro historico, HistoricoCandle candle)
    {
        historico.Symbol = candle.Symbol;
        historico.Open = candle.Open;
        historico.High = candle.High;
        historico.Low = candle.Low;
        historico.Close = candle.Close;
        historico.CloseBRL = candle.CloseBrl;
        historico.Volume = candle.Volume;
        historico.RawJson = candle.RawJson;
    }

    private async Task MarcarSemTokenAsync(IReadOnlyList<AtivoFinanceiro> ativos, ProvedorCotacao provedor, IReadOnlyList<string> symbols, CancellationToken cancellationToken)
    {
        foreach (var ativo in ativos.Where(x => symbols.Contains(ResolverTickerB3(x), StringComparer.OrdinalIgnoreCase)))
            UpsertCotacao(ativo.Id, provedor, ResolverTickerB3(ativo), "BRL", 0m, 0m, null, null, null, StatusCotacao.SemToken, "Configure Financas:MarketData:BrapiToken para cotações B3 completas.", "{}", TimeSpan.FromMinutes(5));

        await Task.CompletedTask;
    }

    private async Task MarcarFalhaAsync(IReadOnlyList<AtivoFinanceiro> ativos, ProvedorCotacao provedor, IReadOnlyList<string> symbols, string error, CancellationToken cancellationToken)
    {
        foreach (var ativo in ativos.Where(x => symbols.Contains(ResolverTickerB3(x), StringComparer.OrdinalIgnoreCase)))
            UpsertCotacao(ativo.Id, provedor, ResolverTickerB3(ativo), "BRL", 0m, 0m, null, null, null, StatusCotacao.Falhou, error, "{}", TimeSpan.FromMinutes(5));

        await Task.CompletedTask;
    }

    private static string ResolverTickerB3(AtivoFinanceiro ativo)
        => !string.IsNullOrWhiteSpace(ativo.Ticker) ? ativo.Ticker.Trim().ToUpperInvariant() : ExtrairTicker(ativo.AssetKey) ?? ExtrairTicker(ativo.Name) ?? ativo.AssetKey.Trim().ToUpperInvariant();

    private static string? ExtrairTicker(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var match = System.Text.RegularExpressions.Regex.Match(value.ToUpperInvariant(), @"\b[A-Z]{4}\d{1,2}\b");
        return match.Success ? match.Value : null;
    }

    private static bool IsBrapiFreeTicker(string ticker)
        => ticker.Equals("PETR4", StringComparison.OrdinalIgnoreCase)
            || ticker.Equals("MGLU3", StringComparison.OrdinalIgnoreCase)
            || ticker.Equals("VALE3", StringComparison.OrdinalIgnoreCase)
            || ticker.Equals("ITUB4", StringComparison.OrdinalIgnoreCase);

    private static string? GetString(JsonElement element, string name)
        => element.ValueKind == JsonValueKind.Object && element.TryGetProperty(name, out var value) && value.ValueKind != JsonValueKind.Null
            ? value.ValueKind == JsonValueKind.String ? value.GetString() : value.GetRawText()
            : null;

    private static decimal? GetDecimal(JsonElement element, string name)
        => element.ValueKind == JsonValueKind.Object && element.TryGetProperty(name, out var value)
            ? value.ValueKind == JsonValueKind.Number && value.TryGetDecimal(out var number) ? number : ParseDecimal(value.GetString())
            : null;

    private static decimal? ParseDecimal(string? value)
        => decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed) ? parsed : null;

    private static DateTime? TryParseDateTime(string? value)
        => DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var parsed) ? parsed.ToUniversalTime() : null;

    private sealed record BinanceTicker(string Symbol, decimal Price, decimal? Change, decimal? ChangePercent, DateTime? MarketTime, string RawJson);

    private sealed record HistoricoCandle(
        int AtivoFinanceiroId,
        ProvedorCotacao Provedor,
        string Symbol,
        DateTime Date,
        string Interval,
        decimal Open,
        decimal High,
        decimal Low,
        decimal Close,
        decimal CloseBrl,
        decimal? Volume,
        string RawJson);
}

internal static partial class FinancasMarketDataLogMessages
{
    [LoggerMessage(EventId = 41, Level = LogLevel.Warning, Message = "Falha ao atualizar cotações via {Provider}: {Message}")]
    public static partial void FalhaCotacao(ILogger logger, string provider, string message);

    [LoggerMessage(EventId = 42, Level = LogLevel.Warning, Message = "Provento duplicado com valor divergente ({Chave}): existente={ValorExistente}, novo={ValorNovo}, fonte={Fonte}.")]
    public static partial void ProventoDivergente(ILogger logger, string chave, decimal valorExistente, decimal valorNovo, string fonte);
}
