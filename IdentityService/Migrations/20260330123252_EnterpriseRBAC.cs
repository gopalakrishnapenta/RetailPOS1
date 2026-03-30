using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace IdentityService.Migrations
{
    /// <inheritdoc />
    public partial class EnterpriseRBAC : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. Create the new RBAC tables first
            migrationBuilder.CreateTable(
                name: "Permissions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Permissions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Roles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Roles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RolePermissions",
                columns: table => new
                {
                    RoleId = table.Column<int>(type: "int", nullable: false),
                    PermissionId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RolePermissions", x => new { x.RoleId, x.PermissionId });
                    table.ForeignKey(
                        name: "FK_RolePermissions_Permissions_PermissionId",
                        column: x => x.PermissionId,
                        principalTable: "Permissions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RolePermissions_Roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "Roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserStoreRoles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    StoreId = table.Column<int>(type: "int", nullable: true),
                    RoleId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserStoreRoles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserStoreRoles_Roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "Roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserStoreRoles_Stores_StoreId",
                        column: x => x.StoreId,
                        principalTable: "Stores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_UserStoreRoles_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            // 2. Seed basic Roles and Permissions
            migrationBuilder.InsertData(
                table: "Permissions",
                columns: new[] { "Id", "Code", "Description" },
                values: new object[,]
                {
                    { 1, "all:all", "Full System Access" },
                    { 2, "orders:create", "Create Sales Bills" },
                    { 3, "orders:read", "View Orders" },
                    { 4, "returns:approve", "Approve Refunds" },
                    { 5, "catalog:manage", "Edit Products/Categories" }
                });

            migrationBuilder.InsertData(
                table: "Roles",
                columns: new[] { "Id", "Name" },
                values: new object[,]
                {
                    { 1, "Admin" },
                    { 2, "StoreManager" },
                    { 3, "Cashier" }
                });

            migrationBuilder.InsertData(
                table: "RolePermissions",
                columns: new[] { "PermissionId", "RoleId" },
                values: new object[,]
                {
                    { 1, 1 },
                    { 2, 2 },
                    { 3, 2 },
                    { 4, 2 },
                    { 5, 2 },
                    { 2, 3 },
                    { 3, 3 }
                });

            // 3. --- DATA MIGRATION ---
            // Move legacy data to new UserStoreRoles table before columns are dropped
            migrationBuilder.Sql(@"
                INSERT INTO UserStoreRoles (UserId, StoreId, RoleId)
                SELECT 
                    Id, 
                    PrimaryStoreId, 
                    (CASE 
                        WHEN Role = 'Admin' THEN 1 
                        WHEN Role = 'StoreManager' THEN 2 
                        WHEN Role = 'Manager' THEN 2 
                        ELSE 3 
                     END) 
                FROM Users
            ");

            // 4. Drop old constraints and columns
            migrationBuilder.DropForeignKey(
                name: "FK_Users_Stores_PrimaryStoreId",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Users_PrimaryStoreId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "PrimaryStoreId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "Role",
                table: "Users");

            // 5. Cleanup existing seed data artifacts (if any)
            migrationBuilder.DeleteData(table: "Users", keyColumn: "Id", keyValue: 1);
            migrationBuilder.DeleteData(table: "Users", keyColumn: "Id", keyValue: 2);
            migrationBuilder.DeleteData(table: "Users", keyColumn: "Id", keyValue: 3);
            migrationBuilder.DeleteData(table: "Users", keyColumn: "Id", keyValue: 4);
            migrationBuilder.DeleteData(table: "Users", keyColumn: "Id", keyValue: 5);

            // 6. Refactor User table columns (nullable/length)
            migrationBuilder.AlterColumn<string>(
                name: "PasswordHash",
                table: "Users",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "EmployeeCode",
                table: "Users",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Email",
                table: "Users",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            // 7. Re-index
            migrationBuilder.CreateIndex(
                name: "IX_Permissions_Code",
                table: "Permissions",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RolePermissions_PermissionId",
                table: "RolePermissions",
                column: "PermissionId");

            migrationBuilder.CreateIndex(
                name: "IX_UserStoreRoles_RoleId",
                table: "UserStoreRoles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_UserStoreRoles_StoreId",
                table: "UserStoreRoles",
                column: "StoreId");

            migrationBuilder.CreateIndex(
                name: "IX_UserStoreRoles_UserId",
                table: "UserStoreRoles",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Reverse logic... (Omitted for brevity in this scratch script but should be correctly implemented in production)
            migrationBuilder.DropTable(name: "RolePermissions");
            migrationBuilder.DropTable(name: "UserStoreRoles");
            migrationBuilder.DropTable(name: "Permissions");
            migrationBuilder.DropTable(name: "Roles");
            
            migrationBuilder.AddColumn<int>(name: "PrimaryStoreId", table: "Users", type: "int", nullable: true);
            migrationBuilder.AddColumn<string>(name: "Role", table: "Users", type: "nvarchar(max)", nullable: false, defaultValue: "");
        }
    }
}
