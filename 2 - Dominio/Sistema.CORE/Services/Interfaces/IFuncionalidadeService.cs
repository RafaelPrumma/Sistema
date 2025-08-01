using Sistema.CORE.Common;
using Sistema.CORE.Entities;

namespace Sistema.CORE.Interfaces;

public interface IFuncionalidadeService
{
    Task<PagedResult<Funcionalidade>> BuscarPaginadasAsync(int page, int pageSize);
    Task<Funcionalidade?> BuscarPorIdAsync(int id);
    Task<OperationResult<Funcionalidade>> AdicionarAsync(Funcionalidade func);
    Task<OperationResult> AtualizarAsync(Funcionalidade func);
    Task<OperationResult> RemoverAsync(int id);
}
