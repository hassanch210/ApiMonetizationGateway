using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ApiMonetizationGateway.UserService.Migrations
{
    /// <inheritdoc />
    public partial class AddJWTAuthAndUserTier : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PasswordHash",
                table: "Users",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "UserTiers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    TierId = table.Column<int>(type: "int", nullable: false),
                    AssignedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedOn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserTiers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserTiers_Tiers_TierId",
                        column: x => x.TierId,
                        principalTable: "Tiers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UserTiers_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

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
                name: "IX_UserTiers_TierId",
                table: "UserTiers",
                column: "TierId");

            migrationBuilder.CreateIndex(
                name: "IX_UserTiers_UserId_TierId_IsActive",
                table: "UserTiers",
                columns: new[] { "UserId", "TierId", "IsActive" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserTiers");

            migrationBuilder.DropColumn(
                name: "PasswordHash",
                table: "Users");

            migrationBuilder.UpdateData(
                table: "Tiers",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 9, 29, 13, 52, 3, 23, DateTimeKind.Utc).AddTicks(5164), new DateTime(2025, 9, 29, 13, 52, 3, 23, DateTimeKind.Utc).AddTicks(5165) });

            migrationBuilder.UpdateData(
                table: "Tiers",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 9, 29, 13, 52, 3, 23, DateTimeKind.Utc).AddTicks(5170), new DateTime(2025, 9, 29, 13, 52, 3, 23, DateTimeKind.Utc).AddTicks(5170) });
        }
    }
}
