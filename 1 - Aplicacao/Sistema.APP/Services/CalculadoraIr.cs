using Sistema.APP.DTOs;
using Sistema.CORE.Entities;

namespace Sistema.APP.Services;

/// <summary>
/// Motor PURO de apuração de IR (sem banco): ganho de capital de renda variável B3 + cripto,
/// Bens e Direitos em 31/12 e proventos (isentos/JCP). Apoio/"cola" — NÃO substitui contador.
/// Regras: ver specs/ir.spec.md (regra cripto VIGENTE em 2026 — a MP 1.303/2025 foi rejeitada).
///
/// Cripto = aplicação no EXTERIOR (Lei 14.754/2023): ganho de capital ANUAL (15% sobre o ganho líquido do
/// ano), NÃO usa a isenção nacional de R$35k/mês; rewards/earn = rendimento tributável (valor BRL na data).
/// Ver specs/ir.spec.md ("Enquadramento — Binance = EXTERIOR") e specs/cripto.spec.md §7 (ponte IR).
///
/// Simplificações (documentadas):
///  - não separa day-trade (tudo tratado como swing);
///  - compensação de prejuízo B3 é cronológica por natureza e atravessa anos (prejuízo de mês isento não é usado);
///  - cripto exterior: ganho líquido anual × 15% (sem compensação cronológica entre anos; perda anual → imposto 0);
///  - proventos classificados pelo IncomeType (texto).
///
/// F3b (jun/2026): Bens e Direitos de cripto com código RFB (08-01/02/03/99) + custo/quantidade em 31/12
/// do ano apurado E do anterior; flag IN 1888 (mês de cripto > R$30k de alienações).
/// TODO (FORA deste escopo): enquadramento nacional como opção; export espelhando as 9 abas do consolidado real.
/// </summary>
public static class CalculadoraIr
{
    // Alíquota do ganho de capital de aplicação financeira no exterior — cripto (Lei 14.754/2023).
    private const decimal AliquotaCriptoExterior = 0.15m;

    // IN 1888/2019: obrigação de declarar à RFB as operações de cripto do mês quando o total > R$ 30.000.
    private const decimal LimiteIN1888 = 30000m;

    public static ApuracaoIrDto Apurar(
        int ano,
        IReadOnlyList<TransacaoFinanceira> transacoes,
        IReadOnlyList<RendimentoInvestimento> proventos)
    {
        var ganhos = ApurarGanhosMensais(ano, transacoes);
        var bensDireitos = MontarBensEDireitos(ano, transacoes);
        var (isentos, exclusiva) = ClassificarProventos(ano, proventos);
        var criptoExterior = ApurarCriptoExterior(ano, transacoes);
        var totalImposto = ganhos.Sum(g => g.Imposto);
        return new ApuracaoIrDto(ano, ganhos, bensDireitos, isentos, exclusiva, totalImposto, criptoExterior);
    }

    // --- Ganho de capital mensal por natureza, com isenção por volume e compensação de prejuízo ---

