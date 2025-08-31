using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Sistema.CORE.Entities;

namespace Sistema.CORE.Repositories.Interfaces;

public interface IPerfilFuncionalidadeRepository
{
    Task<IEnumerable<PerfilFuncionalidade>> BuscarPorPerfilIdAsync(int perfilId, CancellationToken cancellationToken = default);
    Task DefinirParaPerfilAsync(int perfilId, IEnumerable<PerfilFuncionalidade> funcs, CancellationToken cancellationToken = default);
}
