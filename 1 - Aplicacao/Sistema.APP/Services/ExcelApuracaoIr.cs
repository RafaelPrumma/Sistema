using System.Globalization;
using Sistema.APP.DTOs;

namespace Sistema.APP.Services;

/// <summary>
/// Monta o .xlsx da apuração de IR ("cola"), espelhando as abas do consolidado real do usuário
/// (ver specs/ir.spec.md §"Estrutura do export"): Resumo · Como_Usar · Bens_Direitos ·
/// Aplic_Fin_Exterior · Operacoes_Ganho · Rendimentos_Rewards · Resumo_Mensal · Regras_Fontes.
/// Puro/testável, sem dependência externa (escreve via EscritorXlsx — writer OOXML próprio).
/// Apoio — NÃO substitui contador.
/// </summary>
public static class ExcelApuracaoIr
{
    private static readonly string[] MesesPt =
    {
        "", "Janeiro", "Fevereiro", "Março", "Abril", "Maio", "Junho",
        "Julho", "Agosto", "Setembro", "Outubro", "Novembro", "Dezembro"
    };

    // Linha em branco (separador visual entre blocos de uma aba). Compartilhada para não alocar.
    private static readonly object?[] LinhaVazia = Array.Empty<object?>();

    public static byte[] Gerar(ApuracaoIrDto ap)
    {
        ArgumentNullException.ThrowIfNull(ap);
        var abas = new List<AbaXlsx>
        {
            Resumo(ap),
            ComoUsar(),
            BensDireitos(ap.Ano, ap.BensEDireitos),
            AplicFinExterior(ap.Ano, ap.CriptoExterior),
            OperacoesGanho(ap.CriptoExterior.Alienacoes),
            RendimentosRewards(ap.CriptoExterior.Rewards),
            ResumoMensal(ap),
            RegrasFontes(),
        };
        return EscritorXlsx.Gerar(abas);
    }

    // ── Resumo: visão consolidada do imposto (B3 + cripto exterior) + proventos isentos/JCP. ──
    private static AbaXlsx Resumo(ApuracaoIrDto ap)
    {
        var cripto = ap.CriptoExterior;
        var totalIsentos = ap.RendimentosIsentos.Sum(r => r.Valor);
        var totalJcp = ap.TributacaoExclusiva.Sum(r => r.Valor);
        var mesesIN1888 = cripto.MesesIN1888.Where(m => m.UltrapassaLimite).Select(m => MesesPt[m.Mes]).ToList();

        var rows = new List<IReadOnlyList<object?>>
        {
            new object?[] { "Apuração de IR — visão consolidada (apoio, não substitui contador)" },
            new object?[] { "Ano-calendário", ap.Ano },
            LinhaVazia,
            new object?[] { "Bloco", "Valor (R$)", "Observação" },
            new object?[] { "Imposto ganho de capital B3 (DARF)", ap.TotalImpostoDevido, "Ações isenção R$20k/mês; FII 20%; ETF/BDR 15%" },
            new object?[] { "Imposto ganho de capital cripto exterior", cripto.ImpostoGanhoCapital, $"15% sobre o ganho líquido anual (Lei 14.754/2023)" },
            new object?[] { "Imposto total estimado", ap.TotalImpostoDevido + cripto.ImpostoGanhoCapital, "B3 (DARF mensal) + cripto exterior (anual)" },
            LinhaVazia,
            new object?[] { "Ganho de capital líquido cripto exterior", cripto.GanhoCapitalLiquido, "Soma de ganhos/perdas das alienações no ano" },
            new object?[] { "Total de rewards de cripto (rendimento)", cripto.TotalRewards, "Earn/staking/airdrop — tributável, valorado na data" },
            new object?[] { "Proventos isentos (dividendos + FII)", totalIsentos, "Ficha Rendimentos Isentos e Não Tributáveis" },
            new object?[] { "Tributação exclusiva (JCP)", totalJcp, "IRRF 15% na fonte" },
            LinhaVazia,
            new object?[] { "Meses que passaram de R$30k (IN 1888)", mesesIN1888.Count == 0 ? "Nenhum" : string.Join(", ", mesesIN1888), "Obrigação de DECLARAR à RFB (≠ imposto)" },
        };
        return new AbaXlsx("Resumo", rows);
    }

