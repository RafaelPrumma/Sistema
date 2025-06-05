namespace Sistema.CORE.Entities;

public abstract class AuditableEntity
{
    public DateTime DataInclusao { get; set; }
    public DateTime? DataAlteracao { get; set; }
    public string UsuarioInclusao { get; set; } = string.Empty;
    public string? UsuarioAlteracao { get; set; }
}