    private static List<ApuracaoMensalIrDto> ApurarGanhosMensais(int ano, IReadOnlyList<TransacaoFinanceira> transacoes)
    {
        var vendas = ExtrairVendasRealizadas(transacoes);
        var saida = new List<ApuracaoMensalIrDto>();

        foreach (var natureza in vendas.GroupBy(v => v.Natureza))
        {
            var limiteIsencao = natureza.First().LimiteIsencao;
            var aliquotaFn = natureza.First().Aliquota;
            var prejuizo = 0m; // prejuízo acumulado da natureza (compensa meses futuros).

            // Cronológico (inclui anos anteriores) para a compensação atravessar anos corretamente.
            var meses = natureza
                .GroupBy(v => (v.Date.Year, v.Date.Month))
                .OrderBy(g => g.Key.Year).ThenBy(g => g.Key.Month);

            foreach (var mes in meses)
            {
                var totalVendas = mes.Sum(v => v.Proceeds);
                var bruto = mes.Sum(v => v.Gain);
                var isento = limiteIsencao > 0m && totalVendas <= limiteIsencao;

                decimal compensado = 0m, baseCalc = 0m, aliquota = 0m, imposto = 0m;
                if (!isento)
                {
                    if (bruto >= 0m)
                    {
                        compensado = Math.Min(prejuizo, bruto);
                        baseCalc = bruto - compensado;
                        prejuizo -= compensado;
                        aliquota = aliquotaFn(baseCalc);
                        imposto = Math.Round(baseCalc * aliquota, 2);
                    }
                    else
                    {
                        prejuizo += -bruto; // mês com prejuízo: acumula para compensar adiante.
                    }
                }

                if (mes.Key.Year == ano)
                {
                    saida.Add(new ApuracaoMensalIrDto(
                        mes.Key.Year, mes.Key.Month, natureza.Key,
                        Math.Round(totalVendas, 2), Math.Round(bruto, 2),
                        Math.Round(compensado, 2), Math.Round(baseCalc, 2),
                        aliquota, imposto, isento));
                }
            }
        }

        return saida.OrderBy(r => r.Mes).ThenBy(r => r.Natureza).ToList();
    }

    private sealed record Venda(DateTime Date, string Natureza, decimal LimiteIsencao, Func<decimal, decimal> Aliquota, decimal Proceeds, decimal Gain);

    private static List<Venda> ExtrairVendasRealizadas(IReadOnlyList<TransacaoFinanceira> transacoes)
    {
        var vendas = new List<Venda>();
        var estado = new Dictionary<int, (decimal Qtd, decimal Custo)>();

        foreach (var t in transacoes.Where(t => t.Asset is not null).OrderBy(t => t.Date).ThenBy(t => t.Id))
        {
            var st = estado.TryGetValue(t.AssetId, out var s) ? s : (Qtd: 0m, Custo: 0m);
            var delta = Delta(t);
            if (delta > 0m)
            {
                st.Custo += t.Quantity * t.UnitPrice + t.Fees;
                st.Qtd += t.Quantity;
            }
            else if (delta < 0m)
            {
                var pm = st.Qtd > 0m ? st.Custo / st.Qtd : 0m;
                var reduz = Math.Min(t.Quantity, st.Qtd);
                var regra = Regra(t.Asset!);
                if (regra is not null)
                {
                    var proceeds = t.Quantity * t.UnitPrice;
                    var gain = proceeds - t.Fees - reduz * pm;
                    vendas.Add(new Venda(t.Date.Date, regra.Value.Natureza, regra.Value.Limite, regra.Value.Aliquota, proceeds, gain));
                }
                st.Custo -= reduz * pm;
                st.Qtd -= t.Quantity;
                if (st.Qtd <= 0.000000000001m) { st.Qtd = 0m; st.Custo = 0m; }
            }
            estado[t.AssetId] = st;
        }

        return vendas;
    }

    // --- Bens e Direitos: posição em 31/12 do ano (e do anterior), ao custo médio acumulado ---
    // A ficha de B&D pede a situação em 31/12 do ano apurado E do anterior; para cripto, além disso,
    // o código RFB do grupo 08 (08-01 Bitcoin, 08-02 altcoins, 08-03 stablecoin, 08-99 outros).

