using Microsoft.EntityFrameworkCore;
using Sistema.CORE.Entities;

namespace Sistema.INFRA.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Perfil> Perfis => Set<Perfil>();
}
