using Sistema.CORE.Common;
using Sistema.CORE.Entities;
using Sistema.CORE.Interfaces;

namespace Sistema.CORE.Services;

public class FuncionalidadeService : IFuncionalidadeService
{
    private readonly IUnitOfWork _uow;

    public FuncionalidadeService(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public Task<PagedResult<Funcionalidade>> BuscarPaginadasAsync(int page, int pageSize)
        => _uow.Funcionalidades.BuscarPaginadasAsync(page, pageSize);

    public Task<Funcionalidade?> BuscarPorIdAsync(int id) => _uow.Funcionalidades.BuscarPorIdAsync(id);

    public async Task<OperationResult<Funcionalidade>> AdicionarAsync(Funcionalidade func)
    {
        await _uow.Funcionalidades.AdicionarAsync(func);
        await _uow.ConfirmarAsync();
        return new OperationResult<Funcionalidade>(true, "Criado", func);
    }

    public async Task<OperationResult> AtualizarAsync(Funcionalidade func)
    {
        await _uow.Funcionalidades.AtualizarAsync(func);
        await _uow.ConfirmarAsync();
        return new OperationResult(true, "Atualizado");
    }

    public async Task<OperationResult> RemoverAsync(int id)
    {
        await _uow.Funcionalidades.RemoverAsync(id);
        await _uow.ConfirmarAsync();
        return new OperationResult(true, "Removido");
    }
}
