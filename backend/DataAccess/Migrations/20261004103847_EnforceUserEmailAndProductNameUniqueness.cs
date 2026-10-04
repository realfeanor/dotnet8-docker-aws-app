using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class EnforceUserEmailAndProductNameUniqueness : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Fail clearly without truncating or deleting existing records.
            migrationBuilder.Sql(@"
IF EXISTS (SELECT 1 FROM Users WHERE DATALENGTH(Email) > 640)
    THROW 50001, 'Cannot add email index: an existing email exceeds 320 characters.', 1;
IF EXISTS (SELECT 1 FROM Products WHERE DATALENGTH(ProductName) > 60)
    THROW 50002, 'Cannot add product-name index: an existing name exceeds 30 characters.', 1;
IF EXISTS (SELECT Email FROM Users GROUP BY Email HAVING COUNT(*) > 1)
    THROW 50003, 'Cannot add email index: duplicate emails must be resolved first.', 1;
IF EXISTS (SELECT ProductName FROM Products GROUP BY ProductName HAVING COUNT(*) > 1)
    THROW 50004, 'Cannot add product-name index: duplicate names must be resolved first.', 1;");

            migrationBuilder.AlterColumn<string>(
                name: "Email",
                table: "Users",
                type: "nvarchar(320)",
                maxLength: 320,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "ProductName",
                table: "Products",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Products_ProductName",
                table: "Products",
                column: "ProductName",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Users_Email",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Products_ProductName",
                table: "Products");

            migrationBuilder.AlterColumn<string>(
                name: "Email",
                table: "Users",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(320)",
                oldMaxLength: 320);

            migrationBuilder.AlterColumn<string>(
                name: "ProductName",
                table: "Products",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(30)",
                oldMaxLength: 30);
        }
    }
}
