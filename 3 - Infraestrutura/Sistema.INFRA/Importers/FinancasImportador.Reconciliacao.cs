using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Sistema.CORE.Entities;

namespace Sistema.INFRA.Importers;

// F3 — reconciliação da posição pela aba Posição da B3 (custódia oficial) + ativo virtual VARIAÇÃO.
// specs/importador-b3.spec.md §10 passo 4. A lógica PURA (alvo/diff) vive em ReconciliadorPosicaoB3;
// aqui fica só o que é stateful: ler a Posição mais recente do ConteudoBruto, calcular a posição
// canônica corrente (sem os próprios ajustes), apagar os ajustes antigos e gravar os novos.
//
// À PROVA DE FALHA: chamada por GarantirCargaInicialAsync DENTRO de try-catch — se algo aqui lançar,
// o chamador loga e segue (o dashboard não pode quebrar). Mesmo assim, mantemos a operação atômica
// (um único SaveChanges no fim) para não deixar estado meio-aplicado.
public partial class FinancasImportador
{
    // Abas de Posição (custódia oficial) usadas como alvo da reconciliação. Inclui ETF e BDR: senão um
    // ativo desses DETIDO (ex.: GOLD11 na aba ETF, BDRs na aba BDR) não acha alvo → cai em 0 e é zerado
    // por engano, jogando o valor pro VARIAÇÃO. Renda Fixa/Tesouro ficam de fora (classes sem ticker/PM).
    private static readonly string[] AbasPosicaoB3 =
        ["Posição - Ações", "Posição - Fundos", "Posição - ETF", "Posição - BDR"];

