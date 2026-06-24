using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sistema.INFRA.Data.Migrations
{
    /// <inheritdoc />
    public partial class PadronizaFinanceiroPosicaoAtivo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FinanceiroCarteira_FinanceiroCarteira_ParentId",
                table: "FinanceiroCarteira");

            migrationBuilder.DropForeignKey(
                name: "FK_FinanceiroPosicaoEstimativa_FinanceiroAtivo_AssetId",
                table: "FinanceiroPosicaoEstimativa");

            migrationBuilder.RenameColumn(
                name: "TotalSold",
                table: "FinanceiroPosicaoEstimativa",
                newName: "TotalVendido");

            migrationBuilder.RenameColumn(
                name: "TotalInvested",
                table: "FinanceiroPosicaoEstimativa",
                newName: "TotalInvestido");

            migrationBuilder.RenameColumn(
                name: "RealizedResult",
                table: "FinanceiroPosicaoEstimativa",
                newName: "ResultadoRealizado");

            migrationBuilder.RenameColumn(
                name: "Quantity",
                table: "FinanceiroPosicaoEstimativa",
                newName: "Quantidade");

            migrationBuilder.RenameColumn(
                name: "LastOperationDate",
                table: "FinanceiroPosicaoEstimativa",
                newName: "UltimaOperacaoEm");

            migrationBuilder.RenameColumn(
                name: "EstimatedCurrentPosition",
                table: "FinanceiroPosicaoEstimativa",
                newName: "PosicaoAtualEstimada");

            migrationBuilder.RenameColumn(
                name: "ConfidenceLevel",
                table: "FinanceiroPosicaoEstimativa",
                newName: "NivelConfianca");

            migrationBuilder.RenameColumn(
                name: "AveragePrice",
                table: "FinanceiroPosicaoEstimativa",
                newName: "PrecoMedio");

            migrationBuilder.RenameColumn(
                name: "AssetId",
                table: "FinanceiroPosicaoEstimativa",
                newName: "AtivoFinanceiroId");

            migrationBuilder.RenameIndex(
                name: "IX_FinanceiroPosicaoEstimativa_AssetId",
                table: "FinanceiroPosicaoEstimativa",
                newName: "IX_FinanceiroPosicaoEstimativa_AtivoFinanceiroId");

            migrationBuilder.RenameColumn(
                name: "Symbol",
                table: "FinanceiroCotacaoAtivo",
                newName: "Simbolo");

            migrationBuilder.RenameColumn(
                name: "RetrievedAt",
                table: "FinanceiroCotacaoAtivo",
                newName: "ConsultadoEm");

            migrationBuilder.RenameColumn(
                name: "PriceBRL",
                table: "FinanceiroCotacaoAtivo",
                newName: "PrecoBRL");

            migrationBuilder.RenameColumn(
                name: "Price",
                table: "FinanceiroCotacaoAtivo",
                newName: "Preco");

            migrationBuilder.RenameColumn(
                name: "MarketTime",
                table: "FinanceiroCotacaoAtivo",
                newName: "HorarioMercado");

            migrationBuilder.RenameColumn(
                name: "ExpiresAt",
                table: "FinanceiroCotacaoAtivo",
                newName: "ExpiraEm");

            migrationBuilder.RenameColumn(
                name: "ErrorMessage",
                table: "FinanceiroCotacaoAtivo",
                newName: "MensagemErro");

            migrationBuilder.RenameColumn(
                name: "Currency",
                table: "FinanceiroCotacaoAtivo",
                newName: "Moeda");

            migrationBuilder.RenameColumn(
                name: "ChangePercent",
                table: "FinanceiroCotacaoAtivo",
                newName: "VariacaoPercentual");

            migrationBuilder.RenameColumn(
                name: "Change",
                table: "FinanceiroCotacaoAtivo",
                newName: "Variacao");

            migrationBuilder.RenameIndex(
                name: "IX_FinanceiroCotacaoAtivo_RetrievedAt",
                table: "FinanceiroCotacaoAtivo",
                newName: "IX_FinanceiroCotacaoAtivo_ConsultadoEm");

            migrationBuilder.RenameColumn(
                name: "ParentId",
                table: "FinanceiroCarteira",
                newName: "CarteiraPaiId");

            migrationBuilder.RenameColumn(
                name: "IsSistema",
                table: "FinanceiroCarteira",
                newName: "EhSistema");

            migrationBuilder.RenameIndex(
                name: "IX_FinanceiroCarteira_ParentId",
                table: "FinanceiroCarteira",
                newName: "IX_FinanceiroCarteira_CarteiraPaiId");

            migrationBuilder.RenameColumn(
                name: "Ticker",
                table: "FinanceiroAtivo",
                newName: "Sigla");

            migrationBuilder.RenameColumn(
                name: "Name",
                table: "FinanceiroAtivo",
                newName: "Nome");

            migrationBuilder.RenameColumn(
                name: "Market",
                table: "FinanceiroAtivo",
                newName: "Mercado");

            migrationBuilder.RenameColumn(
                name: "IsCrypto",
                table: "FinanceiroAtivo",
                newName: "EhCripto");

            migrationBuilder.RenameColumn(
                name: "IsActive",
                table: "FinanceiroAtivo",
                newName: "Ativo");

            migrationBuilder.RenameColumn(
                name: "Currency",
                table: "FinanceiroAtivo",
                newName: "Moeda");

            migrationBuilder.RenameColumn(
                name: "ConceptRole",
                table: "FinanceiroAtivo",
                newName: "PapelConceitual");

            migrationBuilder.RenameColumn(
                name: "AssetKey",
                table: "FinanceiroAtivo",
                newName: "Chave");

            migrationBuilder.RenameColumn(
                name: "AssetClass",
                table: "FinanceiroAtivo",
                newName: "Classe");

            migrationBuilder.RenameIndex(
                name: "IX_FinanceiroAtivo_AssetKey",
                table: "FinanceiroAtivo",
                newName: "IX_FinanceiroAtivo_Chave");

            migrationBuilder.CreateTable(
                name: "FinanceiroPosicaoAtivo",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AtivoFinanceiroId = table.Column<int>(type: "int", nullable: false),
                    Quantidade = table.Column<decimal>(type: "decimal(28,12)", precision: 28, scale: 12, nullable: false),
                    PrecoMedio = table.Column<decimal>(type: "decimal(28,12)", precision: 28, scale: 12, nullable: false),
                    CustoTotal = table.Column<decimal>(type: "decimal(28,12)", precision: 28, scale: 12, nullable: false),
                    TotalComprado = table.Column<decimal>(type: "decimal(28,12)", precision: 28, scale: 12, nullable: false),
                    TotalVendido = table.Column<decimal>(type: "decimal(28,12)", precision: 28, scale: 12, nullable: false),
                    ResultadoRealizado = table.Column<decimal>(type: "decimal(28,12)", precision: 28, scale: 12, nullable: false),
                    UltimaOperacaoEm = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CalculadoEm = table.Column<DateTime>(type: "datetime2", nullable: false),
                    VersaoCalculo = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    DataInclusao = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DataAlteracao = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UsuarioInclusao = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UsuarioAlteracao = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DataExclusao = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UsuarioExclusao = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinanceiroPosicaoAtivo", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FinanceiroPosicaoAtivo_FinanceiroAtivo_AtivoFinanceiroId",
                        column: x => x.AtivoFinanceiroId,
                        principalTable: "FinanceiroAtivo",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FinanceiroPosicaoAtivo_AtivoFinanceiroId",
                table: "FinanceiroPosicaoAtivo",
                column: "AtivoFinanceiroId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FinanceiroPosicaoAtivo_Status",
                table: "FinanceiroPosicaoAtivo",
                column: "Status");

            migrationBuilder.AddForeignKey(
                name: "FK_FinanceiroCarteira_FinanceiroCarteira_CarteiraPaiId",
                table: "FinanceiroCarteira",
                column: "CarteiraPaiId",
                principalTable: "FinanceiroCarteira",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_FinanceiroPosicaoEstimativa_FinanceiroAtivo_AtivoFinanceiroId",
                table: "FinanceiroPosicaoEstimativa",
                column: "AtivoFinanceiroId",
                principalTable: "FinanceiroAtivo",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FinanceiroCarteira_FinanceiroCarteira_CarteiraPaiId",
                table: "FinanceiroCarteira");

            migrationBuilder.DropForeignKey(
                name: "FK_FinanceiroPosicaoEstimativa_FinanceiroAtivo_AtivoFinanceiroId",
                table: "FinanceiroPosicaoEstimativa");

            migrationBuilder.DropTable(
                name: "FinanceiroPosicaoAtivo");

            migrationBuilder.RenameColumn(
                name: "UltimaOperacaoEm",
                table: "FinanceiroPosicaoEstimativa",
                newName: "LastOperationDate");

            migrationBuilder.RenameColumn(
                name: "TotalVendido",
                table: "FinanceiroPosicaoEstimativa",
                newName: "TotalSold");

            migrationBuilder.RenameColumn(
                name: "TotalInvestido",
                table: "FinanceiroPosicaoEstimativa",
                newName: "TotalInvested");

            migrationBuilder.RenameColumn(
                name: "ResultadoRealizado",
                table: "FinanceiroPosicaoEstimativa",
                newName: "RealizedResult");

            migrationBuilder.RenameColumn(
                name: "Quantidade",
                table: "FinanceiroPosicaoEstimativa",
                newName: "Quantity");

            migrationBuilder.RenameColumn(
                name: "PrecoMedio",
                table: "FinanceiroPosicaoEstimativa",
                newName: "AveragePrice");

            migrationBuilder.RenameColumn(
                name: "PosicaoAtualEstimada",
                table: "FinanceiroPosicaoEstimativa",
                newName: "EstimatedCurrentPosition");

            migrationBuilder.RenameColumn(
                name: "NivelConfianca",
                table: "FinanceiroPosicaoEstimativa",
                newName: "ConfidenceLevel");

            migrationBuilder.RenameColumn(
                name: "AtivoFinanceiroId",
                table: "FinanceiroPosicaoEstimativa",
                newName: "AssetId");

            migrationBuilder.RenameIndex(
                name: "IX_FinanceiroPosicaoEstimativa_AtivoFinanceiroId",
                table: "FinanceiroPosicaoEstimativa",
                newName: "IX_FinanceiroPosicaoEstimativa_AssetId");

            migrationBuilder.RenameColumn(
                name: "VariacaoPercentual",
                table: "FinanceiroCotacaoAtivo",
                newName: "ChangePercent");

            migrationBuilder.RenameColumn(
                name: "Variacao",
                table: "FinanceiroCotacaoAtivo",
                newName: "Change");

            migrationBuilder.RenameColumn(
                name: "Simbolo",
                table: "FinanceiroCotacaoAtivo",
                newName: "Symbol");

            migrationBuilder.RenameColumn(
                name: "PrecoBRL",
                table: "FinanceiroCotacaoAtivo",
                newName: "PriceBRL");

            migrationBuilder.RenameColumn(
                name: "Preco",
                table: "FinanceiroCotacaoAtivo",
                newName: "Price");

            migrationBuilder.RenameColumn(
                name: "Moeda",
                table: "FinanceiroCotacaoAtivo",
                newName: "Currency");

            migrationBuilder.RenameColumn(
                name: "MensagemErro",
                table: "FinanceiroCotacaoAtivo",
                newName: "ErrorMessage");

            migrationBuilder.RenameColumn(
                name: "HorarioMercado",
                table: "FinanceiroCotacaoAtivo",
                newName: "MarketTime");

            migrationBuilder.RenameColumn(
                name: "ExpiraEm",
                table: "FinanceiroCotacaoAtivo",
                newName: "ExpiresAt");

            migrationBuilder.RenameColumn(
                name: "ConsultadoEm",
                table: "FinanceiroCotacaoAtivo",
                newName: "RetrievedAt");

            migrationBuilder.RenameIndex(
                name: "IX_FinanceiroCotacaoAtivo_ConsultadoEm",
                table: "FinanceiroCotacaoAtivo",
                newName: "IX_FinanceiroCotacaoAtivo_RetrievedAt");

            migrationBuilder.RenameColumn(
                name: "CarteiraPaiId",
                table: "FinanceiroCarteira",
                newName: "ParentId");

            migrationBuilder.RenameColumn(
                name: "EhSistema",
                table: "FinanceiroCarteira",
                newName: "IsSistema");

            migrationBuilder.RenameIndex(
                name: "IX_FinanceiroCarteira_CarteiraPaiId",
                table: "FinanceiroCarteira",
                newName: "IX_FinanceiroCarteira_ParentId");

            migrationBuilder.RenameColumn(
                name: "Sigla",
                table: "FinanceiroAtivo",
                newName: "Ticker");

            migrationBuilder.RenameColumn(
                name: "PapelConceitual",
                table: "FinanceiroAtivo",
                newName: "ConceptRole");

            migrationBuilder.RenameColumn(
                name: "Nome",
                table: "FinanceiroAtivo",
                newName: "Name");

            migrationBuilder.RenameColumn(
                name: "Moeda",
                table: "FinanceiroAtivo",
                newName: "Currency");

            migrationBuilder.RenameColumn(
                name: "Mercado",
                table: "FinanceiroAtivo",
                newName: "Market");

            migrationBuilder.RenameColumn(
                name: "EhCripto",
                table: "FinanceiroAtivo",
                newName: "IsCrypto");

            migrationBuilder.RenameColumn(
                name: "Classe",
                table: "FinanceiroAtivo",
                newName: "AssetClass");

            migrationBuilder.RenameColumn(
                name: "Chave",
                table: "FinanceiroAtivo",
                newName: "AssetKey");

            migrationBuilder.RenameColumn(
                name: "Ativo",
                table: "FinanceiroAtivo",
                newName: "IsActive");

            migrationBuilder.RenameIndex(
                name: "IX_FinanceiroAtivo_Chave",
                table: "FinanceiroAtivo",
                newName: "IX_FinanceiroAtivo_AssetKey");

            migrationBuilder.AddForeignKey(
                name: "FK_FinanceiroCarteira_FinanceiroCarteira_ParentId",
                table: "FinanceiroCarteira",
                column: "ParentId",
                principalTable: "FinanceiroCarteira",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_FinanceiroPosicaoEstimativa_FinanceiroAtivo_AssetId",
                table: "FinanceiroPosicaoEstimativa",
                column: "AssetId",
                principalTable: "FinanceiroAtivo",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
