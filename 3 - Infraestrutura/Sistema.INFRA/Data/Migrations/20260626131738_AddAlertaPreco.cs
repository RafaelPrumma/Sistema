using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sistema.INFRA.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAlertaPreco : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FinanceiroAlertaPreco",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AtivoFinanceiroId = table.Column<int>(type: "int", nullable: false),
                    Limiar = table.Column<decimal>(type: "decimal(28,12)", precision: 28, scale: 12, nullable: false),
                    Direcao = table.Column<int>(type: "int", nullable: false),
                    Ativo = table.Column<bool>(type: "bit", nullable: false),
                    Observacao = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
                    DispararadoEm = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UltimoPreco = table.Column<decimal>(type: "decimal(28,12)", precision: 28, scale: 12, nullable: true),
                    DataInclusao = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DataAlteracao = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UsuarioInclusao = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UsuarioAlteracao = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DataExclusao = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UsuarioExclusao = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinanceiroAlertaPreco", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FinanceiroAlertaPreco_FinanceiroAtivo_AtivoFinanceiroId",
                        column: x => x.AtivoFinanceiroId,
                        principalTable: "FinanceiroAtivo",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FinanceiroAlertaPreco_AtivoFinanceiroId_Ativo",
                table: "FinanceiroAlertaPreco",
                columns: new[] { "AtivoFinanceiroId", "Ativo" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FinanceiroAlertaPreco");
        }
    }
}
