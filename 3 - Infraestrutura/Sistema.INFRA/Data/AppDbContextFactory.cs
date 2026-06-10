using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Sistema.APP.Services.Interfaces;

namespace Sistema.INFRA.Data;

public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        optionsBuilder.UseSqlServer("Data Source=localhost; Initial Catalog=SISTEMA; Trusted_Connection=True; Encrypt=False; TrustServerCertificate=True;");
        return new AppDbContext(optionsBuilder.Options, new DesignTimeExecutionContext());
    }

    private sealed class DesignTimeExecutionContext : IExecutionContext
    {
        public string? Usuario => "design-time";
        public string? CorrelationId => null;
    }
}
