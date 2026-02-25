using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Eticaret.Data.Migrations
{
    /// <inheritdoc />
    public partial class ThemesAdded : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "ThemeSettings",
                columns: new[] { "Id", "BackgroundColor", "FooterBgColor", "IsActive", "MainColor", "Name", "NavbarBgColor", "SecondaryColor", "TextColor" },
                values: new object[,]
                {
                    { 1, "#f8f9fa", "#343a40", true, "#0d6efd", "Varsayılan Tema", "#ffffff", "#6c757d", "#212529" },
                    { 2, "#F8F9FA", "#1B4332", false, "#C1121F", "Yılbaşı Teması", "#2D6A4F", "#2D6A4F", "#1B1B1B" },
                    { 3, "#0F172A", "#020617", true, "#6366F1", "Dark Modern", "#020617", "#22C55E", "#E5E7EB" },
                    { 4, "#F1F8F4", "#E8F5E9", true, "#4CAF50", "Soft Nature", "#FFFFFF", "#A3D9A5", "#263238" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "ThemeSettings",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "ThemeSettings",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "ThemeSettings",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "ThemeSettings",
                keyColumn: "Id",
                keyValue: 4);
        }
    }
}
