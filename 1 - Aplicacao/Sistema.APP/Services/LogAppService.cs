using Microsoft.AspNetCore.Http;
using Sistema.APP.Services.Interfaces;
using Sistema.CORE.Entities;
using Sistema.CORE.Repositories.Interfaces;
using System.Diagnostics;
using System.Globalization;
using System.Text.Json;

namespace Sistema.APP.Services;

public class LogAppService(IUnitOfWork uow, IHttpContextAccessor httpContextAccessor) : ILogAppService
{
    private const string AgrupamentoRetencaoLogs = "LogsRetencao";
    private const string ChaveAcessoMeses = "AcessoMeses";
    private const string ChaveComunicacaoMeses = "ComunicacaoMeses";
    private const string ChaveAdministracaoMeses = "AdministracaoMeses";
    private const string ChaveGeralMeses = "GeralMeses";
    private static readonly string FallbackDirectory = Path.Combine(AppContext.BaseDirectory, "log-fallback");
    private static readonly string FallbackFilePath = Path.Combine(FallbackDirectory, "audit-fallback.ndjson");

    private readonly IUnitOfWork _uow = uow;
    private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;

    public Task<IEnumerable<Log>> BuscarFiltradosAsync(DateTime? inicio, DateTime? fim, LogTipo? tipo, LogModulo? modulo = null, CancellationToken cancellationToken = default)
        => _uow.Logs.BuscarFiltradosAsync(inicio, fim, tipo, modulo, cancellationToken);

    public Task<IEnumerable<Log>> BuscarAcessoAsync(DateTime? inicio, DateTime? fim, LogTipo? tipo, CancellationToken cancellationToken = default)
        => _uow.Logs.BuscarFiltradosAsync(inicio, fim, tipo, LogModulo.Acesso, cancellationToken);

    public Task<IEnumerable<Log>> BuscarComunicacaoAsync(DateTime? inicio, DateTime? fim, LogTipo? tipo, CancellationToken cancellationToken = default)
        => _uow.Logs.BuscarFiltradosAsync(inicio, fim, tipo, LogModulo.Comunicacao, cancellationToken);

    public Task<IEnumerable<Log>> BuscarAdministracaoAsync(DateTime? inicio, DateTime? fim, LogTipo? tipo, CancellationToken cancellationToken = default)
        => _uow.Logs.BuscarFiltradosAsync(inicio, fim, tipo, LogModulo.Administracao, cancellationToken);

    public Task RegistrarAsync(string entidade, string operacao, bool sucesso, string mensagem, LogTipo tipo, string usuario, string? detalhe = null, CancellationToken cancellationToken = default)
        => RegistrarPorModuloAsync(entidade, operacao, sucesso, mensagem, tipo, usuario, LogModulo.Administracao, detalhe, cancellationToken);

    public async Task RegistrarPorModuloAsync(string entidade, string operacao, bool sucesso, string mensagem, LogTipo tipo, string usuario, LogModulo modulo, string? detalhe = null, CancellationToken cancellationToken = default)
    {
        var contexto = _httpContextAccessor.HttpContext;
        var correlationId = contexto?.Request.Headers["X-Correlation-ID"].FirstOrDefault();
        correlationId ??= contexto?.TraceIdentifier;

        var log = new Log
        {
            Entidade = entidade,
            Operacao = operacao,
            Sucesso = sucesso,
            Mensagem = mensagem,
            Tipo = tipo,
            Usuario = usuario,
            Detalhe = detalhe,
            Modulo = modulo,
            CorrelationId = correlationId,
            TraceId = Activity.Current?.TraceId.ToString(),
            SpanId = Activity.Current?.SpanId.ToString(),
            DataOperacao = DateTime.UtcNow
        };

        try
        {
            await _uow.Logs.AdicionarAsync(log, cancellationToken);
            await MigrarFallbackParaBancoAsync(cancellationToken);
            await AplicarPoliticaRetencaoAsync(cancellationToken);
        }
        catch
        {
            await GravarFallbackAsync(log, cancellationToken);
        }
    }

    public Task RegistrarAcessoAsync(string entidade, string operacao, bool sucesso, string mensagem, LogTipo tipo, string usuario, string? detalhe = null, CancellationToken cancellationToken = default)
        => RegistrarPorModuloAsync(entidade, operacao, sucesso, mensagem, tipo, usuario, LogModulo.Acesso, detalhe, cancellationToken);

