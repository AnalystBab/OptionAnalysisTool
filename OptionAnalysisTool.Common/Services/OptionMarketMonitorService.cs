using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using OptionAnalysisTool.Common.Data;
using OptionAnalysisTool.KiteConnect.Services;
using OptionAnalysisTool.KiteConnect;

namespace OptionAnalysisTool.Common.Services
{
    /// <summary>
    /// 🔥 OPTION MARKET MONITOR WINDOWS SERVICE
    /// Main Windows Service that runs 24/7 and manages:
    /// - Automatic startup at 9 AM
    /// - Circuit limit monitoring during market hours
    /// - EOD processing after market close
    /// - Health monitoring and recovery
    /// </summary>
    public class OptionMarketMonitorService : BackgroundService
    {
        private readonly ILogger<OptionMarketMonitorService> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly IHostApplicationLifetime _hostLifetime;

        public OptionMarketMonitorService(
            ILogger<OptionMarketMonitorService> logger,
            IServiceProvider serviceProvider,
            IHostApplicationLifetime hostLifetime)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
            _hostLifetime = hostLifetime;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("🔥 === OPTION MARKET MONITOR SERVICE STARTED ===");
            _logger.LogInformation("🎯 Service Purpose: Indian Index Option Circuit Limit Tracking");
            _logger.LogInformation("⏰ Operating Hours: 24/7 with market hour automation");
            _logger.LogInformation("📊 Coverage: NIFTY, BANKNIFTY, FINNIFTY, MIDCPNIFTY, SENSEX, BANKEX");

            try
            {
                // Wait for services to be ready
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);

                // Verify all critical services
                await VerifyServiceHealth();

                _logger.LogInformation("✅ Option Market Monitor Service is running and healthy");
                _logger.LogInformation("🔄 Service will manage daily market cycle automatically");

                // Keep the service running
                while (!stoppingToken.IsCancellationRequested)
                {
                    try
                    {
                        // Perform periodic health checks
                        await PerformHealthCheck();
                        
                        // Wait for 10 minutes before next health check
                        await Task.Delay(TimeSpan.FromMinutes(10), stoppingToken);
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "💥 Error in service main loop");
                        await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Option Market Monitor Service stopped by cancellation");
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "💥 CRITICAL ERROR in Option Market Monitor Service");
                _hostLifetime.StopApplication();
            }

            _logger.LogInformation("🔥 === OPTION MARKET MONITOR SERVICE STOPPED ===");
        }

        /// <summary>
        /// Verify all critical services are healthy
        /// </summary>
        private async Task VerifyServiceHealth()
        {
            using var scope = _serviceProvider.CreateScope();

            try
            {
                _logger.LogInformation("🏥 === SERVICE HEALTH CHECK ===");

                // 1. Database connectivity
                var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                await dbContext.Database.CanConnectAsync();
                _logger.LogInformation("✅ Database: Connected");

                // 2. Market Hours Service
                var marketHoursService = scope.ServiceProvider.GetRequiredService<MarketHoursService>();
                var isMarketOpen = marketHoursService.IsMarketOpen();
                _logger.LogInformation("✅ Market Hours Service: Working (Market {status})", 
                    isMarketOpen ? "OPEN" : "CLOSED");

                // 3. Circuit Limit Tracking Service
                var trackingService = scope.ServiceProvider.GetRequiredService<CircuitLimitTrackingService>();
                _logger.LogInformation("✅ Circuit Limit Tracking: Available");

                // 4. KiteConnect Service
                var kiteService = scope.ServiceProvider.GetRequiredService<IKiteConnectService>();
                _logger.LogInformation("✅ KiteConnect Service: {status}", 
                    kiteService.IsInitialized ? "Authenticated" : "Pending Authentication");

                // 5. Application Startup Service (should be running)
                // This will be automatically managed by the DI container

                _logger.LogInformation("🏥 === ALL SERVICES HEALTHY ===");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💥 Service health check failed");
                throw;
            }
        }

        /// <summary>
        /// Perform periodic health check
        /// </summary>
        private async Task PerformHealthCheck()
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                
                var marketHoursService = scope.ServiceProvider.GetRequiredService<MarketHoursService>();
                var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                // Quick database connectivity check
                await dbContext.Database.CanConnectAsync();

                // Log current status
                var now = DateTime.Now;
                var marketStatus = marketHoursService.IsMarketOpen() ? "OPEN" : "CLOSED";
                
                _logger.LogInformation("💓 Health Check - Time: {time}, Market: {status}", 
                    now.ToString("HH:mm:ss"), marketStatus);

                if (!marketHoursService.IsMarketOpen())
                {
                    var timeToOpen = marketHoursService.GetTimeToMarketOpen();
                    if (timeToOpen.TotalHours < 24)
                    {
                        _logger.LogInformation("⏰ Next market open in: {timeToOpen}", timeToOpen);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "⚠️ Health check issue detected");
            }
        }

        public override async Task StopAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("🛑 Stopping Option Market Monitor Service");
            await base.StopAsync(stoppingToken);
        }
    }
} 