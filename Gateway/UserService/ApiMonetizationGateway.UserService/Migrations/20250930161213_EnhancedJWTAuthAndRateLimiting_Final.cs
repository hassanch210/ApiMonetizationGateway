using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ApiMonetizationGateway.UserService.Migrations
{
    /// <inheritdoc />
    public partial class EnhancedJWTAuthAndRateLimiting_Final : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Tiers",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 9, 30, 16, 12, 11, 685, DateTimeKind.Utc).AddTicks(1178), new DateTime(2025, 9, 30, 16, 12, 11, 685, DateTimeKind.Utc).AddTicks(1179) });

            migrationBuilder.UpdateData(
                table: "Tiers",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 9, 30, 16, 12, 11, 685, DateTimeKind.Utc).AddTicks(1183), new DateTime(2025, 9, 30, 16, 12, 11, 685, DateTimeKind.Utc).AddTicks(1184) });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Tiers",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 9, 29, 20, 21, 55, 152, DateTimeKind.Utc).AddTicks(1909), new DateTime(2025, 9, 29, 20, 21, 55, 152, DateTimeKind.Utc).AddTicks(1910) });

            migrationBuilder.UpdateData(
                table: "Tiers",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 9, 29, 20, 21, 55, 152, DateTimeKind.Utc).AddTicks(1915), new DateTime(2025, 9, 29, 20, 21, 55, 152, DateTimeKind.Utc).AddTicks(1915) });
        }
    }
}
