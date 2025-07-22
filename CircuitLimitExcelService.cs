using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using OptionAnalysisTool.Common.Data;
using OptionAnalysisTool.Models;

namespace OptionAnalysisTool.Common.Services
{
    /// <summary>
    /// 📊 CIRCUIT LIMIT EXCEL SERVICE
    /// Automatically creates and updates Excel files for circuit limit changes
    /// - One Excel file per index (NIFTY.xlsx, BANKNIFTY.xlsx, etc.)
    /// - One sheet per expiry date within each file
    /// - All strikes for each expiry in respective sheets
    /// - Automatic updates when circuit limits change
    /// Uses CSV format for Excel compatibility without external dependencies
    /// </summary>
    public class CircuitLimitExcelService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<CircuitLimitExcelService> _logger;
        
        // Configuration
        private readonly string _exportDirectory;
        private const string BACKUP_SUBFOLDER = "Backups";
        
        // Supported indices
        private readonly string[] SUPPORTED_INDICES = {
            "NIFTY", "BANKNIFTY", "FINNIFTY", "MIDCPNIFTY", 
            "SENSEX", "BANKEX"
        };

        public CircuitLimitExcelService(
            IServiceProvider serviceProvider,
            ILogger<CircuitLimitExcelService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
            
            // Set export directory to a dedicated folder
            _exportDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                "CircuitLimitExcelReports"
            );
            
            // Ensure directories exist
            Directory.CreateDirectory(_exportDirectory);
            Directory.CreateDirectory(Path.Combine(_exportDirectory, BACKUP_SUBFOLDER));
            
