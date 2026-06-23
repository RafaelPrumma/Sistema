using System.Globalization;
using Sistema.CORE.Entities;

namespace Sistema.INFRA.Importers;

/// <summary>
/// Lógica PURA (sem banco) da F3 — reconciliação da posição pela aba Posição da B3 (custódia
/// oficial) + ativo virtual VARIAÇÃO (specs/importador-b3.spec.md §10 passo 4).
///
/// A aba Posição (mais recente) é a verdade de QUANTIDADE: o que não aparece nela vale 0 (vendido).
/// Os relatórios da B3/notas têm "fantasmas" — ativos vendidos que não zeram porque a venda faltou.
/// Esta classe calcula, por ativo, <c>diff = alvo − calculado</c> e devolve um ajuste que leva a
/// posição ao alvo (ao PM corrente → realizado ≈ 0) + contrapartida no ativo VARIAÇÃO (valor da
/// diferença) → a carteira mostra a posição real E a discrepância fica visível/auditável.
///
/// Separada do <see cref="FinancasImportador"/> para ser testável sem DbContext (estilo
/// <see cref="ExtratoB3Materializador"/>).
/// </summary>
public static class ReconciliadorPosicaoB3
{
    /// <summary>Fonte das transações de ajuste. Sobrevive ao resync (que só apaga Importação).</summary>
    public const string Fonte = "Reconciliação";

    /// <summary>Ativo virtual que absorve a diferença não explicada pelos relatórios.</summary>
    public const string AssetKeyVariacao = "VARIACAO";
    public const string NomeVariacao = "Ajuste de Reconciliação (variação de custódia)";

    /// <summary>Tolerância: diferença de quantidade abaixo disso é ruído de arredondamento.</summary>
    public const decimal Epsilon = 0.000001m;

    /// <summary>
    /// Lê as quantidades-alvo da Posição mais recente. <paramref name="linhasPosicao"/> são as linhas
    /// (header→valor) das abas Posição - Ações/Fundos do documento de período MAIS RECENTE. O ticker é
    /// normalizado por <see cref="ExtratoB3Materializador.NormalizarTicker"/> (fracionário + alias) e as
    /// quantidades do mesmo ticker (lote-padrão + fracionário) são somadas. Linha sem ticker/quantidade
    /// é ignorada.
    /// </summary>
    public static Dictionary<string, decimal> ExtrairAlvos(IEnumerable<IReadOnlyDictionary<string, string>> linhasPosicao)
    {
        var alvos = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
        foreach (var row in linhasPosicao)
        {
            var ticker = ExtratoB3Materializador.NormalizarTicker(Campo(row, "Código de Negociação"));
            if (string.IsNullOrWhiteSpace(ticker))
                continue;

            var quantidade = ExtratoB3Materializador.ParseDecimal(Campo(row, "Quantidade"));
            if (quantidade <= 0m)
                continue;

            alvos[ticker] = alvos.GetValueOrDefault(ticker) + quantidade;
        }

        return alvos;
    }

    /// <summary>
    /// Lê o <b>Preço de Fechamento</b> (preço de mercado de fim de mês da custódia) por ticker da
    /// Posição mais recente. Mesmas linhas que <see cref="ExtrairAlvos"/>: o ticker é normalizado
    /// (fracionário + alias) e linhas sem ticker/preço positivo são ignoradas. O fracionário e o
    /// lote-padrão do mesmo papel têm o mesmo preço de fechamento, então o primeiro positivo basta —
    /// não somamos preços. Serve para alimentar uma <c>CotacaoAtivoFinanceiro</c> (B3Custódia) e fazer
    /// o "Resultado" do dashboard deixar de ser 0 para ações/FII sem token Brapi.
    /// </summary>
    public static Dictionary<string, decimal> ExtrairPrecosFechamento(IEnumerable<IReadOnlyDictionary<string, string>> linhasPosicao)
    {
        var precos = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
        foreach (var row in linhasPosicao)
        {
            var ticker = ExtratoB3Materializador.NormalizarTicker(Campo(row, "Código de Negociação"));
            if (string.IsNullOrWhiteSpace(ticker) || precos.ContainsKey(ticker))
                continue;

            var preco = ExtratoB3Materializador.ParseDecimal(Campo(row, "Preço de Fechamento"));
            if (preco <= 0m)
                continue;

            precos[ticker] = preco;
        }

        return precos;
    }

