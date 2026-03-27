using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartTeam.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddBannerStyleFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ButtonBorderRadius",
                table: "Banners",
                type: "int",
                nullable: false,
                defaultValue: 8);

            migrationBuilder.AddColumn<string>(
                name: "ButtonColor",
                table: "Banners",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "#ffffff");

            migrationBuilder.AddColumn<int>(
                name: "ButtonPositionX",
                table: "Banners",
                type: "int",
                nullable: false,
                defaultValue: 50);

            migrationBuilder.AddColumn<int>(
                name: "ButtonPositionY",
                table: "Banners",
                type: "int",
                nullable: false,
                defaultValue: 65);

            migrationBuilder.AddColumn<string>(
                name: "ButtonTextColor",
                table: "Banners",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "#000000");

            migrationBuilder.AddColumn<string>(
                name: "DescriptionColor",
                table: "Banners",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "#eeeeee");

            migrationBuilder.AddColumn<int>(
                name: "DescriptionFontSize",
                table: "Banners",
                type: "int",
                nullable: false,
                defaultValue: 16);

            migrationBuilder.AddColumn<int>(
                name: "DescriptionPositionX",
                table: "Banners",
                type: "int",
                nullable: false,
                defaultValue: 50);

            migrationBuilder.AddColumn<int>(
                name: "DescriptionPositionY",
                table: "Banners",
                type: "int",
                nullable: false,
                defaultValue: 40);

            migrationBuilder.AddColumn<string>(
                name: "TitleAlign",
                table: "Banners",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "center");

            migrationBuilder.AddColumn<string>(
                name: "TitleColor",
                table: "Banners",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "#ffffff");

            migrationBuilder.AddColumn<int>(
                name: "TitleFontSize",
                table: "Banners",
                type: "int",
                nullable: false,
                defaultValue: 32);

            migrationBuilder.AddColumn<int>(
                name: "TitlePositionX",
                table: "Banners",
                type: "int",
                nullable: false,
                defaultValue: 50);

            migrationBuilder.AddColumn<int>(
                name: "TitlePositionY",
                table: "Banners",
                type: "int",
                nullable: false,
                defaultValue: 20);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ButtonBorderRadius",
                table: "Banners");

            migrationBuilder.DropColumn(
                name: "ButtonColor",
                table: "Banners");

            migrationBuilder.DropColumn(
                name: "ButtonPositionX",
                table: "Banners");

            migrationBuilder.DropColumn(
                name: "ButtonPositionY",
                table: "Banners");

            migrationBuilder.DropColumn(
                name: "ButtonTextColor",
                table: "Banners");

            migrationBuilder.DropColumn(
                name: "DescriptionColor",
                table: "Banners");

            migrationBuilder.DropColumn(
                name: "DescriptionFontSize",
                table: "Banners");

            migrationBuilder.DropColumn(
                name: "DescriptionPositionX",
                table: "Banners");

            migrationBuilder.DropColumn(
                name: "DescriptionPositionY",
                table: "Banners");

            migrationBuilder.DropColumn(
                name: "TitleAlign",
                table: "Banners");

            migrationBuilder.DropColumn(
                name: "TitleColor",
                table: "Banners");

            migrationBuilder.DropColumn(
                name: "TitleFontSize",
                table: "Banners");

            migrationBuilder.DropColumn(
                name: "TitlePositionX",
                table: "Banners");

            migrationBuilder.DropColumn(
                name: "TitlePositionY",
                table: "Banners");
        }
    }
}
