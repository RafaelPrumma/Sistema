using Sistema.CORE.Entities;
using Sistema.CORE.Common;
using System.Threading;

namespace Sistema.CORE.Interfaces;

public interface IUsuarioService
{
    Task<PagedResult<Usuario>> BuscarTodosAsync(int page, int pageSize);
    Task<Usuario?> BuscarPorIdAsync(int id);
    Task<Usuario?> BuscarPorCpfAsync(string cpf);
    Task<OperationResult<Usuario>> AdicionarAsync(Usuario usuario, CancellationToken cancellationToken = default);
    Task<OperationResult> AtualizarAsync(Usuario usuario, CancellationToken cancellationToken = default);
    Task<OperationResult> RemoverAsync(int id, CancellationToken cancellationToken = default);
}
