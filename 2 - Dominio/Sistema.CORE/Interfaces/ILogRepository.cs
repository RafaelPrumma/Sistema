using Sistema.CORE.Entities;

namespace Sistema.CORE.Interfaces;

public interface ILogRepository
{
    Task AddAsync(Log log);
}
