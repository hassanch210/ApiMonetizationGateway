using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ApiMonetizationGateway.UserService.Migrations
{
    /// <inheritdoc />
    public partial class SnapshotSync_RemoveTierFK : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
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
    }
}
