namespace Sistema.CORE.Services;

using Sistema.CORE.Entities;

public interface IPerfilService
{
    Task<IEnumerable<Perfil>> GetAllAsync();
    Task<Perfil?> GetByIdAsync(int id);
    Task<Perfil> AddAsync(Perfil perfil);
    Task UpdateAsync(Perfil perfil);
    Task DeleteAsync(int id);
}
