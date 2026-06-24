using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Sistema.CORE.Entities;
using Sistema.INFRA.Data;
using Sistema.INFRA.Services;

namespace Sistema.INFRA.Importers;

public class FinancasDataRepairService(AppDbContext context, ILogger<FinancasDataRepairService> logger)
{
    private const int RepairVersion = 4;
    private const string Agrupamento = "Financas";
    private const string ChaveVersao = "ReparoAtivosVersao";
    private const string UsuarioSistema = "financas-repair";

    private readonly AppDbContext _context = context;
    private readonly ILogger<FinancasDataRepairService> _logger = logger;

    public async Task RepararAsync(CancellationToken cancellationToken = default)
    {
        var config = await _context.Configuracoes
            .FirstOrDefaultAsync(x => x.Agrupamento == Agrupamento && x.Chave == ChaveVersao, cancellationToken);
        var versaoAtual = int.TryParse(config?.Valor, NumberStyles.Integer, CultureInfo.InvariantCulture, out var v) ? v : 0;
        if (versaoAtual >= RepairVersion)
            return;

        var alteracoes = await RepararAtivosAsync(cancellationToken);
        await RepararChavesProventosAsync(cancellationToken);
        await RepararDocumentKindAsync(cancellationToken);
        await RegistrarVersaoAsync(config, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        if (alteracoes.AtivosNormalizados > 0 || alteracoes.AtivosMesclados > 0 || alteracoes.DependenciasReapontadas > 0)
            FinancasRepairLogMessages.ReparoAplicado(_logger, alteracoes.AtivosNormalizados, alteracoes.AtivosMesclados, alteracoes.DependenciasReapontadas);
    }

    private async Task<RepairStats> RepararAtivosAsync(CancellationToken cancellationToken)
    {
        _dependenciasReapontadas = 0;
        var stats = new RepairStats();
        var ativos = await _context.AtivosFinanceiros
            .IgnoreQueryFilters()
            .ToListAsync(cancellationToken);

        var candidatos = ativos
            .Select(a => new { Asset = a, Canonico = ResolverCanonico(a) })
            .Where(x => x.Canonico is not null)
            .GroupBy(x => x.Canonico!.Ticker, StringComparer.OrdinalIgnoreCase);

        foreach (var grupo in candidatos)
        {
            var itens = grupo.ToList();
            var alvo = EscolherAtivoCanonico(itens.Select(x => x.Asset), grupo.Key);
            var alias = itens.First(x => ReferenceEquals(x.Asset, alvo)).Canonico ?? itens.First().Canonico!;

            AplicarIdentidadeCanonica(alvo, alias, stats);

            foreach (var item in itens.Where(x => !ReferenceEquals(x.Asset, alvo)))
            {
                await ReapontarDependenciasAsync(item.Asset.Id, alvo, cancellationToken);
                stats.AtivosMesclados++;
                if (item.Asset.DataExclusao is null)
                    _context.AtivosFinanceiros.Remove(item.Asset);
            }
        }

        await RegistrarAtivosSemTickerAsync(ativos, cancellationToken);
        stats.DependenciasReapontadas = _dependenciasReapontadas;
        return stats;
    }

    private async Task RepararChavesProventosAsync(CancellationToken cancellationToken)
    {
        var rendimentos = await _context.RendimentosInvestimento
            .IgnoreQueryFilters()
            .Where(x => x.AssetId != null && x.PaymentDate != null)
            .ToListAsync(cancellationToken);
        if (rendimentos.Count == 0)
            return;

        var cargaId = await _context.CargasFinanceiras
            .OrderByDescending(x => x.ImportedAt)
            .Select(x => (int?)x.Id)
            .FirstOrDefaultAsync(cancellationToken);

        var grupos = rendimentos
            .GroupBy(r => ProventoDedup.ChaveEconomica(r.AssetId!.Value, r.ReferenceDate, r.PaymentDate, r.IncomeType), StringComparer.OrdinalIgnoreCase);

        foreach (var grupo in grupos)
        {
            var itens = grupo.ToList();
            var vencedor = itens
                .OrderByDescending(PrioridadeFonteProvento)
                .ThenBy(x => x.DataExclusao is not null)
                .ThenBy(x => x.Id)
                .First();
            vencedor.ChaveNatural = grupo.Key;

            foreach (var item in itens.Where(x => !ReferenceEquals(x, vencedor)))
            {
                if (!ProventoDedup.MesmoValor(vencedor.Amount, item.Amount) && cargaId is not null)
                {
                    _context.AlertasConfiabilidade.Add(new AlertaConfiabilidade
                    {
                        CargaFinanceiraId = cargaId.Value,
                        EntityType = nameof(RendimentoInvestimento),
                        EntityId = vencedor.Id,
                        Severity = SeveridadeAlerta.Atencao,
                        Code = "PROVENTO_DUPLICADO_VALOR_DIVERGENTE",
                        Message = "Provento duplicado por chave economica com valor divergente; mantida uma linha e arquivada a duplicata.",
                        Details = System.Text.Json.JsonSerializer.Serialize(new { chave = grupo.Key, mantido = vencedor.Amount, arquivado = item.Amount, item.Fonte }),
                        UsuarioInclusao = UsuarioSistema
                    });
                }

                vencedor.Fonte = MesclarFonte(vencedor.Fonte, item.Fonte);
                _context.RendimentosInvestimento.Remove(item);
            }
        }
    }

    // Reclassifica DocumentKind dos documentos gravados como Desconhecido por versões antigas do
    // importador (que ainda não tinham ClassificarDocumento). Causa-raiz corrigida: o export oficial .xlsx
    // "Histórico de Transações" da Binance ficava como Desconhecido; o netting filtra por
    // BinanceTransactions e o IGNORAVA → só o CSV parcial entrava e a cripto (BTC no Earn) vinha
    // subcontada. Idempotente (só toca em Desconhecido) e à PROVA DE FALHA (não pode derrubar o load).
    private async Task RepararDocumentKindAsync(CancellationToken cancellationToken)
    {
        try
        {
            var docs = await _context.DocumentosFinanceiros
                .IgnoreQueryFilters()
                .Where(d => d.DocumentKind == TipoDocumentoFinanceiro.Desconhecido)
                .ToListAsync(cancellationToken);

            var reclassificados = 0;
            foreach (var doc in docs)
            {
                if (string.IsNullOrWhiteSpace(doc.FileName))
                    continue;

                var kind = FinancasImportador.ClassificarDocumento(doc.FileName);
                if (kind == TipoDocumentoFinanceiro.Desconhecido)
                    continue;

                doc.DocumentKind = kind;
                doc.UsuarioAlteracao = UsuarioSistema;
                reclassificados++;
            }

            if (reclassificados > 0)
                FinancasRepairLogMessages.DocumentKindReclassificado(_logger, reclassificados);
        }
        catch (Exception ex)
        {
            FinancasRepairLogMessages.ReparoDocumentKindFalhou(_logger, ex);
        }
    }

    private static int PrioridadeFonteProvento(RendimentoInvestimento rendimento)
    {
        var fonte = $"{rendimento.Fonte} {rendimento.Source}".ToUpperInvariant();
        if (fonte.Contains("INFORME")) return 100;
        if (rendimento.SourceDocumentId.HasValue) return 80;
        if (fonte.Contains("BRAPI")) return 50;
        if (fonte.Contains("BINANCE")) return 40;
        return 10;
    }

    private static string MesclarFonte(string fonteAtual, string novaFonte)
    {
        if (string.IsNullOrWhiteSpace(novaFonte))
            return fonteAtual;
        if (string.IsNullOrWhiteSpace(fonteAtual))
            return novaFonte;
        return fonteAtual.Contains(novaFonte, StringComparison.OrdinalIgnoreCase)
            ? fonteAtual
            : $"{fonteAtual}+{novaFonte}";
    }

    private int _dependenciasReapontadas;

    private static AtivoCanonico? ResolverCanonico(AtivoFinanceiro ativo)
    {
        if (ativo.EhCripto || ativo.Classe == ClasseAtivo.Cripto || ativo.Mercado.Equals("Binance", StringComparison.OrdinalIgnoreCase))
            return ResolverCripto(ativo);

        var alias = NormalizadorAtivoB3.Normalizar(ativo.Sigla)
            ?? NormalizadorAtivoB3.Normalizar(ativo.Chave)
            ?? NormalizadorAtivoB3.Normalizar(ativo.Nome);

        return alias is null ? null : new AtivoCanonico(alias.Ticker, alias.Classe, "B3", "BRL");
    }

    private static AtivoCanonico? ResolverCripto(AtivoFinanceiro ativo)
    {
        var symbol = (ativo.Sigla ?? ativo.Chave ?? ativo.Nome ?? string.Empty).Trim().ToUpperInvariant();
        if (symbol.Length == 0)
            return null;

        symbol = symbol switch
        {
            "POLYGON" => "POL",
            "POLYGON ECOSYSTEM TOKEN" => "POL",
            _ => symbol
        };

        return new AtivoCanonico(symbol, ClasseAtivo.Cripto, "Binance", "USD/BRL");
    }

    private static AtivoFinanceiro EscolherAtivoCanonico(IEnumerable<AtivoFinanceiro> ativos, string ticker)
    {
        var lista = ativos.ToList();
        return lista
            .OrderByDescending(a => string.Equals(a.Chave, ticker, StringComparison.OrdinalIgnoreCase))
            .ThenByDescending(a => string.Equals(a.Sigla, ticker, StringComparison.OrdinalIgnoreCase))
            .ThenByDescending(a => a.DataExclusao is null)
            .ThenBy(a => a.Id)
            .First();
    }

    private void AplicarIdentidadeCanonica(AtivoFinanceiro ativo, AtivoCanonico canonico, RepairStats stats)
    {
        var mudou = false;
        if (!string.Equals(ativo.Sigla, canonico.Ticker, StringComparison.OrdinalIgnoreCase))
        {
            ativo.Sigla = canonico.Ticker;
            mudou = true;
        }

        if (!string.Equals(ativo.Chave, canonico.Ticker, StringComparison.OrdinalIgnoreCase)
            && !_context.AtivosFinanceiros.IgnoreQueryFilters().Any(x => x.Id != ativo.Id && x.Chave == canonico.Ticker))
        {
            ativo.Chave = canonico.Ticker;
            mudou = true;
        }

        if (ativo.Classe != canonico.Classe)
        {
            ativo.Classe = canonico.Classe;
            mudou = true;
        }

        if (!string.Equals(ativo.Mercado, canonico.Market, StringComparison.OrdinalIgnoreCase))
        {
            ativo.Mercado = canonico.Market;
            mudou = true;
        }

        if (!string.Equals(ativo.Moeda, canonico.Currency, StringComparison.OrdinalIgnoreCase))
        {
            ativo.Moeda = canonico.Currency;
            mudou = true;
        }

        if (canonico.Classe == ClasseAtivo.Cripto && !ativo.EhCripto)
        {
            ativo.EhCripto = true;
            mudou = true;
        }

        if (ativo.DataExclusao is not null)
        {
            ativo.DataExclusao = null;
            ativo.UsuarioExclusao = null;
            ativo.Ativo = true;
            mudou = true;
        }

        if (mudou)
            stats.AtivosNormalizados++;
    }

    private async Task ReapontarDependenciasAsync(int origemId, AtivoFinanceiro alvo, CancellationToken cancellationToken)
    {
        await ReapontarCarteirasAsync(origemId, alvo.Id, cancellationToken);
        await ReapontarCotacoesAsync(origemId, alvo, cancellationToken);
        await ReapontarHistoricoAsync(origemId, alvo, cancellationToken);

        foreach (var op in await _context.OperacoesB3.IgnoreQueryFilters().Where(x => x.AssetId == origemId).ToListAsync(cancellationToken))
            Reapontar(() => op.AssetId = alvo.Id);

        foreach (var tx in await _context.TransacoesFinanceiras.IgnoreQueryFilters().Where(x => x.AssetId == origemId).ToListAsync(cancellationToken))
            Reapontar(() => tx.AssetId = alvo.Id);

        foreach (var pos in await _context.EstimativasPosicaoCarteira.IgnoreQueryFilters().Where(x => x.AtivoFinanceiroId == origemId).ToListAsync(cancellationToken))
            Reapontar(() => pos.AtivoFinanceiroId = alvo.Id);

        foreach (var rendimento in await _context.RendimentosInvestimento.IgnoreQueryFilters().Where(x => x.AssetId == origemId).ToListAsync(cancellationToken))
            Reapontar(() => rendimento.AssetId = alvo.Id);
    }

    private async Task ReapontarCarteirasAsync(int origemId, int alvoId, CancellationToken cancellationToken)
    {
        var links = await _context.CarteirasAtivosFinanceiros
            .IgnoreQueryFilters()
            .Where(x => x.AtivoFinanceiroId == origemId || x.AtivoFinanceiroId == alvoId)
            .ToListAsync(cancellationToken);
        var destinoPorCarteira = links
            .Where(x => x.AtivoFinanceiroId == alvoId)
            .GroupBy(x => x.CarteiraFinanceiraId)
            .ToDictionary(x => x.Key, x => x.OrderBy(l => l.DataExclusao is not null).First());

        foreach (var link in links.Where(x => x.AtivoFinanceiroId == origemId))
        {
            if (destinoPorCarteira.TryGetValue(link.CarteiraFinanceiraId, out var destino))
            {
                destino.PesoAlvo ??= link.PesoAlvo;
                destino.Observacao ??= link.Observacao;
                _context.CarteirasAtivosFinanceiros.Remove(link);
            }
            else
            {
                Reapontar(() => link.AtivoFinanceiroId = alvoId);
            }
        }
    }

    private async Task ReapontarCotacoesAsync(int origemId, AtivoFinanceiro alvo, CancellationToken cancellationToken)
    {
        var cotacoes = await _context.CotacoesAtivosFinanceiros
            .IgnoreQueryFilters()
            .Where(x => x.AtivoFinanceiroId == origemId || x.AtivoFinanceiroId == alvo.Id)
            .ToListAsync(cancellationToken);
        var destinoPorProvedor = cotacoes
            .Where(x => x.AtivoFinanceiroId == alvo.Id)
            .GroupBy(x => x.Provedor)
            .ToDictionary(x => x.Key, x => x.OrderByDescending(c => c.ConsultadoEm).First());

        foreach (var cotacao in cotacoes.Where(x => x.AtivoFinanceiroId == origemId))
        {
            if (destinoPorProvedor.TryGetValue(cotacao.Provedor, out var destino))
            {
                if (cotacao.ConsultadoEm > destino.ConsultadoEm)
                    CopiarCotacao(cotacao, destino, alvo);
                _context.CotacoesAtivosFinanceiros.Remove(cotacao);
            }
            else
            {
                Reapontar(() =>
                {
                    cotacao.AtivoFinanceiroId = alvo.Id;
                    cotacao.Simbolo = alvo.Sigla ?? alvo.Chave;
                });
            }
        }
    }

    private async Task ReapontarHistoricoAsync(int origemId, AtivoFinanceiro alvo, CancellationToken cancellationToken)
    {
        var historicos = await _context.PrecosHistoricosAtivosFinanceiros
            .IgnoreQueryFilters()
            .Where(x => x.AtivoFinanceiroId == origemId || x.AtivoFinanceiroId == alvo.Id)
            .ToListAsync(cancellationToken);
        var destinoPorChave = historicos
            .Where(x => x.AtivoFinanceiroId == alvo.Id)
            .GroupBy(x => (x.Provedor, x.Interval, Data: x.Date.Date))
            .ToDictionary(x => x.Key, x => x.OrderByDescending(h => h.DataAlteracao ?? h.DataInclusao).First());

        foreach (var historico in historicos.Where(x => x.AtivoFinanceiroId == origemId))
        {
            var chave = (historico.Provedor, historico.Interval, Data: historico.Date.Date);
            if (destinoPorChave.ContainsKey(chave))
            {
                _context.PrecosHistoricosAtivosFinanceiros.Remove(historico);
            }
            else
            {
                Reapontar(() =>
                {
                    historico.AtivoFinanceiroId = alvo.Id;
                    historico.Symbol = alvo.Sigla ?? alvo.Chave;
                });
            }
        }
    }

    private void Reapontar(Action action)
    {
        action();
        _dependenciasReapontadas++;
    }

    private static void CopiarCotacao(CotacaoAtivoFinanceiro origem, CotacaoAtivoFinanceiro destino, AtivoFinanceiro alvo)
    {
        destino.Simbolo = alvo.Sigla ?? alvo.Chave;
        destino.Moeda = origem.Moeda;
        destino.Preco = origem.Preco;
        destino.PrecoBRL = origem.PrecoBRL;
        destino.Variacao = origem.Variacao;
        destino.VariacaoPercentual = origem.VariacaoPercentual;
        destino.HorarioMercado = origem.HorarioMercado;
        destino.ConsultadoEm = origem.ConsultadoEm;
        destino.ExpiraEm = origem.ExpiraEm;
        destino.Status = origem.Status;
        destino.MensagemErro = origem.MensagemErro;
        destino.RawJson = origem.RawJson;
    }

    private async Task RegistrarAtivosSemTickerAsync(IReadOnlyList<AtivoFinanceiro> ativos, CancellationToken cancellationToken)
    {
        var cargaId = await _context.CargasFinanceiras
            .OrderByDescending(x => x.ImportedAt)
            .Select(x => (int?)x.Id)
            .FirstOrDefaultAsync(cancellationToken);
        if (cargaId is null)
            return;

        foreach (var ativo in ativos.Where(a =>
                     a.DataExclusao is null
                     && !a.EhCripto
                     && (a.Classe is ClasseAtivo.FII or ClasseAtivo.BDR)
                     && string.IsNullOrWhiteSpace(a.Sigla)
                     && ResolverCanonico(a) is null))
        {
            _context.AlertasConfiabilidade.Add(new AlertaConfiabilidade
            {
                CargaFinanceiraId = cargaId.Value,
                EntityType = nameof(AtivoFinanceiro),
                EntityId = ativo.Id,
                Severity = SeveridadeAlerta.Atencao,
                Code = "ATIVO_SEM_TICKER_CANONICO",
                Message = $"Ativo B3 sem ticker canonico: {ativo.Chave} / {ativo.Nome}.",
                UsuarioInclusao = UsuarioSistema
            });
        }
    }

    private async Task RegistrarVersaoAsync(Configuracao? config, CancellationToken cancellationToken)
    {
        if (config is null)
        {
            await _context.Configuracoes.AddAsync(new Configuracao
            {
                Agrupamento = Agrupamento,
                Chave = ChaveVersao,
                Valor = RepairVersion.ToString(CultureInfo.InvariantCulture),
                Descricao = "Versao interna do reparo idempotente de ativos financeiros.",
                Ativo = true,
                UsuarioInclusao = UsuarioSistema
            }, cancellationToken);
            return;
        }

        config.Valor = RepairVersion.ToString(CultureInfo.InvariantCulture);
    }

    private sealed record AtivoCanonico(string Ticker, ClasseAtivo Classe, string Market, string Currency);

    private sealed class RepairStats
    {
        public int AtivosNormalizados { get; set; }
        public int AtivosMesclados { get; set; }
        public int DependenciasReapontadas { get; set; }
    }
}

internal static partial class FinancasRepairLogMessages
{
    [LoggerMessage(EventId = 53, Level = LogLevel.Information, Message = "Reparo financeiro aplicado: {AtivosNormalizados} ativos normalizados, {AtivosMesclados} ativos mesclados, {DependenciasReapontadas} dependencias reapontadas.")]
    public static partial void ReparoAplicado(ILogger logger, int ativosNormalizados, int ativosMesclados, int dependenciasReapontadas);

    [LoggerMessage(EventId = 54, Level = LogLevel.Information, Message = "Reparo de DocumentKind: {Reclassificados} documentos reclassificados de Desconhecido.")]
    public static partial void DocumentKindReclassificado(ILogger logger, int reclassificados);

    [LoggerMessage(EventId = 55, Level = LogLevel.Warning, Message = "Falha no reparo de DocumentKind (ignorada para nao derrubar o load).")]
    public static partial void ReparoDocumentKindFalhou(ILogger logger, Exception exception);
}
