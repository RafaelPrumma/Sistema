using Sistema.INFRA.Data.Seeds;
using Sistema.CORE.Entities;
using Sistema.CORE.Enums;
using System.Linq;

namespace Sistema.INFRA.Data;

public static class DbInitializer
{
    public static void Seed(AppDbContext context)
    {
        if (!context.Perfis.Any())
        {
            context.Perfis.AddRange(AdminSeed.Get(), UserSeed.Get());
            context.SaveChanges();
        }

        SeedFuncionalidades(context);
        SeedPerfilFuncionalidades(context);

        if (!context.Usuarios.Any())
        {
            context.Usuarios.AddRange(AdminUserSeed.Get(), ComercialUserSeed.Get());
            context.SaveChanges();
        }

        if (!context.Configuracoes.Any())
        {
            context.Configuracoes.AddRange(ConfiguracaoSeed.Get());
            context.SaveChanges();
        }
    }

    private static void SeedFuncionalidades(AppDbContext context)
    {
        var existentes = context.Funcionalidades.Select(f => f.Nome).ToHashSet();
        var novas = FuncionalidadeSeed.Get().Where(f => !existentes.Contains(f.Nome)).ToList();
        if (novas.Count == 0)
            return;

        context.Funcionalidades.AddRange(novas);
        context.SaveChanges();
    }

    private static void SeedPerfilFuncionalidades(AppDbContext context)
    {
        if (!context.Perfis.Any() || !context.Funcionalidades.Any())
            return;

        if (!context.PerfilFuncionalidades.Any())
        {
            context.PerfilFuncionalidades.AddRange(PerfilFuncionalidadeSeed.Get());
            context.SaveChanges();
        }

        var admin = context.Perfis.FirstOrDefault(p => p.Nome == "Admin");
        var comercial = context.Perfis.FirstOrDefault(p => p.Nome == "Comercial");
        var minhasFinancas = context.Funcionalidades.FirstOrDefault(f => f.Nome == "MinhasFinancas");
        if (minhasFinancas is null)
            return;

        AddPerfilFuncionalidadeSeAusente(context, admin, minhasFinancas, Permissao.Administrar);
        AddPerfilFuncionalidadeSeAusente(context, comercial, minhasFinancas, Permissao.Visualizar);
        context.SaveChanges();
    }

    private static void AddPerfilFuncionalidadeSeAusente(AppDbContext context, Perfil? perfil, Funcionalidade funcionalidade, Permissao permissoes)
    {
        if (perfil is null)
            return;

        var existe = context.PerfilFuncionalidades.Any(pf => pf.PerfilId == perfil.Id && pf.FuncionalidadeId == funcionalidade.Id);
        if (existe)
            return;

        context.PerfilFuncionalidades.Add(new PerfilFuncionalidade
        {
            PerfilId = perfil.Id,
            FuncionalidadeId = funcionalidade.Id,
            Permissoes = permissoes
        });
    }
}