    private static List<BemDireitoIrDto> MontarBensEDireitos(int ano, IReadOnlyList<TransacaoFinanceira> transacoes)
    {
        var atual = PosicaoAteCorte(transacoes, new DateTime(ano, 12, 31));
        var anterior = PosicaoAteCorte(transacoes, new DateTime(ano - 1, 12, 31));

        // União dos ativos detidos em qualquer dos dois 31/12 (custo > 0 num ano e zerado no outro = variação).
        var ids = atual.Keys.Union(anterior.Keys);
        var itens = new List<BemDireitoIrDto>();

        foreach (var id in ids)
        {
            var temAtual = atual.TryGetValue(id, out var a);
            var temAnt = anterior.TryGetValue(id, out var p);
            var asset = temAtual ? a.Asset : p.Asset;

            // Só declara o que ainda é detido em 31/12 do ano apurado (qtd > 0). Posição zerada no ano
            // atual mas detida no anterior é informada via a baixa na declaração, não como B&D vigente.
            if (!temAtual || a.Qtd <= 0.000001m)
                continue;

            itens.Add(new BemDireitoIrDto(
                asset.Sigla ?? asset.Chave ?? asset.Nome ?? "?",
                asset.Classe.ToString(),
                Math.Round(a.Qtd, 8),
                Math.Round(a.Custo, 2),
                EhCripto(asset) ? CodigoRfbCripto(asset) : string.Empty,
                temAnt ? Math.Round(p.Custo, 2) : 0m,
                temAnt ? Math.Round(p.Qtd, 8) : 0m));
        }

        return itens.OrderBy(b => b.Classe).ThenBy(b => b.Ticker).ToList();
    }

    // Caminha as transações até a data de corte (inclusive) e devolve a posição (qtd/custo médio) por ativo.
    private static Dictionary<int, (decimal Qtd, decimal Custo, AtivoFinanceiro Asset)> PosicaoAteCorte(
        IReadOnlyList<TransacaoFinanceira> transacoes, DateTime corte)
    {
        var estado = new Dictionary<int, (decimal Qtd, decimal Custo, AtivoFinanceiro Asset)>();

        foreach (var t in transacoes.Where(t => t.Asset is not null && t.Date.Date <= corte).OrderBy(t => t.Date).ThenBy(t => t.Id))
        {
            var st = estado.TryGetValue(t.AssetId, out var s) ? s : (Qtd: 0m, Custo: 0m, Asset: t.Asset!);
            var delta = Delta(t);
            if (delta > 0m)
            {
                st.Custo += t.Quantity * t.UnitPrice + t.Fees;
                st.Qtd += t.Quantity;
            }
            else if (delta < 0m)
            {
                var pm = st.Qtd > 0m ? st.Custo / st.Qtd : 0m;
                var reduz = Math.Min(t.Quantity, st.Qtd);
                st.Custo -= reduz * pm;
                st.Qtd -= t.Quantity;
                if (st.Qtd <= 0.000000000001m) { st.Qtd = 0m; st.Custo = 0m; }
            }
            st.Asset = t.Asset!;
            estado[t.AssetId] = st;
        }

        return estado;
    }

    // Código RFB do grupo 08 (Criptoativos), conforme o ativo (ver specs/ir.spec.md):
    //  08-01 Bitcoin (BTC) · 08-03 stablecoins (USDT/USDC/FDUSD) · 08-02 demais criptomoedas (altcoins)
    //  08-99 outros criptoativos/tokens (ex.: tokens de staking WBETH/BNSOL, NFTs, utility tokens).
    private static string CodigoRfbCripto(AtivoFinanceiro a)
    {
        var ticker = (a.Sigla ?? a.Chave ?? a.Nome ?? string.Empty).Trim().ToUpperInvariant();
        if (ticker is "BTC" or "XBT")
            return "08-01";
        if (EhStablecoin(ticker))
            return "08-03";
        if (EhAltcoin(ticker))
            return "08-02";
        return "08-99"; // demais tokens/criptoativos (staking, utility, NFT…).
    }

    private static bool EhStablecoin(string ticker) =>
        ticker is "USDT" or "USDC" or "FDUSD" or "BUSD" or "DAI" or "TUSD" or "USDP" or "BRLUSD";

    // Altcoins = moedas de protocolo "principais" (08-02). O que não for BTC, stablecoin nem aqui cai em 08-99.
    private static bool EhAltcoin(string ticker) =>
        ticker is "ETH" or "SOL" or "XRP" or "DOGE" or "BNB" or "ADA" or "LTC" or "DOT" or "AVAX"
            or "MATIC" or "POL" or "TRX" or "LINK" or "BCH" or "XLM" or "ATOM" or "ETC" or "NEAR";

    // --- Proventos: isentos (dividendos + FII) vs tributação exclusiva (JCP) ---

