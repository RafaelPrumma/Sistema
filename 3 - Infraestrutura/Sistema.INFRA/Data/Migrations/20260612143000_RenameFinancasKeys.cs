using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sistema.INFRA.Data.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260612143000_RenameFinancasKeys")]
    public partial class RenameFinancasKeys : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                UPDATE [Funcionalidade]
                SET [Nome] = N'Financas'
                WHERE [Nome] = N'MinhasFinancas';
                """);

            migrationBuilder.Sql("""
                UPDATE [Configuracao]
                SET [Agrupamento] = N'Financas'
                WHERE [Agrupamento] = N'MinhasFinancas';
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                UPDATE [Configuracao]
                SET [Agrupamento] = N'MinhasFinancas'
                WHERE [Agrupamento] = N'Financas';
                """);

            migrationBuilder.Sql("""
                UPDATE [Funcionalidade]
                SET [Nome] = N'MinhasFinancas'
                WHERE [Nome] = N'Financas';
                """);
        }
    }
}
