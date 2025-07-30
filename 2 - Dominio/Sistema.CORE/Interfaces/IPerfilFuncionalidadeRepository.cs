using Sistema.CORE.Entities;

namespace Sistema.CORE.Interfaces;

public interface IPerfilFuncionalidadeRepository
{
    Task<IEnumerable<PerfilFuncionalidade>> BuscarPorPerfilIdAsync(int perfilId);
    Task DefinirParaPerfilAsync(int perfilId, IEnumerable<PerfilFuncionalidade> funcs);
}
