using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using OptionAnalysisTool.Common.Data;
using OptionAnalysisTool.Models;
using OptionAnalysisTool.KiteConnect.Services;
using OptionAnalysisTool.Common.Repositories;
using OptionAnalysisTool.Common.Models;
using KiteInstrument = OptionAnalysisTool.KiteConnect.Models.KiteInstrument;
using KiteQuote = OptionAnalysisTool.KiteConnect.Models.KiteQuote;
using DomainInstrument = OptionAnalysisTool.Models.Instrument;
using DomainQuote = OptionAnalysisTool.Models.Quote;
using Microsoft.Extensions.Hosting;
using System.Threading;
using Microsoft.Extensions.Configuration;

namespace OptionAnalysisTool.Common.Services
{
    public class IntradayDataService : BackgroundService
    {
        private readonly IKiteConnectService _kiteConnectService;
        private readonly ApplicationDbContext _dbContext;
        private readonly ILogger<IntradayDataService> _logger;
        private readonly IMarketDataRepository _marketDataRepository;
        private readonly IMarketHoursService _marketHoursService;
        private readonly IConfiguration _configuration;
        private const decimal CIRCUIT_BUFFER_PERCENTAGE = 0.01m;
        private const decimal MAX_CIRCUIT_RANGE_PERCENTAGE = 0.40m;

        public IntradayDataService(
            IKiteConnectService kiteConnectService,
            ApplicationDbContext dbContext,
            ILogger<IntradayDataService> logger,
            IMarketDataRepository marketDataRepository,
            IMarketHoursService marketHoursService,
            IConfiguration configuration)
        {
            _kiteConnectService = kiteConnectService;
            _dbContext = dbContext;
            _logger = logger;
            _marketDataRepository = marketDataRepository;
            _marketHoursService = marketHoursService;
            _configuration = configuration;
        }

        public async Task<List<IntradayOptionSnapshot>> CaptureOptionSnapshots(string underlyingSymbol)
        {
            try
            {
                _logger.LogInformation("Starting option snapshot capture for {symbol}", underlyingSymbol);
                
                var kiteInstruments = await _kiteConnectService.GetInstrumentsAsync("NFO");
                var optionInstruments = kiteInstruments
                    .Where(i => i.Name == underlyingSymbol &&
                               (i.InstrumentType == "CE" || i.InstrumentType == "PE") &&
                               i.Expiry >= DateTime.Today)
                    .OrderBy(i => i.Expiry)  // Order by expiry for better tracking
                    .Select(i => new Instrument
                    {
                        InstrumentToken = i.InstrumentToken,
                        TradingSymbol = i.TradingSymbol,
                        Name = i.Name,
                        Strike = i.Strike,
                        Expiry = i.Expiry,
                        InstrumentType = i.InstrumentType
                    })
                    .ToList();

                _logger.LogInformation("Found {count} active options across all expiries for {symbol}", 
                    optionInstruments.Count, underlyingSymbol);

                // Group by expiry for better logging and tracking
                var expiryGroups = optionInstruments.GroupBy(i => i.Expiry.Value.Date);
                foreach (var group in expiryGroups)
                {
                    _logger.LogInformation("Processing {count} options for expiry {expiry}", 
                        group.Count(), group.Key.ToString("yyyy-MM-dd"));
                }

                var snapshots = new List<IntradayOptionSnapshot>();
                foreach (var option in optionInstruments)
                {
                    var snapshot = await CaptureOptionSnapshot(option);
                    if (snapshot != null)
                    {
                        snapshots.Add(snapshot);
                    }
                }

                await SaveSnapshots(snapshots);
                LogCircuitLimitsSummary(snapshots);

                return snapshots;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error capturing option snapshots for {symbol}", underlyingSymbol);
                return new List<IntradayOptionSnapshot>();
            }
        }

        private bool ValidateCircuitLimits(IntradayOptionSnapshot snapshot)
        {
            // Basic validation
            if (snapshot.LowerCircuitLimit <= 0 || snapshot.UpperCircuitLimit <= 0)
                return false;

            // Circuit limits should have reasonable spread
            if (snapshot.UpperCircuitLimit <= snapshot.LowerCircuitLimit)
                return false;

            // Circuit limits should be within reasonable range of last price
            var maxCircuitRange = snapshot.LastPrice * MAX_CIRCUIT_RANGE_PERCENTAGE;
            if (Math.Abs(snapshot.LastPrice - snapshot.LowerCircuitLimit) > maxCircuitRange ||
                Math.Abs(snapshot.UpperCircuitLimit - snapshot.LastPrice) > maxCircuitRange)
                return false;

            return true;
        }

