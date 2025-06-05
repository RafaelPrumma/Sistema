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

    public Task<PagedResult<Funcionalidade>> GetPagedAsync(int page, int pageSize)
        => _uow.Funcionalidades.GetPagedAsync(page, pageSize);

    public Task<Funcionalidade?> GetByIdAsync(int id) => _uow.Funcionalidades.GetByIdAsync(id);

    public async Task<OperationResult<Funcionalidade>> AddAsync(Funcionalidade func)
    {
        await _uow.Funcionalidades.AddAsync(func);
        await _uow.CommitAsync();
        return new OperationResult<Funcionalidade>(true, "Criado", func);
    }

    public async Task<OperationResult> UpdateAsync(Funcionalidade func)
    {
        await _uow.Funcionalidades.UpdateAsync(func);
        await _uow.CommitAsync();
        return new OperationResult(true, "Atualizado");
    }

    public async Task<OperationResult> DeleteAsync(int id)
    {
        await _uow.Funcionalidades.DeleteAsync(id);
        await _uow.CommitAsync();
        return new OperationResult(true, "Removido");
    }
}