    // ── Como_Usar: onde lançar cada bloco na declaração (texto estático de orientação). ──
    private static AbaXlsx ComoUsar()
    {
        var rows = new List<IReadOnlyList<object?>>
        {
            new object?[] { "Como usar esta cola na declaração (IRPF)" },
            new object?[] { "Esta planilha é apoio. NÃO substitui contador. Confira os valores contra os informes oficiais." },
            LinhaVazia,
            new object?[] { "Aba", "Onde lançar / o que fazer" },
            new object?[] { "Resumo", "Visão geral do imposto e dos proventos do ano. Comece por aqui." },
            new object?[] { "Bens_Direitos", "Ficha Bens e Direitos: um item por ativo (situação em 31/12 ao custo, ano e anterior). Cripto usa código RFB do grupo 08." },
            new object?[] { "Aplic_Fin_Exterior", "Cripto na Binance = aplicação financeira no exterior (Lei 14.754/2023): situação 31/12, ganho/perda anual, imposto 15%." },
            new object?[] { "Operacoes_Ganho", "Memória de cálculo do ganho de capital de cripto: uma linha por alienação (venda E permuta/convert/staking)." },
            new object?[] { "Rendimentos_Rewards", "Rewards de cripto (earn/staking/airdrop): rendimento tributável, valorado em BRL na data do recebimento." },
            new object?[] { "Resumo_Mensal", "Ganhos mensais B3 (base da DARF) + alienações de cripto por mês (flag IN 1888 quando > R$30k)." },
            new object?[] { "Regras_Fontes", "Regras aplicadas e referências oficiais (RFB). Reconfirme a cada ano-calendário." },
            LinhaVazia,
            new object?[] { "TODO", "Compras_BRL (detalhe de aportes em reais) não é gerada: o ApuracaoIrDto não traz o detalhe de compras." },
        };
        return new AbaXlsx("Como_Usar", rows);
    }

    // ── Bens_Direitos: posição em 31/12 ao custo (ano e anterior). Cripto traz código RFB grupo 08. ──
    private static AbaXlsx BensDireitos(int ano, IReadOnlyList<BemDireitoIrDto> bens)
    {
        var rows = new List<IReadOnlyList<object?>>
        {
            new object?[]
            {
                "Ticker", "Classe", "Código RFB",
                $"Qtd 31/12/{ano - 1}", $"Custo 31/12/{ano - 1} (R$)",
                $"Qtd 31/12/{ano}", $"Custo 31/12/{ano} (R$)",
                "Obrigatório? (custo ≥ R$5k)", "Discriminação (sugestão)"
            }
        };
        foreach (var b in bens)
        {
            var obrigatorio = b.Custo >= 5000m || b.CustoAnterior >= 5000m;
            rows.Add(new object?[]
            {
                b.Ticker, b.Classe, b.Codigo,
                b.QuantidadeAnterior, b.CustoAnterior,
                b.Quantidade, b.Custo,
                obrigatorio ? "Sim" : "Não",
                Discriminacao(b, ano)
            });
        }
        return new AbaXlsx("Bens_Direitos", rows);
    }

    private static string Discriminacao(BemDireitoIrDto b, int ano)
    {
        var qtd = b.Quantidade.ToString("0.########", CultureInfo.GetCultureInfo("pt-BR"));
        var custo = b.Custo.ToString("N2", CultureInfo.GetCultureInfo("pt-BR"));
        return string.IsNullOrEmpty(b.Codigo)
            ? $"{qtd} de {b.Ticker} ({b.Classe}). Custo de aquisição acumulado em 31/12/{ano}: R$ {custo}."
            : $"{qtd} de {b.Ticker} ({b.Classe}) custodiado na Binance. Custo de aquisição em 31/12/{ano}: R$ {custo}.";
    }

