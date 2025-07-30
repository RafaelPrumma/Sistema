using Sistema.CORE.Entities;
using Sistema.CORE.Common;

namespace Sistema.CORE.Interfaces;

public interface IFuncionalidadeRepository
{
    Task<PagedResult<Funcionalidade>> BuscarPaginadasAsync(int page, int pageSize);
    Task<Funcionalidade?> BuscarPorIdAsync(int id);
    Task<Funcionalidade> AdicionarAsync(Funcionalidade func);
    Task AtualizarAsync(Funcionalidade func);
    Task RemoverAsync(int id);
}
