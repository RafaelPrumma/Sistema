using Sistema.CORE.Entities;

namespace Sistema.INFRA.Data.Seeds;

public static class UserSeed
{
    public static Perfil Get() => new() { Id = 2, Nome = "Usuario" };
}
