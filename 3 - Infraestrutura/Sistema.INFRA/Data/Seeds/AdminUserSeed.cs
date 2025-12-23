 
using Microsoft.AspNetCore.Identity; 
using Sistema.CORE.Entities;

namespace Sistema.INFRA.Data.Seeds;

public static class AdminUserSeed
{ 
    public static Usuario Get()
    {
        var user = new Usuario
        {
            Nome = "Rafael",
            Cpf = "00000000000",
            Email = "admin@sistema.test",
            PerfilId = 1,
            UsuarioInclusao = "seed",
            Ativo = true
        };
        var hasher = new PasswordHasher<Usuario>();
        user.SenhaHash = hasher.HashPassword(user, "admin123");
        return user;
    } 
}
