namespace Sistema.APP.DTOs;

// Um ponto diário da curva de patrimônio: valor de mercado no fim do dia + fluxo externo líquido
// do dia (aporte positivo / resgate negativo). Base para TWR (sistema de cotas) e MWR (TIR).
public record PontoRentabilidadeDto(DateTime Data, decimal Valor, decimal FluxoLiquido);

// Comparação da carteira com um benchmark acumulado no mesmo período (CDI/Ibovespa/IPCA).
public record ComparativoBenchmarkDto(
    string Nome,
    decimal RetornoBenchmark,
    decimal ExcessoAbsoluto,   // carteira - benchmark (em pontos)
    decimal RetornoRelativo);  // (1+carteira)/(1+benchmark)-1 (ex.: % do CDI)

public record RentabilidadeDto(
    decimal Twr,            // time-weighted (sistema de cotas) — neutraliza aportes; compara com benchmark
    decimal TwrAnualizado,
    decimal Mwr,            // money-weighted (TIR anualizada) — experiência real do investidor
    int Dias,
    IReadOnlyList<ComparativoBenchmarkDto> Benchmarks);
