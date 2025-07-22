using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using OptionAnalysisTool.Common.Data;

namespace OptionAnalysisTool.Common.Services
{
    /// <summary>
    /// 🧹 DATA CLEANUP SERVICE
    /// 
    /// COMPREHENSIVE DATA MANAGEMENT:
    /// ✅ Clear existing data from specific dates
    /// ✅ Remove expired contracts and outdated data
    /// ✅ Maintain data integrity during cleanup
    /// ✅ Backup critical data before cleanup
    /// ✅ Selective cleanup by index, date range, or data type
    /// </summary>
    public class DataCleanupService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<DataCleanupService> _logger;

        public DataCleanupService(
            ApplicationDbContext context,
            ILogger<DataCleanupService> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// CLEAR ALL DATA FROM SPECIFIC DATE
        /// Use this to reset the system from a specific date
        /// </summary>
        public async Task<DataCleanupResult> ClearDataFromDateAsync(DateTime fromDate, bool includeCircuitLimits = true)
        {
            var result = new DataCleanupResult();
            
            try
            {
                _logger.LogWarning("🧹 STARTING DATA CLEANUP FROM DATE: {date}", fromDate);
                
                // 1. INTRADAY DATA CLEANUP
                var intradayRecords = await _context.IntradayOptionSnapshots
                    .Where(s => s.Timestamp >= fromDate)
                    .CountAsync();
                
                if (intradayRecords > 0)
                {
                    _context.IntradayOptionSnapshots.RemoveRange(
                        _context.IntradayOptionSnapshots.Where(s => s.Timestamp >= fromDate));
                    result.IntradayRecordsDeleted = intradayRecords;
                    _logger.LogInformation("🗑️ Deleted {count} intraday records from {date}", intradayRecords, fromDate);
                }

                // 2. HISTORICAL DATA CLEANUP
                var historicalRecords = await _context.HistoricalOptionData
                    .Where(h => h.TradingDate >= fromDate)
                    .CountAsync();
                
                if (historicalRecords > 0)
                {
                    _context.HistoricalOptionData.RemoveRange(
                        _context.HistoricalOptionData.Where(h => h.TradingDate >= fromDate));
                    result.HistoricalRecordsDeleted = historicalRecords;
                    _logger.LogInformation("🗑️ Deleted {count} historical records from {date}", historicalRecords, fromDate);
                }

                // 3. CIRCUIT LIMIT TRACKING CLEANUP (if requested)
                if (includeCircuitLimits)
                {
                    var circuitRecords = await _context.CircuitLimitTrackers
                        .Where(c => c.DetectedAt >= fromDate)
                        .CountAsync();
                    
                    if (circuitRecords > 0)
                    {
                        _context.CircuitLimitTrackers.RemoveRange(
                            _context.CircuitLimitTrackers.Where(c => c.DetectedAt >= fromDate));
                        result.CircuitLimitRecordsDeleted = circuitRecords;
                        _logger.LogInformation("🗑️ Deleted {count} circuit limit records from {date}", circuitRecords, fromDate);
                    }
                }

                // 4. SAVE CHANGES
                await _context.SaveChangesAsync();
                result.Success = true;
                result.CleanupDate = fromDate;
                result.CompletedAt = DateTime.UtcNow;

                _logger.LogWarning("✅ DATA CLEANUP COMPLETED - Deleted: Intraday({intraday}), Historical({historical}), CircuitLimits({circuit})",
                    result.IntradayRecordsDeleted, result.HistoricalRecordsDeleted, result.CircuitLimitRecordsDeleted);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💥 Data cleanup failed");
                result.Success = false;
                result.ErrorMessage = ex.Message;
                return result;
            }
        }

        /// <summary>
        /// SELECTIVE INDEX CLEANUP - Clear data for specific indices
        /// </summary>
        public async Task<DataCleanupResult> ClearIndexDataAsync(string[] indices, DateTime fromDate)
        {
            var result = new DataCleanupResult();
            
            try
            {
                _logger.LogWarning("🧹 STARTING INDEX-SPECIFIC CLEANUP: {indices} from {date}", 
                    string.Join(", ", indices), fromDate);
                
                foreach (var index in indices)
                {
                    // Clear intraday data for this index
                    var intradayCount = await _context.IntradayOptionSnapshots
                        .Where(s => s.UnderlyingSymbol == index && s.Timestamp >= fromDate)
                        .CountAsync();
                    
                    if (intradayCount > 0)
                    {
                        _context.IntradayOptionSnapshots.RemoveRange(
                            _context.IntradayOptionSnapshots.Where(s => s.UnderlyingSymbol == index && s.Timestamp >= fromDate));
                        result.IntradayRecordsDeleted += intradayCount;
                    }

                    // Clear historical data for this index
                    var historicalCount = await _context.HistoricalOptionData
                        .Where(h => h.UnderlyingSymbol == index && h.TradingDate >= fromDate)
                        .CountAsync();
                    
                    if (historicalCount > 0)
                    {
                        _context.HistoricalOptionData.RemoveRange(
                            _context.HistoricalOptionData.Where(h => h.UnderlyingSymbol == index && h.TradingDate >= fromDate));
                        result.HistoricalRecordsDeleted += historicalCount;
                    }

                    // Clear circuit limit data for this index
                    var circuitCount = await _context.CircuitLimitTrackers
                        .Where(c => c.UnderlyingSymbol == index && c.DetectedAt >= fromDate)
                        .CountAsync();
                    
                    if (circuitCount > 0)
                    {
                        _context.CircuitLimitTrackers.RemoveRange(
                            _context.CircuitLimitTrackers.Where(c => c.UnderlyingSymbol == index && c.DetectedAt >= fromDate));
                        result.CircuitLimitRecordsDeleted += circuitCount;
                    }

                    _logger.LogInformation("🗑️ Cleaned {index}: Intraday({intraday}), Historical({historical}), CircuitLimits({circuit})",
                        index, intradayCount, historicalCount, circuitCount);
                }

                await _context.SaveChangesAsync();
                result.Success = true;
                result.CleanupDate = fromDate;
                result.CompletedAt = DateTime.UtcNow;

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💥 Index-specific cleanup failed");
                result.Success = false;
                result.ErrorMessage = ex.Message;
                return result;
            }
        }

        /// <summary>
        /// REMOVE EXPIRED CONTRACTS - Clean up old expired contracts
        /// </summary>
        public async Task<DataCleanupResult> RemoveExpiredContractsAsync(DateTime? expiryBefore = null)
        {
            var result = new DataCleanupResult();
            var cutoffDate = expiryBefore ?? DateTime.Today.AddDays(-7); // Default: Remove contracts expired more than 7 days ago
            
            try
            {
                _logger.LogInformation("🧹 REMOVING EXPIRED CONTRACTS before {date}", cutoffDate);
                
                // Remove expired intraday data
                var expiredIntraday = await _context.IntradayOptionSnapshots
                    .Where(s => s.ExpiryDate < cutoffDate)
                    .CountAsync();
                
                if (expiredIntraday > 0)
                {
                    _context.IntradayOptionSnapshots.RemoveRange(
                        _context.IntradayOptionSnapshots.Where(s => s.ExpiryDate < cutoffDate));
                    result.IntradayRecordsDeleted = expiredIntraday;
                }

                // Remove expired historical data
                var expiredHistorical = await _context.HistoricalOptionData
                    .Where(h => h.ExpiryDate < cutoffDate)
                    .CountAsync();
                
                if (expiredHistorical > 0)
                {
                    _context.HistoricalOptionData.RemoveRange(
                        _context.HistoricalOptionData.Where(h => h.ExpiryDate < cutoffDate));
                    result.HistoricalRecordsDeleted = expiredHistorical;
                }

                // Remove expired circuit limit data
                var expiredCircuit = await _context.CircuitLimitTrackers
                    .Where(c => c.ExpiryDate < cutoffDate)
                    .CountAsync();
                
                if (expiredCircuit > 0)
                {
                    _context.CircuitLimitTrackers.RemoveRange(
                        _context.CircuitLimitTrackers.Where(c => c.ExpiryDate < cutoffDate));
                    result.CircuitLimitRecordsDeleted = expiredCircuit;
                }

                await _context.SaveChangesAsync();
                result.Success = true;
                result.CompletedAt = DateTime.UtcNow;

                _logger.LogInformation("✅ EXPIRED CONTRACTS CLEANUP COMPLETED - Removed: Intraday({intraday}), Historical({historical}), CircuitLimits({circuit})",
                    result.IntradayRecordsDeleted, result.HistoricalRecordsDeleted, result.CircuitLimitRecordsDeleted);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💥 Expired contracts cleanup failed");
                result.Success = false;
                result.ErrorMessage = ex.Message;
                return result;
            }
        }

        /// <summary>
        /// DATA INTEGRITY CHECK - Verify data consistency
        /// </summary>
        public async Task<DataIntegrityReport> CheckDataIntegrityAsync()
        {
            var report = new DataIntegrityReport();
            
            try
            {
                // Count total records
                report.TotalIntradayRecords = await _context.IntradayOptionSnapshots.CountAsync();
                report.TotalHistoricalRecords = await _context.HistoricalOptionData.CountAsync();
                report.TotalCircuitLimitRecords = await _context.CircuitLimitTrackers.CountAsync();

                // Check for orphaned records
                report.OrphanedIntradayRecords = await _context.IntradayOptionSnapshots
                    .Where(s => string.IsNullOrEmpty(s.InstrumentToken) || string.IsNullOrEmpty(s.Symbol))
                    .CountAsync();

                // Check for missing circuit limit data in historical records
                report.HistoricalRecordsWithoutCircuitLimits = await _context.HistoricalOptionData
                    .Where(h => h.LowerCircuitLimit == 0 && h.UpperCircuitLimit == 0)
                    .CountAsync();

                // Check data by index
                var indexStats = await _context.IntradayOptionSnapshots
                    .GroupBy(s => s.UnderlyingSymbol)
                    .Select(g => new { Index = g.Key, Count = g.Count() })
                    .ToListAsync();

                report.RecordsByIndex = indexStats.ToDictionary(s => s.Index, s => s.Count);

                report.IsHealthy = report.OrphanedIntradayRecords == 0;
                report.CheckedAt = DateTime.UtcNow;

                _logger.LogInformation("📊 DATA INTEGRITY CHECK: Total Records: {total}, Orphaned: {orphaned}, Healthy: {healthy}",
                    report.TotalIntradayRecords + report.TotalHistoricalRecords, report.OrphanedIntradayRecords, report.IsHealthy);

                return report;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💥 Data integrity check failed");
                report.ErrorMessage = ex.Message;
                return report;
            }
        }

        /// <summary>
        /// COMPACT DATABASE - Optimize database after cleanup
        /// </summary>
        public async Task CompactDatabaseAsync()
        {
            try
            {
                _logger.LogInformation("🔧 COMPACTING DATABASE...");
                
                // Execute database-specific optimization commands
                await _context.Database.ExecuteSqlRawAsync("DBCC SHRINKDATABASE(0)");
                await _context.Database.ExecuteSqlRawAsync("UPDATE STATISTICS");
                
                _logger.LogInformation("✅ Database compaction completed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💥 Database compaction failed");
            }
        }

        /// <summary>
        /// Removes duplicate entries from all tables based on key fields and timestamps
        /// </summary>
        public async Task<DuplicateCleanupResult> RemoveDuplicatesAsync()
        {
            var result = new DuplicateCleanupResult();
            
            try
            {
                _logger.LogInformation("Starting duplicate data cleanup process...");

                // Clean IntradayOptionSnapshots - Remove duplicates based on Symbol, StrikePrice, and CaptureTime (within same minute)
                var duplicateSnapshots = await _context.IntradayOptionSnapshots
                    .GroupBy(x => new { 
                        x.Symbol, 
                        x.StrikePrice, 
                        x.OptionType,
                        CaptureMinute = new DateTime(x.CaptureTime.Year, x.CaptureTime.Month, x.CaptureTime.Day, x.CaptureTime.Hour, x.CaptureTime.Minute, 0)
                    })
                    .Where(g => g.Count() > 1)
                    .SelectMany(g => g.OrderByDescending(x => x.CaptureTime).Skip(1)) // Keep the latest, remove others
                    .ToListAsync();

                if (duplicateSnapshots.Any())
                {
                    _context.IntradayOptionSnapshots.RemoveRange(duplicateSnapshots);
                    result.IntradaySnapshotDuplicatesRemoved = duplicateSnapshots.Count;
                    _logger.LogInformation($"Removed {duplicateSnapshots.Count} duplicate intraday snapshots");
                }

                // Clean CircuitLimitTrackers - Remove duplicates based on Symbol, StrikePrice, and exact timestamp
                var duplicateCircuits = await _context.CircuitLimitTrackers
                    .GroupBy(x => new { 
                        x.Symbol, 
                        x.StrikePrice, 
                        x.OptionType,
                        x.NewLowerLimit,
                        x.NewUpperLimit,
                        DetectedMinute = new DateTime(x.DetectedAt.Year, x.DetectedAt.Month, x.DetectedAt.Day, x.DetectedAt.Hour, x.DetectedAt.Minute, 0)
                    })
                    .Where(g => g.Count() > 1)
                    .SelectMany(g => g.OrderByDescending(x => x.DetectedAt).Skip(1)) // Keep the latest, remove others
                    .ToListAsync();

                if (duplicateCircuits.Any())
                {
                    _context.CircuitLimitTrackers.RemoveRange(duplicateCircuits);
                    result.CircuitLimitDuplicatesRemoved = duplicateCircuits.Count;
                    _logger.LogInformation($"Removed {duplicateCircuits.Count} duplicate circuit limit records");
                }

                // Clean AuthenticationTokens - Keep only the latest active token, remove old ones
                var oldTokens = await _context.AuthenticationTokens
                    .Where(x => x.IsActive)
                    .OrderByDescending(x => x.CreatedAt)
                    .Skip(1)
                    .ToListAsync();

                if (oldTokens.Any())
                {
                    foreach (var token in oldTokens)
                    {
                        token.IsActive = false;
                    }
                    result.OldTokensDeactivated = oldTokens.Count;
                    _logger.LogInformation($"Deactivated {oldTokens.Count} old authentication tokens");
                }

                // Save changes
                var changesCount = await _context.SaveChangesAsync();
                result.TotalChanges = changesCount;
                result.Success = true;

                _logger.LogInformation($"Duplicate cleanup completed successfully. Total changes: {changesCount}");
                
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during duplicate cleanup");
                result.Success = false;
                result.ErrorMessage = ex.Message;
                return result;
            }
        }

        /// <summary>
        /// Prevents duplicate snapshots before inserting new data
        /// </summary>
        public async Task<bool> IsSnapshotDuplicateAsync(string symbol, decimal strikePrice, string optionType, DateTime captureTime)
        {
            // Check if a snapshot exists within the same minute for the same instrument
            var captureMinute = new DateTime(captureTime.Year, captureTime.Month, captureTime.Day, captureTime.Hour, captureTime.Minute, 0);
            
            return await _context.IntradayOptionSnapshots
                .AnyAsync(x => x.Symbol == symbol && 
                             x.StrikePrice == strikePrice && 
                             x.OptionType == optionType &&
                             x.CaptureTime >= captureMinute && 
                             x.CaptureTime < captureMinute.AddMinutes(1));
        }

        /// <summary>
        /// Prevents duplicate circuit limit tracking
        /// </summary>
        public async Task<bool> IsCircuitLimitDuplicateAsync(string symbol, decimal strikePrice, string optionType, 
            decimal newLowerLimit, decimal newUpperLimit, DateTime detectedAt)
        {
            // Check if the same circuit limit change was already recorded within the last 5 minutes
            var fiveMinutesAgo = detectedAt.AddMinutes(-5);
            
            return await _context.CircuitLimitTrackers
                .AnyAsync(x => x.Symbol == symbol && 
                             x.StrikePrice == strikePrice && 
                             x.OptionType == optionType &&
                             x.NewLowerLimit == newLowerLimit &&
                             x.NewUpperLimit == newUpperLimit &&
                             x.DetectedAt >= fiveMinutesAgo);
        }

        /// <summary>
        /// Gets data quality statistics
        /// </summary>
        public async Task<DataQualityStats> GetDataQualityStatsAsync()
        {
            var stats = new DataQualityStats();
            
            try
            {
                // Total records
                stats.TotalIntradaySnapshots = await _context.IntradayOptionSnapshots.CountAsync();
                stats.TotalCircuitLimitTrackers = await _context.CircuitLimitTrackers.CountAsync();
                stats.TotalAuthTokens = await _context.AuthenticationTokens.CountAsync();

                // Today's records
                var today = DateTime.Today;
                stats.TodayIntradaySnapshots = await _context.IntradayOptionSnapshots
                    .CountAsync(x => x.CaptureTime.Date == today);
                stats.TodayCircuitLimitChanges = await _context.CircuitLimitTrackers
                    .CountAsync(x => x.DetectedAt.Date == today);

                // Active tokens
                stats.ActiveTokens = await _context.AuthenticationTokens
                    .CountAsync(x => x.IsActive && x.ExpiresAt > DateTime.UtcNow);

                // Latest activity
                stats.LatestSnapshotTime = await _context.IntradayOptionSnapshots
                    .MaxAsync(x => (DateTime?)x.CaptureTime);
                stats.LatestCircuitLimitTime = await _context.CircuitLimitTrackers
                    .MaxAsync(x => (DateTime?)x.DetectedAt);

                stats.Success = true;
            }
            catch (Exception ex)
            {
                stats.Success = false;
                stats.ErrorMessage = ex.Message;
            }

            return stats;
        }
    }

