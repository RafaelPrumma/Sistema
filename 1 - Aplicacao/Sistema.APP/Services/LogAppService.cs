using Sistema.APP.Services.Interfaces;
using Sistema.CORE.Entities;
using Sistema.CORE.Repositories.Interfaces;

namespace Sistema.APP.Services;

public class LogAppService(IUnitOfWork uow) : ILogAppService
{
    private readonly IUnitOfWork _uow = uow;

    public Task<IEnumerable<Log>> BuscarFiltradosAsync(DateTime? inicio, DateTime? fim, LogTipo? tipo, CancellationToken cancellationToken = default)
        => _uow.Logs.BuscarFiltradosAsync(inicio, fim, tipo, cancellationToken);

    public Task RegistrarAsync(string entidade, string operacao, bool sucesso, string mensagem, LogTipo tipo, string usuario, string? detalhe = null, CancellationToken cancellationToken = default)
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
        }, cancellationToken);
    }
}
