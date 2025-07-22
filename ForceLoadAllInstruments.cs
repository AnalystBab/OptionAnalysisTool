using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using OptionAnalysisTool.Common.Data;
using OptionAnalysisTool.KiteConnect.Services;
using OptionAnalysisTool.KiteConnect;

class ForceLoadAllInstruments
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("🔥 FORCE LOAD ALL INSTRUMENTS SCRIPT");
        Console.WriteLine("=====================================");
        
        var host = CreateHostBuilder().Build();
        using var scope = host.Services.CreateScope();
        
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var kiteService = scope.ServiceProvider.GetRequiredService<IKiteConnectService>();
        var databaseTokenService = scope.ServiceProvider.GetRequiredService<DatabaseTokenService>();
        
        try
        {
            // Step 1: Clear all existing instruments
            Console.WriteLine("🗑️ Step 1: Clearing all existing instruments...");
            var existingCount = await context.Instruments.CountAsync();
            Console.WriteLine($"   Found {existingCount} existing instruments");
            
            if (existingCount > 0)
            {
                context.Instruments.RemoveRange(context.Instruments);
                await context.SaveChangesAsync();
                Console.WriteLine("   ✅ All existing instruments cleared");
            }
            
            // Step 2: Get access token
            Console.WriteLine("\n🔐 Step 2: Getting access token...");
            var accessToken = await databaseTokenService.GetCurrentAccessTokenAsync("ow1734");
            if (!string.IsNullOrEmpty(accessToken))
            {
                await kiteService.SetAccessToken(accessToken);
                Console.WriteLine("   ✅ Access token set successfully");
            }
            else
            {
                Console.WriteLine("   ❌ No access token available - please authenticate first");
                return;
            }
            
            // Step 3: Load NFO instruments (NSE)
            Console.WriteLine("\n📊 Step 3: Loading NFO instruments (NSE)...");
            var nfoInstruments = await kiteService.GetInstrumentsAsync("NFO");
            Console.WriteLine($"   Found {nfoInstruments.Count} total NFO instruments");
            
            // Step 4: Load BFO instruments (BSE)
            Console.WriteLine("\n📊 Step 4: Loading BFO instruments (BSE)...");
            var bfoInstruments = await kiteService.GetInstrumentsAsync("BFO");
            Console.WriteLine($"   Found {bfoInstruments.Count} total BFO instruments");
            
            // Step 5: Filter for index options
            Console.WriteLine("\n🎯 Step 5: Filtering for index options...");
            var allInstruments = nfoInstruments.Concat(bfoInstruments).ToList();
            
            var indexOptions = allInstruments.Where(i => 
                (i.InstrumentType == "CE" || i.InstrumentType == "PE") &&
                i.Expiry.HasValue && i.Expiry.Value >= DateTime.Today &&
                (i.Name == "NIFTY" || i.Name == "BANKNIFTY" || i.Name == "FINNIFTY" || 
                 i.Name == "MIDCPNIFTY" || i.Name == "SENSEX" || i.Name == "BANKEX"))
                .ToList();
                
            Console.WriteLine($"   Found {indexOptions.Count} index option instruments");
            
            // Step 6: Show breakdown by index
            Console.WriteLine("\n📋 Step 6: Breakdown by index:");
            var indexBreakdown = indexOptions.GroupBy(i => i.Name).OrderBy(g => g.Key);
            foreach (var group in indexBreakdown)
            {
                Console.WriteLine($"   {group.Key}: {group.Count()} instruments");
                
                // Show expiry breakdown for each index
                var expiryBreakdown = group.GroupBy(i => i.Expiry?.Date).OrderBy(g => g.Key);
                foreach (var expiryGroup in expiryBreakdown)
                {
                    Console.WriteLine($"     {expiryGroup.Key?.ToString("yyyy-MM-dd")}: {expiryGroup.Count()} instruments");
                }
            }
            
            // Step 7: Show breakdown by exchange
            Console.WriteLine("\n🏢 Step 7: Breakdown by exchange:");
            var exchangeBreakdown = indexOptions.GroupBy(i => 
            {
                if (i.Name == "SENSEX" || i.Name == "BANKEX") return "BFO (BSE)";
                return "NFO (NSE)";
            }).OrderBy(g => g.Key);
            
            foreach (var group in exchangeBreakdown)
            {
                Console.WriteLine($"   {group.Key}: {group.Count()} instruments");
            }
            
            // Step 8: Save to database
            Console.WriteLine("\n💾 Step 8: Saving to database...");
            var dbInstruments = indexOptions.Select(i => new OptionAnalysisTool.Models.Instrument
            {
                InstrumentToken = i.InstrumentToken,
                ExchangeToken = i.InstrumentToken,
                TradingSymbol = i.TradingSymbol,
                Name = i.Name ?? string.Empty,
                Strike = i.Strike,
                Expiry = i.Expiry,
                InstrumentType = i.InstrumentType ?? "CE",
                Segment = i.Segment ?? string.Empty,
                Exchange = (i.Name == "SENSEX" || i.Name == "BANKEX") ? "BFO" : "NFO",
                CreatedDate = DateTime.Today,
                LastUpdated = DateTime.Now
            }).ToList();
            
            await context.Instruments.AddRangeAsync(dbInstruments);
            await context.SaveChangesAsync();
            
            Console.WriteLine($"   ✅ Successfully saved {dbInstruments.Count} instruments to database");
            
            // Step 9: Verify database
            Console.WriteLine("\n✅ Step 9: Verifying database...");
            var finalCount = await context.Instruments.CountAsync();
            var nfoCount = await context.Instruments.Where(i => i.Exchange == "NFO").CountAsync();
            var bfoCount = await context.Instruments.Where(i => i.Exchange == "BFO").CountAsync();
            
            Console.WriteLine($"   Total instruments in database: {finalCount}");
            Console.WriteLine($"   NFO instruments: {nfoCount}");
            Console.WriteLine($"   BFO instruments: {bfoCount}");
            
            if (finalCount == dbInstruments.Count)
            {
                Console.WriteLine("   ✅ Database verification successful!");
            }
            else
            {
                Console.WriteLine("   ⚠️ Database count mismatch!");
            }
            
            Console.WriteLine("\n🎉 FORCE LOAD COMPLETE!");
            Console.WriteLine("The application should now show the correct instrument count.");
            
        }
        catch (Exception ex)
        {
            Console.WriteLine($"💥 Error: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
        }
    }
    
    static IHostBuilder CreateHostBuilder() =>
        Host.CreateDefaultBuilder()
            .ConfigureAppConfiguration((context, config) =>
            {
                config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
            })
            .ConfigureServices((context, services) =>
            {
                // Database
                services.AddDbContext<ApplicationDbContext>(options =>
                    options.UseSqlServer(context.Configuration.GetConnectionString("DefaultConnection")));
                
                // Kite Connect
                services.AddSingleton<global::KiteConnect.Kite>(provider =>
                {
                    var config = context.Configuration.GetSection("KiteConnect");
                    var apiKey = config["ApiKey"];
                    return new global::KiteConnect.Kite(apiKey, Debug: true);
                });
                
                services.AddScoped<IKiteConnectService, KiteConnectService>();
                services.AddSingleton<DatabaseTokenService>();
            });
} 