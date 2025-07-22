using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using OptionAnalysisTool.Common.Data;
using OptionAnalysisTool.Common.Services;
using OptionAnalysisTool.Common.Repositories;
using OptionAnalysisTool.KiteConnect.Services;
using OptionAnalysisTool.KiteConnect;
using OptionAnalysisTool.Models;
using OptionAnalysisTool.Common.Models;
using System.Threading;
using System.Linq;
using System.Collections.Generic;
using CommonSpotData = OptionAnalysisTool.Common.Models.SpotData;
using DomainSpotData = OptionAnalysisTool.Models.SpotData;
using ExternalKite = KiteConnect;
using OptionAnalysisTool.Console;
using OptionAnalysisTool.Console.Services;
using System.Diagnostics;
using System.Net.Http;
using System.Text;
using Microsoft.Data.SqlClient;
using System.Security.Cryptography;
using Serilog;

namespace OptionAnalysisTool.Console
{
    /// <summary>
    /// 🔥 INDIAN OPTION ANALYSIS - DATA COLLECTION SERVICE
    /// Collects real-time circuit limits and market data for index options
    /// Uses database token authentication for autonomous operation
    /// </summary>
    public class Program
    {
        private static readonly string ApiKey = "kw3ptb0zmocwupmo";
        private static readonly string ApiSecret = "q6iqhpb3lx2sw9tomkrljb5fmczdx6mv";
        private static readonly string ConnectionString = "Server=LAPTOP-B68L4IP9;Database=PalindromeResults;Trusted_Connection=True;TrustServerCertificate=True;";

        public static async Task Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.Console()
                .WriteTo.File("Logs/option_analysis.log", rollingInterval: RollingInterval.Day, retainedFileCountLimit: 7)
                .CreateLogger();
            try
            {
                // Check for test spot data command
                if (args.Length > 0 && args[0] == "--test-spot-data")
                {
                    await TestSpotDataCollectionMode();
                    return;
                }

                // Check for clear and reload spot data command
                if (args.Length > 0 && args[0] == "--clear-and-reload-spot-data")
                {
                    await ClearAndReloadSpotDataMode();
                    return;
                }
                else if (args.Contains("--find-bankex-token"))
                {
                    await FindBankexTokenMode();
                }
                else
                {
                    // Default mode - start the comprehensive data collection
                    await StartComprehensiveDataCollection();
                }
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "CRITICAL ERROR: {Message}", ex.Message);
                System.Console.WriteLine($"💥 CRITICAL ERROR: {ex.Message}");
                System.Console.WriteLine("Press any key to exit...");
                System.Console.ReadKey();
                Environment.Exit(1);
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }

