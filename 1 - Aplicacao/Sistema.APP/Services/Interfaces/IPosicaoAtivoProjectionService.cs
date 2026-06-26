namespace Sistema.APP.Services.Interfaces;

public interface IPosicaoAtivoProjectionService
{
    Task RecalcularAsync(CancellationToken cancellationToken = default);
}
