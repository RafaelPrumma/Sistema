using Sistema.CORE.Entities;

namespace Sistema.CORE.Interfaces;

public interface IPerfilFuncionalidadeRepository
{
    Task<IEnumerable<PerfilFuncionalidade>> GetByPerfilIdAsync(int perfilId);
    Task SetForPerfilAsync(int perfilId, IEnumerable<PerfilFuncionalidade> funcs);
}
