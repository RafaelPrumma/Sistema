using Sistema.CORE.Entities;

namespace Sistema.INFRA.Data.Seeds;

public static class ComercialUserSeed
{
    public static Usuario Get() => new()
    {
        Id = 2,
        Nome = "Allison",
        Cpf = "00000000001",
        PerfilId = 2,
        UsuarioInclusao = "seed"
    };
}
