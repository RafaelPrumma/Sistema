using Sistema.CORE.Common;
using Sistema.CORE.Entities;
using System.Threading;

namespace Sistema.CORE.Interfaces;

public interface IFuncionalidadeService
{
    Task<PagedResult<Funcionalidade>> BuscarPaginadasAsync(int page, int pageSize);
    Task<Funcionalidade?> BuscarPorIdAsync(int id);
    Task<OperationResult<Funcionalidade>> AdicionarAsync(Funcionalidade func, CancellationToken cancellationToken = default);
    Task<OperationResult> AtualizarAsync(Funcionalidade func, CancellationToken cancellationToken = default);
    Task<OperationResult> RemoverAsync(int id, CancellationToken cancellationToken = default);
}
