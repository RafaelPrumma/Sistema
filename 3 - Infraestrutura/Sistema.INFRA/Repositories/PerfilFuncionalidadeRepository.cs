using Microsoft.EntityFrameworkCore;
using Sistema.CORE.Entities;
using Sistema.CORE.Interfaces;
using Sistema.INFRA.Data;

namespace Sistema.INFRA.Repositories;

public class PerfilFuncionalidadeRepository : IPerfilFuncionalidadeRepository
{
    private readonly AppDbContext _context;

    public PerfilFuncionalidadeRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<PerfilFuncionalidade>> GetByPerfilIdAsync(int perfilId)
    {
        return await _context.PerfilFuncionalidades
            .Include(pf => pf.Funcionalidade)
            .Where(pf => pf.PerfilId == perfilId)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task SetForPerfilAsync(int perfilId, IEnumerable<PerfilFuncionalidade> funcs)
    {
        var existing = _context.PerfilFuncionalidades.Where(pf => pf.PerfilId == perfilId);
        _context.PerfilFuncionalidades.RemoveRange(existing);
        await _context.PerfilFuncionalidades.AddRangeAsync(funcs);
    }
}