    /// <summary>
    /// Calcula os ajustes de reconciliação. Para cada ativo B3 (não-cripto) presente no cálculo OU na
    /// Posição, compara a quantidade <paramref name="calculadoPorAtivo"/> (soma canônica, JÁ sem os
    /// ajustes de Reconciliação) com o alvo da Posição (<paramref name="alvoPorTicker"/>; ausente → 0).
    /// Se |diff| &gt; <see cref="Epsilon"/>, emite uma instrução de ajuste levando ao alvo, ao PM
    /// corrente (<paramref name="precoMedioPorAtivo"/>) — realizado ≈ 0.
    /// </summary>
    public static IReadOnlyList<AjusteReconciliacao> CalcularAjustes(
        IReadOnlyList<AtivoReconciliavel> ativos,
        IReadOnlyDictionary<int, decimal> calculadoPorAtivo,
        IReadOnlyDictionary<int, decimal> precoMedioPorAtivo,
        IReadOnlyDictionary<string, decimal> alvoPorTicker)
    {
        var ajustes = new List<AjusteReconciliacao>();
        foreach (var ativo in ativos)
        {
            var calculado = calculadoPorAtivo.GetValueOrDefault(ativo.AssetId);
            var alvo = alvoPorTicker.GetValueOrDefault(ativo.TickerNormalizado);
            var diff = alvo - calculado;
            if (Math.Abs(diff) <= Epsilon)
                continue;

            var pm = precoMedioPorAtivo.GetValueOrDefault(ativo.AssetId);
            if (pm < 0m)
                pm = 0m;

            var tipo = diff > 0m ? TipoOperacaoFinanceira.Compra : TipoOperacaoFinanceira.Venda;
            var quantidade = Math.Abs(diff);
            var valor = decimal.Round(quantidade * pm, 8);
            var sentido = diff > 0m ? "faltando" : "sobrando";
            var observacao =
                $"Ajuste de reconciliação pela Posição B3: alvo {Fmt(alvo)}, calculado {Fmt(calculado)} ({sentido} {Fmt(quantidade)}).";

            ajustes.Add(new AjusteReconciliacao(
                ativo.AssetId, tipo, quantidade, pm, valor, alvo, calculado, observacao));
        }

        return ajustes;
    }

    private static string Fmt(decimal value)
        => value.ToString("0.########", CultureInfo.InvariantCulture);

    private static string? Campo(IReadOnlyDictionary<string, string> row, string header)
        => row.TryGetValue(header, out var value) && !string.IsNullOrWhiteSpace(value) ? value.Trim() : null;
}

/// <summary>Um ativo B3 (não-cripto) candidato à reconciliação, com seu ticker já normalizado.</summary>
public sealed record AtivoReconciliavel(int AssetId, string TickerNormalizado);

/// <summary>
/// Instrução de ajuste para um ativo: leva a posição ao alvo. <see cref="ValorContrapartida"/> é o
/// valor (|diff|×PM) que vai para o ativo VARIAÇÃO como contrapartida (preserva o patrimônio total e
/// deixa a diferença visível).
/// </summary>
public sealed record AjusteReconciliacao(
    int AssetId,
    TipoOperacaoFinanceira OperationType,
    decimal Quantidade,
    decimal PrecoMedio,
    decimal ValorContrapartida,
    decimal Alvo,
    decimal Calculado,
    string Observacao);
