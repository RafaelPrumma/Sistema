namespace Sistema.CORE.Entities;

public abstract class AuditableEntity
{
    public DateTime DataInclusao { get; set; }
    public DateTime? DataAlteracao { get; set; }
}
