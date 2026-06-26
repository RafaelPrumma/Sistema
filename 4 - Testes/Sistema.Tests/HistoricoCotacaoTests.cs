using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Sistema.APP.Services;
using Sistema.APP.Services.Interfaces;
using Sistema.CORE.Entities;
using Sistema.INFRA.Data;
using Sistema.INFRA.Services;

namespace Sistema.Tests;

/// <summary>
/// Modelo de cotações/histórico (spec investimentos F-P): bucketing intradiário 30m, merge OHLC,
/// consolidação diária 1d (B3/cripto) e retenção do intradiário. Lógica pura em
/// HistoricoCotacaoCalculator; consolidação/retenção stateful via DbContext in-memory.
/// </summary>
public class HistoricoCotacaoTests
{
    // --- Início do bucket de 30 min (puro) ---

    [Theory]
    [InlineData("2026-06-25T14:37:12Z", "2026-06-25T14:30:00Z")]
    [InlineData("2026-06-25T14:05:00Z", "2026-06-25T14:00:00Z")]
    [InlineData("2026-06-25T15:30:00Z", "2026-06-25T15:30:00Z")]
    [InlineData("2026-06-25T15:59:59Z", "2026-06-25T15:30:00Z")]
    [InlineData("2026-06-25T00:00:00Z", "2026-06-25T00:00:00Z")]
    [InlineData("2026-06-25T23:45:00Z", "2026-06-25T23:30:00Z")]
    public void InicioBucket30m_TruncaAoMultiploDe30Min(string entrada, string esperado)
    {
        var instante = DateTime.Parse(entrada, null, System.Globalization.DateTimeStyles.AdjustToUniversal);
        var resultado = HistoricoCotacaoCalculator.InicioBucket30m(instante);

        Assert.Equal(DateTime.Parse(esperado, null, System.Globalization.DateTimeStyles.AdjustToUniversal), resultado);
        Assert.Equal(DateTimeKind.Utc, resultado.Kind);
    }

    [Fact]
    public void InicioBucket30m_ConverteHorarioLocalParaUtc()
    {
        var local = new DateTime(2026, 6, 25, 11, 20, 0, DateTimeKind.Local);
        var resultado = HistoricoCotacaoCalculator.InicioBucket30m(local);

        Assert.Equal(DateTimeKind.Utc, resultado.Kind);
        Assert.Equal(0, resultado.Minute % 30);
    }

    // --- Bucket novo vs merge (puro) ---

    [Fact]
    public void NovoBucket30m_AbreOHLCNoPrecoAtual()
    {
        var c = HistoricoCotacaoCalculator.NovoBucket30m(
            new DateTime(2026, 6, 25, 14, 37, 0, DateTimeKind.Utc), preco: 10m, precoBrl: 50m);

        Assert.Equal(new DateTime(2026, 6, 25, 14, 30, 0, DateTimeKind.Utc), c.Date);
        Assert.Equal(10m, c.Open);
        Assert.Equal(10m, c.High);
        Assert.Equal(10m, c.Low);
        Assert.Equal(10m, c.Close);
        Assert.Equal(50m, c.CloseBRL);
    }

    [Fact]
    public void Merge30m_PreservaOpenEstendeHighLowAtualizaClose()
    {
        var inicial = HistoricoCotacaoCalculator.NovoBucket30m(
            new DateTime(2026, 6, 25, 14, 30, 0, DateTimeKind.Utc), preco: 10m, precoBrl: 50m);

        var aposAlta = HistoricoCotacaoCalculator.Merge30m(inicial, preco: 12m, precoBrl: 60m);
        var aposBaixa = HistoricoCotacaoCalculator.Merge30m(aposAlta, preco: 8m, precoBrl: 40m);
        var aposVolta = HistoricoCotacaoCalculator.Merge30m(aposBaixa, preco: 11m, precoBrl: 55m);

        Assert.Equal(10m, aposVolta.Open);     // open preservado do 1º tick
        Assert.Equal(12m, aposVolta.High);     // máxima da janela
        Assert.Equal(8m, aposVolta.Low);       // mínima da janela
        Assert.Equal(11m, aposVolta.Close);    // último tick
        Assert.Equal(55m, aposVolta.CloseBRL); // último BRL
    }

