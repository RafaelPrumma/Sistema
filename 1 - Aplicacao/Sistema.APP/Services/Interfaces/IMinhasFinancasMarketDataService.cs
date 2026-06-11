namespace Sistema.APP.Services.Interfaces;

public interface IMinhasFinancasMarketDataService
{
    Task AtualizarCotacoesAsync(bool force = false, CancellationToken cancellationToken = default);
}
