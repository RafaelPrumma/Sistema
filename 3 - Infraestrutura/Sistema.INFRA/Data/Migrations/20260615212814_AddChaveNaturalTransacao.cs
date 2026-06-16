using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sistema.INFRA.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddChaveNaturalTransacao : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ChaveNatural",
                table: "FinanceiroTransacao",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_FinanceiroTransacao_ChaveNatural",
                table: "FinanceiroTransacao",
                column: "ChaveNatural",
                unique: true,
                filter: "[ChaveNatural] IS NOT NULL AND [DataExclusao] IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_FinanceiroTransacao_ChaveNatural",
                table: "FinanceiroTransacao");

            migrationBuilder.DropColumn(
                name: "ChaveNatural",
                table: "FinanceiroTransacao");
        }
    }
}
