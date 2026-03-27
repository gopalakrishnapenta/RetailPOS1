using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace CatalogService.Migrations
{
    /// <inheritdoc />
    public partial class SeedProducts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Products",
                columns: new[] { "Id", "Barcode", "CategoryId", "IsActive", "MRP", "Name", "ReorderLevel", "SellingPrice", "Sku", "StockQuantity", "StoreId", "TaxCode" },
                values: new object[,]
                {
                    { 1, "", 1, true, 50000m, "Laptop", 10, 45000m, "SKU-001", 10, 1, "" },
                    { 2, "", 1, true, 3000m, "Mechanical Keyboard", 10, 2500m, "SKU-002", 50, 1, "" },
                    { 3, "", 1, true, 1000m, "Mouse", 10, 800m, "SKU-003", 100, 1, "" },
                    { 4, "", 2, true, 50m, "Milk", 10, 48m, "SKU-004", 500, 1, "" },
                    { 5, "", 2, true, 40m, "Bread", 10, 38m, "SKU-005", 200, 1, "" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 5);
        }
    }
}
