using System.Threading;
using System.Threading.Tasks;
using Sistema.CORE.Entities;
using Sistema.CORE.Common;

namespace Sistema.CORE.Services.Interfaces;

/// <summary>
/// Serviço responsável por operações de usuário e auditoria das ações executadas.
/// </summary>
public interface IUsuarioService
{
    /// <summary>
    /// Recupera todos os usuários ativos aplicando paginação.
    /// </summary>
    /// <param name="page">Página solicitada (base 1).</param>
    /// <param name="pageSize">Quantidade de registros por página.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Resultado paginado de usuários.</returns>
    Task<PagedResult<Usuario>> BuscarTodosAsync(int page, int pageSize, CancellationToken cancellationToken = default);

    /// <summary>
    /// Busca um usuário pelo identificador único.
    /// </summary>
    /// <param name="id">Identificador do usuário.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Usuário encontrado ou nulo.</returns>
    Task<Usuario?> BuscarPorIdAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Busca um usuário pelo CPF.
    /// </summary>
    /// <param name="cpf">CPF do usuário.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Usuário localizado ou nulo.</returns>
    Task<Usuario?> BuscarPorCpfAsync(string cpf, CancellationToken cancellationToken = default);

    /// <summary>
    /// Localiza usuário pelo token de redefinição de senha.
    /// </summary>
    /// <param name="token">Token emitido para recuperação.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Usuário correspondente ou nulo.</returns>
    Task<Usuario?> BuscarPorResetTokenAsync(string token, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adiciona um novo usuário, registrando logs de sucesso ou conflito de CPF.
    /// </summary>
    /// <param name="usuario">Entidade a ser criada.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Resultado contendo sucesso e a entidade criada quando aplicável.</returns>
    Task<OperationResult<Usuario>> AdicionarAsync(Usuario usuario, CancellationToken cancellationToken = default);

    /// <summary>
    /// Atualiza dados de um usuário e registra o resultado da operação no log.
    /// </summary>
    /// <param name="usuario">Usuário com informações atualizadas.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Resultado sinalizando sucesso ou conflito de CPF.</returns>
    Task<OperationResult> AtualizarAsync(Usuario usuario, CancellationToken cancellationToken = default);

    /// <summary>
    /// Remove um usuário pelo identificador e registra o evento de exclusão.
    /// </summary>
    /// <param name="id">Identificador do usuário.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Resultado da operação.</returns>
    Task<OperationResult> RemoverAsync(int id, CancellationToken cancellationToken = default);
}
