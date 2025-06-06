using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OptionAnalysisTool.Common.Data;
using OptionAnalysisTool.KiteConnect.Services;
using OptionAnalysisTool.Models;
using Microsoft.EntityFrameworkCore;

namespace OptionAnalysisTool.Common.Services
{
    /// <summary>
    /// 🔥 COMPREHENSIVE AUTOMATIC DATA STORAGE SERVICE
    /// Automatically stores BOTH intraday and EOD data for ALL active expiries
    /// Circuit limit tracking for ALL index option strikes during market hours
    /// </summary>
    public class ComprehensiveDataStorageService : BackgroundService
    {
        private readonly ILogger<ComprehensiveDataStorageService> _logger;
        private readonly ApplicationDbContext _context;
        private readonly IKiteConnectService _kiteConnectService;
        private readonly CircuitLimitTrackingService _circuitLimitTrackingService;
        private readonly MarketHoursService _marketHoursService;
        private readonly EODDataStorageService _eodDataStorageService;

        // Configuration
        private const int INTRADAY_INTERVAL_SECONDS = 30; // Every 30 seconds during market hours
        private const int EOD_CHECK_INTERVAL_MINUTES = 15; // Check EOD every 15 minutes after market close
        
        // All supported index symbols with their exchanges
        private readonly Dictionary<string, string> SUPPORTED_INDICES = new()
        {
            { "NIFTY", "NFO" },
            { "BANKNIFTY", "NFO" },
            { "FINNIFTY", "NFO" },
            { "MIDCPNIFTY", "NFO" },
            { "SENSEX", "BFO" },
            { "BANKEX", "BFO" }
        };

        // Active expiries cache
        private readonly Dictionary<string, List<DateTime>> _activeExpiriesCache = new();
        private DateTime _lastExpiryCacheUpdate = DateTime.MinValue;
        private const int EXPIRY_CACHE_REFRESH_HOURS = 24;

        // Active instruments cache with ALL expiries
        private readonly Dictionary<string, List<InstrumentInfo>> _allActiveInstruments = new();
        private DateTime _lastInstrumentCacheUpdate = DateTime.MinValue;
        private const int INSTRUMENT_CACHE_REFRESH_MINUTES = 15;

