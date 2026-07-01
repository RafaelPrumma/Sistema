using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Sistema.APP.DTOs;
using Sistema.APP.Services.Interfaces;
using Sistema.CORE.Entities;
using Sistema.INFRA.Data;

namespace Sistema.INFRA.Importers;

/// <summary>
/// Serviço do submódulo Gastos (G1). Materializa os lançamentos a partir do TEXTO BRUTO já
/// persistido (ConteudoBrutoFinanceiro) das faturas (ExtratoInvestimentosNubank = cartão) e dos
/// extratos da NuConta (ExtratoContaNubank = conta corrente) — caminho LAZY disparado ao abrir o
/// módulo, idempotente pela chave natural. Assim NÃO altera o load do dashboard de Investimentos.
/// TUDO à prova de falha: qualquer exceção é logada e engolida; a visão geral degrada para vazio.
/// </summary>
public class GastosService(AppDbContext context, ILogger<GastosService> logger) : IGastosService
{
    private const string ParserVersion = "gastos-g1";

    private readonly AppDbContext _context = context;
    private readonly ILogger<GastosService> _logger = logger;

    public async Task<GastosVisaoGeralDto> ObterVisaoGeralAsync(CancellationToken cancellationToken = default)
    {
        var hoje = DateTime.Today;
        try
        {
            await MaterializarPendentesAsync(cancellationToken);

            var inicioMes = new DateTime(hoje.Year, hoje.Month, 1);
            var fimMes = inicioMes.AddMonths(1);

            var total = await _context.LancamentosGasto.CountAsync(cancellationToken);
            var doMes = await _context.LancamentosGasto
                .Where(x => x.Data >= inicioMes && x.Data < fimMes)
                .GroupBy(x => x.Tipo)
                .Select(g => new { Tipo = g.Key, Valor = g.Sum(x => x.Valor), Qtd = g.Count() })
                .ToListAsync(cancellationToken);

            decimal Soma(TipoLancamentoGasto tipo) => doMes.Where(x => x.Tipo == tipo).Sum(x => x.Valor);

            return new GastosVisaoGeralDto
            {
                TotalLancamentos = total,
                LancamentosNoMes = doMes.Sum(x => x.Qtd),
                ReceitaDoMes = Soma(TipoLancamentoGasto.Receita),
                DespesaDoMes = Soma(TipoLancamentoGasto.Despesa),
                AporteDoMes = Soma(TipoLancamentoGasto.Aporte),
                Ano = hoje.Year,
                Mes = hoje.Month,
                Disponivel = true
            };
        }
        catch (Exception ex)
        {
            // À prova de falha: o Index nunca pode estourar (constraint da skill).
            GastosServiceLog.VisaoGeralFalhou(_logger, ex);
            return new GastosVisaoGeralDto { Ano = hoje.Year, Mes = hoje.Month, Disponivel = false };
        }
    }

