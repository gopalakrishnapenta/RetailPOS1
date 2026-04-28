using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CatalogService.Data.Migrations
{
    /// <inheritdoc />
    public partial class SyncModelChanges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "Name", "Sku", "StoreId" },
                values: new object[] { "Laptop (S1)", "SKU-S1-001", 1 });

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "Name", "Sku", "StoreId" },
                values: new object[] { "Mechanical Keyboard (S1)", "SKU-S1-002", 1 });

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "Name", "Sku", "StoreId" },
                values: new object[] { "Mouse (S2)", "SKU-S2-003", 2 });

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "Name", "Sku", "StoreId" },
                values: new object[] { "Milk (S2)", "SKU-S2-004", 2 });

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 5,
                columns: new[] { "Name", "Sku", "StoreId" },
                values: new object[] { "Bread (S2)", "SKU-S2-005", 2 });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "Name", "Sku", "StoreId" },
                values: new object[] { "Laptop", "SKU-001", 0 });

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "Name", "Sku", "StoreId" },
                values: new object[] { "Mechanical Keyboard", "SKU-002", 0 });

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "Name", "Sku", "StoreId" },
                values: new object[] { "Mouse", "SKU-003", 0 });

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "Name", "Sku", "StoreId" },
                values: new object[] { "Milk", "SKU-004", 0 });

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 5,
                columns: new[] { "Name", "Sku", "StoreId" },
                values: new object[] { "Bread", "SKU-005", 0 });
        }
    }
}
