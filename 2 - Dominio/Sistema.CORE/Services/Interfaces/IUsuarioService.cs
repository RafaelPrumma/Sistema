using System.Threading;
using System.Threading.Tasks;
using Sistema.CORE.Entities;
using Sistema.CORE.Common;

namespace Sistema.CORE.Interfaces;

public interface IUsuarioService
{
    Task<PagedResult<Usuario>> BuscarTodosAsync(int page, int pageSize, CancellationToken cancellationToken = default);
    Task<Usuario?> BuscarPorIdAsync(int id, CancellationToken cancellationToken = default);
    Task<Usuario?> BuscarPorCpfAsync(string cpf, CancellationToken cancellationToken = default);
    Task<OperationResult<Usuario>> AdicionarAsync(Usuario usuario, CancellationToken cancellationToken = default);
    Task<OperationResult> AtualizarAsync(Usuario usuario, CancellationToken cancellationToken = default);
    Task<OperationResult> RemoverAsync(int id, CancellationToken cancellationToken = default);
}
