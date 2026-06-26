using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Sistema.APP.Services;
using Sistema.APP.Services.Interfaces;
using Sistema.CORE.Entities;
using Sistema.INFRA.Data;

namespace Sistema.INFRA.Services;

// F-H — motor de alertas de preço/provento (job recorrente "financas-alertas").
// Espelha o padrão do FinancasMarketDataService: usa o AppDbContext direto + IMensagemAppService para
// a notificação interna. À PROVA DE FALHA: try-catch por regra e por provento; o job nunca estoura.
// A notificação vai para todos os usuários do perfil de destino (admin) via EnviarParaPerfilAsync.
public class FinancasAlertaService(
    AppDbContext context,
    IMensagemAppService mensagens,
    IConfiguracaoLeitura config,
    ILogger<FinancasAlertaService> logger) : IFinancasAlertaService
{
    private static readonly CultureInfo Br = CultureInfo.GetCultureInfo("pt-BR");
    private readonly AppDbContext _context = context;
    private readonly IMensagemAppService _mensagens = mensagens;
    private readonly IConfiguracaoLeitura _config = config;
    private readonly ILogger<FinancasAlertaService> _logger = logger;

    public async Task ProcessarAlertasAsync(CancellationToken cancellationToken = default)
    {
        // Perfil de destino das notificações (single-user → admin perfil 1). Configurável.
        var perfilDestino = await _config.ObterIntAsync("Financas", "Alertas:PerfilDestino", 1, cancellationToken);

        await ProcessarAlertasPrecoAsync(perfilDestino, cancellationToken);
        await ProcessarAlertasProventoAsync(perfilDestino, cancellationToken);
    }

    private async Task ProcessarAlertasPrecoAsync(int perfilDestino, CancellationToken cancellationToken)
    {
        List<AlertaPreco> regras;
        try
        {
            regras = await _context.AlertasPreco
                .Include(x => x.AtivoFinanceiro)
                .Where(x => x.Ativo && x.AtivoFinanceiro != null)
                .ToListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            FinancasAlertaLog.FalhaCarregarRegras(_logger, ex);
            return;
        }

        if (regras.Count == 0)
            return;

        // Última cotação (BRL) por ativo. Uma leitura só para todas as regras (mais recente vence).
        Dictionary<int, decimal> precoPorAtivo;
        try
        {
            var cotacoes = await _context.CotacoesAtivosFinanceiros
                .AsNoTracking()
                .OrderByDescending(x => x.ConsultadoEm)
                .Select(x => new { x.AtivoFinanceiroId, x.PrecoBRL })
                .ToListAsync(cancellationToken);
            precoPorAtivo = cotacoes
                .GroupBy(x => x.AtivoFinanceiroId)
                .ToDictionary(g => g.Key, g => g.First().PrecoBRL);
        }
        catch (Exception ex)
        {
            FinancasAlertaLog.FalhaCarregarCotacoes(_logger, ex);
            return;
        }

        var algumaMudanca = false;
        foreach (var regra in regras)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                if (!precoPorAtivo.TryGetValue(regra.AtivoFinanceiroId, out var preco) || preco <= 0m)
                    continue; // sem cotação válida não decide (evita falso positivo com preço 0).

                var decisao = AvaliadorAlertaPreco.Avaliar(regra, preco);
                if (decisao.Disparar)
                {
                    var ticker = regra.AtivoFinanceiro?.Sigla ?? regra.AtivoFinanceiro?.Chave ?? "ativo";
                    var seta = regra.Direcao == DirecaoAlertaPreco.Acima ? "subiu para/acima de" : "caiu para/abaixo de";
                    var assunto = $"Alerta de preço: {ticker}";
                    var corpo = $"{ticker} {seta} {regra.Limiar.ToString("N2", Br)}. Preço atual: {preco.ToString("N2", Br)}.";

                    await _mensagens.EnviarParaPerfilAsync(null, perfilDestino, assunto, corpo, null, cancellationToken);

                    regra.DispararadoEm = DateTime.UtcNow;
                    regra.UltimoPreco = preco;
                    algumaMudanca = true;
                }
                else if (decisao.Rearmar)
                {
                    regra.DispararadoEm = null;
                    regra.UltimoPreco = preco;
                    algumaMudanca = true;
                }
            }
            catch (Exception ex)
            {
                FinancasAlertaLog.FalhaAvaliarAlerta(_logger, regra.Id, ex);
            }
        }

        if (algumaMudanca)
        {
            try { await _context.SaveChangesAsync(cancellationToken); }
            catch (Exception ex) { FinancasAlertaLog.FalhaSalvarRedisparo(_logger, ex); }
        }
    }

    // Provento recém-registrado para um ativo detido → notifica. Dedup pela própria janela + por
    // AlertaConfiabilidade (reuso, F-H): grava um marcador por provento; se já existe, não notifica 2×.
    private async Task ProcessarAlertasProventoAsync(int perfilDestino, CancellationToken cancellationToken)
    {
        List<RendimentoInvestimento> recentes;
        try
        {
            // O job de proventos é diário; uma janela de 25h cobre proventos inseridos desde a última rodada.
            var corte = DateTime.UtcNow.AddHours(-25);
            recentes = await _context.RendimentosInvestimento
                .AsNoTracking()
                .Include(x => x.Asset)
                .Where(x => x.AssetId != null && x.DataInclusao >= corte)
                .OrderBy(x => x.DataInclusao)
                .ToListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            FinancasAlertaLog.FalhaCarregarProventos(_logger, ex);
            return;
        }

        if (recentes.Count == 0)
            return;

        // Ativos com posição em aberto (detidos): só notifica provento de quem ainda detém.
        HashSet<int> detidos;
        try
        {
            detidos = (await _context.PosicoesAtivos
                .AsNoTracking()
                .Where(x => x.Quantidade > 0.000000001m)
                .Select(x => x.AtivoFinanceiroId)
                .ToListAsync(cancellationToken)).ToHashSet();
        }
        catch (Exception ex)
        {
            FinancasAlertaLog.FalhaCarregarPosicoes(_logger, ex);
            return;
        }

        // O marcador de dedup (AlertaConfiabilidade) tem FK obrigatória p/ carga (cascade). Proventos da
        // Brapi têm CargaFinanceiraId nulo → resolve a carga mais recente como âncora. Sem nenhuma carga,
        // não há como gravar o marcador → não notifica (evita re-alertar a cada rodada sem dedup).
        var cargaAncora = await _context.CargasFinanceiras
            .AsNoTracking()
            .OrderByDescending(x => x.ImportedAt)
            .Select(x => (int?)x.Id)
            .FirstOrDefaultAsync(cancellationToken);

        var algumGravado = false;
        foreach (var prov in recentes)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                if (prov.AssetId is null || !detidos.Contains(prov.AssetId.Value))
                    continue;

                var code = "ProventoAlertado";
                var marcador = $"R#{prov.Id}";
                // Dedup: o marcador é gravado como AlertaConfiabilidade; se já existe, não notifica de novo.
                var jaAlertado = await _context.AlertasConfiabilidade
                    .AnyAsync(a => a.Code == code && a.EntityType == nameof(RendimentoInvestimento) && a.EntityId == prov.Id, cancellationToken);
                if (jaAlertado)
                    continue;

                var cargaMarcador = prov.CargaFinanceiraId ?? cargaAncora;
                if (cargaMarcador is null)
                    continue; // sem carga não há como gravar o marcador de dedup → não notifica.

                var ticker = prov.Asset?.Sigla ?? prov.Asset?.Chave ?? "ativo";
                var data = prov.PaymentDate?.ToString("dd/MM/yyyy", Br) ?? prov.ReferenceDate?.ToString("dd/MM/yyyy", Br) ?? "-";
                var tipo = string.IsNullOrWhiteSpace(prov.IncomeType) ? "Provento" : prov.IncomeType;
                var assunto = $"Provento: {ticker}";
                var corpo = $"{tipo} de {ticker} no valor de {prov.Amount.ToString("N2", Br)} (data {data}).";

                await _mensagens.EnviarParaPerfilAsync(null, perfilDestino, assunto, corpo, null, cancellationToken);

                _context.AlertasConfiabilidade.Add(new AlertaConfiabilidade
                {
                    CargaFinanceiraId = cargaMarcador.Value,
                    EntityType = nameof(RendimentoInvestimento),
                    EntityId = prov.Id,
                    Severity = SeveridadeAlerta.Informacao,
                    Code = code,
                    Message = $"Provento de {ticker} notificado ({marcador}).",
                    CreatedAt = DateTime.UtcNow
                });
                algumGravado = true;
            }
            catch (Exception ex)
            {
                FinancasAlertaLog.FalhaAlertarProvento(_logger, prov.Id, ex);
            }
        }

        if (algumGravado)
        {
            try { await _context.SaveChangesAsync(cancellationToken); }
            catch (Exception ex) { FinancasAlertaLog.FalhaSalvarMarcadores(_logger, ex); }
        }
    }
}

