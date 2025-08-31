using Sistema.CORE.Entities;
using Sistema.CORE.Common;
using System.Threading;

namespace Sistema.CORE.Interfaces;

public interface IUsuarioRepository
{
    Task<PagedResult<Usuario>> BuscarTodosAsync(int page, int pageSize, CancellationToken cancellationToken = default);
    Task<PagedResult<Usuario>> BuscarFiltradosAsync(DateTime? inicio, DateTime? fim, int? perfilId, bool? ativo, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<Usuario?> BuscarPorIdAsync(int id, CancellationToken cancellationToken = default);
    Task<Usuario?> BuscarPorCpfAsync(string cpf, CancellationToken cancellationToken = default);
    Task<Usuario?> BuscarPorResetTokenAsync(string token, CancellationToken cancellationToken = default);
    Task<bool> ExisteAtivoPorPerfilAsync(int perfilId, CancellationToken cancellationToken = default);
    Task<Usuario> AdicionarAsync(Usuario usuario, CancellationToken cancellationToken = default);
    Task AtualizarAsync(Usuario usuario);
    Task RemoverAsync(int id, CancellationToken cancellationToken = default);
}
