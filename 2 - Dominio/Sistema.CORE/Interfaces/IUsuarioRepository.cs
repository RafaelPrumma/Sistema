using Sistema.CORE.Entities;

namespace Sistema.CORE.Interfaces;

public interface IUsuarioRepository
{
    Task<PagedResult<Usuario>> GetAllAsync(int page, int pageSize);
    Task<Usuario?> GetByIdAsync(int id);
    Task<Usuario?> GetByCpfAsync(string cpf);
    Task<PagedResult<Usuario>> GetFilteredAsync(DateTime? inicio, DateTime? fim, int? perfilId, bool? ativo, int page, int pageSize);
    Task<bool> ExistsActiveByPerfilAsync(int perfilId);
    Task<Usuario> AddAsync(Usuario usuario);
    Task UpdateAsync(Usuario usuario);
    Task DeleteAsync(int id);
}
