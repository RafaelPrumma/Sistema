using System.Threading;
using System.Threading.Tasks;
using Sistema.CORE.Common;
using Sistema.CORE.Entities;
using Sistema.CORE.Repositories.Interfaces;
using Sistema.CORE.Services.Interfaces;

namespace Sistema.CORE.Services;

public class UsuarioService(IUnitOfWork uow, ILogDomainService log) : IUsuarioDomainService
{
    private readonly IUnitOfWork _uow = uow;
    private readonly ILogDomainService _log = log;

    public Task<PagedResult<Usuario>> BuscarTodosAsync(int page, int pageSize, CancellationToken cancellationToken = default) =>
        _uow.Usuarios.BuscarTodosAsync(page, pageSize, cancellationToken);

    public Task<Usuario?> BuscarPorIdAsync(int id, CancellationToken cancellationToken = default) => _uow.Usuarios.BuscarPorIdAsync(id, cancellationToken);

    public Task<Usuario?> BuscarPorCpfAsync(string cpf, CancellationToken cancellationToken = default) => _uow.Usuarios.BuscarPorCpfAsync(cpf, cancellationToken);

    public Task<Usuario?> BuscarPorResetTokenAsync(string token, CancellationToken cancellationToken = default) => _uow.Usuarios.BuscarPorResetTokenAsync(token, cancellationToken);

    public async Task<OperationResult<Usuario>> AdicionarAsync(Usuario usuario, CancellationToken cancellationToken = default)
    {
        var existing = await _uow.Usuarios.BuscarPorCpfAsync(usuario.Cpf, cancellationToken);
        if (existing is not null)
        {
            await _log.RegistrarAsync(nameof(Usuario), "Add", false, "Usuário já existe", LogTipo.Erro, usuario.UsuarioInclusao, null, cancellationToken);
            await _uow.ConfirmarAsync(cancellationToken);
            return new OperationResult<Usuario>(false, "Usuário já existe");
        }

        var created = await _uow.Usuarios.AdicionarAsync(usuario, cancellationToken);
        await _log.RegistrarAsync(nameof(Usuario), "Add", true, "Usuário criado", LogTipo.Sucesso, usuario.UsuarioInclusao, null, cancellationToken);
        await _uow.ConfirmarAsync(cancellationToken);
        return new OperationResult<Usuario>(true, "Usuário criado com sucesso", created);
    }

    public async Task<OperationResult> AtualizarAsync(Usuario usuario, CancellationToken cancellationToken = default)
    {
        var existing = await _uow.Usuarios.BuscarPorCpfAsync(usuario.Cpf, cancellationToken);
        if (existing is not null && existing.Id != usuario.Id)
        {
            await _log.RegistrarAsync(nameof(Usuario), "Update", false, "CPF já utilizado", LogTipo.Erro, usuario.UsuarioAlteracao ?? "system", null, cancellationToken);
            await _uow.ConfirmarAsync(cancellationToken);
            return new OperationResult(false, "CPF já utilizado");
        }

        await _uow.Usuarios.AtualizarAsync(usuario);
        await _log.RegistrarAsync(nameof(Usuario), "Update", true, "Usuário atualizado", LogTipo.Sucesso, usuario.UsuarioAlteracao ?? "system", null, cancellationToken);
        await _uow.ConfirmarAsync(cancellationToken);
        return new OperationResult(true, "Usuário atualizado com sucesso");
    }

    public async Task<OperationResult> RemoverAsync(int id, CancellationToken cancellationToken = default)
    {
        await _uow.Usuarios.RemoverAsync(id, cancellationToken);
        await _log.RegistrarAsync(nameof(Usuario), "Delete", true, "Usuário removido", LogTipo.Sucesso, "system", null, cancellationToken);
        await _uow.ConfirmarAsync(cancellationToken);
        return new OperationResult(true, "Usuário removido");
    }
}
