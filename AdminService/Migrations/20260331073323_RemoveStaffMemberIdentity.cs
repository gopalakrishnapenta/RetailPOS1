using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AdminService.Migrations
{
    /// <inheritdoc />
    public partial class RemoveStaffMemberIdentity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "StaffMembers");

            migrationBuilder.CreateTable(
                name: "StaffMembers",
                columns: table => new
                {
                    UserId = table.Column<int>(type: "int", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FullName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsAssigned = table.Column<bool>(type: "bit", nullable: false),
                    AssignedStoreId = table.Column<int>(type: "int", nullable: true),
                    AssignedRole = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RegisteredDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StaffMembers", x => x.UserId);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "StaffMembers");

            migrationBuilder.CreateTable(
                name: "StaffMembers",
                columns: table => new
                {
                    UserId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Email = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FullName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsAssigned = table.Column<bool>(type: "bit", nullable: false),
                    AssignedStoreId = table.Column<int>(type: "int", nullable: true),
                    AssignedRole = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RegisteredDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StaffMembers", x => x.UserId);
                });
        }
    }
}
