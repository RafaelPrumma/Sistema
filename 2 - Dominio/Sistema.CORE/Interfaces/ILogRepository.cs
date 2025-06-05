using Sistema.CORE.Entities;

namespace Sistema.CORE.Interfaces;

public interface ILogRepository
{
    Task AddAsync(Log log);
    Task<IEnumerable<Log>> GetFilteredAsync(DateTime? inicio, DateTime? fim, LogTipo? tipo);
}
