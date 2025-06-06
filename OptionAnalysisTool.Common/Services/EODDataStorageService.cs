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
    /// 📊 EOD DATA STORAGE SERVICE
    /// Automatically stores end-of-day option data for ALL INDEX OPTIONS
    /// Circuit limits are applied from last intraday data of the trading day
    /// </summary>
    public class EODDataStorageService
    {
        private readonly ApplicationDbContext _context;
        private readonly IKiteConnectService _kiteConnectService;
        private readonly CircuitLimitTrackingService _circuitLimitTrackingService;
        private readonly ILogger<EODDataStorageService> _logger;

        private readonly string[] SUPPORTED_INDICES = new[]
        {
            "NIFTY", "BANKNIFTY", "FINNIFTY", "MIDCPNIFTY", "SENSEX", "BANKEX"
        };

        public EODDataStorageService(
            ApplicationDbContext context,
            IKiteConnectService kiteConnectService,
            CircuitLimitTrackingService circuitLimitTrackingService,
            ILogger<EODDataStorageService> logger)
        {
            _context = context;
            _kiteConnectService = kiteConnectService;
            _circuitLimitTrackingService = circuitLimitTrackingService;
            _logger = logger;
        }

        /// <summary>
        /// 📊 MAIN EOD COLLECTION METHOD
        /// Collects EOD OHLC data and applies circuit limits from intraday data
        /// </summary>
        public async Task<EODCollectionResult> CollectAndStoreEODData(DateTime? tradingDate = null)
        {
            var workingDate = tradingDate ?? DateTime.Today;
            var result = new EODCollectionResult
            {
                TradingDate = workingDate,
                StartTime = DateTime.UtcNow,
                ProcessedIndices = new List<string>(),
                TotalContractsProcessed = 0,
                TotalErrors = 0,
                SuccessfulSaves = 0
            };

            _logger.LogInformation("🔥 Starting EOD data collection for {tradingDate}", workingDate);

            try
            {
                foreach (var index in SUPPORTED_INDICES)
                {
                    try
                    {
                        _logger.LogInformation("Processing EOD data for index: {index}", index);
                        
                        var indexResult = await ProcessIndexEODData(index, workingDate);
                        
                        result.ProcessedIndices.Add(index);
                        result.TotalContractsProcessed += indexResult.ContractsProcessed;
                        result.SuccessfulSaves += indexResult.SuccessfulSaves;
                        result.TotalErrors += indexResult.Errors;

                        _logger.LogInformation(
                            "✅ Completed {index}: Processed {processed}, Saved {saved}, Errors {errors}",
                            index, indexResult.ContractsProcessed, indexResult.SuccessfulSaves, indexResult.Errors);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "❌ Error processing EOD data for index {index}", index);
                        result.TotalErrors++;
                    }
                }

                // After collecting EOD data, apply circuit limits from intraday data
                await ApplyCircuitLimitsFromIntradayData(workingDate);

                result.EndTime = DateTime.UtcNow;
                result.Duration = result.EndTime - result.StartTime;
                result.IsSuccess = result.TotalErrors == 0;

                _logger.LogInformation(
                    "🎯 EOD Collection Complete: {duration} | Processed {total} contracts | Saved {saved} | Errors {errors}",
                    result.Duration, result.TotalContractsProcessed, result.SuccessfulSaves, result.TotalErrors);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💥 Critical error in EOD data collection");
                result.EndTime = DateTime.UtcNow;
                result.Duration = result.EndTime - result.StartTime;
                result.IsSuccess = false;
                result.TotalErrors++;
                return result;
            }
        }

        /// <summary>
        /// Process EOD data for a specific index - Focus on OHLC data
        /// </summary>
        private async Task<IndexEODResult> ProcessIndexEODData(string index, DateTime tradingDate)
        {
            var result = new IndexEODResult();
            
            try
            {
                // Determine exchange
                string exchange = index == "SENSEX" ? "BSE" : "NFO";
                
                // Get all instruments for the index using the KiteConnect service
                var kiteInstruments = await _kiteConnectService.GetInstrumentsAsync(exchange);
                var optionContracts = kiteInstruments
                    .Where(i => i.TradingSymbol.Contains(index) &&
                               i.InstrumentType != null &&
                               (i.InstrumentType == "CE" || i.InstrumentType == "PE"))
                    .ToList();

                result.ContractsProcessed = optionContracts.Count;
                _logger.LogInformation("Found {count} option contracts for {index}", optionContracts.Count, index);

                // Process in batches to avoid API limits
                const int batchSize = 50;
                for (int i = 0; i < optionContracts.Count; i += batchSize)
                {
                    var batch = optionContracts.Skip(i).Take(batchSize);
                    var batchResult = await ProcessOptionBatch(batch, index, tradingDate);
                    
                    result.SuccessfulSaves += batchResult.SuccessfulSaves;
                    result.Errors += batchResult.Errors;

                    // Small delay between batches to respect API limits
                    await Task.Delay(500);
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing index EOD data for {index}", index);
                result.Errors++;
                return result;
            }
        }

        /// <summary>
        /// Process a batch of option contracts - Focus on OHLC and volume data
        /// </summary>
        private async Task<BatchResult> ProcessOptionBatch(
            IEnumerable<OptionAnalysisTool.KiteConnect.Models.KiteInstrument> batch, 
            string index, 
            DateTime tradingDate)
        {
            var result = new BatchResult();
            
            try
            {
                // Get historical data instead of live quotes for EOD
                foreach (var instrument in batch)
                {
                    try
                    {
                        var instrumentTokenString = instrument.InstrumentToken.ToString();
                        
                        // Get historical data for the trading date
                        var historicalData = await _kiteConnectService.GetHistoricalDataAsync(
                            instrumentTokenString, 
                            tradingDate, 
                            tradingDate, 
                            "day");

                        if (!historicalData.Any())
                        {
                            result.Errors++;
                            continue;
                        }

                        var dayData = historicalData.First();

                        // Create EOD record with OHLC data
                        var eodData = new HistoricalOptionData
                        {
                            InstrumentToken = instrumentTokenString,
                            Symbol = instrument.TradingSymbol,
                            UnderlyingSymbol = index,
                            StrikePrice = instrument.Strike,
                            OptionType = instrument.InstrumentType ?? "CE",
                            ExpiryDate = instrument.Expiry ?? DateTime.Today.AddDays(30),
                            TradingDate = tradingDate,
                            
                            // OHLC data from historical API
                            Open = dayData.Open,
                            High = dayData.High,
                            Low = dayData.Low,
                            Close = dayData.Close,
                            Change = 0, // Will calculate from previous day
                            PercentageChange = 0,
                            Volume = dayData.Volume,
                            OpenInterest = dayData.OI,
                            OIChange = 0, // Will calculate from previous day
                            
                            // Circuit limits will be applied later from intraday data
                            LowerCircuitLimit = 0,
                            UpperCircuitLimit = 0,
                            CircuitLimitChanged = false,
                            
                            // Greeks not available in EOD historical data
                            ImpliedVolatility = 0,
                            Delta = null,
                            Gamma = null,
                            Theta = null,
                            Vega = null,
                            
                            UnderlyingClose = 0, // Will get from index data
                            UnderlyingChange = 0,
                            CapturedAt = DateTime.UtcNow,
                            DataSource = "Kite-Historical",
                            LastUpdated = DateTime.UtcNow
                        };

                        // Calculate change from previous day
                        var previousDay = await GetPreviousEODData(instrumentTokenString, tradingDate);
                        if (previousDay != null)
                        {
                            eodData.Change = eodData.Close - previousDay.Close;
                            eodData.PercentageChange = previousDay.Close != 0 ? 
                                (eodData.Change / previousDay.Close) * 100 : 0;
                            eodData.OIChange = eodData.OpenInterest - previousDay.OpenInterest;
                        }

                        // Check for existing record (prevent duplicates)
                        var existing = await _context.HistoricalOptionData
                            .FirstOrDefaultAsync(h => 
                                h.InstrumentToken == instrumentTokenString && 
                                h.TradingDate.Date == tradingDate.Date);

                        if (existing == null)
                        {
                            _context.HistoricalOptionData.Add(eodData);
                            result.SuccessfulSaves++;
                        }
                        else
                        {
                            // Update existing record
                            existing.Open = eodData.Open;
                            existing.High = eodData.High;
                            existing.Low = eodData.Low;
                            existing.Close = eodData.Close;
                            existing.Change = eodData.Change;
                            existing.PercentageChange = eodData.PercentageChange;
                            existing.Volume = eodData.Volume;
                            existing.OpenInterest = eodData.OpenInterest;
                            existing.OIChange = eodData.OIChange;
                            existing.LastUpdated = DateTime.UtcNow;
                            result.SuccessfulSaves++;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error processing contract {symbol}", instrument.TradingSymbol);
                        result.Errors++;
                    }
                }

                // Save batch to database
                await _context.SaveChangesAsync();
                
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing option batch for {index}", index);
                result.Errors++;
                return result;
            }
        }

        /// <summary>
        /// 🔥 VERY IMPORTANT: Apply circuit limits from last intraday data to EOD records
        /// This is where circuit limits get populated for EOD data
        /// </summary>
        public async Task ApplyCircuitLimitsFromIntradayData(DateTime tradingDate)
        {
            try
            {
                _logger.LogInformation("🔄 Applying circuit limits from intraday data to EOD records for {date}", 
                    tradingDate.ToString("yyyy-MM-dd"));

                var eodRecords = await _context.HistoricalOptionData
                    .Where(h => h.TradingDate.Date == tradingDate.Date && 
                               (h.LowerCircuitLimit == 0 || h.UpperCircuitLimit == 0))
                    .ToListAsync();

                foreach (var eodRecord in eodRecords)
                {
                    var latestIntraday = await _context.IntradayOptionSnapshots
                        .Where(i => i.Symbol == eodRecord.Symbol && 
                                   i.Timestamp.Date == tradingDate.Date)
                        .OrderByDescending(i => i.Timestamp)
                        .FirstOrDefaultAsync();

                    if (latestIntraday != null && latestIntraday.LowerCircuitLimit > 0)
                    {
                        eodRecord.LowerCircuitLimit = latestIntraday.LowerCircuitLimit;
                        eodRecord.UpperCircuitLimit = latestIntraday.UpperCircuitLimit;
                    }
                }

                await _context.SaveChangesAsync();
                _logger.LogInformation("✅ Applied circuit limits to {count} EOD records", eodRecords.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error applying circuit limits from intraday data");
            }
        }

        /// <summary>
        /// Store historical data for a specific date and expiry
        /// </summary>
        public async Task<int> StoreHistoricalDataForDate(string index, DateTime tradingDate, DateTime expiry)
        {
            try
            {
                _logger.LogInformation("📊 Storing historical data for {index} expiry {expiry:yyyy-MM-dd} on {date:yyyy-MM-dd}",
                    index, expiry, tradingDate);

                // Get active instruments for this index and expiry
                var exchange = index.Contains("SENSEX") || index.Contains("BANKEX") ? "BFO" : "NFO";
                var allInstruments = await _kiteConnectService.GetInstrumentsAsync(exchange);
                
                var expiryInstruments = allInstruments
                    .Where(i => i.TradingSymbol.ToUpper().Contains(index) &&
                               (i.InstrumentType == "CE" || i.InstrumentType == "PE") &&
                               i.Expiry.HasValue && i.Expiry.Value.Date == expiry.Date)
                    .ToList();

                int processedCount = 0;

                foreach (var instrument in expiryInstruments)
                {
                    try
                    {
                        // Get historical data for this instrument
                        var historicalData = await _kiteConnectService.GetHistoricalDataAsync(
                            instrument.InstrumentToken.ToString(),
                            tradingDate,
                            tradingDate,
                            "day");

                        if (historicalData.Any())
                        {
                            var dayData = historicalData.First();
                            
                            // Check if already exists
                            var existingRecord = await _context.HistoricalOptionData
                                .FirstOrDefaultAsync(h => h.Symbol == instrument.TradingSymbol && 
                                                        h.TradingDate.Date == tradingDate.Date);

                            if (existingRecord == null)
                            {
                                var eodRecord = new HistoricalOptionData
                                {
                                    InstrumentToken = instrument.InstrumentToken.ToString(),
                                    Symbol = instrument.TradingSymbol,
                                    UnderlyingSymbol = index,
                                    StrikePrice = instrument.Strike,
                                    OptionType = instrument.InstrumentType ?? "CE",
                                    ExpiryDate = instrument.Expiry ?? DateTime.Today.AddDays(30),
                                    TradingDate = tradingDate,
                                    Open = dayData.Open,
                                    High = dayData.High,
                                    Low = dayData.Low,
                                    Close = dayData.Close,
                                    Volume = dayData.Volume,
                                    OpenInterest = dayData.OI,
                                    LowerCircuitLimit = 0, // Will be filled from intraday data
                                    UpperCircuitLimit = 0, // Will be filled from intraday data
                                    CapturedAt = DateTime.UtcNow,
                                    LastUpdated = DateTime.UtcNow
                                };

                                _context.HistoricalOptionData.Add(eodRecord);
                                processedCount++;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error processing historical data for {symbol}", instrument.TradingSymbol);
                    }
                }

                await _context.SaveChangesAsync();
                _logger.LogInformation("✅ Stored {count} EOD records for {index} expiry {expiry:yyyy-MM-dd}",
                    processedCount, index, expiry);

                return processedCount;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error storing historical data for {index} expiry {expiry:yyyy-MM-dd}",
                    index, expiry);
                return 0;
            }
        }

        /// <summary>
        /// Get previous trading day EOD data for calculations
        /// </summary>
        private async Task<HistoricalOptionData?> GetPreviousEODData(string instrumentToken, DateTime currentDate)
        {
            return await _context.HistoricalOptionData
                .Where(h => h.InstrumentToken == instrumentToken && h.TradingDate.Date < currentDate.Date)
                .OrderByDescending(h => h.TradingDate)
                .FirstOrDefaultAsync();
        }

        /// <summary>
        /// Get EOD data collection status
        /// </summary>
        public async Task<List<HistoricalOptionData>> GetEODDataForDate(DateTime tradingDate, string? underlyingSymbol = null)
        {
            var query = _context.HistoricalOptionData
                .Where(h => h.TradingDate.Date == tradingDate.Date);

            if (!string.IsNullOrEmpty(underlyingSymbol))
                query = query.Where(h => h.UnderlyingSymbol == underlyingSymbol);

            return await query
                .OrderBy(h => h.UnderlyingSymbol)
                .ThenBy(h => h.ExpiryDate)
                .ThenBy(h => h.StrikePrice)
                .ThenBy(h => h.OptionType)
                .ToListAsync();
        }

        /// <summary>
        /// Get EOD data with circuit limit statistics
        /// </summary>
        public async Task<object> GetEODCircuitLimitSummary(DateTime tradingDate)
        {
            var eodData = await _context.HistoricalOptionData
                .Where(h => h.TradingDate.Date == tradingDate.Date)
                .ToListAsync();

            return new
            {
                TradingDate = tradingDate,
                TotalContracts = eodData.Count,
                ContractsWithCircuitLimits = eodData.Count(e => e.LowerCircuitLimit > 0 || e.UpperCircuitLimit > 0),
                ContractsWithChangedLimits = eodData.Count(e => e.CircuitLimitChanged),
                IndicesProcessed = eodData.GroupBy(e => e.UnderlyingSymbol).Select(g => g.Key).ToList(),
                ContractsByIndex = eodData.GroupBy(e => e.UnderlyingSymbol)
                    .Select(g => new { Index = g.Key, Count = g.Count() })
                    .ToList()
            };
        }
    }

    // Supporting classes for results
    public class EODCollectionResult
    {
        public DateTime TradingDate { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public TimeSpan Duration { get; set; }
        public List<string> ProcessedIndices { get; set; } = new();
        public int TotalContractsProcessed { get; set; }
        public int SuccessfulSaves { get; set; }
        public int TotalErrors { get; set; }
        public bool IsSuccess { get; set; }
    }

    public class IndexEODResult
    {
        public int ContractsProcessed { get; set; }
        public int SuccessfulSaves { get; set; }
        public int Errors { get; set; }
    }

    public class BatchResult
    {
        public int SuccessfulSaves { get; set; }
        public int Errors { get; set; }
    }
} 