    // --- Consolidação diária 1d a partir dos 30m (puro) ---

    [Fact]
    public void ConsolidarDiario_OpenPrimeiroCloseUltimoMaxMin()
    {
        var buckets = new[]
        {
            new CandleOhlc(new DateTime(2026, 6, 25, 10, 0, 0, DateTimeKind.Utc), 10m, 11m, 9m, 10.5m, 52m),
            new CandleOhlc(new DateTime(2026, 6, 25, 10, 30, 0, DateTimeKind.Utc), 10.5m, 13m, 10m, 12m, 60m),
            new CandleOhlc(new DateTime(2026, 6, 25, 11, 0, 0, DateTimeKind.Utc), 12m, 12.5m, 7m, 8m, 40m),
        };

        var dia = HistoricoCotacaoCalculator.ConsolidarDiario(buckets);

        Assert.NotNull(dia);
        Assert.Equal(new DateTime(2026, 6, 25, 0, 0, 0, DateTimeKind.Utc), dia!.Value.Date);
        Assert.Equal(10m, dia.Value.Open);    // open do 1º bucket
        Assert.Equal(13m, dia.Value.High);    // máxima do dia
        Assert.Equal(7m, dia.Value.Low);      // mínima do dia
        Assert.Equal(8m, dia.Value.Close);    // close do último bucket
        Assert.Equal(40m, dia.Value.CloseBRL);
    }

    [Fact]
    public void ConsolidarDiario_CriptoUsaUltimoBucketDoDiaUtcComoFechamento()
    {
        // Cripto fecha 23:59 UTC: o último bucket do dia (23:30) é o fechamento.
        var buckets = new[]
        {
            new CandleOhlc(new DateTime(2026, 6, 25, 0, 0, 0, DateTimeKind.Utc), 100m, 100m, 100m, 100m, 500m),
            new CandleOhlc(new DateTime(2026, 6, 25, 12, 0, 0, DateTimeKind.Utc), 100m, 120m, 90m, 110m, 550m),
            new CandleOhlc(new DateTime(2026, 6, 25, 23, 30, 0, DateTimeKind.Utc), 110m, 115m, 105m, 108m, 540m),
        };

        var dia = HistoricoCotacaoCalculator.ConsolidarDiario(buckets);

        Assert.NotNull(dia);
        Assert.Equal(100m, dia!.Value.Open);
        Assert.Equal(108m, dia.Value.Close);   // bucket das 23:30 (último antes do fechamento)
        Assert.Equal(540m, dia.Value.CloseBRL);
        Assert.Equal(120m, dia.Value.High);
        Assert.Equal(90m, dia.Value.Low);
    }

    [Fact]
    public void ConsolidarDiario_IgnoraBucketsZeradosDeFalha()
    {
        var buckets = new[]
        {
            new CandleOhlc(new DateTime(2026, 6, 25, 10, 0, 0, DateTimeKind.Utc), 0m, 0m, 0m, 0m, 0m),
            new CandleOhlc(new DateTime(2026, 6, 25, 10, 30, 0, DateTimeKind.Utc), 10m, 11m, 9m, 10m, 50m),
        };

        var dia = HistoricoCotacaoCalculator.ConsolidarDiario(buckets);

        Assert.NotNull(dia);
        Assert.Equal(10m, dia!.Value.Open);  // ignora o bucket zerado, usa o 1º válido
        Assert.Equal(10m, dia.Value.Close);
    }

    [Fact]
    public void ConsolidarDiario_SemBucketValido_RetornaNull()
    {
        var buckets = new[]
        {
            new CandleOhlc(new DateTime(2026, 6, 25, 10, 0, 0, DateTimeKind.Utc), 0m, 0m, 0m, 0m, 0m),
        };

        Assert.Null(HistoricoCotacaoCalculator.ConsolidarDiario(buckets));
    }

