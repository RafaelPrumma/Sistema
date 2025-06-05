using Sistema.CORE.Common;
using Sistema.CORE.Entities;
using Sistema.CORE.Interfaces;

namespace Sistema.CORE.Services;

    public class PerfilService : IPerfilService
{
    private readonly IUnitOfWork _uow;

    public PerfilService(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public Task<PagedResult<Perfil>> GetAllAsync(int page, int pageSize) => _uow.Perfis.GetAllAsync(page, pageSize);

    public Task<Perfil?> GetByIdAsync(int id) => _uow.Perfis.GetByIdAsync(id);

    public Task<PagedResult<Perfil>> GetFilteredAsync(bool? ativo, int page, int pageSize) => _uow.Perfis.GetFilteredAsync(ativo, page, pageSize);

    public async Task<OperationResult<Perfil>> AddAsync(Perfil perfil)
    {
        var existing = await _uow.Perfis.GetByNameAsync(perfil.Nome);
        if (existing is not null)
        {
            await _uow.Logs.AddAsync(new Log
            {
                Entidade = nameof(Perfil),
                Operacao = "Add",
                Sucesso = false,
                Mensagem = "Perfil já existe",
                Tipo = LogTipo.Erro,
                Usuario = perfil.UsuarioInclusao
            });
            return new OperationResult<Perfil>(false, "Perfil já existe");
        }

        var created = await _uow.Perfis.AddAsync(perfil);
        await _uow.Logs.AddAsync(new Log
        {
            Entidade = nameof(Perfil),
            Operacao = "Add",
            Sucesso = true,
            Mensagem = "Perfil criado",
            Tipo = LogTipo.Sucesso,
            Usuario = perfil.UsuarioInclusao
        });
        await _uow.CommitAsync();
        return new OperationResult<Perfil>(true, "Perfil criado com sucesso", created);
    }

    public async Task<OperationResult> UpdateAsync(Perfil perfil)
    {
        var existing = await _uow.Perfis.GetByNameAsync(perfil.Nome);
        if (existing is not null && existing.Id != perfil.Id)
        {
            await _uow.Logs.AddAsync(new Log
            {
                Entidade = nameof(Perfil),
                Operacao = "Update",
                Sucesso = false,
                Mensagem = "Nome já utilizado",
                Tipo = LogTipo.Erro,
                Usuario = perfil.UsuarioAlteracao ?? "system"
            });
            return new OperationResult(false, "Nome já utilizado");
        }

        await _uow.Perfis.UpdateAsync(perfil);
        await _uow.Logs.AddAsync(new Log
        {
            Entidade = nameof(Perfil),
            Operacao = "Update",
            Sucesso = true,
            Mensagem = "Perfil atualizado",
            Tipo = LogTipo.Sucesso,
            Usuario = perfil.UsuarioAlteracao ?? "system"
        });
        await _uow.CommitAsync();
        return new OperationResult(true, "Perfil atualizado com sucesso");
    }

    public async Task<OperationResult> DeleteAsync(int id)
    {
        await _uow.Perfis.DeleteAsync(id);
        await _uow.Logs.AddAsync(new Log
        {
            Entidade = nameof(Perfil),
            Operacao = "Delete",
            Sucesso = true,
            Mensagem = "Perfil removido",
            Tipo = LogTipo.Sucesso,
            Usuario = "system"
        });
        await _uow.CommitAsync();
        return new OperationResult(true, "Perfil removido");
    }

    public async Task<OperationResult> AlterarAtivoAsync(int id, bool ativo, string usuario)
    {
        var perfil = await _uow.Perfis.GetByIdAsync(id);
        if (perfil is null) return new OperationResult(false, "Perfil não encontrado");
        if (!ativo)
        {
            var hasUsers = await _uow.Usuarios.ExistsActiveByPerfilAsync(id);
            if (hasUsers)
            {
                return new OperationResult(false, "Não é possível desativar, há usuários ativos");
            }
        }

        perfil.Ativo = ativo;
        perfil.UsuarioAlteracao = usuario;
        await _uow.Perfis.UpdateAsync(perfil);
        await _uow.Logs.AddAsync(new Log
        {
            Entidade = nameof(Perfil),
            Operacao = ativo ? "Ativar" : "Desativar",
            Sucesso = true,
            Mensagem = ativo ? "Perfil ativado" : "Perfil desativado",
            Tipo = LogTipo.Informacao,
            Usuario = usuario
        });
        await _uow.CommitAsync();
        return new OperationResult(true, "Operação concluída");
    }

    public Task<IEnumerable<PerfilFuncionalidade>> GetFuncionalidadesAsync(int perfilId)
        => _uow.PerfilFuncionalidades.GetByPerfilIdAsync(perfilId);

    public async Task<OperationResult> DefinirFuncionalidadesAsync(int perfilId, IEnumerable<PerfilFuncionalidade> funcoes)
    {
        await _uow.PerfilFuncionalidades.SetForPerfilAsync(perfilId, funcoes);
        await _uow.CommitAsync();
        return new OperationResult(true, "Funcionalidades atualizadas");
    }
}
