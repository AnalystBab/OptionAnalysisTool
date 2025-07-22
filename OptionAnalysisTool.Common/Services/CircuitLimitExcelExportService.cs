using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using System.Drawing;
using OptionAnalysisTool.Common.Data;
using OptionAnalysisTool.Models;

namespace OptionAnalysisTool.Common.Services
{
    /// <summary>
    /// 📊 CIRCUIT LIMIT EXCEL EXPORT SERVICE
    /// Automatically creates and updates Excel files for circuit limit changes
    /// - One Excel file per index (NIFTY.xlsx, BANKNIFTY.xlsx, etc.)
    /// - One sheet per expiry date within each file
    /// - All strikes for each expiry in respective sheets
    /// - Automatic updates when circuit limits change
    /// </summary>
    public class CircuitLimitExcelExportService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<CircuitLimitExcelExportService> _logger;
        
        // Configuration
        private readonly string _exportDirectory;
        private const string BACKUP_SUBFOLDER = "Backups";
        
        // Supported indices
        private readonly string[] SUPPORTED_INDICES = {
            "NIFTY", "BANKNIFTY", "FINNIFTY", "MIDCPNIFTY", 
            "SENSEX", "BANKEX"
        };

        // Excel styling cache
        private readonly Dictionary<string, ExcelRange> _headerStyleCache = new();

        public CircuitLimitExcelExportService(
            IServiceProvider serviceProvider,
            ILogger<CircuitLimitExcelExportService> logger)
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
            
            // Set EPPlus license context
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            
            _logger.LogInformation("📊 Excel Export Service initialized. Export directory: {directory}", _exportDirectory);
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
        /// Update or create Excel file for a specific index
        /// </summary>
        private async Task UpdateIndexExcelFile(string indexName, List<CircuitLimitTracker> changes)
        {
            try
            {
                var fileName = $"{indexName}_CircuitLimits.xlsx";
                var filePath = Path.Combine(_exportDirectory, fileName);
                
                _logger.LogInformation("📊 Updating Excel file for {index}: {fileName}", indexName, fileName);

                // Create backup if file exists
                if (File.Exists(filePath))
                {
                    await CreateBackup(filePath, indexName);
                }

                using var package = File.Exists(filePath) 
                    ? new ExcelPackage(new FileInfo(filePath))
                    : new ExcelPackage();

                // Get all current data for this index (including historical)
                using var scope = _serviceProvider.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                
                var allIndexData = await GetCompleteIndexData(context, indexName);
                
                // Group by expiry date
                var dataByExpiry = allIndexData
                    .GroupBy(d => d.ExpiryDate.Date)
                    .OrderBy(g => g.Key)
                    .ToList();

                foreach (var expiryGroup in dataByExpiry)
                {
                    var expiryDate = expiryGroup.Key;
                    var expiryData = expiryGroup.OrderBy(d => d.StrikePrice).ThenBy(d => d.OptionType).ToList();
                    
                    await UpdateExpirySheet(package, indexName, expiryDate, expiryData);
                }

                // Add summary sheet
                await CreateSummarySheet(package, indexName, allIndexData);

                // Save the file
                await package.SaveAsync();
                
                _logger.LogInformation("✅ Updated Excel file: {fileName} with {expiries} expiry sheets", 
                    fileName, dataByExpiry.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💥 Error updating Excel file for {index}", indexName);
            }
        }

        /// <summary>
        /// Get complete circuit limit data for an index (current + historical)
        /// </summary>
        private async Task<List<CircuitLimitExportData>> GetCompleteIndexData(ApplicationDbContext context, string indexName)
        {
            var data = new List<CircuitLimitExportData>();

            // 1. Get latest circuit limit changes
            var circuitChanges = await context.CircuitLimitTrackers
                .Where(t => t.UnderlyingSymbol == indexName)
                .OrderByDescending(t => t.DetectedAt)
                .ToListAsync();

            // 2. Get latest intraday snapshots for current circuit limits
            var latestSnapshots = await context.IntradayOptionSnapshots
                .Where(s => s.UnderlyingSymbol == indexName)
                .GroupBy(s => new { s.Symbol, s.StrikePrice, s.OptionType, s.ExpiryDate })
                .Select(g => g.OrderByDescending(s => s.Timestamp).First())
                .ToListAsync();

            // 3. Combine data for export
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
                    
                    // Status indicators
                    HasCircuitLimitChanged = latestChange != null,
                    IsActivelyTrading = snapshot.Volume > 0
                };

                data.Add(exportData);
            }

