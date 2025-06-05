namespace Sistema.CORE.Services;

using Sistema.CORE.Entities;
using Sistema.CORE.Common;

public interface IPerfilService
{
    Task<PagedResult<Perfil>> GetAllAsync(int page, int pageSize);
    Task<Perfil?> GetByIdAsync(int id);
    Task<PagedResult<Perfil>> GetFilteredAsync(bool? ativo, int page, int pageSize);
    Task<OperationResult<Perfil>> AddAsync(Perfil perfil);
    Task<OperationResult> UpdateAsync(Perfil perfil);
    Task<OperationResult> DeleteAsync(int id);
    Task<OperationResult> AlterarAtivoAsync(int id, bool ativo, string usuario);
    Task<IEnumerable<PerfilFuncionalidade>> GetFuncionalidadesAsync(int perfilId);
    Task<OperationResult> DefinirFuncionalidadesAsync(int perfilId, IEnumerable<PerfilFuncionalidade> funcoes);
}
