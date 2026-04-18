namespace Sistema.CORE.Entities;

public class Log
{
    public int Id { get; set; }
    public DateTime DataOperacao { get; set; }
    public string Entidade { get; set; } = string.Empty;
    public string Operacao { get; set; } = string.Empty;
    public bool Sucesso { get; set; }
    public string Mensagem { get; set; } = string.Empty;
    public LogTipo Tipo { get; set; }
    public LogModulo Modulo { get; set; } = LogModulo.Geral;
    public string Usuario { get; set; } = string.Empty;
    public string? CorrelationId { get; set; }
    public string? TraceId { get; set; }
    public string? SpanId { get; set; }
    public string? Detalhe { get; set; }
}
