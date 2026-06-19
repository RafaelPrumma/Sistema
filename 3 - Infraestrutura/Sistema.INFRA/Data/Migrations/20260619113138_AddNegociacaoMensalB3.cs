using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sistema.INFRA.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddNegociacaoMensalB3 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FinanceiroNegociacaoMensalB3",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CargaFinanceiraId = table.Column<int>(type: "int", nullable: false),
                    SourceDocumentId = table.Column<int>(type: "int", nullable: true),
                    AssetId = table.Column<int>(type: "int", nullable: false),
                    AnoMes = table.Column<int>(type: "int", nullable: false),
                    OperationType = table.Column<int>(type: "int", nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(28,12)", precision: 28, scale: 12, nullable: false),
                    UnitPrice = table.Column<decimal>(type: "decimal(28,12)", precision: 28, scale: 12, nullable: false),
                    GrossAmount = table.Column<decimal>(type: "decimal(28,12)", precision: 28, scale: 12, nullable: false),
                    PeriodoInicial = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PeriodoFinal = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Broker = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    ChaveNatural = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
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
                    table.PrimaryKey("PK_FinanceiroNegociacaoMensalB3", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FinanceiroNegociacaoMensalB3_FinanceiroAtivo_AssetId",
                        column: x => x.AssetId,
                        principalTable: "FinanceiroAtivo",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FinanceiroNegociacaoMensalB3_FinanceiroCarga_CargaFinanceiraId",
                        column: x => x.CargaFinanceiraId,
                        principalTable: "FinanceiroCarga",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FinanceiroNegociacaoMensalB3_FinanceiroDocumento_SourceDocumentId",
                        column: x => x.SourceDocumentId,
                        principalTable: "FinanceiroDocumento",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_FinanceiroNegociacaoMensalB3_AssetId_AnoMes",
                table: "FinanceiroNegociacaoMensalB3",
                columns: new[] { "AssetId", "AnoMes" });

            migrationBuilder.CreateIndex(
                name: "IX_FinanceiroNegociacaoMensalB3_CargaFinanceiraId",
                table: "FinanceiroNegociacaoMensalB3",
                column: "CargaFinanceiraId");

            migrationBuilder.CreateIndex(
                name: "IX_FinanceiroNegociacaoMensalB3_ChaveNatural",
                table: "FinanceiroNegociacaoMensalB3",
                column: "ChaveNatural",
                unique: true,
                filter: "[ChaveNatural] IS NOT NULL AND [DataExclusao] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_FinanceiroNegociacaoMensalB3_SourceDocumentId",
                table: "FinanceiroNegociacaoMensalB3",
                column: "SourceDocumentId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FinanceiroNegociacaoMensalB3");
        }
    }
}
