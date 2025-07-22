using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using OptionAnalysisTool.Common.Data;
using OptionAnalysisTool.Common.Services;
using OptionAnalysisTool.Models;
using System.Windows.Forms;

namespace OptionAnalysisTool.Common.Services
{
    /// <summary>
    /// 🔔 CIRCUIT LIMIT NOTIFICATION SERVICE WITH EXCEL EXPORT
    /// Provides real-time notifications when circuit limits change for any index
    /// Shows index-wise alerts with summary statistics
    /// Automatically updates Excel files when circuit limits change
    /// </summary>
    public class CircuitLimitNotificationService : BackgroundService
    {
        private readonly ILogger<CircuitLimitNotificationService> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly MarketHoursService _marketHoursService;
        private readonly CircuitLimitExcelService _excelService;
        
        // Notification tracking
        private readonly Dictionary<string, DateTime> _lastNotificationTime = new();
        private readonly Dictionary<string, int> _dailyNotificationCount = new();
        private DateTime _lastNotificationReset = DateTime.Today;
        
        // Configuration
        private const int NOTIFICATION_COOLDOWN_MINUTES = 2; // Don't spam notifications
        private const int MAX_DAILY_NOTIFICATIONS_PER_INDEX = 50; // Limit notifications per index per day
        private const int CHECK_INTERVAL_SECONDS = 10; // Check for changes every 10 seconds
        
        // Supported indices for notifications
        private readonly string[] SUPPORTED_INDICES = {
            "NIFTY", "BANKNIFTY", "FINNIFTY", "MIDCPNIFTY", 
            "SENSEX", "BANKEX"
        };

        public CircuitLimitNotificationService(
            ILogger<CircuitLimitNotificationService> logger,
            IServiceProvider serviceProvider,
            MarketHoursService marketHoursService,
            CircuitLimitExcelService excelService)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
            _marketHoursService = marketHoursService;
            _excelService = excelService;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("🔔 Circuit Limit Notification Service with Excel Export started");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    // Reset daily counters if needed
                    ResetDailyCountersIfNeeded();

                    // Only check during market hours
                    if (_marketHoursService.IsMarketOpen())
                    {
                        await CheckForCircuitLimitChanges();
                    }

                    // Wait before next check
                    await Task.Delay(TimeSpan.FromSeconds(CHECK_INTERVAL_SECONDS), stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in circuit limit notification service");
                    await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
                }
            }

