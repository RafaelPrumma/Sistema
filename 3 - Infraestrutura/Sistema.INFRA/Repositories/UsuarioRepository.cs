using Microsoft.EntityFrameworkCore;
using Sistema.CORE.Entities;
using Sistema.CORE.Interfaces; 
using Sistema.CORE.Common;
using Sistema.INFRA.Data;
using System.Linq; 

namespace Sistema.INFRA.Repositories;

public class UsuarioRepository : IUsuarioRepository
{
    private readonly AppDbContext _context;

    public UsuarioRepository(AppDbContext context)
    {
        _context = context;
    }

    public Task<Usuario> AddAsync(Usuario usuario)
    {
        _context.Usuarios.Add(usuario);
        return Task.FromResult(usuario);
    }

    public async Task DeleteAsync(int id)
    {
        var usuario = await _context.Usuarios.FindAsync(id);
        if (usuario is null) return;
        _context.Usuarios.Remove(usuario);
    }
 
    public Task<PagedResult<Usuario>> GetAllAsync(int page, int pageSize)
    {
        var query = _context.Usuarios.AsNoTracking().OrderBy(u => u.Id);
        return query.ToPagedResultAsync(page, pageSize);
    }

    public Task<PagedResult<Usuario>> GetFilteredAsync(DateTime? inicio, DateTime? fim, int? perfilId, bool? ativo, int page, int pageSize)
    {
        var query = _context.Usuarios.AsQueryable();
        if (inicio.HasValue)
            query = query.Where(u => u.DataInclusao >= inicio.Value);
        if (fim.HasValue)
            query = query.Where(u => u.DataInclusao <= fim.Value);
        if (perfilId.HasValue)
            query = query.Where(u => u.PerfilId == perfilId.Value);
        if (ativo.HasValue)
            query = query.Where(u => u.Ativo == ativo.Value);
        query = query.AsNoTracking().OrderBy(u => u.Id);
        return query.ToPagedResultAsync(page, pageSize);
    }

    public async Task<bool> ExistsActiveByPerfilAsync(int perfilId)
    {
        return await _context.Usuarios.AnyAsync(u => u.PerfilId == perfilId && u.Ativo);
    }

    public async Task<Usuario?> GetByCpfAsync(string cpf)
    {
        return await _context.Usuarios.FirstOrDefaultAsync(u => u.Cpf == cpf);
    }

    public async Task<Usuario?> GetByIdAsync(int id)
    {
        return await _context.Usuarios.FindAsync(id);
    }

    public Task UpdateAsync(Usuario usuario)
    {
        _context.Usuarios.Update(usuario);
        return Task.CompletedTask;
    }
}
