using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sistema.INFRA.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Configuracao",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Agrupamento = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Chave = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Valor = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Tipo = table.Column<int>(type: "int", nullable: false),
                    Descricao = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Ativo = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    DataInclusao = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DataAlteracao = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UsuarioInclusao = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UsuarioAlteracao = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Configuracao", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Funcionalidade",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nome = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Ativo = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    DataInclusao = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DataAlteracao = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UsuarioInclusao = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UsuarioAlteracao = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Funcionalidade", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Log",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DataOperacao = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Entidade = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Operacao = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Sucesso = table.Column<bool>(type: "bit", nullable: false),
                    Mensagem = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Tipo = table.Column<int>(type: "int", nullable: false),
                    Usuario = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Detalhe = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Log", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Perfil",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nome = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Ativo = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    DataInclusao = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DataAlteracao = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UsuarioInclusao = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UsuarioAlteracao = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Perfil", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PerfilFuncionalidade",
                columns: table => new
                {
                    PerfilId = table.Column<int>(type: "int", nullable: false),
                    FuncionalidadeId = table.Column<int>(type: "int", nullable: false),
                    PodeLer = table.Column<bool>(type: "bit", nullable: false),
                    PodeEscrever = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PerfilFuncionalidade", x => new { x.PerfilId, x.FuncionalidadeId });
                    table.ForeignKey(
                        name: "FK_PerfilFuncionalidade_Funcionalidade_FuncionalidadeId",
                        column: x => x.FuncionalidadeId,
                        principalTable: "Funcionalidade",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PerfilFuncionalidade_Perfil_PerfilId",
                        column: x => x.PerfilId,
                        principalTable: "Perfil",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Usuario",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nome = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Cpf = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Ativo = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    SenhaHash = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PerfilId = table.Column<int>(type: "int", nullable: false),
                    DataInclusao = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DataAlteracao = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UsuarioInclusao = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UsuarioAlteracao = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Usuario", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Usuario_Perfil_PerfilId",
                        column: x => x.PerfilId,
                        principalTable: "Perfil",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Tema",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UsuarioId = table.Column<int>(type: "int", nullable: false),
                    ModoEscuro = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    CorHeader = table.Column<string>(type: "nvarchar(7)", maxLength: 7, nullable: false),
                    CorBarraEsquerda = table.Column<string>(type: "nvarchar(7)", maxLength: 7, nullable: false),
                    CorBarraDireita = table.Column<string>(type: "nvarchar(7)", maxLength: 7, nullable: false),
                    CorFooter = table.Column<string>(type: "nvarchar(7)", maxLength: 7, nullable: false),
                    HeaderFixo = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    FooterFixo = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    MenuLateralExpandido = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    DataInclusao = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DataAlteracao = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UsuarioInclusao = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UsuarioAlteracao = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tema", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Tema_Usuario_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuario",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PerfilFuncionalidade_FuncionalidadeId",
                table: "PerfilFuncionalidade",
                column: "FuncionalidadeId");

            migrationBuilder.CreateIndex(
                name: "IX_Tema_UsuarioId",
                table: "Tema",
                column: "UsuarioId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Usuario_PerfilId",
                table: "Usuario",
                column: "PerfilId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Configuracao");

            migrationBuilder.DropTable(
                name: "Log");

            migrationBuilder.DropTable(
                name: "PerfilFuncionalidade");

            migrationBuilder.DropTable(
                name: "Tema");

            migrationBuilder.DropTable(
                name: "Funcionalidade");

            migrationBuilder.DropTable(
                name: "Usuario");

            migrationBuilder.DropTable(
                name: "Perfil");
        }
    }
}
