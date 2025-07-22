using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OptionAnalysisTool.Common.Migrations
{
    /// <inheritdoc />
    public partial class EnhancedCircuitLimitTrackerWithIndexOHLC : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CircuitLimitTracker_SeverityLevel",
                table: "CircuitLimitTrackers");

            migrationBuilder.DropColumn(
                name: "IsBreachAlert",
                table: "CircuitLimitTrackers");

            migrationBuilder.DropColumn(
                name: "SeverityLevel",
                table: "CircuitLimitTrackers");

            migrationBuilder.RenameColumn(
                name: "UpperLimitChangePercent",
                table: "CircuitLimitTrackers",
                newName: "UnderlyingUpperCircuitLimit");

            migrationBuilder.RenameColumn(
                name: "RangeChangePercent",
                table: "CircuitLimitTrackers",
                newName: "UnderlyingPercentageChange");

            migrationBuilder.RenameColumn(
                name: "LowerLimitChangePercent",
                table: "CircuitLimitTrackers",
                newName: "UnderlyingOpen");

            migrationBuilder.AddColumn<DateTime>(
                name: "LastUpdated",
                table: "SpotData",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "ChangeReason",
                table: "IntradayOptionSnapshots",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "OHLCDate",
                table: "IntradayOptionSnapshots",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "TradingSymbol",
                table: "IntradayOptionSnapshots",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "UnderlyingChange",
                table: "CircuitLimitTrackers",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "UnderlyingCircuitStatus",
                table: "CircuitLimitTrackers",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "UnderlyingClose",
                table: "CircuitLimitTrackers",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "UnderlyingHigh",
                table: "CircuitLimitTrackers",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "UnderlyingLow",
                table: "CircuitLimitTrackers",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "UnderlyingLowerCircuitLimit",
                table: "CircuitLimitTrackers",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<long>(
                name: "UnderlyingVolume",
                table: "CircuitLimitTrackers",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.CreateTable(
                name: "AuthenticationTokens",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AccessToken = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    ApiKey = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ApiSecret = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    UserId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    UserName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastUsedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    Source = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    SourceIpAddress = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Metadata = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuthenticationTokens", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Instruments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    InstrumentToken = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ExchangeToken = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    TradingSymbol = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Strike = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Expiry = table.Column<DateTime>(type: "datetime2", nullable: true),
                    InstrumentType = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Segment = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Exchange = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Instruments", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CircuitLimitTracker_DetectedAt",
                table: "CircuitLimitTrackers",
                column: "DetectedAt");

            migrationBuilder.CreateIndex(
                name: "IX_AuthenticationTokens_ApiKey_IsActive",
                table: "AuthenticationTokens",
                columns: new[] { "ApiKey", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_AuthenticationTokens_CreatedAt",
                table: "AuthenticationTokens",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_AuthenticationTokens_ExpiresAt_IsActive",
                table: "AuthenticationTokens",
                columns: new[] { "ExpiresAt", "IsActive" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AuthenticationTokens");

            migrationBuilder.DropTable(
                name: "Instruments");

            migrationBuilder.DropIndex(
                name: "IX_CircuitLimitTracker_DetectedAt",
                table: "CircuitLimitTrackers");

            migrationBuilder.DropColumn(
                name: "LastUpdated",
                table: "SpotData");

            migrationBuilder.DropColumn(
                name: "ChangeReason",
                table: "IntradayOptionSnapshots");

            migrationBuilder.DropColumn(
                name: "OHLCDate",
                table: "IntradayOptionSnapshots");

            migrationBuilder.DropColumn(
                name: "TradingSymbol",
                table: "IntradayOptionSnapshots");

            migrationBuilder.DropColumn(
                name: "UnderlyingChange",
                table: "CircuitLimitTrackers");

            migrationBuilder.DropColumn(
                name: "UnderlyingCircuitStatus",
                table: "CircuitLimitTrackers");

            migrationBuilder.DropColumn(
                name: "UnderlyingClose",
                table: "CircuitLimitTrackers");

            migrationBuilder.DropColumn(
                name: "UnderlyingHigh",
                table: "CircuitLimitTrackers");

            migrationBuilder.DropColumn(
                name: "UnderlyingLow",
                table: "CircuitLimitTrackers");

            migrationBuilder.DropColumn(
                name: "UnderlyingLowerCircuitLimit",
                table: "CircuitLimitTrackers");

            migrationBuilder.DropColumn(
                name: "UnderlyingVolume",
                table: "CircuitLimitTrackers");

            migrationBuilder.RenameColumn(
                name: "UnderlyingUpperCircuitLimit",
                table: "CircuitLimitTrackers",
                newName: "UpperLimitChangePercent");

            migrationBuilder.RenameColumn(
                name: "UnderlyingPercentageChange",
                table: "CircuitLimitTrackers",
                newName: "RangeChangePercent");

            migrationBuilder.RenameColumn(
                name: "UnderlyingOpen",
                table: "CircuitLimitTrackers",
                newName: "LowerLimitChangePercent");

            migrationBuilder.AddColumn<bool>(
                name: "IsBreachAlert",
                table: "CircuitLimitTrackers",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "SeverityLevel",
                table: "CircuitLimitTrackers",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_CircuitLimitTracker_SeverityLevel",
                table: "CircuitLimitTrackers",
                column: "SeverityLevel");
        }
    }
}
