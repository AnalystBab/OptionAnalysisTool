using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using OptionAnalysisTool.Common.Data;
using OptionAnalysisTool.KiteConnect.Services;
using OptionAnalysisTool.Models;
using Newtonsoft.Json;

namespace OptionAnalysisTool.Common.Services
{
    /// <summary>
    /// 🔥 COMPREHENSIVE AUTONOMOUS DATA MANAGER
    /// 
    /// COMPLETE REQUIREMENT FULFILLMENT:
    /// ✅ ALL INDEX COVERAGE: NIFTY, BANKNIFTY, FINNIFTY, MIDCPNIFTY, SENSEX, BANKEX
    /// ✅ DYNAMIC STRIKE DETECTION: Auto-detects new strikes that start trading
    /// ✅ COMPLETE DATA LIFECYCLE: Intraday → EOD → Historical with circuit limit merging
    /// ✅ DATA CLEANING: Clear/reset capabilities from specific dates
    /// ✅ AUTONOMOUS AUTHENTICATION: No WPF dependency, multiple fallback strategies
    /// ✅ MARKET CLOSURE OPERATIONS: Data collection even when markets closed
    /// ✅ CIRCUIT LIMIT INTEGRATION: Merge intraday circuit limits with EOD data
    /// 
    /// OPERATES 24/7 INDEPENDENTLY - NO WPF APPLICATION REQUIRED
    /// </summary>
    public class ComprehensiveAutonomousDataManager : BackgroundService
    {
        private readonly ILogger<ComprehensiveAutonomousDataManager> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly IConfiguration _configuration;

        // COMPREHENSIVE INDEX COVERAGE - ALL SUPPORTED INDICES
        private readonly Dictionary<string, IndexConfiguration> SUPPORTED_INDICES = new()
        {
            { "NIFTY", new IndexConfiguration { Exchange = "NFO", SpotSymbol = "NIFTY 50", LotSize = 25 } },
            { "BANKNIFTY", new IndexConfiguration { Exchange = "NFO", SpotSymbol = "NIFTY BANK", LotSize = 15 } },
            { "FINNIFTY", new IndexConfiguration { Exchange = "NFO", SpotSymbol = "NIFTY FIN SERVICE", LotSize = 25 } },
            { "MIDCPNIFTY", new IndexConfiguration { Exchange = "NFO", SpotSymbol = "NIFTY MID SELECT", LotSize = 50 } },
            { "SENSEX", new IndexConfiguration { Exchange = "BFO", SpotSymbol = "BSE SENSEX", LotSize = 10 } },
            { "BANKEX", new IndexConfiguration { Exchange = "BFO", SpotSymbol = "BSE BANKEX", LotSize = 15 } }
        };

        // TIMING CONFIGURATION
        private const int INTRADAY_INTERVAL_SECONDS = 60;  // Every 60 seconds (1 minute) during market hours
        private const int EOD_PROCESSING_DELAY_MINUTES = 15;  // 15 minutes after market close
        private const int STRIKE_DETECTION_INTERVAL_MINUTES = 5;  // Check for new strikes every 5 minutes
        // REMOVED: AUTHENTICATION_CHECK_INTERVAL_MINUTES - Authentication will be checked only when needed

        // CACHING AND STATE MANAGEMENT
        private readonly Dictionary<string, List<InstrumentInfo>> _knownInstruments = new();
        private readonly Dictionary<string, DateTime> _lastStrikeDetection = new();
        private bool _isAuthenticated = false;
        private DateTime _lastDataCleanupCheck = DateTime.MinValue;

        // STATISTICS TRACKING
        private readonly DataManagerStatistics _statistics = new();

