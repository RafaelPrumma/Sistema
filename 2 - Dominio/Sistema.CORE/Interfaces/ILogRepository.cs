using Sistema.CORE.Entities;

namespace Sistema.CORE.Interfaces;

public interface ILogRepository
{
    Task AdicionarAsync(Log log);
    Task<IEnumerable<Log>> BuscarFiltradosAsync(DateTime? inicio, DateTime? fim, LogTipo? tipo);
}