        private void LogCircuitLimitsSummary(List<IntradayOptionSnapshot> snapshots)
        {
            if (!snapshots.Any()) return;

            var groupedByExpiry = snapshots.GroupBy(s => s.ExpiryDate.Date);
            foreach (var expiryGroup in groupedByExpiry)
            {
                _logger.LogInformation("Circuit Limits Summary for Expiry {expiry}:", expiryGroup.Key.ToString("yyyy-MM-dd"));
                
                var ceOptions = expiryGroup.Where(s => s.OptionType == "CE").OrderBy(s => s.StrikePrice);
                var peOptions = expiryGroup.Where(s => s.OptionType == "PE").OrderBy(s => s.StrikePrice);

                foreach (var option in ceOptions)
                {
                    _logger.LogInformation("CE {strike}: Lower: {lower}, Upper: {upper}, Last: {last}", 
                        option.StrikePrice, option.LowerCircuitLimit, option.UpperCircuitLimit, option.LastPrice);
                }

                foreach (var option in peOptions)
                {
                    _logger.LogInformation("PE {strike}: Lower: {lower}, Upper: {upper}, Last: {last}", 
                        option.StrikePrice, option.LowerCircuitLimit, option.UpperCircuitLimit, option.LastPrice);
                }
            }
        }

        private async Task<IntradayOptionSnapshot?> CaptureOptionSnapshot(Instrument option)
        {
            try
            {
                var quotes = await _kiteConnectService.GetQuotesAsync(new[] { option.InstrumentToken });
                if (!quotes.Any()) return null;

                var kiteQuote = quotes.First().Value;
                if (kiteQuote == null) return null;

                var quote = new DomainQuote
                {
                    InstrumentToken = kiteQuote.InstrumentToken.ToString(),
                    LastPrice = kiteQuote.LastPrice,
                    Change = kiteQuote.Change,
                    Open = kiteQuote.Open,
                    High = kiteQuote.High,
                    Low = kiteQuote.Low,
                    Close = kiteQuote.Close,
                    Volume = kiteQuote.Volume,
                    OpenInterest = kiteQuote.OpenInterest,
                    LowerCircuitLimit = kiteQuote.LowerCircuitLimit,
                    UpperCircuitLimit = kiteQuote.UpperCircuitLimit,
                    ImpliedVolatility = kiteQuote.ImpliedVolatility
                };

                // TEMPORARY: COMMENT OUT ALL DUPLICATE PREVENTION LOGIC
                // Always capture snapshot for every minute
                bool shouldCaptureSnapshot = true;
                string changeReason = "Minute-by-Minute Collection";

                // COMMENTED OUT: Original duplicate prevention logic
                /*
                // Get the latest snapshot for this instrument
                var latestSnapshot = await _dbContext.IntradayOptionSnapshots
                    .Where(s => s.InstrumentToken == option.InstrumentToken)
                    .OrderByDescending(s => s.Timestamp)
                    .FirstOrDefaultAsync();

                var marketHours = _marketHoursService.IsMarketOpen();
                bool shouldCaptureSnapshot = false;
                string? changeReason = null;

                // TEMPORARY: Relaxed duplicate prevention for testing - capture data more frequently
                if (latestSnapshot == null)
                {
                    // First snapshot for this instrument
                    shouldCaptureSnapshot = true;
                    changeReason = "Initial Snapshot";
                }
                else
                {
                    // Check if enough time has passed since last snapshot (5 minutes for testing)
                    var timeSinceLastSnapshot = DateTime.Now - latestSnapshot.Timestamp;
                    if (timeSinceLastSnapshot.TotalMinutes >= 5)
                    {
                        shouldCaptureSnapshot = true;
                        changeReason = $"Regular Update - {timeSinceLastSnapshot.TotalMinutes:F1} minutes since last snapshot";
                    }
                    else if (marketHours)
                    {
                        // During market hours - capture if any significant trading data changes
                        shouldCaptureSnapshot = 
                            Math.Abs(latestSnapshot.LastPrice - quote.LastPrice) > 0.01m ||
                            Math.Abs(latestSnapshot.Volume - quote.Volume) > 100 ||
                            Math.Abs(latestSnapshot.OpenInterest - quote.OpenInterest) > 100 ||
                            latestSnapshot.LowerCircuitLimit != quote.LowerCircuitLimit ||
                            latestSnapshot.UpperCircuitLimit != quote.UpperCircuitLimit;
                        
                        if (shouldCaptureSnapshot)
                        {
                            changeReason = "Market Hours - Significant Data Change";
                        }
                    }
                    else
                    {
                        // After market hours - capture if circuit limits change
                        shouldCaptureSnapshot = 
                            latestSnapshot.LowerCircuitLimit != quote.LowerCircuitLimit ||
                            latestSnapshot.UpperCircuitLimit != quote.UpperCircuitLimit ||
                            Math.Abs(latestSnapshot.OpenInterest - quote.OpenInterest) > 1000;
                        
                        if (shouldCaptureSnapshot)
                        {
                            changeReason = "After Hours - Circuit Limit or OI Change";
                        }
                    }
                }

                if (!shouldCaptureSnapshot)
                {
                    _logger.LogDebug("⏸️ Skipping snapshot for {symbol} - no significant changes detected", option.TradingSymbol);
                    return null;
                }
                */

                var snapshot = new IntradayOptionSnapshot
                {
                    InstrumentToken = option.InstrumentToken,
                    TradingSymbol = option.TradingSymbol ?? string.Empty,
                    Symbol = option.TradingSymbol ?? string.Empty,
                    UnderlyingSymbol = option.Name ?? string.Empty,
                    StrikePrice = option.Strike,
                    OptionType = option.InstrumentType ?? string.Empty,
                    ExpiryDate = option.Expiry ?? DateTime.MinValue,
                    LastPrice = quote.LastPrice,
                    Change = quote.Change,
                    Open = quote.Open,
                    High = quote.High,
                    Low = quote.Low,
                    Close = quote.Close,
                    Volume = quote.Volume,
                    OpenInterest = quote.OpenInterest,
                    LowerCircuitLimit = quote.LowerCircuitLimit,
                    UpperCircuitLimit = quote.UpperCircuitLimit,
                    ImpliedVolatility = quote.ImpliedVolatility,
                    Timestamp = DateTime.Now,
                    OHLCDate = DateTime.Today,
                    LastUpdated = DateTime.UtcNow,
                    ChangeReason = changeReason,
                    CircuitLimitStatus = DetermineCircuitLimitStatus(quote),
                    ValidationMessage = string.Empty,
                    TradingStatus = "Normal",
                    IsValidData = true
                };

                _logger.LogInformation("📊 Creating snapshot for {symbol}: {reason}", option.TradingSymbol, changeReason);
                return snapshot;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error capturing option snapshot for {symbol}", option.TradingSymbol);
                return null;
            }
        }

