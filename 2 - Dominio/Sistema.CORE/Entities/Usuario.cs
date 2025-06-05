namespace Sistema.CORE.Entities;

public class Usuario : AuditableEntity
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Cpf { get; set; } = string.Empty;
    public int PerfilId { get; set; }
    public Perfil? Perfil { get; set; }
}
