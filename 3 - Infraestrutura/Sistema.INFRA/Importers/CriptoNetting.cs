using Sistema.CORE.Entities;

namespace Sistema.INFRA.Importers;

/// <summary>
/// Lógica PURA (sem banco) do netting das transações cripto da Binance (F1 — specs/cripto.spec.md).
/// Mantida separada do <see cref="FinancasImportador"/> para ser testável sem DbContext, no estilo de
/// <see cref="ExtratoB3Materializador"/>.
///
/// Causa-raiz que isto corrige: o ledger da Binance ("Histórico de Transações") registra cada operação
/// como UMA ou DUAS pernas (linhas) com o campo <c>Alterar</c> (Change) assinado e o mesmo timestamp.
/// A materialização antiga só pegava negócios COM preço (<c>Price &gt; 0</c>), então as pernas de
/// permuta/earn (sem preço) não entravam: a origem nunca era abatida → fantasmas (N compras / 0 vendas),
/// stablecoins/BRL como posição e saldos negativos.
///
/// O netting aqui converte cada perna em um movimento canônico de POSIÇÃO (quantidade), abatendo a
/// origem das permutas. Escopo F1 = posição correta por QUANTIDADE; a valoração em BRL e a ponte para o
/// IR ficam para F2/F3. Por isso a perna de saída sai com preço = PM corrente do ativo (resolvido na
/// persistência, que é stateful) → realizado ≈ 0 nesta fase; e o earn entra com custo 0.
/// </summary>
public static class CriptoNetting
{
    public const string Fonte = "Binance";

    /// <summary>
    /// Moedas fiduciárias: NÃO são ativo de posição — viram caixa e ficam fora da carteira.
    /// Corrige o bug "Cripto · BRLUSDT" / BRL aparecendo como posição.
    /// </summary>
    private static readonly HashSet<string> Fiats = new(StringComparer.OrdinalIgnoreCase)
    {
        "BRL", "USD", "EUR", "GBP", "ARS"
    };

    /// <summary>
    /// Decide o efeito de um movimento cripto na posição a partir da operação do ledger e do sinal do
    /// Change. Lógica pura: recebe os movimentos já parseados (uma linha do ledger = um
    /// <see cref="MovimentoCriptoBruto"/>) e devolve apenas os que mudam a posição de um ativo detido.
    ///
    /// Regras:
    /// - <b>Fiat</b> (BRL/USD/…): descartado (é caixa, não posição).
    /// - <b>Transferência interna</b> (Simple Earn Subscription/Redemption, Main↔Funding): a mesma moeda
    ///   sai e volta — net-zero na posição → descartado, não polui PM nem infla quantidade.
    /// - <b>Rendimento</b> (earn/juros/airdrop/reward/crypto box/voucher/launchpool/token swap): entrada
    ///   de posição SEM custo de compra → <see cref="TipoOperacaoFinanceira.Rendimento"/>, custo 0.
    /// - <b>Permuta / trade / staking-purchase / fee</b>: cada perna cripto vira Compra (Change&gt;0) ou
    ///   Venda (Change&lt;0). Assim a origem de toda troca é abatida (some fantasma/negativo). A perna de
    ///   saída sai com preço a definir (PM corrente, resolvido na persistência) → realizado ≈ 0 na F1.
    /// </summary>
    public static IReadOnlyList<MovimentoCriptoCanonico> Netar(IEnumerable<MovimentoCriptoBruto> movimentos)
    {
        var canonicos = new List<MovimentoCriptoCanonico>();
        foreach (var mov in movimentos)
        {
            var symbol = (mov.AssetSymbol ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(symbol))
                continue;

            var categoria = Classificar(mov.RawOperation);

            // Fiat nunca é posição (caixa). Vale para qualquer categoria (depósito BRL, perna BRL de
            // compra/convert, withdraw fiat…).
            if (categoria == CategoriaCripto.Fiat || EhFiat(symbol))
                continue;

            // Transferência interna: mesma moeda, soma zero — não muda holdings.
            if (categoria == CategoriaCripto.TransferenciaInterna)
                continue;

            var quantidade = Math.Abs(mov.Change);
            if (quantidade <= 0m)
                continue;

            if (categoria == CategoriaCripto.Rendimento)
            {
                // Earn/airdrop só faz sentido como entrada (Change > 0). Um Change negativo numa categoria
                // de rendimento seria anômalo — ignora para não criar venda fantasma.
                if (mov.Change <= 0m)
                    continue;

                canonicos.Add(new MovimentoCriptoCanonico(
                    symbol, mov.Timestamp, TipoOperacaoFinanceira.Rendimento, quantidade,
                    DefinirPrecoNaPersistencia: false, PrecoZero: true, mov.RawOperation, mov.SourceStagingId));
                continue;
            }

            // Permuta/trade/staking/fee: a perna assinada vira Compra (entra) ou Venda (sai/abate).
            if (mov.Change > 0m)
            {
                // Entrada de uma troca: a quantidade entra; o custo (em BRL) é F2 → preço definido na
                // persistência como o PM da saída correspondente (≈ neutro nesta fase). Aqui marcamos
                // para a persistência resolver o preço.
                canonicos.Add(new MovimentoCriptoCanonico(
                    symbol, mov.Timestamp, TipoOperacaoFinanceira.Compra, quantidade,
                    DefinirPrecoNaPersistencia: true, PrecoZero: false, mov.RawOperation, mov.SourceStagingId));
            }
            else
            {
                // Saída de uma troca (ou fee): ABATE a origem. Preço = PM corrente (persistência) →
                // realizado ≈ 0 na F1.
                canonicos.Add(new MovimentoCriptoCanonico(
                    symbol, mov.Timestamp, TipoOperacaoFinanceira.Venda, quantidade,
                    DefinirPrecoNaPersistencia: true, PrecoZero: false, mov.RawOperation, mov.SourceStagingId));
            }
        }

        return canonicos;
    }

