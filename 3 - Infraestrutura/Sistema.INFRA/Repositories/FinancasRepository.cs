using Microsoft.EntityFrameworkCore;
using Sistema.CORE.Common;
using Sistema.CORE.Entities;
using Sistema.CORE.Repositories.Interfaces;
using Sistema.INFRA.Data;

namespace Sistema.INFRA.Repositories;

public class FinancasRepository(AppDbContext context) : IFinancasRepository
{
    private readonly AppDbContext _context = context;

    public Task<bool> ExisteCargaComShaAsync(string sha256, CancellationToken cancellationToken = default)
        => _context.CargasFinanceiras.AnyAsync(x => x.JsonSha256 == sha256, cancellationToken);

    public Task<CargaFinanceira?> ObterCargaMaisRecenteAsync(CancellationToken cancellationToken = default)
        => _context.CargasFinanceiras.AsNoTracking().OrderByDescending(x => x.ImportedAt).FirstOrDefaultAsync(cancellationToken);

    public async Task AdicionarCargaAsync(CargaFinanceira carga, CancellationToken cancellationToken = default)
        => await _context.CargasFinanceiras.AddAsync(carga, cancellationToken);

    public async Task<PagedResult<DocumentoFinanceiro>> BuscarDocumentosAsync(int page, int pageSize, string? termo, CancellationToken cancellationToken = default)
    {
        var cargaId = await ObterCargaIdAsync(cancellationToken);
        var query = _context.DocumentosFinanceiros.AsNoTracking().Where(x => x.CargaFinanceiraId == cargaId);
        if (!string.IsNullOrWhiteSpace(termo))
        {
            query = query.Where(x => x.FileName.Contains(termo) || x.Path.Contains(termo) || x.Source.Contains(termo));
        }

        return await query.OrderBy(x => x.FileName).ToPagedResultAsync(NormalizarPage(page), NormalizarPageSize(pageSize), cancellationToken);
    }

    public Task<DocumentoFinanceiro?> ObterDocumentoAsync(int id, CancellationToken cancellationToken = default)
        => _context.DocumentosFinanceiros.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<IReadOnlyList<ConteudoBrutoFinanceiro>> BuscarConteudosDocumentoAsync(int documentoId, CancellationToken cancellationToken = default)
        => await _context.ConteudosBrutosFinanceiros
            .AsNoTracking()
            .Where(x => x.DocumentoFinanceiroId == documentoId)
            .OrderBy(x => x.PageNumber ?? int.MaxValue)
            .ThenBy(x => x.SheetName)
            .ThenBy(x => x.RowNumber ?? 0)
            .ToListAsync(cancellationToken);

    public async Task<PagedResult<OperacaoB3>> BuscarOperacoesB3Async(int page, int pageSize, string? termo, int? ano, string? classe, CancellationToken cancellationToken = default)
    {
        var cargaId = await ObterCargaIdAsync(cancellationToken);
        var query = _context.OperacoesB3
            .AsNoTracking()
            .Include(x => x.Asset)
            .Where(x => x.CargaFinanceiraId == cargaId && x.IsCanonical);

        if (!string.IsNullOrWhiteSpace(termo))
            query = query.Where(x => x.OriginalAssetName.Contains(termo) || x.SourceFile.Contains(termo) || (x.Asset != null && x.Asset.Nome.Contains(termo)));

        if (ano.HasValue)
            query = query.Where(x => x.TradeDate.HasValue && x.TradeDate.Value.Year == ano.Value);

        if (!string.IsNullOrWhiteSpace(classe) && Enum.TryParse<ClasseAtivo>(classe, true, out var classeAtivo))
            query = query.Where(x => x.Asset != null && x.Asset.Classe == classeAtivo);

        return await query.OrderByDescending(x => x.TradeDate).ThenByDescending(x => x.Id).ToPagedResultAsync(NormalizarPage(page), NormalizarPageSize(pageSize), cancellationToken);
    }

