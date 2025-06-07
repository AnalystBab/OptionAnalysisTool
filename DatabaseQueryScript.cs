using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using OptionAnalysisTool.Common.Data;

namespace DatabaseQuery
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("🔍 DATABASE CIRCUIT LIMIT DATA CHECKER");
            Console.WriteLine("=====================================");

            // Setup services
            var services = new ServiceCollection();
            
            // Add configuration
            var configuration = new ConfigurationBuilder()
                .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                .AddJsonFile("OptionAnalysisTool.App/appsettings.json", optional: false, reloadOnChange: true)
                .Build();
                
            services.AddSingleton<IConfiguration>(configuration);
            
            // Add DbContext with SQL Server
            var connectionString = configuration.GetConnectionString("DefaultConnection");
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(connectionString));

            var serviceProvider = services.BuildServiceProvider();

            try
            {
                using var scope = serviceProvider.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                
                Console.WriteLine($"📊 Connecting to: {connectionString.Replace(connectionString.Split(';')[0].Split('=')[1], "***")}");
                Console.WriteLine();

                // Test database connection
                try
                {
                    await context.Database.CanConnectAsync();
                    Console.WriteLine("✅ Database connection successful");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Database connection failed: {ex.Message}");
                    return;
                }

                // Check circuit limit trackers
                Console.WriteLine("\n🔍 CIRCUIT LIMIT TRACKERS DATA:");
                Console.WriteLine("==============================");
                
                var trackers = await context.CircuitLimitTrackers
                    .OrderByDescending(x => x.DetectedAt)
                    .Take(10)
                    .ToListAsync();
                
                if (trackers.Any())
                {
                    Console.WriteLine($"Total Records: {await context.CircuitLimitTrackers.CountAsync()}");
                    Console.WriteLine($"Latest 10 records:");
                    Console.WriteLine($"{"Symbol",-15} {"Strike",-8} {"Type",-4} {"Old Lower",-10} {"New Lower",-10} {"Old Upper",-10} {"New Upper",-10} {"Detected",-20} {"Severity",-10}");
                    Console.WriteLine(new string('-', 110));
                    
                    foreach (var tracker in trackers)
                    {
                        Console.WriteLine($"{tracker.Symbol,-15} {tracker.StrikePrice,-8:F0} {tracker.OptionType,-4} {tracker.PreviousLowerLimit,-10:F2} {tracker.NewLowerLimit,-10:F2} {tracker.PreviousUpperLimit,-10:F2} {tracker.NewUpperLimit,-10:F2} {tracker.DetectedAt.ToString("MM/dd HH:mm:ss"),-20} {tracker.SeverityLevel,-10}");
                    }
                }
                else
                {
                    Console.WriteLine("❌ No circuit limit tracker records found");
                }

                // Check intraday snapshots
                Console.WriteLine("\n🔍 INTRADAY OPTION SNAPSHOTS DATA:");
                Console.WriteLine("=================================");
                
                var snapshots = await context.IntradayOptionSnapshots
                    .OrderByDescending(x => x.Timestamp)
                    .Take(10)
                    .ToListAsync();
                
                if (snapshots.Any())
                {
                    Console.WriteLine($"Total Records: {await context.IntradayOptionSnapshots.CountAsync()}");
                    Console.WriteLine($"Latest 10 records:");
                    Console.WriteLine($"{"Symbol",-15} {"Strike",-8} {"Type",-4} {"Last Price",-10} {"Lower Limit",-12} {"Upper Limit",-12} {"Status",-15} {"Timestamp",-20}");
                    Console.WriteLine(new string('-', 115));
                    
                    foreach (var snapshot in snapshots)
                    {
                        Console.WriteLine($"{snapshot.Symbol,-15} {snapshot.StrikePrice,-8:F0} {snapshot.OptionType,-4} {snapshot.LastPrice,-10:F2} {snapshot.LowerCircuitLimit,-12:F2} {snapshot.UpperCircuitLimit,-12:F2} {snapshot.CircuitLimitStatus,-15} {snapshot.Timestamp.ToString("MM/dd HH:mm:ss"),-20}");
                    }
                }
                else
                {
                    Console.WriteLine("❌ No intraday snapshot records found");
                }

                // Check historical option data
                Console.WriteLine("\n🔍 HISTORICAL OPTION DATA:");
                Console.WriteLine("=========================");
                
                var historical = await context.HistoricalOptionData
                    .OrderByDescending(x => x.TradingDate)
                    .Take(10)
                    .ToListAsync();
                
                if (historical.Any())
                {
                    Console.WriteLine($"Total Records: {await context.HistoricalOptionData.CountAsync()}");
                    Console.WriteLine($"Latest 10 records:");
                    Console.WriteLine($"{"Symbol",-15} {"Strike",-8} {"Type",-4} {"Trading Date",-12} {"Close",-8} {"Lower Limit",-12} {"Upper Limit",-12} {"Changed",-8}");
                    Console.WriteLine(new string('-', 105));
                    
                    foreach (var hist in historical)
                    {
                        Console.WriteLine($"{hist.Symbol,-15} {hist.StrikePrice,-8:F0} {hist.OptionType,-4} {hist.TradingDate.ToString("MM/dd/yy"),-12} {hist.Close,-8:F2} {hist.LowerCircuitLimit,-12:F2} {hist.UpperCircuitLimit,-12:F2} {(hist.CircuitLimitChanged ? "YES" : "NO"),-8}");
                    }
                }
                else
                {
                    Console.WriteLine("❌ No historical option data records found");
                }

                // Check circuit limit changes
                Console.WriteLine("\n🔍 CIRCUIT LIMIT CHANGES DATA:");
                Console.WriteLine("=============================");
                
                var changes = await context.CircuitLimitChanges
                    .OrderByDescending(x => x.Timestamp)
                    .Take(10)
                    .ToListAsync();
                
                if (changes.Any())
                {
                    Console.WriteLine($"Total Records: {await context.CircuitLimitChanges.CountAsync()}");
                    Console.WriteLine($"Latest 10 records:");
                    Console.WriteLine($"{"Symbol",-15} {"Strike",-8} {"Old Lower",-10} {"New Lower",-10} {"Old Upper",-10} {"New Upper",-10} {"Timestamp",-20} {"Reason",-15}");
                    Console.WriteLine(new string('-', 115));
                    
                    foreach (var change in changes)
                    {
                        Console.WriteLine($"{change.Symbol,-15} {change.StrikePrice?.ToString("F0") ?? "N/A",-8} {change.OldLowerCircuitLimit,-10:F2} {change.NewLowerCircuitLimit,-10:F2} {change.OldUpperCircuitLimit,-10:F2} {change.NewUpperCircuitLimit,-10:F2} {change.Timestamp.ToString("MM/dd HH:mm:ss"),-20} {change.ChangeReason,-15}");
                    }
                }
                else
                {
                    Console.WriteLine("❌ No circuit limit change records found");
                }

                // Summary
                Console.WriteLine("\n📊 SUMMARY:");
                Console.WriteLine("===========");
                Console.WriteLine($"Circuit Limit Trackers: {await context.CircuitLimitTrackers.CountAsync()}");
                Console.WriteLine($"Intraday Snapshots: {await context.IntradayOptionSnapshots.CountAsync()}");
                Console.WriteLine($"Historical Data: {await context.HistoricalOptionData.CountAsync()}");
                Console.WriteLine($"Circuit Changes: {await context.CircuitLimitChanges.CountAsync()}");
                
                var totalRecords = await context.CircuitLimitTrackers.CountAsync() + 
                                  await context.IntradayOptionSnapshots.CountAsync() + 
                                  await context.HistoricalOptionData.CountAsync() + 
                                  await context.CircuitLimitChanges.CountAsync();
                
                Console.WriteLine($"Total Circuit Limit Records: {totalRecords}");
                
                if (totalRecords == 0)
                {
                    Console.WriteLine("\n❗ ISSUE FOUND: NO DATA IN CIRCUIT LIMIT TABLES");
                    Console.WriteLine("This explains why your WPF grid is empty!");
                    Console.WriteLine("\n💡 SOLUTIONS:");
                    Console.WriteLine("1. Click 'Test NIFTY Circuit Limits' button in WPF app");
                    Console.WriteLine("2. Run data collection during market hours");
                    Console.WriteLine("3. Check if database migration completed properly");
                }
                else
                {
                    Console.WriteLine($"\n✅ Data exists but WPF may have display/binding issues");
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }

            Console.WriteLine("\nPress any key to exit...");
            Console.ReadKey();
        }
    }
} 