using Sistema.CORE.Common;
using Sistema.CORE.Entities;

namespace Sistema.APP.Services.Interfaces;

public interface IFuncionalidadeService
{
    Task<PagedResult<Funcionalidade>> BuscarPaginadasAsync(int page, int pageSize, CancellationToken cancellationToken = default);
    Task<Funcionalidade?> BuscarPorIdAsync(int id, CancellationToken cancellationToken = default);
    Task<OperationResult<Funcionalidade>> AdicionarAsync(Funcionalidade func, CancellationToken cancellationToken = default);
    Task<OperationResult> AtualizarAsync(Funcionalidade func, CancellationToken cancellationToken = default);
    Task<OperationResult> RemoverAsync(int id, CancellationToken cancellationToken = default);
}
