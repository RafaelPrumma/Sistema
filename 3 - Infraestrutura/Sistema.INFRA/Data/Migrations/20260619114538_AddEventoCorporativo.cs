using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sistema.INFRA.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddEventoCorporativo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FinanceiroEventoCorporativo",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AtivoFinanceiroId = table.Column<int>(type: "int", nullable: false),
                    Tipo = table.Column<int>(type: "int", nullable: false),
                    Data = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Fator = table.Column<decimal>(type: "decimal(18,8)", precision: 18, scale: 8, nullable: false),
                    Fonte = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    ChaveNatural = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    DataInclusao = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DataAlteracao = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UsuarioInclusao = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UsuarioAlteracao = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DataExclusao = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UsuarioExclusao = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinanceiroEventoCorporativo", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FinanceiroEventoCorporativo_FinanceiroAtivo_AtivoFinanceiroId",
                        column: x => x.AtivoFinanceiroId,
                        principalTable: "FinanceiroAtivo",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FinanceiroEventoCorporativo_AtivoFinanceiroId_Data",
                table: "FinanceiroEventoCorporativo",
                columns: new[] { "AtivoFinanceiroId", "Data" });

            migrationBuilder.CreateIndex(
                name: "IX_FinanceiroEventoCorporativo_ChaveNatural",
                table: "FinanceiroEventoCorporativo",
                column: "ChaveNatural",
                unique: true,
                filter: "[ChaveNatural] IS NOT NULL AND [DataExclusao] IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FinanceiroEventoCorporativo");
        }
    }
}
