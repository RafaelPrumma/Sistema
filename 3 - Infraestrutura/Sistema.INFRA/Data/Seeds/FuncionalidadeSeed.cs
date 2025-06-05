using Sistema.CORE.Entities;

namespace Sistema.INFRA.Data.Seeds;

public static class FuncionalidadeSeed
{
    public static IEnumerable<Funcionalidade> Get()
    {
        return new List<Funcionalidade>
        {
            new() { Id = 1, Nome = "Perfil", UsuarioInclusao = "seed" },
            new() { Id = 2, Nome = "Usuario", UsuarioInclusao = "seed" },
            new() { Id = 3, Nome = "Log", UsuarioInclusao = "seed" }
        };
    }
}
