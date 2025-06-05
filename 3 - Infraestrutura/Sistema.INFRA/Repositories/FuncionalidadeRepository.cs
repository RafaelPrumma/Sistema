using Microsoft.EntityFrameworkCore;
using Sistema.CORE.Entities;
using Sistema.CORE.Interfaces;
using Sistema.CORE.Common;
using Sistema.INFRA.Data;

namespace Sistema.INFRA.Repositories;

public class FuncionalidadeRepository : IFuncionalidadeRepository
{
    private readonly AppDbContext _context;

    public FuncionalidadeRepository(AppDbContext context)
    {
        _context = context;
    }

    public Task<Funcionalidade> AddAsync(Funcionalidade func)
    {
        _context.Funcionalidades.Add(func);
        return Task.FromResult(func);
    }

    public async Task DeleteAsync(int id)
    {
        var obj = await _context.Funcionalidades.FindAsync(id);
        if (obj is null) return;
        _context.Funcionalidades.Remove(obj);
    }

    public async Task<Funcionalidade?> GetByIdAsync(int id)
    {
        return await _context.Funcionalidades.FindAsync(id);
    }

    public Task UpdateAsync(Funcionalidade func)
    {
        _context.Funcionalidades.Update(func);
        return Task.CompletedTask;
    }

    public async Task<PagedResult<Funcionalidade>> GetPagedAsync(int page, int pageSize)
    {
        var query = _context.Funcionalidades.AsNoTracking().OrderBy(f => f.Id);
        return await query.ToPagedResultAsync(page, pageSize);
    }
}
