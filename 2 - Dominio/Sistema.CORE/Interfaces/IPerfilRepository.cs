using Sistema.CORE.Entities;
using Sistema.CORE.Common;

namespace Sistema.CORE.Interfaces;

public interface IPerfilRepository
{
    Task<PagedResult<Perfil>> GetAllAsync(int page, int pageSize);
    Task<PagedResult<Perfil>> GetFilteredAsync(bool? ativo, int page, int pageSize);
    Task<Perfil?> GetByIdAsync(int id);
    Task<Perfil?> GetByNameAsync(string nome);
    Task<Perfil> AddAsync(Perfil perfil);
    Task UpdateAsync(Perfil perfil);
    Task DeleteAsync(int id);
}