    /// <summary>True se o símbolo é uma moeda fiduciária (caixa, fora da posição).</summary>
    public static bool EhFiat(string? symbol)
        => !string.IsNullOrWhiteSpace(symbol) && Fiats.Contains(symbol.Trim());

    /// <summary>
    /// Classifica a operação bruta do ledger da Binance numa categoria de netting. Baseado no vocabulário
    /// real do "Histórico de Transações" (Deposit, Buy Crypto With Fiat, Transaction Spend/Buy/Sold/
    /// Revenue/Fee, Binance Convert, Small Assets Exchange BNB, SOL/WBETH/BNSOL Staking, Simple Earn
    /// Flexible Subscription/Redemption/Interest/Locked*, HODLer Airdrops, Earn-Airdrop, Crypto Box,
    /// Token Swap, Cash Voucher, Launchpool, Transfer Between Main and Funding Wallet, Fiat Withdraw…).
    /// </summary>
    public static CategoriaCripto Classificar(string? rawOperation)
    {
        var op = (rawOperation ?? string.Empty).Trim().ToUpperInvariant();
        if (op.Length == 0)
            return CategoriaCripto.Permuta;

        // Depósito/saque de fiat — caixa.
        if (op.Contains("DEPOSIT") && !op.Contains("EARN"))
            return CategoriaCripto.Fiat;
        if (op.Contains("FIAT WITHDRAW") || op == "WITHDRAW")
            return CategoriaCripto.Fiat;

        // Transferências internas (não mudam holdings): Earn subscription/redemption (mesma moeda) e
        // transferência entre carteiras.
        if (op.Contains("SUBSCRIPTION") || op.Contains("REDEMPTION") || op.Contains("TRANSFER BETWEEN"))
            return CategoriaCripto.TransferenciaInterna;

        // Rendimentos (entrada sem custo de compra). "Earn …" cobre Flexible/Locked Interest e Rewards,
        // Airdrop, Crypto Box, Voucher, Launchpool, Token Swap, Asset Recovery (poeira recuperada).
        if (op.Contains("INTEREST") || op.Contains("REWARD") || op.Contains("AIRDROP")
            || op.Contains("CRYPTO BOX") || op.Contains("VOUCHER") || op.Contains("LAUNCHPOOL")
            || op.Contains("TOKEN SWAP") || op.Contains("DISTRIBUTION") || op.Contains("ASSET RECOVERY"))
            return CategoriaCripto.Rendimento;

        // Tudo o mais que move cripto é permuta/trade/staking-purchase/fee → cada perna abate/soma.
        return CategoriaCripto.Permuta;
    }
}

/// <summary>Categoria de netting de uma linha do ledger cripto.</summary>
public enum CategoriaCripto
{
    /// <summary>Permuta/compra/venda/staking-purchase/fee — cada perna cripto soma ou abate a posição.</summary>
    Permuta = 1,

    /// <summary>Rendimento (earn/juros/airdrop/reward) — entra na posição sem custo de compra.</summary>
    Rendimento = 2,

    /// <summary>Moeda fiduciária (BRL/USD/…) — caixa, fora da posição.</summary>
    Fiat = 3,

    /// <summary>Transferência interna (Earn subscription/redemption, Main↔Funding) — net-zero, ignorada.</summary>
    TransferenciaInterna = 4
}

/// <summary>
/// Uma linha bruta do ledger cripto (uma perna). <see cref="Change"/> é o campo "Alterar" ASSINADO
/// (positivo = entrou; negativo = saiu). <see cref="SourceStagingId"/> liga ao staging (TransacaoCripto)
/// para idempotência na materialização.
/// </summary>
public sealed record MovimentoCriptoBruto(
    string AssetSymbol,
    DateTime? Timestamp,
    string? RawOperation,
    decimal Change,
    int SourceStagingId);

/// <summary>
/// Movimento canônico de posição produzido pelo netting. <see cref="DefinirPrecoNaPersistencia"/> sinaliza
/// que o preço deve ser resolvido pelo PM corrente do ativo (perna de troca → realizado ≈ 0 na F1).
/// <see cref="PrecoZero"/> sinaliza custo 0 (rendimento).
/// </summary>
public sealed record MovimentoCriptoCanonico(
    string AssetSymbol,
    DateTime? Timestamp,
    TipoOperacaoFinanceira OperationType,
    decimal Quantity,
    bool DefinirPrecoNaPersistencia,
    bool PrecoZero,
    string? RawOperation,
    int SourceStagingId);
