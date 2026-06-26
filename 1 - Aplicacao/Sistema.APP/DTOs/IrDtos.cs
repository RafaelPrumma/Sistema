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

// Um item de Bens e Direitos em 31/12 ao custo. Para cripto traz o código RFB do grupo 08
// (08-01 Bitcoin, 08-02 altcoins, 08-03 stablecoin, 08-99 outros tokens/criptoativos) e o
// custo no 31/12 do ano APURADO e do ANTERIOR (a ficha de B&D pede situação atual e anterior).
public record BemDireitoIrDto(
    string Ticker,
    string Classe,
    decimal Quantidade,
    decimal Custo,                  // custo de aquisição acumulado em 31/12 do ano apurado
    string Codigo = "",             // código RFB (grupo 08 p/ cripto; vazio p/ B3 — CNPJ vem de outra fonte)
    decimal CustoAnterior = 0m,     // custo de aquisição acumulado em 31/12 do ano ANTERIOR
    decimal QuantidadeAnterior = 0m); // quantidade detida em 31/12 do ano anterior

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
    decimal TotalRewards,                             // soma dos rewards do ano (rendimento tributável)
    // IN 1888/2019: obrigação de DECLARAR (≠ imposto) os meses cujo total de ALIENAÇÕES de cripto
    // ultrapassa R$ 30.000. Cada item traz o mês, o total alienado e a flag de superação.
    IReadOnlyList<MesIN1888Dto> MesesIN1888);

// Total de alienações de cripto em um mês, com a flag da IN 1888 (> R$ 30.000 obriga declarar).
public record MesIN1888Dto(
    int Mes,
    decimal TotalAlienacoes, // soma das alienações (venda + permuta) de cripto no mês, em BRL
    bool UltrapassaLimite);  // true quando > R$ 30.000 (limite IN 1888/2019)

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
