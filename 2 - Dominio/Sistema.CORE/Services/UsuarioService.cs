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

    public Task<PagedResult<Usuario>> GetAllAsync(int page, int pageSize) => _uow.Usuarios.GetAllAsync(page, pageSize);

    public Task<Usuario?> GetByIdAsync(int id) => _uow.Usuarios.GetByIdAsync(id);

    public Task<PagedResult<Usuario>> GetFilteredAsync(DateTime? inicio, DateTime? fim, int? perfilId, bool? ativo, int page, int pageSize)
        => _uow.Usuarios.GetFilteredAsync(inicio, fim, perfilId, ativo, page, pageSize);

    public async Task<OperationResult<Usuario>> AddAsync(Usuario usuario)
    {
        var existing = await _uow.Usuarios.GetByCpfAsync(usuario.Cpf);
        if (existing is not null)
        {
            await _uow.Logs.AddAsync(new Log
            {
                Entidade = nameof(Usuario),
                Operacao = "Add",
                Sucesso = false,
                Mensagem = "Usuário já existe",
                Tipo = LogTipo.Erro,
                Usuario = usuario.UsuarioInclusao
            });
            return new OperationResult<Usuario>(false, "Usuário já existe");
        }

        var created = await _uow.Usuarios.AddAsync(usuario);
        await _uow.Logs.AddAsync(new Log
        {
            Entidade = nameof(Usuario),
            Operacao = "Add",
            Sucesso = true,
            Mensagem = "Usuário criado",
            Tipo = LogTipo.Sucesso,
            Usuario = usuario.UsuarioInclusao
        });
        await _uow.CommitAsync();
        return new OperationResult<Usuario>(true, "Usuário criado com sucesso", created);
    }

    public async Task<OperationResult> UpdateAsync(Usuario usuario)
    {
        var existing = await _uow.Usuarios.GetByCpfAsync(usuario.Cpf);
        if (existing is not null && existing.Id != usuario.Id)
        {
            await _uow.Logs.AddAsync(new Log
            {
                Entidade = nameof(Usuario),
                Operacao = "Update",
                Sucesso = false,
                Mensagem = "CPF já utilizado",
                Tipo = LogTipo.Erro,
                Usuario = usuario.UsuarioAlteracao ?? "system"
            });
            return new OperationResult(false, "CPF já utilizado");
        }

        await _uow.Usuarios.UpdateAsync(usuario);
        await _uow.Logs.AddAsync(new Log
        {
            Entidade = nameof(Usuario),
            Operacao = "Update",
            Sucesso = true,
            Mensagem = "Usuário atualizado",
            Tipo = LogTipo.Sucesso,
            Usuario = usuario.UsuarioAlteracao ?? "system"
        });
        await _uow.CommitAsync();
        return new OperationResult(true, "Usuário atualizado com sucesso");
    }

    public async Task<OperationResult> DeleteAsync(int id)
    {
        await _uow.Usuarios.DeleteAsync(id);
        await _uow.Logs.AddAsync(new Log
        {
            Entidade = nameof(Usuario),
            Operacao = "Delete",
            Sucesso = true,
            Mensagem = "Usuário removido",
            Tipo = LogTipo.Sucesso,
            Usuario = "system"
        });
        await _uow.CommitAsync();
        return new OperationResult(true, "Usuário removido");
    }

    public async Task<OperationResult> AlterarAtivoAsync(int id, bool ativo, string usuario)
    {
        var obj = await _uow.Usuarios.GetByIdAsync(id);
        if (obj is null) return new OperationResult(false, "Usuário não encontrado");
        obj.Ativo = ativo;
        obj.UsuarioAlteracao = usuario;
        await _uow.Usuarios.UpdateAsync(obj);
        await _uow.Logs.AddAsync(new Log
        {
            Entidade = nameof(Usuario),
            Operacao = ativo ? "Ativar" : "Desativar",
            Sucesso = true,
            Mensagem = ativo ? "Usuário ativado" : "Usuário desativado",
            Tipo = LogTipo.Informacao,
            Usuario = usuario
        });
        await _uow.CommitAsync();
        return new OperationResult(true, "Operação concluída");
    }
}
