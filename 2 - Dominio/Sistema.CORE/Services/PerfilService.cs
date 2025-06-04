using Sistema.CORE.Entities;
using Sistema.CORE.Interfaces;

namespace Sistema.CORE.Services;

public class PerfilService : IPerfilService
{
    private readonly IPerfilRepository _repository;

    public PerfilService(IPerfilRepository repository)
    {
        _repository = repository;
    }

    public Task<IEnumerable<Perfil>> GetAllAsync() => _repository.GetAllAsync();

    public Task<Perfil?> GetByIdAsync(int id) => _repository.GetByIdAsync(id);

    public Task<Perfil> AddAsync(Perfil perfil) => _repository.AddAsync(perfil);

    public Task UpdateAsync(Perfil perfil) => _repository.UpdateAsync(perfil);

    public Task DeleteAsync(int id) => _repository.DeleteAsync(id);
}
