using Sistema.CORE.Entities;

namespace Sistema.CORE.Interfaces;

public interface IPerfilRepository
{
    Task<PagedResult<Perfil>> GetAllAsync(int page, int pageSize);
    Task<Perfil?> GetByIdAsync(int id);
    Task<Perfil?> GetByNameAsync(string nome);
    Task<PagedResult<Perfil>> GetFilteredAsync(bool? ativo, int page, int pageSize);
    Task<Perfil> AddAsync(Perfil perfil);
    Task UpdateAsync(Perfil perfil);
    Task DeleteAsync(int id);
}
