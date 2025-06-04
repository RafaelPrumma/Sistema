using Sistema.CORE.Entities;
using Sistema.CORE.Interfaces;
using Sistema.INFRA.Data;

namespace Sistema.INFRA.Repositories;

public class LogRepository : ILogRepository
{
    private readonly AppDbContext _context;

    public LogRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(Log log)
    {
        _context.Logs.Add(log);
        await _context.SaveChangesAsync();
    }
}
