using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using OptionAnalysisTool.Common.Data;
using OptionAnalysisTool.Models;
using OptionAnalysisTool.KiteConnect.Services;

namespace OptionAnalysisTool.Common.Services
{
    /// <summary>
    /// 🌅 END-OF-DAY CIRCUIT LIMIT PROCESSOR
    /// Merges intraday circuit limit data with Kite EOD data
    /// Addresses the issue: Kite EOD data may not include circuit limits
    /// Solution: Apply circuit limits from intraday snapshots to EOD historical data
    /// </summary>
    public class EODCircuitLimitProcessor
    {
        private readonly ILogger<EODCircuitLimitProcessor> _logger;
        private readonly ApplicationDbContext _context;
        private readonly IKiteConnectService _kiteConnectService;
        private readonly MarketHoursService _marketHoursService;

        // Processing configuration
        private readonly string[] SUPPORTED_INDICES = {
            "NIFTY", "BANKNIFTY", "FINNIFTY", "MIDCPNIFTY", "SENSEX", "BANKEX"
        };

        public EODCircuitLimitProcessor(
            ILogger<EODCircuitLimitProcessor> logger,
            ApplicationDbContext context,
            IKiteConnectService kiteConnectService,
            MarketHoursService marketHoursService)
        {
            _logger = logger;
            _context = context;
            _kiteConnectService = kiteConnectService;
            _marketHoursService = marketHoursService;
        }

        /// <summary>
        /// Process EOD data for a specific trading date
        /// </summary>
        public async Task ProcessEODDataAsync(DateTime tradingDate)
        {
            _logger.LogInformation("🌅 === EOD PROCESSING STARTED ===");
            _logger.LogInformation("📅 Processing date: {date}", tradingDate.ToString("yyyy-MM-dd"));

            try
            {
                // Validate that it's a completed trading day
                if (!await ValidateTradingDay(tradingDate))
                {
                    _logger.LogWarning("⚠️ Invalid trading day or processing too early: {date}", tradingDate);
                    return;
                }

                // Process each supported index
                var totalProcessed = 0;
                var totalCircuitLimitsMerged = 0;

                foreach (var index in SUPPORTED_INDICES)
                {
                    _logger.LogInformation("📊 Processing {index} options...", index);
                    
                    var (processed, circuitsMerged) = await ProcessIndexEODData(index, tradingDate);
                    totalProcessed += processed;
                    totalCircuitLimitsMerged += circuitsMerged;
                    
                    _logger.LogInformation("✅ {index}: {processed} contracts processed, {circuits} circuit limits merged", 
                        index, processed, circuitsMerged);
                }

                // Generate summary statistics
                await GenerateEODSummary(tradingDate, totalProcessed, totalCircuitLimitsMerged);

                _logger.LogInformation("🌅 === EOD PROCESSING COMPLETED ===");
                _logger.LogInformation("📊 Total: {processed} contracts, {circuits} circuit limits merged", 
                    totalProcessed, totalCircuitLimitsMerged);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💥 Error in EOD processing for {date}", tradingDate);
                throw;
            }
        }

        /// <summary>
        /// Process EOD data for a specific index
        /// </summary>
        private async Task<(int processed, int circuitsMerged)> ProcessIndexEODData(string index, DateTime tradingDate)
        {
            try
            {
                // Get all intraday snapshots for this index and date
                var intradaySnapshots = await _context.IntradayOptionSnapshots
                    .Where(s => s.UnderlyingSymbol == index && 
                               s.Timestamp.Date == tradingDate.Date)
                    .OrderBy(s => s.Symbol)
                    .ThenBy(s => s.StrikePrice)
                    .ThenBy(s => s.OptionType)
                    .ToListAsync();

                if (!intradaySnapshots.Any())
                {
                    _logger.LogWarning("⚠️ No intraday data found for {index} on {date}", index, tradingDate);
                    return (0, 0);
                }

                // Group by contract (Symbol + Strike + Option Type)
                var contractGroups = intradaySnapshots
                    .GroupBy(s => new { s.Symbol, s.StrikePrice, s.OptionType, s.ExpiryDate })
                    .ToList();

                _logger.LogInformation("📊 Found {groups} unique contracts for {index}", contractGroups.Count, index);

                var processed = 0;
                var circuitsMerged = 0;

                foreach (var contractGroup in contractGroups)
                {
                    try
                    {
                        var result = await ProcessContractEODData(contractGroup, tradingDate);
                        if (result.success)
                        {
                            processed++;
                            if (result.circuitLimitsMerged)
                                circuitsMerged++;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "⚠️ Error processing contract {symbol}", contractGroup.Key.Symbol);
                    }
                }

                return (processed, circuitsMerged);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💥 Error processing {index} EOD data", index);
                return (0, 0);
            }
        }

