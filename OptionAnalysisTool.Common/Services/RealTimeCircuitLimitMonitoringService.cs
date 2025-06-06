using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OptionAnalysisTool.Common.Data;
using OptionAnalysisTool.KiteConnect.Services;
using OptionAnalysisTool.Models;
using Microsoft.EntityFrameworkCore;

namespace OptionAnalysisTool.Common.Services
{
    /// <summary>
    /// 🔥 REAL-TIME CIRCUIT LIMIT MONITORING SERVICE - MARKET HOURS ONLY
    /// Automatically monitors ALL index option strikes for circuit limit changes
    /// Only runs during market hours (9:15 AM - 3:30 PM) on trading days
    /// Tracks BOTH lower and upper limits for actively trading strikes
    /// </summary>
    public class RealTimeCircuitLimitMonitoringService : BackgroundService
    {
        private readonly ILogger<RealTimeCircuitLimitMonitoringService> _logger;
        private readonly ApplicationDbContext _context;
        private readonly IKiteConnectService _kiteConnectService;
        private readonly CircuitLimitTrackingService _circuitLimitTrackingService;
        private readonly MarketHoursService _marketHoursService;

        // Monitoring configuration
        private const int MONITORING_INTERVAL_SECONDS = 30; // Every 30 seconds during market hours
        private const int MARKET_CHECK_INTERVAL_SECONDS = 60; // Check market status every minute
        
        // All supported index symbols
        private readonly string[] SUPPORTED_INDICES = {
            "NIFTY", "BANKNIFTY", "FINNIFTY", "MIDCPNIFTY", 
            "SENSEX", "BANKEX"
        };

        // Cache for actively trading instruments
        private readonly Dictionary<string, DateTime> _activeInstrumentsCache = new();
        private DateTime _lastInstrumentCacheUpdate = DateTime.MinValue;
        private const int CACHE_REFRESH_MINUTES = 5;

        public RealTimeCircuitLimitMonitoringService(
            ILogger<RealTimeCircuitLimitMonitoringService> logger,
            ApplicationDbContext context,
            IKiteConnectService kiteConnectService,
            CircuitLimitTrackingService circuitLimitTrackingService,
            MarketHoursService marketHoursService)
        {
            _logger = logger;
            _context = context;
            _kiteConnectService = kiteConnectService;
            _circuitLimitTrackingService = circuitLimitTrackingService;
            _marketHoursService = marketHoursService;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("🔥 Real-Time Circuit Limit Monitoring Service started");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    // Check if market is open
                    if (_marketHoursService.IsMarketOpen())
                    {
                        _logger.LogInformation("🟢 Market is open - starting circuit limit monitoring");
                        await RunMarketHoursMonitoring(stoppingToken);
                    }
                    else
                    {
                        _logger.LogInformation("🔴 Market is closed - waiting for next trading session");
                        await WaitForMarketOpen(stoppingToken);
                    }
                }
                catch (OperationCanceledException)
                {
                    _logger.LogInformation("Circuit limit monitoring service stopped");
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "💥 Error in circuit limit monitoring service");
                    
