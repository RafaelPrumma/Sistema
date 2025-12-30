namespace Sistema.CORE.Services.Interfaces;

public interface IAuthService
{
    Task<string?> AutenticarAsync(string cpf, string senha, CancellationToken cancellationToken = default);
}