    // ── Aplic_Fin_Exterior: resumo do ganho de capital cripto exterior (Lei 14.754) + rewards + IN 1888. ──
    private static AbaXlsx AplicFinExterior(int ano, CriptoExteriorIrDto cripto)
    {
        var rows = new List<IReadOnlyList<object?>>
        {
            new object?[] { "Cripto como aplicação financeira no exterior (Lei 14.754/2023) — país: Malta (Binance)" },
            new object?[] { "Ano-calendário", ano },
            LinhaVazia,
            new object?[] { "Item", "Valor", "Observação" },
            new object?[] { "Ganho de capital líquido do ano (R$)", cripto.GanhoCapitalLiquido, "Soma de ganhos/perdas das alienações (inclui permuta)" },
            new object?[] { "Alíquota", cripto.Aliquota, "15% sobre o ganho líquido anual" },
            new object?[] { "Imposto sobre ganho de capital (R$)", cripto.ImpostoGanhoCapital, "0 quando o ano fecha em prejuízo" },
            new object?[] { "Total de rewards no ano (R$)", cripto.TotalRewards, "Rendimento tributável (ver aba Rendimentos_Rewards)" },
            new object?[] { "Imposto pago no exterior (R$)", 0m, "Binance não retém IR brasileiro (informar 0 salvo retenção comprovada)" },
            LinhaVazia,
            new object?[] { "IN 1888/2019 — meses com total de alienações > R$30.000 (obrigação de declarar):" },
            new object?[] { "Mês", "Total alienações cripto (R$)", "Passou de R$30k?" },
        };
        foreach (var m in cripto.MesesIN1888.OrderBy(m => m.Mes))
            rows.Add(new object?[] { Mes(m.Mes), m.TotalAlienacoes, m.UltrapassaLimite ? "Sim" : "Não" });
        return new AbaXlsx("Aplic_Fin_Exterior", rows);
    }

