using Sistema.CORE.Entities;
using System.Threading;

namespace Sistema.CORE.Interfaces;

public interface IPerfilFuncionalidadeRepository
{
    Task<IEnumerable<PerfilFuncionalidade>> BuscarPorPerfilIdAsync(int perfilId, CancellationToken cancellationToken = default);
    Task DefinirParaPerfilAsync(int perfilId, IEnumerable<PerfilFuncionalidade> funcs, CancellationToken cancellationToken = default);
}
