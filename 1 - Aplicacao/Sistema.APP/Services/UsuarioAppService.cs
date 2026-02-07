using Sistema.APP.Services.Interfaces;
using Sistema.CORE.Common;
using Sistema.CORE.Entities;

namespace Sistema.APP.Services;

public class UsuarioAppService(Sistema.CORE.Services.Interfaces.IUsuarioService domainService) : IUsuarioService
{
    private readonly Sistema.CORE.Services.Interfaces.IUsuarioService _domainService = domainService;

    public Task<PagedResult<Usuario>> BuscarTodosAsync(int page, int pageSize, CancellationToken cancellationToken = default) =>
        _domainService.BuscarTodosAsync(page, pageSize, cancellationToken);

    public Task<Usuario?> BuscarPorIdAsync(int id, CancellationToken cancellationToken = default) =>
        _domainService.BuscarPorIdAsync(id, cancellationToken);

    public Task<Usuario?> BuscarPorCpfAsync(string cpf, CancellationToken cancellationToken = default) =>
        _domainService.BuscarPorCpfAsync(cpf, cancellationToken);

    public Task<Usuario?> BuscarPorResetTokenAsync(string token, CancellationToken cancellationToken = default) =>
        _domainService.BuscarPorResetTokenAsync(token, cancellationToken);

    public Task<OperationResult<Usuario>> AdicionarAsync(Usuario usuario, CancellationToken cancellationToken = default) =>
        _domainService.AdicionarAsync(usuario, cancellationToken);

    public Task<OperationResult> AtualizarAsync(Usuario usuario, CancellationToken cancellationToken = default) =>
        _domainService.AtualizarAsync(usuario, cancellationToken);

    public Task<OperationResult> RemoverAsync(int id, CancellationToken cancellationToken = default) =>
        _domainService.RemoverAsync(id, cancellationToken);
}
