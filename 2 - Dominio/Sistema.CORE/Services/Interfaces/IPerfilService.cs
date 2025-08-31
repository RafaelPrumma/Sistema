namespace Sistema.CORE.Interfaces;

using System.Threading;
using System.Threading.Tasks;
using Sistema.CORE.Entities;
using Sistema.CORE.Common;

public interface IPerfilService
{
    Task<PagedResult<Perfil>> BuscarTodosAsync(int page, int pageSize, CancellationToken cancellationToken = default);
    Task<Perfil?> BuscarPorIdAsync(int id, CancellationToken cancellationToken = default);
    Task<OperationResult<Perfil>> AdicionarAsync(Perfil perfil, CancellationToken cancellationToken = default);
    Task<OperationResult> AtualizarAsync(Perfil perfil, CancellationToken cancellationToken = default);
    Task<OperationResult> RemoverAsync(int id, CancellationToken cancellationToken = default);
}
