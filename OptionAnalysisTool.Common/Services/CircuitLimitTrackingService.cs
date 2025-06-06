using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using OptionAnalysisTool.Common.Data;
using OptionAnalysisTool.Models;
using Microsoft.EntityFrameworkCore;

namespace OptionAnalysisTool.Common.Services
{
    /// <summary>
    /// 🔥 ENHANCED CIRCUIT LIMIT TRACKING SERVICE - MARKET HOURS ONLY
    /// Tracks BOTH lower and upper circuit limit changes for ALL index option strikes
    /// Only during market hours (9:15 AM - 3:30 PM) on trading days
    /// </summary>
    public class CircuitLimitTrackingService
    {
        private readonly ApplicationDbContext _context;
        private readonly MarketHoursService _marketHoursService;
        private readonly ILogger<CircuitLimitTrackingService> _logger;

        // All supported index symbols for circuit limit tracking
        private readonly string[] SUPPORTED_INDICES = {
            "NIFTY", "BANKNIFTY", "FINNIFTY", "MIDCPNIFTY", 
            "SENSEX", "BANKEX"
        };

        public CircuitLimitTrackingService(
            ApplicationDbContext context,
            MarketHoursService marketHoursService,
            ILogger<CircuitLimitTrackingService> logger)
        {
            _context = context;
            _marketHoursService = marketHoursService;
            _logger = logger;
        }

        /// <summary>
        /// 🔥 CORE METHOD: Tracks circuit limit changes ONLY during market hours
        /// Tracks BOTH lower and upper limits for any index option strike
        /// </summary>
        public async Task<CircuitLimitTracker?> TrackCircuitLimitChange(
            string instrumentToken,
            string symbol,
            string underlyingSymbol,
            decimal strikePrice,
            string optionType,
            DateTime expiryDate,
            decimal currentLowerLimit,
            decimal currentUpperLimit,
            decimal currentPrice,
            decimal underlyingPrice,
            long volume,
            long openInterest)
        {
            try
            {
                // 🔥 CRITICAL CHECK: Only track during market hours
                if (!_marketHoursService.IsMarketOpen())
                {
                    _logger.LogDebug("🚫 Market closed - skipping circuit limit tracking for {symbol}", symbol);
                    return null;
                }

                // Check if this is a supported index option
                bool isSupportedIndex = SUPPORTED_INDICES.Any(index => 
                    underlyingSymbol.ToUpper().Contains(index));
                
                if (!isSupportedIndex)
                {
                    _logger.LogDebug("🚫 Not a supported index option: {symbol} ({underlying})", 
                        symbol, underlyingSymbol);
                    return null;
                }

                // Get the last tracked limit for this instrument
                var lastTracked = await _context.CircuitLimitTrackers
                    .Where(t => t.InstrumentToken == instrumentToken)
                    .OrderByDescending(t => t.DetectedAt)
                    .FirstOrDefaultAsync();

                // Check if EITHER lower OR upper limits have changed
                bool hasLowerChanged = false;
                bool hasUpperChanged = false;
                
                if (lastTracked != null)
                {
                    hasLowerChanged = Math.Abs(lastTracked.NewLowerLimit - currentLowerLimit) > 0.01m;
                    hasUpperChanged = Math.Abs(lastTracked.NewUpperLimit - currentUpperLimit) > 0.01m;
                }
                else
                {
                    // First time tracking - consider it a change
                    hasLowerChanged = true;
                    hasUpperChanged = true;
                }

                bool hasAnyChange = hasLowerChanged || hasUpperChanged;
                
                if (!hasAnyChange)
                {
                    _logger.LogDebug("✅ No circuit limit changes for {symbol}", symbol);
                    return null; // No change to track
                }

                // Create new tracking record for ANY circuit limit change
                var tracker = new CircuitLimitTracker
                {
                    InstrumentToken = instrumentToken,
                    Symbol = symbol,
                    UnderlyingSymbol = underlyingSymbol,
                    StrikePrice = strikePrice,
                    OptionType = optionType,
                    ExpiryDate = expiryDate,
                    PreviousLowerLimit = lastTracked?.NewLowerLimit ?? 0m,
                    NewLowerLimit = currentLowerLimit,
                    PreviousUpperLimit = lastTracked?.NewUpperLimit ?? 0m,
                    NewUpperLimit = currentUpperLimit,
                    CurrentPrice = currentPrice,
                    UnderlyingPrice = underlyingPrice,
                    Volume = volume,
                    OpenInterest = openInterest,
                    DetectedAt = DateTime.UtcNow,
                    LastUpdated = DateTime.UtcNow
                };

                // Calculate change percentages for BOTH limits
                if (lastTracked != null)
                {
                    tracker.LowerLimitChangePercent = lastTracked.NewLowerLimit != 0 
                        ? ((currentLowerLimit - lastTracked.NewLowerLimit) / lastTracked.NewLowerLimit) * 100
                        : 0;
                    
                    tracker.UpperLimitChangePercent = lastTracked.NewUpperLimit != 0
                        ? ((currentUpperLimit - lastTracked.NewUpperLimit) / lastTracked.NewUpperLimit) * 100
                        : 0;
                    
                    var previousRange = lastTracked.NewUpperLimit - lastTracked.NewLowerLimit;
                    var currentRange = currentUpperLimit - currentLowerLimit;
                    tracker.RangeChangePercent = previousRange != 0
                        ? ((currentRange - previousRange) / previousRange) * 100
                        : 0;
                }

                // Determine severity level and breach alerts
                tracker.SeverityLevel = DetermineSeverityLevel(tracker);
                tracker.IsBreachAlert = DetermineBreachAlert(tracker);
                tracker.ChangeReason = DetermineChangeReason(tracker);

                // Save to database
                _context.CircuitLimitTrackers.Add(tracker);
                await _context.SaveChangesAsync();

                // Log the change with details about which limits changed
                string changeDetails = "";
                if (hasLowerChanged && hasUpperChanged)
                    changeDetails = $"BOTH limits changed: Lower {tracker.PreviousLowerLimit:F2} → {tracker.NewLowerLimit:F2} ({tracker.LowerLimitChangePercent:F1}%), Upper {tracker.PreviousUpperLimit:F2} → {tracker.NewUpperLimit:F2} ({tracker.UpperLimitChangePercent:F1}%)";
                else if (hasLowerChanged)
                    changeDetails = $"LOWER limit changed: {tracker.PreviousLowerLimit:F2} → {tracker.NewLowerLimit:F2} ({tracker.LowerLimitChangePercent:F1}%)";
                else if (hasUpperChanged)
                    changeDetails = $"UPPER limit changed: {tracker.PreviousUpperLimit:F2} → {tracker.NewUpperLimit:F2} ({tracker.UpperLimitChangePercent:F1}%)";

                _logger.LogInformation(
                    "🔥 Circuit limit change tracked for {symbol}: {changeDetails}, Severity: {severity}",
                    symbol, changeDetails, tracker.SeverityLevel);

                return tracker;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💥 Error tracking circuit limit change for {instrumentToken}", instrumentToken);
                return null;
            }
        }

