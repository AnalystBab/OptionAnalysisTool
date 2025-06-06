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
using KiteInstrument = OptionAnalysisTool.KiteConnect.Models.KiteInstrument;
using KiteQuote = OptionAnalysisTool.KiteConnect.Models.KiteQuote;
using DomainInstrument = OptionAnalysisTool.Models.Instrument;
using DomainQuote = OptionAnalysisTool.Models.Quote;

namespace OptionAnalysisTool.Common.Services
{
    public class IntradayDataService
    {
        private readonly IKiteConnectService _kiteConnectService;
        private readonly ApplicationDbContext _dbContext;
        private readonly ILogger<IntradayDataService> _logger;
        private readonly IMarketDataRepository _marketDataRepository;
        private const decimal CIRCUIT_BUFFER_PERCENTAGE = 0.01m;
        private const decimal MAX_CIRCUIT_RANGE_PERCENTAGE = 0.40m;

        public IntradayDataService(
            IKiteConnectService kiteConnectService,
            ApplicationDbContext dbContext,
            ILogger<IntradayDataService> logger,
            IMarketDataRepository marketDataRepository)
        {
            _kiteConnectService = kiteConnectService;
            _dbContext = dbContext;
            _logger = logger;
            _marketDataRepository = marketDataRepository;
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
                        // Validate circuit limits
                        if (!ValidateCircuitLimits(snapshot))
                        {
                            _logger.LogWarning("Invalid circuit limits detected for {symbol}. Upper: {upper}, Lower: {lower}, Last: {last}", 
                                snapshot.Symbol, snapshot.UpperCircuitLimit, snapshot.LowerCircuitLimit, snapshot.LastPrice);
                            continue;
                        }
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
            // Basic validation rules for circuit limits
            if (snapshot.UpperCircuitLimit <= 0 || snapshot.LowerCircuitLimit <= 0)
            {
                return false;
            }

            if (snapshot.UpperCircuitLimit <= snapshot.LowerCircuitLimit)
            {
                return false;
            }

            // Check if last price is within circuit limits
            if (snapshot.LastPrice < snapshot.LowerCircuitLimit || snapshot.LastPrice > snapshot.UpperCircuitLimit)
            {
                return false;
            }

            // Additional validation for reasonable circuit limit range
            var priceRange = snapshot.UpperCircuitLimit - snapshot.LowerCircuitLimit;
            var averagePrice = (snapshot.UpperCircuitLimit + snapshot.LowerCircuitLimit) / 2m;
            
            // Circuit range should not be more than 40% of average price for options
            if (priceRange > (averagePrice * MAX_CIRCUIT_RANGE_PERCENTAGE))
            {
                return false;
            }

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

        private async Task<IntradayOptionSnapshot> CaptureOptionSnapshot(Instrument option)
        {
            try
            {
                var quote = await _kiteConnectService.GetQuotesAsync(new[] { option.InstrumentToken });
                if (!quote.Any())
                {
                    _logger.LogWarning("No quote data available for {symbol}", option.TradingSymbol);
                    return null;
                }

                var kiteQuote = quote.Values.First();
                var commonQuote = DomainQuote.FromKiteQuote(kiteQuote);
                if (commonQuote == null)
                {
                    _logger.LogWarning("Failed to convert quote data for {symbol}", option.TradingSymbol);
                    return null;
                }

                // Validate circuit limits from quote
                if (commonQuote.LowerCircuitLimit <= 0 || commonQuote.UpperCircuitLimit <= 0)
                {
                    _logger.LogWarning("Invalid circuit limits in quote for {symbol}. Upper: {upper}, Lower: {lower}", 
                        option.TradingSymbol, commonQuote.UpperCircuitLimit, commonQuote.LowerCircuitLimit);
                    return null;
                }

                var snapshot = new IntradayOptionSnapshot
                {
                    InstrumentToken = option.InstrumentToken,
                    Symbol = option.TradingSymbol,
                    UnderlyingSymbol = option.Name,
                    StrikePrice = option.Strike,
                    OptionType = option.InstrumentType,
                    ExpiryDate = option.Expiry ?? DateTime.MinValue,
                    LastPrice = commonQuote.LastPrice,
                    Open = commonQuote.Open,
                    High = commonQuote.High,
                    Low = commonQuote.Low,
                    Close = commonQuote.Close,
                    Change = commonQuote.Change,
                    Volume = commonQuote.Volume,
                    OpenInterest = commonQuote.OpenInterest,
                    LowerCircuitLimit = commonQuote.LowerCircuitLimit,
                    UpperCircuitLimit = commonQuote.UpperCircuitLimit,
                    ImpliedVolatility = commonQuote.ImpliedVolatility,
                    Timestamp = commonQuote.TimeStamp,
                    LastUpdated = DateTime.UtcNow,
                    CircuitLimitStatus = DetermineCircuitLimitStatus(commonQuote),
                    ValidationMessage = string.Empty,
                    TradingStatus = "Normal",
                    IsValidData = true
                };

                _logger.LogDebug("Captured snapshot for {symbol} at {time}. Circuit Limits - Lower: {lower}, Upper: {upper}", 
                    option.TradingSymbol, snapshot.LastUpdated, snapshot.LowerCircuitLimit, snapshot.UpperCircuitLimit);

                return snapshot;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error capturing snapshot for {symbol}", option.TradingSymbol);
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
                // Group snapshots by symbol to handle duplicates
                var groupedSnapshots = snapshots.GroupBy(s => s.Symbol);
                
                foreach (var group in groupedSnapshots)
                {
                    var symbol = group.Key;
                    var latestSnapshot = group.OrderByDescending(s => s.Timestamp).First();

                    // Check for existing snapshot in last minute
                    var existingSnapshot = await _dbContext.IntradayOptionSnapshots
                        .Where(s => s.Symbol == symbol && 
                                  s.Timestamp >= latestSnapshot.Timestamp.AddMinutes(-1))
                        .FirstOrDefaultAsync();

                    if (existingSnapshot == null)
                    {
                        await _dbContext.IntradayOptionSnapshots.AddAsync(latestSnapshot);
                    }
                    else
                    {
                        _dbContext.Entry(existingSnapshot).CurrentValues.SetValues(latestSnapshot);
                    }
                }

                await _dbContext.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving snapshots");
                throw;
            }
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
                _logger.LogError(ex, "Error saving snapshot for {symbol}", snapshot.Symbol);
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
    }
} 