        /// <summary>
        /// 🔍 CHECK AUTHENTICATION STATUS
        /// Verifies if there's a valid access token in the database
        /// </summary>
        private static async Task<bool> CheckAuthenticationStatusAsync()
        {
            try
            {
                using var connection = new SqlConnection(ConnectionString);
                await connection.OpenAsync();
                
                var query = @"
                    SELECT COUNT(*) 
                    FROM AuthenticationTokens 
                    WHERE IsActive = 1 
                      AND ExpiresAt > GETUTCDATE() 
                      AND ApiKey = @ApiKey";
                
                using var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@ApiKey", ApiKey);
                
                var count = (int)await command.ExecuteScalarAsync();
                
                if (count > 0)
                {
                    // Get token details for display
                    var detailQuery = @"
                        SELECT TOP 1 LEFT(AccessToken, 10) + '...', ExpiresAt, Source 
                        FROM AuthenticationTokens 
                        WHERE IsActive = 1 AND ExpiresAt > GETUTCDATE() AND ApiKey = @ApiKey 
                        ORDER BY CreatedAt DESC";
                    
                    using var detailCommand = new SqlCommand(detailQuery, connection);
                    detailCommand.Parameters.AddWithValue("@ApiKey", ApiKey);
                    
                    using var reader = await detailCommand.ExecuteReaderAsync();
                    if (reader.Read())
                    {
                        var tokenPreview = reader.GetString(0);
                        var expiresAt = reader.GetDateTime(1);
                        var source = reader.GetString(2);
                        
                        var istExpiry = TimeZoneInfo.ConvertTimeFromUtc(expiresAt, 
                            TimeZoneInfo.FindSystemTimeZoneById("India Standard Time"));
                        
                        System.Console.WriteLine($"✅ Token found: {tokenPreview}");
                        System.Console.WriteLine($"   Expires: {istExpiry:yyyy-MM-dd HH:mm} IST");
                        System.Console.WriteLine($"   Source: {source}");
                        
                        var timeUntilExpiry = expiresAt - DateTime.UtcNow;
                        if (timeUntilExpiry.TotalHours < 2)
                        {
                            System.Console.WriteLine($"⚠️  WARNING: Token expires in {timeUntilExpiry.TotalMinutes:F0} minutes!");
                            System.Console.WriteLine("   Consider getting a fresh token soon.");
                        }
                    }
                    
                    return true;
                }
                
                return false;
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"❌ Error checking authentication: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 🔐 LAUNCH GUI AUTHENTICATION PROCESS
        /// Launches the SimpleKiteAuth GUI application for authentication
        /// </summary>
        private static async Task<bool> LaunchGuiAuthenticationAsync()
        {
            try
            {
                System.Console.WriteLine("🔐 === AUTHENTICATION REQUIRED ===");
                System.Console.WriteLine();
                System.Console.WriteLine("To collect live market data, we need to authenticate with Kite Connect.");
                System.Console.WriteLine("This will open a user-friendly GUI window for authentication.");
                System.Console.WriteLine();
                
                // STEP 1: Launch GUI Authentication
                System.Console.WriteLine("🖥️ STEP 1: Launching GUI Authentication Window...");
                
                try
                {
                    // First, try to build the SimpleKiteAuth project
                    System.Console.WriteLine("🔨 Building SimpleKiteAuth project...");
                    var buildProcess = new Process
                    {
                        StartInfo = new ProcessStartInfo
                        {
                            FileName = "dotnet",
                            Arguments = "build SimpleKiteAuth/SimpleKiteAuth.csproj",
                            UseShellExecute = false,
                            RedirectStandardOutput = true,
                            RedirectStandardError = true,
                            WorkingDirectory = System.IO.Directory.GetCurrentDirectory()
                        }
                    };
                    
                    buildProcess.Start();
                    var buildOutput = await buildProcess.StandardOutput.ReadToEndAsync();
                    var buildError = await buildProcess.StandardError.ReadToEndAsync();
                    await buildProcess.WaitForExitAsync();
                    
                    if (buildProcess.ExitCode != 0)
                    {
                        System.Console.WriteLine("❌ Build failed for SimpleKiteAuth project!");
                        System.Console.WriteLine($"Build Output: {buildOutput}");
                        System.Console.WriteLine($"Build Errors: {buildError}");
                        System.Console.WriteLine();
                        System.Console.WriteLine("🔄 FALLBACK: Manual Authentication");
                        System.Console.WriteLine("Please manually run: dotnet run --project SimpleKiteAuth");
                        System.Console.WriteLine("Or use your request token directly...");
                        
                        return await FallbackManualAuthenticationAsync();
                    }
                    
                    System.Console.WriteLine("✅ SimpleKiteAuth project built successfully!");
                    
                    // Launch the SimpleKiteAuth GUI application
                    var authProcess = new Process
                    {
                        StartInfo = new ProcessStartInfo
                        {
                            FileName = "dotnet",
                            Arguments = "run --project SimpleKiteAuth",
                            UseShellExecute = false,
                            WorkingDirectory = System.IO.Directory.GetCurrentDirectory()
                        }
                    };
                    
                    authProcess.Start();
                    System.Console.WriteLine("✅ Authentication window opened!");
                    System.Console.WriteLine();
                    System.Console.WriteLine("📋 INSTRUCTIONS:");
                    System.Console.WriteLine("   1. In the GUI window that opened:");
                    System.Console.WriteLine("      - Click 'Open Kite Connect Login'");
                    System.Console.WriteLine("      - Complete login in browser");
                    System.Console.WriteLine("      - Copy the request_token from the redirect URL");
                    System.Console.WriteLine("      - Paste it in the GUI window");
                    System.Console.WriteLine("      - Click 'Submit Token'");
                    System.Console.WriteLine();
                    
                    // Wait for the process to complete
                    System.Console.WriteLine("⏳ Waiting for authentication to complete...");
                    System.Console.WriteLine("   (The console will wait until you complete authentication in the GUI)");
                    
                    await authProcess.WaitForExitAsync();
                    
                    // Check if authentication was successful by looking for token in database
                    System.Console.WriteLine("🔍 Checking if authentication was successful...");
                    await Task.Delay(2000); // Wait a moment for database update
                    
                    var isAuthenticated = await CheckAuthenticationStatusAsync();
                    
                    if (isAuthenticated)
                    {
                        System.Console.WriteLine("🎉 SUCCESS! Authentication completed successfully!");
                        System.Console.WriteLine("   The application can now collect live market data.");
                        return true;
                    }
                    else
                    {
                        System.Console.WriteLine("❌ Authentication was not completed successfully.");
                        System.Console.WriteLine("   Please try again or check the GUI window for errors.");
                        return false;
                    }
                }
                catch (Exception ex)
                {
                    System.Console.WriteLine($"⚠️ Could not launch GUI authentication: {ex.Message}");
                    System.Console.WriteLine();
                    System.Console.WriteLine("🔄 FALLBACK: Manual Authentication");
                    System.Console.WriteLine("Please manually run: dotnet run --project SimpleKiteAuth");
                    System.Console.WriteLine("Or use your request token directly...");
                    
                    // Fallback to manual token input if GUI fails
                    return await FallbackManualAuthenticationAsync();
                }
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"❌ Authentication error: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 🔄 FALLBACK MANUAL AUTHENTICATION
        /// If GUI authentication fails, provide manual option
        /// </summary>
        private static async Task<bool> FallbackManualAuthenticationAsync()
        {
            try
            {
                System.Console.WriteLine();
                System.Console.WriteLine("📝 MANUAL TOKEN INPUT:");
                System.Console.WriteLine("If you already have a request token, you can enter it here.");
                System.Console.WriteLine();
                
                // STEP 2: Get request token from user
                System.Console.Write("📝 Paste your request_token here (or press Enter to skip): ");
                var requestToken = System.Console.ReadLine()?.Trim();
                
                if (string.IsNullOrEmpty(requestToken))
                {
                    System.Console.WriteLine("⏭️ Manual authentication skipped.");
                    return false;
                }
                
                System.Console.WriteLine($"✅ Request token received: {requestToken[..Math.Min(10, requestToken.Length)]}...");
                
                // STEP 3: Convert to access token
                System.Console.WriteLine("🔄 Converting to access token...");
                var accessToken = await GetAccessTokenAsync(requestToken);
                
                if (string.IsNullOrEmpty(accessToken))
                {
                    System.Console.WriteLine("❌ Failed to get access token!");
                    return false;
                }
                
                // STEP 4: Store in database
                System.Console.WriteLine("💾 Storing token in database...");
                var stored = await StoreTokenInDatabaseAsync(accessToken);
                
                if (stored)
                {
                    System.Console.WriteLine("🎉 SUCCESS! Authentication completed and token stored!");
                    System.Console.WriteLine("   The application can now collect live market data.");
                    return true;
                }
                else
                {
                    System.Console.WriteLine("❌ Failed to store token in database!");
                    return false;
                }
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"❌ Manual authentication error: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 🔄 CONVERT REQUEST TOKEN TO ACCESS TOKEN
        /// </summary>
        private static async Task<string?> GetAccessTokenAsync(string requestToken)
        {
            try
            {
                using var client = new HttpClient();
                
                // Generate checksum: SHA256(api_key + request_token + api_secret)
                var checksumData = ApiKey + requestToken + ApiSecret;
                using var sha256 = System.Security.Cryptography.SHA256.Create();
                var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(checksumData));
                var checksum = Convert.ToHexString(hash).ToLower();
                
                var content = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("api_key", ApiKey),
                    new KeyValuePair<string, string>("request_token", requestToken),
                    new KeyValuePair<string, string>("checksum", checksum)
                });
                
                var response = await client.PostAsync("https://api.kite.trade/session/token", content);
                var responseString = await response.Content.ReadAsStringAsync();
                
                System.Console.WriteLine($"🔍 API Response: {responseString[..Math.Min(100, responseString.Length)]}...");
                
                // Simple JSON parsing for access_token
                if (responseString.Contains("\"access_token\""))
                {
                    var start = responseString.IndexOf("\"access_token\":\"") + 16;
                    var end = responseString.IndexOf("\"", start);
                    return responseString.Substring(start, end - start);
                }
                else
                {
                    System.Console.WriteLine($"❌ No access token in response: {responseString}");
                    return null;
                }
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"❌ Error getting access token: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 💾 STORE TOKEN IN DATABASE
        /// </summary>
        private static async Task<bool> StoreTokenInDatabaseAsync(string accessToken)
        {
            try
            {
                using var connection = new SqlConnection(ConnectionString);
                await connection.OpenAsync();
                System.Console.WriteLine("✅ Connected to database");
                
                // Clear old tokens for this API key
                var deleteCmd = new SqlCommand("DELETE FROM AuthenticationTokens WHERE ApiKey = @ApiKey", connection);
                deleteCmd.Parameters.AddWithValue("@ApiKey", ApiKey);
                var deletedRows = await deleteCmd.ExecuteNonQueryAsync();
                System.Console.WriteLine($"🗑️  Removed {deletedRows} old tokens");
                
                // Insert new authentication data
                var insertCmd = new SqlCommand(@"
                    INSERT INTO AuthenticationTokens 
                    (AccessToken, ApiKey, ApiSecret, UserId, UserName, CreatedAt, ExpiresAt, LastUsedAt, IsActive, Source, UpdatedAt, Metadata) 
                    VALUES 
                    (@AccessToken, @ApiKey, @ApiSecret, @UserId, @UserName, @CreatedAt, @ExpiresAt, @LastUsedAt, 1, @Source, @UpdatedAt, @Metadata)", connection);
                
                var now = DateTime.UtcNow;
                var expiry = now.AddHours(24); // Kite tokens expire in 24 hours
                
                insertCmd.Parameters.AddWithValue("@AccessToken", accessToken);
                insertCmd.Parameters.AddWithValue("@ApiKey", ApiKey);
                insertCmd.Parameters.AddWithValue("@ApiSecret", ApiSecret);
                insertCmd.Parameters.AddWithValue("@UserId", "VDZ315");
                insertCmd.Parameters.AddWithValue("@UserName", "Indian Option Analysis User");
                insertCmd.Parameters.AddWithValue("@CreatedAt", now);
                insertCmd.Parameters.AddWithValue("@ExpiresAt", expiry);
                insertCmd.Parameters.AddWithValue("@LastUsedAt", now);
                insertCmd.Parameters.AddWithValue("@Source", "ConsoleApp");
                insertCmd.Parameters.AddWithValue("@UpdatedAt", now);
                insertCmd.Parameters.AddWithValue("@Metadata", $"{{\"created_at_ist\":\"{now.AddHours(5.5):yyyy-MM-dd HH:mm:ss}\"}}");
                
                await insertCmd.ExecuteNonQueryAsync();
                
                System.Console.WriteLine("✅ Authentication data stored successfully!");
                System.Console.WriteLine($"🕘 Token expires at: {expiry.AddHours(5.5):yyyy-MM-dd HH:mm:ss} IST");
                
                return true;
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"❌ Error storing token: {ex.Message}");
                return false;
            }
        }

        private static async Task StartLiveDataCollection(
            IKiteConnectService kiteService, 
            IntradayDataService intradayService, 
            MarketHoursService marketHours)
        {
            System.Console.WriteLine("🔄 Fetching option instruments...");
            
            // Get all index option instruments
            var allInstruments = await kiteService.GetInstrumentsAsync("NFO"); // Options exchange
            var indexOptions = allInstruments
                .Where(i => i.InstrumentType == "CE" || i.InstrumentType == "PE")
                .Where(i => i.Name == "NIFTY" || i.Name == "BANKNIFTY" || i.Name == "SENSEX" || i.Name == "BANKEX")
                .ToList();

            System.Console.WriteLine($"✅ Found {indexOptions.Count} index option contracts");

            var cancellationTokenSource = new CancellationTokenSource();
            
            System.Console.WriteLine();
            System.Console.WriteLine("📊 LIVE DATA COLLECTION STARTED");
            System.Console.WriteLine("⏹️  Press 'q' to stop, 's' for status");
            System.Console.WriteLine();

            _ = Task.Run(async () =>
            {
                while (!cancellationTokenSource.Token.IsCancellationRequested && marketHours.IsMarketOpen())
                {
                    try
                    {
                        System.Console.WriteLine($"🔄 {DateTime.Now:HH:mm:ss} - Collecting market data...");
                        
                        // Collect data for each index
                        foreach (var index in new[] { "NIFTY", "BANKNIFTY", "SENSEX", "BANKEX" })
                        {
                            var snapshots = await intradayService.CaptureOptionSnapshots(index);
                            System.Console.WriteLine($"   {index}: {snapshots.Count} snapshots captured");
                        }

                        // Wait 1 minute before next collection
                        await Task.Delay(TimeSpan.FromMinutes(1), cancellationTokenSource.Token);
                    }
                    catch (Exception ex)
                    {
                        System.Console.WriteLine($"⚠️ Error during data collection: {ex.Message}");
                        await Task.Delay(TimeSpan.FromSeconds(30), cancellationTokenSource.Token);
                    }
                }
            });

            // Handle user input
            while (!cancellationTokenSource.Token.IsCancellationRequested)
            {
                var key = System.Console.ReadKey(true);
                if (key.KeyChar == 'q' || key.KeyChar == 'Q')
                {
                    cancellationTokenSource.Cancel();
                    break;
                }
                else if (key.KeyChar == 's' || key.KeyChar == 'S')
                {
                    System.Console.WriteLine($"📊 Status: Market {(marketHours.IsMarketOpen() ? "OPEN" : "CLOSED")} - Data collection running");
                }
            }

            System.Console.WriteLine("🛑 Data collection stopped");
        }

        private static async Task CollectLastAvailableData(
            IKiteConnectService kiteService, 
            IntradayDataService intradayService,
            ApplicationDbContext dbContext)
        {
            try
            {
                System.Console.WriteLine("🔥 === COMPREHENSIVE CIRCUIT LIMIT COLLECTION ===");
                System.Console.WriteLine("📊 Fetching ALL strikes for ALL index options with timestamp tracking");
                
                // Step 1: Get ALL instruments for ALL indices
                System.Console.WriteLine("🔄 Fetching NFO and BFO instruments...");
                var nfoInstruments = await kiteService.GetInstrumentsAsync("NFO"); // NIFTY, BANKNIFTY, etc.
                var bfoInstruments = await kiteService.GetInstrumentsAsync("BFO"); // SENSEX, BANKEX
                var allInstruments = nfoInstruments.Concat(bfoInstruments).ToList();
                
                System.Console.WriteLine($"📋 Total instruments: NFO={nfoInstruments.Count}, BFO={bfoInstruments.Count}");
                
                // Step 2: Filter for ALL index option strikes (NO FILTERING BY STRIKE PRICE)
                var allIndexOptions = allInstruments
                    .Where(i => i.InstrumentType == "CE" || i.InstrumentType == "PE")
                    .Where(i => i.Name == "NIFTY" || i.Name == "BANKNIFTY" || i.Name == "SENSEX" || 
                               i.Name == "BANKEX" || i.Name == "FINNIFTY" || i.Name == "MIDCPNIFTY")
                    .Where(i => i.Expiry >= DateTime.Now.Date) // Only non-expired options
                    .OrderBy(i => i.Name)
                    .ThenBy(i => i.Strike)
                    .ThenBy(i => i.InstrumentType)
                    .ToList();

                System.Console.WriteLine($"✅ Found {allIndexOptions.Count} index option contracts across all strikes");
                
                // Step 3: Get current spot prices for all indices  
                var spotPrices = await GetCurrentSpotPrices(kiteService);
                System.Console.WriteLine($"📈 Current spot prices: {string.Join(", ", spotPrices.Select(s => $"{s.Key}={s.Value:F2}"))}");
                
                // Step 4: Process in batches to avoid API rate limits
                var batchSize = 100; // Process 100 contracts at a time
                var totalBatches = (int)Math.Ceiling((double)allIndexOptions.Count / batchSize);
                var collectionTime = DateTime.UtcNow;
                
                System.Console.WriteLine($"🔄 Processing {totalBatches} batches of {batchSize} contracts each...");
                
                var processedCount = 0;
                var failedCount = 0;
                var circuitLimitsFound = 0;
                var storedSnapshots = 0;

                for (int batchIndex = 0; batchIndex < totalBatches; batchIndex++)
                {
                    var batch = allIndexOptions.Skip(batchIndex * batchSize).Take(batchSize).ToList();
                    
                    System.Console.WriteLine($"📦 Batch {batchIndex + 1}/{totalBatches}: Processing {batch.Count} contracts...");
                    
                    try
                    {
                        // Get quotes for this batch
                        var instrumentTokens = batch.Select(i => i.InstrumentToken.ToString()).ToArray();
                        var quotes = await kiteService.GetQuotesAsync(instrumentTokens);
                        
                        foreach (var instrument in batch)
                        {
                            try
                            {
                                var tokenString = instrument.InstrumentToken.ToString();
                                
                                if (quotes.ContainsKey(tokenString))
                                {
                                    var quote = quotes[tokenString];
                                    var underlyingSpot = spotPrices.GetValueOrDefault(instrument.Name, 0);
                                    
                                    // Create comprehensive intraday snapshot with timestamp and spot tracking
                                    var snapshot = new IntradayOptionSnapshot
                                    {
                                        InstrumentToken = tokenString,
                                        Symbol = instrument.TradingSymbol,
                                        UnderlyingSymbol = instrument.Name,
                                        StrikePrice = instrument.Strike,
                                        OptionType = instrument.InstrumentType ?? "CE",
                                        ExpiryDate = instrument.Expiry ?? DateTime.Today.AddDays(7),
                                        
                                        // Price data
                                        LastPrice = quote.LastPrice,
                                        Open = quote.Open,
                                        High = quote.High,
                                        Low = quote.Low,
                                        Close = quote.Close,
                                        Change = quote.Change,
                                        
                                        // Volume and OI
                                        Volume = quote.Volume,
                                        OpenInterest = quote.OpenInterest,
                                        
                                        // 🔥 CIRCUIT LIMITS - Core requirement
                                        LowerCircuitLimit = quote.LowerCircuitLimit,
                                        UpperCircuitLimit = quote.UpperCircuitLimit,
                                        CircuitLimitStatus = DetermineCircuitStatus(quote),
                                        
                                        // Greeks
                                        ImpliedVolatility = 0, // Not available in quote API
                                        
                                        // 🔥 TIMESTAMP TRACKING - IST format (same as Kite API)
                                        Timestamp = GetCurrentIST(),
                                        CaptureTime = GetCurrentIST(),
                                        LastUpdated = GetCurrentIST(),
                                        
                                        // Data quality
                                        IsValidData = quote.LastPrice > 0,
                                        ValidationMessage = quote.LastPrice > 0 ? "Valid" : "No trading activity",
                                        TradingStatus = quote.Volume > 0 ? "Active" : "No Volume"
                                    };
                                    
                                    // Store in database
                                    await intradayService.SaveSnapshotAsync(snapshot);
                                    storedSnapshots++;
                                    
                                    // Track circuit limits
                                    if (quote.LowerCircuitLimit > 0 && quote.UpperCircuitLimit > 0)
                                    {
                                        circuitLimitsFound++;
                                        
                                        // Also store spot price tracking
                                        StoreSpotPriceTracking(dbContext, instrument.Name, underlyingSpot, collectionTime);
                                    }
                                    
                                    processedCount++;
                                    
                                    // Log progress for significant strikes
                                    if (quote.Volume > 0 || (batchIndex == 0 && processedCount <= 5))
                                    {
                                        System.Console.WriteLine($"📈 {instrument.TradingSymbol}: LC={quote.LowerCircuitLimit:F2}, UC={quote.UpperCircuitLimit:F2}, " +
                                                               $"LTP={quote.LastPrice:F2}, Spot={underlyingSpot:F2}, Vol={quote.Volume}");
                                    }
                                }
                                else
                                {
                                    failedCount++;
                                }
                            }
                            catch (Exception ex)
                            {
                                System.Console.WriteLine($"⚠️ Error processing {instrument.TradingSymbol}: {ex.Message}");
                                failedCount++;
                            }
                        }
                        
                        // Save batch to database
                        await dbContext.SaveChangesAsync();
                        
                        // Progress update
                        var progressPercent = (double)(batchIndex + 1) / totalBatches * 100;
                        System.Console.WriteLine($"   ✅ Batch completed. Progress: {progressPercent:F1}% | Stored: {storedSnapshots} | Circuit limits: {circuitLimitsFound}");
                        
                        // Rate limiting delay
                        await Task.Delay(500); // 500ms between batches
                    }
                    catch (Exception ex)
                    {
                        System.Console.WriteLine($"❌ Batch {batchIndex + 1} failed: {ex.Message}");
                        await Task.Delay(1000); // Longer delay on error
                    }
                }

                // Final summary
                System.Console.WriteLine();
                System.Console.WriteLine($"🎉 === COMPREHENSIVE COLLECTION COMPLETED ===");
                System.Console.WriteLine($"   📊 Total Processed: {processedCount:N0} contracts");
                System.Console.WriteLine($"   ✅ Successfully Stored: {storedSnapshots:N0} snapshots");
                System.Console.WriteLine($"   🔥 Circuit Limits Found: {circuitLimitsFound:N0} contracts");
                System.Console.WriteLine($"   ❌ Failed: {failedCount:N0} contracts");
                System.Console.WriteLine($"   🕐 Collection Time: {collectionTime:yyyy-MM-dd HH:mm:ss} UTC");
                System.Console.WriteLine($"   📈 Spot Prices: {string.Join(", ", spotPrices.Select(s => $"{s.Key}={s.Value:F2}"))}");
                System.Console.WriteLine();
                System.Console.WriteLine("💡 TIP: Use GetYesterdayCircuitLimits.sql to query stored data");
                System.Console.WriteLine("�� All data stored in IntradayOptionSnapshots table with full timestamp tracking");
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"❌ Error in comprehensive collection: {ex.Message}");
            }

            System.Console.WriteLine();
            System.Console.WriteLine("Press any key to exit...");
            System.Console.ReadKey();
        }
        
        /// <summary>
        /// Get current spot prices for all supported indices
        /// </summary>
        private static async Task<Dictionary<string, decimal>> GetCurrentSpotPrices(IKiteConnectService kiteService)
        {
            var spotPrices = new Dictionary<string, decimal>();
            
            // Map of index names to their spot instrument tokens (these need to be updated with actual tokens)
            var spotTokens = new Dictionary<string, string>
            {
                { "NIFTY", "256265" },      // NIFTY 50 index token
                { "BANKNIFTY", "260105" },  // BANK NIFTY index token  
                { "SENSEX", "265" },        // SENSEX index token
                { "BANKEX", "274441" },         // BANKEX index token
                { "FINNIFTY", "257801" },   // FINNIFTY index token
                { "MIDCPNIFTY", "288009" }  // MIDCAP NIFTY index token
            };
            
            try
            {
                var allTokens = spotTokens.Values.ToArray();
                var quotes = await kiteService.GetQuotesAsync(allTokens);
                
                foreach (var kvp in spotTokens)
                {
                    var indexName = kvp.Key;
                    var token = kvp.Value;
                    
                    if (quotes.ContainsKey(token))
                    {
                        spotPrices[indexName] = quotes[token].LastPrice;
                    }
                    else
                    {
                        spotPrices[indexName] = 0; // Default if not available
                    }
                }
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"⚠️ Error fetching spot prices: {ex.Message}");
                // Set default values
                spotPrices["NIFTY"] = 25000;
                spotPrices["BANKNIFTY"] = 52000;
                spotPrices["SENSEX"] = 82000;
                spotPrices["BANKEX"] = 57000;
                spotPrices["FINNIFTY"] = 23000;
                spotPrices["MIDCPNIFTY"] = 11000;
            }
            
            return spotPrices;
        }
        
        /// <summary>
        /// Store spot price tracking for underlying indices
        /// </summary>
        private static void StoreSpotPriceTracking(ApplicationDbContext dbContext, string indexName, decimal spotPrice, DateTime timestamp)
        {
            try
            {
                var spotData = new DomainSpotData
                {
                    Symbol = indexName,
                    Exchange = indexName.Contains("SENSEX") || indexName.Contains("BANKEX") ? "BSE" : "NSE",
                    LastPrice = spotPrice,
                    Timestamp = timestamp,
                    CapturedAt = timestamp,
                    IsValidData = spotPrice > 0,
                    ValidationMessage = spotPrice > 0 ? "Valid spot price" : "No spot price available"
                };
                
                dbContext.SpotData.Add(spotData);
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"⚠️ Error storing spot price for {indexName}: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Determine circuit limit status
        /// </summary>
        private static string DetermineCircuitStatus(OptionAnalysisTool.KiteConnect.Models.KiteQuote quote)
        {
            if (quote.LastPrice <= quote.LowerCircuitLimit) return "Lower Circuit";
            if (quote.LastPrice >= quote.UpperCircuitLimit) return "Upper Circuit";
            if (quote.LastPrice <= quote.LowerCircuitLimit * 1.02m) return "Near Lower Circuit";
            if (quote.LastPrice >= quote.UpperCircuitLimit * 0.98m) return "Near Upper Circuit";
            return "Normal";
        }

        private static async Task TestSpotDataCollection(IKiteConnectService kiteService, ApplicationDbContext dbContext)
        {
            try
            {
                System.Console.WriteLine("🔥 === TESTING SPOT DATA COLLECTION ===");
                
                // Test tokens for spot indices
                var spotIndices = new Dictionary<string, string>
                {
                    { "NIFTY", "256265" },
                    { "BANKNIFTY", "260105" },
                    { "FINNIFTY", "257801" },
                    { "MIDCPNIFTY", "288009" },
                    { "SENSEX", "265" },
                    { "BANKEX", "274441" }
                };

                foreach (var index in spotIndices)
                {
                    try
                    {
                        System.Console.WriteLine($"📊 Testing {index.Key} (Token: {index.Value})...");
                        var quote = await kiteService.GetQuoteAsync(index.Value);
                        
                        if (quote != null && quote.LastPrice > 0)
                        {
                            var percentageChange = quote.Close > 0 ? (quote.Change / quote.Close) * 100 : 0;
                            System.Console.WriteLine($"✅ {index.Key}: {quote.LastPrice} ({quote.Change:+#,##0.00;-#,##0.00}) ({percentageChange:+#,##0.00;-#,##0.00}%)");
                            
                            // Save to database
                            var spotData = new DomainSpotData
                            {
                                Symbol = index.Key,
                                Exchange = index.Key == "SENSEX" || index.Key == "BANKEX" ? "BSE" : "NSE",
                                LastPrice = quote.LastPrice,
                                Change = quote.Change,
                                PercentageChange = percentageChange,
                                Open = quote.Open,
                                High = quote.High,
                                Low = quote.Low,
                                Close = quote.Close,
                                Volume = quote.Volume,
                                LowerCircuitLimit = quote.LowerCircuitLimit,
                                UpperCircuitLimit = quote.UpperCircuitLimit,
                                CircuitStatus = "Normal",
                                Timestamp = DateTime.Now,
                                LastUpdated = DateTime.Now,
                                CapturedAt = DateTime.Now,
                                IsValidData = true,
                                ValidationMessage = "Test data from console"
                            };
                            
                            dbContext.SpotData.Add(spotData);
                        }
                        else
                        {
                            System.Console.WriteLine($"❌ {index.Key}: No valid quote received");
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Console.WriteLine($"❌ {index.Key}: Error - {ex.Message}");
                    }
                }
                
                await dbContext.SaveChangesAsync();
                System.Console.WriteLine("✅ Spot data test completed and saved to database");
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"❌ Error in spot data test: {ex.Message}");
            }
        }

        private static async Task TestSpotDataCollectionMode()
        {
            try
            {
                System.Console.WriteLine("🔥 === TESTING SPOT DATA COLLECTION ===");
                
                // Check authentication
                var isAuthenticated = await CheckAuthenticationStatusAsync();
                if (!isAuthenticated)
                {
                    System.Console.WriteLine("❌ No valid authentication token found!");
                    System.Console.WriteLine("Please authenticate first using the main application.");
                    return;
                }

                // Create services
                var host = CreateHostBuilder(new string[0]).UseSerilog().Build();
                var kiteService = host.Services.GetRequiredService<IKiteConnectService>();
                var dbContext = host.Services.GetRequiredService<ApplicationDbContext>();
                
                // Get access token and set it
                var databaseTokenService = host.Services.GetRequiredService<DatabaseTokenService>();
                var accessToken = await databaseTokenService.GetCurrentAccessTokenAsync(ApiKey);
                if (!string.IsNullOrEmpty(accessToken))
                {
                    await kiteService.SetAccessToken(accessToken);
                }

                // Test spot data collection
                await TestSpotDataCollection(kiteService, dbContext);
                
                System.Console.WriteLine("✅ Spot data test completed!");
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"❌ Error in spot data test: {ex.Message}");
            }
        }

        private static async Task ClearAndReloadSpotDataMode()
        {
            try
            {
                System.Console.WriteLine("🔥 === CLEARING AND RELOADING SPOT DATA ===");
                
                // Check authentication
                var isAuthenticated = await CheckAuthenticationStatusAsync();
                if (!isAuthenticated)
                {
                    System.Console.WriteLine("❌ No valid authentication token found!");
                    System.Console.WriteLine("Please authenticate first using the main application.");
                    return;
                }

                // Create services
                var host = CreateHostBuilder(new string[0]).UseSerilog().Build();
                var kiteService = host.Services.GetRequiredService<IKiteConnectService>();
                var dbContext = host.Services.GetRequiredService<ApplicationDbContext>();
                
                // Get access token and set it
                var databaseTokenService = host.Services.GetRequiredService<DatabaseTokenService>();
                var accessToken = await databaseTokenService.GetCurrentAccessTokenAsync(ApiKey);
                if (!string.IsNullOrEmpty(accessToken))
                {
                    await kiteService.SetAccessToken(accessToken);
                }

                // Clear and reload spot data
                await ClearAndReloadSpotData(kiteService, dbContext);
                
                System.Console.WriteLine("✅ Spot data cleared and reloaded!");
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"❌ Error in clear and reload: {ex.Message}");
            }
        }

        private static async Task ClearAndReloadSpotData(IKiteConnectService kiteService, ApplicationDbContext dbContext)
        {
            try
            {
                System.Console.WriteLine("🔥 === CLEARING AND RELOADING SPOT DATA ===");
                
                // Step 1: Clear all existing spot data
                System.Console.WriteLine("🗑️ Clearing all existing spot data...");
                var existingSpotData = await dbContext.SpotData.ToListAsync();
                dbContext.SpotData.RemoveRange(existingSpotData);
                await dbContext.SaveChangesAsync();
                System.Console.WriteLine($"✅ Cleared {existingSpotData.Count} existing spot data records");
                
                // Step 2: Fetch real-time spot values from Kite API
                System.Console.WriteLine("📊 Fetching real-time spot values from Kite API...");
                
                var spotIndices = new Dictionary<string, string>
                {
                    { "NIFTY", "256265" },
                    { "BANKNIFTY", "260105" },
                    { "FINNIFTY", "257801" },
                    { "MIDCPNIFTY", "288009" },
                    { "SENSEX", "265" },
                    { "BANKEX", "274441" }
                };

                var today = DateTime.Today;
                var recordsAdded = 0;

                foreach (var index in spotIndices)
                {
                    try
                    {
                        System.Console.WriteLine($"📈 Fetching {index.Key} (Token: {index.Value})...");
                        var quote = await kiteService.GetQuoteAsync(index.Value);
                        
                        if (quote != null && quote.LastPrice > 0)
                        {
                            var percentageChange = quote.Close > 0 ? (quote.Change / quote.Close) * 100 : 0;
                            System.Console.WriteLine($"✅ {index.Key}: {quote.LastPrice:N2} ({quote.Change:+#,##0.00;-#,##0.00}) ({percentageChange:+#,##0.00;-#,##0.00}%)");
                            
                            // Create new spot data record with today's timestamp
                            var spotData = new DomainSpotData
                            {
                                Symbol = index.Key,
                                Exchange = index.Key == "SENSEX" || index.Key == "BANKEX" ? "BSE" : "NSE",
                                LastPrice = quote.LastPrice,
                                Change = quote.Change,
                                PercentageChange = percentageChange,
                                Open = quote.Open,
                                High = quote.High,
                                Low = quote.Low,
                                Close = quote.Close,
                                Volume = quote.Volume,
                                LowerCircuitLimit = quote.LowerCircuitLimit,
                                UpperCircuitLimit = quote.UpperCircuitLimit,
                                CircuitStatus = "Normal",
                                Timestamp = DateTime.Now,
                                LastUpdated = DateTime.Now,
                                CapturedAt = DateTime.Now,
                                IsValidData = true,
                                ValidationMessage = $"Real-time data from Kite API - {DateTime.Now:yyyy-MM-dd HH:mm:ss}"
                            };
                            
                            dbContext.SpotData.Add(spotData);
                            recordsAdded++;
                        }
                        else
                        {
                            System.Console.WriteLine($"❌ {index.Key}: No valid quote received");
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Console.WriteLine($"❌ {index.Key}: Error - {ex.Message}");
                    }
                }
                
                // Step 3: Save all new records
                await dbContext.SaveChangesAsync();
                System.Console.WriteLine($"✅ Successfully added {recordsAdded} new spot data records");
                System.Console.WriteLine($"📅 Data timestamp: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                System.Console.WriteLine("🎯 All spot data is now real-time from Kite API!");
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"❌ Error in clear and reload: {ex.Message}");
            }
        }

        private static async Task FindBankexToken(IKiteConnectService kiteService)
        {
            try
            {
                System.Console.WriteLine("🔍 === SEARCHING FOR CORRECT BANKEX TOKEN ===");
                
                // Get all BSE instruments
                System.Console.WriteLine("📊 Fetching all BSE instruments...");
                var bseInstruments = await kiteService.GetInstrumentsAsync("BSE");
                
                if (bseInstruments == null || !bseInstruments.Any())
                {
                    System.Console.WriteLine("❌ No BSE instruments found");
                    return;
                }
                
                System.Console.WriteLine($"📋 Found {bseInstruments.Count} BSE instruments");
                
                // Search for BANKEX-related instruments
                var bankexInstruments = bseInstruments
                    .Where(i => (i.Name?.Contains("BANKEX", StringComparison.OrdinalIgnoreCase) == true) ||
                               (i.TradingSymbol?.Contains("BANKEX", StringComparison.OrdinalIgnoreCase) == true))
                    .ToList();
                
                System.Console.WriteLine($"🎯 Found {bankexInstruments.Count} BANKEX-related instruments:");
                
                foreach (var instrument in bankexInstruments)
                {
                    System.Console.WriteLine($"   📋 Token: {instrument.InstrumentToken}");
                    System.Console.WriteLine($"      Name: {instrument.Name}");
                    System.Console.WriteLine($"      TradingSymbol: {instrument.TradingSymbol}");
                    System.Console.WriteLine($"      Exchange: {instrument.Exchange}");
                    System.Console.WriteLine($"      Segment: {instrument.Segment}");
                    System.Console.WriteLine($"      InstrumentType: {instrument.InstrumentType}");
                    System.Console.WriteLine();
                }
                
                // Also search for any instruments with "BANK" in the name
                var bankInstruments = bseInstruments
                    .Where(i => (i.Name?.Contains("BANK", StringComparison.OrdinalIgnoreCase) == true) ||
                               (i.TradingSymbol?.Contains("BANK", StringComparison.OrdinalIgnoreCase) == true))
                    .Take(20) // Limit to first 20 to avoid spam
                    .ToList();
                
                System.Console.WriteLine($"🏦 Found {bankInstruments.Count} BANK-related instruments (showing first 20):");
                
                foreach (var instrument in bankInstruments)
                {
                    System.Console.WriteLine($"   📋 Token: {instrument.InstrumentToken}");
                    System.Console.WriteLine($"      Name: {instrument.Name}");
                    System.Console.WriteLine($"      TradingSymbol: {instrument.TradingSymbol}");
                    System.Console.WriteLine();
                }
                
                // Test some common BANKEX tokens
                var commonTokens = new[] { "1", "12", "532", "12311", "265", "256265" };
                System.Console.WriteLine("🧪 Testing common BANKEX tokens:");
                
                foreach (var token in commonTokens)
                {
                    try
                    {
                        var quote = await kiteService.GetQuoteAsync(token);
                        if (quote != null && quote.LastPrice > 0)
                        {
                            System.Console.WriteLine($"✅ Token {token}: {quote.LastPrice} - {quote.InstrumentToken}");
                        }
                        else
                        {
                            System.Console.WriteLine($"❌ Token {token}: No valid quote");
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Console.WriteLine($"❌ Token {token}: Error - {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"❌ Error in FindBankexToken: {ex.Message}");
            }
        }

        private static async Task FindBankexTokenMode()
        {
            try
            {
                System.Console.WriteLine("🔍 === BANKEX TOKEN DISCOVERY MODE ===");
                
                // Check authentication
                var isAuthenticated = await CheckAuthenticationStatusAsync();
                if (!isAuthenticated)
                {
                    System.Console.WriteLine("❌ No valid authentication token found!");
                    var authSuccess = await LaunchGuiAuthenticationAsync();
                    if (!authSuccess)
                    {
                        System.Console.WriteLine("❌ Authentication failed. Cannot proceed without valid token.");
                        return;
                    }
                }
                
                // Create host and get services
                var host = CreateHostBuilder(new string[0]).UseSerilog().Build();
                var kiteService = host.Services.GetRequiredService<IKiteConnectService>();
                var databaseTokenService = host.Services.GetRequiredService<DatabaseTokenService>();
                var configuration = host.Services.GetRequiredService<IConfiguration>();
                
                // Get and set the access token
                var apiKey = configuration["KiteConnect:ApiKey"];
                var accessToken = await databaseTokenService.GetCurrentAccessTokenAsync(apiKey);
                if (!string.IsNullOrEmpty(accessToken))
                {
                    await kiteService.SetAccessToken(accessToken);
                    System.Console.WriteLine("✅ Access token set successfully");
                }
                else
                {
                    System.Console.WriteLine("❌ No access token available");
                    return;
                }
                
                // Find BANKEX token
                await FindBankexToken(kiteService);
                
                System.Console.WriteLine("✅ BANKEX token discovery complete!");
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"❌ Error in FindBankexTokenMode: {ex.Message}");
            }
        }

        private static async Task StartComprehensiveDataCollection()
        {
            try
            {
                System.Console.WriteLine("🔥 === INDIAN OPTION ANALYSIS - DATA COLLECTION SERVICE ===");
                System.Console.WriteLine("🎯 COLLECTING: Circuit Limits, Quotes, Option Data");
                System.Console.WriteLine("📊 COVERAGE: NIFTY, BANKNIFTY, SENSEX, BANKEX Options");
                System.Console.WriteLine("🔐 AUTHENTICATION: Automatic Token Management");
                System.Console.WriteLine();

                // 🚨 STEP 1: AUTOMATIC AUTHENTICATION CHECK
                System.Console.WriteLine("🔍 STEP 1: Checking authentication status...");
                var isAuthenticated = await CheckAuthenticationStatusAsync();
                
                if (!isAuthenticated)
                {
                    System.Console.WriteLine("❌ No valid authentication token found!");
                    System.Console.WriteLine();
                    
                    // 🔐 STEP 2: LAUNCH GUI AUTHENTICATION
                    var authSuccess = await LaunchGuiAuthenticationAsync();
                    
                    if (!authSuccess)
                    {
                        System.Console.WriteLine("❌ Authentication failed. Cannot proceed without valid token.");
                        System.Console.WriteLine("Press any key to exit...");
                        System.Console.ReadKey();
                        return;
                    }
                }
                else
                {
                    System.Console.WriteLine("✅ Valid authentication token found!");
                }

                System.Console.WriteLine();
                System.Console.WriteLine("🚀 STEP 3: Starting data collection services...");

                var host = CreateHostBuilder(new string[0]).UseSerilog().Build();
                await host.RunAsync();
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"❌ Error in StartComprehensiveDataCollection: {ex.Message}");
            }
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .UseWindowsService(options =>
                {
                    options.ServiceName = "IndianOptions_OptionMarketMonitorService";
                })
                .ConfigureServices((hostContext, services) =>
                {
                    // Add configuration
                    services.AddSingleton<IConfiguration>(hostContext.Configuration);

                    // Add database context
                    services.AddDbContext<ApplicationDbContext>(options =>
                        options.UseSqlServer(
                            hostContext.Configuration.GetConnectionString("DefaultConnection"),
                            sqlOptions => sqlOptions.EnableRetryOnFailure()
                        )
                    );

                    // Add KiteConnect dependencies
                    services.AddScoped<ExternalKite.Kite>(provider =>
                    {
                        var config = provider.GetRequiredService<KiteConnectConfig>();
                        return new ExternalKite.Kite(config.ApiKey, config.AccessToken);
                    });
                    services.AddScoped<KiteConnectWrapper>();
                    services.AddScoped<KiteConnectConfig>();

                    // Add repositories
                    services.AddScoped<IMarketDataRepository, MarketDataRepository>();
                    services.AddScoped<MarketDataRepository>();
                    services.AddScoped<DatabaseTokenService>();

                    // Add services
                    services.AddScoped<IKiteConnectService, KiteConnectService>();
                    services.AddScoped<MarketHoursService>();
                    services.AddScoped<CircuitLimitTrackingService>();
                    services.AddScoped<EODCircuitLimitProcessor>();
                    services.AddScoped<IntradayDataService>();
                    services.AddScoped<ComprehensiveInstrumentLoader>();
                    services.AddScoped<EODDataStorageService>();

                    // Add hosted services
                    services.AddHostedService<ComprehensiveAutonomousDataManager>();
                    services.AddHostedService<OptionMarketMonitorService>();
                })
                .ConfigureLogging((hostContext, logging) =>
                {
                    logging.ClearProviders();
                    logging.AddConfiguration(hostContext.Configuration.GetSection("Logging"));
                    logging.AddConsole();
                    logging.AddDebug();
                    logging.AddEventLog();
                });

        // Helper method to get current IST time
        private static DateTime GetCurrentIST()
        {
            try
            {
                var ist = TimeZoneInfo.FindSystemTimeZoneById("India Standard Time");
                return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, ist);
            }
            catch
            {
                return DateTime.Now; // Fallback to local time
            }
        }
    }
} 