        public ComprehensiveDataStorageService(
            ILogger<ComprehensiveDataStorageService> logger,
            ApplicationDbContext context,
            IKiteConnectService kiteConnectService,
            CircuitLimitTrackingService circuitLimitTrackingService,
            MarketHoursService marketHoursService,
            EODDataStorageService eodDataStorageService)
        {
            _logger = logger;
            _context = context;
            _kiteConnectService = kiteConnectService;
            _circuitLimitTrackingService = circuitLimitTrackingService;
            _marketHoursService = marketHoursService;
            _eodDataStorageService = eodDataStorageService;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("🔥 Comprehensive Data Storage Service started - ALL expiries, ALL indices");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    if (_marketHoursService.IsMarketOpen())
                    {
                        // INTRADAY DATA COLLECTION + CIRCUIT LIMIT TRACKING
                        await RunIntradayDataCollection(stoppingToken);
                    }
                    else if (_marketHoursService.IsEndOfDay())
                    {
                        // EOD DATA COLLECTION
                        await RunEODDataCollection(stoppingToken);
                    }
                    else
                    {
                        // WAIT FOR MARKET OR EOD
                        await WaitForNextDataCollection(stoppingToken);
                    }
                }
                catch (OperationCanceledException)
                {
                    _logger.LogInformation("Comprehensive data storage service stopped");
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "💥 Error in comprehensive data storage service");
                    await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
                }
            }

            _logger.LogInformation("🔥 Comprehensive Data Storage Service stopped");
        }

        /// <summary>
        /// 🔥 INTRADAY DATA COLLECTION - ALL ACTIVE EXPIRIES
        /// Runs during market hours, collects data + tracks circuit limits
        /// </summary>
        private async Task RunIntradayDataCollection(CancellationToken stoppingToken)
        {
            _logger.LogInformation("🟢 Starting intraday data collection for ALL active expiries");

            while (_marketHoursService.IsMarketOpen() && !stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var startTime = DateTime.UtcNow;

                    // Refresh active instruments and expiries cache
                    await RefreshActiveInstrumentsAndExpiries();

                    // Collect data for ALL active instruments across ALL expiries
                    var collectionResults = await CollectIntradayDataForAllExpiries();

                    // Track circuit limit changes
                    var circuitChanges = await TrackCircuitLimitChanges(collectionResults);

                    var duration = DateTime.UtcNow - startTime;
                    _logger.LogInformation("✅ Intraday collection cycle: {processed} instruments, {changes} circuit changes, {duration:ss\\.ff}s",
                        collectionResults.TotalProcessed, circuitChanges, duration);

                    // Wait for next cycle
                    await Task.Delay(TimeSpan.FromSeconds(INTRADAY_INTERVAL_SECONDS), stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in intraday data collection cycle");
                    await Task.Delay(TimeSpan.FromSeconds(INTRADAY_INTERVAL_SECONDS), stoppingToken);
                }
            }
        }

        /// <summary>
        /// 🔥 EOD DATA COLLECTION - ALL ACTIVE EXPIRIES
        /// Runs after market close, consolidates daily data
        /// </summary>
        private async Task RunEODDataCollection(CancellationToken stoppingToken)
        {
            _logger.LogInformation("🟡 Starting EOD data collection for ALL active expiries");

            try
            {
                var tradingDate = DateTime.Today;
                var startTime = DateTime.UtcNow;

                // Process EOD for each supported index
                int totalEODProcessed = 0;
                foreach (var index in SUPPORTED_INDICES.Keys)
                {
                    try
                    {
                        _logger.LogInformation("📊 Processing EOD data for {index}", index);
                        
                        // Get all active expiries for this index
                        var activeExpiries = await GetActiveExpiries(index);
                        
                        foreach (var expiry in activeExpiries)
                        {
                            var processed = await _eodDataStorageService.StoreHistoricalDataForDate(
                                index, tradingDate, expiry);
                            totalEODProcessed += processed;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error processing EOD data for {index}", index);
                    }
                }

                // Apply circuit limits from intraday data to EOD records
                await _eodDataStorageService.ApplyCircuitLimitsFromIntradayData(tradingDate);

                var duration = DateTime.UtcNow - startTime;
                _logger.LogInformation("✅ EOD collection complete: {processed} records, {duration:mm\\:ss}",
                    totalEODProcessed, duration);

                // Wait before next EOD check
                await Task.Delay(TimeSpan.FromMinutes(EOD_CHECK_INTERVAL_MINUTES), stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in EOD data collection");
                await Task.Delay(TimeSpan.FromMinutes(EOD_CHECK_INTERVAL_MINUTES), stoppingToken);
            }
        }

        /// <summary>
        /// Wait for next data collection opportunity
        /// </summary>
        private async Task WaitForNextDataCollection(CancellationToken stoppingToken)
        {
            var timeToMarketOpen = _marketHoursService.GetTimeToMarketOpen();
            
            if (timeToMarketOpen.TotalMinutes <= 15)
            {
                _logger.LogInformation("⏰ Market opens in {minutes:F1} minutes - preparing data collection", 
                    timeToMarketOpen.TotalMinutes);
            }

            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }

        /// <summary>
        /// 🔥 REFRESH ALL ACTIVE INSTRUMENTS AND EXPIRIES
        /// Gets ALL active expiries for ALL supported indices
        /// </summary>
        private async Task RefreshActiveInstrumentsAndExpiries()
        {
            if (DateTime.UtcNow - _lastInstrumentCacheUpdate < TimeSpan.FromMinutes(INSTRUMENT_CACHE_REFRESH_MINUTES))
                return;

            try
            {
                _logger.LogInformation("🔄 Refreshing ALL active instruments and expiries cache");

                _allActiveInstruments.Clear();
                _activeExpiriesCache.Clear();

                foreach (var indexInfo in SUPPORTED_INDICES)
                {
                    var index = indexInfo.Key;
                    var exchange = indexInfo.Value;

                    try
                    {
                        // Get all instruments for this index
                        var allInstruments = await _kiteConnectService.GetInstrumentsAsync(exchange);
                        var indexInstruments = allInstruments
                            .Where(i => i.TradingSymbol.ToUpper().Contains(index) &&
                                       (i.InstrumentType == "CE" || i.InstrumentType == "PE"))
                            .Select(i => new InstrumentInfo
                            {
                                InstrumentToken = i.InstrumentToken.ToString(),
                                TradingSymbol = i.TradingSymbol,
                                UnderlyingSymbol = index,
                                StrikePrice = i.Strike,
                                OptionType = i.InstrumentType ?? "CE",
                                ExpiryDate = i.Expiry ?? DateTime.Today.AddDays(30),
                                Exchange = exchange
                            })
                            .ToList();

                        _allActiveInstruments[index] = indexInstruments;

                        // Extract unique expiry dates
                        var expiries = indexInstruments
                            .Select(i => i.ExpiryDate.Date)
                            .Distinct()
                            .Where(d => d >= DateTime.Today) // Only future expiries
                            .OrderBy(d => d)
                            .ToList();

                        _activeExpiriesCache[index] = expiries;

                        _logger.LogInformation("✅ {index}: {instruments} instruments across {expiries} active expiries",
                            index, indexInstruments.Count, expiries.Count);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error refreshing instruments for {index}", index);
                    }
                }

                _lastInstrumentCacheUpdate = DateTime.UtcNow;

                var totalInstruments = _allActiveInstruments.Values.Sum(list => list.Count);
                var totalExpiries = _activeExpiriesCache.Values.Sum(list => list.Count);
                
                _logger.LogInformation("🎯 Cache refresh complete: {total} instruments across {expiries} total expiries for {indices} indices",
                    totalInstruments, totalExpiries, SUPPORTED_INDICES.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error refreshing active instruments and expiries cache");
            }
        }

        /// <summary>
        /// 🔥 COLLECT INTRADAY DATA FOR ALL EXPIRIES
        /// Gets real-time data for ALL active instruments across ALL expiries
        /// </summary>
        private async Task<DataCollectionResults> CollectIntradayDataForAllExpiries()
        {
            var results = new DataCollectionResults();

            try
            {
                foreach (var indexInfo in _allActiveInstruments)
                {
                    var index = indexInfo.Key;
                    var instruments = indexInfo.Value;

                    // Process in batches to avoid overwhelming the API
                    const int batchSize = 100;
                    var batches = instruments.Chunk(batchSize);

                    foreach (var batch in batches)
                    {
                        try
                        {
                            var instrumentTokens = batch.Select(i => i.InstrumentToken).ToArray();
                            var quotes = await _kiteConnectService.GetQuotesAsync(instrumentTokens);

                            foreach (var instrument in batch)
                            {
                                if (quotes.TryGetValue(instrument.InstrumentToken, out var quote))
                                {
                                    // Only process if actively trading
                                    if (IsActivelyTrading(quote))
                                    {
                                        // Store intraday snapshot
                                        await StoreIntradaySnapshot(instrument, quote);
                                        results.TotalProcessed++;
                                        results.ActiveInstruments.Add(instrument);
                                    }
                                    else
                                    {
                                        results.TotalSkipped++;
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error processing batch for {index}", index);
                            results.TotalErrors++;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in collect intraday data for all expiries");
                results.TotalErrors++;
            }

            return results;
        }

        /// <summary>
        /// Track circuit limit changes for collected instruments
        /// </summary>
        private async Task<int> TrackCircuitLimitChanges(DataCollectionResults results)
        {
            int changesDetected = 0;

            foreach (var instrument in results.ActiveInstruments)
            {
                try
                {
                    var change = await _circuitLimitTrackingService.TrackCircuitLimitChange(
                        instrument.InstrumentToken,
                        instrument.TradingSymbol,
                        instrument.UnderlyingSymbol,
                        instrument.StrikePrice,
                        instrument.OptionType,
                        instrument.ExpiryDate,
                        instrument.LowerCircuitLimit,
                        instrument.UpperCircuitLimit,
                        instrument.LastPrice,
                        instrument.UnderlyingPrice,
                        instrument.Volume,
                        instrument.OpenInterest);

                    if (change != null)
                    {
                        changesDetected++;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error tracking circuit limits for {symbol}", instrument.TradingSymbol);
                }
            }

            return changesDetected;
        }

        /// <summary>
        /// Store intraday snapshot
        /// </summary>
        private async Task StoreIntradaySnapshot(InstrumentInfo instrument, OptionAnalysisTool.KiteConnect.Models.KiteQuote quote)
        {
            var snapshot = new IntradayOptionSnapshot
            {
                InstrumentToken = instrument.InstrumentToken,
                Symbol = instrument.TradingSymbol,
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
                Timestamp = DateTime.UtcNow,
                CircuitLimitStatus = DetermineCircuitStatus(quote),
                ValidationMessage = "Auto-collected",
                TradingStatus = "Normal",
                IsValidData = true
            };

            _context.IntradayOptionSnapshots.Add(snapshot);
            await _context.SaveChangesAsync();

            // Update instrument info with current data
            instrument.LastPrice = quote.LastPrice;
            instrument.Volume = quote.Volume;
            instrument.OpenInterest = quote.OpenInterest;
            instrument.LowerCircuitLimit = quote.LowerCircuitLimit;
            instrument.UpperCircuitLimit = quote.UpperCircuitLimit;
            instrument.UnderlyingPrice = 0; // TODO: Get actual underlying price
        }

        /// <summary>
        /// Get active expiries for an index
        /// </summary>
        private async Task<List<DateTime>> GetActiveExpiries(string index)
        {
            if (_activeExpiriesCache.TryGetValue(index, out var expiries))
            {
                return expiries;
            }

            // Fallback: refresh cache
            await RefreshActiveInstrumentsAndExpiries();
            return _activeExpiriesCache.GetValueOrDefault(index, new List<DateTime>());
        }

        /// <summary>
        /// Check if instrument is actively trading
        /// </summary>
        private bool IsActivelyTrading(OptionAnalysisTool.KiteConnect.Models.KiteQuote quote)
        {
            return quote.LastPrice > 0 && 
                   quote.LowerCircuitLimit > 0 && 
                   quote.UpperCircuitLimit > 0 &&
                   (quote.Volume > 0 || quote.OpenInterest > 0);
        }

        /// <summary>
        /// Determine circuit status
        /// </summary>
        private string DetermineCircuitStatus(OptionAnalysisTool.KiteConnect.Models.KiteQuote quote)
        {
            if (quote.LastPrice <= quote.LowerCircuitLimit) return "Lower Circuit";
            if (quote.LastPrice >= quote.UpperCircuitLimit) return "Upper Circuit";
            if (quote.LastPrice <= quote.LowerCircuitLimit * 1.02m) return "Near Lower Circuit";
            if (quote.LastPrice >= quote.UpperCircuitLimit * 0.98m) return "Near Upper Circuit";
            return "Normal";
        }

        public override async Task StopAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("🛑 Stopping Comprehensive Data Storage Service");
            await base.StopAsync(stoppingToken);
        }
    }

    /// <summary>
    /// Results from data collection cycle
    /// </summary>
    public class DataCollectionResults
    {
        public int TotalProcessed { get; set; }
        public int TotalSkipped { get; set; }
        public int TotalErrors { get; set; }
        public List<InstrumentInfo> ActiveInstruments { get; set; } = new();
    }

    /// <summary>
    /// Instrument information with current data
    /// </summary>
    public class InstrumentInfo
    {
        public string InstrumentToken { get; set; } = string.Empty;
        public string TradingSymbol { get; set; } = string.Empty;
        public string UnderlyingSymbol { get; set; } = string.Empty;
        public decimal StrikePrice { get; set; }
        public string OptionType { get; set; } = string.Empty;
        public DateTime ExpiryDate { get; set; }
        public string Exchange { get; set; } = string.Empty;
        
        // Current market data
        public decimal LastPrice { get; set; }
        public long Volume { get; set; }
        public long OpenInterest { get; set; }
        public decimal LowerCircuitLimit { get; set; }
        public decimal UpperCircuitLimit { get; set; }
        public decimal UnderlyingPrice { get; set; }
    }
} 