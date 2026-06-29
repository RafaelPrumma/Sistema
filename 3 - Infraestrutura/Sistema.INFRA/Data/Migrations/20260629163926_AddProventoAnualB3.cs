using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sistema.INFRA.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddProventoAnualB3 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FinanceiroProventoAnualB3",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CargaFinanceiraId = table.Column<int>(type: "int", nullable: true),
                    SourceDocumentId = table.Column<int>(type: "int", nullable: true),
                    AssetId = table.Column<int>(type: "int", nullable: false),
                    Year = table.Column<int>(type: "int", nullable: false),
                    Tipo = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    ValorLiquido = table.Column<decimal>(type: "decimal(24,8)", precision: 24, scale: 8, nullable: false),
                    ChaveNatural = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
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
                    table.PrimaryKey("PK_FinanceiroProventoAnualB3", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FinanceiroProventoAnualB3_FinanceiroAtivo_AssetId",
                        column: x => x.AssetId,
                        principalTable: "FinanceiroAtivo",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FinanceiroProventoAnualB3_FinanceiroCarga_CargaFinanceiraId",
                        column: x => x.CargaFinanceiraId,
                        principalTable: "FinanceiroCarga",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_FinanceiroProventoAnualB3_FinanceiroDocumento_SourceDocumentId",
                        column: x => x.SourceDocumentId,
                        principalTable: "FinanceiroDocumento",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_FinanceiroProventoAnualB3_AssetId",
                table: "FinanceiroProventoAnualB3",
                column: "AssetId");

            migrationBuilder.CreateIndex(
                name: "IX_FinanceiroProventoAnualB3_CargaFinanceiraId",
                table: "FinanceiroProventoAnualB3",
                column: "CargaFinanceiraId");

            migrationBuilder.CreateIndex(
                name: "IX_FinanceiroProventoAnualB3_ChaveNatural",
                table: "FinanceiroProventoAnualB3",
                column: "ChaveNatural",
                unique: true,
                filter: "[ChaveNatural] IS NOT NULL AND [DataExclusao] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_FinanceiroProventoAnualB3_SourceDocumentId",
                table: "FinanceiroProventoAnualB3",
                column: "SourceDocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_FinanceiroProventoAnualB3_Year_AssetId",
                table: "FinanceiroProventoAnualB3",
                columns: new[] { "Year", "AssetId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FinanceiroProventoAnualB3");
        }
    }
}