    // Materializa os documentos de fatura/extrato cujo conteúdo bruto já foi persistido e que ainda
    // não foram materializados nesta versão do parser. Idempotente: a chave natural impede duplicar.
    private async Task MaterializarPendentesAsync(CancellationToken cancellationToken)
    {
        var documentos = await _context.DocumentosFinanceiros
            .Where(d => d.DocumentKind == TipoDocumentoFinanceiro.ExtratoContaNubank
                     || d.DocumentKind == TipoDocumentoFinanceiro.ExtratoInvestimentosNubank)
            .Select(d => new { d.Id, d.FileName, d.DocumentKind })
            .ToListAsync(cancellationToken);
        if (documentos.Count == 0)
            return;

        // Chaves naturais já gravadas (idempotência cross-documento — a mesma compra em duas
        // exportações da fatura não duplica).
        var chavesExistentes = new HashSet<string>(
            await _context.LancamentosGasto
                .Where(x => x.ChaveNatural != null)
                .Select(x => x.ChaveNatural!)
                .ToListAsync(cancellationToken),
            StringComparer.Ordinal);

        // Regras de categorização ativas, ordenadas por prioridade (projeção pura p/ o materializador).
        var regras = (await _context.RegrasCategorizacao
                .Where(r => r.Ativo)
                .OrderBy(r => r.Prioridade)
                .ThenBy(r => r.Id)
                .Select(r => new { r.Padrao, r.TipoMatch, r.CategoriaId, r.Prioridade })
                .ToListAsync(cancellationToken))
            .Select(r => new RegraTexto(r.Padrao, r.TipoMatch, r.CategoriaId, r.Prioridade))
            .ToList();

        var novos = new List<LancamentoGasto>();
        foreach (var doc in documentos)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                // Documento já materializado? Pula a leitura/parse (barato e idempotente).
                var jaMaterializado = await _context.LancamentosGasto
                    .AnyAsync(x => x.SourceDocumentId == doc.Id, cancellationToken);
                if (jaMaterializado)
                    continue;

                var texto = await ObterTextoBrutoAsync(doc.Id, cancellationToken);
                if (string.IsNullOrWhiteSpace(texto))
                    continue;

                IReadOnlyList<LancamentoGastoParseado> parseados = doc.DocumentKind switch
                {
                    TipoDocumentoFinanceiro.ExtratoInvestimentosNubank
                        => GastosMaterializador.ParsearFatura(texto, GastosMaterializador.AnoBaseDoNomeFatura(doc.FileName, DateTime.Today.Year), regras),
                    TipoDocumentoFinanceiro.ExtratoContaNubank
                        => GastosMaterializador.ParsearExtratoConta(texto, regras),
                    _ => []
                };

                foreach (var p in parseados)
                {
                    if (!chavesExistentes.Add(p.ChaveNatural))
                        continue;

                    novos.Add(new LancamentoGasto
                    {
                        Data = p.Data,
                        Descricao = Truncar(p.Descricao, 400) ?? string.Empty,
                        Valor = p.Valor,
                        Tipo = p.Tipo,
                        CategoriaId = p.CategoriaId,
                        Fonte = p.Fonte,
                        Estabelecimento = Truncar(p.Estabelecimento, 200),
                        ParcelaAtual = p.ParcelaAtual,
                        ParcelaTotal = p.ParcelaTotal,
                        SourceDocumentId = doc.Id,
                        ChaveNatural = p.ChaveNatural,
                        RawJson = "{}",
                        UsuarioInclusao = ParserVersion
                    });
                }
            }
            catch (Exception ex)
            {
                // Um documento ilegível NÃO pode derrubar a materialização dos demais.
                GastosServiceLog.MaterializacaoDocumentoFalhou(_logger, doc.Id, doc.FileName, ex);
            }
        }

        if (novos.Count > 0)
        {
            await _context.LancamentosGasto.AddRangeAsync(novos, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    // Texto bruto persistido do PDF (todas as páginas concatenadas por '\n', em ordem de página).
    private async Task<string> ObterTextoBrutoAsync(int documentoId, CancellationToken cancellationToken)
    {
        var paginas = await _context.ConteudosBrutosFinanceiros
            .Where(c => c.DocumentoFinanceiroId == documentoId
                     && c.ContentType == TipoConteudoBrutoFinanceiro.TextoPagina
                     && c.RawText != null)
            .OrderBy(c => c.PageNumber)
            .Select(c => c.RawText!)
            .ToListAsync(cancellationToken);

        return paginas.Count == 0 ? string.Empty : string.Join('\n', paginas);
    }

    private static string? Truncar(string? texto, int max)
    {
        if (string.IsNullOrEmpty(texto))
            return texto;
        return texto.Length <= max ? texto : texto[..max];
    }
}

// Logging de alto desempenho (CA1848): mensagens source-generated do serviço de Gastos.
internal static partial class GastosServiceLog
{
    [LoggerMessage(EventId = 40, Level = LogLevel.Warning, Message = "Falha ao obter a visão geral de Gastos; degradando para vazio.")]
    public static partial void VisaoGeralFalhou(ILogger logger, Exception exception);

    [LoggerMessage(EventId = 41, Level = LogLevel.Warning, Message = "Falha ao materializar gastos do documento {DocumentoId} ({FileName}).")]
    public static partial void MaterializacaoDocumentoFalhou(ILogger logger, int documentoId, string fileName, Exception exception);
}
