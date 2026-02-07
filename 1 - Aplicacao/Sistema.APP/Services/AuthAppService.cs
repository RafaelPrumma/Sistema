using Sistema.APP.Services.Interfaces;

namespace Sistema.APP.Services;

public class AuthAppService(Sistema.CORE.Services.Interfaces.IAuthDomainService domainService) : IAuthAppService
{
    private readonly Sistema.CORE.Services.Interfaces.IAuthDomainService _domainService = domainService;

    public Task<string?> AutenticarAsync(string cpf, string senha, CancellationToken cancellationToken = default) =>
        _domainService.AutenticarAsync(cpf, senha, cancellationToken);
}