    // ── Operacoes_Ganho: uma linha por alienação de cripto (venda + permuta/convert/staking). ──
    private static AbaXlsx OperacoesGanho(IReadOnlyList<AlienacaoCriptoIrDto> alienacoes)
    {
        var rows = new List<IReadOnlyList<object?>>
        {
            new object?[]
            {
                "Data", "Mês", "Ativo", "Quantidade",
                "Valor de alienação (R$)", "Custo de aquisição (R$)", "Ganho/Perda (R$)"
            }
        };
        foreach (var a in alienacoes.OrderBy(a => a.Data).ThenBy(a => a.Ativo, StringComparer.Ordinal))
            rows.Add(new object?[]
            {
                a.Data.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture),
                Mes(a.Mes), a.Ativo, a.Quantidade,
                a.ValorAlienacao, a.Custo, a.Ganho
            });
        return new AbaXlsx("Operacoes_Ganho", rows);
    }

    // ── Rendimentos_Rewards: earn/staking/airdrop/interest, valorados em BRL na data. ──
    private static AbaXlsx RendimentosRewards(IReadOnlyList<RewardCriptoIrDto> rewards)
    {
        var rows = new List<IReadOnlyList<object?>>
        {
            new object?[] { "Data", "Mês", "Ativo", "Quantidade", "Valor (R$)" }
        };
        foreach (var r in rewards.OrderBy(r => r.Data).ThenBy(r => r.Ativo, StringComparer.Ordinal))
            rows.Add(new object?[]
            {
                r.Data.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture),
                Mes(r.Mes), r.Ativo, r.Quantidade, r.ValorBRL
            });
        return new AbaXlsx("Rendimentos_Rewards", rows);
    }

    // ── Resumo_Mensal: ganhos mensais B3 (DARF) + alienações de cripto agregadas por mês (flag IN 1888). ──
    private static AbaXlsx ResumoMensal(ApuracaoIrDto ap)
    {
        var rows = new List<IReadOnlyList<object?>>
        {
            new object?[] { "Ganho de capital B3 por mês (base da DARF)" },
            new object?[] { "Mês", "Natureza", "Total vendas (R$)", "Resultado (R$)", "Prejuízo compensado (R$)", "Base de cálculo (R$)", "Alíquota", "Imposto DARF (R$)", "Isento?" }
        };
        foreach (var g in ap.GanhosMensais.OrderBy(g => g.Mes).ThenBy(g => g.Natureza, StringComparer.Ordinal))
            rows.Add(new object?[]
            {
                Mes(g.Mes), g.Natureza, g.TotalVendas, g.Resultado, g.PrejuizoCompensado,
                g.BaseCalculo, g.Aliquota, g.Imposto, g.Isento ? "Sim" : "Não"
            });

        rows.Add(LinhaVazia);
        rows.Add(new object?[] { "Alienações de cripto por mês (exterior — Lei 14.754) + flag IN 1888" });
        rows.Add(new object?[] { "Mês", "Total alienações (R$)", "Passou de R$30k (IN 1888)?" });
        foreach (var m in ap.CriptoExterior.MesesIN1888.OrderBy(m => m.Mes))
            rows.Add(new object?[] { Mes(m.Mes), m.TotalAlienacoes, m.UltrapassaLimite ? "Sim" : "Não" });

        return new AbaXlsx("Resumo_Mensal", rows);
    }

    // ── Regras_Fontes: regras aplicadas + referências oficiais (texto estático). ──
    private static AbaXlsx RegrasFontes()
    {
        var rows = new List<IReadOnlyList<object?>>
        {
            new object?[] { "Regras aplicadas e fontes — reconfirme a cada ano-calendário" },
            LinhaVazia,
            new object?[] { "Tema", "Regra", "Fonte / referência" },
            new object?[] { "Ações (B3)", "Isenção se vendas no mês ≤ R$20.000; acima, 15% sobre o lucro.", "Lei 11.033/2004; IN RFB 1.585/2015" },
            new object?[] { "FII (B3)", "Ganho de capital sempre tributado a 20%; sem isenção de R$20k.", "Lei 8.668/1993; IN RFB 1.585/2015" },
            new object?[] { "ETF / BDR (B3)", "Ganho de capital tributado a 15% (sem a isenção de R$20k de ações).", "IN RFB 1.585/2015" },
            new object?[] { "Dividendos (ações)", "Isentos para pessoa física.", "Lei 9.249/1995, art. 10" },
            new object?[] { "Rendimento de FII", "Isento para PF (cotista pessoa física, condições da lei).", "Lei 11.033/2004, art. 3º" },
            new object?[] { "JCP", "Tributação exclusiva na fonte, IRRF 15%.", "Lei 9.249/1995, art. 9º" },
            new object?[] { "Cripto na Binance", "Aplicação financeira no exterior: ganho de capital ANUAL a 15%; rendimentos tributáveis.", "Lei 14.754/2023" },
            new object?[] { "Permuta cripto-cripto", "Toda alienação é tributável, inclusive permuta (convert, small assets, staking ETH→WBETH/SOL→BNSOL).", "Lei 14.754/2023; entendimento RFB" },
            new object?[] { "IN 1888/2019", "Obrigação de DECLARAR à RFB as operações do mês quando o total > R$30.000 (≠ imposto).", "IN RFB 1.888/2019" },
            new object?[] { "Conversão em reais", "Valores em moeda estrangeira pelo câmbio da data da operação/recebimento.", "Regra RFB de conversão cambial" },
            LinhaVazia,
            new object?[] { "Aviso", "A MP 1.303/2025 (17,5% fixo) foi rejeitada (Câmara 08/10/2025). Valem as regras anteriores. Apoio — não substitui contador." },
        };
        return new AbaXlsx("Regras_Fontes", rows);
    }

    private static string Mes(int mes) => mes >= 1 && mes <= 12 ? MesesPt[mes] : mes.ToString(CultureInfo.InvariantCulture);
}
