using Microsoft.AspNetCore.Identity;
using Sistema.CORE.Entities;

namespace Sistema.INFRA.Data.Seeds;

public static class ComercialUserSeed
{
    public static Usuario Get()
    {
        var user = new Usuario
        {
            Id = 2,
            Nome = "Allison",
            Cpf = "00000000001",
            PerfilId = 2,
            UsuarioInclusao = "seed",
            Ativo = true
        };
        var hasher = new PasswordHasher<Usuario>();
        user.SenhaHash = hasher.HashPassword(user, "comercial123");
        return user;
    }
}