        private string DetermineCircuitLimitStatus(DomainQuote quote)
        {
            if (quote.LastPrice <= quote.LowerCircuitLimit)
                return "Lower Circuit";
            if (quote.LastPrice >= quote.UpperCircuitLimit)
                return "Upper Circuit";
            return "Normal";
        }

        private async Task SaveSnapshots(List<IntradayOptionSnapshot> snapshots)
        {
            if (!snapshots.Any()) return;

            try
            {
                _logger.LogInformation("🔍 Processing {count} snapshots for database save", snapshots.Count);
                var savedCount = 0;

                // TEMPORARY: COMMENT OUT ALL DUPLICATE PREVENTION LOGIC
                // Save all snapshots without any duplicate checking
                foreach (var snapshot in snapshots)
                {
                    try
                    {
                        // Add new snapshot directly without any duplicate checking
                        await _dbContext.IntradayOptionSnapshots.AddAsync(snapshot);
                        savedCount++;
                        _logger.LogDebug("✅ Added snapshot for {symbol} at {time}", 
                            snapshot.Symbol, snapshot.Timestamp);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error processing snapshot for {symbol}", snapshot.Symbol);
                    }
                }

                // COMMENTED OUT: Original duplicate prevention logic
                /*
                var duplicateCount = 0;

                foreach (var snapshot in snapshots)
                {
                    try
                    {
                        // TEMPORARY: Simplified duplicate prevention - only check for exact duplicates in the same minute
                        var captureMinute = new DateTime(
                            snapshot.Timestamp.Year, 
                            snapshot.Timestamp.Month, 
                            snapshot.Timestamp.Day, 
                            snapshot.Timestamp.Hour, 
                            snapshot.Timestamp.Minute, 0);

                        var existingSnapshot = await _dbContext.IntradayOptionSnapshots
                            .Where(s => s.InstrumentToken == snapshot.InstrumentToken &&
                                       s.Timestamp >= captureMinute && 
                                       s.Timestamp < captureMinute.AddMinutes(1) &&
                                       s.LastPrice == snapshot.LastPrice &&
                                       s.LowerCircuitLimit == snapshot.LowerCircuitLimit &&
                                       s.UpperCircuitLimit == snapshot.UpperCircuitLimit)
                            .FirstOrDefaultAsync();

                        if (existingSnapshot != null)
                        {
                            duplicateCount++;
                            _logger.LogDebug("⚠️ Skipping exact duplicate for {symbol} at {time}", 
                                snapshot.Symbol, snapshot.Timestamp);
                            continue;
                        }

                        // Add new snapshot
                        await _dbContext.IntradayOptionSnapshots.AddAsync(snapshot);
                        savedCount++;
                        _logger.LogDebug("✅ Added new snapshot for {symbol} at {time}", 
                            snapshot.Symbol, snapshot.Timestamp);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error processing snapshot for {symbol}", snapshot.Symbol);
                    }
                }
                */

                await _dbContext.SaveChangesAsync();
                
                _logger.LogInformation("✅ Snapshot processing complete - Saved: {saved} snapshots", savedCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💥 Error saving snapshots");
                throw;
            }
        }

        /// <summary>
        /// Smart duplicate detection - checks multiple criteria to prevent duplicates
        /// </summary>
        private async Task<bool> IsSnapshotDuplicateAsync(IntradayOptionSnapshot snapshot)
        {
            // Define the time window for duplicate detection (1 minute)
            var captureMinute = new DateTime(
                snapshot.CaptureTime.Year, 
                snapshot.CaptureTime.Month, 
                snapshot.CaptureTime.Day, 
                snapshot.CaptureTime.Hour, 
                snapshot.CaptureTime.Minute, 0);

            // Check for exact duplicate (same symbol, strike, option type, and minute with identical data)
            var existingSnapshot = await _dbContext.IntradayOptionSnapshots
                .Where(s => s.Symbol == snapshot.Symbol && 
                           s.StrikePrice == snapshot.StrikePrice &&
                           s.OptionType == snapshot.OptionType &&
                           s.CaptureTime >= captureMinute && 
                           s.CaptureTime < captureMinute.AddMinutes(1) &&
                           s.LastPrice == snapshot.LastPrice &&
                           s.LowerCircuitLimit == snapshot.LowerCircuitLimit &&
                           s.UpperCircuitLimit == snapshot.UpperCircuitLimit)
                .FirstOrDefaultAsync();

            return existingSnapshot != null;
        }

        /// <summary>
        /// Get existing snapshot in the same minute (for potential updates)
        /// </summary>
        private async Task<IntradayOptionSnapshot> GetExistingSnapshotInSameMinuteAsync(IntradayOptionSnapshot snapshot)
        {
            var captureMinute = new DateTime(
                snapshot.CaptureTime.Year, 
                snapshot.CaptureTime.Month, 
                snapshot.CaptureTime.Day, 
                snapshot.CaptureTime.Hour, 
                snapshot.CaptureTime.Minute, 0);

            return await _dbContext.IntradayOptionSnapshots
                .Where(s => s.Symbol == snapshot.Symbol && 
                           s.StrikePrice == snapshot.StrikePrice &&
                           s.OptionType == snapshot.OptionType &&
                           s.CaptureTime >= captureMinute && 
                           s.CaptureTime < captureMinute.AddMinutes(1))
                .FirstOrDefaultAsync();
        }

        /// <summary>
        /// Check if snapshot data has changed (for update decisions)
        /// </summary>
        private bool HasSnapshotDataChanged(IntradayOptionSnapshot existing, IntradayOptionSnapshot newSnapshot)
        {
            return existing.LastPrice != newSnapshot.LastPrice ||
                   existing.LowerCircuitLimit != newSnapshot.LowerCircuitLimit ||
                   existing.UpperCircuitLimit != newSnapshot.UpperCircuitLimit ||
                   existing.Volume != newSnapshot.Volume ||
                   existing.OpenInterest != newSnapshot.OpenInterest ||
                   existing.High != newSnapshot.High ||
                   existing.Low != newSnapshot.Low;
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
            existing.LastUpdated = DateTime.UtcNow;
            existing.CaptureTime = newSnapshot.CaptureTime; // Update to latest capture time
        }

        public async Task<List<IntradayOptionSnapshot>> GetLatestSnapshots(string underlyingSymbol)
        {
            try
            {
                var snapshots = await _dbContext.IntradayOptionSnapshots
                    .Where(s => s.UnderlyingSymbol == underlyingSymbol)
                    .OrderByDescending(s => s.Timestamp)
                    .ToListAsync();

                return snapshots;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting latest snapshots for {symbol}", underlyingSymbol);
                return new List<IntradayOptionSnapshot>();
            }
        }

        public async Task<List<IntradayOptionSnapshot>> GetCircuitLimitBreaches(string underlyingSymbol)
        {
            try
            {
                var snapshots = await _dbContext.IntradayOptionSnapshots
                    .Where(s => s.UnderlyingSymbol == underlyingSymbol &&
                              (s.CircuitLimitStatus == "Upper Circuit" || s.CircuitLimitStatus == "Lower Circuit"))
                    .OrderByDescending(s => s.Timestamp)
                    .ToListAsync();

                return snapshots;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting circuit limit breaches for {symbol}", underlyingSymbol);
                return new List<IntradayOptionSnapshot>();
            }
        }

        public async Task<bool> SaveSnapshotAsync(IntradayOptionSnapshot snapshot)
        {
            try
            {
                _dbContext.IntradayOptionSnapshots.Add(snapshot);
                await _dbContext.SaveChangesAsync();
                await CheckCircuitLimitChangesAsync(snapshot);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving snapshot for {symbol}. Fields: TradingSymbol='{TradingSymbol}', OHLCDate='{OHLCDate}', ChangeReason='{ChangeReason}', LastUpdated='{LastUpdated}'", snapshot.Symbol, snapshot.TradingSymbol, snapshot.OHLCDate, snapshot.ChangeReason, snapshot.LastUpdated);
                return false;
            }
        }

        private bool ValidateSnapshot(IntradayOptionSnapshot snapshot)
        {
            if (string.IsNullOrEmpty(snapshot.Symbol))
                return false;

            if (snapshot.Timestamp == default)
                return false;

            if (snapshot.LastPrice <= 0)
                return false;

            if (snapshot.UpperCircuitLimit <= 0 || snapshot.LowerCircuitLimit <= 0)
                return false;

            if (snapshot.UpperCircuitLimit <= snapshot.LowerCircuitLimit)
                return false;

            if (snapshot.LastPrice < snapshot.LowerCircuitLimit || snapshot.LastPrice > snapshot.UpperCircuitLimit)
                return false;

            return true;
        }

        private async Task CheckCircuitLimitChangesAsync(IntradayOptionSnapshot snapshot)
        {
            try
            {
                var previousSnapshot = await _dbContext.IntradayOptionSnapshots
                    .Where(s => s.Symbol == snapshot.Symbol && s.Timestamp < snapshot.Timestamp)
                    .OrderByDescending(s => s.Timestamp)
                    .FirstOrDefaultAsync();

                if (previousSnapshot == null)
                    return;

                var changeType = DetermineCircuitLimitChangeType(
                    previousSnapshot.LowerCircuitLimit,
                    previousSnapshot.UpperCircuitLimit,
                    snapshot.LowerCircuitLimit,
                    snapshot.UpperCircuitLimit);

                if (changeType != "No Change")
                {
                    var change = new CircuitLimitChange
                    {
                        Symbol = snapshot.Symbol,
                        OldLowerCircuitLimit = previousSnapshot.LowerCircuitLimit,
                        NewLowerCircuitLimit = snapshot.LowerCircuitLimit,
                        OldUpperCircuitLimit = previousSnapshot.UpperCircuitLimit,
                        NewUpperCircuitLimit = snapshot.UpperCircuitLimit,
                        LastPrice = snapshot.LastPrice,
                        Timestamp = snapshot.Timestamp,
                        ChangeReason = changeType
                    };

                    await _dbContext.CircuitLimitChanges.AddAsync(change);
                    await _dbContext.SaveChangesAsync();

                    _logger.LogInformation("Circuit limit change detected for {symbol}: {type}", snapshot.Symbol, changeType);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking circuit limit changes for {symbol}", snapshot.Symbol);
            }
        }

        public async Task<List<IntradayOptionSnapshot>> GetSnapshotsAsync(string symbol, DateTime startTime, DateTime endTime)
        {
            try
            {
                return await _dbContext.IntradayOptionSnapshots
                    .Where(s => s.Symbol == symbol && s.Timestamp >= startTime && s.Timestamp <= endTime)
                    .OrderBy(s => s.Timestamp)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting snapshots for {symbol}", symbol);
                return new List<IntradayOptionSnapshot>();
            }
        }

        public async Task<List<CircuitLimitChange>> GetCircuitLimitChangesAsync(string symbol, DateTime startTime, DateTime endTime)
        {
            try
            {
                return await _dbContext.CircuitLimitChanges
                    .Where(c => c.Symbol == symbol && c.Timestamp >= startTime && c.Timestamp <= endTime)
                    .OrderBy(c => c.Timestamp)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting circuit limit changes for {symbol}", symbol);
                return new List<CircuitLimitChange>();
            }
        }

        private string DetermineCircuitLimitChangeType(
            decimal? prevLower, decimal? prevUpper,
            decimal? newLower, decimal? newUpper)
        {
            if (!prevLower.HasValue || !prevUpper.HasValue || !newLower.HasValue || !newUpper.HasValue)
                return "No Change";

            if (prevLower != newLower && prevUpper != newUpper)
                return "Both Limits Changed";
            if (prevLower != newLower)
                return "Lower Limit Changed";
            if (prevUpper != newUpper)
                return "Upper Limit Changed";

            return "No Change";
        }

        private decimal? CalculateCircuitLimitChangePercentage(
            decimal? prevLower, decimal? prevUpper,
            decimal? newLower, decimal? newUpper)
        {
            if (!prevLower.HasValue || !prevUpper.HasValue || !newLower.HasValue || !newUpper.HasValue)
                return null;

            var prevRange = prevUpper.Value - prevLower.Value;
            var newRange = newUpper.Value - newLower.Value;

            if (prevRange == 0)
                return null;

            return ((newRange - prevRange) / prevRange) * 100;
        }

        public async Task<List<CircuitLimitChange>> GetCircuitLimitChanges(
            string symbol,
            DateTime startTime,
            DateTime endTime,
            string? changeType = null)
        {
            try
            {
                var query = _dbContext.CircuitLimitChanges
                    .Where(c => c.Symbol == symbol && c.Timestamp >= startTime && c.Timestamp <= endTime);

                if (!string.IsNullOrEmpty(changeType))
                {
                    query = query.Where(c => c.ChangeReason == changeType);
                }

                return await query
                    .OrderBy(c => c.Timestamp)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting circuit limit changes for {symbol}", symbol);
                return new List<CircuitLimitChange>();
            }
        }

        public async Task<List<CircuitLimitChange>> GetCircuitLimitChangesByUnderlying(
            string underlyingSymbol,
            DateTime startTime,
            DateTime endTime)
        {
            try
            {
                return await _dbContext.CircuitLimitChanges
                    .Where(c => c.Symbol.StartsWith(underlyingSymbol) && c.Timestamp >= startTime && c.Timestamp <= endTime)
                    .OrderBy(c => c.Timestamp)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting circuit limit changes for underlying {symbol}", underlyingSymbol);
                return new List<CircuitLimitChange>();
            }
        }

        public async Task<bool> SaveSnapshotsAsync(IEnumerable<IntradayOptionSnapshot> snapshots)
        {
            try
            {
                foreach (var snapshot in snapshots)
                {
                    await _dbContext.IntradayOptionSnapshots.AddAsync(snapshot);
                }

                await _dbContext.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving snapshots");
                return false;
            }
        }

        public async Task<IntradayOptionSnapshot> GetLatestSnapshotAsync(string instrumentToken)
        {
            return await _dbContext.IntradayOptionSnapshots
                .Where(s => s.InstrumentToken == instrumentToken)
                .OrderByDescending(s => s.Timestamp)
                .FirstOrDefaultAsync();
        }

        public async Task<List<IntradayOptionSnapshot>> GetLatestSnapshotsAsync(string symbol)
        {
            return await _dbContext.IntradayOptionSnapshots
                .Where(s => s.Symbol == symbol)
                .OrderByDescending(s => s.Timestamp)
                .Take(100)
                .ToListAsync();
        }

        public async Task<List<IntradayOptionSnapshot>> GetAllSnapshotsAsync(string symbol)
        {
            return await _dbContext.IntradayOptionSnapshots
                .Where(s => s.Symbol == symbol)
                .OrderBy(s => s.Timestamp)
                .ToListAsync();
        }

        public async Task<bool> DeleteSnapshotsAsync(string symbol, DateTime olderThan)
        {
            try
            {
                var snapshots = await _dbContext.IntradayOptionSnapshots
                    .Where(s => s.Symbol == symbol && s.Timestamp < olderThan)
                    .ToListAsync();

                _dbContext.IntradayOptionSnapshots.RemoveRange(snapshots);
                await _dbContext.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting snapshots for {symbol}");
                return false;
            }
        }

        public async Task<bool> DeleteAllSnapshotsAsync(string symbol)
        {
            try
            {
                var snapshots = await _dbContext.IntradayOptionSnapshots
                    .Where(s => s.Symbol == symbol)
                    .ToListAsync();

                _dbContext.IntradayOptionSnapshots.RemoveRange(snapshots);
                await _dbContext.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting all snapshots for {symbol}");
                return false;
            }
        }

        public DomainQuote ConvertKiteQuote(KiteQuote kiteQuote)
        {
            if (kiteQuote == null) return new DomainQuote();

            return new DomainQuote
            {
                InstrumentToken = kiteQuote.InstrumentToken,
                LastPrice = kiteQuote.LastPrice,
                Change = kiteQuote.Change,
                Open = kiteQuote.Open,
                High = kiteQuote.High,
                Low = kiteQuote.Low,
                Close = kiteQuote.Close,
                Volume = kiteQuote.Volume,
                OpenInterest = kiteQuote.OpenInterest,
                TimeStamp = DateTime.UtcNow
            };
        }

        public async Task StartDataCollectionAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Starting intraday data collection");
            await ExecuteAsync(cancellationToken);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Starting intraday data collection service");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    // Check authentication status
                    var isAuthenticated = await _kiteConnectService.ValidateSessionAsync();
                    
                    if (!isAuthenticated)
                    {
                        _logger.LogWarning("Authentication invalid - waiting for token refresh");
                        await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
                        continue;
                    }

                    // Capture data for all supported indices
                    foreach (var symbol in new[] { "NIFTY", "BANKNIFTY", "FINNIFTY", "MIDCPNIFTY", "SENSEX", "BANKEX" })
                    {
                        try
                        {
                            var snapshots = await CaptureOptionSnapshots(symbol);
                            if (snapshots.Any())
                            {
                                _logger.LogInformation("Captured {count} new snapshots with circuit limit changes for {symbol}", 
                                    snapshots.Count, symbol);
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error capturing snapshots for {symbol}", symbol);
                        }
                    }

                    // Wait for next capture cycle (1 minute)
                    await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in data collection cycle");
                    await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
                }
            }

            _logger.LogInformation("Intraday data collection service stopped");
        }

