namespace Sistema.CORE.Services;

using Sistema.CORE.Entities;
using Sistema.CORE.Common;

public interface IPerfilService
{
    Task<IEnumerable<Perfil>> BuscarTodosAsync();
    Task<Perfil?> BuscarPorIdAsync(int id);
    Task<OperationResult<Perfil>> AdicionarAsync(Perfil perfil);
    Task<OperationResult> AtualizarAsync(Perfil perfil);
    Task<OperationResult> RemoverAsync(int id);
}
