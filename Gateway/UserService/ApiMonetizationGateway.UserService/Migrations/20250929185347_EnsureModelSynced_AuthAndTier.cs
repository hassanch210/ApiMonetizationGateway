using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ApiMonetizationGateway.UserService.Migrations
{
    /// <inheritdoc />
    public partial class EnsureModelSynced_AuthAndTier : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Tiers",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 9, 29, 18, 53, 46, 947, DateTimeKind.Utc).AddTicks(9426), new DateTime(2025, 9, 29, 18, 53, 46, 947, DateTimeKind.Utc).AddTicks(9427) });

            migrationBuilder.UpdateData(
                table: "Tiers",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 9, 29, 18, 53, 46, 947, DateTimeKind.Utc).AddTicks(9431), new DateTime(2025, 9, 29, 18, 53, 46, 947, DateTimeKind.Utc).AddTicks(9431) });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Tiers",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 9, 29, 18, 22, 32, 943, DateTimeKind.Utc).AddTicks(6543), new DateTime(2025, 9, 29, 18, 22, 32, 943, DateTimeKind.Utc).AddTicks(6544) });

            migrationBuilder.UpdateData(
                table: "Tiers",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "CreatedAt", "UpdatedAt" },
                values: new object[] { new DateTime(2025, 9, 29, 18, 22, 32, 943, DateTimeKind.Utc).AddTicks(6548), new DateTime(2025, 9, 29, 18, 22, 32, 943, DateTimeKind.Utc).AddTicks(6548) });
        }
    }
}
