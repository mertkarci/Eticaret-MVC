using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Eticaret.Data.Migrations
{
    /// <inheritdoc />
    public partial class OrderTCAddressTCattAdded : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BillingTC",
                table: "Orders",
                type: "TEXT",
                maxLength: 11,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsCorporate",
                table: "Addresses",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "TaxNumber",
                table: "Addresses",
                type: "TEXT",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TaxOffice",
                table: "Addresses",
                type: "TEXT",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TcNo",
                table: "Addresses",
                type: "TEXT",
                maxLength: 11,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BillingTC",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "IsCorporate",
                table: "Addresses");

            migrationBuilder.DropColumn(
                name: "TaxNumber",
                table: "Addresses");

            migrationBuilder.DropColumn(
                name: "TaxOffice",
                table: "Addresses");

            migrationBuilder.DropColumn(
                name: "TcNo",
                table: "Addresses");
        }
    }
}
