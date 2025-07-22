using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using OptionAnalysisTool.Common.Data;
using OptionAnalysisTool.KiteConnect.Services;
using OptionAnalysisTool.KiteConnect;

class LoadAllInstrumentsDirect
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("🔥 DIRECT INSTRUMENT LOADER");
        Console.WriteLine("============================");
        
        try
        {
            // Create host with minimal services
            var host = CreateHostBuilder().Build();
            using var scope = host.Services.CreateScope();
            
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var kiteService = scope.ServiceProvider.GetRequiredService<IKiteConnectService>();
            
            Console.WriteLine("✅ Services initialized");
            
            // Clear existing instruments first
            Console.WriteLine("🧹 Clearing existing instruments...");
            var existingCount = await context.Instruments.CountAsync();
            context.Instruments.RemoveRange(context.Instruments);
            await context.SaveChangesAsync();
            Console.WriteLine($"🗑️ Removed {existingCount} existing instruments");
            
            // Load NFO instruments
            Console.WriteLine("📊 Loading NFO instruments...");
            var nfoInstruments = await kiteService.GetInstrumentsAsync("NFO");
            if (nfoInstruments != null && nfoInstruments.Any())
            {
                var nfoOptions = nfoInstruments
                    .Where(i => (i.InstrumentType == "CE" || i.InstrumentType == "PE") &&
                               (i.Name?.Contains("NIFTY") == true || i.Name?.Contains("BANKNIFTY") == true || 
                                i.Name?.Contains("FINNIFTY") == true || i.Name?.Contains("MIDCPNIFTY") == true))
                    .Where(i => i.Expiry.HasValue && i.Expiry.Value >= DateTime.Today)
                    .Select(i => new Instrument
                    {
                        InstrumentToken = i.InstrumentToken,
                        ExchangeToken = i.InstrumentToken,
                        TradingSymbol = i.TradingSymbol,
                        Name = i.Name ?? string.Empty,
                        Strike = i.Strike,
                        Expiry = i.Expiry,
                        InstrumentType = i.InstrumentType ?? "CE",
                        Segment = i.Segment ?? string.Empty,
                        Exchange = "NFO"
                    })
                    .ToList();
                
                context.Instruments.AddRange(nfoOptions);
                await context.SaveChangesAsync();
                Console.WriteLine($"✅ Loaded {nfoOptions.Count} NFO instruments");
            }
            
            // Load BFO instruments
            Console.WriteLine("📊 Loading BFO instruments...");
            var bfoInstruments = await kiteService.GetInstrumentsAsync("BFO");
            if (bfoInstruments != null && bfoInstruments.Any())
            {
                var bfoOptions = bfoInstruments
                    .Where(i => (i.InstrumentType == "CE" || i.InstrumentType == "PE") &&
                               (i.Name?.Contains("SENSEX") == true || i.Name?.Contains("BANKEX") == true))
                    .Where(i => i.Expiry.HasValue && i.Expiry.Value >= DateTime.Today)
                    .Select(i => new Instrument
                    {
                        InstrumentToken = i.InstrumentToken,
                        ExchangeToken = i.InstrumentToken,
                        TradingSymbol = i.TradingSymbol,
                        Name = i.Name ?? string.Empty,
                        Strike = i.Strike,
                        Expiry = i.Expiry,
                        InstrumentType = i.InstrumentType ?? "CE",
                        Segment = i.Segment ?? string.Empty,
                        Exchange = "BFO"
                    })
                    .ToList();
                
                context.Instruments.AddRange(bfoOptions);
                await context.SaveChangesAsync();
                Console.WriteLine($"✅ Loaded {bfoOptions.Count} BFO instruments");
            }
            
            // Final statistics
            var totalInstruments = await context.Instruments.CountAsync();
            var nfoCount = await context.Instruments.Where(i => i.Exchange == "NFO").CountAsync();
            var bfoCount = await context.Instruments.Where(i => i.Exchange == "BFO").CountAsync();
            
            Console.WriteLine();
            Console.WriteLine("🎯 LOADING COMPLETE!");
            Console.WriteLine($"📊 Total Instruments: {totalInstruments}");
            Console.WriteLine($"📊 NFO Instruments: {nfoCount}");
            Console.WriteLine($"📊 BFO Instruments: {bfoCount}");
            Console.WriteLine();
            Console.WriteLine("✅ All instruments loaded successfully!");
            
        }
        catch (Exception ex)
        {
            Console.WriteLine($"💥 Error: {ex.Message}");
            Console.WriteLine($"💥 Stack Trace: {ex.StackTrace}");
        }
        
        Console.WriteLine("Press any key to exit...");
        Console.ReadKey();
    }
    
    static IHostBuilder CreateHostBuilder()
    {
        return Microsoft.Extensions.Hosting.Host.CreateDefaultBuilder()
            .ConfigureAppConfiguration((context, config) =>
            {
                config.AddJsonFile("OptionAnalysisTool.App/appsettings.json", optional: false, reloadOnChange: true);
            })
            .ConfigureServices((context, services) =>
            {
                services.AddDbContext<ApplicationDbContext>(options =>
                    options.UseSqlServer(context.Configuration.GetConnectionString("DefaultConnection")));
                
                services.Configure<KiteConnectConfig>(context.Configuration.GetSection("KiteConnect"));
                
                services.AddSingleton<global::KiteConnect.Kite>(provider =>
                {
                    var config = context.Configuration.GetSection("KiteConnect");
                    var apiKey = config["ApiKey"];
                    return new global::KiteConnect.Kite(apiKey, Debug: true);
                });
                
                services.AddSingleton<KiteConnectWrapper>();
                services.AddScoped<IKiteConnectService, KiteConnectService>();
            });
    }
} 