using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sistema.INFRA.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddGastos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "GastoCategoria",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nome = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Tipo = table.Column<int>(type: "int", nullable: false),
                    Icone = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: true),
                    Cor = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    CategoriaPaiId = table.Column<int>(type: "int", nullable: true),
                    Ativo = table.Column<bool>(type: "bit", nullable: false),
                    ChaveNatural = table.Column<string>(type: "nvarchar(140)", maxLength: 140, nullable: true),
                    DataInclusao = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DataAlteracao = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UsuarioInclusao = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UsuarioAlteracao = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DataExclusao = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UsuarioExclusao = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GastoCategoria", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GastoCategoria_GastoCategoria_CategoriaPaiId",
                        column: x => x.CategoriaPaiId,
                        principalTable: "GastoCategoria",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "GastoLancamento",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Data = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Descricao = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false),
                    Valor = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Tipo = table.Column<int>(type: "int", nullable: false),
                    CategoriaId = table.Column<int>(type: "int", nullable: true),
                    Fonte = table.Column<int>(type: "int", nullable: false),
                    Estabelecimento = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ParcelaAtual = table.Column<int>(type: "int", nullable: true),
                    ParcelaTotal = table.Column<int>(type: "int", nullable: true),
                    SourceDocumentId = table.Column<int>(type: "int", nullable: true),
                    ChaveNatural = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
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
                    table.PrimaryKey("PK_GastoLancamento", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GastoLancamento_FinanceiroDocumento_SourceDocumentId",
                        column: x => x.SourceDocumentId,
                        principalTable: "FinanceiroDocumento",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_GastoLancamento_GastoCategoria_CategoriaId",
                        column: x => x.CategoriaId,
                        principalTable: "GastoCategoria",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "GastoRegraCategorizacao",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Padrao = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    TipoMatch = table.Column<int>(type: "int", nullable: false),
                    CategoriaId = table.Column<int>(type: "int", nullable: false),
                    Prioridade = table.Column<int>(type: "int", nullable: false),
                    Origem = table.Column<int>(type: "int", nullable: false),
                    Ativo = table.Column<bool>(type: "bit", nullable: false),
                    ChaveNatural = table.Column<string>(type: "nvarchar(220)", maxLength: 220, nullable: true),
                    DataInclusao = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DataAlteracao = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UsuarioInclusao = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UsuarioAlteracao = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DataExclusao = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UsuarioExclusao = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GastoRegraCategorizacao", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GastoRegraCategorizacao_GastoCategoria_CategoriaId",
                        column: x => x.CategoriaId,
                        principalTable: "GastoCategoria",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GastoCategoria_CategoriaPaiId",
                table: "GastoCategoria",
                column: "CategoriaPaiId");

            migrationBuilder.CreateIndex(
                name: "IX_GastoCategoria_ChaveNatural",
                table: "GastoCategoria",
                column: "ChaveNatural",
                unique: true,
                filter: "[ChaveNatural] IS NOT NULL AND [DataExclusao] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_GastoLancamento_CategoriaId",
                table: "GastoLancamento",
                column: "CategoriaId");

            migrationBuilder.CreateIndex(
                name: "IX_GastoLancamento_ChaveNatural",
                table: "GastoLancamento",
                column: "ChaveNatural",
                unique: true,
                filter: "[ChaveNatural] IS NOT NULL AND [DataExclusao] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_GastoLancamento_Data_Fonte",
                table: "GastoLancamento",
                columns: new[] { "Data", "Fonte" });

            migrationBuilder.CreateIndex(
                name: "IX_GastoLancamento_SourceDocumentId",
                table: "GastoLancamento",
                column: "SourceDocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_GastoRegraCategorizacao_Ativo_Prioridade",
                table: "GastoRegraCategorizacao",
                columns: new[] { "Ativo", "Prioridade" });

            migrationBuilder.CreateIndex(
                name: "IX_GastoRegraCategorizacao_CategoriaId",
                table: "GastoRegraCategorizacao",
                column: "CategoriaId");

            migrationBuilder.CreateIndex(
                name: "IX_GastoRegraCategorizacao_ChaveNatural",
                table: "GastoRegraCategorizacao",
                column: "ChaveNatural",
                unique: true,
                filter: "[ChaveNatural] IS NOT NULL AND [DataExclusao] IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GastoLancamento");

            migrationBuilder.DropTable(
                name: "GastoRegraCategorizacao");

            migrationBuilder.DropTable(
                name: "GastoCategoria");
        }
    }
}
