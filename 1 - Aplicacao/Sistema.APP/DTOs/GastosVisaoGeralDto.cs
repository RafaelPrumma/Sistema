namespace Sistema.APP.DTOs;

/// <summary>
/// Visão geral do submódulo Gastos (G1) — números do mês corrente + total geral. À prova de
/// falha: em qualquer erro o serviço devolve um DTO zerado/marcado como indisponível, nunca estoura.
/// </summary>
public sealed class GastosVisaoGeralDto
{
    public int TotalLancamentos { get; init; }
    public int LancamentosNoMes { get; init; }
    public decimal ReceitaDoMes { get; init; }
    public decimal DespesaDoMes { get; init; }
    public decimal SaldoDoMes => ReceitaDoMes - DespesaDoMes;
    public decimal AporteDoMes { get; init; }
    public int Ano { get; init; }
    public int Mes { get; init; }
    /// <summary>false quando o módulo degradou (erro/sem banco) — a view mostra o estado vazio.</summary>
    public bool Disponivel { get; init; }
}
