namespace Sistema.APP.DTOs;

// Resultado da apuração de IR de um ano-calendário (apoio/"cola" — não substitui contador).
public record ApuracaoIrDto(
    int Ano,
    IReadOnlyList<ApuracaoMensalIrDto> GanhosMensais,   // ganho de capital B3 por mês/natureza (DARF)
    IReadOnlyList<BemDireitoIrDto> BensEDireitos,        // posição em 31/12 ao custo
    IReadOnlyList<RendimentoIrDto> RendimentosIsentos,   // dividendos + rendimento de FII
    IReadOnlyList<RendimentoIrDto> TributacaoExclusiva,  // JCP (IRRF na fonte)
    decimal TotalImpostoDevido,                          // soma do imposto (DARF) B3 do ano
    // Cripto = aplicação no exterior (Lei 14.754/2023): NÃO usa a isenção nacional de R$35k/mês.
    CriptoExteriorIrDto CriptoExterior);                 // ganho de capital exterior + rewards de cripto

// Uma linha de apuração mensal de ganho de capital, por natureza (Ações/FII/ETF/BDR/Cripto).
public record ApuracaoMensalIrDto(
    int Ano,
    int Mes,
    string Natureza,
    decimal TotalVendas,        // volume de alienações no mês (base da isenção)
    decimal Resultado,          // lucro/prejuízo bruto do mês
    decimal PrejuizoCompensado, // prejuízo de meses anteriores usado neste mês
    decimal BaseCalculo,        // resultado - prejuízo compensado
    decimal Aliquota,           // alíquota aplicada (0 quando isento)
    decimal Imposto,            // imposto devido (DARF) do mês
    bool Isento);

public record BemDireitoIrDto(string Ticker, string Classe, decimal Quantidade, decimal Custo);

public record RendimentoIrDto(string Tipo, decimal Valor);

// Apuração de cripto como aplicação financeira no EXTERIOR (Lei 14.754/2023): o ganho de capital
// é ANUAL (não mensal), 15% sobre o ganho líquido do ano; NÃO entra na isenção nacional de R$35k/mês.
// Rewards/earn/airdrop/staking-reward = rendimento tributável (valor BRL na data do recebimento).
public record CriptoExteriorIrDto(
    IReadOnlyList<AlienacaoCriptoIrDto> Alienacoes,  // uma linha por venda/permuta-saída de cripto
    IReadOnlyList<RewardCriptoIrDto> Rewards,        // earn/staking/airdrop valorados em BRL
    decimal GanhoCapitalLiquido,                      // soma de ganhos/perdas das alienações no ano
    decimal Aliquota,                                 // 15% (Lei 14.754/2023)
    decimal ImpostoGanhoCapital,                      // imposto sobre o ganho líquido (0 se perda)
    decimal TotalRewards);                            // soma dos rewards do ano (rendimento tributável)

// Uma alienação de cripto (venda por fiat OU permuta cripto-cripto), valorada em BRL.
public record AlienacaoCriptoIrDto(
    int Mes,
    DateTime Data,
    string Ativo,
    decimal Quantidade,
    decimal ValorAlienacao, // valor de alienação em BRL (preço de mercado na data × quantidade)
    decimal Custo,          // custo de aquisição em BRL (custo médio corrente × quantidade)
    decimal Ganho);         // ganho/perda = valor de alienação − custo

// Um rendimento de cripto (earn/staking reward/airdrop/interest), valorado em BRL na data.
public record RewardCriptoIrDto(
    int Mes,
    DateTime Data,
    string Ativo,
    decimal Quantidade,
    decimal ValorBRL);
