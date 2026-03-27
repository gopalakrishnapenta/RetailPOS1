using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace CatalogService.Migrations
{
    /// <inheritdoc />
    public partial class SeedCategories : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                SET IDENTITY_INSERT Categories ON;
                IF NOT EXISTS (SELECT 1 FROM Categories WHERE Id = 1) INSERT INTO Categories (Id, Name, Description, IsActive) VALUES (1, 'Electronics', 'Gadgets', 1);
                IF NOT EXISTS (SELECT 1 FROM Categories WHERE Id = 2) INSERT INTO Categories (Id, Name, Description, IsActive) VALUES (2, 'Grocery', 'Daily essentials', 1);
                IF NOT EXISTS (SELECT 1 FROM Categories WHERE Id = 3) INSERT INTO Categories (Id, Name, Description, IsActive) VALUES (3, 'Beverages', 'Drinks', 1);
                IF NOT EXISTS (SELECT 1 FROM Categories WHERE Id = 4) INSERT INTO Categories (Id, Name, Description, IsActive) VALUES (4, 'Fruits', 'Fresh fruits', 1);
                IF NOT EXISTS (SELECT 1 FROM Categories WHERE Id = 5) INSERT INTO Categories (Id, Name, Description, IsActive) VALUES (5, 'Veggies', 'Fresh vegetables', 1);
                SET IDENTITY_INSERT Categories OFF;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DELETE FROM Categories WHERE Id IN (1, 2, 3, 4, 5)");
        }
    }
}
