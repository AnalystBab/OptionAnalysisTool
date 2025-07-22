using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using OptionAnalysisTool.Common.Data;
using OptionAnalysisTool.KiteConnect.Services;
using OptionAnalysisTool.Models;

namespace OptionAnalysisTool.Common.Services
{
    /// <summary>
    /// 📈 SPOT DATA COLLECTION SERVICE
    /// 
    /// Collects real-time spot prices for all supported indices:
    /// - NIFTY 50 (NSE)
    /// - NIFTY BANK (NSE) 
    /// - NIFTY FIN SERVICE (NSE)
    /// - NIFTY MID SELECT (NSE)
    /// - BSE SENSEX (BSE)
    /// - BSE BANKEX (BSE)
    /// </summary>
    public class SpotDataCollectionService : BackgroundService
    {
        private readonly ILogger<SpotDataCollectionService> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly IConfiguration _configuration;

        // Spot index symbols and their corresponding Kite tokens
        private readonly Dictionary<string, SpotIndexConfig> SPOT_INDICES = new()
        {
            { "NIFTY", new SpotIndexConfig { Symbol = "NIFTY 50", Exchange = "NSE", Token = "256265" } },
            { "BANKNIFTY", new SpotIndexConfig { Symbol = "NIFTY BANK", Exchange = "NSE", Token = "260105" } },
            { "FINNIFTY", new SpotIndexConfig { Symbol = "NIFTY FIN SERVICE", Exchange = "NSE", Token = "257801" } },
            { "MIDCPNIFTY", new SpotIndexConfig { Symbol = "NIFTY MID SELECT", Exchange = "NSE", Token = "288009" } },
            { "SENSEX", new SpotIndexConfig { Symbol = "SENSEX", Exchange = "BSE", Token = "265" } },
            { "BANKEX", new SpotIndexConfig { Symbol = "BANKEX", Exchange = "BSE", Token = "274441" } }
        };

        private const int COLLECTION_INTERVAL_SECONDS = 30; // Collect every 30 seconds
        private bool _isAuthenticated = false;

