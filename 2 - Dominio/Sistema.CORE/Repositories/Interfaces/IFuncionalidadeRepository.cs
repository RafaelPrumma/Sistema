using System.Threading;
using System.Threading.Tasks;
using Sistema.CORE.Common;
using Sistema.CORE.Entities;

namespace Sistema.CORE.Repositories.Interfaces;

public interface IFuncionalidadeRepository
{
    Task<PagedResult<Funcionalidade>> BuscarPaginadasAsync(int page, int pageSize, CancellationToken cancellationToken = default);
    Task<Funcionalidade?> BuscarPorIdAsync(int id, CancellationToken cancellationToken = default);
    Task<Funcionalidade> AdicionarAsync(Funcionalidade func, CancellationToken cancellationToken = default);
    Task AtualizarAsync(Funcionalidade func);
    Task RemoverAsync(int id, CancellationToken cancellationToken = default);
}
