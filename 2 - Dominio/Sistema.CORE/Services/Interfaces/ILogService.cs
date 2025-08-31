using Sistema.CORE.Entities;

namespace Sistema.CORE.Interfaces;

public interface ILogService
{
    Task<IEnumerable<Log>> BuscarFiltradosAsync(DateTime? inicio, DateTime? fim, LogTipo? tipo);
}

