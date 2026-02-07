using Sistema.CORE.Common;
using Sistema.CORE.Entities;

namespace Sistema.APP.Services.Interfaces;

public interface IUsuarioAppService
{
    Task<PagedResult<Usuario>> BuscarTodosAsync(int page, int pageSize, CancellationToken cancellationToken = default);
    Task<Usuario?> BuscarPorIdAsync(int id, CancellationToken cancellationToken = default);
    Task<Usuario?> BuscarPorCpfAsync(string cpf, CancellationToken cancellationToken = default);
    Task<Usuario?> BuscarPorResetTokenAsync(string token, CancellationToken cancellationToken = default);
    Task<OperationResult<Usuario>> AdicionarAsync(Usuario usuario, CancellationToken cancellationToken = default);
    Task<OperationResult> AtualizarAsync(Usuario usuario, CancellationToken cancellationToken = default);
    Task<OperationResult> RemoverAsync(int id, CancellationToken cancellationToken = default);
}
