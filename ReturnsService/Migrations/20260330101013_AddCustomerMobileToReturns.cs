using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ReturnsService.Migrations
{
    /// <inheritdoc />
    public partial class AddCustomerMobileToReturns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CustomerMobile",
                table: "Returns",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CustomerMobile",
                table: "Returns");
        }
    }
}
