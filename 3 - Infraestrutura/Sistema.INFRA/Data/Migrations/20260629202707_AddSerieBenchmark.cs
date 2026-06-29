using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sistema.INFRA.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSerieBenchmark : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FinanceiroSerieBenchmark",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Indice = table.Column<int>(type: "int", nullable: false),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Valor = table.Column<decimal>(type: "decimal(28,12)", precision: 28, scale: 12, nullable: false),
                    Fonte = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    ChaveNatural = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: true),
                    RawJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DataInclusao = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DataAlteracao = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UsuarioInclusao = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UsuarioAlteracao = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DataExclusao = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UsuarioExclusao = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinanceiroSerieBenchmark", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FinanceiroSerieBenchmark_ChaveNatural",
                table: "FinanceiroSerieBenchmark",
                column: "ChaveNatural",
                unique: true,
                filter: "[ChaveNatural] IS NOT NULL AND [DataExclusao] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_FinanceiroSerieBenchmark_Indice_Date",
                table: "FinanceiroSerieBenchmark",
                columns: new[] { "Indice", "Date" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FinanceiroSerieBenchmark");
        }
    }
}
