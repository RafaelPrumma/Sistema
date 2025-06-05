using Sistema.CORE.Entities;

namespace Sistema.CORE.Interfaces;

public interface IUsuarioRepository
{
    Task<IEnumerable<Usuario>> GetAllAsync();
    Task<Usuario?> GetByIdAsync(int id);
    Task<Usuario?> GetByCpfAsync(string cpf);
    Task<IEnumerable<Usuario>> GetFilteredAsync(DateTime? inicio, DateTime? fim, int? perfilId, bool? ativo);
    Task<bool> ExistsActiveByPerfilAsync(int perfilId);
    Task<Usuario> AddAsync(Usuario usuario);
    Task UpdateAsync(Usuario usuario);
    Task DeleteAsync(int id);
}
