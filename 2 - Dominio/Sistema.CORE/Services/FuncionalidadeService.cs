using System.Threading;
using System.Threading.Tasks;
using Sistema.CORE.Common;
using Sistema.CORE.Entities;
using Sistema.CORE.Repositories.Interfaces;
using Sistema.CORE.Services.Interfaces;

namespace Sistema.CORE.Services;

/// <summary>
/// Serviço que gerencia funcionalidades do sistema e registra auditorias das operações.
/// </summary>
public class FuncionalidadeService(IUnitOfWork uow, ILogService log) : IFuncionalidadeService
{
    private readonly IUnitOfWork _uow = uow;
    private readonly ILogService _log = log;

    /// <summary>
    /// Obtém funcionalidades com suporte a paginação.
    /// </summary>
    /// <param name="page">Página solicitada (base 1).</param>
    /// <param name="pageSize">Quantidade de registros por página.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Resultado paginado de funcionalidades.</returns>
    public Task<PagedResult<Funcionalidade>> BuscarPaginadasAsync(int page, int pageSize, CancellationToken cancellationToken = default)
        => _uow.Funcionalidades.BuscarPaginadasAsync(page, pageSize, cancellationToken);

    /// <summary>
    /// Busca uma funcionalidade pelo identificador.
    /// </summary>
    /// <param name="id">Identificador da funcionalidade.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Funcionalidade encontrada ou nula.</returns>
    public Task<Funcionalidade?> BuscarPorIdAsync(int id, CancellationToken cancellationToken = default) => _uow.Funcionalidades.BuscarPorIdAsync(id, cancellationToken);

    /// <summary>
    /// Adiciona uma nova funcionalidade e registra o evento no log.
    /// </summary>
    /// <param name="func">Entidade a ser criada.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Resultado com a entidade criada.</returns>
    public async Task<OperationResult<Funcionalidade>> AdicionarAsync(Funcionalidade func, CancellationToken cancellationToken = default)
    {
        await _uow.Funcionalidades.AdicionarAsync(func, cancellationToken);
        await _log.RegistrarAsync(nameof(Funcionalidade), "Add", true, "Funcionalidade criada", LogTipo.Sucesso, func.UsuarioInclusao, null, cancellationToken);
        await _uow.ConfirmarAsync(cancellationToken);
        return new OperationResult<Funcionalidade>(true, "Criado", func);
    }

    /// <summary>
    /// Atualiza uma funcionalidade existente e registra a operação.
    /// </summary>
    /// <param name="func">Funcionalidade com dados atualizados.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Resultado da operação.</returns>
    public async Task<OperationResult> AtualizarAsync(Funcionalidade func, CancellationToken cancellationToken = default)
    {
        await _uow.Funcionalidades.AtualizarAsync(func);
        await _log.RegistrarAsync(nameof(Funcionalidade), "Update", true, "Funcionalidade atualizada", LogTipo.Sucesso, func.UsuarioAlteracao ?? "system", null, cancellationToken);
        await _uow.ConfirmarAsync(cancellationToken);
        return new OperationResult(true, "Atualizado");
    }

    /// <summary>
    /// Remove uma funcionalidade e registra o evento de exclusão.
    /// </summary>
    /// <param name="id">Identificador da funcionalidade.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Resultado da operação.</returns>
    public async Task<OperationResult> RemoverAsync(int id, CancellationToken cancellationToken = default)
    {
        await _uow.Funcionalidades.RemoverAsync(id, cancellationToken);
        await _log.RegistrarAsync(nameof(Funcionalidade), "Delete", true, "Funcionalidade removida", LogTipo.Sucesso, "system", null, cancellationToken);
        await _uow.ConfirmarAsync(cancellationToken);
        return new OperationResult(true, "Removido");
    }
}
