using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using OptionAnalysisTool.Common.Data;
using OptionAnalysisTool.Models;
using OptionAnalysisTool.KiteConnect.Services;
using Microsoft.EntityFrameworkCore;

namespace OptionAnalysisTool.Common.Services
{
    /// <summary>
    /// 🔥 CIRCUIT LIMIT TEST SERVICE - REAL KITE DATA TESTING
    /// Tests circuit limit tracking with actual trading strikes only
    /// </summary>
    public class CircuitLimitTestService
    {
        private readonly ApplicationDbContext _context;
        private readonly IKiteConnectService _kiteConnectService;
        private readonly CircuitLimitTrackingService _circuitLimitTrackingService;
        private readonly ILogger<CircuitLimitTestService> _logger;

        public CircuitLimitTestService(
            ApplicationDbContext context,
            IKiteConnectService kiteConnectService,
            CircuitLimitTrackingService circuitLimitTrackingService,
            ILogger<CircuitLimitTestService> logger)
        {
            _context = context;
            _kiteConnectService = kiteConnectService;
            _circuitLimitTrackingService = circuitLimitTrackingService;
            _logger = logger;
        }

        /// <summary>
        /// 🔥 TEST NIFTY CIRCUIT LIMITS - ONLY TRADING STRIKES
        /// </summary>
        public async Task<CircuitLimitTestResult> TestNiftyCircuitLimits()
        {
            var result = new CircuitLimitTestResult
            {
                TestStartTime = DateTime.UtcNow,
                Symbol = "NIFTY",
                Exchange = "NFO"
            };

            try
            {
                _logger.LogInformation("🔥 Starting NIFTY circuit limit test - ONLY TRADING STRIKES");

                // Get NIFTY option instruments
                var instruments = await _kiteConnectService.GetInstrumentsAsync("NFO");
                var niftyOptions = instruments
                    .Where(i => i.TradingSymbol.Contains("NIFTY") &&
                               i.InstrumentType != null &&
                               (i.InstrumentType == "CE" || i.InstrumentType == "PE"))
                    .Take(20) // Test with first 20 strikes
                    .ToList();

                result.TotalInstruments = niftyOptions.Count;
                _logger.LogInformation("Found {count} NIFTY option instruments to test", niftyOptions.Count);

                // Get quotes and filter for actively trading strikes
                var instrumentTokens = niftyOptions.Select(i => i.InstrumentToken.ToString()).ToArray();
                var quotes = await _kiteConnectService.GetQuotesAsync(instrumentTokens);

                foreach (var instrument in niftyOptions)
                {
                    try
                    {
                        var tokenString = instrument.InstrumentToken.ToString();
                        if (!quotes.TryGetValue(tokenString, out var quote))
                        {
                            result.SkippedInstruments++;
                            continue;
                        }

                        // 🔥 CRITICAL CHECK: Only process if strike is actively trading
                        bool isActivelyTrading = IsActivelyTrading(quote);
                        
                        if (!isActivelyTrading)
                        {
                            result.SkippedInstruments++;
                            _logger.LogDebug("Skipping {symbol} - not actively trading (Volume: {volume}, LastPrice: {price})", 
                                instrument.TradingSymbol, quote.Volume, quote.LastPrice);
                            continue;
                        }

                        // Process actively trading strike
                        var circuitLimitData = new ActiveStrikeData
                        {
                            InstrumentToken = tokenString,
                            Symbol = instrument.TradingSymbol,
                            StrikePrice = instrument.Strike,
                            OptionType = instrument.InstrumentType ?? "CE",
                            LastPrice = quote.LastPrice,
                            Volume = quote.Volume,
                            OpenInterest = quote.OpenInterest,
                            LowerCircuitLimit = quote.LowerCircuitLimit,
                            UpperCircuitLimit = quote.UpperCircuitLimit,
                            CircuitLimitStatus = DetermineCircuitStatus(quote),
                            IsActivelyTrading = true,
                            CapturedAt = DateTime.UtcNow
                        };

                        result.ActiveStrikes.Add(circuitLimitData);

                        // Store intraday snapshot for actively trading strike
                        await StoreTestSnapshot(tokenString, instrument.TradingSymbol, quote);

                        // Track circuit limit changes
                        await _circuitLimitTrackingService.TrackCircuitLimitChange(
                            tokenString,
                            instrument.TradingSymbol,
                            "NIFTY",
                            instrument.Strike,
                            instrument.InstrumentType ?? "CE",
                            DateTime.Now.AddDays(7),
                            quote.LowerCircuitLimit,
                            quote.UpperCircuitLimit,
                            quote.LastPrice,
                            25000, // Assumed NIFTY spot
                            quote.Volume,
                            quote.OpenInterest);

                        result.ProcessedInstruments++;

                        _logger.LogInformation("✅ Processed {symbol}: Strike={strike}, Type={type}, Price={price}, Lower={lower}, Upper={upper}, Volume={volume}",
                            instrument.TradingSymbol, instrument.Strike, instrument.InstrumentType, 
                            quote.LastPrice, quote.LowerCircuitLimit, quote.UpperCircuitLimit, quote.Volume);
                    }
                    catch (Exception ex)
                    {
                        result.ErrorCount++;
                        _logger.LogError(ex, "Error processing {symbol}", instrument.TradingSymbol);
                    }
                }

                // Save all changes
                await _context.SaveChangesAsync();

                result.TestEndTime = DateTime.UtcNow;
                result.Duration = result.TestEndTime - result.TestStartTime;
                result.Success = result.ErrorCount == 0;

                _logger.LogInformation("🎯 NIFTY Circuit Limit Test Complete: " +
                    "Total={total}, Processed={processed}, Skipped={skipped}, Errors={errors}, Duration={duration}",
                    result.TotalInstruments, result.ProcessedInstruments, 
                    result.SkippedInstruments, result.ErrorCount, result.Duration);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💥 Critical error in NIFTY circuit limit test");
                result.TestEndTime = DateTime.UtcNow;
                result.Duration = result.TestEndTime - result.TestStartTime;
                result.Success = false;
                result.ErrorCount++;
                return result;
            }
        }

