using Microsoft.EntityFrameworkCore;
using Sistema.CORE.Entities;
using Sistema.CORE.Interfaces;
using Sistema.INFRA.Data;

namespace Sistema.INFRA.Repositories;

public class PerfilRepository : IPerfilRepository
{
    private readonly AppDbContext _context;

    public PerfilRepository(AppDbContext context)
    {
        _context = context;
    }

    public Task<Perfil> AddAsync(Perfil perfil)
    {
        _context.Perfis.Add(perfil);
        return Task.FromResult(perfil);
    }

    public async Task DeleteAsync(int id)
    {
        var perfil = await _context.Perfis.FindAsync(id);
        if (perfil is null) return;
        _context.Perfis.Remove(perfil);
    }

    public async Task<IEnumerable<Perfil>> GetAllAsync()
    {
        return await _context.Perfis.ToListAsync();
    }

    public async Task<Perfil?> GetByNameAsync(string nome)
    {
        return await _context.Perfis.FirstOrDefaultAsync(p => p.Nome == nome);
    }
    
    public async Task<Perfil?> GetByIdAsync(int id)
    {
        return await _context.Perfis.FindAsync(id);
    }

    public Task UpdateAsync(Perfil perfil)
    {
        _context.Perfis.Update(perfil);
        return Task.CompletedTask;
    }
}
