using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IdentityService.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Tables already exist in the database.
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Do nothing as we didn't create anything.
        }
    }
}