            _logger.LogInformation("🔔 Circuit Limit Notification Service stopped");
        }

        /// <summary>
        /// Check for recent circuit limit changes and send notifications + update Excel
        /// </summary>
        private async Task CheckForCircuitLimitChanges()
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            try
            {
                var recentChangesCutoff = DateTime.UtcNow.AddMinutes(-5); // Last 5 minutes

                // Get recent circuit limit changes grouped by index
                var recentChanges = await context.CircuitLimitTrackers
                    .Where(t => t.DetectedAt >= recentChangesCutoff)
                    .GroupBy(t => t.UnderlyingSymbol)
                    .ToListAsync();

                foreach (var indexGroup in recentChanges)
                {
                    var indexName = indexGroup.Key;
                    var changes = indexGroup.ToList();

                    // Check if we should send notification for this index
                    if (ShouldSendNotification(indexName))
                    {
                        await SendIndexCircuitLimitNotification(indexName, changes);
                        
                        // 📊 AUTOMATICALLY UPDATE EXCEL FILES
                        await UpdateExcelFilesForChanges(indexName, changes);
                        
                        UpdateNotificationTracking(indexName);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking for circuit limit changes");
            }
        }

        /// <summary>
        /// 📊 UPDATE EXCEL FILES WHEN CIRCUIT LIMITS CHANGE
        /// </summary>
        private async Task UpdateExcelFilesForChanges(string indexName, List<CircuitLimitTracker> changes)
        {
            try
            {
                _logger.LogInformation("📊 Updating Excel files for {index} with {count} circuit limit changes", 
                    indexName, changes.Count);

                // Export to Excel automatically
                await _excelService.ExportCircuitLimitChangesToExcel(changes);

                _logger.LogInformation("✅ Excel files updated successfully for {index}", indexName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💥 Error updating Excel files for {index}", indexName);
            }
        }

        /// <summary>
        /// Send notification for circuit limit changes in a specific index
        /// </summary>
        private async Task SendIndexCircuitLimitNotification(string indexName, List<CircuitLimitTracker> changes)
        {
            try
            {
                var criticalChanges = changes.Where(c => c.SeverityLevel == "Critical").Count();
                var highChanges = changes.Where(c => c.SeverityLevel == "High").Count();
                var totalChanges = changes.Count;

                // Create notification message
                var title = $"🔥 {indexName} CIRCUIT LIMIT ALERT";
                var message = $"📊 Total Changes: {totalChanges}\n" +
                             $"🚨 Critical: {criticalChanges}\n" +
                             $"⚠️ High: {highChanges}\n" +
                             $"🕐 Time: {DateTime.Now:HH:mm:ss}\n" +
                             $"📋 Excel files updated automatically";

                // Show system notification (Windows toast)
                ShowSystemNotification(title, message);

                // Log the notification
                _logger.LogInformation("🔔 NOTIFICATION SENT: {index} - {total} changes ({critical} critical, {high} high) + Excel updated",
                    indexName, totalChanges, criticalChanges, highChanges);

                // Write to console for immediate visibility
                Console.WriteLine();
                Console.WriteLine("🔔 ===============================================");
                Console.WriteLine($"🔥 {indexName} CIRCUIT LIMIT CHANGES DETECTED");
                Console.WriteLine("🔔 ===============================================");
                Console.WriteLine($"📊 Total Changes: {totalChanges}");
                Console.WriteLine($"🚨 Critical Changes: {criticalChanges}");
                Console.WriteLine($"⚠️ High Priority Changes: {highChanges}");
                Console.WriteLine($"🕐 Detection Time: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                Console.WriteLine($"📋 Excel Files: AUTOMATICALLY UPDATED");

                // Show top 3 most significant changes
                var topChanges = changes
                    .OrderByDescending(c => Math.Max(Math.Abs(c.LowerLimitChangePercent), Math.Abs(c.UpperLimitChangePercent)))
                    .Take(3);

                Console.WriteLine();
                Console.WriteLine("🎯 TOP CHANGES:");
                foreach (var change in topChanges)
                {
                    Console.WriteLine($"   📈 {change.Symbol}: LC {change.PreviousLowerLimit:F2}→{change.NewLowerLimit:F2} " +
                                    $"UC {change.PreviousUpperLimit:F2}→{change.NewUpperLimit:F2} ({change.SeverityLevel})");
                }
                
                Console.WriteLine();
                Console.WriteLine("📊 EXCEL FILES LOCATION:");
                Console.WriteLine($"   📂 {_excelService.GetExportDirectory()}");
                Console.WriteLine($"   📄 {indexName}_Summary.csv (Main summary)");
                Console.WriteLine($"   📄 {indexName}_YYYY-MM-DD.csv (Per expiry files)");
                Console.WriteLine("🔔 ===============================================");
                Console.WriteLine();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending notification for {index}", indexName);
            }
        }

        /// <summary>
        /// Show Windows system notification
        /// </summary>
        private void ShowSystemNotification(string title, string message)
        {
            try
            {
                // Create balloon tip notification
                var notifyIcon = new NotifyIcon
                {
                    Icon = SystemIcons.Information,
                    BalloonTipTitle = title,
                    BalloonTipText = message,
                    BalloonTipIcon = ToolTipIcon.Warning,
                    Visible = true
                };

                notifyIcon.ShowBalloonTip(5000); // Show for 5 seconds

                // Dispose after showing
                Task.Delay(6000).ContinueWith(_ => notifyIcon.Dispose());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error showing system notification");
            }
        }

        /// <summary>
        /// Check if we should send notification for this index
        /// </summary>
        private bool ShouldSendNotification(string indexName)
        {
            // Check cooldown period
            if (_lastNotificationTime.ContainsKey(indexName))
            {
                var timeSinceLastNotification = DateTime.UtcNow - _lastNotificationTime[indexName];
                if (timeSinceLastNotification.TotalMinutes < NOTIFICATION_COOLDOWN_MINUTES)
                {
                    return false;
                }
            }

            // Check daily limit
            var dailyCount = _dailyNotificationCount.GetValueOrDefault(indexName, 0);
            if (dailyCount >= MAX_DAILY_NOTIFICATIONS_PER_INDEX)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Update notification tracking
        /// </summary>
        private void UpdateNotificationTracking(string indexName)
        {
            _lastNotificationTime[indexName] = DateTime.UtcNow;
            _dailyNotificationCount[indexName] = _dailyNotificationCount.GetValueOrDefault(indexName, 0) + 1;
        }

        /// <summary>
        /// Reset daily counters at start of new day
        /// </summary>
        private void ResetDailyCountersIfNeeded()
        {
            if (DateTime.Today > _lastNotificationReset)
            {
                _dailyNotificationCount.Clear();
                _lastNotificationReset = DateTime.Today;
                _logger.LogInformation("🔔 Daily notification counters reset for new trading day");
            }
        }

        /// <summary>
        /// Manual Excel export for all indices
        /// </summary>
        public async Task ExportAllToExcel()
        {
            try
            {
                _logger.LogInformation("📊 Manual Excel export triggered");
                await _excelService.ExportAllIndexesToExcel();
                _logger.LogInformation("✅ Manual Excel export completed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💥 Error in manual Excel export");
            }
        }

        /// <summary>
        /// Get notification statistics
        /// </summary>
        public object GetNotificationStatistics()
        {
            return new
            {
                TodaysNotifications = _dailyNotificationCount.Sum(kvp => kvp.Value),
                NotificationsByIndex = _dailyNotificationCount.ToDictionary(kvp => kvp.Key, kvp => kvp.Value),
                LastNotificationTimes = _lastNotificationTime.ToDictionary(kvp => kvp.Key, kvp => kvp.Value),
                DailyLimitPerIndex = MAX_DAILY_NOTIFICATIONS_PER_INDEX,
                CooldownMinutes = NOTIFICATION_COOLDOWN_MINUTES,
                SupportedIndices = SUPPORTED_INDICES,
                ExcelExportDirectory = _excelService.GetExportDirectory()
            };
        }
    }
} 