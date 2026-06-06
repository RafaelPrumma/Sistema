using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable
#pragma warning disable CA1861

namespace Sistema.INFRA.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddMinhasFinancas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FinanceiroAtivo",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AssetKey = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    Ticker = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    Name = table.Column<string>(type: "nvarchar(240)", maxLength: 240, nullable: false),
                    AssetClass = table.Column<int>(type: "int", nullable: false),
                    Market = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    Currency = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    IsCrypto = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    ConceptRole = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    DataInclusao = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DataAlteracao = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UsuarioInclusao = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UsuarioAlteracao = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DataExclusao = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UsuarioExclusao = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinanceiroAtivo", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FinanceiroCarga",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SchemaVersion = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    JsonSha256 = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    SourcePath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    GeneratedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ImportedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    SummaryJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DashboardJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DataInclusao = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DataAlteracao = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UsuarioInclusao = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UsuarioAlteracao = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DataExclusao = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UsuarioExclusao = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinanceiroCarga", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FinanceiroAgregado",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CargaFinanceiraId = table.Column<int>(type: "int", nullable: false),
                    Dimensao = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Chave = table.Column<string>(type: "nvarchar(180)", maxLength: 180, nullable: false),
                    Ano = table.Column<int>(type: "int", nullable: true),
                    Mes = table.Column<string>(type: "nvarchar(7)", maxLength: 7, nullable: true),
                    ClasseAtivo = table.Column<int>(type: "int", nullable: true),
                    ValorCompra = table.Column<decimal>(type: "decimal(24,8)", precision: 24, scale: 8, nullable: true),
                    ValorVenda = table.Column<decimal>(type: "decimal(24,8)", precision: 24, scale: 8, nullable: true),
                    Saldo = table.Column<decimal>(type: "decimal(24,8)", precision: 24, scale: 8, nullable: true),
                    Quantidade = table.Column<decimal>(type: "decimal(28,12)", precision: 28, scale: 12, nullable: true),
                    Contagem = table.Column<int>(type: "int", nullable: true),
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
                    table.PrimaryKey("PK_FinanceiroAgregado", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FinanceiroAgregado_FinanceiroCarga_CargaFinanceiraId",
                        column: x => x.CargaFinanceiraId,
                        principalTable: "FinanceiroCarga",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FinanceiroAlertaConfiabilidade",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CargaFinanceiraId = table.Column<int>(type: "int", nullable: false),
                    EntityType = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    EntityId = table.Column<int>(type: "int", nullable: true),
                    Severity = table.Column<int>(type: "int", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Message = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    Details = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DataInclusao = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DataAlteracao = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UsuarioInclusao = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UsuarioAlteracao = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DataExclusao = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UsuarioExclusao = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinanceiroAlertaConfiabilidade", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FinanceiroAlertaConfiabilidade_FinanceiroCarga_CargaFinanceiraId",
                        column: x => x.CargaFinanceiraId,
                        principalTable: "FinanceiroCarga",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FinanceiroDocumento",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CargaFinanceiraId = table.Column<int>(type: "int", nullable: false),
                    Colecao = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Path = table.Column<string>(type: "nvarchar(700)", maxLength: 700, nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(260)", maxLength: 260, nullable: false),
                    FileType = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Source = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Sha256 = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    SizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    ReferenceYear = table.Column<int>(type: "int", nullable: true),
                    PageCount = table.Column<int>(type: "int", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    RawMetadataJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DataInclusao = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DataAlteracao = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UsuarioInclusao = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UsuarioAlteracao = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DataExclusao = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UsuarioExclusao = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinanceiroDocumento", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FinanceiroDocumento_FinanceiroCarga_CargaFinanceiraId",
                        column: x => x.CargaFinanceiraId,
                        principalTable: "FinanceiroCarga",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FinanceiroPosicaoEstimativa",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CargaFinanceiraId = table.Column<int>(type: "int", nullable: false),
                    AssetId = table.Column<int>(type: "int", nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(24,8)", precision: 24, scale: 8, nullable: false),
                    AveragePrice = table.Column<decimal>(type: "decimal(24,8)", precision: 24, scale: 8, nullable: false),
                    TotalInvested = table.Column<decimal>(type: "decimal(24,8)", precision: 24, scale: 8, nullable: false),
                    TotalSold = table.Column<decimal>(type: "decimal(24,8)", precision: 24, scale: 8, nullable: false),
                    RealizedResult = table.Column<decimal>(type: "decimal(24,8)", precision: 24, scale: 8, nullable: false),
                    EstimatedCurrentPosition = table.Column<decimal>(type: "decimal(24,8)", precision: 24, scale: 8, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    ConfidenceLevel = table.Column<int>(type: "int", nullable: false),
                    LastOperationDate = table.Column<DateTime>(type: "datetime2", nullable: true),
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
                    table.PrimaryKey("PK_FinanceiroPosicaoEstimativa", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FinanceiroPosicaoEstimativa_FinanceiroAtivo_AssetId",
                        column: x => x.AssetId,
                        principalTable: "FinanceiroAtivo",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FinanceiroPosicaoEstimativa_FinanceiroCarga_CargaFinanceiraId",
                        column: x => x.CargaFinanceiraId,
                        principalTable: "FinanceiroCarga",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FinanceiroRendimento",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CargaFinanceiraId = table.Column<int>(type: "int", nullable: false),
                    SourceDocumentId = table.Column<int>(type: "int", nullable: true),
                    AssetId = table.Column<int>(type: "int", nullable: true),
                    PaymentDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReferenceDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IncomeType = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    Source = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(24,8)", precision: 24, scale: 8, nullable: false),
                    TaxWithheld = table.Column<decimal>(type: "decimal(24,8)", precision: 24, scale: 8, nullable: false),
                    Currency = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Taxation = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
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
                    table.PrimaryKey("PK_FinanceiroRendimento", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FinanceiroRendimento_FinanceiroAtivo_AssetId",
                        column: x => x.AssetId,
                        principalTable: "FinanceiroAtivo",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FinanceiroRendimento_FinanceiroCarga_CargaFinanceiraId",
                        column: x => x.CargaFinanceiraId,
                        principalTable: "FinanceiroCarga",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FinanceiroConteudoBruto",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DocumentoFinanceiroId = table.Column<int>(type: "int", nullable: false),
                    ContentType = table.Column<int>(type: "int", nullable: false),
                    PageNumber = table.Column<int>(type: "int", nullable: true),
                    SheetName = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: true),
                    RowNumber = table.Column<int>(type: "int", nullable: true),
                    RawText = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RawJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DataInclusao = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DataAlteracao = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UsuarioInclusao = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UsuarioAlteracao = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DataExclusao = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UsuarioExclusao = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinanceiroConteudoBruto", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FinanceiroConteudoBruto_FinanceiroDocumento_DocumentoFinanceiroId",
                        column: x => x.DocumentoFinanceiroId,
                        principalTable: "FinanceiroDocumento",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FinanceiroOperacaoB3",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CargaFinanceiraId = table.Column<int>(type: "int", nullable: false),
                    SourceDocumentId = table.Column<int>(type: "int", nullable: true),
                    TradeDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    NoteNumber = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: true),
                    PageNumber = table.Column<int>(type: "int", nullable: true),
                    Broker = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Market = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    OperationType = table.Column<int>(type: "int", nullable: false),
                    AssetId = table.Column<int>(type: "int", nullable: true),
                    OriginalAssetName = table.Column<string>(type: "nvarchar(240)", maxLength: 240, nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(24,8)", precision: 24, scale: 8, nullable: false),
                    UnitPrice = table.Column<decimal>(type: "decimal(24,8)", precision: 24, scale: 8, nullable: false),
                    GrossAmount = table.Column<decimal>(type: "decimal(24,8)", precision: 24, scale: 8, nullable: false),
                    Fees = table.Column<decimal>(type: "decimal(24,8)", precision: 24, scale: 8, nullable: false),
                    NetAmount = table.Column<decimal>(type: "decimal(24,8)", precision: 24, scale: 8, nullable: false),
                    DebitCredit = table.Column<string>(type: "nvarchar(4)", maxLength: 4, nullable: false),
                    IsCanonical = table.Column<bool>(type: "bit", nullable: false),
                    DuplicateGroupKey = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    ConfidenceLevel = table.Column<int>(type: "int", nullable: false),
                    SourceFile = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
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
                    table.PrimaryKey("PK_FinanceiroOperacaoB3", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FinanceiroOperacaoB3_FinanceiroAtivo_AssetId",
                        column: x => x.AssetId,
                        principalTable: "FinanceiroAtivo",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FinanceiroOperacaoB3_FinanceiroCarga_CargaFinanceiraId",
                        column: x => x.CargaFinanceiraId,
                        principalTable: "FinanceiroCarga",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FinanceiroOperacaoB3_FinanceiroDocumento_SourceDocumentId",
                        column: x => x.SourceDocumentId,
                        principalTable: "FinanceiroDocumento",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "FinanceiroTransacaoCripto",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CargaFinanceiraId = table.Column<int>(type: "int", nullable: false),
                    SourceDocumentId = table.Column<int>(type: "int", nullable: true),
                    TransactionDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Exchange = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    OperationType = table.Column<int>(type: "int", nullable: false),
                    AssetSymbol = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Pair = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    Amount = table.Column<decimal>(type: "decimal(28,12)", precision: 28, scale: 12, nullable: false),
                    Price = table.Column<decimal>(type: "decimal(28,12)", precision: 28, scale: 12, nullable: true),
                    Total = table.Column<decimal>(type: "decimal(28,12)", precision: 28, scale: 12, nullable: true),
                    FeeAsset = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    FeeAmount = table.Column<decimal>(type: "decimal(28,12)", precision: 28, scale: 12, nullable: true),
                    RawType = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    SourceFile = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
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
                    table.PrimaryKey("PK_FinanceiroTransacaoCripto", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FinanceiroTransacaoCripto_FinanceiroCarga_CargaFinanceiraId",
                        column: x => x.CargaFinanceiraId,
                        principalTable: "FinanceiroCarga",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FinanceiroTransacaoCripto_FinanceiroDocumento_SourceDocumentId",
                        column: x => x.SourceDocumentId,
                        principalTable: "FinanceiroDocumento",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_FinanceiroAgregado_CargaFinanceiraId_Dimensao",
                table: "FinanceiroAgregado",
                columns: new[] { "CargaFinanceiraId", "Dimensao" });

            migrationBuilder.CreateIndex(
                name: "IX_FinanceiroAlertaConfiabilidade_CargaFinanceiraId_Severity",
                table: "FinanceiroAlertaConfiabilidade",
                columns: new[] { "CargaFinanceiraId", "Severity" });

            migrationBuilder.CreateIndex(
                name: "IX_FinanceiroAtivo_AssetKey",
                table: "FinanceiroAtivo",
                column: "AssetKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FinanceiroCarga_JsonSha256",
                table: "FinanceiroCarga",
                column: "JsonSha256",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FinanceiroConteudoBruto_DocumentoFinanceiroId_ContentType_PageNumber",
                table: "FinanceiroConteudoBruto",
                columns: new[] { "DocumentoFinanceiroId", "ContentType", "PageNumber" });

            migrationBuilder.CreateIndex(
                name: "IX_FinanceiroDocumento_CargaFinanceiraId_FileName",
                table: "FinanceiroDocumento",
                columns: new[] { "CargaFinanceiraId", "FileName" });

            migrationBuilder.CreateIndex(
                name: "IX_FinanceiroOperacaoB3_AssetId_IsCanonical",
                table: "FinanceiroOperacaoB3",
                columns: new[] { "AssetId", "IsCanonical" });

            migrationBuilder.CreateIndex(
                name: "IX_FinanceiroOperacaoB3_CargaFinanceiraId_TradeDate",
                table: "FinanceiroOperacaoB3",
                columns: new[] { "CargaFinanceiraId", "TradeDate" });

            migrationBuilder.CreateIndex(
                name: "IX_FinanceiroOperacaoB3_DuplicateGroupKey",
                table: "FinanceiroOperacaoB3",
                column: "DuplicateGroupKey");

            migrationBuilder.CreateIndex(
                name: "IX_FinanceiroOperacaoB3_SourceDocumentId",
                table: "FinanceiroOperacaoB3",
                column: "SourceDocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_FinanceiroPosicaoEstimativa_AssetId",
                table: "FinanceiroPosicaoEstimativa",
                column: "AssetId");

            migrationBuilder.CreateIndex(
                name: "IX_FinanceiroPosicaoEstimativa_CargaFinanceiraId_Status",
                table: "FinanceiroPosicaoEstimativa",
                columns: new[] { "CargaFinanceiraId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_FinanceiroRendimento_AssetId",
                table: "FinanceiroRendimento",
                column: "AssetId");

            migrationBuilder.CreateIndex(
                name: "IX_FinanceiroRendimento_CargaFinanceiraId",
                table: "FinanceiroRendimento",
                column: "CargaFinanceiraId");

            migrationBuilder.CreateIndex(
                name: "IX_FinanceiroTransacaoCripto_AssetSymbol",
                table: "FinanceiroTransacaoCripto",
                column: "AssetSymbol");

            migrationBuilder.CreateIndex(
                name: "IX_FinanceiroTransacaoCripto_CargaFinanceiraId_TransactionDate",
                table: "FinanceiroTransacaoCripto",
                columns: new[] { "CargaFinanceiraId", "TransactionDate" });

            migrationBuilder.CreateIndex(
                name: "IX_FinanceiroTransacaoCripto_SourceDocumentId",
                table: "FinanceiroTransacaoCripto",
                column: "SourceDocumentId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FinanceiroAgregado");

            migrationBuilder.DropTable(
                name: "FinanceiroAlertaConfiabilidade");

            migrationBuilder.DropTable(
                name: "FinanceiroConteudoBruto");

            migrationBuilder.DropTable(
                name: "FinanceiroOperacaoB3");

            migrationBuilder.DropTable(
                name: "FinanceiroPosicaoEstimativa");

            migrationBuilder.DropTable(
                name: "FinanceiroRendimento");

            migrationBuilder.DropTable(
                name: "FinanceiroTransacaoCripto");

            migrationBuilder.DropTable(
                name: "FinanceiroAtivo");

            migrationBuilder.DropTable(
                name: "FinanceiroDocumento");

            migrationBuilder.DropTable(
                name: "FinanceiroCarga");
        }
    }
}