        private async Task ProcessQuoteData(KiteQuote quote)
        {
            try
            {
                var instrument = await _dbContext.Instruments
                    .FirstOrDefaultAsync(i => i.InstrumentToken == quote.InstrumentToken.ToString());

                if (instrument == null)
                {
                    _logger.LogWarning("Received quote for unknown instrument: {token}", quote.InstrumentToken);
                    return;
                }

                var snapshot = new IntradayOptionSnapshot
                {
                    InstrumentToken = instrument.InstrumentToken,
                    TradingSymbol = instrument.TradingSymbol,
                    StrikePrice = instrument.Strike,
                    ExpiryDate = instrument.Expiry ?? DateTime.MinValue,
                    OptionType = instrument.InstrumentType,
                    LastPrice = quote.LastPrice,
                    Change = quote.Change,
                    Open = quote.Open,
                    High = quote.High,
                    Low = quote.Low,
                    Close = quote.Close,
                    Volume = quote.Volume,
                    OpenInterest = quote.OpenInterest,
                    LowerCircuitLimit = quote.LowerCircuitLimit,
                    UpperCircuitLimit = quote.UpperCircuitLimit,
                    ImpliedVolatility = quote.ImpliedVolatility,
                    Timestamp = DateTime.Now,
                    ChangeReason = "Real-time Quote Update"
                };

                await SaveSnapshotAsync(snapshot);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing quote data for instrument {token}", quote.InstrumentToken);
            }
        }
    }
} 