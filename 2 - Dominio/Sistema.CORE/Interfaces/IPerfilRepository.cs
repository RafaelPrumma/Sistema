using Sistema.CORE.Entities;

namespace Sistema.CORE.Interfaces;

public interface IPerfilRepository
{
    Task<IEnumerable<Perfil>> GetAllAsync();
    Task<Perfil?> GetByIdAsync(int id);
    Task<Perfil?> GetByNameAsync(string nome);
    Task<IEnumerable<Perfil>> GetFilteredAsync(bool? ativo);
    Task<Perfil> AddAsync(Perfil perfil);
    Task UpdateAsync(Perfil perfil);
    Task DeleteAsync(int id);
}