    public async Task<IReadOnlyList<OperacaoB3>> BuscarUltimasOperacoesB3Async(int quantidade, CancellationToken cancellationToken = default)
    {
        var cargaId = await ObterCargaIdAsync(cancellationToken);
        return await _context.OperacoesB3
            .AsNoTracking()
            .Include(x => x.Asset)
            .Where(x => x.CargaFinanceiraId == cargaId && x.IsCanonical)
            .OrderByDescending(x => x.TradeDate)
            .ThenByDescending(x => x.Id)
            .Take(quantidade)
            .ToListAsync(cancellationToken);
    }

    public async Task<PagedResult<TransacaoCripto>> BuscarTransacoesCriptoAsync(int page, int pageSize, string? termo, CancellationToken cancellationToken = default)
    {
        var cargaId = await ObterCargaIdAsync(cancellationToken);
        var query = _context.TransacoesCripto.AsNoTracking().Where(x => x.CargaFinanceiraId == cargaId);

        if (!string.IsNullOrWhiteSpace(termo))
            query = query.Where(x => x.AssetSymbol.Contains(termo) || (x.Pair != null && x.Pair.Contains(termo)) || x.RawType.Contains(termo));

        return await query.OrderByDescending(x => x.TransactionDate).ThenByDescending(x => x.Id).ToPagedResultAsync(NormalizarPage(page), NormalizarPageSize(pageSize), cancellationToken);
    }

