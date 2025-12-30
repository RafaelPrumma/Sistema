using Sistema.CORE.Entities;

namespace Sistema.CORE.Services.Interfaces;

/// <summary>
/// Serviço responsável por gerenciar configurações do sistema e delegar persistência ao repositório.
/// </summary>
public interface IConfiguracaoService
{
    /// <summary>
    /// Busca configurações pertencentes a um agrupamento específico.
    /// </summary>
    /// <param name="agrupamento">Agrupamento utilizado como chave de filtro.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Enumerable com as configurações encontradas.</returns>
    Task<IEnumerable<Configuracao>> BuscarPorAgrupamentoAsync(string agrupamento, CancellationToken cancellationToken = default);

    /// <summary>
    /// Obtém uma configuração específica a partir do agrupamento e da chave informados.
    /// </summary>
    /// <param name="agrupamento">Agrupamento ao qual a configuração pertence.</param>
    /// <param name="chave">Chave exclusiva dentro do agrupamento.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Configuração localizada ou nula quando inexistente.</returns>
    Task<Configuracao?> BuscarPorChaveAsync(string agrupamento, string chave, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adiciona uma nova configuração ao repositório e persiste a alteração.
    /// </summary>
    /// <param name="config">Entidade de configuração a ser incluída.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Entidade persistida com seu identificador.</returns>
    Task<Configuracao> AdicionarAsync(Configuracao config, CancellationToken cancellationToken = default);

    /// <summary>
    /// Atualiza uma configuração existente e confirma a transação.
    /// </summary>
    /// <param name="config">Configuração já existente com dados atualizados.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    Task AtualizarAsync(Configuracao config, CancellationToken cancellationToken = default);

    /// <summary>
    /// Remove uma configuração pelo identificador e persiste a exclusão.
    /// </summary>
    /// <param name="id">Identificador da configuração.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    Task RemoverAsync(int id, CancellationToken cancellationToken = default);
}
