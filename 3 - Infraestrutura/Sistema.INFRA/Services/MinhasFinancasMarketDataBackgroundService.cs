using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Sistema.APP.Services.Interfaces;

namespace Sistema.INFRA.Services;

public class MinhasFinancasMarketDataBackgroundService(
    IServiceProvider serviceProvider,
    IConfiguration configuration,
    ILogger<MinhasFinancasMarketDataBackgroundService> logger) : BackgroundService
{
    private readonly IServiceProvider _serviceProvider = serviceProvider;
    private readonly IConfiguration _configuration = configuration;
    private readonly ILogger<MinhasFinancasMarketDataBackgroundService> _logger = logger;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_configuration.GetValue("MinhasFinancas:MarketData:BackgroundEnabled", true))
            return;

        await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(_configuration.GetValue("MinhasFinancas:MarketData:RefreshSeconds", 60)));

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var service = scope.ServiceProvider.GetRequiredService<IMinhasFinancasMarketDataService>();
                await service.AtualizarCotacoesAsync(force: false, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                MinhasFinancasMarketDataBackgroundLogMessages.FalhaAtualizacao(_logger, ex.Message);
            }

            await timer.WaitForNextTickAsync(stoppingToken);
        }
    }
}

internal static partial class MinhasFinancasMarketDataBackgroundLogMessages
{
    [LoggerMessage(EventId = 42, Level = LogLevel.Warning, Message = "Falha na atualização periódica de cotações financeiras: {Message}")]
    public static partial void FalhaAtualizacao(ILogger logger, string message);
}
