namespace Sistema.CORE.Services;

using Sistema.CORE.Entities;
using Sistema.CORE.Common;

public interface IPerfilService
{
    Task<IEnumerable<Perfil>> GetAllAsync();
    Task<Perfil?> GetByIdAsync(int id);
    Task<OperationResult<Perfil>> AddAsync(Perfil perfil);
    Task<OperationResult> UpdateAsync(Perfil perfil);
    Task<OperationResult> DeleteAsync(int id);
}
