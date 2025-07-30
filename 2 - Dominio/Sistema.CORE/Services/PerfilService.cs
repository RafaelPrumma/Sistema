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

    public Task<IEnumerable<Perfil>> BuscarTodosAsync() => _uow.Perfis.BuscarTodosAsync();

    public Task<Perfil?> BuscarPorIdAsync(int id) => _uow.Perfis.BuscarPorIdAsync(id);

    public async Task<OperationResult<Perfil>> AdicionarAsync(Perfil perfil)
    {
        var existing = await _uow.Perfis.BuscarPorNomeAsync(perfil.Nome);
        if (existing is not null)
        {
            await _uow.Logs.AdicionarAsync(new Log
            {
                Entidade = nameof(Perfil),
                Operacao = "Add",
                Sucesso = false,
                Mensagem = "Perfil já existe",
                Tipo = LogTipo.Erro,
                Usuario = perfil.UsuarioInclusao
            });
            await _uow.ConfirmarAsync();
            return new OperationResult<Perfil>(false, "Perfil já existe");
        }

        var created = await _uow.Perfis.AdicionarAsync(perfil);
        await _uow.Logs.AdicionarAsync(new Log
        {
            Entidade = nameof(Perfil),
            Operacao = "Add",
            Sucesso = true,
            Mensagem = "Perfil criado",
            Tipo = LogTipo.Sucesso,
            Usuario = perfil.UsuarioInclusao
        });
        await _uow.ConfirmarAsync();
        return new OperationResult<Perfil>(true, "Perfil criado com sucesso", created);
    }

    public async Task<OperationResult> AtualizarAsync(Perfil perfil)
    {
        var existing = await _uow.Perfis.BuscarPorNomeAsync(perfil.Nome);
        if (existing is not null && existing.Id != perfil.Id)
        {
            await _uow.Logs.AdicionarAsync(new Log
            {
                Entidade = nameof(Perfil),
                Operacao = "Update",
                Sucesso = false,
                Mensagem = "Nome já utilizado",
                Tipo = LogTipo.Erro,
                Usuario = perfil.UsuarioAlteracao ?? "system"
            });
            await _uow.ConfirmarAsync();
            return new OperationResult(false, "Nome já utilizado");
        }

        await _uow.Perfis.AtualizarAsync(perfil);
        await _uow.Logs.AdicionarAsync(new Log
        {
            Entidade = nameof(Perfil),
            Operacao = "Update",
            Sucesso = true,
            Mensagem = "Perfil atualizado",
            Tipo = LogTipo.Sucesso,
            Usuario = perfil.UsuarioAlteracao ?? "system"
        });
        await _uow.ConfirmarAsync();
        return new OperationResult(true, "Perfil atualizado com sucesso");
    }

    public async Task<OperationResult> RemoverAsync(int id)
    {
        await _uow.Perfis.RemoverAsync(id);
        await _uow.Logs.AdicionarAsync(new Log
        {
            Entidade = nameof(Perfil),
            Operacao = "Delete",
            Sucesso = true,
            Mensagem = "Perfil removido",
            Tipo = LogTipo.Sucesso,
            Usuario = "system"
        });
        await _uow.ConfirmarAsync();
        return new OperationResult(true, "Perfil removido");
    }
}
