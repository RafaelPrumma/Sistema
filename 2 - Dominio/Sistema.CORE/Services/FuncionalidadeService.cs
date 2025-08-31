using System.Threading;
using System.Threading.Tasks;
using Sistema.CORE.Common;
using Sistema.CORE.Entities;
using Sistema.CORE.Interfaces;

namespace Sistema.CORE.Services;

public class FuncionalidadeService : IFuncionalidadeService
{
    private readonly IUnitOfWork _uow;
    private readonly ILogService _log;

    public FuncionalidadeService(IUnitOfWork uow, ILogService log)
    {
        _uow = uow;
        _log = log;
    }

    public Task<PagedResult<Funcionalidade>> BuscarPaginadasAsync(int page, int pageSize, CancellationToken cancellationToken = default)
        => _uow.Funcionalidades.BuscarPaginadasAsync(page, pageSize, cancellationToken);

    public Task<Funcionalidade?> BuscarPorIdAsync(int id, CancellationToken cancellationToken = default) => _uow.Funcionalidades.BuscarPorIdAsync(id, cancellationToken);

    public async Task<OperationResult<Funcionalidade>> AdicionarAsync(Funcionalidade func, CancellationToken cancellationToken = default)
    {
        await _uow.Funcionalidades.AdicionarAsync(func, cancellationToken);
        await _log.RegistrarAsync(nameof(Funcionalidade), "Add", true, "Funcionalidade criada", LogTipo.Sucesso, func.UsuarioInclusao, cancellationToken);
        await _uow.ConfirmarAsync(cancellationToken);
        return new OperationResult<Funcionalidade>(true, "Criado", func);
    }

    public async Task<OperationResult> AtualizarAsync(Funcionalidade func, CancellationToken cancellationToken = default)
    {
        await _uow.Funcionalidades.AtualizarAsync(func);
        await _log.RegistrarAsync(nameof(Funcionalidade), "Update", true, "Funcionalidade atualizada", LogTipo.Sucesso, func.UsuarioAlteracao ?? "system", cancellationToken);
        await _uow.ConfirmarAsync(cancellationToken);
        return new OperationResult(true, "Atualizado");
    }

    public async Task<OperationResult> RemoverAsync(int id, CancellationToken cancellationToken = default)
    {
        await _uow.Funcionalidades.RemoverAsync(id, cancellationToken);
        await _log.RegistrarAsync(nameof(Funcionalidade), "Delete", true, "Funcionalidade removida", LogTipo.Sucesso, "system", cancellationToken);
        await _uow.ConfirmarAsync(cancellationToken);
        return new OperationResult(true, "Removido");
    }
}
