using System.Threading;
using System.Threading.Tasks;
using Sistema.CORE.Common;
using Sistema.CORE.Entities;

namespace Sistema.CORE.Repositories.Interfaces;

public interface IPerfilRepository
{
    Task<PagedResult<Perfil>> BuscarTodosAsync(int page, int pageSize, CancellationToken cancellationToken = default);
    Task<PagedResult<Perfil>> BuscarFiltradosAsync(bool? ativo, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<Perfil?> BuscarPorIdAsync(int id, CancellationToken cancellationToken = default);
    Task<Perfil?> BuscarPorNomeAsync(string nome, CancellationToken cancellationToken = default);
    Task<Perfil> AdicionarAsync(Perfil perfil, CancellationToken cancellationToken = default);
    Task AtualizarAsync(Perfil perfil);
    Task RemoverAsync(int id, CancellationToken cancellationToken = default);
}