            return data;
        }

        /// <summary>
        /// Update or create worksheet for a specific expiry
        /// </summary>
        private async Task UpdateExpirySheet(ExcelPackage package, string indexName, DateTime expiryDate, 
            List<CircuitLimitExportData> expiryData)
        {
            try
            {
                var sheetName = expiryDate.ToString("yyyy-MM-dd");
                
                // Remove existing sheet if it exists
                var existingSheet = package.Workbook.Worksheets[sheetName];
                if (existingSheet != null)
                {
                    package.Workbook.Worksheets.Delete(existingSheet);
                }

                // Create new sheet
                var worksheet = package.Workbook.Worksheets.Add(sheetName);
                
                // Set up headers
                var headers = new[]
                {
                    "Symbol", "Strike", "Type", "Current LC", "Current UC", "Previous LC", "Previous UC",
                    "LC Change %", "UC Change %", "Current Price", "Volume", "OI", "Severity", 
                    "Last Updated", "Change Detected", "Status"
                };

                // Apply headers
                for (int i = 0; i < headers.Length; i++)
                {
                    worksheet.Cells[1, i + 1].Value = headers[i];
                }

                // Style headers
                using (var headerRange = worksheet.Cells[1, 1, 1, headers.Length])
                {
                    headerRange.Style.Font.Bold = true;
                    headerRange.Style.Fill.PatternType = ExcelFillPatternType.Solid;
                    headerRange.Style.Fill.BackgroundColor.SetColor(Color.LightBlue);
                    headerRange.Style.Border.BorderAround(ExcelBorderStyle.Medium);
                }

                // Add data
                int row = 2;
                foreach (var data in expiryData)
                {
                    worksheet.Cells[row, 1].Value = data.Symbol;
                    worksheet.Cells[row, 2].Value = data.StrikePrice;
                    worksheet.Cells[row, 3].Value = data.OptionType;
                    worksheet.Cells[row, 4].Value = data.CurrentLowerLimit;
                    worksheet.Cells[row, 5].Value = data.CurrentUpperLimit;
                    worksheet.Cells[row, 6].Value = data.PreviousLowerLimit;
                    worksheet.Cells[row, 7].Value = data.PreviousUpperLimit;
                    worksheet.Cells[row, 8].Value = data.LowerLimitChangePercent;
                    worksheet.Cells[row, 9].Value = data.UpperLimitChangePercent;
                    worksheet.Cells[row, 10].Value = data.CurrentPrice;
                    worksheet.Cells[row, 11].Value = data.Volume;
                    worksheet.Cells[row, 12].Value = data.OpenInterest;
                    worksheet.Cells[row, 13].Value = data.SeverityLevel;
                    worksheet.Cells[row, 14].Value = data.LastUpdated.ToString("yyyy-MM-dd HH:mm:ss");
                    worksheet.Cells[row, 15].Value = data.ChangeDetectedAt?.ToString("yyyy-MM-dd HH:mm:ss") ?? "No Change";
                    worksheet.Cells[row, 16].Value = data.IsActivelyTrading ? "Active" : "Inactive";

                    // Highlight rows with circuit limit changes
                    if (data.HasCircuitLimitChanged)
                    {
                        using (var dataRange = worksheet.Cells[row, 1, row, headers.Length])
                        {
                            dataRange.Style.Fill.PatternType = ExcelFillPatternType.Solid;
                            
                            // Color based on severity
                            switch (data.SeverityLevel)
                            {
                                case "Critical":
                                    dataRange.Style.Fill.BackgroundColor.SetColor(Color.LightPink);
                                    break;
                                case "High":
                                    dataRange.Style.Fill.BackgroundColor.SetColor(Color.LightYellow);
                                    break;
                                case "Medium":
                                    dataRange.Style.Fill.BackgroundColor.SetColor(Color.LightGreen);
                                    break;
                            }
                        }
                    }

                    row++;
                }

                // Auto-fit columns
                worksheet.Cells.AutoFitColumns();
                
                // Add sheet summary
                worksheet.Cells[row + 2, 1].Value = "Summary:";
                worksheet.Cells[row + 3, 1].Value = $"Total Strikes: {expiryData.Count}";
                worksheet.Cells[row + 4, 1].Value = $"Changed Limits: {expiryData.Count(d => d.HasCircuitLimitChanged)}";
                worksheet.Cells[row + 5, 1].Value = $"Active Trading: {expiryData.Count(d => d.IsActivelyTrading)}";
                worksheet.Cells[row + 6, 1].Value = $"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}";

                _logger.LogInformation("✅ Updated sheet '{sheetName}' with {count} strikes", sheetName, expiryData.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💥 Error updating expiry sheet for {expiry}", expiryDate);
            }
        }

        /// <summary>
        /// Create summary sheet for the index
        /// </summary>
        private async Task CreateSummarySheet(ExcelPackage package, string indexName, List<CircuitLimitExportData> allData)
        {
            try
            {
                // Remove existing summary sheet if it exists
                var existingSummary = package.Workbook.Worksheets["Summary"];
                if (existingSummary != null)
                {
                    package.Workbook.Worksheets.Delete(existingSummary);
                }

                // Create summary sheet
                var worksheet = package.Workbook.Worksheets.Add("Summary");
                
                // Add title
                worksheet.Cells[1, 1].Value = $"{indexName} Circuit Limit Summary";
                worksheet.Cells[1, 1].Style.Font.Size = 16;
                worksheet.Cells[1, 1].Style.Font.Bold = true;

                // Add statistics
                int row = 3;
                worksheet.Cells[row++, 1].Value = $"Report Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}";
                worksheet.Cells[row++, 1].Value = $"Total Strikes Tracked: {allData.Count}";
                worksheet.Cells[row++, 1].Value = $"Strikes with Circuit Changes: {allData.Count(d => d.HasCircuitLimitChanged)}";
                worksheet.Cells[row++, 1].Value = $"Actively Trading Strikes: {allData.Count(d => d.IsActivelyTrading)}";
                
                // Expiry breakdown
                row += 2;
                worksheet.Cells[row++, 1].Value = "Expiry Breakdown:";
                
                var expiryStats = allData
                    .GroupBy(d => d.ExpiryDate.Date)
                    .Select(g => new
                    {
                        Expiry = g.Key,
                        TotalStrikes = g.Count(),
                        ChangedStrikes = g.Count(d => d.HasCircuitLimitChanged),
                        ActiveStrikes = g.Count(d => d.IsActivelyTrading)
                    })
                    .OrderBy(e => e.Expiry)
                    .ToList();

                worksheet.Cells[row, 1].Value = "Expiry Date";
                worksheet.Cells[row, 2].Value = "Total Strikes";
                worksheet.Cells[row, 3].Value = "Changed Limits";
                worksheet.Cells[row, 4].Value = "Active Trading";
                row++;

                foreach (var stat in expiryStats)
                {
                    worksheet.Cells[row, 1].Value = stat.Expiry.ToString("yyyy-MM-dd");
                    worksheet.Cells[row, 2].Value = stat.TotalStrikes;
                    worksheet.Cells[row, 3].Value = stat.ChangedStrikes;
                    worksheet.Cells[row, 4].Value = stat.ActiveStrikes;
                    row++;
                }

                worksheet.Cells.AutoFitColumns();
                
                _logger.LogInformation("✅ Created summary sheet for {index}", indexName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💥 Error creating summary sheet for {index}", indexName);
            }
        }

        /// <summary>
        /// Create backup of existing file
        /// </summary>
        private async Task CreateBackup(string filePath, string indexName)
        {
            try
            {
                var fileName = Path.GetFileNameWithoutExtension(filePath);
                var extension = Path.GetExtension(filePath);
                var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                var backupFileName = $"{fileName}_backup_{timestamp}{extension}";
                var backupPath = Path.Combine(_exportDirectory, BACKUP_SUBFOLDER, backupFileName);
                
                File.Copy(filePath, backupPath, true);
                _logger.LogInformation("📋 Created backup: {backupFileName}", backupFileName);
                
                // Keep only last 10 backups per index
                await CleanupOldBackups(indexName);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "⚠️ Failed to create backup for {index}", indexName);
            }
        }

        /// <summary>
        /// Clean up old backup files
        /// </summary>
        private async Task CleanupOldBackups(string indexName)
        {
            try
            {
                var backupDir = Path.Combine(_exportDirectory, BACKUP_SUBFOLDER);
                var backupFiles = Directory.GetFiles(backupDir, $"{indexName}_CircuitLimits_backup_*.xlsx")
                    .Select(f => new FileInfo(f))
                    .OrderByDescending(f => f.CreationTime)
                    .Skip(10) // Keep latest 10 backups
                    .ToList();

                foreach (var file in backupFiles)
                {
                    file.Delete();
                }
                
                if (backupFiles.Any())
                {
                    _logger.LogInformation("🧹 Cleaned up {count} old backup files for {index}", backupFiles.Count, indexName);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "⚠️ Failed to cleanup old backups for {index}", indexName);
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
    }
} 