    // Reconcilia a posição calculada com a Posição mais recente da B3, criando ajustes idempotentes
    // (Fonte="Reconciliação") + contrapartida no ativo virtual VARIAÇÃO. Idempotente: apaga os ajustes
    // anteriores e recalcula; nunca toca em importação/manuais reais.
    private async Task ReconciliarPosicaoB3Async(CancellationToken cancellationToken)
    {
        // 1) Idempotência: remove os ajustes da execução anterior ANTES de recalcular o "calculado".
        //    (Hard delete, como o resync faz para as transações de importação.)
        await _context.TransacoesFinanceiras
            .IgnoreQueryFilters()
            .Where(x => x.Fonte == ReconciliadorPosicaoB3.Fonte)
            .ExecuteDeleteAsync(cancellationToken);

        // 2) Alvo: quantidades da Posição MAIS RECENTE (abas Posição - Ações/Fundos).
        var alvoPorTicker = await CarregarAlvosPosicaoB3Async(cancellationToken);
        if (alvoPorTicker is null)
            return; // sem nenhuma Posição importada não há o que reconciliar.

        // 3) Calculado + PM corrente por ativo, das transações canônicas (já sem Reconciliação, pois
        //    apagamos acima). Cripto fora (não há Posição importada).
        var transacoes = await _context.TransacoesFinanceiras
            .AsNoTracking()
            .Where(x => x.IsCanonical)
            .Include(x => x.Asset)
            .Where(x => x.Asset != null && !x.Asset!.IsCrypto)
            .OrderBy(x => x.Date)
            .ThenBy(x => x.Id)
            .ToListAsync(cancellationToken);

        // Aplica os eventos corporativos (split/grupamento) ANTES de acumular — a posição canônica que
        // o dashboard mostra usa as cotas pós-evento (BuscarTodasTransacoesAsync), e a Posição da B3 já
        // vem pós-evento. Sem isso o diff de um ativo splitado seria espúrio.
        AplicarEventosCorporativos(transacoes, await _context.EventosCorporativos.AsNoTracking().ToListAsync(cancellationToken));

        var (calculadoPorAtivo, precoMedioPorAtivo) = CalcularPosicaoCanonica(transacoes);

        // 4) Universo de ativos a reconciliar = ativos B3 (não-cripto) que aparecem no cálculo OU na
        //    Posição (para zerar fantasmas e materializar quem só está na custódia).
        var ativosB3 = await _context.AtivosFinanceiros
            .Where(a => !a.IsCrypto)
            .ToListAsync(cancellationToken);

        var assetKeyPorId = new Dictionary<int, string>();
        var reconciliaveis = new List<AtivoReconciliavel>();
        foreach (var a in ativosB3)
        {
            var tickerNorm = ExtratoB3Materializador.NormalizarTicker(a.Ticker ?? a.AssetKey);
            if (string.IsNullOrWhiteSpace(tickerNorm))
                continue;

            // Mais de um ativo pode mapear no mesmo ticker (ITUB4 + ITUB4F); o resync já unifica em
            // ITUB4, então só reconciliamos o ativo cujo AssetKey É o ticker normalizado (o ativo-base).
            // Os demais (fracionário órfão) não têm posição própria e ficam de fora.
            if (!string.Equals(a.AssetKey, tickerNorm, StringComparison.OrdinalIgnoreCase))
                continue;

            assetKeyPorId[a.Id] = a.AssetKey;
            reconciliaveis.Add(new AtivoReconciliavel(a.Id, tickerNorm));
        }

        // 5) Lógica pura: diff = alvo − calculado por ativo.
        var ajustes = ReconciliadorPosicaoB3.CalcularAjustes(
            reconciliaveis, calculadoPorAtivo, precoMedioPorAtivo, alvoPorTicker);
        if (ajustes.Count == 0)
            return;

        // 6) Ativo virtual VARIAÇÃO (contrapartida do valor da diferença). Persistir ANTES de
        //    referenciar o Id escalar — senão a FK FinanceiroTransacao→FinanceiroAtivo vê AssetId=0.
        var variacao = await ObterOuCriarAtivoVariacaoAsync(cancellationToken);

        var data = DateTime.UtcNow.Date;
        var novas = new List<TransacaoFinanceira>(ajustes.Count * 2);
        foreach (var ajuste in ajustes)
        {
            // Ajuste no próprio ativo: leva a posição ao alvo, ao PM corrente (realizado ≈ 0).
            novas.Add(new TransacaoFinanceira
            {
                Origem = OrigemTransacao.Manual, // sobrevive ao resync (que só apaga Importação)
                AssetId = ajuste.AssetId,
                Date = data,
                OperationType = ajuste.OperationType,
                Quantity = ajuste.Quantidade,
                UnitPrice = ajuste.PrecoMedio,
                GrossAmount = decimal.Round(ajuste.Quantidade * ajuste.PrecoMedio, 8),
                Fees = 0m,
                Currency = "BRL",
                Broker = "B3 Custódia",
                Fonte = ReconciliadorPosicaoB3.Fonte,
                Observacao = ajuste.Observacao,
                IsCanonical = true,
                ConfidenceLevel = NivelConfianca.Media,
                RawJson = JsonSerializer.Serialize(new { ajuste.Alvo, ajuste.Calculado, ajuste.PrecoMedio }),
                UsuarioInclusao = "financas-reconciliacao"
            });

            // Contrapartida no ativo VARIAÇÃO: valor |diff|×PM (UnitPrice=1 → aparece como R$ na
            // carteira). Sentido inverso ao ajuste para não "sumir" patrimônio: se o ativo recebeu
            // cotas (Compra), a VARIAÇÃO devolve o valor (Venda) e vice-versa.
            var tipoContrapartida = ajuste.OperationType == TipoOperacaoFinanceira.Compra
                ? TipoOperacaoFinanceira.Venda
                : TipoOperacaoFinanceira.Compra;

            novas.Add(new TransacaoFinanceira
            {
                Origem = OrigemTransacao.Manual,
                AssetId = variacao.Id,
                Date = data,
                OperationType = tipoContrapartida,
                Quantity = ajuste.ValorContrapartida,
                UnitPrice = 1m,
                GrossAmount = ajuste.ValorContrapartida,
                Fees = 0m,
                Currency = "BRL",
                Broker = "B3 Custódia",
                Fonte = ReconciliadorPosicaoB3.Fonte,
                Observacao = $"Contrapartida do ajuste de {assetKeyPorId.GetValueOrDefault(ajuste.AssetId, "ativo")}.",
                IsCanonical = true,
                ConfidenceLevel = NivelConfianca.Media,
                RawJson = "{}",
                UsuarioInclusao = "financas-reconciliacao"
            });
        }

        await _context.TransacoesFinanceiras.AddRangeAsync(novas, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    // Carrega os alvos da Posição MAIS RECENTE: acha o documento B3 com maior referencePeriod que tenha
    // alguma aba de Posição, lê suas linhas de ConteudoBruto e extrai (ticker → quantidade) via lógica
    // pura. Retorna null quando não há NENHUMA Posição importada.
    private async Task<Dictionary<string, decimal>?> CarregarAlvosPosicaoB3Async(CancellationToken cancellationToken)
    {
        // Documentos B3 com período derivado (RawMetadataJson.referencePeriod = "yyyy-MM").
        var docs = await _context.DocumentosFinanceiros
            .Where(d => d.DocumentKind == TipoDocumentoFinanceiro.ExtratoConsolidadoB3)
            .Select(d => new { d.Id, d.RawMetadataJson })
            .ToListAsync(cancellationToken);

        var docsComPeriodo = docs
            .Select(d => new { d.Id, Periodo = ExtrairReferencePeriod(d.RawMetadataJson) })
            .Where(d => d.Periodo is not null)
            .OrderByDescending(d => d.Periodo, StringComparer.Ordinal)
            .ToList();
        if (docsComPeriodo.Count == 0)
            return null;

        // Percorre do mais recente para trás até achar um documento que tenha linhas de Posição.
        // (Robusto a um mês recente sem aba de Posição.)
        foreach (var doc in docsComPeriodo)
        {
            var conteudos = await _context.ConteudosBrutosFinanceiros
                .AsNoTracking()
                .Where(c => c.DocumentoFinanceiroId == doc.Id
                            && c.SheetName != null
                            && AbasPosicaoB3.Contains(c.SheetName))
                .Select(c => c.RawJson)
                .ToListAsync(cancellationToken);

            var linhas = conteudos
                .Select(LinhaDePosicao)
                .Where(r => r is not null)
                .Select(r => r!)
                .ToList();
            if (linhas.Count == 0)
                continue;

            return ReconciliadorPosicaoB3.ExtrairAlvos(linhas);
        }

        return null;
    }

    // Extrai o "row" (header→valor) de uma linha de Posição, pulando o cabeçalho. O payload gravado em
    // ProcessarExtratoB3Async é { sheet, isHeader, cells, row }. Retorna null para o header/linha inválida.
    private static IReadOnlyDictionary<string, string>? LinhaDePosicao(string? rawJson)
    {
        if (string.IsNullOrWhiteSpace(rawJson))
            return null;

        try
        {
            using var doc = JsonDocument.Parse(rawJson);
            var root = doc.RootElement;
            if (root.TryGetProperty("isHeader", out var header) && header.ValueKind == JsonValueKind.True)
                return null;

            if (!root.TryGetProperty("row", out var row) || row.ValueKind != JsonValueKind.Object)
                return null;

            var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var prop in row.EnumerateObject())
                dict[prop.Name] = prop.Value.ValueKind == JsonValueKind.String ? prop.Value.GetString() ?? string.Empty : prop.Value.ToString();

            return dict.Count == 0 ? null : dict;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static string? ExtrairReferencePeriod(string? rawMetadataJson)
    {
        if (string.IsNullOrWhiteSpace(rawMetadataJson))
            return null;

        try
        {
            using var doc = JsonDocument.Parse(rawMetadataJson);
            return doc.RootElement.TryGetProperty("referencePeriod", out var p) && p.ValueKind == JsonValueKind.String
                ? p.GetString()
                : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    // Aplica o produto dos fatores dos eventos corporativos posteriores à data de cada transação
    // (mesma regra de BuscarTodasTransacoesAsync) — mantém o "calculado" consistente com a posição que
    // o dashboard exibe e com a Posição da B3 (ambas pós-evento). Muta as instâncias (são AsNoTracking).
    private static void AplicarEventosCorporativos(IReadOnlyList<TransacaoFinanceira> transacoes, IReadOnlyList<EventoCorporativo> eventos)
    {
        if (eventos.Count == 0)
            return;

        var eventosPorAtivo = eventos
            .GroupBy(e => e.AtivoFinanceiroId)
            .ToDictionary(g => g.Key, g => g.OrderBy(e => e.Data).ToList());

        foreach (var t in transacoes)
        {
            if (!eventosPorAtivo.TryGetValue(t.AssetId, out var evs))
                continue;

            var fatorAcumulado = evs
                .Where(e => t.Date < e.Data)
                .Aggregate(1m, (acc, e) => acc * e.Fator);
            if (fatorAcumulado == 1m)
                continue;

            t.Quantity *= fatorAcumulado;
            t.UnitPrice /= fatorAcumulado;
        }
    }

    // Posição canônica (qtd) + PM corrente por ativo, custo médio ponderado móvel — mesma regra do
    // CalcularPosicoes do app. As transações já vêm ordenadas (Date, Id) e SEM os ajustes de
    // Reconciliação (apagados antes).
    private static (Dictionary<int, decimal> Quantidade, Dictionary<int, decimal> PrecoMedio) CalcularPosicaoCanonica(IReadOnlyList<TransacaoFinanceira> transacoes)
    {
        var quantidades = new Dictionary<int, decimal>();
        var custos = new Dictionary<int, decimal>();

        foreach (var t in transacoes)
        {
            quantidades.TryGetValue(t.AssetId, out var qtd);
            custos.TryGetValue(t.AssetId, out var custo);

            var delta = DeltaQuantidadeReconciliacao(t);
            if (delta > 0m)
            {
                custo += t.Quantity * t.UnitPrice + t.Fees;
                qtd += t.Quantity;
            }
            else if (delta < 0m)
            {
                var pm = qtd > 0m ? custo / qtd : 0m;
                var reduz = Math.Min(t.Quantity, qtd);
                custo -= reduz * pm;
                qtd -= t.Quantity;
                if (qtd <= 0.000000000001m)
                {
                    qtd = 0m;
                    custo = 0m;
                }
            }

            quantidades[t.AssetId] = qtd;
            custos[t.AssetId] = custo;
        }

        var precoMedio = new Dictionary<int, decimal>();
        foreach (var (assetId, qtd) in quantidades)
            precoMedio[assetId] = qtd > 0m ? custos[assetId] / qtd : 0m;

        return (quantidades, precoMedio);
    }

    // Mesmo sentido do DeltaQuantidade do app (Compra/Deposito/Rendimento +, Venda/Saque/Taxa −).
    private static decimal DeltaQuantidadeReconciliacao(TransacaoFinanceira t) => t.OperationType switch
    {
        TipoOperacaoFinanceira.Compra or TipoOperacaoFinanceira.Deposito or TipoOperacaoFinanceira.Rendimento => t.Quantity,
        TipoOperacaoFinanceira.Venda or TipoOperacaoFinanceira.Saque or TipoOperacaoFinanceira.Taxa => -t.Quantity,
        _ => 0m
    };

    private async Task<AtivoFinanceiro> ObterOuCriarAtivoVariacaoAsync(CancellationToken cancellationToken)
    {
        var variacao = await _context.AtivosFinanceiros
            .FirstOrDefaultAsync(a => a.AssetKey == ReconciliadorPosicaoB3.AssetKeyVariacao, cancellationToken);
        if (variacao is not null)
            return variacao;

        variacao = new AtivoFinanceiro
        {
            AssetKey = ReconciliadorPosicaoB3.AssetKeyVariacao,
            Ticker = ReconciliadorPosicaoB3.AssetKeyVariacao,
            Name = ReconciliadorPosicaoB3.NomeVariacao,
            AssetClass = ClasseAtivo.Outro,
            Market = "B3",
            Currency = "BRL",
            IsCrypto = false,
            IsActive = true,
            UsuarioInclusao = "financas-reconciliacao"
        };
        _context.AtivosFinanceiros.Add(variacao);
        // Persiste para materializar o Id escalar antes de qualquer transação referenciá-lo (evita FK 0).
        await _context.SaveChangesAsync(cancellationToken);
        return variacao;
    }
}