        /// <summary>
        /// 🔥 CRITICAL: Check if strike is actively trading
        /// Only these strikes should have circuit limits stored
        /// </summary>
        private bool IsActivelyTrading(OptionAnalysisTool.KiteConnect.Models.KiteQuote quote)
        {
            // Strike is actively trading if:
            // 1. Has volume > 0 (actual trades happened)
            // 2. Has valid last price > 0
            // 3. Has open interest > 0 (positions exist)
            // 4. Bid/Ask spread exists (market makers present)
            
            bool hasVolume = quote.Volume > 0;
            bool hasValidPrice = quote.LastPrice > 0;
            bool hasOpenInterest = quote.OpenInterest > 0;
            bool hasValidCircuitLimits = quote.LowerCircuitLimit > 0 && quote.UpperCircuitLimit > 0;

            // At minimum, need valid price and circuit limits
            // Volume or OI can be zero for newly listed strikes
            return hasValidPrice && hasValidCircuitLimits && (hasVolume || hasOpenInterest);
        }

        /// <summary>
        /// Store snapshot for active instrument
        /// </summary>
        private async Task StoreTestSnapshot(string instrumentToken, string symbol, OptionAnalysisTool.KiteConnect.Models.KiteQuote quote)
        {
            var snapshot = new IntradayOptionSnapshot
            {
                InstrumentToken = instrumentToken,
                Symbol = symbol,
                UnderlyingSymbol = "NIFTY",
                StrikePrice = 25000,
                OptionType = "CE",
                ExpiryDate = DateTime.Today.AddDays(7),
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
                ValidationMessage = "Test data",
                TradingStatus = "Normal",
                IsValidData = true
            };

            _context.IntradayOptionSnapshots.Add(snapshot);
            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// Store manual test snapshot
        /// </summary>
        private async Task StoreManualTestSnapshot(string symbol, OptionAnalysisTool.KiteConnect.Models.KiteQuote quote)
        {
            var snapshot = new IntradayOptionSnapshot
            {
                InstrumentToken = "TEST_TOKEN",
                Symbol = symbol,
                UnderlyingSymbol = "NIFTY",
                StrikePrice = 25000,
                OptionType = "CE",
                ExpiryDate = DateTime.Today.AddDays(7),
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
                ValidationMessage = "Manual test data",
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

        /// <summary>
        /// Get actively trading strikes summary
        /// </summary>
        public async Task<object> GetActiveTradingSummary(DateTime date)
        {
            var snapshots = await _context.IntradayOptionSnapshots
                .Where(s => s.Timestamp.Date == date.Date && s.Volume > 0)
                .GroupBy(s => s.UnderlyingSymbol)
                .ToListAsync();

            return snapshots.Select(g => new
            {
                Index = g.Key,
                ActiveStrikes = g.Count(),
                TotalVolume = g.Sum(s => s.Volume),
                TotalOpenInterest = g.Sum(s => s.OpenInterest),
                CircuitBreach = g.Count(s => s.CircuitLimitStatus != "Normal"),
                CallStrikes = g.Count(s => s.OptionType == "CE"),
                PutStrikes = g.Count(s => s.OptionType == "PE")
            }).ToList();
        }

        /// <summary>
        /// Get circuit limit changes for actively trading strikes
        /// </summary>
        public async Task<List<CircuitLimitTracker>> GetActiveStrikeCircuitChanges(DateTime date)
        {
            // Get circuit limit changes for strikes that had volume on the date
            var activeTokens = await _context.IntradayOptionSnapshots
                .Where(s => s.Timestamp.Date == date.Date && s.Volume > 0)
                .Select(s => s.InstrumentToken)
                .Distinct()
                .ToListAsync();

            return await _context.CircuitLimitTrackers
                .Where(t => t.DetectedAt.Date == date.Date && activeTokens.Contains(t.InstrumentToken))
                .OrderByDescending(t => t.DetectedAt)
                .ToListAsync();
        }
    }

    // Supporting classes
    public class CircuitLimitTestResult
    {
        public DateTime TestStartTime { get; set; }
        public DateTime TestEndTime { get; set; }
        public TimeSpan Duration { get; set; }
        public string Symbol { get; set; } = string.Empty;
        public string Exchange { get; set; } = string.Empty;
        public int TotalInstruments { get; set; }
        public int ProcessedInstruments { get; set; }
        public int SkippedInstruments { get; set; }
        public int ErrorCount { get; set; }
        public bool Success { get; set; }
        public List<ActiveStrikeData> ActiveStrikes { get; set; } = new();
    }

    public class ActiveStrikeData
    {
        public string InstrumentToken { get; set; } = string.Empty;
        public string Symbol { get; set; } = string.Empty;
        public decimal StrikePrice { get; set; }
        public string OptionType { get; set; } = string.Empty;
        public decimal LastPrice { get; set; }
        public long Volume { get; set; }
        public long OpenInterest { get; set; }
        public decimal LowerCircuitLimit { get; set; }
        public decimal UpperCircuitLimit { get; set; }
        public string CircuitLimitStatus { get; set; } = string.Empty;
        public bool IsActivelyTrading { get; set; }
        public DateTime CapturedAt { get; set; }
    }
} 