        /// <summary>
        /// Process EOD data for a specific contract
        /// </summary>
        private async Task<(bool success, bool circuitLimitsMerged)> ProcessContractEODData(
            IGrouping<object, IntradayOptionSnapshot> contractGroup, DateTime tradingDate)
        {
            var contract = contractGroup.Key;
            var snapshots = contractGroup.OrderBy(s => s.Timestamp).ToList();
            
            if (!snapshots.Any())
                return (false, false);

            var firstSnapshot = snapshots.First();
            var lastSnapshot = snapshots.Last();

            try
            {
                // Check if historical data already exists
                var existingHistorical = await _context.HistoricalOptionData
                    .FirstOrDefaultAsync(h => h.Symbol == firstSnapshot.Symbol && 
                                            h.TradingDate.Date == tradingDate.Date);

                if (existingHistorical != null)
                {
                    // Update existing record with circuit limit data
                    return await UpdateExistingHistoricalData(existingHistorical, snapshots);
                }
                else
                {
                    // Create new historical record
                    return await CreateNewHistoricalData(contract, snapshots, tradingDate);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💥 Error processing contract {symbol}", firstSnapshot.Symbol);
                return (false, false);
            }
        }

        /// <summary>
        /// Update existing historical data with circuit limits from intraday data
        /// </summary>
        private async Task<(bool success, bool circuitLimitsMerged)> UpdateExistingHistoricalData(
            HistoricalOptionData existingData, List<IntradayOptionSnapshot> snapshots)
        {
            try
            {
                var lastSnapshot = snapshots.Last();
                var hasCircuitChange = snapshots.Any(s => s.CircuitLimitStatus != "Normal");

                // Update circuit limit information
                existingData.LowerCircuitLimit = lastSnapshot.LowerCircuitLimit;
                existingData.UpperCircuitLimit = lastSnapshot.UpperCircuitLimit;
                existingData.CircuitLimitChanged = hasCircuitChange;
                existingData.LastUpdated = DateTime.UtcNow;

                // Mark as updated with circuit limits
                existingData.ValidationMessage = hasCircuitChange 
                    ? "EOD data updated with intraday circuit limits - Circuit breaches detected"
                    : "EOD data updated with intraday circuit limits - No breaches";

                await _context.SaveChangesAsync();

                _logger.LogDebug("✅ Updated {symbol} with circuit limits: LCL={lcl}, UCL={ucl}", 
                    existingData.Symbol, existingData.LowerCircuitLimit, existingData.UpperCircuitLimit);

                return (true, true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💥 Error updating historical data for {symbol}", existingData.Symbol);
                return (false, false);
            }
        }

        /// <summary>
        /// Create new historical data from intraday snapshots
        /// </summary>
        private async Task<(bool success, bool circuitLimitsMerged)> CreateNewHistoricalData(
            object contract, List<IntradayOptionSnapshot> snapshots, DateTime tradingDate)
        {
            try
            {
                var firstSnapshot = snapshots.First();
                var lastSnapshot = snapshots.Last();
                var hasCircuitChange = snapshots.Any(s => s.CircuitLimitStatus != "Normal");

                // Calculate OHLC from intraday data
                var open = snapshots.First().LastPrice;
                var close = snapshots.Last().LastPrice;
                var high = snapshots.Max(s => s.High > 0 ? s.High : s.LastPrice);
                var low = snapshots.Min(s => s.Low > 0 ? s.Low : s.LastPrice);
                var volume = snapshots.Max(s => s.Volume); // Use max volume seen during day
                var openInterest = lastSnapshot.OpenInterest;

                var historicalData = new HistoricalOptionData
                {
                    InstrumentToken = firstSnapshot.InstrumentToken,
                    Symbol = firstSnapshot.Symbol,
                    UnderlyingSymbol = firstSnapshot.UnderlyingSymbol,
                    StrikePrice = firstSnapshot.StrikePrice,
                    OptionType = firstSnapshot.OptionType,
                    ExpiryDate = firstSnapshot.ExpiryDate,
                    TradingDate = tradingDate,

                    // OHLC Data derived from intraday
                    Open = open,
                    High = high,
                    Low = low,
                    Close = close,
                    Change = close - open,
                    PercentageChange = open != 0 ? ((close - open) / open) * 100 : 0,

                    // Volume & Interest
                    Volume = volume,
                    OpenInterest = openInterest,
                    OIChange = 0, // Would need previous day data to calculate

                    // Circuit Limits from intraday data
                    LowerCircuitLimit = lastSnapshot.LowerCircuitLimit,
                    UpperCircuitLimit = lastSnapshot.UpperCircuitLimit,
                    CircuitLimitChanged = hasCircuitChange,

                    // Greeks (if available)
                    ImpliedVolatility = lastSnapshot.ImpliedVolatility,

                    // Timestamps
                    CapturedAt = DateTime.UtcNow,
                    LastUpdated = DateTime.UtcNow,

                    // Quality Control
                    IsValidData = true,
                    ValidationMessage = hasCircuitChange 
                        ? "Created from intraday data - Circuit breaches detected"
                        : "Created from intraday data - No circuit breaches"
                };

                _context.HistoricalOptionData.Add(historicalData);
                await _context.SaveChangesAsync();

                _logger.LogDebug("✅ Created historical data for {symbol}: OHLC={open}/{high}/{low}/{close}, Circuits={lcl}/{ucl}", 
                    firstSnapshot.Symbol, open, high, low, close, 
                    historicalData.LowerCircuitLimit, historicalData.UpperCircuitLimit);

                return (true, true);
            }
            catch (Exception ex)
            {
                var firstSnapshot = snapshots.FirstOrDefault();
                var symbol = firstSnapshot?.Symbol ?? "Unknown";
                _logger.LogError(ex, "💥 Error creating historical data for {symbol}", symbol);
                return (false, false);
            }
        }

        /// <summary>
        /// Validate that it's a proper trading day for processing
        /// </summary>
        private async Task<bool> ValidateTradingDay(DateTime tradingDate)
        {
            try
            {
                // Check if it's a trading day
                if (!_marketHoursService.IsTradingDay(tradingDate))
                {
                    return false;
                }

                // Check if market has closed for this day (don't process same day until after close)
                if (tradingDate.Date == DateTime.Today.Date && _marketHoursService.IsMarketOpen())
                {
                    return false;
                }

                // Check if we have intraday data for this date
                var hasIntradayData = await _context.IntradayOptionSnapshots
                    .AnyAsync(s => s.Timestamp.Date == tradingDate.Date);

                return hasIntradayData;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating trading day {date}", tradingDate);
                return false;
            }
        }

        /// <summary>
        /// Generate EOD processing summary
        /// </summary>
        private async Task GenerateEODSummary(DateTime tradingDate, int totalProcessed, int circuitsMerged)
        {
            try
            {
                _logger.LogInformation("📊 === EOD PROCESSING SUMMARY ===");
                _logger.LogInformation("📅 Date: {date}", tradingDate.ToString("yyyy-MM-dd"));
                _logger.LogInformation("📊 Contracts Processed: {processed}", totalProcessed);
                _logger.LogInformation("🔥 Circuit Limits Merged: {circuits}", circuitsMerged);

                // Get circuit limit statistics for the day
                var circuitLimitChanges = await _context.CircuitLimitTrackers
                    .Where(t => t.DetectedAt.Date == tradingDate.Date)
                    .ToListAsync();

                _logger.LogInformation("📈 Circuit Limit Changes by Type:");
                // Group by change type instead of severity level
                var changeTypeGroups = circuitLimitChanges
                    .GroupBy(t => t.ChangeReason)
                    .Select(g => new
                    {
                        ChangeType = g.Key,
                        Count = g.Count(),
                        LastChange = g.Max(t => t.DetectedAt)
                    })
                    .ToList();

                foreach (var stat in changeTypeGroups)
                {
                    _logger.LogInformation("   {changeType}: {count} (Last at {lastChange})", stat.ChangeType, stat.Count, stat.LastChange.ToString("yyyy-MM-dd HH:mm:ss"));
                }

                // Log indices covered
                var indicesCovered = await _context.HistoricalOptionData
                    .Where(h => h.TradingDate.Date == tradingDate.Date)
                    .Select(h => h.UnderlyingSymbol)
                    .Distinct()
                    .ToListAsync();

                _logger.LogInformation("📊 Indices Covered: {indices}", string.Join(", ", indicesCovered));
                _logger.LogInformation("📊 === EOD SUMMARY COMPLETED ===");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating EOD summary");
            }
        }

        /// <summary>
        /// Process EOD data for the last trading day (convenience method)
        /// </summary>
        public async Task ProcessLastTradingDayAsync()
        {
            var lastTradingDay = GetLastTradingDay();
            await ProcessEODDataAsync(lastTradingDay);
        }

        /// <summary>
        /// Get the last trading day
        /// </summary>
        private DateTime GetLastTradingDay()
        {
            var date = DateTime.Today.AddDays(-1);
            
            // Skip weekends
            while (date.DayOfWeek == DayOfWeek.Saturday || date.DayOfWeek == DayOfWeek.Sunday)
            {
                date = date.AddDays(-1);
            }
            
            // TODO: Add holiday calendar support
            return date;
        }

        public async Task ProcessTodaysDataAsync()
        {
            try
            {
                _logger.LogInformation("Starting EOD data processing for today");
                var today = DateTime.Today;
                
                var todaysSnapshots = await _context.IntradayOptionSnapshots
                    .Where(s => s.CaptureTime.Date == today)
                    .OrderBy(s => s.CaptureTime)
                    .ToListAsync();

                if (!todaysSnapshots.Any())
                {
                    _logger.LogWarning("No snapshots found for today");
                    return;
                }

                _logger.LogInformation($"Found {todaysSnapshots.Count} snapshots for processing");

                // Process the snapshots and store EOD data
                foreach (var snapshot in todaysSnapshots)
                {
                    // Add your EOD processing logic here
                    // For example, calculating daily statistics, storing aggregated data, etc.
                }

                _logger.LogInformation("Completed EOD data processing for today");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing EOD data");
                throw;
            }
        }
    }
} 