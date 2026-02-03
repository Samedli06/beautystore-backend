using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartTeam.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateWalletTransactionSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "BalanceAfter",
                table: "WalletTransactions",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "BalanceBefore",
                table: "WalletTransactions",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BalanceAfter",
                table: "WalletTransactions");

            migrationBuilder.DropColumn(
                name: "BalanceBefore",
                table: "WalletTransactions");
        }
    }
}
