using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sistema.INFRA.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddProventoBrapi : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FinanceiroRendimento_FinanceiroCarga_CargaFinanceiraId",
                table: "FinanceiroRendimento");

            migrationBuilder.DropIndex(
                name: "IX_FinanceiroRendimento_AssetId",
                table: "FinanceiroRendimento");

            migrationBuilder.AlterColumn<int>(
                name: "CargaFinanceiraId",
                table: "FinanceiroRendimento",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<string>(
                name: "ChaveNatural",
                table: "FinanceiroRendimento",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Fonte",
                table: "FinanceiroRendimento",
                type: "nvarchar(80)",
                maxLength: 80,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "Quantity",
                table: "FinanceiroRendimento",
                type: "decimal(28,12)",
                precision: 28,
                scale: 12,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "RatePerShare",
                table: "FinanceiroRendimento",
                type: "decimal(28,12)",
                precision: 28,
                scale: 12,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_FinanceiroRendimento_AssetId_PaymentDate",
                table: "FinanceiroRendimento",
                columns: new[] { "AssetId", "PaymentDate" });

            migrationBuilder.CreateIndex(
                name: "IX_FinanceiroRendimento_ChaveNatural",
                table: "FinanceiroRendimento",
                column: "ChaveNatural",
                unique: true,
                filter: "[ChaveNatural] IS NOT NULL AND [DataExclusao] IS NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_FinanceiroRendimento_FinanceiroCarga_CargaFinanceiraId",
                table: "FinanceiroRendimento",
                column: "CargaFinanceiraId",
                principalTable: "FinanceiroCarga",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FinanceiroRendimento_FinanceiroCarga_CargaFinanceiraId",
                table: "FinanceiroRendimento");

            migrationBuilder.DropIndex(
                name: "IX_FinanceiroRendimento_AssetId_PaymentDate",
                table: "FinanceiroRendimento");

            migrationBuilder.DropIndex(
                name: "IX_FinanceiroRendimento_ChaveNatural",
                table: "FinanceiroRendimento");

            migrationBuilder.DropColumn(
                name: "ChaveNatural",
                table: "FinanceiroRendimento");

            migrationBuilder.DropColumn(
                name: "Fonte",
                table: "FinanceiroRendimento");

            migrationBuilder.DropColumn(
                name: "Quantity",
                table: "FinanceiroRendimento");

            migrationBuilder.DropColumn(
                name: "RatePerShare",
                table: "FinanceiroRendimento");

            migrationBuilder.AlterColumn<int>(
                name: "CargaFinanceiraId",
                table: "FinanceiroRendimento",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_FinanceiroRendimento_AssetId",
                table: "FinanceiroRendimento",
                column: "AssetId");

            migrationBuilder.AddForeignKey(
                name: "FK_FinanceiroRendimento_FinanceiroCarga_CargaFinanceiraId",
                table: "FinanceiroRendimento",
                column: "CargaFinanceiraId",
                principalTable: "FinanceiroCarga",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
