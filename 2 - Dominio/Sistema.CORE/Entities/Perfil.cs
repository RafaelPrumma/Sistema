namespace Sistema.CORE.Entities;

public class Perfil : AuditableEntity
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public bool Ativo { get; set; } = true;
}
