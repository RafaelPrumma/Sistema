using Sistema.APP.Services.Interfaces;
using Sistema.CORE.Entities;

namespace Sistema.APP.Services;

public class TemaAppService(Sistema.CORE.Services.Interfaces.ITemaDomainService domainService) : ITemaAppService
{
    private readonly Sistema.CORE.Services.Interfaces.ITemaDomainService _domainService = domainService;

    public Task<Tema?> BuscarPorUsuarioIdAsync(int usuarioId, CancellationToken cancellationToken = default) =>
        _domainService.BuscarPorUsuarioIdAsync(usuarioId, cancellationToken);

    public Task SalvarAsync(Tema tema, CancellationToken cancellationToken = default) =>
        _domainService.SalvarAsync(tema, cancellationToken);
}
