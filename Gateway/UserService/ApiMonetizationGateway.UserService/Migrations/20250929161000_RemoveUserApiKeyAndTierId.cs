using Microsoft.EntityFrameworkCore.Migrations;

namespace ApiMonetizationGateway.UserService.Migrations
{
    public partial class RemoveUserApiKeyAndTierId : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Drop FK and index on TierId if exists
            try { migrationBuilder.DropForeignKey(name: "FK_Users_Tiers_TierId", table: "Users"); } catch {}
            try { migrationBuilder.DropIndex(name: "IX_Users_TierId", table: "Users"); } catch {}

            // Drop ApiKey index if exists
            try { migrationBuilder.DropIndex(name: "IX_Users_ApiKey", table: "Users"); } catch {}

            // Drop columns
            try { migrationBuilder.DropColumn(name: "ApiKey", table: "Users"); } catch {}
            try { migrationBuilder.DropColumn(name: "TierId", table: "Users"); } catch {}
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ApiKey",
                table: "Users",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TierId",
                table: "Users",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Users_TierId",
                table: "Users",
                column: "TierId");

            migrationBuilder.AddForeignKey(
                name: "FK_Users_Tiers_TierId",
                table: "Users",
                column: "TierId",
                principalTable: "Tiers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}


