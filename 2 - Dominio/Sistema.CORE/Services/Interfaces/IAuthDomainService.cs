namespace Sistema.CORE.Services.Interfaces;

/// <summary>
/// Serviço de autenticação responsável por validar credenciais e emitir tokens JWT.
/// </summary>
public interface IAuthDomainService
{
    /// <summary>
    /// Autentica um usuário pelo CPF e senha, retornando um token JWT válido ou nulo em caso de falha.
    /// </summary>
    /// <param name="cpf">CPF informado no login.</param>
    /// <param name="senha">Senha em texto puro para validação.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Token JWT quando autenticado ou nulo quando inválido.</returns>
    Task<string?> AutenticarAsync(string cpf, string senha, CancellationToken cancellationToken = default);
}
