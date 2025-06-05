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
    public string Usuario { get; set; } = string.Empty;
    public string? Detalhe { get; set; }
}
