using Sistema.CORE.Entities;
using Sistema.CORE.Common;

namespace Sistema.CORE.Services;

public interface IUsuarioService
{
    Task<PagedResult<Usuario>> BuscarTodosAsync(int page, int pageSize);
    Task<Usuario?> BuscarPorIdAsync(int id);
    Task<OperationResult<Usuario>> AdicionarAsync(Usuario usuario);
    Task<OperationResult> AtualizarAsync(Usuario usuario);
    Task<OperationResult> RemoverAsync(int id);
}
