using Sistema.CORE.Entities;
using Sistema.CORE.Common;

namespace Sistema.CORE.Interfaces;

public interface IUsuarioRepository
{
    Task<PagedResult<Usuario>> BuscarTodosAsync(int page, int pageSize);
    Task<PagedResult<Usuario>> BuscarFiltradosAsync(DateTime? inicio, DateTime? fim, int? perfilId, bool? ativo, int page, int pageSize);
    Task<Usuario?> BuscarPorIdAsync(int id);
    Task<Usuario?> BuscarPorCpfAsync(string cpf);
    Task<bool> ExisteAtivoPorPerfilAsync(int perfilId);
    Task<Usuario> AdicionarAsync(Usuario usuario);
    Task AtualizarAsync(Usuario usuario);
    Task RemoverAsync(int id);
}
