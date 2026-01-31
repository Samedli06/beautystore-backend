using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartTeam.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class DeferredOrderCreation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Payments_Orders_OrderId",
                table: "Payments");

            migrationBuilder.DropIndex(
                name: "IX_Payments_OrderId",
                table: "Payments");

            migrationBuilder.AlterColumn<Guid>(
                name: "OrderId",
                table: "Payments",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.AddColumn<Guid>(
                name: "PendingOrderId",
                table: "Payments",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "PendingOrders",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CartSnapshot = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CustomerInfo = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TotalAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    PromoCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PendingOrders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PendingOrders_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Payments_OrderId",
                table: "Payments",
                column: "OrderId",
                unique: true,
                filter: "[OrderId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_PendingOrderId",
                table: "Payments",
                column: "PendingOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_PendingOrders_ExpiresAt",
                table: "PendingOrders",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_PendingOrders_UserId",
                table: "PendingOrders",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Payments_Orders_OrderId",
                table: "Payments",
                column: "OrderId",
                principalTable: "Orders",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Payments_PendingOrders_PendingOrderId",
                table: "Payments",
                column: "PendingOrderId",
                principalTable: "PendingOrders",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Payments_Orders_OrderId",
                table: "Payments");

            migrationBuilder.DropForeignKey(
                name: "FK_Payments_PendingOrders_PendingOrderId",
                table: "Payments");

            migrationBuilder.DropTable(
                name: "PendingOrders");

            migrationBuilder.DropIndex(
                name: "IX_Payments_OrderId",
                table: "Payments");

            migrationBuilder.DropIndex(
                name: "IX_Payments_PendingOrderId",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "PendingOrderId",
                table: "Payments");

            migrationBuilder.AlterColumn<Guid>(
                name: "OrderId",
                table: "Payments",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Payments_OrderId",
                table: "Payments",
                column: "OrderId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Payments_Orders_OrderId",
                table: "Payments",
                column: "OrderId",
                principalTable: "Orders",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
