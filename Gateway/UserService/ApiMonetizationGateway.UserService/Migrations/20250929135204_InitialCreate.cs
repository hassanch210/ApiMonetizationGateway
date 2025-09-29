using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace ApiMonetizationGateway.UserService.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Tiers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    MonthlyQuota = table.Column<long>(type: "bigint", nullable: false),
                    RateLimit = table.Column<int>(type: "int", nullable: false),
                    MonthlyPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tiers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Email = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    FirstName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    LastName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ApiKey = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    TierId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Users_Tiers_TierId",
                        column: x => x.TierId,
                        principalTable: "Tiers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ApiUsages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    Endpoint = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    HttpMethod = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    ResponseStatusCode = table.Column<int>(type: "int", nullable: false),
                    ResponseTimeMs = table.Column<long>(type: "bigint", nullable: false),
                    IpAddress = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    UserAgent = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    RequestTimestamp = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Metadata = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApiUsages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ApiUsages_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MonthlyUsageSummaries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    Year = table.Column<int>(type: "int", nullable: false),
                    Month = table.Column<int>(type: "int", nullable: false),
                    TotalRequests = table.Column<long>(type: "bigint", nullable: false),
                    SuccessfulRequests = table.Column<long>(type: "bigint", nullable: false),
                    FailedRequests = table.Column<long>(type: "bigint", nullable: false),
                    CalculatedCost = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TierPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ProcessedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsBilled = table.Column<bool>(type: "bit", nullable: false),
                    EndpointUsageJson = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MonthlyUsageSummaries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MonthlyUsageSummaries_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Tiers",
                columns: new[] { "Id", "CreatedAt", "Description", "IsActive", "MonthlyPrice", "MonthlyQuota", "Name", "RateLimit", "UpdatedAt" },
                values: new object[,]
                {
                    { 1, new DateTime(2025, 9, 29, 13, 52, 3, 23, DateTimeKind.Utc).AddTicks(5164), "Free tier with basic limits", true, 0m, 100L, "Free", 2, new DateTime(2025, 9, 29, 13, 52, 3, 23, DateTimeKind.Utc).AddTicks(5165) },
                    { 2, new DateTime(2025, 9, 29, 13, 52, 3, 23, DateTimeKind.Utc).AddTicks(5170), "Professional tier with higher limits", true, 50m, 100000L, "Pro", 10, new DateTime(2025, 9, 29, 13, 52, 3, 23, DateTimeKind.Utc).AddTicks(5170) }
                });

            migrationBuilder.CreateIndex(
                name: "IX_ApiUsages_RequestTimestamp",
                table: "ApiUsages",
                column: "RequestTimestamp");

            migrationBuilder.CreateIndex(
                name: "IX_ApiUsages_UserId_RequestTimestamp",
                table: "ApiUsages",
                columns: new[] { "UserId", "RequestTimestamp" });

            migrationBuilder.CreateIndex(
                name: "IX_MonthlyUsageSummaries_UserId_Year_Month",
                table: "MonthlyUsageSummaries",
                columns: new[] { "UserId", "Year", "Month" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Tiers_Name",
                table: "Tiers",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_ApiKey",
                table: "Users",
                column: "ApiKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_TierId",
                table: "Users",
                column: "TierId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ApiUsages");

            migrationBuilder.DropTable(
                name: "MonthlyUsageSummaries");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "Tiers");
        }
    }
}
