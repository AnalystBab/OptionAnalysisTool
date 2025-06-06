using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using OptionAnalysisTool.Common.Data;
using OptionAnalysisTool.Common.Services;
using OptionAnalysisTool.KiteConnect.Services;

namespace OptionAnalysisTool.Console;

class Program
{
    static async Task Main(string[] args)
    {
        System.Console.WriteLine("🚀 COMPREHENSIVE OPTION ANALYSIS SYSTEM");
        System.Console.WriteLine("========================================");
        System.Console.WriteLine("INDEX OPTIONS | CIRCUIT LIMITS | HLC PREDICTION");
        System.Console.WriteLine();

        var host = CreateHostBuilder(args).Build();

        try
        {
            await RunComprehensiveAnalysis(host);
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"❌ CRITICAL ERROR: {ex.Message}");
        }

        System.Console.WriteLine("\nPress any key to exit...");
        System.Console.ReadKey();
    }

    private static async Task RunComprehensiveAnalysis(IHost host)
    {
        using var scope = host.Services.CreateScope();
        var services = scope.ServiceProvider;
        
        var logger = services.GetRequiredService<ILogger<Program>>();
        var context = services.GetRequiredService<ApplicationDbContext>();
        var testService = services.GetRequiredService<IOptionAnalysisTestService>();
        var dataService = services.GetRequiredService<IComprehensiveOptionDataService>();
        var analysisService = services.GetRequiredService<ICircuitLimitAnalysisService>();

        System.Console.WriteLine("🔧 Initializing System...");
        
        // Ensure database is created
        await context.Database.EnsureCreatedAsync();
        System.Console.WriteLine("✅ Database initialized");

        // Show menu
        while (true)
        {
            ShowMenu();
            var choice = System.Console.ReadLine();

            switch (choice)
            {
                case "1":
                    await RunComprehensiveTests(testService);
                    break;
                case "2":
                    await DemonstrateHLCPrediction();
                    break;
                case "3":
                    await StartDataCollection(dataService);
                    break;
                case "4":
                    await ShowCircuitLimitAnalysis(analysisService);
                    break;
                case "5":
                    await ShowSystemStatus(context);
                    break;
                case "0":
                    return;
                default:
                    System.Console.WriteLine("❌ Invalid choice. Please try again.");
                    break;
            }

            System.Console.WriteLine("\nPress any key to continue...");
            System.Console.ReadKey();
        }
    }

    private static void ShowMenu()
    {
        System.Console.Clear();
        System.Console.WriteLine("🚀 COMPREHENSIVE OPTION ANALYSIS SYSTEM");
        System.Console.WriteLine("========================================");
        System.Console.WriteLine();
        System.Console.WriteLine("📋 MAIN MENU:");
        System.Console.WriteLine("1. 🧪 Run Comprehensive Tests");
        System.Console.WriteLine("2. 🎯 Demonstrate HLC Prediction");
        System.Console.WriteLine("3. 📊 Start Data Collection");
        System.Console.WriteLine("4. ⚡ Circuit Limit Analysis");
        System.Console.WriteLine("5. 📈 System Status");
        System.Console.WriteLine("0. 🚪 Exit");
        System.Console.WriteLine();
        System.Console.Write("Enter your choice: ");
    }

    private static async Task RunComprehensiveTests(IOptionAnalysisTestService testService)
    {
        System.Console.WriteLine("\n🧪 Running Comprehensive System Tests...");
        System.Console.WriteLine("==========================================");
        
        var results = await testService.RunComprehensiveTestAsync();
        
        System.Console.WriteLine($"\n📊 TEST RESULTS:");
        System.Console.WriteLine($"   Total Tests: {results.TotalTests}");
        System.Console.WriteLine($"   Passed: {results.TestsPassed}");
        System.Console.WriteLine($"   Failed: {results.TotalTests - results.TestsPassed}");
        System.Console.WriteLine($"   Duration: {results.TestDuration.TotalSeconds:F1}s");
        System.Console.WriteLine($"   Success: {(results.Success ? "✅ YES" : "❌ NO")}");
        
        if (!string.IsNullOrEmpty(results.ErrorMessage))
        {
            System.Console.WriteLine($"   Error: {results.ErrorMessage}");
        }

        System.Console.WriteLine($"\n🔍 Individual Test Results:");
        System.Console.WriteLine($"   Database Schema: {(results.DatabaseSchemaTest ? "✅" : "❌")}");
        System.Console.WriteLine($"   Data Collection: {(results.DataCollectionTest ? "✅" : "❌")}");
        System.Console.WriteLine($"   Circuit Tracking: {(results.CircuitLimitTrackingTest ? "✅" : "❌")}");
        System.Console.WriteLine($"   Analytics: {(results.AnalyticsTest ? "✅" : "❌")}");
        System.Console.WriteLine($"   Data Integrity: {(results.DataIntegrityTest ? "✅" : "❌")}");
        System.Console.WriteLine($"   HLC Prediction: {(results.PerformanceTest ? "✅" : "❌")}");
    }

    private static async Task DemonstrateHLCPrediction()
    {
        System.Console.WriteLine("\n🎯 HLC PREDICTION DEMONSTRATION");
        System.Console.WriteLine("===============================");
        System.Console.WriteLine("Using your provided CSV data for mathematical prediction");
        System.Console.WriteLine();

        // Sample data from your CSV input
        var historicalData = new[]
        {
            new { Date = "Day1", Open = 24699m, High = 23644.75m, Low = 23643.25m, Close = 23644.0m },
            new { Date = "Day2", Open = 24634m, High = 23694.75m, Low = 23693.25m, Close = 23694.0m },
            new { Date = "Day3", Open = 24690.25m, High = 23744.75m, Low = 23743.25m, Close = 23744.0m },
            new { Date = "Day4", Open = 24645m, High = 23794.75m, Low = 23793.25m, Close = 23794.0m },
            new { Date = "Day5", Open = 24598.75m, High = 23844.75m, Low = 23843.25m, Close = 23844.0m }
        };

        System.Console.WriteLine("📊 Historical Data:");
        foreach (var day in historicalData)
        {
            System.Console.WriteLine($"   {day.Date}: O={day.Open:F2}, H={day.High:F2}, L={day.Low:F2}, C={day.Close:F2}");
        }

        // Calculate average deltas
        var avgHighDelta = historicalData.Average(d => d.High - d.Open);
        var avgLowDelta = historicalData.Average(d => d.Low - d.Open);
        var avgCloseDelta = historicalData.Average(d => d.Close - d.Open);

        System.Console.WriteLine($"\n🔢 Calculated Average Deltas:");
        System.Console.WriteLine($"   High Delta: {avgHighDelta:F4}");
        System.Console.WriteLine($"   Low Delta: {avgLowDelta:F4}");
        System.Console.WriteLine($"   Close Delta: {avgCloseDelta:F4}");

        // Predict for next day
        System.Console.Write("\n📈 Enter Open price for prediction: ");
        if (decimal.TryParse(System.Console.ReadLine(), out var nextOpen))
        {
            var predictedHigh = nextOpen + avgHighDelta;
            var predictedLow = nextOpen + avgLowDelta;
            var predictedClose = nextOpen + avgCloseDelta;

            System.Console.WriteLine($"\n🎯 PREDICTIONS for Open = {nextOpen:F2}:");
            System.Console.WriteLine($"   Predicted High: {predictedHigh:F2}");
            System.Console.WriteLine($"   Predicted Low: {predictedLow:F2}");
            System.Console.WriteLine($"   Predicted Close: {predictedClose:F2}");
            System.Console.WriteLine($"   High-Low Range: {(predictedHigh - predictedLow):F2}");

            // Show confidence metrics
            var highVariance = historicalData.Select(d => d.High - d.Open).Variance();
            var lowVariance = historicalData.Select(d => d.Low - d.Open).Variance();
            var closeVariance = historicalData.Select(d => d.Close - d.Open).Variance();

            System.Console.WriteLine($"\n📊 Prediction Confidence (lower variance = higher confidence):");
            System.Console.WriteLine($"   High Variance: {highVariance:F6}");
            System.Console.WriteLine($"   Low Variance: {lowVariance:F6}");
            System.Console.WriteLine($"   Close Variance: {closeVariance:F6}");
        }
        else
        {
            System.Console.WriteLine("❌ Invalid input. Using default value 24600.");
            var defaultOpen = 24600m;
            var predictedHigh = defaultOpen + avgHighDelta;
            var predictedLow = defaultOpen + avgLowDelta;
            var predictedClose = defaultOpen + avgCloseDelta;

            System.Console.WriteLine($"\n🎯 PREDICTIONS for Open = {defaultOpen:F2}:");
            System.Console.WriteLine($"   Predicted High: {predictedHigh:F2}");
            System.Console.WriteLine($"   Predicted Low: {predictedLow:F2}");
            System.Console.WriteLine($"   Predicted Close: {predictedClose:F2}");
        }
    }

    private static async Task StartDataCollection(IComprehensiveOptionDataService dataService)
    {
        System.Console.WriteLine("\n📊 STARTING DATA COLLECTION");
        System.Console.WriteLine("============================");
        System.Console.WriteLine("Focus: INDEX OPTIONS (NIFTY, BANKNIFTY, FINNIFTY, etc.)");
        System.Console.WriteLine("Monitoring: Circuit Limits (VERY IMPORTANT)");
        System.Console.WriteLine();

        System.Console.WriteLine("⚠️  This will start continuous data collection.");
        System.Console.Write("Continue? (y/n): ");
        
        if (System.Console.ReadLine()?.ToLower() == "y")
        {
            System.Console.WriteLine("🚀 Starting comprehensive data collection...");
            System.Console.WriteLine("   - Intraday data (30s intervals)");
            System.Console.WriteLine("   - Spot data (10s intervals)");
            System.Console.WriteLine("   - Circuit limit monitoring (15s intervals) ⚡ CRITICAL");
            System.Console.WriteLine("   - Historical data (daily)");
            System.Console.WriteLine();
            System.Console.WriteLine("Press 'q' to stop data collection...");
            
            // Start data collection in background
            _ = Task.Run(async () => await dataService.StartComprehensiveDataCollectionAsync());
            
            // Wait for user to press 'q'
            while (System.Console.ReadKey().KeyChar != 'q')
            {
                // Continue collecting
            }
            
            System.Console.WriteLine("\n🛑 Stopping data collection...");
            await dataService.StopComprehensiveDataCollectionAsync();
            System.Console.WriteLine("✅ Data collection stopped.");
        }
    }

    private static async Task ShowCircuitLimitAnalysis(ICircuitLimitAnalysisService analysisService)
    {
        System.Console.WriteLine("\n⚡ CIRCUIT LIMIT ANALYSIS");
        System.Console.WriteLine("========================");
        System.Console.WriteLine("Focus: INDEX OPTIONS circuit limit changes");
        System.Console.WriteLine();

        var today = DateTime.Now.Date;
        
        // Show daily summary
        var summary = await analysisService.GetDailyCircuitLimitSummaryAsync(today);
        System.Console.WriteLine($"📊 Today's Summary ({today:yyyy-MM-dd}):");
        System.Console.WriteLine($"   Total Circuit Changes: {summary.TotalCircuitChanges}");
        System.Console.WriteLine($"   Circuit Breaches: {summary.TotalCircuitBreaches}");
        System.Console.WriteLine($"   Symbols Affected: {summary.UnderlyingSymbolsAffected}");
        System.Console.WriteLine($"   Most Active: {summary.MostActiveUnderlying}");
        System.Console.WriteLine($"   Market Sentiment: {summary.MarketSentiment}");

        // Show recent patterns
        var patterns = await analysisService.GetCircuitLimitPatternsAsync("NIFTY", 7);
        System.Console.WriteLine($"\n📈 NIFTY Patterns (Last 7 days):");
        foreach (var pattern in patterns.Take(3))
        {
            System.Console.WriteLine($"   {pattern.Date:yyyy-MM-dd}: {pattern.TotalChanges} changes, {pattern.PatternType}");
        }

        // Show breach alerts
        var alerts = await analysisService.GetCircuitBreachAlertsAsync(today);
        if (alerts.Any())
        {
            System.Console.WriteLine($"\n⚠️  Circuit Breach Alerts:");
            foreach (var alert in alerts.Take(5))
            {
                System.Console.WriteLine($"   {alert.Symbol}: {alert.BreachType} circuit at {alert.Price:F2} ({alert.Severity})");
            }
        }
        else
        {
            System.Console.WriteLine("\n✅ No circuit breaches detected today.");
        }
    }

    private static async Task ShowSystemStatus(ApplicationDbContext context)
    {
        System.Console.WriteLine("\n📈 SYSTEM STATUS");
        System.Console.WriteLine("================");
        
        try
        {
            var quoteCount = context.Quotes.Count();
            var historicalCount = context.HistoricalOptionData.Count();
            var spotCount = context.SpotData.Count();
            var circuitTrackingCount = context.CircuitLimitTrackers.Count();
            var intradayCount = context.IntradayOptionSnapshots.Count();

            System.Console.WriteLine($"📊 Database Records:");
            System.Console.WriteLine($"   Quotes: {quoteCount:N0}");
            System.Console.WriteLine($"   Historical Data: {historicalCount:N0}");
            System.Console.WriteLine($"   Spot Data: {spotCount:N0}");
            System.Console.WriteLine($"   Circuit Tracking: {circuitTrackingCount:N0}");
            System.Console.WriteLine($"   Intraday Snapshots: {intradayCount:N0}");

            var recentActivity = context.CircuitLimitTrackers
                .Where(c => c.ChangeTimestamp >= DateTime.Now.AddHours(-24))
                .Count();

            System.Console.WriteLine($"\n⚡ Recent Activity (24h):");
            System.Console.WriteLine($"   Circuit Changes: {recentActivity:N0}");

            System.Console.WriteLine($"\n🔧 System Health: ✅ OPERATIONAL");
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"❌ Database Error: {ex.Message}");
        }
    }

    private static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureServices((context, services) =>
            {
                // Database
                services.AddDbContext<ApplicationDbContext>(options =>
                    options.UseSqlServer("Server=(localdb)\\mssqllocaldb;Database=OptionAnalysisDB;Trusted_Connection=true;"));

                // Services
                services.AddScoped<IKiteConnectService, KiteConnectService>();
                services.AddScoped<MarketHoursService>();
                services.AddScoped<IComprehensiveOptionDataService, ComprehensiveOptionDataService>();
                services.AddScoped<ICircuitLimitAnalysisService, CircuitLimitAnalysisService>();
                services.AddScoped<IOptionAnalysisTestService, OptionAnalysisTestService>();

                // Logging
                services.AddLogging(builder =>
                {
                    builder.AddConsole();
                    builder.SetMinimumLevel(LogLevel.Information);
                });
            });
}

// Extension method for variance calculation
public static class MathExtensions
{
    public static double Variance(this IEnumerable<decimal> values)
    {
        var doubleValues = values.Select(v => (double)v).ToArray();
        if (!doubleValues.Any()) return 0;
        
        var mean = doubleValues.Average();
        return doubleValues.Average(v => Math.Pow(v - mean, 2));
    }
} 