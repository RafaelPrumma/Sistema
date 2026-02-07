using Sistema.APP.Services.Interfaces;
using Sistema.CORE.Common;
using Sistema.CORE.Entities;

namespace Sistema.APP.Services;

public class PerfilAppService(Sistema.CORE.Services.Interfaces.IPerfilService domainService) : IPerfilService
{
    private readonly Sistema.CORE.Services.Interfaces.IPerfilService _domainService = domainService;

    public Task<PagedResult<Perfil>> BuscarTodosAsync(int page, int pageSize, CancellationToken cancellationToken = default) =>
        _domainService.BuscarTodosAsync(page, pageSize, cancellationToken);

    public Task<Perfil?> BuscarPorIdAsync(int id, CancellationToken cancellationToken = default) =>
        _domainService.BuscarPorIdAsync(id, cancellationToken);

    public Task<OperationResult<Perfil>> AdicionarAsync(Perfil perfil, CancellationToken cancellationToken = default) =>
        _domainService.AdicionarAsync(perfil, cancellationToken);

    public Task<OperationResult> AtualizarAsync(Perfil perfil, CancellationToken cancellationToken = default) =>
        _domainService.AtualizarAsync(perfil, cancellationToken);

    public Task<OperationResult> RemoverAsync(int id, CancellationToken cancellationToken = default) =>
        _domainService.RemoverAsync(id, cancellationToken);
}