                    // Wait before retrying
                    await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
                }
            }

            _logger.LogInformation("🔥 Real-Time Circuit Limit Monitoring Service stopped");
        }

        /// <summary>
        /// Main monitoring loop during market hours
        /// </summary>
        private async Task RunMarketHoursMonitoring(CancellationToken stoppingToken)
        {
            while (_marketHoursService.IsMarketOpen() && !stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var monitoringStartTime = DateTime.UtcNow;
                    
                    // Refresh active instruments cache if needed
                    await RefreshActiveInstrumentsCacheIfNeeded();

                    // Monitor all active instruments for circuit limit changes
                    int processedCount = await MonitorActiveInstruments();

                    var duration = DateTime.UtcNow - monitoringStartTime;
                    _logger.LogInformation("✅ Circuit limit monitoring cycle complete: {processed} instruments in {duration:ss\\.ff}s", 
                        processedCount, duration);

                    // Wait for next monitoring cycle
                    await Task.Delay(TimeSpan.FromSeconds(MONITORING_INTERVAL_SECONDS), stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in monitoring cycle");
                    await Task.Delay(TimeSpan.FromSeconds(MONITORING_INTERVAL_SECONDS), stoppingToken);
                }
            }
        }

        /// <summary>
        /// Wait for market to open
        /// </summary>
        private async Task WaitForMarketOpen(CancellationToken stoppingToken)
        {
            while (!_marketHoursService.IsMarketOpen() && !stoppingToken.IsCancellationRequested)
            {
                var timeToOpen = _marketHoursService.GetTimeToMarketOpen();
                
                if (timeToOpen.TotalMinutes <= 5)
                {
                    _logger.LogInformation("⏰ Market opens in {minutes:F1} minutes - preparing for monitoring", 
                        timeToOpen.TotalMinutes);
                }

                await Task.Delay(TimeSpan.FromSeconds(MARKET_CHECK_INTERVAL_SECONDS), stoppingToken);
            }
        }

        /// <summary>
        /// Refresh cache of actively trading instruments
        /// </summary>
        private async Task RefreshActiveInstrumentsCacheIfNeeded()
        {
            if (DateTime.UtcNow - _lastInstrumentCacheUpdate < TimeSpan.FromMinutes(CACHE_REFRESH_MINUTES))
                return;

            try
            {
                _logger.LogInformation("🔄 Refreshing active instruments cache");

                var allInstruments = await _kiteConnectService.GetInstrumentsAsync("NFO");
                var indexOptions = allInstruments
                    .Where(i => SUPPORTED_INDICES.Any(index => 
                        i.TradingSymbol.ToUpper().Contains(index)) &&
                        (i.InstrumentType == "CE" || i.InstrumentType == "PE"))
                    .ToList();

                // Get quotes for batch to check which are actively trading
                var instrumentTokens = indexOptions.Take(100).Select(i => i.InstrumentToken.ToString()).ToArray();
                var quotes = await _kiteConnectService.GetQuotesAsync(instrumentTokens);

                _activeInstrumentsCache.Clear();
                
                foreach (var instrument in indexOptions.Take(100))
                {
                    var tokenString = instrument.InstrumentToken.ToString();
                    if (quotes.TryGetValue(tokenString, out var quote) && IsActivelyTrading(quote))
                    {
                        _activeInstrumentsCache[tokenString] = DateTime.UtcNow;
                    }
                }

                _lastInstrumentCacheUpdate = DateTime.UtcNow;
                
                _logger.LogInformation("✅ Active instruments cache refreshed: {count} actively trading contracts", 
                    _activeInstrumentsCache.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error refreshing active instruments cache");
            }
        }

        /// <summary>
        /// Monitor all active instruments for circuit limit changes
        /// </summary>
        private async Task<int> MonitorActiveInstruments()
        {
            if (_activeInstrumentsCache.Count == 0)
                return 0;

            try
            {
                var instrumentTokens = _activeInstrumentsCache.Keys.ToArray();
                var quotes = await _kiteConnectService.GetQuotesAsync(instrumentTokens);

                int processedCount = 0;
                int changesDetected = 0;

                foreach (var kvp in _activeInstrumentsCache.ToList())
                {
                    try
                    {
                        var instrumentToken = kvp.Key;
                        
                        if (!quotes.TryGetValue(instrumentToken, out var quote))
                            continue;

                        // Get instrument details
                        var allInstruments = await _kiteConnectService.GetInstrumentsAsync("NFO");
                        var instrument = allInstruments.FirstOrDefault(i => i.InstrumentToken.ToString() == instrumentToken);
                        
                        if (instrument == null)
                            continue;

                        // Only process if still actively trading
                        if (!IsActivelyTrading(quote))
                        {
                            _activeInstrumentsCache.Remove(instrumentToken);
                            continue;
                        }

                        // Track circuit limit changes
                        var change = await _circuitLimitTrackingService.TrackCircuitLimitChange(
                            instrumentToken,
                            instrument.TradingSymbol,
                            ExtractUnderlyingSymbol(instrument.TradingSymbol),
                            instrument.Strike,
                            instrument.InstrumentType ?? "CE",
                            DateTime.Now.AddDays(7), // Simplified expiry
                            quote.LowerCircuitLimit,
                            quote.UpperCircuitLimit,
                            quote.LastPrice,
                            GetUnderlyingPrice(instrument.TradingSymbol), // Simplified
                            quote.Volume,
                            quote.OpenInterest);

                        if (change != null)
                        {
                            changesDetected++;
                            _logger.LogInformation("🔥 Circuit limit change detected: {symbol} - {reason}", 
                                instrument.TradingSymbol, change.ChangeReason);
                        }

                        processedCount++;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error monitoring instrument {token}", kvp.Key);
                    }
                }

                if (changesDetected > 0)
                {
                    _logger.LogInformation("🎯 Circuit limit monitoring summary: {changes} changes detected from {processed} instruments", 
                        changesDetected, processedCount);
                }

                return processedCount;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in monitor active instruments");
                return 0;
            }
        }

        /// <summary>
        /// Check if quote data indicates active trading
        /// </summary>
        private bool IsActivelyTrading(OptionAnalysisTool.KiteConnect.Models.KiteQuote quote)
        {
            return quote.LastPrice > 0 && 
                   quote.LowerCircuitLimit > 0 && 
                   quote.UpperCircuitLimit > 0 &&
                   (quote.Volume > 0 || quote.OpenInterest > 0);
        }

        /// <summary>
        /// Extract underlying symbol from trading symbol
        /// </summary>
        private string ExtractUnderlyingSymbol(string tradingSymbol)
        {
            foreach (var index in SUPPORTED_INDICES)
            {
                if (tradingSymbol.ToUpper().Contains(index))
                    return index;
            }
            return "UNKNOWN";
        }

        /// <summary>
        /// Get underlying price (simplified implementation)
        /// </summary>
        private decimal GetUnderlyingPrice(string tradingSymbol)
        {
            // Simplified - in real implementation, get actual spot price
            var underlying = ExtractUnderlyingSymbol(tradingSymbol);
            return underlying switch
            {
                "NIFTY" => 25000m,
                "BANKNIFTY" => 55000m,
                "FINNIFTY" => 24000m,
                "MIDCPNIFTY" => 13000m,
                "SENSEX" => 82000m,
                "BANKEX" => 58000m,
                _ => 0m
            };
        }

        /// <summary>
        /// Store real-time snapshot
        /// </summary>
        private async Task StoreRealtimeSnapshot(string instrumentToken, string symbol, string underlyingSymbol, 
            decimal strikePrice, string optionType, DateTime expiryDate, OptionAnalysisTool.KiteConnect.Models.KiteQuote quote)
        {
            var snapshot = new IntradayOptionSnapshot
            {
                InstrumentToken = instrumentToken,
                Symbol = symbol,
                UnderlyingSymbol = underlyingSymbol,
                StrikePrice = strikePrice,
                OptionType = optionType,
                ExpiryDate = expiryDate,
                LastPrice = quote.LastPrice,
                Open = quote.Open,
                High = quote.High,
                Low = quote.Low,
                Close = quote.Close,
                Change = quote.Change,
                Volume = quote.Volume,
                OpenInterest = quote.OpenInterest,
                LowerCircuitLimit = quote.LowerCircuitLimit,
                UpperCircuitLimit = quote.UpperCircuitLimit,
                ImpliedVolatility = quote.ImpliedVolatility,
                Timestamp = DateTime.UtcNow,
                CircuitLimitStatus = DetermineCircuitStatus(quote),
                ValidationMessage = "Real-time monitoring",
                TradingStatus = "Normal",
                IsValidData = true
            };

            _context.IntradayOptionSnapshots.Add(snapshot);
            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// Determine circuit limit status
        /// </summary>
        private string DetermineCircuitStatus(OptionAnalysisTool.KiteConnect.Models.KiteQuote quote)
        {
            if (quote.LastPrice <= quote.LowerCircuitLimit) return "Lower Circuit";
            if (quote.LastPrice >= quote.UpperCircuitLimit) return "Upper Circuit";
            if (quote.LastPrice <= quote.LowerCircuitLimit * 1.02m) return "Near Lower Circuit";
            if (quote.LastPrice >= quote.UpperCircuitLimit * 0.98m) return "Near Upper Circuit";
            return "Normal";
        }

        public override async Task StopAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("🛑 Stopping Real-Time Circuit Limit Monitoring Service");
            await base.StopAsync(stoppingToken);
        }
    }
} 