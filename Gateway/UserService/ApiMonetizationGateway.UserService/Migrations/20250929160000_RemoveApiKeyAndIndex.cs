using Microsoft.EntityFrameworkCore.Migrations;

namespace ApiMonetizationGateway.UserService.Migrations
{
    public partial class RemoveApiKeyAndIndex : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Drop unique index on ApiKey if exists
            try
            {
                migrationBuilder.DropIndex(
                    name: "IX_Users_ApiKey",
                    table: "Users");
            }
            catch
            {
                // ignore if index already missing
            }

            // Make ApiKey nullable
            migrationBuilder.AlterColumn<string>(
                name: "ApiKey",
                table: "Users",
                type: "nvarchar(450)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Revert ApiKey to non-nullable (may fail if nulls present)
            migrationBuilder.AlterColumn<string>(
                name: "ApiKey",
                table: "Users",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldNullable: true);

            // Re-create unique index on ApiKey
            migrationBuilder.CreateIndex(
                name: "IX_Users_ApiKey",
                table: "Users",
                column: "ApiKey",
                unique: true);
        }
    }
}


