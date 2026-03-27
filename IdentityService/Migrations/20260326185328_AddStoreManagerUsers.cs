using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace IdentityService.Migrations
{
    /// <inheritdoc />
    public partial class AddStoreManagerUsers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("IF NOT EXISTS (SELECT 1 FROM Stores WHERE Id = 1) " +
                "INSERT INTO Stores (Id, IsActive, Location, Name, StoreCode) VALUES (1, 1, 'Downtown', 'Nexus Main Store', 'S001')");

            migrationBuilder.Sql("SET IDENTITY_INSERT Users ON");
            
            migrationBuilder.Sql("IF NOT EXISTS (SELECT 1 FROM Users WHERE Id = 1) " +
                "INSERT INTO Users (Id, Email, EmployeeCode, PasswordHash, PrimaryStoreId, Role) VALUES (1, 'admin@nexus.com', 'E001', 'Admin@123', 1, 'Admin')");
            
            migrationBuilder.Sql("IF NOT EXISTS (SELECT 1 FROM Users WHERE Id = 2) " +
                "INSERT INTO Users (Id, Email, EmployeeCode, PasswordHash, PrimaryStoreId, Role) VALUES (2, 'admin@gmail.com', 'E003', 'admin', 1, 'Admin')");

            migrationBuilder.Sql("IF NOT EXISTS (SELECT 1 FROM Users WHERE Id = 3) " +
                "INSERT INTO Users (Id, Email, EmployeeCode, PasswordHash, PrimaryStoreId, Role) VALUES (3, 'manager@nexus.com', 'E004', 'Manager@123', 1, 'Manager')");

            migrationBuilder.Sql("IF NOT EXISTS (SELECT 1 FROM Users WHERE Id = 4) " +
                "INSERT INTO Users (Id, Email, EmployeeCode, PasswordHash, PrimaryStoreId, Role) VALUES (4, 'storemanager@nexus.com', 'E005', 'SManager@123', 1, 'StoreManager')");

            migrationBuilder.Sql("IF NOT EXISTS (SELECT 1 FROM Users WHERE Id = 5) " +
                "INSERT INTO Users (Id, Email, EmployeeCode, PasswordHash, PrimaryStoreId, Role) VALUES (5, 'cashier@nexus.com', 'E002', 'Cashier@123', 1, 'Cashier')");

            migrationBuilder.Sql("SET IDENTITY_INSERT Users OFF");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 5);

            migrationBuilder.DeleteData(
                table: "Stores",
                keyColumn: "Id",
                keyValue: 1);
        }
    }
}