    // --- Regra de retenção (pura) ---

    [Fact]
    public void PodeApagarBucket30m_SoQuandoPassou24hDoFimDoDiaEHaFechamento()
    {
        var dataBucket = new DateTime(2026, 6, 23, 10, 0, 0, DateTimeKind.Utc); // dia 23
        var fimDoDia = new DateTime(2026, 6, 24, 0, 0, 0, DateTimeKind.Utc);

        // Antes de 24h após o fim do dia → não apaga, mesmo com fechamento.
        Assert.False(HistoricoCotacaoCalculator.PodeApagarBucket30m(dataBucket, fimDoDia.AddHours(23), fechamentoDiarioExiste: true));
        // 24h+ após o fim do dia e com fechamento → apaga.
        Assert.True(HistoricoCotacaoCalculator.PodeApagarBucket30m(dataBucket, fimDoDia.AddHours(25), fechamentoDiarioExiste: true));
        // 24h+ passou mas SEM fechamento 1d → não apaga (perderia o dado).
        Assert.False(HistoricoCotacaoCalculator.PodeApagarBucket30m(dataBucket, fimDoDia.AddHours(25), fechamentoDiarioExiste: false));
    }

    // --- Consolidação + retenção stateful (DbContext in-memory) ---

    [Fact]
    public async Task ConsolidarHistoricoDiario_GeraCandle1dEApagaIntradiarioAntigo()
    {
        await using var ctx = CriarContexto();
        var ontem = DateTime.UtcNow.Date.AddDays(-1);

        // Buckets 30m de ONTEM (entram na janela de consolidação e na retenção após 24h).
        ctx.PrecosHistoricosAtivosFinanceiros.AddRange(
            Bucket30m(ativoId: 1, ontem.AddHours(10), open: 10m, high: 11m, low: 9m, close: 10.5m, brl: 52m),
            Bucket30m(ativoId: 1, ontem.AddHours(14), open: 10.5m, high: 13m, low: 8m, close: 12m, brl: 60m));
        await ctx.SaveChangesAsync();

        var service = CriarService(ctx);
        await service.ConsolidarHistoricoDiarioAsync();

        var diario = await ctx.PrecosHistoricosAtivosFinanceiros
            .SingleAsync(x => x.AtivoFinanceiroId == 1 && x.Interval == "1d");
        Assert.Equal(ontem, diario.Date);
        Assert.Equal(10m, diario.Open);
        Assert.Equal(13m, diario.High);
        Assert.Equal(8m, diario.Low);
        Assert.Equal(12m, diario.Close);
        Assert.Equal(60m, diario.CloseBRL);

        // Retenção: 30m de ontem ainda NÃO passou de 24h após o fim do dia → preservado.
        Assert.True(await ctx.PrecosHistoricosAtivosFinanceiros.AnyAsync(x => x.Interval == "30m"));
    }

    [Fact]
    public async Task ConsolidarHistoricoDiario_ApagaIntradiarioComMaisDe24hQuandoHaFechamento()
    {
        await using var ctx = CriarContexto();
        var antigo = DateTime.UtcNow.Date.AddDays(-3); // > 24h após o fim do dia

        ctx.PrecosHistoricosAtivosFinanceiros.AddRange(
            Bucket30m(ativoId: 1, antigo.AddHours(10), open: 10m, high: 11m, low: 9m, close: 10m, brl: 50m),
            Diario(ativoId: 1, antigo, close: 10m, brl: 50m)); // fechamento já persistido
        await ctx.SaveChangesAsync();

        var service = CriarService(ctx);
        await service.ConsolidarHistoricoDiarioAsync();

        Assert.False(await ctx.PrecosHistoricosAtivosFinanceiros
            .AnyAsync(x => x.Interval == "30m" && x.Date.Date == antigo));
        Assert.True(await ctx.PrecosHistoricosAtivosFinanceiros
            .AnyAsync(x => x.Interval == "1d" && x.Date == antigo));
    }