    public async Task<IReadOnlyList<TransacaoCripto>> BuscarUltimasTransacoesCriptoAsync(int quantidade, CancellationToken cancellationToken = default)
    {
        var cargaId = await ObterCargaIdAsync(cancellationToken);
        return await _context.TransacoesCripto
            .AsNoTracking()
            .Where(x => x.CargaFinanceiraId == cargaId)
            .OrderByDescending(x => x.TransactionDate)
            .ThenByDescending(x => x.Id)
            .Take(quantidade)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<EstimativaPosicaoCarteira>> BuscarPosicoesAsync(bool? somenteAbertas, CancellationToken cancellationToken = default)
    {
        var cargaId = await ObterCargaIdAsync(cancellationToken);
        var query = _context.EstimativasPosicaoCarteira.AsNoTracking().Include(x => x.AtivoFinanceiro).Where(x => x.CargaFinanceiraId == cargaId);
        if (somenteAbertas.HasValue)
        {
            query = query.Where(x => somenteAbertas.Value
                ? x.Status == StatusEstimativaPosicao.AbertaOuResidual
                : x.Status == StatusEstimativaPosicao.EncerradaPorOperacoes);
        }

        return await query.OrderByDescending(x => x.PosicaoAtualEstimada).ThenBy(x => x.AtivoFinanceiro!.Nome).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<AlertaConfiabilidade>> BuscarAlertasAsync(CancellationToken cancellationToken = default)
    {
        var cargaId = await ObterCargaIdAsync(cancellationToken);
        return await _context.AlertasConfiabilidade
            .AsNoTracking()
            .Where(x => x.CargaFinanceiraId == cargaId)
            .OrderByDescending(x => x.Severity)
            .ThenByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<AgregadoFinanceiro>> BuscarAgregadosAsync(string dimensao, CancellationToken cancellationToken = default)
    {
        var cargaId = await ObterCargaIdAsync(cancellationToken);
        return await _context.AgregadosFinanceiros
            .AsNoTracking()
            .Where(x => x.CargaFinanceiraId == cargaId && x.Dimensao == dimensao)
            .OrderBy(x => x.Ano ?? 0)
            .ThenBy(x => x.Mes)
            .ThenByDescending(x => x.Saldo)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<RendimentoInvestimento>> BuscarRendimentosAsync(CancellationToken cancellationToken = default)
    {
        var cargaId = await ObterCargaIdAsync(cancellationToken);
        return await _context.RendimentosInvestimento
            .AsNoTracking()
            .Where(x => x.CargaFinanceiraId == cargaId)
            .OrderByDescending(x => x.Amount)
            .ToListAsync(cancellationToken);
    }

    public async Task<PagedResult<RendimentoInvestimento>> BuscarProventosAsync(int page, int pageSize, string? termo, CancellationToken cancellationToken = default)
    {
        var query = _context.RendimentosInvestimento.AsNoTracking().Include(x => x.Asset).AsQueryable();
        if (!string.IsNullOrWhiteSpace(termo))
            query = query.Where(x =>
                x.IncomeType.Contains(termo) ||
                x.Source.Contains(termo) ||
                (x.Asset != null && (x.Asset.Nome.Contains(termo) || (x.Asset.Sigla != null && x.Asset.Sigla.Contains(termo)))));

        return await query.OrderByDescending(x => x.PaymentDate).ThenByDescending(x => x.Id).ToPagedResultAsync(NormalizarPage(page), NormalizarPageSize(pageSize), cancellationToken);
    }

    public async Task<IReadOnlyList<RendimentoInvestimento>> BuscarProventosPorPeriodoAsync(DateTime inicio, DateTime fim, CancellationToken cancellationToken = default)
        => await _context.RendimentosInvestimento
            .AsNoTracking()
            .Include(x => x.Asset)
            .Where(x => x.PaymentDate != null && x.PaymentDate >= inicio && x.PaymentDate <= fim)
            .OrderBy(x => x.PaymentDate)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<AtivoFinanceiro>> BuscarAtivosComPosicaoAbertaAsync(CancellationToken cancellationToken = default)
    {
        var cargaId = await ObterCargaIdAsync(cancellationToken);
        return await _context.EstimativasPosicaoCarteira
            .AsNoTracking()
            .Where(x => x.CargaFinanceiraId == cargaId && x.Status == StatusEstimativaPosicao.AbertaOuResidual && x.AtivoFinanceiro != null)
            .Select(x => x.AtivoFinanceiro!)
            .Distinct()
            .OrderBy(x => x.Classe)
            .ThenBy(x => x.Sigla ?? x.Chave)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<CotacaoAtivoFinanceiro>> BuscarCotacoesAtuaisAsync(CancellationToken cancellationToken = default)
        => await _context.CotacoesAtivosFinanceiros
            .AsNoTracking()
            .Include(x => x.AtivoFinanceiro)
            .OrderByDescending(x => x.ConsultadoEm)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<PosicaoAtivo>> BuscarPosicoesAtivosAsync(CancellationToken cancellationToken = default)
        => await _context.PosicoesAtivos
            .AsNoTracking()
            .Include(x => x.AtivoFinanceiro)
            .Where(x => x.AtivoFinanceiro != null)
            .OrderBy(x => x.AtivoFinanceiro!.Sigla ?? x.AtivoFinanceiro.Chave)
            .ToListAsync(cancellationToken);

    public async Task SubstituirPosicoesAtivosAsync(IReadOnlyList<PosicaoAtivo> posicoes, CancellationToken cancellationToken = default)
    {
        await _context.PosicoesAtivos.ExecuteDeleteAsync(cancellationToken);
        if (posicoes.Count > 0)
            await _context.PosicoesAtivos.AddRangeAsync(posicoes, cancellationToken);
    }

    public async Task<IReadOnlyList<PrecoHistoricoAtivoFinanceiro>> BuscarHistoricoPrecosAsync(DateTime inicio, CancellationToken cancellationToken = default)
        => await _context.PrecosHistoricosAtivosFinanceiros
            .AsNoTracking()
            .Where(x => x.Date >= inicio)
            .OrderBy(x => x.Date)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<CarteiraFinanceira>> BuscarCarteirasComAtivosAsync(CancellationToken cancellationToken = default)
        => await _context.CarteirasFinanceiras
            .AsNoTracking()
            .Include(x => x.Ativos.Where(a => a.Ativo))
                .ThenInclude(x => x.AtivoFinanceiro)
            .Where(x => x.Ativo)
            .OrderBy(x => x.Ordem)
            .ThenBy(x => x.Nome)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<DocumentoFinanceiro>> BuscarDocumentosMonitoradosAsync(CancellationToken cancellationToken = default)
        => await _context.DocumentosFinanceiros
            .AsNoTracking()
            .Where(x => x.ImportacaoFinanceiraArquivoId != null)
            .OrderByDescending(x => x.DataInclusao)
            .ToListAsync(cancellationToken);

    // F-L(b): rastreabilidade por documento. Conta linhas/abas de ConteudoBruto e alertas em SQL (subquery
    // correlacionada por DocumentoFinanceiroId / EntityType+EntityId); o RawMetadataJson volta cru para o
    // serviço derivar fonte/período. Mesmo filtro de BuscarDocumentosMonitoradosAsync (só pasta monitorada).
    public async Task<IReadOnlyList<RastreabilidadeDocumentoProjecao>> BuscarRastreabilidadeDocumentosAsync(CancellationToken cancellationToken = default)
    {
        var tipoDocumento = nameof(DocumentoFinanceiro);
        return await _context.DocumentosFinanceiros
            .AsNoTracking()
            .Where(x => x.ImportacaoFinanceiraArquivoId != null)
            .OrderByDescending(x => x.DataInclusao)
            .Select(x => new RastreabilidadeDocumentoProjecao(
                x.Id,
                x.FileName,
                x.DocumentKind,
                x.ParseStatus,
                x.Status,
                x.ReferenceYear,
                x.RawMetadataJson,
                _context.ConteudosBrutosFinanceiros.Count(c => c.DocumentoFinanceiroId == x.Id),
                _context.ConteudosBrutosFinanceiros
                    .Where(c => c.DocumentoFinanceiroId == x.Id && c.SheetName != null)
                    .Select(c => c.SheetName)
                    .Distinct()
                    .Count(),
                _context.AlertasConfiabilidade.Count(a => a.EntityType == tipoDocumento && a.EntityId == x.Id)))
            .ToListAsync(cancellationToken);
    }

    public Task<ImportacaoFinanceiraArquivo?> ObterUltimaImportacaoArquivoAsync(CancellationToken cancellationToken = default)
        => _context.ImportacoesFinanceirasArquivo
            .AsNoTracking()
            .OrderByDescending(x => x.StartedAt)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<IReadOnlyList<TransacaoFinanceira>> BuscarTodasTransacoesAsync(CancellationToken cancellationToken = default)
    {
        var transacoes = await _context.TransacoesFinanceiras
            .AsNoTracking()
            .Include(x => x.Asset)
            .Where(x => x.IsCanonical && x.Asset != null)
            .OrderBy(x => x.Date)
            .ThenBy(x => x.Id)
            .ToListAsync(cancellationToken);

        var eventos = await _context.EventosCorporativos
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        if (eventos.Count == 0)
            return transacoes;

        // Agrupa eventos por ativo: para cada transação pré-Data, aplica o produto dos fatores.
        var eventosPorAtivo = eventos
            .GroupBy(e => e.AtivoFinanceiroId)
            .ToDictionary(g => g.Key, g => g.OrderBy(e => e.Data).ToList());

        foreach (var t in transacoes)
        {
            if (!eventosPorAtivo.TryGetValue(t.AssetId, out var evs))
                continue;

            // Produto dos fatores de todos os eventos posteriores à data da transação.
            var fatorAcumulado = evs
                .Where(e => t.Date < e.Data)
                .Aggregate(1m, (acc, e) => acc * e.Fator);

            if (fatorAcumulado == 1m)
                continue;

            t.Quantity *= fatorAcumulado;
            t.UnitPrice /= fatorAcumulado;
            // GrossAmount permanece inalterado (= Quantity_pré × UnitPrice_pré = Quantity_pós × UnitPrice_pós).
        }

        return transacoes;
    }

    public async Task<PagedResult<TransacaoFinanceira>> BuscarTransacoesAsync(int page, int pageSize, string? termo, OrigemTransacao? origem, CancellationToken cancellationToken = default)
    {
        var query = _context.TransacoesFinanceiras.AsNoTracking().Include(x => x.Asset).AsQueryable();

        if (origem.HasValue)
            query = query.Where(x => x.Origem == origem.Value);

        if (!string.IsNullOrWhiteSpace(termo))
            query = query.Where(x =>
                (x.Asset != null && (x.Asset.Nome.Contains(termo) || (x.Asset.Sigla != null && x.Asset.Sigla.Contains(termo)))) ||
                x.Broker.Contains(termo));

        return await query.OrderByDescending(x => x.Date).ThenByDescending(x => x.Id).ToPagedResultAsync(NormalizarPage(page), NormalizarPageSize(pageSize), cancellationToken);
    }

    public Task<DataTablesResponse<TransacaoFinanceira>> BuscarTransacoesDataTableAsync(DataTablesRequest request, OrigemTransacao? origem, CancellationToken cancellationToken = default)
    {
        var query = _context.TransacoesFinanceiras.AsNoTracking().Include(x => x.Asset)
            .Where(x => x.IsCanonical && x.Asset != null);
        if (origem.HasValue)
            query = query.Where(x => x.Origem == origem.Value);

        var ordenacoes = new Dictionary<string, Func<IQueryable<TransacaoFinanceira>, bool, IOrderedQueryable<TransacaoFinanceira>>>(StringComparer.OrdinalIgnoreCase)
        {
            ["data"] = (q, d) => d ? q.OrderByDescending(x => x.Date) : q.OrderBy(x => x.Date),
            ["ticker"] = (q, d) => d ? q.OrderByDescending(x => x.Asset!.Sigla) : q.OrderBy(x => x.Asset!.Sigla),
            ["tipo"] = (q, d) => d ? q.OrderByDescending(x => x.OperationType) : q.OrderBy(x => x.OperationType),
            ["quantidade"] = (q, d) => d ? q.OrderByDescending(x => x.Quantity) : q.OrderBy(x => x.Quantity),
            ["precoUnitario"] = (q, d) => d ? q.OrderByDescending(x => x.UnitPrice) : q.OrderBy(x => x.UnitPrice),
            ["valorTotal"] = (q, d) => d ? q.OrderByDescending(x => x.GrossAmount) : q.OrderBy(x => x.GrossAmount),
            ["corretora"] = (q, d) => d ? q.OrderByDescending(x => x.Broker) : q.OrderBy(x => x.Broker),
            ["fonte"] = (q, d) => d ? q.OrderByDescending(x => x.Fonte) : q.OrderBy(x => x.Fonte)
        };

        return query.ToDataTablesAsync(
            request,
            (q, termo) => q.Where(x => x.Asset!.Nome.Contains(termo) || (x.Asset.Sigla != null && x.Asset.Sigla.Contains(termo)) || x.Broker.Contains(termo)),
            ordenacoes,
            "data",
            cancellationToken);
    }

    public Task<TransacaoFinanceira?> ObterTransacaoAsync(int id, CancellationToken cancellationToken = default)
        => _context.TransacoesFinanceiras.Include(x => x.Asset).FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public Task<bool> TransacaoExisteAsync(string duplicateGroupKey, CancellationToken cancellationToken = default)
        => _context.TransacoesFinanceiras.AnyAsync(x => x.DuplicateGroupKey == duplicateGroupKey, cancellationToken);

    public async Task AdicionarTransacaoAsync(TransacaoFinanceira transacao, CancellationToken cancellationToken = default)
        => await _context.TransacoesFinanceiras.AddAsync(transacao, cancellationToken);

    public void AtualizarTransacao(TransacaoFinanceira transacao)
        => _context.TransacoesFinanceiras.Update(transacao);

    public void RemoverTransacao(TransacaoFinanceira transacao)
        => _context.TransacoesFinanceiras.Remove(transacao);

    public Task<AtivoFinanceiro?> ObterAtivoPorChaveOuTickerAsync(string chaveOuTicker, CancellationToken cancellationToken = default)
        => _context.AtivosFinanceiros.FirstOrDefaultAsync(
            x => x.Chave == chaveOuTicker || (x.Sigla != null && x.Sigla == chaveOuTicker),
            cancellationToken);

    public async Task AdicionarAtivoAsync(AtivoFinanceiro ativo, CancellationToken cancellationToken = default)
        => await _context.AtivosFinanceiros.AddAsync(ativo, cancellationToken);

    public async Task<PagedResult<EventoCorporativo>> BuscarEventosCorporativosAsync(int page, int pageSize, string? termo, CancellationToken cancellationToken = default)
    {
        var query = _context.EventosCorporativos.AsNoTracking().Include(x => x.AtivoFinanceiro).AsQueryable();
        if (!string.IsNullOrWhiteSpace(termo))
            query = query.Where(x =>
                (x.AtivoFinanceiro != null && (x.AtivoFinanceiro.Sigla != null && x.AtivoFinanceiro.Sigla.Contains(termo)))
                || x.Fonte.Contains(termo));
        return await query.OrderByDescending(x => x.Data).ThenByDescending(x => x.Id).ToPagedResultAsync(NormalizarPage(page), NormalizarPageSize(pageSize), cancellationToken);
    }

    public Task<EventoCorporativo?> ObterEventoCorporativoAsync(int id, CancellationToken cancellationToken = default)
        => _context.EventosCorporativos.Include(x => x.AtivoFinanceiro).FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task AdicionarEventoCorporativoAsync(EventoCorporativo evento, CancellationToken cancellationToken = default)
        => await _context.EventosCorporativos.AddAsync(evento, cancellationToken);

    public void AtualizarEventoCorporativo(EventoCorporativo evento)
        => _context.EventosCorporativos.Update(evento);

    public void RemoverEventoCorporativo(EventoCorporativo evento)
        => _context.EventosCorporativos.Remove(evento);

    // Tracked de propósito: o job de alertas altera DispararadoEm/UltimoPreco e salva.
    public async Task<IReadOnlyList<AlertaPreco>> BuscarAlertasPrecoParaJobAsync(CancellationToken cancellationToken = default)
        => await _context.AlertasPreco
            .Include(x => x.AtivoFinanceiro)
            .Where(x => x.Ativo && x.AtivoFinanceiro != null)
            .ToListAsync(cancellationToken);

    public async Task<PagedResult<AlertaPreco>> BuscarAlertasPrecoAsync(int page, int pageSize, string? termo, CancellationToken cancellationToken = default)
    {
        var query = _context.AlertasPreco.AsNoTracking().Include(x => x.AtivoFinanceiro).AsQueryable();
        if (!string.IsNullOrWhiteSpace(termo))
            query = query.Where(x =>
                x.AtivoFinanceiro != null
                && ((x.AtivoFinanceiro.Sigla != null && x.AtivoFinanceiro.Sigla.Contains(termo))
                    || x.AtivoFinanceiro.Chave.Contains(termo)));
        return await query.OrderByDescending(x => x.Ativo).ThenByDescending(x => x.Id).ToPagedResultAsync(NormalizarPage(page), NormalizarPageSize(pageSize), cancellationToken);
    }

    public Task<AlertaPreco?> ObterAlertaPrecoAsync(int id, CancellationToken cancellationToken = default)
        => _context.AlertasPreco.Include(x => x.AtivoFinanceiro).FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task AdicionarAlertaPrecoAsync(AlertaPreco alerta, CancellationToken cancellationToken = default)
        => await _context.AlertasPreco.AddAsync(alerta, cancellationToken);

    public void AtualizarAlertaPreco(AlertaPreco alerta)
        => _context.AlertasPreco.Update(alerta);

    public void RemoverAlertaPreco(AlertaPreco alerta)
        => _context.AlertasPreco.Remove(alerta);

    private async Task<int> ObterCargaIdAsync(CancellationToken cancellationToken)
    {
        var carga = await _context.CargasFinanceiras.AsNoTracking().OrderByDescending(x => x.ImportedAt).Select(x => (int?)x.Id).FirstOrDefaultAsync(cancellationToken);
        return carga ?? 0;
    }

    private static int NormalizarPage(int page) => page < 1 ? 1 : page;
    private static int NormalizarPageSize(int pageSize) => pageSize is < 1 or > 200 ? 25 : pageSize;
}
