using Sistema.CORE.Entities;

namespace Sistema.CORE.Interfaces;

public interface ILogService
{

    Task<IEnumerable<Log>> BuscarFiltradosAsync(DateTime? inicio, DateTime? fim, LogTipo? tipo);



    Task RegistrarAsync(string entidade, string operacao, bool sucesso, string mensagem, LogTipo tipo, string usuario, string? detalhe = null);
}