    private static (List<RendimentoIrDto> Isentos, List<RendimentoIrDto> Exclusiva) ClassificarProventos(
        int ano, IReadOnlyList<RendimentoInvestimento> proventos)
    {
        var doAno = proventos.Where(p => (p.PaymentDate ?? p.ReferenceDate)?.Year == ano);
        var isentos = new List<RendimentoIrDto>();
        var exclusiva = new List<RendimentoIrDto>();

        foreach (var g in doAno.GroupBy(p => ClassificarTipo(p.IncomeType)))
        {
            var total = Math.Round(g.Sum(p => p.Amount), 2);
            if (total <= 0m)
                continue;
            var item = new RendimentoIrDto(g.Key.Rotulo, total);
            if (g.Key.Categoria == CategoriaProvento.Exclusiva)
                exclusiva.Add(item);
            else if (g.Key.Categoria == CategoriaProvento.Isento)
                isentos.Add(item);
            // Tributável (earn cripto) fica fora destes blocos — entra na apuração de cripto.
        }

        return (isentos, exclusiva);
    }

    private enum CategoriaProvento { Isento, Exclusiva, Tributavel }

    private static (CategoriaProvento Categoria, string Rotulo) ClassificarTipo(string? incomeType)
    {
        var t = (incomeType ?? string.Empty).ToUpperInvariant();
        if (t.Contains("JCP") || t.Contains("JURO"))
            return (CategoriaProvento.Exclusiva, "Juros sobre Capital Próprio (JCP)");
        if (t.Contains("DIVIDEND"))
            return (CategoriaProvento.Isento, "Dividendos");
        if (t.Contains("EARN") || t.Contains("STAK") || t.Contains("CRIPTO"))
            return (CategoriaProvento.Tributavel, "Rendimentos de cripto (earn)");
        return (CategoriaProvento.Isento, "Rendimentos de FII / isentos");
    }

    // --- Cripto como aplicação no EXTERIOR (Lei 14.754/2023) ---
    // Ganho de capital ANUAL (15% sobre o ganho líquido do ano); NÃO usa a isenção nacional de R$35k/mês.
    // Rewards/earn = rendimento tributável (valor BRL na data). As alienações cripto já chegam valoradas em
    // BRL (F2): pernas Venda com UnitPrice/GrossAmount em BRL; earn como Rendimento com UnitPrice em BRL.

