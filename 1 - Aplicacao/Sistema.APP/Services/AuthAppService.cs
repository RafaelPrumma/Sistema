using Sistema.APP.Services.Interfaces;

namespace Sistema.APP.Services;

public class AuthAppService(Sistema.CORE.Services.Interfaces.IAuthService domainService) : IAuthService
{
    private readonly Sistema.CORE.Services.Interfaces.IAuthService _domainService = domainService;

    public Task<string?> AutenticarAsync(string cpf, string senha, CancellationToken cancellationToken = default) =>
        _domainService.AutenticarAsync(cpf, senha, cancellationToken);
}
