using Sistema.CORE.Common;
using Sistema.CORE.Entities;
using Sistema.CORE.Interfaces;

namespace Sistema.CORE.Services;

public class PerfilService : IPerfilService
{
    private readonly IUnitOfWork _uow;
    private readonly ILogService _log;

    public PerfilService(IUnitOfWork uow, ILogService log)
    {
        _uow = uow;
        _log = log;
    }

public Task<PagedResult<Perfil>> BuscarTodosAsync(int page, int pageSize) =>
    _uow.Perfis.BuscarTodosAsync(page, pageSize);

    public Task<Perfil?> BuscarPorIdAsync(int id) => _uow.Perfis.BuscarPorIdAsync(id);

    public async Task<OperationResult<Perfil>> AdicionarAsync(Perfil perfil)
    {
        var existing = await _uow.Perfis.BuscarPorNomeAsync(perfil.Nome);
        if (existing is not null)
        {
            await _log.RegistrarAsync(nameof(Perfil), "Add", false, "Perfil já existe", LogTipo.Erro, perfil.UsuarioInclusao);
            await _uow.ConfirmarAsync();
            return new OperationResult<Perfil>(false, "Perfil já existe");
        }

        var created = await _uow.Perfis.AdicionarAsync(perfil);
        await _log.RegistrarAsync(nameof(Perfil), "Add", true, "Perfil criado", LogTipo.Sucesso, perfil.UsuarioInclusao);
        await _uow.ConfirmarAsync();
        return new OperationResult<Perfil>(true, "Perfil criado com sucesso", created);
    }

    public async Task<OperationResult> AtualizarAsync(Perfil perfil)
    {
        var existing = await _uow.Perfis.BuscarPorNomeAsync(perfil.Nome);
        if (existing is not null && existing.Id != perfil.Id)
        {
            await _log.RegistrarAsync(nameof(Perfil), "Update", false, "Nome já utilizado", LogTipo.Erro, perfil.UsuarioAlteracao ?? "system");
            await _uow.ConfirmarAsync();
            return new OperationResult(false, "Nome já utilizado");
        }

        await _uow.Perfis.AtualizarAsync(perfil);
        await _log.RegistrarAsync(nameof(Perfil), "Update", true, "Perfil atualizado", LogTipo.Sucesso, perfil.UsuarioAlteracao ?? "system");
        await _uow.ConfirmarAsync();
        return new OperationResult(true, "Perfil atualizado com sucesso");
    }

    public async Task<OperationResult> RemoverAsync(int id)
    {
        await _uow.Perfis.RemoverAsync(id);
        await _log.RegistrarAsync(nameof(Perfil), "Delete", true, "Perfil removido", LogTipo.Sucesso, "system");
        await _uow.ConfirmarAsync();
        return new OperationResult(true, "Perfil removido");
    }
}
