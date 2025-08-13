using Sistema.CORE.Common;
using Sistema.CORE.Entities;
using Sistema.CORE.Interfaces;

namespace Sistema.CORE.Services;

public class UsuarioService : IUsuarioService
{
    private readonly IUnitOfWork _uow;

    public UsuarioService(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public Task<PagedResult<Usuario>> BuscarTodosAsync(int page, int pageSize) =>
        _uow.Usuarios.BuscarTodosAsync(page, pageSize);

    public Task<Usuario?> BuscarPorIdAsync(int id) => _uow.Usuarios.BuscarPorIdAsync(id);

    public Task<Usuario?> BuscarPorCpfAsync(string cpf) => _uow.Usuarios.BuscarPorCpfAsync(cpf);

    public async Task<OperationResult<Usuario>> AdicionarAsync(Usuario usuario)
    {
        var existing = await _uow.Usuarios.BuscarPorCpfAsync(usuario.Cpf);
        if (existing is not null)
        {
            await _uow.Logs.AdicionarAsync(new Log
            {
                Entidade = nameof(Usuario),
                Operacao = "Add",
                Sucesso = false,
                Mensagem = "Usuário já existe",
                Tipo = LogTipo.Erro,
                Usuario = usuario.UsuarioInclusao
            });
            await _uow.ConfirmarAsync();
            return new OperationResult<Usuario>(false, "Usuário já existe");
        }

        var created = await _uow.Usuarios.AdicionarAsync(usuario);
        await _uow.Logs.AdicionarAsync(new Log
        {
            Entidade = nameof(Usuario),
            Operacao = "Add",
            Sucesso = true,
            Mensagem = "Usuário criado",
            Tipo = LogTipo.Sucesso,
            Usuario = usuario.UsuarioInclusao
        });
        await _uow.ConfirmarAsync();
        return new OperationResult<Usuario>(true, "Usuário criado com sucesso", created);
    }

    public async Task<OperationResult> AtualizarAsync(Usuario usuario)
    {
        var existing = await _uow.Usuarios.BuscarPorCpfAsync(usuario.Cpf);
        if (existing is not null && existing.Id != usuario.Id)
        {
            await _uow.Logs.AdicionarAsync(new Log
            {
                Entidade = nameof(Usuario),
                Operacao = "Update",
                Sucesso = false,
                Mensagem = "CPF já utilizado",
                Tipo = LogTipo.Erro,
                Usuario = usuario.UsuarioAlteracao ?? "system"
            });
            await _uow.ConfirmarAsync();
            return new OperationResult(false, "CPF já utilizado");
        }

        await _uow.Usuarios.AtualizarAsync(usuario);
        await _uow.Logs.AdicionarAsync(new Log
        {
            Entidade = nameof(Usuario),
            Operacao = "Update",
            Sucesso = true,
            Mensagem = "Usuário atualizado",
            Tipo = LogTipo.Sucesso,
            Usuario = usuario.UsuarioAlteracao ?? "system"
        });
        await _uow.ConfirmarAsync();
        return new OperationResult(true, "Usuário atualizado com sucesso");
    }

    public async Task<OperationResult> RemoverAsync(int id)
    {
        await _uow.Usuarios.RemoverAsync(id);
        await _uow.Logs.AdicionarAsync(new Log
        {
            Entidade = nameof(Usuario),
            Operacao = "Delete",
            Sucesso = true,
            Mensagem = "Usuário removido",
            Tipo = LogTipo.Sucesso,
            Usuario = "system"
        });
        await _uow.ConfirmarAsync();
        return new OperationResult(true, "Usuário removido");
    }
}
