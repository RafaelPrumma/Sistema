using Sistema.CORE.Entities;
using Sistema.CORE.Common;

namespace Sistema.CORE.Services;

public interface IUsuarioService
{
    Task<IEnumerable<Usuario>> GetAllAsync();
    Task<Usuario?> GetByIdAsync(int id);
    Task<IEnumerable<Usuario>> GetFilteredAsync(DateTime? inicio, DateTime? fim, int? perfilId, bool? ativo);
    Task<OperationResult<Usuario>> AddAsync(Usuario usuario);
    Task<OperationResult> UpdateAsync(Usuario usuario);
    Task<OperationResult> DeleteAsync(int id);
    Task<OperationResult> AlterarAtivoAsync(int id, bool ativo, string usuario);
}
