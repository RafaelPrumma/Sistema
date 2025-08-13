namespace Sistema.CORE.Entities;

public class Tema : AuditableEntity
{
    public int Id { get; set; }
    public int UsuarioId { get; set; }
    public bool ModoEscuro { get; set; }
    public string CorHeader { get; set; } = "#0d6efd";
    public string CorBarraEsquerda { get; set; } = "#0d6efd";
    public string CorBarraDireita { get; set; } = "#f8f9fa";
    public string CorFooter { get; set; } = "#0d6efd";
    public Usuario? Usuario { get; set; }
}

