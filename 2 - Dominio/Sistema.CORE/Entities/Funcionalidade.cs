namespace Sistema.CORE.Entities;

public class Funcionalidade : AuditableEntity
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public bool Ativo { get; set; } = true;
    public ICollection<PerfilFuncionalidade> Perfis { get; set; } = new List<PerfilFuncionalidade>();
}
