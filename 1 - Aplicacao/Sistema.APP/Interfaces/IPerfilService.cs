namespace Sistema.CORE.Interfaces;

using Sistema.CORE.Entities;
using Sistema.CORE.Common;

public interface IPerfilService
{
    Task<PagedResult<Perfil>> BuscarTodosAsync(int page, int pageSize);
    Task<Perfil?> BuscarPorIdAsync(int id);
    Task<OperationResult<Perfil>> AdicionarAsync(Perfil perfil);
    Task<OperationResult> AtualizarAsync(Perfil perfil);
    Task<OperationResult> RemoverAsync(int id);
}