            _logger.LogInformation("📊 Excel Service initialized. Export directory: {directory}", _exportDirectory);
        }

        /// <summary>
        /// 🔥 MAIN METHOD: Export circuit limit changes to Excel when they occur
        /// Called automatically when circuit limits change during market hours
        /// </summary>
        public async Task ExportCircuitLimitChangesToExcel(List<CircuitLimitTracker> circuitLimitChanges)
        {
            try
            {
                if (!circuitLimitChanges?.Any() == true)
                {
                    _logger.LogDebug("No circuit limit changes to export");
                    return;
                }

                _logger.LogInformation("📊 Starting Excel export for {count} circuit limit changes", circuitLimitChanges.Count);

                // Group changes by index
                var changesByIndex = circuitLimitChanges
                    .GroupBy(c => c.UnderlyingSymbol)
                    .Where(g => SUPPORTED_INDICES.Contains(g.Key))
                    .ToList();

                foreach (var indexGroup in changesByIndex)
                {
                    var indexName = indexGroup.Key;
                    var indexChanges = indexGroup.ToList();
                    
                    await UpdateIndexExcelFile(indexName, indexChanges);
                }

                _logger.LogInformation("✅ Excel export completed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💥 Error exporting circuit limit changes to Excel");
            }
        }

        /// <summary>
        /// Update or create Excel files for a specific index
        /// </summary>
        private async Task UpdateIndexExcelFile(string indexName, List<CircuitLimitTracker> changes)
        {
            try
            {
                _logger.LogInformation("📊 Updating Excel files for {index} with {count} changes", indexName, changes.Count);

                // Get all current data for this index
                using var scope = _serviceProvider.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                
                var allIndexData = await GetCompleteIndexData(context, indexName);
                
                // Group by expiry date
                var dataByExpiry = allIndexData
                    .GroupBy(d => d.ExpiryDate.Date)
                    .OrderBy(g => g.Key)
                    .ToList();

                // Create one file per expiry (easier than managing sheets)
                foreach (var expiryGroup in dataByExpiry)
                {
                    var expiryDate = expiryGroup.Key;
                    var expiryData = expiryGroup.OrderBy(d => d.StrikePrice).ThenBy(d => d.OptionType).ToList();
                    
                    await CreateExpiryExcelFile(indexName, expiryDate, expiryData);
                }

                // Create main summary file
                await CreateSummaryExcelFile(indexName, allIndexData);
                
                _logger.LogInformation("✅ Updated Excel files for {index} with {expiries} expiry files", 
                    indexName, dataByExpiry.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💥 Error updating Excel files for {index}", indexName);
            }
        }

        /// <summary>
        /// Get complete circuit limit data for an index
        /// </summary>
        private async Task<List<CircuitLimitExportData>> GetCompleteIndexData(ApplicationDbContext context, string indexName)
        {
            var data = new List<CircuitLimitExportData>();

            try
            {
                // Get latest circuit limit changes
                var circuitChanges = await context.CircuitLimitTrackers
                    .Where(t => t.UnderlyingSymbol == indexName)
                    .OrderByDescending(t => t.DetectedAt)
                    .ToListAsync();

                // Get latest intraday snapshots for current circuit limits
                var latestSnapshots = await context.IntradayOptionSnapshots
                    .Where(s => s.UnderlyingSymbol == indexName)
                    .GroupBy(s => new { s.Symbol, s.StrikePrice, s.OptionType, s.ExpiryDate })
                    .Select(g => g.OrderByDescending(s => s.Timestamp).First())
                    .ToListAsync();

                // Get latest spot price for this index
                var latestSpotPrice = await context.SpotData
                    .Where(s => s.Symbol == indexName)
                    .OrderByDescending(s => s.Timestamp)
                    .FirstOrDefaultAsync();

                // Combine data for export
                foreach (var snapshot in latestSnapshots)
                {
                    // Find latest circuit limit change for this instrument
                    var latestChange = circuitChanges
                        .Where(c => c.Symbol == snapshot.Symbol)
                        .OrderByDescending(c => c.DetectedAt)
                        .FirstOrDefault();

                    var exportData = new CircuitLimitExportData
                    {
                        Symbol = snapshot.Symbol,
                        UnderlyingSymbol = indexName,
                        StrikePrice = snapshot.StrikePrice,
                        OptionType = snapshot.OptionType,
                        ExpiryDate = snapshot.ExpiryDate,
                        
                        // Current values
                        CurrentLowerLimit = snapshot.LowerCircuitLimit,
                        CurrentUpperLimit = snapshot.UpperCircuitLimit,
                        CurrentPrice = snapshot.LastPrice,
                        Volume = snapshot.Volume,
                        OpenInterest = snapshot.OpenInterest,
                        LastUpdated = snapshot.Timestamp,
                        
                        // Previous values (if circuit limit changed)
                        PreviousLowerLimit = latestChange?.PreviousLowerLimit ?? snapshot.LowerCircuitLimit,
                        PreviousUpperLimit = latestChange?.PreviousUpperLimit ?? snapshot.UpperCircuitLimit,
                        LowerLimitChangePercent = latestChange?.LowerLimitChangePercent ?? 0,
                        UpperLimitChangePercent = latestChange?.UpperLimitChangePercent ?? 0,
                        ChangeDetectedAt = latestChange?.DetectedAt,
                        SeverityLevel = latestChange?.SeverityLevel ?? "No Change",
                        
                        // 📈 SPOT PRICE DATA - ENHANCED
                        CurrentSpotPrice = latestSpotPrice?.LastPrice ?? latestChange?.UnderlyingPrice ?? 0,
                        SpotPriceAtChange = latestChange?.UnderlyingPrice ?? 0,
                        SpotPriceChangePercent = latestSpotPrice != null && latestChange != null && latestChange.UnderlyingPrice > 0 
                            ? ((latestSpotPrice.LastPrice - latestChange.UnderlyingPrice) / latestChange.UnderlyingPrice) * 100 
                            : 0,
                        SpotPriceTimestamp = latestSpotPrice?.Timestamp ?? snapshot.Timestamp,
                        
                        // Status indicators
                        HasCircuitLimitChanged = latestChange != null,
                        IsActivelyTrading = snapshot.Volume > 0
                    };

                    data.Add(exportData);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting complete index data for {index}", indexName);
            }

            return data;
        }

        /// <summary>
        /// Create Excel file for a specific expiry (CSV format for Excel compatibility)
        /// </summary>
        private async Task CreateExpiryExcelFile(string indexName, DateTime expiryDate, List<CircuitLimitExportData> expiryData)
        {
            try
            {
                var fileName = $"{indexName}_{expiryDate:yyyy-MM-dd}.csv";
                var filePath = Path.Combine(_exportDirectory, fileName);
                
                // Create backup if file exists
                if (File.Exists(filePath))
                {
                    await CreateBackup(filePath);
                }

                var csv = new StringBuilder();
                
                // Add title and metadata
                csv.AppendLine($"{indexName} Circuit Limits - Expiry: {expiryDate:yyyy-MM-dd}");
                csv.AppendLine($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                csv.AppendLine($"Total Strikes: {expiryData.Count}");
                csv.AppendLine($"Changed Limits: {expiryData.Count(d => d.HasCircuitLimitChanged)}");
                csv.AppendLine($"Active Trading: {expiryData.Count(d => d.IsActivelyTrading)}");
                
                // Add current spot price information
                var currentSpot = expiryData.FirstOrDefault()?.CurrentSpotPrice ?? 0;
                if (currentSpot > 0)
                {
                    csv.AppendLine($"Current {indexName} Spot: {currentSpot:F2}");
                }
                csv.AppendLine(); // Empty line

                // Headers with spot price columns
                csv.AppendLine("Symbol,Strike,Type,Current_LC,Current_UC,Previous_LC,Previous_UC,LC_Change_%,UC_Change_%,Current_Price,Volume,OI,Current_Spot,Spot_At_Change,Spot_Change_%,Severity,Last_Updated,Change_Detected,Status");

                // Data rows with spot prices
                foreach (var data in expiryData)
                {
                    csv.AppendLine($"{data.Symbol}," +
                                 $"{data.StrikePrice}," +
                                 $"{data.OptionType}," +
                                 $"{data.CurrentLowerLimit:F2}," +
                                 $"{data.CurrentUpperLimit:F2}," +
                                 $"{data.PreviousLowerLimit:F2}," +
                                 $"{data.PreviousUpperLimit:F2}," +
                                 $"{data.LowerLimitChangePercent:F2}," +
                                 $"{data.UpperLimitChangePercent:F2}," +
                                 $"{data.CurrentPrice:F2}," +
                                 $"{data.Volume}," +
                                 $"{data.OpenInterest}," +
                                 $"{data.CurrentSpotPrice:F2}," +
                                 $"{data.SpotPriceAtChange:F2}," +
                                 $"{data.SpotPriceChangePercent:F2}," +
                                 $"{data.SeverityLevel}," +
                                 $"{data.LastUpdated:yyyy-MM-dd HH:mm:ss}," +
                                 $"{data.ChangeDetectedAt?.ToString("yyyy-MM-dd HH:mm:ss") ?? "No Change"}," +
                                 $"{(data.IsActivelyTrading ? "Active" : "Inactive")}");
                }

                await File.WriteAllTextAsync(filePath, csv.ToString());
                
                _logger.LogInformation("✅ Created Excel file: {fileName} with {count} strikes + spot prices", fileName, expiryData.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💥 Error creating expiry Excel file for {index} {expiry}", indexName, expiryDate);
            }
        }

        /// <summary>
        /// Create main summary Excel file for an index
        /// </summary>
        private async Task CreateSummaryExcelFile(string indexName, List<CircuitLimitExportData> allData)
        {
            try
            {
                var fileName = $"{indexName}_Summary.csv";
                var filePath = Path.Combine(_exportDirectory, fileName);
                
                // Create backup if file exists
                if (File.Exists(filePath))
                {
                    await CreateBackup(filePath);
                }

                var csv = new StringBuilder();
                
                // Add title and overall statistics
                csv.AppendLine($"{indexName} Circuit Limit Summary Report");
                csv.AppendLine($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                csv.AppendLine($"Total Strikes Tracked: {allData.Count}");
                csv.AppendLine($"Strikes with Circuit Changes: {allData.Count(d => d.HasCircuitLimitChanged)}");
                csv.AppendLine($"Actively Trading Strikes: {allData.Count(d => d.IsActivelyTrading)}");
                csv.AppendLine();

                // Expiry breakdown
                csv.AppendLine("Expiry Breakdown:");
                csv.AppendLine("Expiry_Date,Total_Strikes,Changed_Limits,Active_Trading,Critical_Changes,High_Changes");
                
                var expiryStats = allData
                    .GroupBy(d => d.ExpiryDate.Date)
                    .Select(g => new
                    {
                        Expiry = g.Key,
                        TotalStrikes = g.Count(),
                        ChangedStrikes = g.Count(d => d.HasCircuitLimitChanged),
                        ActiveStrikes = g.Count(d => d.IsActivelyTrading),
                        CriticalChanges = g.Count(d => d.SeverityLevel == "Critical"),
                        HighChanges = g.Count(d => d.SeverityLevel == "High")
                    })
                    .OrderBy(e => e.Expiry)
                    .ToList();

                foreach (var stat in expiryStats)
                {
                    csv.AppendLine($"{stat.Expiry:yyyy-MM-dd}," +
                                 $"{stat.TotalStrikes}," +
                                 $"{stat.ChangedStrikes}," +
                                 $"{stat.ActiveStrikes}," +
                                 $"{stat.CriticalChanges}," +
                                 $"{stat.HighChanges}");
                }

                csv.AppendLine();
                csv.AppendLine("Recent Circuit Limit Changes (Critical and High only):");
                csv.AppendLine("Symbol,Strike,Type,Severity,LC_Change_%,UC_Change_%,Change_Time");

                var criticalChanges = allData
                    .Where(d => d.HasCircuitLimitChanged && (d.SeverityLevel == "Critical" || d.SeverityLevel == "High"))
                    .OrderByDescending(d => d.ChangeDetectedAt)
                    .Take(50) // Latest 50 critical/high changes
                    .ToList();

                foreach (var change in criticalChanges)
                {
                    csv.AppendLine($"{change.Symbol}," +
                                 $"{change.StrikePrice}," +
                                 $"{change.OptionType}," +
                                 $"{change.SeverityLevel}," +
                                 $"{change.LowerLimitChangePercent:F2}," +
                                 $"{change.UpperLimitChangePercent:F2}," +
                                 $"{change.ChangeDetectedAt:yyyy-MM-dd HH:mm:ss}");
                }

                await File.WriteAllTextAsync(filePath, csv.ToString());
                
                _logger.LogInformation("✅ Created summary Excel file: {fileName}", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💥 Error creating summary Excel file for {index}", indexName);
            }
        }

        /// <summary>
        /// Create backup of existing file
        /// </summary>
        private async Task CreateBackup(string filePath)
        {
            try
            {
                var fileName = Path.GetFileNameWithoutExtension(filePath);
                var extension = Path.GetExtension(filePath);
                var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                var backupFileName = $"{fileName}_backup_{timestamp}{extension}";
                var backupPath = Path.Combine(_exportDirectory, BACKUP_SUBFOLDER, backupFileName);
                
                File.Copy(filePath, backupPath, true);
                _logger.LogDebug("📋 Created backup: {backupFileName}", backupFileName);
                
                // Keep only last 5 backups per file
                await CleanupOldBackups(fileName);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "⚠️ Failed to create backup");
            }
        }

        /// <summary>
        /// Clean up old backup files
        /// </summary>
        private async Task CleanupOldBackups(string baseFileName)
        {
            try
            {
                var backupDir = Path.Combine(_exportDirectory, BACKUP_SUBFOLDER);
                var backupFiles = Directory.GetFiles(backupDir, $"{baseFileName}_backup_*.*")
                    .Select(f => new FileInfo(f))
                    .OrderByDescending(f => f.CreationTime)
                    .Skip(5) // Keep latest 5 backups
                    .ToList();

                foreach (var file in backupFiles)
                {
                    file.Delete();
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "⚠️ Failed to cleanup old backups");
            }
        }

        /// <summary>
        /// Export all current data to Excel files (manual export)
        /// </summary>
        public async Task ExportAllIndexesToExcel()
        {
            try
            {
                _logger.LogInformation("📊 Starting manual export of all indices to Excel");

                using var scope = _serviceProvider.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                foreach (var indexName in SUPPORTED_INDICES)
                {
                    var indexData = await GetCompleteIndexData(context, indexName);
                    
                    if (indexData.Any())
                    {
                        // Create fake circuit limit changes to trigger export
                        var fakeChanges = indexData
                            .Where(d => d.HasCircuitLimitChanged)
                            .Select(d => new CircuitLimitTracker
                            {
                                Symbol = d.Symbol,
                                UnderlyingSymbol = d.UnderlyingSymbol,
                                StrikePrice = d.StrikePrice,
                                OptionType = d.OptionType,
                                ExpiryDate = d.ExpiryDate,
                                DetectedAt = d.ChangeDetectedAt ?? DateTime.UtcNow
                            })
                            .ToList();

                        await UpdateIndexExcelFile(indexName, fakeChanges);
                    }
                }

                _logger.LogInformation("✅ Manual export completed for all indices");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💥 Error in manual export of all indices");
            }
        }

        /// <summary>
        /// Get export directory path
        /// </summary>
        public string GetExportDirectory() => _exportDirectory;
    }

    /// <summary>
    /// Data structure for Excel export
    /// </summary>
    public class CircuitLimitExportData
    {
        public string Symbol { get; set; } = string.Empty;
        public string UnderlyingSymbol { get; set; } = string.Empty;
        public decimal StrikePrice { get; set; }
        public string OptionType { get; set; } = string.Empty;
        public DateTime ExpiryDate { get; set; }
        
        // Current circuit limits
        public decimal CurrentLowerLimit { get; set; }
        public decimal CurrentUpperLimit { get; set; }
        
        // Previous circuit limits
        public decimal PreviousLowerLimit { get; set; }
        public decimal PreviousUpperLimit { get; set; }
        
        // Change analysis
        public decimal LowerLimitChangePercent { get; set; }
        public decimal UpperLimitChangePercent { get; set; }
        
        // Market data
        public decimal CurrentPrice { get; set; }
        public long Volume { get; set; }
        public long OpenInterest { get; set; }
        
        // Timestamps
        public DateTime LastUpdated { get; set; }
        public DateTime? ChangeDetectedAt { get; set; }
        
        // Status
        public string SeverityLevel { get; set; } = string.Empty;
        public bool HasCircuitLimitChanged { get; set; }
        public bool IsActivelyTrading { get; set; }
        
        // 📈 SPOT PRICE DATA - ENHANCED
        public decimal CurrentSpotPrice { get; set; }
        public decimal SpotPriceAtChange { get; set; }
        public decimal SpotPriceChangePercent { get; set; }
        public DateTime SpotPriceTimestamp { get; set; }
    }
} 