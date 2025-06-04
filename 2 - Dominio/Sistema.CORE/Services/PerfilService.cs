using Sistema.CORE.Common;
using Sistema.CORE.Entities;
using Sistema.CORE.Interfaces;

namespace Sistema.CORE.Services;

public class PerfilService : IPerfilService
{
    private readonly IPerfilRepository _repository;
    private readonly ILogRepository _logs;

    public PerfilService(IPerfilRepository repository, ILogRepository logs)
    {
        _repository = repository;
        _logs = logs;
    }

    public Task<IEnumerable<Perfil>> GetAllAsync() => _repository.GetAllAsync();

    public Task<Perfil?> GetByIdAsync(int id) => _repository.GetByIdAsync(id);

    public async Task<OperationResult<Perfil>> AddAsync(Perfil perfil)
    {
        var existing = await _repository.GetByNameAsync(perfil.Nome);
        if (existing is not null)
        {
            await _logs.AddAsync(new Log
            {
                Entidade = nameof(Perfil),
                Operacao = "Add",
                Sucesso = false,
                Mensagem = "Perfil já existe"
            });
            return new OperationResult<Perfil>(false, "Perfil já existe");
        }

        var created = await _repository.AddAsync(perfil);
        await _logs.AddAsync(new Log
        {
            Entidade = nameof(Perfil),
            Operacao = "Add",
            Sucesso = true,
            Mensagem = "Perfil criado"
        });
        return new OperationResult<Perfil>(true, "Perfil criado com sucesso", created);
    }

    public async Task<OperationResult> UpdateAsync(Perfil perfil)
    {
        var existing = await _repository.GetByNameAsync(perfil.Nome);
        if (existing is not null && existing.Id != perfil.Id)
        {
            await _logs.AddAsync(new Log
            {
                Entidade = nameof(Perfil),
                Operacao = "Update",
                Sucesso = false,
                Mensagem = "Nome já utilizado"
            });
            return new OperationResult(false, "Nome já utilizado");
        }

        await _repository.UpdateAsync(perfil);
        await _logs.AddAsync(new Log
        {
            Entidade = nameof(Perfil),
            Operacao = "Update",
            Sucesso = true,
            Mensagem = "Perfil atualizado"
        });
        return new OperationResult(true, "Perfil atualizado com sucesso");
    }

    public async Task<OperationResult> DeleteAsync(int id)
    {
        await _repository.DeleteAsync(id);
        await _logs.AddAsync(new Log
        {
            Entidade = nameof(Perfil),
            Operacao = "Delete",
            Sucesso = true,
            Mensagem = "Perfil removido"
        });
        return new OperationResult(true, "Perfil removido");
    }
}
