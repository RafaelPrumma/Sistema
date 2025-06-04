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

    public Task<IEnumerable<Perfil>> GetAllAsync() => _uow.Perfis.GetAllAsync();

    public Task<Perfil?> GetByIdAsync(int id) => _uow.Perfis.GetByIdAsync(id);

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
                Mensagem = "Perfil já existe"
            });
            return new OperationResult<Perfil>(false, "Perfil já existe");
        }

        var created = await _uow.Perfis.AddAsync(perfil);
        await _uow.Logs.AddAsync(new Log
        {
            Entidade = nameof(Perfil),
            Operacao = "Add",
            Sucesso = true,
            Mensagem = "Perfil criado"
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
                Mensagem = "Nome já utilizado"
            });
            return new OperationResult(false, "Nome já utilizado");
        }

        await _uow.Perfis.UpdateAsync(perfil);
        await _uow.Logs.AddAsync(new Log
        {
            Entidade = nameof(Perfil),
            Operacao = "Update",
            Sucesso = true,
            Mensagem = "Perfil atualizado"
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
            Mensagem = "Perfil removido"
        });
        await _uow.CommitAsync();
        return new OperationResult(true, "Perfil removido");
    }
}
