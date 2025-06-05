using Sistema.CORE.Common;
using Sistema.CORE.Entities;

namespace Sistema.CORE.Services;

public interface IFuncionalidadeService
{
    Task<PagedResult<Funcionalidade>> GetPagedAsync(int page, int pageSize);
    Task<Funcionalidade?> GetByIdAsync(int id);
    Task<OperationResult<Funcionalidade>> AddAsync(Funcionalidade func);
    Task<OperationResult> UpdateAsync(Funcionalidade func);
    Task<OperationResult> DeleteAsync(int id);
}
