using Sistema.CORE.Entities;

namespace Sistema.INFRA.Data.Seeds;

public static class FuncionalidadeSeed
{
    public static IEnumerable<Funcionalidade> Get()
    {
        return new List<Funcionalidade>
        {
            new() { Nome = "Perfil", UsuarioInclusao = "seed" },
            new() { Nome = "Usuario", UsuarioInclusao = "seed" },
            new() { Nome = "Log", UsuarioInclusao = "seed" }
        };
    }
}