// Logging source-generated (CA1848): evita boxing/alocação no caminho de log do job de alertas.
internal static partial class FinancasAlertaLog
{
    [LoggerMessage(EventId = 51, Level = LogLevel.Warning, Message = "F-H: falha ao carregar regras de alerta de preço.")]
    public static partial void FalhaCarregarRegras(ILogger logger, Exception ex);

    [LoggerMessage(EventId = 52, Level = LogLevel.Warning, Message = "F-H: falha ao carregar cotações para alertas de preço.")]
    public static partial void FalhaCarregarCotacoes(ILogger logger, Exception ex);

    [LoggerMessage(EventId = 53, Level = LogLevel.Warning, Message = "F-H: falha ao avaliar alerta de preço #{AlertaId}.")]
    public static partial void FalhaAvaliarAlerta(ILogger logger, int alertaId, Exception ex);

    [LoggerMessage(EventId = 54, Level = LogLevel.Warning, Message = "F-H: falha ao salvar estado de re-disparo dos alertas de preço.")]
    public static partial void FalhaSalvarRedisparo(ILogger logger, Exception ex);

    [LoggerMessage(EventId = 55, Level = LogLevel.Warning, Message = "F-H: falha ao carregar proventos recentes para alerta.")]
    public static partial void FalhaCarregarProventos(ILogger logger, Exception ex);

    [LoggerMessage(EventId = 56, Level = LogLevel.Warning, Message = "F-H: falha ao carregar posições para alerta de provento.")]
    public static partial void FalhaCarregarPosicoes(ILogger logger, Exception ex);

    [LoggerMessage(EventId = 57, Level = LogLevel.Warning, Message = "F-H: falha ao alertar provento #{ProventoId}.")]
    public static partial void FalhaAlertarProvento(ILogger logger, int proventoId, Exception ex);

    [LoggerMessage(EventId = 58, Level = LogLevel.Warning, Message = "F-H: falha ao gravar marcadores de provento alertado.")]
    public static partial void FalhaSalvarMarcadores(ILogger logger, Exception ex);
}
