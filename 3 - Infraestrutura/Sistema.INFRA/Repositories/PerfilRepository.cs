using Microsoft.EntityFrameworkCore;
using Sistema.CORE.Common;
using Sistema.CORE.Entities;
using Sistema.CORE.Repositories.Interfaces;
using Sistema.INFRA.Data;

namespace Sistema.INFRA.Repositories;

public class PerfilRepository(AppDbContext context) : IPerfilRepository
{
    private readonly AppDbContext _context = context;

	public async Task<Perfil> AdicionarAsync(Perfil perfil, CancellationToken cancellationToken = default)
    {
        await _context.Perfis.AddAsync(perfil, cancellationToken);
        return perfil;
    }

    public async Task RemoverAsync(int id, CancellationToken cancellationToken = default)
    {
        var perfil = await _context.Perfis.FindAsync([id], cancellationToken);
        if (perfil is null) return;
        _context.Perfis.Remove(perfil);
    }

    public async Task<PagedResult<Perfil>> BuscarTodosAsync(int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = _context.Perfis.AsNoTracking().OrderBy(p => p.Id);
        return await query.ToPagedResultAsync(page, pageSize, cancellationToken);
    }

    public async Task<PagedResult<Perfil>> BuscarFiltradosAsync(bool? ativo, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = _context.Perfis.AsQueryable();
        if (ativo.HasValue)
        {
            query = query.Where(p => p.Ativo == ativo.Value);
        }
        query = query.AsNoTracking().OrderBy(p => p.Id);
        return await query.ToPagedResultAsync(page, pageSize, cancellationToken);
    }

    public async Task<Perfil?> BuscarPorNomeAsync(string nome, CancellationToken cancellationToken = default)
    {
        return await _context.Perfis
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Nome == nome, cancellationToken);
    }

    public async Task<Perfil?> BuscarPorIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.Perfis.FindAsync([id], cancellationToken);
    }

    public Task AtualizarAsync(Perfil perfil)
    {
        _context.Perfis.Update(perfil);
        return Task.CompletedTask;
    }
}
