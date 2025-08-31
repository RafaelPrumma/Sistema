using Sistema.CORE.Entities;
using Sistema.CORE.Common;
using System.Threading;

namespace Sistema.CORE.Interfaces;

public interface IFuncionalidadeRepository
{
    Task<PagedResult<Funcionalidade>> BuscarPaginadasAsync(int page, int pageSize, CancellationToken cancellationToken = default);
    Task<Funcionalidade?> BuscarPorIdAsync(int id, CancellationToken cancellationToken = default);
    Task<Funcionalidade> AdicionarAsync(Funcionalidade func, CancellationToken cancellationToken = default);
    Task AtualizarAsync(Funcionalidade func);
    Task RemoverAsync(int id, CancellationToken cancellationToken = default);
}