        /// <summary>
        /// Process intraday snapshot and track circuit limit changes
        /// </summary>
        public async Task<CircuitLimitTracker?> ProcessIntradaySnapshotAsync(IntradayOptionSnapshot snapshot)
        {
            try
            {
                return await TrackCircuitLimitChange(
                    snapshot.InstrumentToken,
                    snapshot.Symbol,
                    snapshot.UnderlyingSymbol,
                    snapshot.StrikePrice,
                    snapshot.OptionType,
                    snapshot.ExpiryDate,
                    snapshot.LowerCircuitLimit,
                    snapshot.UpperCircuitLimit,
                    snapshot.LastPrice,
                    0m, // underlyingPrice - not available in snapshot
                    snapshot.Volume,
                    snapshot.OpenInterest);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process intraday snapshot for {symbol}", snapshot.Symbol);
                return null;
            }
        }

        /// <summary>
        /// Get circuit limit changes during market hours only
        /// </summary>
        public async Task<List<CircuitLimitTracker>> GetTodaysCircuitLimitChanges(
            string? underlyingSymbol = null,
            string? severityLevel = null)
        {
            var today = DateTime.Today;
            
            // Only show changes if today is a trading day
            if (!_marketHoursService.IsTradingDay(today))
            {
                _logger.LogInformation("📅 Today is not a trading day - no circuit limit changes to show");
                return new List<CircuitLimitTracker>();
            }

            var query = _context.CircuitLimitTrackers
                .Where(t => t.DetectedAt.Date == today);

            if (!string.IsNullOrEmpty(underlyingSymbol))
                query = query.Where(t => t.UnderlyingSymbol == underlyingSymbol);

            if (!string.IsNullOrEmpty(severityLevel))
                query = query.Where(t => t.SeverityLevel == severityLevel);

            var changes = await query
                .OrderByDescending(t => t.DetectedAt)
                .ToListAsync();

            _logger.LogInformation("📊 Retrieved {count} circuit limit changes for today", changes.Count);
            return changes;
        }

        /// <summary>
        /// Get circuit limit changes for analysis with market hours filtering
        /// </summary>
        public async Task<List<CircuitLimitTracker>> GetCircuitLimitChanges(
            string? underlyingSymbol = null,
            DateTime? fromDate = null,
            DateTime? toDate = null,
            string? severityLevel = null)
        {
            var query = _context.CircuitLimitTrackers.AsQueryable();

            if (!string.IsNullOrEmpty(underlyingSymbol))
                query = query.Where(t => t.UnderlyingSymbol == underlyingSymbol);

            if (fromDate.HasValue)
                query = query.Where(t => t.DetectedAt >= fromDate.Value);

            if (toDate.HasValue)
                query = query.Where(t => t.DetectedAt <= toDate.Value);

            if (!string.IsNullOrEmpty(severityLevel))
                query = query.Where(t => t.SeverityLevel == severityLevel);

            return await query
                .OrderByDescending(t => t.DetectedAt)
                .Take(1000) // Limit results
                .ToListAsync();
        }

