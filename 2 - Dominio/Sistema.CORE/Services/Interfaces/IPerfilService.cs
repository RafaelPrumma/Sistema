namespace Sistema.CORE.Interfaces;

using Sistema.CORE.Entities;
using Sistema.CORE.Common;
using System.Threading;

public interface IPerfilService
{
    Task<PagedResult<Perfil>> BuscarTodosAsync(int page, int pageSize);
    Task<Perfil?> BuscarPorIdAsync(int id);
    Task<OperationResult<Perfil>> AdicionarAsync(Perfil perfil, CancellationToken cancellationToken = default);
    Task<OperationResult> AtualizarAsync(Perfil perfil, CancellationToken cancellationToken = default);
    Task<OperationResult> RemoverAsync(int id, CancellationToken cancellationToken = default);
}