    // SUPPORTING CLASSES

    public class DataCleanupResult
    {
        public bool Success { get; set; }
        public DateTime CleanupDate { get; set; }
        public DateTime CompletedAt { get; set; }
        public int IntradayRecordsDeleted { get; set; }
        public int HistoricalRecordsDeleted { get; set; }
        public int CircuitLimitRecordsDeleted { get; set; }
        public string? ErrorMessage { get; set; }

        public int TotalRecordsDeleted => IntradayRecordsDeleted + HistoricalRecordsDeleted + CircuitLimitRecordsDeleted;
    }

    public class DataIntegrityReport
    {
        public DateTime CheckedAt { get; set; }
        public bool IsHealthy { get; set; }
        public int TotalIntradayRecords { get; set; }
        public int TotalHistoricalRecords { get; set; }
        public int TotalCircuitLimitRecords { get; set; }
        public int OrphanedIntradayRecords { get; set; }
        public int HistoricalRecordsWithoutCircuitLimits { get; set; }
        public Dictionary<string, int> RecordsByIndex { get; set; } = new();
        public string? ErrorMessage { get; set; }
    }

    public class DuplicateCleanupResult
    {
        public bool Success { get; set; }
        public int IntradaySnapshotDuplicatesRemoved { get; set; }
        public int CircuitLimitDuplicatesRemoved { get; set; }
        public int OldTokensDeactivated { get; set; }
        public int TotalChanges { get; set; }
        public string ErrorMessage { get; set; }
    }

    public class DataQualityStats
    {
        public bool Success { get; set; }
        public int TotalIntradaySnapshots { get; set; }
        public int TotalCircuitLimitTrackers { get; set; }
        public int TotalAuthTokens { get; set; }
        public int TodayIntradaySnapshots { get; set; }
        public int TodayCircuitLimitChanges { get; set; }
        public int ActiveTokens { get; set; }
        public DateTime? LatestSnapshotTime { get; set; }
        public DateTime? LatestCircuitLimitTime { get; set; }
        public string ErrorMessage { get; set; }
    }
} 