    public Task RegistrarComunicacaoAsync(string entidade, string operacao, bool sucesso, string mensagem, LogTipo tipo, string usuario, string? detalhe = null, CancellationToken cancellationToken = default)
        => RegistrarPorModuloAsync(entidade, operacao, sucesso, mensagem, tipo, usuario, LogModulo.Comunicacao, detalhe, cancellationToken);

    public Task RegistrarAdministracaoAsync(string entidade, string operacao, bool sucesso, string mensagem, LogTipo tipo, string usuario, string? detalhe = null, CancellationToken cancellationToken = default)
        => RegistrarPorModuloAsync(entidade, operacao, sucesso, mensagem, tipo, usuario, LogModulo.Administracao, detalhe, cancellationToken);

    private async Task AplicarPoliticaRetencaoAsync(CancellationToken cancellationToken)
    {
        var mesesAcesso = await ObterMesesRetencaoAsync(ChaveAcessoMeses, 3, cancellationToken);
        var mesesComunicacao = await ObterMesesRetencaoAsync(ChaveComunicacaoMeses, 6, cancellationToken);
        var mesesAdministracao = await ObterMesesRetencaoAsync(ChaveAdministracaoMeses, 12, cancellationToken);
        var mesesGeral = await ObterMesesRetencaoAsync(ChaveGeralMeses, 12, cancellationToken);

        await _uow.Logs.RemoverAntesDeAsync(LogModulo.Acesso, DateTime.UtcNow.AddMonths(-mesesAcesso), cancellationToken);
        await _uow.Logs.RemoverAntesDeAsync(LogModulo.Comunicacao, DateTime.UtcNow.AddMonths(-mesesComunicacao), cancellationToken);
        await _uow.Logs.RemoverAntesDeAsync(LogModulo.Administracao, DateTime.UtcNow.AddMonths(-mesesAdministracao), cancellationToken);
        await _uow.Logs.RemoverAntesDeAsync(LogModulo.Geral, DateTime.UtcNow.AddMonths(-mesesGeral), cancellationToken);
    }

    private async Task<int> ObterMesesRetencaoAsync(string chave, int padrao, CancellationToken cancellationToken)
    {
        var config = await _uow.Configuracoes.BuscarPorChaveAsync(AgrupamentoRetencaoLogs, chave, cancellationToken);
        if (config is null || string.IsNullOrWhiteSpace(config.Valor))
            return padrao;

        return int.TryParse(config.Valor, NumberStyles.Integer, CultureInfo.InvariantCulture, out var meses) && meses > 0
            ? meses
            : padrao;
    }

    private static async Task GravarFallbackAsync(Log log, CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(FallbackDirectory);
        var linha = JsonSerializer.Serialize(log);
        await File.AppendAllTextAsync(FallbackFilePath, linha + Environment.NewLine, cancellationToken);
    }

    private async Task MigrarFallbackParaBancoAsync(CancellationToken cancellationToken)
    {
        if (!File.Exists(FallbackFilePath))
            return;

        var linhas = await File.ReadAllLinesAsync(FallbackFilePath, cancellationToken);
        if (linhas.Length == 0)
            return;

        var registrosValidos = new List<Log>();
        var linhasInvalidas = new List<string>();

        foreach (var linha in linhas)
        {
            if (string.IsNullOrWhiteSpace(linha))
                continue;

            try
            {
                var item = JsonSerializer.Deserialize<Log>(linha);
                if (item is not null)
                {
                    item.Id = 0;
                    if (item.DataOperacao == default)
                        item.DataOperacao = DateTime.UtcNow;
                    registrosValidos.Add(item);
                }
            }
            catch
            {
                linhasInvalidas.Add(linha);
            }
        }

        if (registrosValidos.Count != 0)
        {
            await _uow.Logs.AdicionarEmLoteAsync(registrosValidos, cancellationToken);
        }

        if (linhasInvalidas.Count == 0)
        {
            File.Delete(FallbackFilePath);
        }
        else
        {
            await File.WriteAllLinesAsync(FallbackFilePath, linhasInvalidas, cancellationToken);
        }
    }
}
