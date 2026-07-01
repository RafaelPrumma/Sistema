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
    IFinancasAppService financas,
    ILogger<FinancasAlertaService> logger) : IFinancasAlertaService
{
    private static readonly CultureInfo Br = CultureInfo.GetCultureInfo("pt-BR");
    private const decimal QuantidadeAbertaMinima = 0.000000001m;
    private readonly AppDbContext _context = context;
    private readonly IMensagemAppService _mensagens = mensagens;
    private readonly IConfiguracaoLeitura _config = config;
    private readonly IFinancasAppService _financas = financas;
    private readonly ILogger<FinancasAlertaService> _logger = logger;

    public async Task ProcessarAlertasAsync(CancellationToken cancellationToken = default)
    {
        // Perfil de destino das notificações (single-user → admin perfil 1). Configurável.
        var perfilDestino = await _config.ObterIntAsync("Financas", "Alertas:PerfilDestino", 1, cancellationToken);

        await ProcessarAlertasPrecoAsync(perfilDestino, cancellationToken);
        await ProcessarAlertasProventoAsync(perfilDestino, cancellationToken);
        // F-H (alertas de ESTADO, persistem entre rodadas): dedup por marcador AlertaConfiabilidade
        // com Code próprio por tipo+ativo; re-arma removendo o marcador quando a condição deixa de valer.
        await ProcessarAlertasCotacaoCriticaAsync(perfilDestino, cancellationToken);
        await ProcessarAlertasSemCarteiraAsync(perfilDestino, cancellationToken);
        await ProcessarAlertasDivergenciaCustodiaAsync(perfilDestino, cancellationToken);
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

    // ── F-H: alertas de ESTADO ────────────────────────────────────────────────────────────────────
    // Estes três são condições que PERSISTEM entre rodadas (não eventos pontuais). Para não notificar a
    // cada execução do job, cada um grava um marcador AlertaConfiabilidade (Code por tipo, EntityId =
    // id do ativo). Re-arme: quando a condição deixa de valer para o ativo, o marcador é removido —
    // assim a condição pode voltar a alertar no futuro. Padrão de dedup espelha ProcessarAlertasProvento.

    // Resolve a carga-âncora (FK obrigatória do marcador AlertaConfiabilidade). Sem nenhuma carga não há
    // como gravar marcador → o chamador NÃO notifica (evita re-alertar a cada rodada sem dedup).
    private Task<int?> ObterCargaAncoraAsync(CancellationToken cancellationToken)
        => _context.CargasFinanceiras
            .AsNoTracking()
            .OrderByDescending(x => x.ImportedAt)
            .Select(x => (int?)x.Id)
            .FirstOrDefaultAsync(cancellationToken);

    // F-H/1 — cotação vencida / sem fonte. Para cada ativo com posição > 0 cuja cotação esteja em estado
    // crítico (Severidade.Critico do ClassificadorSaudeCotacao: fallback custo / falhou / não suportada),
    // notifica UMA vez enquanto a condição persistir; re-arma quando a cotação volta a ser utilizável.
    private async Task ProcessarAlertasCotacaoCriticaAsync(int perfilDestino, CancellationToken cancellationToken)
    {
        const string code = "CotacaoCriticaAlertada";
        const string entityType = nameof(AtivoFinanceiro);

        List<PosicaoAtivo> detidas;
        Dictionary<int, List<CotacaoAtivoFinanceiro>> cotacoesPorAtivo;
        try
        {
            detidas = await _context.PosicoesAtivos
                .AsNoTracking()
                .Include(p => p.AtivoFinanceiro)
                .Where(p => p.Quantidade > QuantidadeAbertaMinima && p.AtivoFinanceiro != null)
                .ToListAsync(cancellationToken);

            var cotacoes = await _context.CotacoesAtivosFinanceiros
                .AsNoTracking()
                .ToListAsync(cancellationToken);
            cotacoesPorAtivo = cotacoes
                .GroupBy(c => c.AtivoFinanceiroId)
                .ToDictionary(g => g.Key, g => g.ToList());
        }
        catch (Exception ex)
        {
            FinancasAlertaLog.FalhaCarregarCotacaoCritica(_logger, ex);
            return;
        }

        if (detidas.Count == 0)
            return;

        var cargaAncora = await ObterCargaAncoraAsync(cancellationToken);
        var agora = DateTime.UtcNow;
        var algumaMudanca = false;

        foreach (var pos in detidas)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                var ativo = pos.AtivoFinanceiro!;
                cotacoesPorAtivo.TryGetValue(ativo.Id, out var doAtivo);
                doAtivo ??= [];

                // Mesma escolha da valoração/F-S: prefere cotação com PrecoBRL>0 (a mais recente); sem
                // nenhuma utilizável, pega a última tentada para explicar o motivo do estado crítico.
                var utilizavel = doAtivo
                    .Where(c => c.PrecoBRL > 0m)
                    .OrderByDescending(c => c.ConsultadoEm)
                    .FirstOrDefault();
                var referencia = utilizavel ?? doAtivo.OrderByDescending(c => c.ConsultadoEm).FirstOrDefault();
                var temPreco = utilizavel is not null;
                var provedor = referencia?.Provedor ?? ProvedorCotacao.Manual;
                var statusCotacao = referencia?.Status ?? StatusCotacao.Falhou;
                var vencida = utilizavel is { ExpiraEm: not null } && utilizavel.ExpiraEm < agora;

                var classificacao = ClassificadorSaudeCotacao.Classificar(temPreco, provedor, statusCotacao, vencida);
                var critico = classificacao.Nivel == ClassificadorSaudeCotacao.Severidade.Critico;

                var marcador = await BuscarMarcadorAsync(code, entityType, ativo.Id, cancellationToken);

                if (critico)
                {
                    if (marcador is not null)
                        continue; // já notificado e ainda crítico → não repete.
                    if (cargaAncora is null)
                        continue; // sem carga não há como gravar dedup → não notifica.

                    var ticker = ativo.Sigla ?? ativo.Chave ?? ativo.Nome ?? "ativo";
                    var assunto = $"Cotação sem fonte: {ticker}";
                    var corpo = $"A cotação de {ticker} está em estado crítico ({classificacao.Status}). " +
                                "A posição está sendo valorada pelo custo ou sem preço de mercado utilizável.";
                    await _mensagens.EnviarParaPerfilAsync(null, perfilDestino, assunto, corpo, null, cancellationToken);

                    GravarMarcador(cargaAncora.Value, entityType, ativo.Id, code,
                        $"Cotação crítica de {ticker} notificada ({classificacao.Status}).");
                    algumaMudanca = true;
                }
                else if (marcador is not null)
                {
                    // Re-arme: voltou a ter cotação utilizável → remove o marcador para poder alertar de novo.
                    _context.AlertasConfiabilidade.Remove(marcador);
                    algumaMudanca = true;
                }
            }
            catch (Exception ex)
            {
                FinancasAlertaLog.FalhaAlertarCotacaoCritica(_logger, pos.AtivoFinanceiroId, ex);
            }
        }

        await SalvarSeMudouAsync(algumaMudanca, cancellationToken);
    }

    // F-H/2 — ativo detido (posição > 0) sem nenhuma carteira ativa. Notifica uma vez; re-arma quando o
    // ativo passa a estar em alguma CarteiraAtivoFinanceiro com Ativo=true (ou deixa de ser detido).
    private async Task ProcessarAlertasSemCarteiraAsync(int perfilDestino, CancellationToken cancellationToken)
    {
        const string code = "SemCarteiraAlertado";
        const string entityType = nameof(AtivoFinanceiro);

        List<PosicaoAtivo> detidas;
        HashSet<int> emCarteira;
        try
        {
            detidas = await _context.PosicoesAtivos
                .AsNoTracking()
                .Include(p => p.AtivoFinanceiro)
                .Where(p => p.Quantidade > QuantidadeAbertaMinima && p.AtivoFinanceiro != null)
                .ToListAsync(cancellationToken);

            emCarteira = (await _context.CarteirasAtivosFinanceiros
                .AsNoTracking()
                .Where(c => c.Ativo && c.CarteiraFinanceira != null && c.CarteiraFinanceira.Ativo)
                .Select(c => c.AtivoFinanceiroId)
                .ToListAsync(cancellationToken)).ToHashSet();
        }
        catch (Exception ex)
        {
            FinancasAlertaLog.FalhaCarregarSemCarteira(_logger, ex);
            return;
        }

        if (detidas.Count == 0)
            return;

        var porId = detidas
            .GroupBy(p => p.AtivoFinanceiroId)
            .ToDictionary(g => g.Key, g => g.First().AtivoFinanceiro!);
        var semCarteira = AvaliadorAlertaEstado.AtivosSemCarteira(porId.Keys, emCarteira).ToHashSet();

        var cargaAncora = await ObterCargaAncoraAsync(cancellationToken);
        var algumaMudanca = false;

        // Notifica os detidos sem carteira ainda não marcados.
        foreach (var ativoId in semCarteira)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                if (await BuscarMarcadorAsync(code, entityType, ativoId, cancellationToken) is not null)
                    continue;
                if (cargaAncora is null)
                    continue;

                var ativo = porId[ativoId];
                var ticker = ativo.Sigla ?? ativo.Chave ?? ativo.Nome ?? "ativo";
                var assunto = $"Ativo sem carteira: {ticker}";
                var corpo = $"{ticker} tem posição em aberto mas não está em nenhuma carteira ativa. " +
                            "Classifique-o em uma carteira para que ele apareça corretamente no rebalanceamento.";
                await _mensagens.EnviarParaPerfilAsync(null, perfilDestino, assunto, corpo, null, cancellationToken);

                GravarMarcador(cargaAncora.Value, entityType, ativoId, code,
                    $"Ativo {ticker} detido sem carteira notificado.");
                algumaMudanca = true;
            }
            catch (Exception ex)
            {
                FinancasAlertaLog.FalhaAlertarSemCarteira(_logger, ativoId, ex);
            }
        }

        // Re-arme: marcadores de ativos que voltaram a ter carteira (ou deixaram de ser detidos) são removidos.
        try
        {
            var marcadores = await _context.AlertasConfiabilidade
                .Where(a => a.Code == code && a.EntityType == entityType && a.EntityId != null)
                .ToListAsync(cancellationToken);
            foreach (var m in marcadores)
            {
                if (!semCarteira.Contains(m.EntityId!.Value))
                {
                    _context.AlertasConfiabilidade.Remove(m);
                    algumaMudanca = true;
                }
            }
        }
        catch (Exception ex)
        {
            FinancasAlertaLog.FalhaRearmarSemCarteira(_logger, ex);
        }

        await SalvarSeMudouAsync(algumaMudanca, cancellationToken);
    }

    // F-H/3 — divergência calculado×custódia (reconciliação B3). REUSA o número da ilha de reconciliação
    // (ObterReconciliacaoDashboardAsync → ValorTotalVariacao, o valor BRL parado no ativo VARIAÇÃO) e
    // dispara quando ele passa de um limiar configurável (em módulo BRL, ou em % do patrimônio de
    // referência). Marcador único (EntityId=0); re-arma quando a divergência cai abaixo do limiar.
    private async Task ProcessarAlertasDivergenciaCustodiaAsync(int perfilDestino, CancellationToken cancellationToken)
    {
        const string code = "DivergenciaCustodiaAlertada";
        const string entityType = "ReconciliacaoB3";
        const int entityId = 0; // alerta global (não por ativo) → id fixo.

        decimal limiarAbsoluto;
        decimal limiarPercentual;
        try
        {
            // Limiares configuráveis (inteiros): BRL absoluto e percentual do patrimônio de referência.
            limiarAbsoluto = await _config.ObterIntAsync("Financas", "Alertas:DivergenciaValorLimiar", 1000, cancellationToken);
            limiarPercentual = await _config.ObterIntAsync("Financas", "Alertas:DivergenciaPctLimiar", 5, cancellationToken);
        }
        catch (Exception ex)
        {
            FinancasAlertaLog.FalhaConfigDivergencia(_logger, ex);
            return;
        }

        decimal valorDivergencia;
        try
        {
            // Reusa o MESMO número que a ilha de reconciliação mostra ("Valor no ativo VARIAÇÃO").
            var reconc = await _financas.ObterReconciliacaoDashboardAsync(cancellationToken);
            if (!reconc.TemDados)
            {
                // Sem reconciliação ativa → não há divergência. Re-arma marcador residual, se houver.
                await RemoverMarcadorSeExistirAsync(code, entityType, entityId, cancellationToken);
                return;
            }
            valorDivergencia = reconc.ValorTotalVariacao;
        }
        catch (Exception ex)
        {
            FinancasAlertaLog.FalhaCarregarDivergencia(_logger, ex);
            return;
        }

        // Patrimônio de referência (base BRL do percentual): soma do custo das posições detidas. É uma
        // referência estável do read model; não recalcula a divergência (essa vem da ilha, acima).
        decimal patrimonioReferencia;
        try
        {
            patrimonioReferencia = await _context.PosicoesAtivos
                .AsNoTracking()
                .Where(p => p.Quantidade > QuantidadeAbertaMinima)
                .SumAsync(p => p.CustoTotal, cancellationToken);
        }
        catch (Exception ex)
        {
            FinancasAlertaLog.FalhaCarregarDivergencia(_logger, ex);
            return;
        }

        var dispara = AvaliadorAlertaEstado.DivergenciaAcimaDoLimiar(
            valorDivergencia, patrimonioReferencia, limiarAbsoluto, limiarPercentual);

        var algumaMudanca = false;
        try
        {
            var marcador = await BuscarMarcadorAsync(code, entityType, entityId, cancellationToken);
            if (dispara)
            {
                if (marcador is null)
                {
                    var cargaAncora = await ObterCargaAncoraAsync(cancellationToken);
                    if (cargaAncora is not null)
                    {
                        var modulo = Math.Abs(valorDivergencia).ToString("N2", Br);
                        var assunto = "Divergência de custódia (reconciliação B3)";
                        var corpo = $"A diferença entre o calculado e a custódia oficial da B3 está em {modulo} " +
                                    "(valor no ativo VARIAÇÃO), acima do limiar configurado. Revise a reconciliação no dashboard.";
                        await _mensagens.EnviarParaPerfilAsync(null, perfilDestino, assunto, corpo, null, cancellationToken);

                        GravarMarcador(cargaAncora.Value, entityType, entityId, code,
                            $"Divergência de custódia {modulo} notificada.");
                        algumaMudanca = true;
                    }
                }
            }
            else if (marcador is not null)
            {
                _context.AlertasConfiabilidade.Remove(marcador); // re-arme: divergência voltou abaixo do limiar.
                algumaMudanca = true;
            }
        }
        catch (Exception ex)
        {
            FinancasAlertaLog.FalhaAlertarDivergencia(_logger, ex);
        }

        await SalvarSeMudouAsync(algumaMudanca, cancellationToken);
    }

    // ── Helpers de marcador (dedup/re-arme) ───────────────────────────────────────────────────────
    private Task<AlertaConfiabilidade?> BuscarMarcadorAsync(string code, string entityType, int entityId, CancellationToken cancellationToken)
        => _context.AlertasConfiabilidade
            .FirstOrDefaultAsync(a => a.Code == code && a.EntityType == entityType && a.EntityId == entityId, cancellationToken);

    private void GravarMarcador(int cargaAncora, string entityType, int entityId, string code, string mensagem)
        => _context.AlertasConfiabilidade.Add(new AlertaConfiabilidade
        {
            CargaFinanceiraId = cargaAncora,
            EntityType = entityType,
            EntityId = entityId,
            Severity = SeveridadeAlerta.Atencao,
            Code = code,
            Message = mensagem,
            CreatedAt = DateTime.UtcNow
        });

    private async Task RemoverMarcadorSeExistirAsync(string code, string entityType, int entityId, CancellationToken cancellationToken)
    {
        try
        {
            var marcador = await BuscarMarcadorAsync(code, entityType, entityId, cancellationToken);
            if (marcador is not null)
            {
                _context.AlertasConfiabilidade.Remove(marcador);
                await _context.SaveChangesAsync(cancellationToken);
            }
        }
        catch (Exception ex)
        {
            FinancasAlertaLog.FalhaSalvarMarcadores(_logger, ex);
        }
    }

    private async Task SalvarSeMudouAsync(bool algumaMudanca, CancellationToken cancellationToken)
    {
        if (!algumaMudanca)
            return;
        try { await _context.SaveChangesAsync(cancellationToken); }
        catch (Exception ex) { FinancasAlertaLog.FalhaSalvarMarcadores(_logger, ex); }
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

    [LoggerMessage(EventId = 59, Level = LogLevel.Warning, Message = "F-H: falha ao carregar dados de cotação crítica.")]
    public static partial void FalhaCarregarCotacaoCritica(ILogger logger, Exception ex);

    [LoggerMessage(EventId = 60, Level = LogLevel.Warning, Message = "F-H: falha ao alertar cotação crítica do ativo #{AtivoId}.")]
    public static partial void FalhaAlertarCotacaoCritica(ILogger logger, int ativoId, Exception ex);

    [LoggerMessage(EventId = 61, Level = LogLevel.Warning, Message = "F-H: falha ao carregar dados de ativo sem carteira.")]
    public static partial void FalhaCarregarSemCarteira(ILogger logger, Exception ex);

    [LoggerMessage(EventId = 62, Level = LogLevel.Warning, Message = "F-H: falha ao alertar ativo #{AtivoId} sem carteira.")]
    public static partial void FalhaAlertarSemCarteira(ILogger logger, int ativoId, Exception ex);

    [LoggerMessage(EventId = 63, Level = LogLevel.Warning, Message = "F-H: falha ao re-armar marcadores de ativo sem carteira.")]
    public static partial void FalhaRearmarSemCarteira(ILogger logger, Exception ex);

    [LoggerMessage(EventId = 64, Level = LogLevel.Warning, Message = "F-H: falha ao ler config de divergência de custódia.")]
    public static partial void FalhaConfigDivergencia(ILogger logger, Exception ex);

    [LoggerMessage(EventId = 65, Level = LogLevel.Warning, Message = "F-H: falha ao carregar divergência de custódia para alerta.")]
    public static partial void FalhaCarregarDivergencia(ILogger logger, Exception ex);

    [LoggerMessage(EventId = 66, Level = LogLevel.Warning, Message = "F-H: falha ao alertar divergência de custódia.")]
    public static partial void FalhaAlertarDivergencia(ILogger logger, Exception ex);
}
