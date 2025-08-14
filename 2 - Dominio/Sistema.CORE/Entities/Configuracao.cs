namespace Sistema.CORE.Entities;

public class Configuracao : AuditableEntity
{
    public int Id { get; set; }
    public string Agrupamento { get; set; } = string.Empty;
    public string Chave { get; set; } = string.Empty;
    public string Valor { get; set; } = string.Empty;
    public ConfiguracaoTipo Tipo { get; set; }
    public string? Descricao { get; set; }
    public bool Ativo { get; set; } = true;
}
