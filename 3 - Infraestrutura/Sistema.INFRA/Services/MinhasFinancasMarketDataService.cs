using System.Globalization;
using System.Net;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Sistema.APP.Services.Interfaces;
using Sistema.CORE.Entities;
using Sistema.INFRA.Data;

namespace Sistema.INFRA.Services;

public class MinhasFinancasMarketDataService(
    AppDbContext context,
    IHttpClientFactory httpClientFactory,
    IConfiguration configuration,
    ILogger<MinhasFinancasMarketDataService> logger) : IMinhasFinancasMarketDataService
{
    private const string UsuarioSistema = "market-data";
    private readonly AppDbContext _context = context;
    private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;
    private readonly IConfiguration _configuration = configuration;
    private readonly ILogger<MinhasFinancasMarketDataService> _logger = logger;

    public async Task AtualizarCotacoesAsync(bool force = false, CancellationToken cancellationToken = default)
    {
        var cargaId = await _context.CargasFinanceiras
            .OrderByDescending(x => x.ImportedAt)
            .Select(x => (int?)x.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (cargaId is null)
            return;

        var posicoes = await _context.EstimativasPosicaoCarteira
            .Include(x => x.Asset)
            .Where(x => x.CargaFinanceiraId == cargaId && x.Status == StatusEstimativaPosicao.AbertaOuResidual && x.Asset != null)
            .ToListAsync(cancellationToken);

        if (posicoes.Count == 0)
            return;

        var ativos = posicoes.Select(x => x.Asset!).DistinctBy(x => x.Id).ToList();
        await AtualizarB3Async(ativos.Where(x => !x.IsCrypto).ToList(), force, cancellationToken);
        await AtualizarCriptoAsync(ativos.Where(x => x.IsCrypto).ToList(), force, cancellationToken);
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

        var token = _configuration["MinhasFinancas:MarketData:BrapiToken"];
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
                ImportarHistoricoBrapi(ativo.Id, symbol, result);
            }
        }
        catch (Exception ex)
        {
            MinhasFinancasMarketDataLogMessages.FalhaCotacao(_logger, "Brapi", ex.Message);
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
        var client = _httpClientFactory.CreateClient("Binance");
        using var response = await client.GetAsync($"api/v3/klines?symbol={WebUtility.UrlEncode(symbol)}&interval=1d&limit=370", cancellationToken);
        if (!response.IsSuccessStatusCode)
            return;

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
        if (doc.RootElement.ValueKind != JsonValueKind.Array)
            return;

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
            UpsertHistorico(ativoId, ProvedorCotacao.Binance, symbol, date, "1d", open, high, low, close, close * multiplier, ParseDecimal(row[5].GetString()), row.GetRawText());
        }
    }

    private void ImportarHistoricoBrapi(int ativoId, string symbol, JsonElement result)
    {
        if (!result.TryGetProperty("historicalDataPrice", out var historical) || historical.ValueKind != JsonValueKind.Array)
            return;

        foreach (var item in historical.EnumerateArray())
        {
            var unix = GetDecimal(item, "date");
            if (!unix.HasValue)
                continue;

            var date = DateTimeOffset.FromUnixTimeSeconds((long)unix.Value).UtcDateTime.Date;
            var close = GetDecimal(item, "adjustedClose") ?? GetDecimal(item, "close") ?? 0m;
            UpsertHistorico(
                ativoId,
                ProvedorCotacao.Brapi,
                symbol,
                date,
                "1d",
                GetDecimal(item, "open") ?? close,
                GetDecimal(item, "high") ?? close,
                GetDecimal(item, "low") ?? close,
                close,
                close,
                GetDecimal(item, "volume"),
                item.GetRawText());
        }
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

    private void UpsertHistorico(int ativoId, ProvedorCotacao provedor, string symbol, DateTime date, string interval, decimal open, decimal high, decimal low, decimal close, decimal closeBrl, decimal? volume, string rawJson)
    {
        var historico = _context.PrecosHistoricosAtivosFinanceiros.FirstOrDefault(x =>
            x.AtivoFinanceiroId == ativoId &&
            x.Provedor == provedor &&
            x.Symbol == symbol &&
            x.Interval == interval &&
            x.Date == date);

        if (historico is null)
        {
            historico = new PrecoHistoricoAtivoFinanceiro
            {
                AtivoFinanceiroId = ativoId,
                Provedor = provedor,
                Symbol = symbol,
                Interval = interval,
                Date = date,
                UsuarioInclusao = UsuarioSistema
            };
            _context.PrecosHistoricosAtivosFinanceiros.Add(historico);
        }

        historico.Open = open;
        historico.High = high;
        historico.Low = low;
        historico.Close = close;
        historico.CloseBRL = closeBrl;
        historico.Volume = volume;
        historico.RawJson = rawJson;
    }

    private async Task MarcarSemTokenAsync(IReadOnlyList<AtivoFinanceiro> ativos, ProvedorCotacao provedor, IReadOnlyList<string> symbols, CancellationToken cancellationToken)
    {
        foreach (var ativo in ativos.Where(x => symbols.Contains(ResolverTickerB3(x), StringComparer.OrdinalIgnoreCase)))
            UpsertCotacao(ativo.Id, provedor, ResolverTickerB3(ativo), "BRL", 0m, 0m, null, null, null, StatusCotacao.SemToken, "Configure MinhasFinancas:MarketData:BrapiToken para cotações B3 completas.", "{}", TimeSpan.FromMinutes(5));

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
}

internal static partial class MinhasFinancasMarketDataLogMessages
{
    [LoggerMessage(EventId = 41, Level = LogLevel.Warning, Message = "Falha ao atualizar cotações via {Provider}: {Message}")]
    public static partial void FalhaCotacao(ILogger logger, string provider, string message);
}
