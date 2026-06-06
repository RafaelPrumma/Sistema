using Microsoft.EntityFrameworkCore;
using Sistema.CORE.Common;
using Sistema.CORE.Entities;
using Sistema.CORE.Repositories.Interfaces;
using Sistema.INFRA.Data;

namespace Sistema.INFRA.Repositories;

public class MinhasFinancasRepository(AppDbContext context) : IMinhasFinancasRepository
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
            query = query.Where(x => x.OriginalAssetName.Contains(termo) || x.SourceFile.Contains(termo) || (x.Asset != null && x.Asset.Name.Contains(termo)));

        if (ano.HasValue)
            query = query.Where(x => x.TradeDate.HasValue && x.TradeDate.Value.Year == ano.Value);

        if (!string.IsNullOrWhiteSpace(classe) && Enum.TryParse<ClasseAtivo>(classe, true, out var classeAtivo))
            query = query.Where(x => x.Asset != null && x.Asset.AssetClass == classeAtivo);

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
        var query = _context.EstimativasPosicaoCarteira.AsNoTracking().Include(x => x.Asset).Where(x => x.CargaFinanceiraId == cargaId);
        if (somenteAbertas.HasValue)
        {
            query = query.Where(x => somenteAbertas.Value
                ? x.Status == StatusEstimativaPosicao.AbertaOuResidual
                : x.Status == StatusEstimativaPosicao.EncerradaPorOperacoes);
        }

        return await query.OrderByDescending(x => x.EstimatedCurrentPosition).ThenBy(x => x.Asset!.Name).ToListAsync(cancellationToken);
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

    private async Task<int> ObterCargaIdAsync(CancellationToken cancellationToken)
    {
        var carga = await _context.CargasFinanceiras.AsNoTracking().OrderByDescending(x => x.ImportedAt).Select(x => (int?)x.Id).FirstOrDefaultAsync(cancellationToken);
        return carga ?? 0;
    }

    private static int NormalizarPage(int page) => page < 1 ? 1 : page;
    private static int NormalizarPageSize(int pageSize) => pageSize is < 1 or > 200 ? 25 : pageSize;
}
