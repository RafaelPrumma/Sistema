namespace Sistema.CORE.Entities;

public class PerfilFuncionalidade
{
    public int PerfilId { get; set; }
    public Perfil Perfil { get; set; } = null!;
    public int FuncionalidadeId { get; set; }
    public Funcionalidade Funcionalidade { get; set; } = null!;
    public bool PodeLer { get; set; }
    public bool PodeEscrever { get; set; }
}