        public SpotDataCollectionService(
            ILogger<SpotDataCollectionService> logger,
            IServiceProvider serviceProvider,
            IConfiguration configuration)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
            _configuration = configuration;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("🚀 Starting Spot Data Collection Service");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await CollectSpotDataAsync();
                    await Task.Delay(TimeSpan.FromSeconds(COLLECTION_INTERVAL_SECONDS), stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "💥 Error in spot data collection cycle");
                    await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
                }
            }
        }

        private async Task CollectSpotDataAsync()
        {
            using var scope = _serviceProvider.CreateScope();
            
            try
            {
                _logger.LogInformation("🔄 [SPOT DATA] Starting spot data collection cycle...");
                
                var kiteService = scope.ServiceProvider.GetRequiredService<IKiteConnectService>();
                var databaseTokenService = scope.ServiceProvider.GetRequiredService<DatabaseTokenService>();
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                // Check authentication
                if (!_isAuthenticated)
                {
                    _logger.LogInformation("🔐 [SPOT DATA] Checking authentication...");
                    var accessToken = await databaseTokenService.GetCurrentAccessTokenAsync(_configuration["KiteConnect:ApiKey"]);
                    if (!string.IsNullOrEmpty(accessToken))
                    {
                        _logger.LogInformation("🔐 [SPOT DATA] Found access token, setting in Kite service...");
                        var setResult = await kiteService.SetAccessToken(accessToken);
                        if (setResult)
                        {
                            _logger.LogInformation("✅ [SPOT DATA] Authentication successful");
                            _isAuthenticated = true;
                        }
                        else
                        {
                            _logger.LogWarning("⚠️ [SPOT DATA] Failed to set access token in Kite service");
                            return;
                        }
                    }
                    else
                    {
                        _logger.LogWarning("⚠️ [SPOT DATA] No access token available - please authenticate via Settings");
                        return;
                    }
                }

                _logger.LogInformation("📈 [SPOT DATA] Collecting spot data for {count} indices...", SPOT_INDICES.Count);

                foreach (var indexConfig in SPOT_INDICES)
                {
                    var indexName = indexConfig.Key;
                    var config = indexConfig.Value;

                    try
                    {
                        _logger.LogInformation("📊 [SPOT DATA] Fetching quote for {index} (Token: {token})", indexName, config.Token);
                        
                        // Get quote for the spot index
                        var quote = await kiteService.GetQuoteAsync(config.Token);
                        
                        if (quote != null && quote.LastPrice > 0)
                        {
                            var percentageChange = quote.Close > 0 ? (quote.Change / quote.Close) * 100 : 0;
                            
                            _logger.LogInformation("📊 [SPOT DATA] {index}: {price} ({change:+#,##0.00;-#,##0.00}) ({percent:+#,##0.00;-#,##0.00}%)", 
                                indexName, quote.LastPrice, quote.Change, percentageChange);

                            // Create or update spot data record
                            var spotData = new SpotData
                            {
                                Symbol = indexName,
                                Exchange = config.Exchange,
                                LastPrice = quote.LastPrice,
                                Change = quote.Change,
                                PercentageChange = percentageChange,
                                Open = quote.Open,
                                High = quote.High,
                                Low = quote.Low,
                                Close = quote.Close,
                                Volume = quote.Volume,
                                LowerCircuitLimit = quote.LowerCircuitLimit,
                                UpperCircuitLimit = quote.UpperCircuitLimit,
                                CircuitStatus = DetermineCircuitStatus(quote),
                                Timestamp = DateTime.Now,
                                LastUpdated = DateTime.Now,
                                CapturedAt = DateTime.Now,
                                IsValidData = true,
                                ValidationMessage = "Real-time data from Kite API"
                            };

                            // Check if we already have recent data for this index
                            var existingData = await context.SpotData
                                .Where(s => s.Symbol == indexName && s.IsValidData)
                                .OrderByDescending(s => s.Timestamp)
                                .FirstOrDefaultAsync();

                            if (existingData == null || 
                                Math.Abs((existingData.LastPrice - quote.LastPrice) / existingData.LastPrice) > 0.001m) // 0.1% change threshold
                            {
                                // Add new record
                                context.SpotData.Add(spotData);
                                await context.SaveChangesAsync();
                                
                                _logger.LogInformation("✅ [SPOT DATA] Updated spot data for {index}: {price} ({change:+#,##0.00;-#,##0.00})", 
                                    indexName, quote.LastPrice, quote.Change);
                            }
                            else
                            {
                                _logger.LogDebug("⏸️ [SPOT DATA] No significant change for {index}, skipping update", indexName);
                            }
                        }
                        else
                        {
                            _logger.LogWarning("⚠️ [SPOT DATA] No valid quote received for {index} (Token: {token})", indexName, config.Token);
                            if (quote == null)
                            {
                                _logger.LogWarning("⚠️ [SPOT DATA] Quote is null for {index}", indexName);
                            }
                            else
                            {
                                _logger.LogWarning("⚠️ [SPOT DATA] Quote has invalid price for {index}: {price}", indexName, quote.LastPrice);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "💥 [SPOT DATA] Error collecting spot data for {index} (Token: {token})", indexName, config.Token);
                    }
                }
                
                _logger.LogInformation("✅ [SPOT DATA] Spot data collection cycle completed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💥 [SPOT DATA] Error in spot data collection");
            }
        }

        private string DetermineCircuitStatus(dynamic quote)
        {
            if (quote.LastPrice <= quote.LowerCircuitLimit)
                return "Lower Circuit";
            else if (quote.LastPrice >= quote.UpperCircuitLimit)
                return "Upper Circuit";
            else
                return "Normal";
        }

        private class SpotIndexConfig
        {
            public string Symbol { get; set; } = string.Empty;
            public string Exchange { get; set; } = string.Empty;
            public string Token { get; set; } = string.Empty;
        }
    }
} 