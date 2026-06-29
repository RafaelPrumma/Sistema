using Sistema.APP.DTOs;

namespace Sistema.APP.Services;

// F-V — lógica PURA (sem DbContext, testável isolada) da reconciliação ANUAL de proventos.
// Compara, por ano, o agregado OFICIAL do relatório anual da B3 (ProventoAnualB3 — total do ano por
// ticker×tipo, sem datas) contra o que foi MATERIALIZADO (soma dos RendimentoInvestimento daquele ano).
//
// Base de comparação = VALOR LÍQUIDO:
//  - oficial: a planilha anual já traz "Valor líquido";
//  - materializado: Amount - TaxWithheld (mesmo "líquido" usado no resto do dashboard, ValorLiquido).
// Pequenas diferenças gross×net da Brapi são esperadas — o objetivo é achar BURACOS grandes
// (mês inteiro faltando, dupla contagem), não bater centavo a centavo. Por isso o status usa uma
// tolerância (epsilon absoluto + percentual), parametrizável pelo chamador.
//
// Não lê banco nem chama API: recebe as duas listas já carregadas e devolve o DTO da ilha.
public static class ReconciliadorProventosAnuais
{
    // Linha oficial do anual (projeção mínima de ProventoAnualB3, desacoplada do EF p/ testar).
    public readonly record struct OficialAnual(int Ano, string Ticker, string Tipo, decimal ValorLiquido);

    // Provento materializado relevante (projeção mínima de RendimentoInvestimento + Asset).
    //  Ticker: sigla do ativo (pode ser null/vazio quando o ativo não tem sigla).
    //  Liquido: já calculado como Amount - TaxWithheld pelo chamador (fonte única do "líquido").
    public readonly record struct MaterializadoAnual(int Ano, string? Ticker, string Tipo, decimal Liquido);

    // Tolerância do status "bate": |diff| <= max(EpsilonAbsoluto, EpsilonPercentual × oficial).
    public readonly record struct Tolerancia(decimal EpsilonAbsoluto, decimal EpsilonPercentual)
    {
        public static readonly Tolerancia Padrao = new(5m, 0.02m); // R$5 ou 2% do oficial.
    }

    // Status de uma linha/ano da reconciliação. Mantido como string no DTO (a view dá cor); aqui é
    // enum interno para a lógica ficar explícita.
    public const string StatusBate = "Bate";
    public const string StatusFaltaMaterializado = "Falta materializado";   // oficial > materializado
    public const string StatusSobraMaterializado = "Sobra materializado";   // materializado > oficial
    public const string StatusSemAtivo = "Sem ativo";                       // oficial sem materializado nenhum

    /// <summary>
    /// Reconcilia oficial × materializado por ano (cada ano vira uma seção com linhas por ticker×tipo).
    /// Os anos saem em ordem decrescente (mais recente primeiro). Lista vazia de oficiais → DTO vazio.
    /// </summary>
    public static FinancasReconciliacaoProventosAnualDto Reconciliar(
        IReadOnlyList<OficialAnual> oficiais,
        IReadOnlyList<MaterializadoAnual> materializados,
        Tolerancia? tolerancia = null)
    {
        if (oficiais is null || oficiais.Count == 0)
            return Vazio();

        var tol = tolerancia ?? Tolerancia.Padrao;

        // Agrupa o materializado por (ano, ticker-normalizado, tipo) p/ casar com o oficial.
        var matPorChave = materializados is null
            ? new Dictionary<(int, string, string), decimal>()
            : materializados
                .GroupBy(m => (m.Ano, Chave(m.Ticker), Chave(m.Tipo)))
                .ToDictionary(g => g.Key, g => g.Sum(x => x.Liquido));

        // Soma do materializado por ano (para o total do ano, mesmo onde não há linha oficial).
        var matTotalAno = materializados is null
            ? new Dictionary<int, decimal>()
            : materializados.GroupBy(m => m.Ano).ToDictionary(g => g.Key, g => g.Sum(x => x.Liquido));

        var anos = new List<ReconciliacaoProventoAnoDto>();
        foreach (var grupoAno in oficiais.GroupBy(o => o.Ano).OrderByDescending(g => g.Key))
        {
            var ano = grupoAno.Key;
            var linhas = new List<ReconciliacaoProventoLinhaDto>();

            foreach (var of in grupoAno
                         .GroupBy(o => (Ticker: Chave(o.Ticker), Tipo: Chave(o.Tipo)))
                         .Select(g => new
                         {
                             g.Key.Ticker,
                             g.Key.Tipo,
                             // Tickers/tipos de exibição: o primeiro valor não-normalizado do grupo.
                             TickerExib = g.First().Ticker,
                             TipoExib = g.First().Tipo,
                             Oficial = g.Sum(x => x.ValorLiquido)
                         })
                         .OrderBy(x => x.TickerExib).ThenBy(x => x.TipoExib))
            {
                var materializado = matPorChave.GetValueOrDefault((ano, of.Ticker, of.Tipo), 0m);
                var diff = Math.Round(materializado - of.Oficial, 2);
                var status = ClassificarStatus(of.Oficial, materializado, diff, tol);

                linhas.Add(new ReconciliacaoProventoLinhaDto(
                    of.TickerExib,
                    of.TipoExib,
                    Math.Round(of.Oficial, 2),
                    Math.Round(materializado, 2),
                    diff,
                    status));
            }

            var totalOficial = Math.Round(grupoAno.Sum(o => o.ValorLiquido), 2);
            var totalMaterializado = Math.Round(matTotalAno.GetValueOrDefault(ano, 0m), 2);
            var diffTotal = Math.Round(totalMaterializado - totalOficial, 2);

            anos.Add(new ReconciliacaoProventoAnoDto(
                ano,
                totalOficial,
                totalMaterializado,
                diffTotal,
                ClassificarStatus(totalOficial, totalMaterializado, diffTotal, tol),
                linhas.Count(l => l.Status != StatusBate),
                linhas));
        }

        var temDivergencia = anos.Any(a => a.LinhasDivergentes > 0 || a.Status != StatusBate);

        return new FinancasReconciliacaoProventosAnualDto(
            TemDados: true,
            TotalOficial: Math.Round(oficiais.Sum(o => o.ValorLiquido), 2),
            TotalMaterializado: Math.Round(anos.Sum(a => a.TotalMaterializado), 2),
            TemDivergencia: temDivergencia,
            Anos: anos);
    }

    // Status pela diferença líquida com a tolerância configurada.
    private static string ClassificarStatus(decimal oficial, decimal materializado, decimal diff, Tolerancia tol)
    {
        // Oficial existe mas nada foi materializado → buraco total (mês(es) faltando / ativo não casou).
        if (materializado == 0m && oficial > 0m)
            return StatusSemAtivo;

        var limite = Math.Max(tol.EpsilonAbsoluto, Math.Abs(oficial) * tol.EpsilonPercentual);
        if (Math.Abs(diff) <= limite)
            return StatusBate;

        return diff < 0m ? StatusFaltaMaterializado : StatusSobraMaterializado;
    }

    // Normaliza ticker/tipo para casar oficial × materializado (trim + maiúsculas).
    private static string Chave(string? valor) => (valor ?? string.Empty).Trim().ToUpperInvariant();

    private static FinancasReconciliacaoProventosAnualDto Vazio()
        => new(false, 0m, 0m, false, []);
}
