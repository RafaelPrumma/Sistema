namespace Sistema.CORE.Entities;

public class Layout : AuditableEntity
{
    public int Id { get; set; }
    public int UsuarioId { get; set; }
    public bool ModoEscuro { get; set; }
    public string CorPrimaria { get; set; } = "azul";
    public Usuario? Usuario { get; set; }
}

