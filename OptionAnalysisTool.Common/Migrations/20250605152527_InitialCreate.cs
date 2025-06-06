using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OptionAnalysisTool.Common.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CircuitBreakers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Symbol = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    UnderlyingSymbol = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    StrikePrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    OptionType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ExpiryDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TradingDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LowerCircuitLimit = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    UpperCircuitLimit = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    IsBreached = table.Column<bool>(type: "bit", nullable: false),
                    BreachCount = table.Column<int>(type: "int", nullable: false),
                    FirstBreachTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastBreachTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    BreachType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BreachReason = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastUpdated = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CircuitBreakers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CircuitLimitChanges",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    InstrumentToken = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Symbol = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    OldLowerCircuitLimit = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    NewLowerCircuitLimit = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    OldUpperCircuitLimit = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    NewUpperCircuitLimit = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    LastPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Timestamp = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ChangeReason = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    InstrumentType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    StrikePrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    ExpiryDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CircuitLimitChanges", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CircuitLimitTrackers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    InstrumentToken = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Symbol = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    UnderlyingSymbol = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    StrikePrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    OptionType = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    ExpiryDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PreviousLowerLimit = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    NewLowerLimit = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    PreviousUpperLimit = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    NewUpperLimit = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    LowerLimitChangePercent = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    UpperLimitChangePercent = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    RangeChangePercent = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CurrentPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    UnderlyingPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Volume = table.Column<long>(type: "bigint", nullable: false),
                    OpenInterest = table.Column<long>(type: "bigint", nullable: false),
                    DetectedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastUpdated = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsBreachAlert = table.Column<bool>(type: "bit", nullable: false),
                    SeverityLevel = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ChangeReason = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsValidData = table.Column<bool>(type: "bit", nullable: false),
                    ValidationMessage = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CircuitLimitTrackers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DailyOptionContracts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    InstrumentToken = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Symbol = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    UnderlyingSymbol = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    StrikePrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    OptionType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ExpiryDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TradingDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LowerCircuitLimit = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    UpperCircuitLimit = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CircuitLimitBreached = table.Column<bool>(type: "bit", nullable: false),
                    CircuitBreachCount = table.Column<int>(type: "int", nullable: false),
                    CircuitBreachTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CircuitBreachType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DayOpen = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    DayHigh = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    DayLow = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    DayClose = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    PreviousClose = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalVolume = table.Column<long>(type: "bigint", nullable: false),
                    OpenInterest = table.Column<long>(type: "bigint", nullable: false),
                    ImpliedVolatility = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    DataQualityNotes = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastUpdated = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DailyOptionContracts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DataDownloadStatuses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Symbol = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    DataType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastUpdated = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TotalRecords = table.Column<int>(type: "int", nullable: false),
                    ProcessedRecords = table.Column<int>(type: "int", nullable: false),
                    FailedRecords = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ErrorMessage = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsCompleted = table.Column<bool>(type: "bit", nullable: false),
                    HasErrors = table.Column<bool>(type: "bit", nullable: false),
                    IsInProgress = table.Column<bool>(type: "bit", nullable: false),
                    LastDownloadTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastSuccessfulDownload = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastProcessedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastDownloadedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Exchange = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DataDownloadStatuses", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "HistoricalOptionData",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    InstrumentToken = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Symbol = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    UnderlyingSymbol = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    StrikePrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    OptionType = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    ExpiryDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TradingDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Open = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    High = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Low = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Close = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Change = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    PercentageChange = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Volume = table.Column<long>(type: "bigint", nullable: false),
                    OpenInterest = table.Column<long>(type: "bigint", nullable: false),
                    OIChange = table.Column<long>(type: "bigint", nullable: false),
                    LowerCircuitLimit = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    UpperCircuitLimit = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CircuitLimitChanged = table.Column<bool>(type: "bit", nullable: false),
                    ImpliedVolatility = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Delta = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    Gamma = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    Theta = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    Vega = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    UnderlyingClose = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    UnderlyingChange = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CapturedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastUpdated = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsValidData = table.Column<bool>(type: "bit", nullable: false),
                    ValidationMessage = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DataSource = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HistoricalOptionData", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "HistoricalPrices",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Symbol = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Exchange = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    InstrumentToken = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Open = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    High = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Low = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Close = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Volume = table.Column<long>(type: "bigint", nullable: false),
                    OpenInterest = table.Column<long>(type: "bigint", nullable: false),
                    TimeStamp = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HistoricalPrices", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "IndexSnapshots",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Symbol = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    LastPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Open = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    High = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Low = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Close = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Volume = table.Column<long>(type: "bigint", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastUpdated = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IndexSnapshots", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "IntradayOptionSnapshots",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    InstrumentToken = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Symbol = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    UnderlyingSymbol = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    StrikePrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    OptionType = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    ExpiryDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Open = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    High = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Low = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Close = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Change = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Volume = table.Column<long>(type: "bigint", nullable: false),
                    OpenInterest = table.Column<long>(type: "bigint", nullable: false),
                    LowerCircuitLimit = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    UpperCircuitLimit = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CircuitLimitStatus = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ImpliedVolatility = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Timestamp = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CaptureTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastUpdated = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsValidData = table.Column<bool>(type: "bit", nullable: false),
                    ValidationMessage = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TradingStatus = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IntradayOptionSnapshots", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OptionContracts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    InstrumentToken = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Symbol = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    UnderlyingSymbol = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    StrikePrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    OptionType = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    ExpiryDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Open = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    High = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Low = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Close = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Change = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Volume = table.Column<int>(type: "int", nullable: false),
                    OpenInterest = table.Column<int>(type: "int", nullable: false),
                    ImpliedVolatility = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Timestamp = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastUpdated = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsLiveData = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OptionContracts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OptionMonitoringStats",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Symbol = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    UnderlyingSymbol = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    OptionType = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Timestamp = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsMarketOpen = table.Column<bool>(type: "bit", nullable: false),
                    ActiveContractsCount = table.Column<int>(type: "int", nullable: false),
                    NewContractsFound = table.Column<int>(type: "int", nullable: false),
                    SnapshotsSaved = table.Column<int>(type: "int", nullable: false),
                    ErrorCount = table.Column<int>(type: "int", nullable: false),
                    LastError = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AverageProcessingTime = table.Column<double>(type: "float", nullable: false),
                    NextUpdateTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastUpdated = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OptionMonitoringStats", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Quotes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    InstrumentToken = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    LastPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Open = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    High = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Low = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Close = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    PreviousClose = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Change = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    PercentageChange = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Volume = table.Column<long>(type: "bigint", nullable: false),
                    OpenInterest = table.Column<long>(type: "bigint", nullable: false),
                    LastQuantity = table.Column<int>(type: "int", nullable: false),
                    AveragePrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    BuyQuantity = table.Column<int>(type: "int", nullable: false),
                    SellQuantity = table.Column<int>(type: "int", nullable: false),
                    LowerCircuitLimit = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    UpperCircuitLimit = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    ImpliedVolatility = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Delta = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    Gamma = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    Theta = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    Vega = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    BidPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    AskPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    BidQuantity = table.Column<long>(type: "bigint", nullable: false),
                    AskQuantity = table.Column<long>(type: "bigint", nullable: false),
                    MarketDepthJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TimeStamp = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CaptureTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastUpdated = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsValidData = table.Column<bool>(type: "bit", nullable: false),
                    ValidationMessage = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Quotes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SpotData",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Symbol = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Exchange = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    LastPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Open = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    High = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Low = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Close = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Change = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    PercentageChange = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Volume = table.Column<long>(type: "bigint", nullable: false),
                    LowerCircuitLimit = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    UpperCircuitLimit = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CircuitStatus = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CapturedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsValidData = table.Column<bool>(type: "bit", nullable: false),
                    ValidationMessage = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SpotData", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "StockHistoricalPrices",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Symbol = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Exchange = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Open = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    High = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Low = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Close = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Volume = table.Column<long>(type: "bigint", nullable: false),
                    TimeStamp = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockHistoricalPrices", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CircuitBreakers_Symbol_TradingDate",
                table: "CircuitBreakers",
                columns: new[] { "Symbol", "TradingDate" });

            migrationBuilder.CreateIndex(
                name: "IX_CircuitLimitChanges_InstrumentToken_Timestamp",
                table: "CircuitLimitChanges",
                columns: new[] { "InstrumentToken", "Timestamp" });

            migrationBuilder.CreateIndex(
                name: "IX_CircuitLimitChanges_Symbol_Timestamp",
                table: "CircuitLimitChanges",
                columns: new[] { "Symbol", "Timestamp" });

            migrationBuilder.CreateIndex(
                name: "IX_CircuitLimitTracker_InstrumentToken_DetectedAt",
                table: "CircuitLimitTrackers",
                columns: new[] { "InstrumentToken", "DetectedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_CircuitLimitTracker_SeverityLevel",
                table: "CircuitLimitTrackers",
                column: "SeverityLevel");

            migrationBuilder.CreateIndex(
                name: "IX_CircuitLimitTracker_Symbol_DetectedAt",
                table: "CircuitLimitTrackers",
                columns: new[] { "Symbol", "DetectedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_CircuitLimitTracker_UnderlyingSymbol_DetectedAt",
                table: "CircuitLimitTrackers",
                columns: new[] { "UnderlyingSymbol", "DetectedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_DailyOptionContracts_Symbol_TradingDate",
                table: "DailyOptionContracts",
                columns: new[] { "Symbol", "TradingDate" });

            migrationBuilder.CreateIndex(
                name: "IX_DailyOptionContracts_UnderlyingSymbol_TradingDate",
                table: "DailyOptionContracts",
                columns: new[] { "UnderlyingSymbol", "TradingDate" });

            migrationBuilder.CreateIndex(
                name: "IX_DataDownloadStatuses_Symbol_Exchange",
                table: "DataDownloadStatuses",
                columns: new[] { "Symbol", "Exchange" });

            migrationBuilder.CreateIndex(
                name: "IX_HistoricalOptionData_Symbol_TradingDate",
                table: "HistoricalOptionData",
                columns: new[] { "Symbol", "TradingDate" });

            migrationBuilder.CreateIndex(
                name: "IX_HistoricalOptionData_UnderlyingSymbol_ExpiryDate_StrikePrice_OptionType",
                table: "HistoricalOptionData",
                columns: new[] { "UnderlyingSymbol", "ExpiryDate", "StrikePrice", "OptionType" });

            migrationBuilder.CreateIndex(
                name: "IX_HistoricalOptionData_UnderlyingSymbol_TradingDate_OptionType",
                table: "HistoricalOptionData",
                columns: new[] { "UnderlyingSymbol", "TradingDate", "OptionType" });

            migrationBuilder.CreateIndex(
                name: "IX_HistoricalPrices_Symbol_Date",
                table: "HistoricalPrices",
                columns: new[] { "Symbol", "Date" });

            migrationBuilder.CreateIndex(
                name: "IX_IndexSnapshots_Symbol_Timestamp",
                table: "IndexSnapshots",
                columns: new[] { "Symbol", "Timestamp" });

            migrationBuilder.CreateIndex(
                name: "IX_IntradayOptionSnapshots_Symbol_Timestamp",
                table: "IntradayOptionSnapshots",
                columns: new[] { "Symbol", "Timestamp" });

            migrationBuilder.CreateIndex(
                name: "IX_IntradayOptionSnapshots_UnderlyingSymbol_Timestamp",
                table: "IntradayOptionSnapshots",
                columns: new[] { "UnderlyingSymbol", "Timestamp" });

            migrationBuilder.CreateIndex(
                name: "IX_OptionContracts_Symbol_Timestamp",
                table: "OptionContracts",
                columns: new[] { "Symbol", "Timestamp" });

            migrationBuilder.CreateIndex(
                name: "IX_OptionMonitoringStats_Symbol_Timestamp",
                table: "OptionMonitoringStats",
                columns: new[] { "Symbol", "Timestamp" });

            migrationBuilder.CreateIndex(
                name: "IX_Quotes_InstrumentToken_TimeStamp",
                table: "Quotes",
                columns: new[] { "InstrumentToken", "TimeStamp" });

            migrationBuilder.CreateIndex(
                name: "IX_Quotes_TimeStamp",
                table: "Quotes",
                column: "TimeStamp");

            migrationBuilder.CreateIndex(
                name: "IX_SpotData_Symbol_Exchange_Timestamp",
                table: "SpotData",
                columns: new[] { "Symbol", "Exchange", "Timestamp" });

            migrationBuilder.CreateIndex(
                name: "IX_SpotData_Symbol_Timestamp",
                table: "SpotData",
                columns: new[] { "Symbol", "Timestamp" });

            migrationBuilder.CreateIndex(
                name: "IX_StockHistoricalPrices_Symbol_Date",
                table: "StockHistoricalPrices",
                columns: new[] { "Symbol", "Date" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CircuitBreakers");

            migrationBuilder.DropTable(
                name: "CircuitLimitChanges");

            migrationBuilder.DropTable(
                name: "CircuitLimitTrackers");

            migrationBuilder.DropTable(
                name: "DailyOptionContracts");

            migrationBuilder.DropTable(
                name: "DataDownloadStatuses");

            migrationBuilder.DropTable(
                name: "HistoricalOptionData");

            migrationBuilder.DropTable(
                name: "HistoricalPrices");

            migrationBuilder.DropTable(
                name: "IndexSnapshots");

            migrationBuilder.DropTable(
                name: "IntradayOptionSnapshots");

            migrationBuilder.DropTable(
                name: "OptionContracts");

            migrationBuilder.DropTable(
                name: "OptionMonitoringStats");

            migrationBuilder.DropTable(
                name: "Quotes");

            migrationBuilder.DropTable(
                name: "SpotData");

            migrationBuilder.DropTable(
                name: "StockHistoricalPrices");
        }
    }
}
