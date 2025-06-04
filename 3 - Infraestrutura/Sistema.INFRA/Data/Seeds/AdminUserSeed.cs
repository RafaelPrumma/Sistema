using Sistema.CORE.Entities;

namespace Sistema.INFRA.Data.Seeds;

public static class AdminUserSeed
{
    public static Usuario Get() => new()
    {
        Id = 1,
        Nome = "Rafael",
        Cpf = "00000000000",
        PerfilId = 1,
        UsuarioInclusao = "seed"
    };
}
