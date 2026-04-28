using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PaymentService.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Payments]') AND type in (N'U'))
                BEGIN
                    CREATE TABLE [Payments] (
                        [Id] int NOT NULL IDENTITY(1, 1),
                        [BillId] int NOT NULL,
                        [PaymentMode] nvarchar(20) NOT NULL,
                        [Amount] decimal(18,2) NOT NULL,
                        [ReferenceNumber] nvarchar(100) NOT NULL,
                        [StoreId] int NOT NULL,
                        CONSTRAINT [PK_Payments] PRIMARY KEY ([Id])
                    );
                END
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Payments]') AND type in (N'U'))
                BEGIN
                    DROP TABLE [Payments];
                END
            ");
        }
    }
}