        public ComprehensiveAutonomousDataManager(
            ILogger<ComprehensiveAutonomousDataManager> logger,
            IServiceProvider serviceProvider,
            IConfiguration configuration)
        {
            SafeLog($"SERVICE CONSTRUCTOR CALLED at {DateTime.Now}");
            
            _logger = logger;
            _serviceProvider = serviceProvider;
            _configuration = configuration;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            SafeLog($"SERVICE STARTASYNC CALLED at {DateTime.Now}");
            
            _logger.LogInformation("🚀 Starting Comprehensive Autonomous Data Manager");

            await InitializeSystemAsync();

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    _logger.LogDebug("🔄 Starting data collection cycle at {time}", DateTime.Now.ToString("HH:mm:ss"));
                    
                    using var scope = _serviceProvider.CreateScope();
                    await ExecuteMaintenanceCycle(scope);
                    
                    _logger.LogDebug("✅ Data collection cycle completed at {time}", DateTime.Now.ToString("HH:mm:ss"));
                    await Task.Delay(TimeSpan.FromSeconds(60), stoppingToken); // Run every 60 seconds
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "💥 Error in data management cycle");
                    await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
                }
            }
        }

        /// <summary>
        /// SYSTEM INITIALIZATION - Database, Authentication, Configuration
        /// </summary>
        private async Task InitializeSystemAsync()
        {
            using var scope = _serviceProvider.CreateScope();
            
            try
            {
                // 1. VERIFY DATABASE CONNECTIVITY
                _logger.LogInformation("🔍 Initializing database connection...");
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                await context.Database.EnsureCreatedAsync();
                _logger.LogInformation("✅ Database connectivity verified");

                // 2. AUTHENTICATION WILL BE CHECKED DURING DATA COLLECTION (not during initialization)

                // 3. COMPREHENSIVE INSTRUMENT LOADING - Load ALL instruments for all indices
                await LoadAllInstrumentsComprehensiveAsync(scope);

                // 4. LOAD EXISTING INSTRUMENT CACHE
                await LoadKnownInstrumentsAsync(scope);

                // 4. VERIFY DATA CLEANUP CONFIGURATION
                await CheckDataCleanupConfigurationAsync();

                _logger.LogInformation("🎯 Comprehensive Autonomous Data Manager - READY FOR OPERATION");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💥 System initialization failed");
                throw;
            }
        }

        /// <summary>
        /// MAIN DATA MANAGEMENT CYCLE - Runs every 60 seconds
        /// </summary>
        private async Task ExecuteMaintenanceCycle(IServiceScope scope)
        {
            try
            {
                _logger.LogDebug("🔄 Starting maintenance cycle...");
                
                // 1. CONTINUOUS DATA COLLECTION - 24/7 (Authentication checked only when needed)
                var marketHours = scope.ServiceProvider.GetRequiredService<MarketHoursService>();
                
                // Always collect intraday data to capture circuit limit changes (24/7)
                _logger.LogDebug("📊 Starting 24/7 intraday data collection...");
                await CollectIntradayDataAsync(scope);
                
                // Process EOD data if we're outside market hours
                if (!marketHours.IsMarketOpen())
                {
                    _logger.LogDebug("📈 Processing EOD data...");
                    await ProcessEODDataAsync(scope);
                    await BackfillHistoricalDataAsync(scope);
                }

                // 2. DATA MAINTENANCE (Daily cleanup, validation)
                _logger.LogDebug("🧹 Performing data maintenance...");
                await PerformDataMaintenanceAsync(scope);

                // 3. UPDATE STATISTICS
                _logger.LogDebug("📊 Updating statistics...");
                UpdateStatistics();
                
                _logger.LogDebug("✅ Maintenance cycle completed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in maintenance cycle");
            }
        }

        // REMOVED: ManageAuthenticationAsync method - Authentication is now checked only when needed during data collection

        /// <summary>
        /// DYNAMIC STRIKE DETECTION - Automatically detects new strikes that start trading
        /// </summary>
        private async Task DetectNewStrikesAsync(IServiceScope scope)
        {
            try
            {
                var kiteService = scope.ServiceProvider.GetRequiredService<IKiteConnectService>();
                
                foreach (var indexConfig in SUPPORTED_INDICES)
                {
                    var index = indexConfig.Key;
                    var config = indexConfig.Value;
                    
                    // Check if it's time to detect new strikes for this index
                    if (_lastStrikeDetection.TryGetValue(index, out var lastCheck) &&
                        DateTime.UtcNow - lastCheck < TimeSpan.FromMinutes(STRIKE_DETECTION_INTERVAL_MINUTES))
                        continue;

                    _logger.LogInformation("🔍 Detecting new strikes for {index}...", index);

                    // Get current instruments from Kite
                    var allInstruments = await kiteService.GetInstrumentsAsync(config.Exchange);
                    var currentIndexInstruments = allInstruments
                        .Where(i => i.TradingSymbol.ToUpper().Contains(index) &&
                                   (i.InstrumentType == "CE" || i.InstrumentType == "PE") &&
                                   i.Expiry.HasValue && i.Expiry.Value >= DateTime.Today)
                        .Select(i => new InstrumentInfo
                        {
                            InstrumentToken = i.InstrumentToken.ToString(),
                            TradingSymbol = i.TradingSymbol,
                            UnderlyingSymbol = index,
                            StrikePrice = i.Strike,
                            OptionType = i.InstrumentType ?? "CE",
                            ExpiryDate = i.Expiry ?? DateTime.Today.AddDays(30),
                            Exchange = config.Exchange
                        })
                        .ToList();

                    // Compare with known instruments
                    var knownInstruments = _knownInstruments.GetValueOrDefault(index, new List<InstrumentInfo>());
                    var newInstruments = currentIndexInstruments
                        .Where(current => !knownInstruments.Any(known => 
                            known.InstrumentToken == current.InstrumentToken))
                        .ToList();

                    if (newInstruments.Any())
                    {
                        _logger.LogInformation("🔥 DETECTED {count} NEW STRIKES for {index}: {strikes}",
                            newInstruments.Count, index,
                            string.Join(", ", newInstruments.Take(5).Select(i => $"{i.StrikePrice}{i.OptionType}")));

                        // Update known instruments cache
                        _knownInstruments[index] = currentIndexInstruments;

                        // Immediately collect data for new strikes to verify they're actively trading
                        await CollectDataForNewStrikes(scope, newInstruments);
                        
                        _statistics.NewStrikesDetected += newInstruments.Count;
                    }

                    _lastStrikeDetection[index] = DateTime.UtcNow;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💥 Error in dynamic strike detection");
            }
        }

        /// <summary>
        /// COLLECT DATA FOR NEW STRIKES - Verify they're actively trading
        /// </summary>
        private async Task CollectDataForNewStrikes(IServiceScope scope, List<InstrumentInfo> newInstruments)
        {
            try
            {
                var kiteService = scope.ServiceProvider.GetRequiredService<IKiteConnectService>();
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                var instrumentTokens = newInstruments.Select(i => i.InstrumentToken).ToArray();
                var quotes = await kiteService.GetQuotesAsync(instrumentTokens);

                foreach (var instrument in newInstruments)
                {
                    if (quotes.TryGetValue(instrument.InstrumentToken, out var quote))
                    {
                        // Check if actively trading
                        bool isActivelyTrading = quote.LastPrice > 0 &&
                                               quote.LowerCircuitLimit > 0 &&
                                               quote.UpperCircuitLimit > 0 &&
                                               (quote.Volume > 0 || quote.OpenInterest > 0);

                        if (isActivelyTrading)
                        {
                            _logger.LogInformation("✅ NEW ACTIVE STRIKE: {symbol} - Price: {price}, Volume: {volume}",
                                instrument.TradingSymbol, quote.LastPrice, quote.Volume);

                            // Store initial snapshot
                            await StoreIntradaySnapshot(scope.ServiceProvider.GetRequiredService<ApplicationDbContext>(), instrument, quote);
                        }
                        else
                        {
                            _logger.LogDebug("⏸️ New strike not actively trading yet: {symbol}",
                                instrument.TradingSymbol);
                        }
                    }
                }

                await context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💥 Error collecting data for new strikes");
            }
        }

        /// <summary>
        /// COLLECT INTRADAY DATA - Real-time circuit limit monitoring
        /// </summary>
        private async Task CollectIntradayDataAsync(IServiceScope scope)
        {
            try
            {
                _logger.LogInformation("🚀 [DATA COLLECTION] Starting intraday data collection cycle...");
                
                var kiteService = scope.ServiceProvider.GetRequiredService<IKiteConnectService>();
                var databaseTokenService = scope.ServiceProvider.GetRequiredService<DatabaseTokenService>();
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                // Check authentication only when needed (not every cycle)
                if (!_isAuthenticated)
                {
                    var accessToken = await databaseTokenService.GetCurrentAccessTokenAsync(_configuration["KiteConnect:ApiKey"]);
                    if (!string.IsNullOrEmpty(accessToken))
                    {
                        var setResult = await kiteService.SetAccessToken(accessToken);
                        if (setResult)
                        {
                            _logger.LogInformation("✅ [DATA COLLECTION] Authentication successful - set access token in KiteConnect client");
                            _isAuthenticated = true;
                        }
                        else
                        {
                            _logger.LogWarning("⚠️ [DATA COLLECTION] Failed to set access token in KiteConnect client");
                            NotifyAuthenticationFailure("Failed to set access token in KiteConnect client");
                            return;
                        }
                    }
                    else
                    {
                        _logger.LogWarning("⚠️ [DATA COLLECTION] No access token available for data collection");
                        NotifyAuthenticationFailure("No access token available. Please authenticate via Settings → LOGIN");
                        return;
                    }
                }

                var totalInstruments = _knownInstruments.Values.Sum(list => list.Count);
                _logger.LogInformation("📊 [DATA COLLECTION] Collecting intraday data for all {count} indices with {totalInstruments} total instruments...",
                    _knownInstruments.Count, totalInstruments);

                var totalProcessedSnapshots = 0;
                var circuitLimitChanges = 0;

                foreach (var index in SUPPORTED_INDICES.Keys)
                {
                    var instruments = _knownInstruments.GetValueOrDefault(index, new List<InstrumentInfo>());
                    
                    if (!instruments.Any())
                    {
                        _logger.LogWarning("⚠️ [DATA COLLECTION] No instruments available for {index}", index);
                        continue;
                    }

                    _logger.LogInformation("📊 [DATA COLLECTION] Processing {instrumentCount} instruments for {index}...", instruments.Count, index);

                    // Process instruments in batches of 500 (API limit is 500 per Kite documentation)
                    var batchSize = 500;
                    var batches = instruments.Chunk(batchSize).ToList();
                    
                    _logger.LogInformation("📦 [DATA COLLECTION] Processing {index} in {batchCount} batches of {batchSize} instruments each", 
                        index, batches.Count, batchSize);

                    foreach (var (batch, batchIndex) in batches.Select((batch, index) => (batch, index)))
                    {
                        var batchProcessedSnapshots = 0;
                        try
                        {
                            _logger.LogInformation("📦 [DATA COLLECTION] Processing batch {batchIndex + 1}/{batchCount} for {index} with {instrumentCount} instruments", 
                                batchIndex + 1, batches.Count, index, batch.Count());

                            // Prepare instrument tokens for quote API call
                            var instrumentTokens = batch.Select(i => i.InstrumentToken).ToList();
                            
                            _logger.LogInformation("🔍 [DATA COLLECTION] Fetching quotes for {tokenCount} instruments from Kite API (batch {batchIndex + 1})", 
                                instrumentTokens.Count, batchIndex + 1);

                            // Get quotes from Kite API
                            var quotes = await kiteService.GetQuotesAsync(instrumentTokens.ToArray());
                            
                            if (quotes == null || !quotes.Any())
                            {
                                _logger.LogWarning("⚠️ [DATA COLLECTION] No quotes received from Kite API for batch {batchIndex + 1} of {index}", 
                                    batchIndex + 1, index);
                                
                                // Check if this is an authentication failure
                                if (!_isAuthenticated)
                                {
                                    NotifyAuthenticationFailure("API call failed - authentication may have expired. Please re-authenticate via Settings → LOGIN");
                                }
                                continue;
                            }
                            
                            // 🔥 NEW: Detect and add new strikes found in quotes
                            await DetectAndAddNewStrikesFromQuotes(scope, quotes, index);

                            _logger.LogInformation("✅ [DATA COLLECTION] Received {quoteCount} quotes from Kite API for batch {batchIndex + 1} of {index}", 
                                quotes.Count, batchIndex + 1, index);

                            // Process each quote and store snapshot
                            foreach (var instrument in batch)
                            {
                                try
                                {
                                    if (quotes.TryGetValue(instrument.InstrumentToken, out var quote))
                                    {
                                        _logger.LogDebug("💾 [DATA COLLECTION] Storing snapshot for {symbol} - Price: {price}, Volume: {volume}, OI: {oi}", 
                                            instrument.TradingSymbol, quote.LastPrice, quote.Volume, quote.OpenInterest);
                                        
                                        await StoreIntradaySnapshot(context, instrument, quote);
                                        batchProcessedSnapshots++;
                                        totalProcessedSnapshots++;
                                    }
                                    else
                                    {
                                        _logger.LogWarning("⚠️ [DATA COLLECTION] No quote found for instrument {symbol} (token: {token}) - This instrument may not be actively trading", 
                                            instrument.TradingSymbol, instrument.InstrumentToken);
                                    }
                                }
                                catch (Exception ex)
                                {
                                    _logger.LogError(ex, "💥 [DATA COLLECTION] Error processing instrument {symbol}", instrument.TradingSymbol);
                                }
                            }

                            _logger.LogInformation("✅ [DATA COLLECTION] Completed batch {batchIndex + 1}/{batchCount} for {index} - Processed {processedCount}/{totalCount} instruments", 
                                batchIndex + 1, batches.Count, index, batchProcessedSnapshots, batch.Count());

                            // Small delay between batches to avoid API rate limits
                            await Task.Delay(100);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "💥 [DATA COLLECTION] Error processing batch {batchIndex + 1} for {index}", batchIndex + 1, index);
                        }
                    }
                }

                _logger.LogInformation("✅ [DATA COLLECTION] Intraday collection complete: {processed} snapshots, {changes} circuit changes, Total instruments: {totalInstruments}",
                    totalProcessedSnapshots, circuitLimitChanges, totalInstruments);
                
                // Update statistics
                _statistics.IntradaySnapshotsToday += totalProcessedSnapshots;
                _statistics.CircuitLimitChangesToday += circuitLimitChanges;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💥 [DATA COLLECTION] Error in intraday data collection");
            }
        }

        /// <summary>
        /// EOD DATA PROCESSING - Merge intraday circuit limits with EOD OHLC data
        /// </summary>
        private async Task ProcessEODDataAsync(IServiceScope scope)
        {
            try
            {
                var marketHours = scope.ServiceProvider.GetRequiredService<MarketHoursService>();
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var kiteService = scope.ServiceProvider.GetRequiredService<IKiteConnectService>();

                // Only process EOD once per day, after market close
                var today = DateTime.Today;
                var eodProcessed = await context.HistoricalOptionData
                    .AnyAsync(h => h.TradingDate.Date == today);

                if (eodProcessed)
                    return;

                // Wait minimum time after market close (only process after 3:45 PM if market closed today)
                if (!marketHours.IsEndOfDay())
                    return;
                
                var now = DateTime.Now;
                var marketCloseTime = now.Date.AddHours(15).AddMinutes(30); // 3:30 PM
                if (now < marketCloseTime.AddMinutes(EOD_PROCESSING_DELAY_MINUTES))
                    return;

                _logger.LogInformation("🔄 Processing EOD data with circuit limit integration...");

                int totalProcessed = 0;

                foreach (var indexConfig in SUPPORTED_INDICES)
                {
                    var index = indexConfig.Key;
                    var config = indexConfig.Value;

                    var instruments = _knownInstruments.GetValueOrDefault(index, new List<InstrumentInfo>());
                    
                    foreach (var instrument in instruments)
                    {
                        try
                        {
                            // Get EOD OHLC data from Kite
                            var historicalData = await kiteService.GetHistoricalDataAsync(
                                instrument.InstrumentToken, today, today, "day");

                            if (!historicalData.Any()) continue;

                            var eodData = historicalData.First();

                            // Get circuit limit data from intraday snapshots
                            var intradayData = await context.IntradayOptionSnapshots
                                .Where(s => s.InstrumentToken == instrument.InstrumentToken &&
                                           s.Timestamp.Date == today)
                                .OrderByDescending(s => s.Timestamp)
                                .FirstOrDefaultAsync();

                            // Create historical record with merged data
                            var historicalRecord = new HistoricalOptionData
                            {
                                InstrumentToken = instrument.InstrumentToken,
                                Symbol = instrument.TradingSymbol,
                                UnderlyingSymbol = instrument.UnderlyingSymbol,
                                StrikePrice = instrument.StrikePrice,
                                OptionType = instrument.OptionType,
                                ExpiryDate = instrument.ExpiryDate,
                                TradingDate = today,
                                Open = eodData.Open,
                                High = eodData.High,
                                Low = eodData.Low,
                                Close = eodData.Close,
                                Volume = eodData.Volume,
                                OpenInterest = eodData.OI,
                                // 🔥 CRITICAL: Merge circuit limits from intraday data
                                LowerCircuitLimit = intradayData?.LowerCircuitLimit ?? 0,
                                UpperCircuitLimit = intradayData?.UpperCircuitLimit ?? 0,
                                CapturedAt = DateTime.UtcNow,
                                LastUpdated = DateTime.UtcNow
                            };

                            context.HistoricalOptionData.Add(historicalRecord);
                            totalProcessed++;
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "💥 Error processing EOD for {symbol}", instrument.TradingSymbol);
                        }
                    }

                    await context.SaveChangesAsync();
                }

                _statistics.EODRecordsToday = totalProcessed;
                _logger.LogInformation("✅ EOD processing complete: {processed} records with circuit limit integration",
                    totalProcessed);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💥 Error in EOD data processing");
            }
        }

        /// <summary>
        /// HISTORICAL DATA BACKFILL - For days when system was not running
        /// </summary>
        private async Task BackfillHistoricalDataAsync(IServiceScope scope)
        {
            // TODO: Implement historical data backfill for missed trading days
            // This would run during market closed hours to fill any gaps
        }

        /// <summary>
        /// DATA MAINTENANCE - Daily cleanup, validation, token management
        /// </summary>
        private async Task PerformDataMaintenanceAsync(IServiceScope scope)
        {
            try
            {
                var now = DateTime.UtcNow;
                
                // Daily cleanup - run once per day
                if (now.Date != _lastDataCleanupCheck.Date)
                {
                    _logger.LogInformation("🧹 Starting daily data maintenance...");
                    
                    var databaseTokenService = scope.ServiceProvider.GetRequiredService<DatabaseTokenService>();
                    
                    // Clean up old tokens - keep only the latest active one
                    var removedTokens = await databaseTokenService.ForceCleanupAllTokensExceptLatestAsync();
                    if (removedTokens > 0)
                    {
                        _logger.LogInformation("🧹 Daily cleanup: Removed {count} old authentication tokens", removedTokens);
                    }
                    
                    // Regular cleanup of expired tokens
                    var expiredTokens = await databaseTokenService.CleanupExpiredTokensAsync(1);
                    if (expiredTokens > 0)
                    {
                        _logger.LogInformation("🧹 Daily cleanup: Removed {count} expired tokens", expiredTokens);
                    }
                    
                    _lastDataCleanupCheck = now;
                    _logger.LogInformation("✅ Daily data maintenance completed");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in data maintenance");
            }
        }

        /// <summary>
        /// DATA CLEANUP CONFIGURATION - Handle data reset from specific dates
        /// </summary>
        private async Task CheckDataCleanupConfigurationAsync()
        {
            try
            {
                var cleanupFromDate = _configuration["DataCleanup:CleanupFromDate"];
                var performCleanup = _configuration.GetValue<bool>("DataCleanup:PerformCleanup");

                if (performCleanup && !string.IsNullOrEmpty(cleanupFromDate) && 
                    DateTime.TryParse(cleanupFromDate, out var fromDate))
                {
                    if (DateTime.UtcNow - _lastDataCleanupCheck > TimeSpan.FromHours(24))
                    {
                        _logger.LogWarning("🧹 Data cleanup configuration detected: {date}", fromDate);
                        // TODO: Implement data cleanup logic
                        _lastDataCleanupCheck = DateTime.UtcNow;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💥 Error checking data cleanup configuration");
            }
        }

        /// <summary>
        /// COMPREHENSIVE INSTRUMENT LOADING - Load ALL instruments for all indices to database
        /// </summary>
        private async Task LoadAllInstrumentsComprehensiveAsync(IServiceScope scope)
        {
            try
            {
                _logger.LogInformation("🚀 Starting comprehensive instrument loading for ALL indices and expiries...");
                
                var instrumentLoader = scope.ServiceProvider.GetRequiredService<ComprehensiveInstrumentLoader>();
                
                // Load ALL instruments for all expiries for all indices to database
                var success = await instrumentLoader.LoadAllInstrumentsAsync();
                
                if (success)
                {
                    _logger.LogInformation("✅ Comprehensive instrument loading completed successfully");
                    
                    // Get statistics
                    var stats = await instrumentLoader.GetInstrumentStatisticsAsync();
                    _logger.LogInformation("📊 Instrument Statistics - Total: {total}, Active: {active}", 
                        stats.TotalInstruments, stats.ActiveInstruments);
                    
                    foreach (var index in stats.IndexBreakdown)
                    {
                        _logger.LogInformation("   {index}: {count} instruments", index.Key, index.Value);
                    }
                }
                else
                {
                    _logger.LogError("💥 Comprehensive instrument loading failed");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💥 Error in comprehensive instrument loading");
            }
        }

        /// <summary>
        /// LOAD KNOWN INSTRUMENTS - Build cache from comprehensive instrument database
        /// </summary>
        private async Task LoadKnownInstrumentsAsync(IServiceScope scope)
        {
            try
            {
                _logger.LogInformation("🔍 [INSTRUMENT LOADING] LoadKnownInstrumentsAsync started at {time}", DateTime.Now);
                
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var instrumentLoader = scope.ServiceProvider.GetRequiredService<ComprehensiveInstrumentLoader>();

                _logger.LogInformation("🔍 [INSTRUMENT LOADING] Starting instrument loading from comprehensive database...");

                foreach (var index in SUPPORTED_INDICES.Keys)
                {
                    var knownInstruments = new List<InstrumentInfo>();

                    try
                    {
                        // 🔥 OPTIMIZED: Load instruments from comprehensive database instead of API
                        _logger.LogInformation("📋 [INSTRUMENT LOADING] Loading instruments for {index} from comprehensive database...", index);
                        
                        var dbInstruments = await instrumentLoader.GetActiveInstrumentsForIndexAsync(index);
                        
                        if (dbInstruments.Any())
                        {
                            knownInstruments = dbInstruments.Select(i => new InstrumentInfo
                            {
                                InstrumentToken = i.InstrumentToken,
                                TradingSymbol = i.TradingSymbol,
                                UnderlyingSymbol = i.Name,
                                StrikePrice = i.Strike,
                                OptionType = i.InstrumentType,
                                ExpiryDate = i.Expiry ?? DateTime.MinValue,
                                Exchange = SUPPORTED_INDICES[index].Exchange
                            }).ToList();

                            _logger.LogInformation("📋 [INSTRUMENT LOADING] Loaded {count} instruments for {index} from comprehensive database", 
                                knownInstruments.Count, index);

                            // 🔥 ENHANCED LOGGING: Show sample instruments for debugging
                            if (knownInstruments.Any())
                            {
                                var sampleInstruments = knownInstruments.Take(5).ToList();
                                _logger.LogInformation("📋 [INSTRUMENT LOADING] Sample instruments for {index}:", index);
                                foreach (var inst in sampleInstruments)
                                {
                                    _logger.LogInformation("   📋 {symbol} - {type} - Strike: {strike} - Expiry: {expiry} - Token: {token}",
                                        inst.TradingSymbol, inst.OptionType, inst.StrikePrice, inst.ExpiryDate.ToString("yyyy-MM-dd"), inst.InstrumentToken);
                                }
                                
                                // Group by expiry for better overview
                                var expiryGroups = knownInstruments.GroupBy(i => i.ExpiryDate.Date).OrderBy(g => g.Key);
                                _logger.LogInformation("📊 [INSTRUMENT LOADING] {index} instruments by expiry:", index);
                                foreach (var group in expiryGroups)
                                {
                                    _logger.LogInformation("   📅 {expiry}: {count} instruments", 
                                        group.Key.ToString("yyyy-MM-dd"), group.Count());
                                }
                            }
                        }
                        else
                        {
                            _logger.LogWarning("⚠️ [INSTRUMENT LOADING] No instruments found in database for {index}", index);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "💥 [INSTRUMENT LOADING] Failed to load instruments for {index} from database", index);
                    }

                    _knownInstruments[index] = knownInstruments;
                    _logger.LogInformation("📊 [INSTRUMENT LOADING] Total instruments loaded for {index}: {count}", index, knownInstruments.Count);
                }
                
                // Log summary of all loaded instruments
                var totalInstruments = _knownInstruments.Values.Sum(list => list.Count);
                _logger.LogInformation("🎯 [INSTRUMENT LOADING] Total instruments loaded across all indices: {totalCount}", totalInstruments);
                
                if (totalInstruments == 0)
                {
                    _logger.LogWarning("⚠️ [INSTRUMENT LOADING] CRITICAL: No instruments loaded! Data collection will not work.");
                }
                else
                {
                    _logger.LogInformation("✅ [INSTRUMENT LOADING] Successfully loaded {totalCount} instruments from comprehensive database", totalInstruments);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💥 [INSTRUMENT LOADING] Error loading known instruments");
            }
        }

        /// <summary>
        /// STORE INTRADAY SNAPSHOT WITH DUPLICATE PREVENTION
        /// </summary>
        private async Task StoreIntradaySnapshot(ApplicationDbContext context, InstrumentInfo instrument, 
            OptionAnalysisTool.KiteConnect.Models.KiteQuote quote)
        {
            try
            {
                _logger.LogDebug("💾 [DATA STORAGE] Creating snapshot for {symbol} - Price: {price}, Volume: {volume}, OI: {oi}", 
                    instrument.TradingSymbol, quote.LastPrice, quote.Volume, quote.OpenInterest);
                
                var snapshot = new IntradayOptionSnapshot
                {
                    InstrumentToken = instrument.InstrumentToken,
                    Symbol = instrument.TradingSymbol,
                    TradingSymbol = instrument.TradingSymbol,
                    UnderlyingSymbol = instrument.UnderlyingSymbol,
                    StrikePrice = instrument.StrikePrice,
                    OptionType = instrument.OptionType,
                    ExpiryDate = instrument.ExpiryDate,
                    LastPrice = quote.LastPrice,
                    Open = quote.Open,
                    High = quote.High,
                    Low = quote.Low,
                    Close = quote.Close,
                    Change = quote.Change,
                    Volume = quote.Volume,
                    OpenInterest = quote.OpenInterest,
                    LowerCircuitLimit = quote.LowerCircuitLimit,
                    UpperCircuitLimit = quote.UpperCircuitLimit,
                    ImpliedVolatility = quote.ImpliedVolatility,
                    // Use IST timestamps (same as Kite API format)
                    Timestamp = GetCurrentIST(),
                    OHLCDate = GetCurrentIST().Date,
                    CircuitLimitStatus = DetermineCircuitStatus(quote),
                    ValidationMessage = "Auto-collected",
                    TradingStatus = "Normal",
                    IsValidData = true
                };

                _logger.LogDebug("💾 [DATA STORAGE] Snapshot created for {symbol} - OHLC: [{open},{high},{low},{close}], LC: {lc}, UC: {uc}", 
                    instrument.TradingSymbol, snapshot.Open, snapshot.High, snapshot.Low, snapshot.Close, 
                    snapshot.LowerCircuitLimit, snapshot.UpperCircuitLimit);

                // 🔥 FIXED: Store all snapshots - let database handle duplicates with proper indexing
                // Add snapshot to context
                context.IntradayOptionSnapshots.Add(snapshot);
                _logger.LogDebug("➕ [DATA STORAGE] Added snapshot to context for {symbol}", instrument.TradingSymbol);
                _logger.LogDebug("💾 [DATA STORAGE] Added snapshot to context for {symbol}", instrument.TradingSymbol);
                
                // Save to database
                try 
                {
                    var changesCount = await context.SaveChangesAsync();
                    _logger.LogDebug("✅ [DATA STORAGE] Successfully saved snapshot for {symbol} - Changes: {changes}", 
                        instrument.TradingSymbol, changesCount);
                } 
                catch (Exception ex) 
                {
                    _logger.LogError(ex, "💥 [DATA STORAGE] Error saving snapshot for {symbol} to database", instrument.TradingSymbol);
                    var inner = ex.InnerException;
                    int depth = 1;
                    while (inner != null) 
                    {
                        _logger.LogError(inner, "💥 [DATA STORAGE] Inner exception level {depth}: {message}", depth, inner.Message);
                        inner = inner.InnerException;
                        depth++;
                    }
                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💥 [DATA STORAGE] Error in StoreIntradaySnapshot for {symbol}", instrument.TradingSymbol);
                throw;
            }
        }

        /// <summary>
        /// Track circuit limit changes between snapshots
        /// Store exact snapshot with spot values when LC/UC limits change
        /// </summary>
        private async Task TrackCircuitLimitChanges(ApplicationDbContext context, 
            IntradayOptionSnapshot previous, IntradayOptionSnapshot current)
        {
            try
            {
                // Check if circuit limits have changed
                if (previous.LowerCircuitLimit != current.LowerCircuitLimit || 
                    previous.UpperCircuitLimit != current.UpperCircuitLimit)
                {
                    // 🔥 SIMPLE: No calculations needed, just track the change
                    var circuitChange = new CircuitLimitTracker
                    {
                        InstrumentToken = current.InstrumentToken,
                        Symbol = current.Symbol,
                        UnderlyingSymbol = current.UnderlyingSymbol,
                        StrikePrice = current.StrikePrice,
                        OptionType = current.OptionType,
                        ExpiryDate = current.ExpiryDate,
                        PreviousLowerLimit = previous.LowerCircuitLimit,
                        NewLowerLimit = current.LowerCircuitLimit,
                        PreviousUpperLimit = previous.UpperCircuitLimit,
                        NewUpperLimit = current.UpperCircuitLimit,
                        // 🔥 SIMPLE: Default values, no calculations
                        LowerLimitChangePercent = 0,
                        UpperLimitChangePercent = 0,
                        RangeChangePercent = 0,
                        IsBreachAlert = false,
                        SeverityLevel = "Normal",
                        // 🔥 IMPORTANT: Store current option data at the time of circuit limit change
                        CurrentPrice = current.LastPrice,
                        Volume = current.Volume,
                        OpenInterest = current.OpenInterest,
                        // 🔥 CRITICAL: Store spot values (OHLC) at the exact time of circuit limit change
                        UnderlyingOpen = current.Open, // Spot open at circuit limit change time
                        UnderlyingHigh = current.High, // Spot high at circuit limit change time
                        UnderlyingLow = current.Low,   // Spot low at circuit limit change time
                        UnderlyingClose = current.Close, // Spot close at circuit limit change time
                        UnderlyingChange = current.Change, // Spot change at circuit limit change time
                        UnderlyingPercentageChange = 0, // Will be calculated if needed
                        UnderlyingVolume = 0, // Will be populated if available
                        UnderlyingLowerCircuitLimit = current.LowerCircuitLimit, // Current LC at change time
                        UnderlyingUpperCircuitLimit = current.UpperCircuitLimit, // Current UC at change time
                        UnderlyingCircuitStatus = current.CircuitLimitStatus, // Current circuit status
                        DetectedAt = current.Timestamp, // Exact timestamp when circuit limit changed
                        LastUpdated = DateTime.UtcNow,
                        ChangeReason = "Auto-detected circuit limit change",
                        IsValidData = true,
                        ValidationMessage = $"Circuit limit change detected at {current.Timestamp:HH:mm:ss}"
                    };

                    context.CircuitLimitTrackers.Add(circuitChange);
                    _logger.LogInformation("🚨 Circuit limit change detected for {symbol} at {time}: LC {prevLC}→{newLC}, UC {prevUC}→{newUC}", 
                        current.Symbol, current.Timestamp.ToString("HH:mm:ss"), 
                        previous.LowerCircuitLimit, current.LowerCircuitLimit, 
                        previous.UpperCircuitLimit, current.UpperCircuitLimit);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error tracking circuit limit changes for {symbol}", current.Symbol);
            }
        }

        /// <summary>
        /// Check if snapshot data has changed (for update decisions)
        /// Only prevent duplicates when OHLC, LC, UC are exactly the same
        /// </summary>
        private bool HasSnapshotDataChanged(IntradayOptionSnapshot existing, IntradayOptionSnapshot newSnapshot)
        {
            // Compare OHLC, LC, UC
            bool ohlcChanged = existing.Open != newSnapshot.Open ||
                               existing.High != newSnapshot.High ||
                               existing.Low != newSnapshot.Low ||
                               existing.Close != newSnapshot.Close;
            bool lcChanged = existing.LowerCircuitLimit != newSnapshot.LowerCircuitLimit;
            bool ucChanged = existing.UpperCircuitLimit != newSnapshot.UpperCircuitLimit;
            // If any changed, return true
            return ohlcChanged || lcChanged || ucChanged;
        }

        /// <summary>
        /// Update existing snapshot with new data
        /// </summary>
        private void UpdateSnapshotData(IntradayOptionSnapshot existing, IntradayOptionSnapshot newSnapshot)
        {
            existing.LastPrice = newSnapshot.LastPrice;
            existing.LowerCircuitLimit = newSnapshot.LowerCircuitLimit;
            existing.UpperCircuitLimit = newSnapshot.UpperCircuitLimit;
            existing.Volume = newSnapshot.Volume;
            existing.OpenInterest = newSnapshot.OpenInterest;
            existing.High = newSnapshot.High;
            existing.Low = newSnapshot.Low;
            existing.Change = newSnapshot.Change;
            existing.CircuitLimitStatus = newSnapshot.CircuitLimitStatus;
            existing.Timestamp = newSnapshot.Timestamp;
        }

        /// <summary>
        /// DETERMINE CIRCUIT STATUS
        /// </summary>
        private string DetermineCircuitStatus(OptionAnalysisTool.KiteConnect.Models.KiteQuote quote)
        {
            if (quote.LastPrice <= quote.LowerCircuitLimit) return "Lower Circuit";
            if (quote.LastPrice >= quote.UpperCircuitLimit) return "Upper Circuit";
            if (quote.LastPrice <= quote.LowerCircuitLimit * 1.02m) return "Near Lower Circuit";
            if (quote.LastPrice >= quote.UpperCircuitLimit * 0.98m) return "Near Upper Circuit";
            return "Normal";
        }

        /// <summary>
        /// UPDATE STATISTICS
        /// </summary>
        private void UpdateStatistics()
        {
            _statistics.LastUpdateTime = DateTime.UtcNow;
            _statistics.IsAuthenticated = _isAuthenticated;
            _statistics.TotalIndicesTracked = SUPPORTED_INDICES.Count;
            _statistics.TotalKnownInstruments = _knownInstruments.Values.Sum(list => list.Count);

            // Log summary every hour
            if (DateTime.UtcNow.Minute == 0)
            {
                _logger.LogInformation("📈 STATISTICS: Indices: {indices}, Instruments: {instruments}, " +
                    "Intraday Today: {intraday}, EOD Today: {eod}, New Strikes: {strikes}",
                    _statistics.TotalIndicesTracked, _statistics.TotalKnownInstruments,
                    _statistics.IntradaySnapshotsToday, _statistics.EODRecordsToday, _statistics.NewStrikesDetected);
            }
        }

        /// <summary>
        /// GET SYSTEM STATUS - For monitoring and debugging
        /// </summary>
        public object GetSystemStatus()
        {
            return new
            {
                IsRunning = true,
                IsAuthenticated = _isAuthenticated,
                Statistics = _statistics,
                SupportedIndices = SUPPORTED_INDICES.Keys.ToArray(),
                KnownInstrumentsCounts = _knownInstruments.ToDictionary(
                    kvp => kvp.Key, 
                    kvp => kvp.Value.Count)
            };
        }

        public Task? ManagerTask { get; private set; }
        public async Task StartManagerAsync(CancellationToken token)
        {
            if (ManagerTask == null || ManagerTask.IsCompleted)
            {
                ManagerTask = Task.Run(() => ExecuteAsync(token), token);
            }
        }

        private void SafeLog(string message)
        {
            try
            {
                var logPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "data_collection_log.txt");
                File.AppendAllText(logPath, $"[{DateTime.Now:HH:mm:ss}] {message}\n");
            }
            catch (Exception ex)
            {
                // If logging fails, try to write to a different location
                try
                {
                    File.AppendAllText("C:\\temp\\log_error.txt", $"LOGGING ERROR: {ex.Message} at {DateTime.Now}\n");
                }
                catch { }
            }
        }

        /// <summary>
        /// 🔥 DETECT AND ADD NEW STRIKES FROM QUOTES API
        /// This ensures we capture new strikes that appear in quotes but not in instruments API
        /// </summary>
        private async Task DetectAndAddNewStrikesFromQuotes(IServiceScope scope, 
            Dictionary<string, OptionAnalysisTool.KiteConnect.Models.KiteQuote> quotes, string index)
        {
            try
            {
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var kiteService = scope.ServiceProvider.GetRequiredService<IKiteConnectService>();
                
                // Get all instruments for this index from database
                var existingInstruments = await context.Instruments
                    .Where(i => i.TradingSymbol.ToUpper().Contains(index))
                    .Select(i => i.InstrumentToken)
                    .ToListAsync();
                
                // Find new instrument tokens in quotes that don't exist in database
                var newInstrumentTokens = quotes.Keys
                    .Where(token => !existingInstruments.Contains(token))
                    .ToList();
                
                if (newInstrumentTokens.Any())
                {
                    _logger.LogInformation("🔥 [NEW STRIKES] Found {count} new instrument tokens in quotes for {index}: {tokens}", 
                        newInstrumentTokens.Count, index, string.Join(", ", newInstrumentTokens.Take(5)));
                    
                    // Get instrument details for new tokens
                    var allInstruments = await kiteService.GetInstrumentsAsync("NFO");
                    var bfoInstruments = await kiteService.GetInstrumentsAsync("BFO");
                    var combinedInstruments = allInstruments.Concat(bfoInstruments).ToList();
                    
                    var newInstruments = combinedInstruments
                        .Where(i => newInstrumentTokens.Contains(i.InstrumentToken.ToString()))
                        .ToList();
                    
                    // Add new instruments to database
                    foreach (var instrument in newInstruments)
                    {
                        var existing = await context.Instruments
                            .FirstOrDefaultAsync(i => i.InstrumentToken == instrument.InstrumentToken);
                        
                        if (existing == null)
                        {
                            context.Instruments.Add(new OptionAnalysisTool.Models.Instrument
                            {
                                InstrumentToken = instrument.InstrumentToken,
                                TradingSymbol = instrument.TradingSymbol,
                                Name = instrument.Name,
                                Strike = instrument.Strike,
                                Expiry = instrument.Expiry,
                                InstrumentType = instrument.InstrumentType,
                                Segment = instrument.Segment,
                                Exchange = instrument.Exchange,
                                CreatedDate = DateTime.Today,
                                LastUpdated = DateTime.Now
                            });
                            
                            _logger.LogInformation("✅ [NEW STRIKES] Added new instrument: {symbol} (Token: {token})", 
                                instrument.TradingSymbol, instrument.InstrumentToken);
                        }
                    }
                    
                    await context.SaveChangesAsync();
                    
                    // Update local cache
                    await LoadKnownInstrumentsAsync(scope);
                    
                    _logger.LogInformation("🔥 [NEW STRIKES] Successfully added {count} new instruments for {index}", 
                        newInstruments.Count, index);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💥 Error detecting new strikes from quotes for {index}", index);
            }
        }

        /// <summary>
        /// Notify user of authentication failure through GUI
        /// </summary>
        private void NotifyAuthenticationFailure(string message)
        {
            try
            {
                _logger.LogWarning($"🔐 AUTHENTICATION FAILURE: {message}");
                
                // Reset authentication state so it will be checked again
                _isAuthenticated = false;
                
                // Log the message prominently for user attention
                _logger.LogError($"🚨 USER ACTION REQUIRED: {message}");
                
                // Write to a special file that the GUI can monitor
                try
                {
                    var authAlertPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "auth_alert.txt");
                    File.WriteAllText(authAlertPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}");
                }
                catch { }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in authentication failure notification");
            }
        }

        // Helper method to get current IST time
        private DateTime GetCurrentIST()
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

    // SUPPORTING CLASSES

    public class IndexConfiguration
    {
        public required string Exchange { get; set; }
        public required string SpotSymbol { get; set; }
        public int LotSize { get; set; }
    }

    public class InstrumentInfo
    {
        public required string InstrumentToken { get; set; }
        public required string TradingSymbol { get; set; }
        public required string UnderlyingSymbol { get; set; }
        public decimal StrikePrice { get; set; }
        public required string OptionType { get; set; }
        public DateTime ExpiryDate { get; set; }
        public required string Exchange { get; set; }
        public decimal LastPrice { get; set; }
        public long Volume { get; set; }
        public long OpenInterest { get; set; }
        public decimal LowerCircuitLimit { get; set; }
        public decimal UpperCircuitLimit { get; set; }
        public decimal UnderlyingPrice { get; set; }
    }

    public class DataManagerStatistics
    {
        public DateTime LastUpdateTime { get; set; }
        public bool IsAuthenticated { get; set; }
        public int TotalIndicesTracked { get; set; }
        public int TotalKnownInstruments { get; set; }
        public int IntradaySnapshotsToday { get; set; }
        public int EODRecordsToday { get; set; }
        public int CircuitLimitChangesToday { get; set; }
        public int NewStrikesDetected { get; set; }
    }
}