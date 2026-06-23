using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sistema.INFRA.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCarteiraHierarquia : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ParentId",
                table: "FinanceiroCarteira",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_FinanceiroCarteira_ParentId",
                table: "FinanceiroCarteira",
                column: "ParentId");

            migrationBuilder.AddForeignKey(
                name: "FK_FinanceiroCarteira_FinanceiroCarteira_ParentId",
                table: "FinanceiroCarteira",
                column: "ParentId",
                principalTable: "FinanceiroCarteira",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FinanceiroCarteira_FinanceiroCarteira_ParentId",
                table: "FinanceiroCarteira");

            migrationBuilder.DropIndex(
                name: "IX_FinanceiroCarteira_ParentId",
                table: "FinanceiroCarteira");

            migrationBuilder.DropColumn(
                name: "ParentId",
                table: "FinanceiroCarteira");
        }
    }
}
