using Sistema.CORE.Entities;

namespace Sistema.INFRA.Data.Seeds;

public static class AdminSeed
{
    public static Perfil Get() => new() { Id = 1, Nome = "Admin" };
}
