using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sistema.INFRA.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddFonteTransacao : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Fonte",
                table: "FinanceiroTransacao",
                type: "nvarchar(80)",
                maxLength: 80,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_FinanceiroTransacao_Fonte",
                table: "FinanceiroTransacao",
                column: "Fonte");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_FinanceiroTransacao_Fonte",
                table: "FinanceiroTransacao");

            migrationBuilder.DropColumn(
                name: "Fonte",
                table: "FinanceiroTransacao");
        }
    }
}
