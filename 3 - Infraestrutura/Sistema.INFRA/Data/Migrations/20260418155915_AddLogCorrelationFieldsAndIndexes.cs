using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sistema.INFRA.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddLogCorrelationFieldsAndIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Mensagens_Mensagens_MensagemPaiId",
                table: "Mensagens");

            migrationBuilder.AlterColumn<int>(
                name: "DestinatarioId",
                table: "Mensagens",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<string>(
                name: "Corpo",
                table: "Mensagens",
                type: "nvarchar(max)",
                maxLength: 5000,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(2000)",
                oldMaxLength: 2000);

            migrationBuilder.AddColumn<int>(
                name: "AutorId",
                table: "Mensagens",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AvisoAudiencia",
                table: "Mensagens",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AvisoGrupo",
                table: "Mensagens",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AvisoPrioridade",
                table: "Mensagens",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "AvisoValidoAte",
                table: "Mensagens",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "Fixada",
                table: "Mensagens",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "PerfilId",
                table: "Mensagens",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "Mensagens",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Tipo",
                table: "Mensagens",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "CorrelationId",
                table: "Log",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Modulo",
                table: "Log",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "SpanId",
                table: "Log",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TraceId",
                table: "Log",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "MensagemDestinatarios",
                columns: table => new
                {
                    MensagemId = table.Column<int>(type: "int", nullable: false),
                    UsuarioId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MensagemDestinatarios", x => new { x.MensagemId, x.UsuarioId });
                    table.ForeignKey(
                        name: "FK_MensagemDestinatarios_Mensagens_MensagemId",
                        column: x => x.MensagemId,
                        principalTable: "Mensagens",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MensagemDestinatarios_Usuario_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuario",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MensagemLeituras",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PublicacaoId = table.Column<int>(type: "int", nullable: false),
                    UsuarioId = table.Column<int>(type: "int", nullable: false),
                    DataLeitura = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DataEntrega = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MensagemLeituras", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MensagemLeituras_Mensagens_PublicacaoId",
                        column: x => x.PublicacaoId,
                        principalTable: "Mensagens",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MensagemLeituras_Usuario_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuario",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MensagemReacoes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PublicacaoId = table.Column<int>(type: "int", nullable: false),
                    UsuarioId = table.Column<int>(type: "int", nullable: false),
                    TipoReacao = table.Column<int>(type: "int", nullable: false),
                    Data = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MensagemReacoes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MensagemReacoes_Mensagens_PublicacaoId",
                        column: x => x.PublicacaoId,
                        principalTable: "Mensagens",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MensagemReacoes_Usuario_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuario",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Mensagens_AutorId",
                table: "Mensagens",
                column: "AutorId");

            migrationBuilder.CreateIndex(
                name: "IX_Mensagens_PerfilId",
                table: "Mensagens",
                column: "PerfilId");

            migrationBuilder.CreateIndex(
                name: "IX_Log_CorrelationId",
                table: "Log",
                column: "CorrelationId");

            migrationBuilder.CreateIndex(
                name: "IX_Log_Modulo_DataOperacao",
                table: "Log",
                columns: new[] { "Modulo", "DataOperacao" });

            migrationBuilder.CreateIndex(
                name: "IX_MensagemDestinatarios_UsuarioId",
                table: "MensagemDestinatarios",
                column: "UsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_MensagemLeituras_PublicacaoId_UsuarioId",
                table: "MensagemLeituras",
                columns: new[] { "PublicacaoId", "UsuarioId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MensagemLeituras_UsuarioId",
                table: "MensagemLeituras",
                column: "UsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_MensagemReacoes_PublicacaoId_UsuarioId",
                table: "MensagemReacoes",
                columns: new[] { "PublicacaoId", "UsuarioId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MensagemReacoes_UsuarioId",
                table: "MensagemReacoes",
                column: "UsuarioId");

            migrationBuilder.AddForeignKey(
                name: "FK_Mensagens_Mensagens_MensagemPaiId",
                table: "Mensagens",
                column: "MensagemPaiId",
                principalTable: "Mensagens",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Mensagens_Perfil_PerfilId",
                table: "Mensagens",
                column: "PerfilId",
                principalTable: "Perfil",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Mensagens_Usuario_AutorId",
                table: "Mensagens",
                column: "AutorId",
                principalTable: "Usuario",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Mensagens_Mensagens_MensagemPaiId",
                table: "Mensagens");

            migrationBuilder.DropForeignKey(
                name: "FK_Mensagens_Perfil_PerfilId",
                table: "Mensagens");

            migrationBuilder.DropForeignKey(
                name: "FK_Mensagens_Usuario_AutorId",
                table: "Mensagens");

            migrationBuilder.DropTable(
                name: "MensagemDestinatarios");

            migrationBuilder.DropTable(
                name: "MensagemLeituras");

            migrationBuilder.DropTable(
                name: "MensagemReacoes");

            migrationBuilder.DropIndex(
                name: "IX_Mensagens_AutorId",
                table: "Mensagens");

            migrationBuilder.DropIndex(
                name: "IX_Mensagens_PerfilId",
                table: "Mensagens");

            migrationBuilder.DropIndex(
                name: "IX_Log_CorrelationId",
                table: "Log");

            migrationBuilder.DropIndex(
                name: "IX_Log_Modulo_DataOperacao",
                table: "Log");

            migrationBuilder.DropColumn(
                name: "AutorId",
                table: "Mensagens");

            migrationBuilder.DropColumn(
                name: "AvisoAudiencia",
                table: "Mensagens");

            migrationBuilder.DropColumn(
                name: "AvisoGrupo",
                table: "Mensagens");

            migrationBuilder.DropColumn(
                name: "AvisoPrioridade",
                table: "Mensagens");

            migrationBuilder.DropColumn(
                name: "AvisoValidoAte",
                table: "Mensagens");

            migrationBuilder.DropColumn(
                name: "Fixada",
                table: "Mensagens");

            migrationBuilder.DropColumn(
                name: "PerfilId",
                table: "Mensagens");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Mensagens");

            migrationBuilder.DropColumn(
                name: "Tipo",
                table: "Mensagens");

            migrationBuilder.DropColumn(
                name: "CorrelationId",
                table: "Log");

            migrationBuilder.DropColumn(
                name: "Modulo",
                table: "Log");

            migrationBuilder.DropColumn(
                name: "SpanId",
                table: "Log");

            migrationBuilder.DropColumn(
                name: "TraceId",
                table: "Log");

            migrationBuilder.AlterColumn<int>(
                name: "DestinatarioId",
                table: "Mensagens",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Corpo",
                table: "Mensagens",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldMaxLength: 5000);

            migrationBuilder.AddForeignKey(
                name: "FK_Mensagens_Mensagens_MensagemPaiId",
                table: "Mensagens",
                column: "MensagemPaiId",
                principalTable: "Mensagens",
                principalColumn: "Id");
        }
    }
}
