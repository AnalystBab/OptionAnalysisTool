using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using OptionAnalysisTool.Common.Data;
using OptionAnalysisTool.KiteConnect.Services;
using OptionAnalysisTool.Models;

namespace OptionAnalysisTool.Common.Services
{
    /// <summary>
    /// Automated health check for data collection correctness and freshness.
    /// Compares latest DB snapshot with Kite API for each symbol and logs mismatches.
    /// </summary>
    public class DataCollectionHealthCheckService : BackgroundService
    {
        private readonly ILogger<DataCollectionHealthCheckService> _logger;
        private readonly ApplicationDbContext _dbContext;
        private readonly IKiteConnectService _kiteConnectService;
        private readonly TimeSpan _checkInterval = TimeSpan.FromMinutes(5);
        private readonly string[] _symbols = new[] { "NIFTY", "BANKNIFTY", "FINNIFTY", "MIDCPNIFTY", "SENSEX", "BANKEX" };

        public DataCollectionHealthCheckService(
            ILogger<DataCollectionHealthCheckService> logger,
            ApplicationDbContext dbContext,
            IKiteConnectService kiteConnectService)
        {
            _logger = logger;
            _dbContext = dbContext;
            _kiteConnectService = kiteConnectService;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("🚦 Data Collection Health Check Service started");
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await RunHealthCheckAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in health check loop");
                }
                await Task.Delay(_checkInterval, stoppingToken);
            }
        }

        private async Task RunHealthCheckAsync()
        {
            foreach (var symbol in _symbols)
            {
                try
                {
                    // Get latest snapshot for this symbol
                    var latestSnapshot = await _dbContext.IntradayOptionSnapshots
                        .Where(s => s.UnderlyingSymbol == symbol)
                        .OrderByDescending(s => s.Timestamp)
                        .FirstOrDefaultAsync();

                    if (latestSnapshot == null)
                    {
                        _logger.LogWarning($"[HealthCheck] No snapshot found for {symbol}");
                        continue;
                    }

                    // Get latest quote from Kite
                    var kiteQuote = await _kiteConnectService.GetQuoteAsync(latestSnapshot.InstrumentToken);
                    if (kiteQuote == null)
                    {
                        _logger.LogWarning($"[HealthCheck] No quote from Kite for {symbol} (token {latestSnapshot.InstrumentToken})");
                        continue;
                    }

                    // Compare fields
                    var mismatches = new List<string>();
                    if (latestSnapshot.LastPrice != kiteQuote.LastPrice)
                        mismatches.Add($"LastPrice: DB={latestSnapshot.LastPrice}, API={kiteQuote.LastPrice}");
                    if (latestSnapshot.Volume != kiteQuote.Volume)
                        mismatches.Add($"Volume: DB={latestSnapshot.Volume}, API={kiteQuote.Volume}");
                    if (latestSnapshot.LowerCircuitLimit != kiteQuote.LowerCircuitLimit)
                        mismatches.Add($"LowerCircuitLimit: DB={latestSnapshot.LowerCircuitLimit}, API={kiteQuote.LowerCircuitLimit}");
                    if (latestSnapshot.UpperCircuitLimit != kiteQuote.UpperCircuitLimit)
                        mismatches.Add($"UpperCircuitLimit: DB={latestSnapshot.UpperCircuitLimit}, API={kiteQuote.UpperCircuitLimit}");
                    if (latestSnapshot.OpenInterest != kiteQuote.OpenInterest)
                        mismatches.Add($"OpenInterest: DB={latestSnapshot.OpenInterest}, API={kiteQuote.OpenInterest}");
                    if (latestSnapshot.Open != kiteQuote.Open)
                        mismatches.Add($"Open: DB={latestSnapshot.Open}, API={kiteQuote.Open}");
                    if (latestSnapshot.High != kiteQuote.High)
                        mismatches.Add($"High: DB={latestSnapshot.High}, API={kiteQuote.High}");
                    if (latestSnapshot.Low != kiteQuote.Low)
                        mismatches.Add($"Low: DB={latestSnapshot.Low}, API={kiteQuote.Low}");
                    if (latestSnapshot.Close != kiteQuote.Close)
                        mismatches.Add($"Close: DB={latestSnapshot.Close}, API={kiteQuote.Close}");
                    if (latestSnapshot.ImpliedVolatility != kiteQuote.ImpliedVolatility)
                        mismatches.Add($"IV: DB={latestSnapshot.ImpliedVolatility}, API={kiteQuote.ImpliedVolatility}");

                    // Check staleness
                    var minutesOld = (DateTime.UtcNow - latestSnapshot.Timestamp).TotalMinutes;
                    if (minutesOld > 2)
                        mismatches.Add($"Stale: Last collected {minutesOld:F1} minutes ago");

                    if (mismatches.Count > 0)
                    {
                        _logger.LogWarning($"[HealthCheck] Data mismatch for {symbol}: {string.Join("; ", mismatches)}");
                    }
                    else
                    {
                        _logger.LogInformation($"[HealthCheck] {symbol}: Data OK. Last collected {latestSnapshot.Timestamp:yyyy-MM-dd HH:mm:ss}");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"[HealthCheck] Error checking {symbol}");
                }
            }
        }
    }
} 