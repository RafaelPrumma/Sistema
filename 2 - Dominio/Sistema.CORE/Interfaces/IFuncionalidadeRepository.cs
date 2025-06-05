using Sistema.CORE.Entities;
using Sistema.CORE.Common;

namespace Sistema.CORE.Interfaces;

public interface IFuncionalidadeRepository
{
    Task<PagedResult<Funcionalidade>> GetPagedAsync(int page, int pageSize);
    Task<Funcionalidade?> GetByIdAsync(int id);
    Task<Funcionalidade> AddAsync(Funcionalidade func);
    Task UpdateAsync(Funcionalidade func);
    Task DeleteAsync(int id);
}
