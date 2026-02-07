using Sistema.APP.Services.Interfaces;
using Sistema.CORE.Common;
using Sistema.CORE.Entities;

namespace Sistema.APP.Services;

public class FuncionalidadeAppService(Sistema.CORE.Services.Interfaces.IFuncionalidadeDomainService domainService) : IFuncionalidadeAppService
{
    private readonly Sistema.CORE.Services.Interfaces.IFuncionalidadeDomainService _domainService = domainService;

    public Task<PagedResult<Funcionalidade>> BuscarPaginadasAsync(int page, int pageSize, CancellationToken cancellationToken = default) =>
        _domainService.BuscarPaginadasAsync(page, pageSize, cancellationToken);

    public Task<Funcionalidade?> BuscarPorIdAsync(int id, CancellationToken cancellationToken = default) =>
        _domainService.BuscarPorIdAsync(id, cancellationToken);

    public Task<OperationResult<Funcionalidade>> AdicionarAsync(Funcionalidade func, CancellationToken cancellationToken = default) =>
        _domainService.AdicionarAsync(func, cancellationToken);

    public Task<OperationResult> AtualizarAsync(Funcionalidade func, CancellationToken cancellationToken = default) =>
        _domainService.AtualizarAsync(func, cancellationToken);

    public Task<OperationResult> RemoverAsync(int id, CancellationToken cancellationToken = default) =>
        _domainService.RemoverAsync(id, cancellationToken);
}
