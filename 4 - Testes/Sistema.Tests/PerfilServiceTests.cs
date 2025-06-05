using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Sistema.CORE.Entities;
using Sistema.CORE.Services;
using Sistema.INFRA;
using Sistema.INFRA.Data;
using Sistema.INFRA.Repositories;
using Xunit;

namespace Sistema.Tests;

public class PerfilServiceTests
{
    private static PerfilService CreateService(AppDbContext context)
    {
        var uow = new UnitOfWork(
            context,
            new PerfilRepository(context),
            new UsuarioRepository(context),
            new LogRepository(context),
            new FuncionalidadeRepository(context),
            new PerfilFuncionalidadeRepository(context));
        return new PerfilService(uow);
    }

    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    [Fact]
    public async Task AddAsync_DuplicateName_ReturnsError()
    {
        using var context = CreateContext();
        var service = CreateService(context);
        context.Perfis.Add(new Perfil { Nome = "Admin", UsuarioInclusao = "test" });
        await context.SaveChangesAsync();

        var result = await service.AddAsync(new Perfil { Nome = "Admin", UsuarioInclusao = "test" });

        Assert.False(result.Success);
        Assert.Equal("Perfil já existe", result.Message);
    }

    [Fact]
    public async Task AddAsync_UniqueName_ReturnsSuccess()
    {
        using var context = CreateContext();
        var service = CreateService(context);

        var result = await service.AddAsync(new Perfil { Nome = "Novo", UsuarioInclusao = "test" });

        Assert.True(result.Success);
        Assert.NotNull(result.Data);
    }
}
