using Sistema.CORE.Entities;
using Sistema.CORE.Common;

namespace Sistema.CORE.Interfaces;

public interface IPerfilRepository
{
    Task<PagedResult<Perfil>> BuscarTodosAsync(int page, int pageSize);
    Task<PagedResult<Perfil>> BuscarFiltradosAsync(bool? ativo, int page, int pageSize);
    Task<Perfil?> BuscarPorIdAsync(int id);
    Task<Perfil?> BuscarPorNomeAsync(string nome);
    Task<Perfil> AdicionarAsync(Perfil perfil);
    Task AtualizarAsync(Perfil perfil);
    Task RemoverAsync(int id);
}
