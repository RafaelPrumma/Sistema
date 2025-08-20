using Sistema.INFRA.Data.Seeds;
using System.Linq;

namespace Sistema.INFRA.Data;

public static class DbInitializer
{
    public static void Seed(AppDbContext context)
    {
        context.Database.EnsureCreated();

        if (!context.Perfis.Any())
        {
            context.Perfis.AddRange(AdminSeed.Get(), UserSeed.Get());
            context.SaveChanges();
        }

        if (!context.Funcionalidades.Any())
        {
            context.Funcionalidades.AddRange(FuncionalidadeSeed.Get());
            context.SaveChanges();
        }

        if (!context.PerfilFuncionalidades.Any())
        {
            context.PerfilFuncionalidades.AddRange(PerfilFuncionalidadeSeed.Get());
            context.SaveChanges();
        }

        if (!context.Usuarios.Any())
        {
            context.Usuarios.AddRange(AdminUserSeed.Get(), ComercialUserSeed.Get());
            context.SaveChanges();
        }
    }
}