    private static CriptoExteriorIrDto ApurarCriptoExterior(int ano, IReadOnlyList<TransacaoFinanceira> transacoes)
    {
        var alienacoes = new List<AlienacaoCriptoIrDto>();
        var rewards = new List<RewardCriptoIrDto>();
        var estado = new Dictionary<int, (decimal Qtd, decimal Custo)>(); // custo médio móvel por ativo cripto.

        // Cronológico (inclui anos anteriores) para o custo médio ficar correto na alienação do ano apurado.
        foreach (var t in transacoes.Where(t => t.Asset is not null && EhCripto(t.Asset!)).OrderBy(t => t.Date).ThenBy(t => t.Id))
        {
            var st = estado.TryGetValue(t.AssetId, out var s) ? s : (Qtd: 0m, Custo: 0m);
            var doAno = t.Date.Year == ano;
            var rotulo = t.Asset!.Sigla ?? t.Asset!.Chave ?? t.Asset!.Nome ?? "?";

            if (t.OperationType == TipoOperacaoFinanceira.Rendimento)
            {
                // Earn/staking-reward/airdrop: entra na posição com custo = valor de mercado BRL na data;
                // esse MESMO valor é o rendimento tributável do exterior.
                var valor = t.Quantity * t.UnitPrice;
                st.Custo += valor;
                st.Qtd += t.Quantity;
                if (doAno && valor > 0m)
                    rewards.Add(new RewardCriptoIrDto(t.Date.Month, t.Date.Date, rotulo, t.Quantity, Math.Round(valor, 2)));
            }
            else
            {
                var delta = Delta(t);
                if (delta > 0m) // Compra (com fiat) ou perna-entrada de permuta: aumenta posição/custo.
                {
                    st.Custo += t.Quantity * t.UnitPrice + t.Fees;
                    st.Qtd += t.Quantity;
                }
                else if (delta < 0m) // Venda por fiat OU permuta-saída: é alienação tributável (exterior).
                {
                    var pm = st.Qtd > 0m ? st.Custo / st.Qtd : 0m;
                    var reduz = Math.Min(t.Quantity, st.Qtd);
                    var valorAlienacao = t.Quantity * t.UnitPrice;
                    var custo = reduz * pm;
                    var ganho = valorAlienacao - t.Fees - custo;
                    if (doAno)
                        alienacoes.Add(new AlienacaoCriptoIrDto(
                            t.Date.Month, t.Date.Date, rotulo, t.Quantity,
                            Math.Round(valorAlienacao, 2), Math.Round(custo, 2), Math.Round(ganho, 2)));
                    st.Custo -= custo;
                    st.Qtd -= t.Quantity;
                    if (st.Qtd <= 0.000000000001m) { st.Qtd = 0m; st.Custo = 0m; }
                }
            }
            estado[t.AssetId] = st;
        }

        var ganhoLiquido = Math.Round(alienacoes.Sum(a => a.Ganho), 2);
        var imposto = ganhoLiquido > 0m ? Math.Round(ganhoLiquido * AliquotaCriptoExterior, 2) : 0m;
        var totalRewards = Math.Round(rewards.Sum(r => r.ValorBRL), 2);

        // IN 1888/2019: total de ALIENAÇÕES de cripto por mês; marca os meses > R$ 30.000 (obriga declarar).
        var mesesIN1888 = alienacoes
            .GroupBy(a => a.Mes)
            .Select(g =>
            {
                var total = Math.Round(g.Sum(a => a.ValorAlienacao), 2);
                return new MesIN1888Dto(g.Key, total, total > LimiteIN1888);
            })
            .OrderBy(m => m.Mes)
            .ToList();

        return new CriptoExteriorIrDto(
            alienacoes.OrderBy(a => a.Data).ThenBy(a => a.Ativo).ToList(),
            rewards.OrderBy(r => r.Data).ThenBy(r => r.Ativo).ToList(),
            ganhoLiquido, AliquotaCriptoExterior, imposto, totalRewards, mesesIN1888);
    }

    // --- Helpers ---

    private static bool EhCripto(AtivoFinanceiro a) => a.EhCripto || a.Classe == ClasseAtivo.Cripto;

    private static decimal Delta(TransacaoFinanceira t) => t.OperationType switch
    {
        TipoOperacaoFinanceira.Compra or TipoOperacaoFinanceira.Deposito or TipoOperacaoFinanceira.Rendimento => t.Quantity,
        TipoOperacaoFinanceira.Venda or TipoOperacaoFinanceira.Saque or TipoOperacaoFinanceira.Taxa => -t.Quantity,
        _ => 0m
    };

    private readonly record struct RegraNatureza(string Natureza, decimal Limite, Func<decimal, decimal> Aliquota);

    private static RegraNatureza? Regra(AtivoFinanceiro a)
    {
        // Cripto NÃO entra no ganho de capital mensal nacional: é apurado como exterior (Lei 14.754/2023)
        // em ApurarCriptoExterior. Retornar null aqui evita que a venda cripto consuma a isenção R$35k.
        if (EhCripto(a))
            return null;

        return a.Classe switch
        {
            ClasseAtivo.Acao => new RegraNatureza("Ações", 20000m, _ => 0.15m),
            ClasseAtivo.FII => new RegraNatureza("FII", 0m, _ => 0.20m),
            ClasseAtivo.ETF => new RegraNatureza("ETF", 0m, _ => 0.15m),
            ClasseAtivo.BDR => new RegraNatureza("BDR", 0m, _ => 0.15m),
            _ => (RegraNatureza?)null // RendaFixa/Caixa/Outro: fora do escopo (tributação na fonte/à parte).
        };
    }
}
