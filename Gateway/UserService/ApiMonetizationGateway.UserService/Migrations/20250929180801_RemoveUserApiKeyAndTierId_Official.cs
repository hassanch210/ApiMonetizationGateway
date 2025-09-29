using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ApiMonetizationGateway.UserService.Migrations
{
    /// <inheritdoc />
    public partial class RemoveUserApiKeyAndTierId_Official : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Users_Tiers_TierId",
                table: "Users");

            // Drop index on ApiKey before dropping the column
            migrationBuilder.DropIndex(
                name: "IX_Users_ApiKey",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "ApiKey",
                table: "Users");

            // Drop TierId column entirely
            migrationBuilder.DropIndex(
                name: "IX_Users_TierId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "TierId",
                table: "Users");

            migrationBuilder.UpdateData(
                table: "Tiers",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 9, 29, 18, 7, 59, 515, DateTimeKind.Utc).AddTicks(249), new DateTime(2025, 9, 29, 18, 7, 59, 515, DateTimeKind.Utc).AddTicks(250) });

            migrationBuilder.UpdateData(
                table: "Tiers",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 9, 29, 18, 7, 59, 515, DateTimeKind.Utc).AddTicks(257), new DateTime(2025, 9, 29, 18, 7, 59, 515, DateTimeKind.Utc).AddTicks(257) });

        }

        /// <inheritdoc />
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

            migrationBuilder.UpdateData(
                table: "Tiers",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 9, 29, 15, 1, 15, 409, DateTimeKind.Utc).AddTicks(9826), new DateTime(2025, 9, 29, 15, 1, 15, 409, DateTimeKind.Utc).AddTicks(9826) });

            migrationBuilder.UpdateData(
                table: "Tiers",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 9, 29, 15, 1, 15, 409, DateTimeKind.Utc).AddTicks(9831), new DateTime(2025, 9, 29, 15, 1, 15, 409, DateTimeKind.Utc).AddTicks(9832) });

            migrationBuilder.CreateIndex(
                name: "IX_Users_ApiKey",
                table: "Users",
                column: "ApiKey",
                unique: true,
                filter: "[ApiKey] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Users_TierId",
                table: "Users",
                column: "TierId");

            migrationBuilder.AddForeignKey(
                name: "FK_Users_Tiers_TierId",
                table: "Users",
                column: "TierId",
                principalTable: "Tiers",
                principalColumn: "Id");
        }
    }
}
