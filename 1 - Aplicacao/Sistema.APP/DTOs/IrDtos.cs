namespace Sistema.APP.DTOs;

// Resultado da apuração de IR de um ano-calendário (apoio/"cola" — não substitui contador).
public record ApuracaoIrDto(
    int Ano,
    IReadOnlyList<ApuracaoMensalIrDto> GanhosMensais,   // ganho de capital por mês/natureza (B3 + cripto)
    IReadOnlyList<BemDireitoIrDto> BensEDireitos,        // posição em 31/12 ao custo
    IReadOnlyList<RendimentoIrDto> RendimentosIsentos,   // dividendos + rendimento de FII
    IReadOnlyList<RendimentoIrDto> TributacaoExclusiva,  // JCP (IRRF na fonte)
    decimal TotalImpostoDevido);                          // soma do imposto (DARF) do ano

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
