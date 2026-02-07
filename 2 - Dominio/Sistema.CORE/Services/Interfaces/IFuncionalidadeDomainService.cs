using System.Threading;
using System.Threading.Tasks;
using Sistema.CORE.Common;
using Sistema.CORE.Entities;

namespace Sistema.CORE.Services.Interfaces;

/// <summary>
/// Serviço que gerencia funcionalidades do sistema e registra auditorias das operações.
/// </summary>
public interface IFuncionalidadeDomainService
{
    /// <summary>
    /// Obtém funcionalidades com suporte a paginação.
    /// </summary>
    /// <param name="page">Página solicitada (base 1).</param>
    /// <param name="pageSize">Quantidade de registros por página.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Resultado paginado de funcionalidades.</returns>
    Task<PagedResult<Funcionalidade>> BuscarPaginadasAsync(int page, int pageSize, CancellationToken cancellationToken = default);

    /// <summary>
    /// Busca uma funcionalidade pelo identificador.
    /// </summary>
    /// <param name="id">Identificador da funcionalidade.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Funcionalidade encontrada ou nula.</returns>
    Task<Funcionalidade?> BuscarPorIdAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adiciona uma nova funcionalidade e registra o evento no log.
    /// </summary>
    /// <param name="func">Entidade a ser criada.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Resultado com a entidade criada.</returns>
    Task<OperationResult<Funcionalidade>> AdicionarAsync(Funcionalidade func, CancellationToken cancellationToken = default);

    /// <summary>
    /// Atualiza uma funcionalidade existente e registra a operação.
    /// </summary>
    /// <param name="func">Funcionalidade com dados atualizados.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Resultado da operação.</returns>
    Task<OperationResult> AtualizarAsync(Funcionalidade func, CancellationToken cancellationToken = default);

    /// <summary>
    /// Remove uma funcionalidade e registra o evento de exclusão.
    /// </summary>
    /// <param name="id">Identificador da funcionalidade.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Resultado da operação.</returns>
    Task<OperationResult> RemoverAsync(int id, CancellationToken cancellationToken = default);
}
