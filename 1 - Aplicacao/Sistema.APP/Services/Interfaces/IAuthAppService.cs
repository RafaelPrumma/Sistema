namespace Sistema.APP.Services.Interfaces;

public interface IAuthAppService
{
    Task<string?> AutenticarAsync(string cpf, string senha, CancellationToken cancellationToken = default);
}
