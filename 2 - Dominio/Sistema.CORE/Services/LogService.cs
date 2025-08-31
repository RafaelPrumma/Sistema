using Sistema.CORE.Entities;
using Sistema.CORE.Interfaces;

namespace Sistema.CORE.Services;

public class LogService : ILogService
{
    private readonly IUnitOfWork _uow;

    public LogService(IUnitOfWork uow)
    {
        _uow = uow;
    }


    public Task<IEnumerable<Log>> BuscarFiltradosAsync(DateTime? inicio, DateTime? fim, LogTipo? tipo)
        => _uow.Logs.BuscarFiltradosAsync(inicio, fim, tipo);
    public Task RegistrarAsync(string entidade, string operacao, bool sucesso, string mensagem, LogTipo tipo, string usuario, string? detalhe = null)
    {
        return _uow.Logs.AdicionarAsync(new Log
        {
            Entidade = entidade,
            Operacao = operacao,
            Sucesso = sucesso,
            Mensagem = mensagem,
            Tipo = tipo,
            Usuario = usuario,
            Detalhe = detalhe
        });
    }
}

