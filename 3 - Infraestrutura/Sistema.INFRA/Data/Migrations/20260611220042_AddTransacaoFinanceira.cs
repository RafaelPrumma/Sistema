using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sistema.INFRA.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddTransacaoFinanceira : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FinanceiroTransacao",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Origem = table.Column<int>(type: "int", nullable: false),
                    AssetId = table.Column<int>(type: "int", nullable: false),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    OperationType = table.Column<int>(type: "int", nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(28,12)", precision: 28, scale: 12, nullable: false),
                    UnitPrice = table.Column<decimal>(type: "decimal(28,12)", precision: 28, scale: 12, nullable: false),
                    GrossAmount = table.Column<decimal>(type: "decimal(28,12)", precision: 28, scale: 12, nullable: false),
                    Fees = table.Column<decimal>(type: "decimal(28,12)", precision: 28, scale: 12, nullable: false),
                    Currency = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Broker = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Observacao = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    SourceDocumentId = table.Column<int>(type: "int", nullable: true),
                    CargaFinanceiraId = table.Column<int>(type: "int", nullable: true),
                    StagingTipo = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    StagingId = table.Column<int>(type: "int", nullable: true),
                    DuplicateGroupKey = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: true),
                    IsCanonical = table.Column<bool>(type: "bit", nullable: false),
                    ConfidenceLevel = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_FinanceiroTransacao", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FinanceiroTransacao_FinanceiroAtivo_AssetId",
                        column: x => x.AssetId,
                        principalTable: "FinanceiroAtivo",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FinanceiroTransacao_FinanceiroCarga_CargaFinanceiraId",
                        column: x => x.CargaFinanceiraId,
                        principalTable: "FinanceiroCarga",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_FinanceiroTransacao_FinanceiroDocumento_SourceDocumentId",
                        column: x => x.SourceDocumentId,
                        principalTable: "FinanceiroDocumento",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_FinanceiroTransacao_AssetId_Date",
                table: "FinanceiroTransacao",
                columns: new[] { "AssetId", "Date" });

            migrationBuilder.CreateIndex(
                name: "IX_FinanceiroTransacao_CargaFinanceiraId",
                table: "FinanceiroTransacao",
                column: "CargaFinanceiraId");

            migrationBuilder.CreateIndex(
                name: "IX_FinanceiroTransacao_DuplicateGroupKey",
                table: "FinanceiroTransacao",
                column: "DuplicateGroupKey");

            migrationBuilder.CreateIndex(
                name: "IX_FinanceiroTransacao_Origem",
                table: "FinanceiroTransacao",
                column: "Origem");

            migrationBuilder.CreateIndex(
                name: "IX_FinanceiroTransacao_SourceDocumentId",
                table: "FinanceiroTransacao",
                column: "SourceDocumentId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FinanceiroTransacao");
        }
    }
}
