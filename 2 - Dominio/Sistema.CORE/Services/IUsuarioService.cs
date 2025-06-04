using Sistema.CORE.Entities;
using Sistema.CORE.Common;

namespace Sistema.CORE.Services;

public interface IUsuarioService
{
    Task<IEnumerable<Usuario>> GetAllAsync();
    Task<Usuario?> GetByIdAsync(int id);
    Task<OperationResult<Usuario>> AddAsync(Usuario usuario);
    Task<OperationResult> UpdateAsync(Usuario usuario);
    Task<OperationResult> DeleteAsync(int id);
}