    [Fact]
    public async Task ConsolidarHistoricoDiario_NaoApagaIntradiarioAntigoSemFechamento()
    {
        await using var ctx = CriarContexto();
        var antigo = DateTime.UtcNow.Date.AddDays(-3);

        // 30m antigo, mas SEM 1d daquele dia → não pode apagar (a consolidação está fora da janela
        // de ±1 dia, então não cria o 1d; a retenção tem que preservar o 30m).
        ctx.PrecosHistoricosAtivosFinanceiros.Add(
            Bucket30m(ativoId: 1, antigo.AddHours(10), open: 10m, high: 11m, low: 9m, close: 10m, brl: 50m));
        await ctx.SaveChangesAsync();

        var service = CriarService(ctx);
        await service.ConsolidarHistoricoDiarioAsync();

        Assert.True(await ctx.PrecosHistoricosAtivosFinanceiros
            .AnyAsync(x => x.Interval == "30m" && x.Date.Date == antigo));
    }

    [Fact]
    public async Task ConsolidarHistoricoDiario_NaoSobrescreveCandle1dOficialDaApi()
    {
        await using var ctx = CriarContexto();
        var ontem = DateTime.UtcNow.Date.AddDays(-1);

        // 1d oficial (RawJson com payload, não "{}") + buckets 30m do mesmo dia com valores diferentes.
        var oficial = Diario(ativoId: 1, ontem, close: 99m, brl: 495m);
        oficial.RawJson = "{\"close\":99}";
        ctx.PrecosHistoricosAtivosFinanceiros.Add(oficial);
        ctx.PrecosHistoricosAtivosFinanceiros.Add(
            Bucket30m(ativoId: 1, ontem.AddHours(14), open: 10m, high: 13m, low: 8m, close: 12m, brl: 60m));
        await ctx.SaveChangesAsync();

        var service = CriarService(ctx);
        await service.ConsolidarHistoricoDiarioAsync();

        var diario = await ctx.PrecosHistoricosAtivosFinanceiros
            .SingleAsync(x => x.AtivoFinanceiroId == 1 && x.Interval == "1d");
        Assert.Equal(99m, diario.Close); // candle oficial preservado
        Assert.Equal(495m, diario.CloseBRL);
    }

    // --- Helpers ---

    private static PrecoHistoricoAtivoFinanceiro Bucket30m(int ativoId, DateTime date, decimal open, decimal high, decimal low, decimal close, decimal brl)
        => new()
        {
            AtivoFinanceiroId = ativoId,
            Provedor = ProvedorCotacao.Binance,
            Symbol = "BTCBRL",
            Interval = "30m",
            Date = HistoricoCotacaoCalculator.InicioBucket30m(date),
            Open = open,
            High = high,
            Low = low,
            Close = close,
            CloseBRL = brl,
            RawJson = "{}",
            UsuarioInclusao = "teste"
        };

    private static PrecoHistoricoAtivoFinanceiro Diario(int ativoId, DateTime dia, decimal close, decimal brl)
        => new()
        {
            AtivoFinanceiroId = ativoId,
            Provedor = ProvedorCotacao.Binance,
            Symbol = "BTCBRL",
            Interval = "1d",
            Date = dia.Date,
            Open = close,
            High = close,
            Low = close,
            Close = close,
            CloseBRL = brl,
            RawJson = "{}",
            UsuarioInclusao = "teste"
        };

    private static FinancasMarketDataService CriarService(AppDbContext ctx)
    {
        var httpFactory = new Mock<IHttpClientFactory>();
        var config = new Mock<IConfiguracaoLeitura>();
        return new FinancasMarketDataService(ctx, httpFactory.Object, config.Object, NullLogger<FinancasMarketDataService>.Instance);
    }

    private static AppDbContext CriarContexto()
    {
        var opts = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        var exec = new Mock<IExecutionContext>();
        exec.SetupGet(x => x.Usuario).Returns("teste");
        return new AppDbContext(opts, exec.Object);
    }
}
