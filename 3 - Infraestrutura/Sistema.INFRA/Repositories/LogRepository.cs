using Sistema.CORE.Entities;
using Sistema.CORE.Interfaces;
using Sistema.INFRA.Data;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace Sistema.INFRA.Repositories;

public class LogRepository : ILogRepository
{
    private readonly AppDbContext _context;

    public LogRepository(AppDbContext context)
    {
        _context = context;
    }

    public Task AddAsync(Log log)
    {
        _context.Logs.Add(log);
        return Task.CompletedTask;
    }

    public async Task<IEnumerable<Log>> GetFilteredAsync(DateTime? inicio, DateTime? fim, LogTipo? tipo)
    {
        var query = _context.Logs.AsQueryable();
        if (inicio.HasValue)
            query = query.Where(l => l.DataOperacao >= inicio.Value);
        if (fim.HasValue)
            query = query.Where(l => l.DataOperacao <= fim.Value);
        if (tipo.HasValue)
            query = query.Where(l => l.Tipo == tipo.Value);
        return await query.AsNoTracking().ToListAsync();
    }
}