        /// <summary>
        /// Get critical circuit limit alerts (High/Critical severity only)
        /// </summary>
        public async Task<List<CircuitLimitTracker>> GetCriticalAlerts(DateTime? since = null)
        {
            var cutoffTime = since ?? DateTime.UtcNow.AddHours(-24);
            
            return await _context.CircuitLimitTrackers
                .Where(t => t.DetectedAt >= cutoffTime && 
                           (t.SeverityLevel == "High" || t.SeverityLevel == "Critical"))
                .OrderByDescending(t => t.DetectedAt)
                .Take(100)
                .ToListAsync();
        }

        /// <summary>
        /// Get statistics for ALL supported indices circuit limit changes
        /// </summary>
        public async Task<object> GetComprehensiveCircuitLimitStatistics(DateTime fromDate)
        {
            var allChanges = await _context.CircuitLimitTrackers
                .Where(t => t.DetectedAt >= fromDate)
                .ToListAsync();

            var statisticsByIndex = SUPPORTED_INDICES.Select(index =>
            {
                var indexChanges = allChanges.Where(t => t.UnderlyingSymbol.ToUpper().Contains(index)).ToList();
                
                return new
                {
                    Index = index,
                    TotalChanges = indexChanges.Count,
                    LowerLimitChanges = indexChanges.Count(t => t.HasLowerLimitChanged),
                    UpperLimitChanges = indexChanges.Count(t => t.HasUpperLimitChanged),
                    BothLimitsChanged = indexChanges.Count(t => t.HasLowerLimitChanged && t.HasUpperLimitChanged),
                    CriticalAlerts = indexChanges.Count(t => t.SeverityLevel == "Critical"),
                    HighAlerts = indexChanges.Count(t => t.SeverityLevel == "High"),
                    MediumAlerts = indexChanges.Count(t => t.SeverityLevel == "Medium"),
                    LowAlerts = indexChanges.Count(t => t.SeverityLevel == "Low"),
                    MaxLowerChangePercent = indexChanges.Any() ? indexChanges.Max(t => Math.Abs(t.LowerLimitChangePercent)) : 0,
                    MaxUpperChangePercent = indexChanges.Any() ? indexChanges.Max(t => Math.Abs(t.UpperLimitChangePercent)) : 0,
                    LastChangeTime = indexChanges.Any() ? indexChanges.Max(t => t.DetectedAt) : (DateTime?)null
                };
            }).ToList();

            return new
            {
                FromDate = fromDate,
                ToDate = DateTime.UtcNow,
                MarketHoursOnly = true,
                TotalIndicesTracked = SUPPORTED_INDICES.Length,
                OverallStatistics = new
                {
                    TotalChanges = allChanges.Count,
                    TotalLowerLimitChanges = allChanges.Count(t => t.HasLowerLimitChanged),
                    TotalUpperLimitChanges = allChanges.Count(t => t.HasUpperLimitChanged),
                    TotalCriticalAlerts = allChanges.Count(t => t.SeverityLevel == "Critical"),
                    TotalHighAlerts = allChanges.Count(t => t.SeverityLevel == "High")
                },
                StatisticsByIndex = statisticsByIndex
            };
        }

        /// <summary>
        /// Check if we should be tracking (market hours check)
        /// </summary>
        public bool ShouldTrackNow()
        {
            return _marketHoursService.IsMarketOpen();
        }

        /// <summary>
        /// Get time until next tracking session starts
        /// </summary>
        public TimeSpan GetTimeToNextTrackingSession()
        {
            if (_marketHoursService.IsMarketOpen())
                return TimeSpan.Zero;
            
            return _marketHoursService.GetTimeToMarketOpen();
        }

        private string DetermineSeverityLevel(CircuitLimitTracker tracker)
        {
            var maxChangePercent = Math.Max(
                Math.Abs(tracker.LowerLimitChangePercent),
                Math.Abs(tracker.UpperLimitChangePercent));

            if (maxChangePercent >= 20) return "Critical";
            if (maxChangePercent >= 10) return "High";
            if (maxChangePercent >= 5) return "Medium";
            return "Low";
        }

        private bool DetermineBreachAlert(CircuitLimitTracker tracker)
        {
            // Alert if current price is close to limits (within 2%)
            var lowerThreshold = tracker.NewLowerLimit * 1.02m;
            var upperThreshold = tracker.NewUpperLimit * 0.98m;
            
            return tracker.CurrentPrice <= lowerThreshold || tracker.CurrentPrice >= upperThreshold;
        }

        private string DetermineChangeReason(CircuitLimitTracker tracker)
        {
            if (Math.Abs(tracker.RangeChangePercent) > 15)
                return "Significant range change";
            
            if (tracker.HasLowerLimitChanged && tracker.HasUpperLimitChanged)
                return "Both limits changed";
            
            if (tracker.HasLowerLimitChanged)
                return "Lower limit adjustment";
            
            if (tracker.HasUpperLimitChanged)
                return "Upper limit adjustment";
            
            return "Limit update";
        }
    }
} 