using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OptionAnalysisTool.Common.Data;
using OptionAnalysisTool.KiteConnect.Services;
using OptionAnalysisTool.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OptionAnalysisTool.Common.Services
{
    /// <summary>
    /// Comprehensive instrument loader that loads ALL instruments for all expiries for all indices
    /// to the database table during app initialization, then only adds newly added instruments
    /// </summary>
    public class ComprehensiveInstrumentLoader
    {
        private readonly ILogger<ComprehensiveInstrumentLoader> _logger;
        private readonly IKiteConnectService _kiteService;
        private readonly ApplicationDbContext _dbContext;

        // Supported exchanges for index options
        private static readonly Dictionary<string, string> SUPPORTED_EXCHANGES = new Dictionary<string, string>
        {
            { "NFO", "NSE" },  // NIFTY, BANKNIFTY, FINNIFTY, MIDCPNIFTY
            { "BFO", "BSE" }   // SENSEX, BANKEX
        };

        // Supported indices to track
        private static readonly string[] SUPPORTED_INDICES = new string[]
        {
            "NIFTY", "BANKNIFTY", "FINNIFTY", "MIDCPNIFTY", "SENSEX", "BANKEX"
        };

        public ComprehensiveInstrumentLoader(
            ILogger<ComprehensiveInstrumentLoader> logger,
            IKiteConnectService kiteService,
            ApplicationDbContext dbContext)
        {
            _logger = logger;
            _kiteService = kiteService;
            _dbContext = dbContext;
        }

        /// <summary>
        /// Load ALL instruments for all expiries for all indices to database
        /// This should be called once during app initialization after authentication
        /// </summary>
        public async Task<bool> LoadAllInstrumentsAsync()
        {
            try
            {
                _logger.LogInformation("🚀 [INSTRUMENT LOADING] Starting comprehensive instrument loading for ALL indices and expiries...");
                _logger.LogInformation("📊 [INSTRUMENT LOADING] Supported exchanges: {exchanges}", 
                    string.Join(", ", SUPPORTED_EXCHANGES.Select(e => $"{e.Key}({e.Value})")));
                _logger.LogInformation("📋 [INSTRUMENT LOADING] Supported indices: {indices}", 
                    string.Join(", ", SUPPORTED_INDICES));

                var totalLoaded = 0;
                var totalNew = 0;

                foreach (var exchange in SUPPORTED_EXCHANGES)
                {
                    _logger.LogInformation("📊 [INSTRUMENT LOADING] Processing exchange: {exchange} ({exchangeName})", 
                        exchange.Key, exchange.Value);

                    var exchangeResult = await LoadInstrumentsForExchangeAsync(exchange.Key, exchange.Value);
                    totalLoaded += exchangeResult.Total;
                    totalNew += exchangeResult.New;
                }

                _logger.LogInformation("✅ [INSTRUMENT LOADING] Comprehensive instrument loading complete! Total: {total}, New: {new}", 
                    totalLoaded, totalNew);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💥 [INSTRUMENT LOADING] Failed to load all instruments");
                return false;
            }
        }

        /// <summary>
        /// Load instruments for a specific exchange
        /// </summary>
        private async Task<ExchangeLoadResult> LoadInstrumentsForExchangeAsync(string exchange, string exchangeName)
        {
            try
            {
                _logger.LogInformation("🔍 [INSTRUMENT LOADING] Fetching instruments from Kite API for exchange: {exchange}", exchange);

                // Get ALL instruments from Kite API for this exchange
                var kiteInstruments = await _kiteService.GetInstrumentsAsync(exchange);
                
                if (kiteInstruments == null || !kiteInstruments.Any())
                {
                    _logger.LogWarning("⚠️ [INSTRUMENT LOADING] No instruments received from Kite API for exchange: {exchange}", exchange);
                    return new ExchangeLoadResult { Total = 0, New = 0 };
                }

                _logger.LogInformation("📊 [INSTRUMENT LOADING] Received {count} total instruments from Kite API for {exchange}", 
                    kiteInstruments.Count, exchange);

                // 🔥 ENHANCED DEBUG: Log sample instruments to understand the data structure
                var sampleInstruments = kiteInstruments.Take(10).ToList();
                _logger.LogInformation("🔍 [INSTRUMENT LOADING] Sample instruments from {exchange}:", exchange);
                foreach (var inst in sampleInstruments)
                {
                    _logger.LogInformation("   📋 Name: '{name}', TradingSymbol: '{symbol}', Type: '{type}', Expiry: {expiry}, Strike: {strike}",
                        inst.Name ?? "NULL", inst.TradingSymbol ?? "NULL", inst.InstrumentType ?? "NULL", 
                        inst.Expiry?.ToString("yyyy-MM-dd") ?? "NULL", inst.Strike);
                }

                // Filter for index options (CE/PE) for supported indices
                var indexOptions = kiteInstruments
                    .Where(i => (i.InstrumentType == "CE" || i.InstrumentType == "PE") &&
                               SUPPORTED_INDICES.Any(index => 
                                   (i.Name != null && i.Name.Contains(index, StringComparison.OrdinalIgnoreCase)) ||
                                   (i.TradingSymbol != null && i.TradingSymbol.Contains(index, StringComparison.OrdinalIgnoreCase))))
                    .Where(i => i.Expiry.HasValue && i.Expiry.Value >= DateTime.Today) // Only non-expired options
                    .ToList();

                _logger.LogInformation("🔍 [INSTRUMENT LOADING] Filtered {filteredCount} index options from {totalCount} total instruments for {exchange}",
                    indexOptions.Count, kiteInstruments.Count, exchange);

                // 🔥 ENHANCED DEBUG: Log sample filtered instruments
                if (indexOptions.Any())
                {
                    var sampleFiltered = indexOptions.Take(5).ToList();
                    _logger.LogInformation("🔍 [INSTRUMENT LOADING] Sample filtered instruments for {exchange}:", exchange);
                    foreach (var inst in sampleFiltered)
                    {
                        _logger.LogInformation("   📋 Name: '{name}', TradingSymbol: '{symbol}', Type: '{type}', Expiry: {expiry}, Strike: {strike}",
                            inst.Name ?? "NULL", inst.TradingSymbol ?? "NULL", inst.InstrumentType ?? "NULL", 
                            inst.Expiry?.ToString("yyyy-MM-dd") ?? "NULL", inst.Strike);
                    }
                }
                else
                {
                    _logger.LogWarning("⚠️ [INSTRUMENT LOADING] No instruments passed the filter for {exchange}. Checking why...", exchange);
                    
                    // Check how many instruments have CE/PE type
                    var cePeInstruments = kiteInstruments.Where(i => i.InstrumentType == "CE" || i.InstrumentType == "PE").ToList();
                    _logger.LogInformation("🔍 [INSTRUMENT LOADING] {cePeCount} instruments have CE/PE type for {exchange}", cePeInstruments.Count, exchange);
                    
                    // Check how many instruments match each supported index
                    foreach (var index in SUPPORTED_INDICES)
                    {
                        var matchingInstruments = kiteInstruments.Where(i => 
                            (i.Name != null && i.Name.Contains(index, StringComparison.OrdinalIgnoreCase)) ||
                            (i.TradingSymbol != null && i.TradingSymbol.Contains(index, StringComparison.OrdinalIgnoreCase))).ToList();
                        _logger.LogInformation("🔍 [INSTRUMENT LOADING] {count} instruments match '{index}' for {exchange}", matchingInstruments.Count, index, exchange);
                    }
                }

                // Group by index for better logging
                var indexGroups = indexOptions.GroupBy(i => 
                {
                    foreach (var index in SUPPORTED_INDICES)
                    {
                        if ((i.Name != null && i.Name.Contains(index, StringComparison.OrdinalIgnoreCase)) ||
                            (i.TradingSymbol != null && i.TradingSymbol.Contains(index, StringComparison.OrdinalIgnoreCase)))
                        {
                            return index;
                        }
                    }
                    return "UNKNOWN";
                }).OrderBy(g => g.Key);

                foreach (var group in indexGroups)
                {
                    _logger.LogInformation("📋 [INSTRUMENT LOADING] {index}: {count} instruments", group.Key, group.Count());
                    
                    // Group by expiry for better overview
                    var expiryGroups = group.GroupBy(i => i.Expiry?.Date).OrderBy(g => g.Key);
                    foreach (var expiryGroup in expiryGroups)
                    {
                        _logger.LogInformation("   📅 [INSTRUMENT LOADING] {expiry}: {count} instruments", 
                            expiryGroup.Key?.ToString("yyyy-MM-dd") ?? "NULL", expiryGroup.Count());
                    }
                }

                // Convert to database models
                var dbInstruments = indexOptions.Select(i => new Instrument
                {
                    InstrumentToken = i.InstrumentToken,
                    ExchangeToken = i.InstrumentToken, // Using same as instrument token for now
                    TradingSymbol = i.TradingSymbol,
                    Name = i.Name ?? string.Empty,
                    Strike = i.Strike,
                    Expiry = i.Expiry,
                    InstrumentType = i.InstrumentType ?? "CE",
                    Segment = i.Segment ?? string.Empty,
                    Exchange = exchange
                }).ToList();

                // Save to database with duplicate prevention
                var result = await SaveInstrumentsToDatabaseAsync(dbInstruments);

                _logger.LogInformation("💾 [INSTRUMENT LOADING] Saved {total} instruments to database ({new} new) for exchange {exchange}", 
                    result.Total, result.New, exchange);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💥 [INSTRUMENT LOADING] Failed to load instruments for exchange: {exchange}", exchange);
                return new ExchangeLoadResult { Total = 0, New = 0 };
            }
        }

        /// <summary>
        /// Save instruments to database with EXCHANGE-SPECIFIC duplicate prevention
        /// </summary>
        private async Task<ExchangeLoadResult> SaveInstrumentsToDatabaseAsync(List<Instrument> instruments)
        {
            try
            {
                var total = 0;
                var newCount = 0;
                var today = DateTime.Today;

                // 🔥 EXCHANGE-SPECIFIC DUPLICATE PREVENTION: Remove duplicates by InstrumentToken AND Exchange
                var exchange = instruments.FirstOrDefault()?.Exchange ?? "UNKNOWN";
                var instrumentTokens = instruments.Select(i => i.InstrumentToken).ToList();
                
                // 🔥 CRITICAL FIX: Only remove instruments from the SAME exchange to prevent cross-exchange deletion
                var existingInstruments = await _dbContext.Instruments
                    .Where(db => instrumentTokens.Contains(db.InstrumentToken) && db.Exchange == exchange)
                    .ToListAsync();

                if (existingInstruments.Any())
                {
                    _logger.LogInformation("🗑️ Removing {count} existing instruments with duplicate tokens for exchange {exchange} to prevent duplicates", 
                        existingInstruments.Count, exchange);
                    _dbContext.Instruments.RemoveRange(existingInstruments);
                    await _dbContext.SaveChangesAsync();
                }

                // Add CreatedDate to all instruments
                foreach (var instrument in instruments)
                {
                    instrument.CreatedDate = today;
                    instrument.LastUpdated = DateTime.Now;
                }

                // Process in batches to avoid memory issues
                const int batchSize = 1000;
                for (int i = 0; i < instruments.Count; i += batchSize)
                {
                    var batch = instruments.Skip(i).Take(batchSize).ToList();
                    
                    _logger.LogInformation("➕ Adding {count} instruments to database (batch {batch}/{total}) for {date}", 
                        batch.Count, (i / batchSize) + 1, (instruments.Count + batchSize - 1) / batchSize, today.ToString("yyyy-MM-dd"));

                    await _dbContext.Instruments.AddRangeAsync(batch);
                    newCount += batch.Count;
                    total += batch.Count;
                }

                // Save all changes
                await _dbContext.SaveChangesAsync();

                _logger.LogInformation("✅ Successfully saved {total} instruments to database for {date}", 
                    total, today.ToString("yyyy-MM-dd"));

                return new ExchangeLoadResult { Total = total, New = newCount };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💥 Failed to save instruments to database");
                return new ExchangeLoadResult { Total = 0, New = 0 };
            }
        }

        /// <summary>
        /// Get all active instruments from database for a specific index
        /// </summary>
        public async Task<List<Instrument>> GetActiveInstrumentsForIndexAsync(string index)
        {
            try
            {
                // 🔥 FIXED: Use index name matching to get all instruments for the index
                var instruments = await _dbContext.Instruments
                    .Where(i => (i.Name != null && i.Name.Contains(index, StringComparison.OrdinalIgnoreCase)) &&
                               (i.InstrumentType == "CE" || i.InstrumentType == "PE") &&
                               i.Expiry >= DateTime.Today)
                    .OrderBy(i => i.Strike)
                    .ThenBy(i => i.Expiry)
                    .ThenBy(i => i.InstrumentType)
                    .ToListAsync();

                _logger.LogInformation("📋 Retrieved {count} active instruments for {index} from database", 
                    instruments.Count, index);

                // 🔥 ENHANCED LOGGING: Show sample instruments for debugging
                if (instruments.Any())
                {
                    var sampleInstruments = instruments.Take(5).ToList();
                    _logger.LogInformation("📋 Sample instruments for {index}:", index);
                    foreach (var inst in sampleInstruments)
                    {
                        _logger.LogInformation("   {symbol} - {type} - Strike: {strike} - Expiry: {expiry} - Token: {token}",
                            inst.TradingSymbol, inst.InstrumentType, inst.Strike, inst.Expiry?.ToString("yyyy-MM-dd"), inst.InstrumentToken);
                    }
                    
                    // Group by expiry for better overview
                    var expiryGroups = instruments.GroupBy(i => i.Expiry?.Date).OrderBy(g => g.Key);
                    _logger.LogInformation("📊 {index} instruments by expiry:", index);
                    foreach (var group in expiryGroups)
                    {
                        _logger.LogInformation("   {expiry}: {count} instruments", 
                            group.Key?.ToString("yyyy-MM-dd") ?? "NULL", group.Count());
                    }
                }
                else
                {
                    _logger.LogWarning("⚠️ No instruments found for {index} with exact name matching", index);
                }

                return instruments;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💥 Failed to get active instruments for index: {index}", index);
                return new List<Instrument>();
            }
        }

        /// <summary>
        /// Get all active instruments from database for all indices
        /// </summary>
        public async Task<List<Instrument>> GetAllActiveInstrumentsAsync()
        {
            try
            {
                var instruments = await _dbContext.Instruments
                    .Where(i => (i.InstrumentType == "CE" || i.InstrumentType == "PE") &&
                               i.Expiry >= DateTime.Today)
                    .OrderBy(i => i.Name)
                    .ThenBy(i => i.Strike)
                    .ThenBy(i => i.Expiry)
                    .ThenBy(i => i.InstrumentType)
                    .ToListAsync();

                _logger.LogInformation("📋 Retrieved {count} total active instruments from database", instruments.Count);

                return instruments;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💥 Failed to get all active instruments");
                return new List<Instrument>();
            }
        }

        /// <summary>
        /// Check for and add newly added instruments (to be called periodically)
        /// </summary>
        public async Task<int> CheckForNewInstrumentsAsync()
        {
            try
            {
                _logger.LogInformation("🔍 Checking for newly added instruments...");

                var totalNew = 0;

                foreach (var exchange in SUPPORTED_EXCHANGES)
                {
                    var newCount = await CheckForNewInstrumentsInExchangeAsync(exchange.Key, exchange.Value);
                    totalNew += newCount;
                }

                if (totalNew > 0)
                {
                    _logger.LogInformation("✅ Found and added {count} new instruments", totalNew);
                }
                else
                {
                    _logger.LogInformation("ℹ️ No new instruments found");
                }

                return totalNew;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💥 Failed to check for new instruments");
                return 0;
            }
        }

        /// <summary>
        /// Check for new instruments in a specific exchange
        /// </summary>
        private async Task<int> CheckForNewInstrumentsInExchangeAsync(string exchange, string exchangeName)
        {
            try
            {
                // Get current instruments from Kite API
                var kiteInstruments = await _kiteService.GetInstrumentsAsync(exchange);
                
                if (kiteInstruments == null || !kiteInstruments.Any())
                {
                    return 0;
                }

                // Filter for index options
                var indexOptions = kiteInstruments
                    .Where(i => (i.InstrumentType == "CE" || i.InstrumentType == "PE") &&
                               SUPPORTED_INDICES.Any(index => 
                                   (i.Name != null && i.Name.Contains(index, StringComparison.OrdinalIgnoreCase)) ||
                                   (i.TradingSymbol != null && i.TradingSymbol.Contains(index, StringComparison.OrdinalIgnoreCase))))
                    .Where(i => i.Expiry.HasValue && i.Expiry.Value >= DateTime.Today)
                    .ToList();

                // Get existing instrument tokens from database
                var existingTokens = await _dbContext.Instruments
                    .Where(i => i.Exchange == exchange)
                    .Select(i => i.InstrumentToken)
                    .ToListAsync();

                // Find new instruments
                var newInstruments = indexOptions
                    .Where(i => !existingTokens.Contains(i.InstrumentToken))
                    .Select(i => new Instrument
                    {
                        InstrumentToken = i.InstrumentToken,
                        ExchangeToken = i.InstrumentToken,
                        TradingSymbol = i.TradingSymbol,
                        Name = i.Name ?? string.Empty,
                        Strike = i.Strike,
                        Expiry = i.Expiry,
                        InstrumentType = i.InstrumentType ?? "CE",
                        Segment = i.Segment ?? string.Empty,
                        Exchange = exchange
                    })
                    .ToList();

                if (newInstruments.Any())
                {
                    _logger.LogInformation("➕ Found {count} new instruments for exchange {exchange}", 
                        newInstruments.Count, exchange);

                    await _dbContext.Instruments.AddRangeAsync(newInstruments);
                    await _dbContext.SaveChangesAsync();

                    return newInstruments.Count;
                }

                return 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💥 Failed to check for new instruments in exchange: {exchange}", exchange);
                return 0;
            }
        }

        /// <summary>
        /// Get instrument statistics
        /// </summary>
        public async Task<InstrumentStatistics> GetInstrumentStatisticsAsync()
        {
            try
            {
                var totalInstruments = await _dbContext.Instruments.CountAsync();
                var activeInstruments = await _dbContext.Instruments
                    .Where(i => i.Expiry >= DateTime.Today)
                    .CountAsync();

                var indexStats = await _dbContext.Instruments
                    .Where(i => i.Expiry >= DateTime.Today)
                    .GroupBy(i => SUPPORTED_INDICES.FirstOrDefault(index => 
                        i.Name.Contains(index, StringComparison.OrdinalIgnoreCase) ||
                        i.TradingSymbol.Contains(index, StringComparison.OrdinalIgnoreCase)) ?? "OTHER")
                    .Select(g => new { Index = g.Key, Count = g.Count() })
                    .ToListAsync();

                var expiryStats = await _dbContext.Instruments
                    .Where(i => i.Expiry >= DateTime.Today)
                    .GroupBy(i => i.Expiry.HasValue ? i.Expiry.Value.Date : (DateTime?)null)
                    .OrderBy(g => g.Key)
                    .Select(g => new { Expiry = g.Key, Count = g.Count() })
                    .Take(10) // Top 10 expiries
                    .ToListAsync();

                return new InstrumentStatistics
                {
                    TotalInstruments = totalInstruments,
                    ActiveInstruments = activeInstruments,
                    IndexBreakdown = indexStats.ToDictionary(x => x.Index, x => x.Count),
                    ExpiryBreakdown = expiryStats.ToDictionary(x => x.Expiry?.ToString("yyyy-MM-dd") ?? "NULL", x => x.Count)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💥 Failed to get instrument statistics");
                return new InstrumentStatistics();
            }
        }
    }

    /// <summary>
    /// Exchange load result
    /// </summary>
    public class ExchangeLoadResult
    {
        public int Total { get; set; }
        public int New { get; set; }
    }

    /// <summary>
    /// Instrument statistics
    /// </summary>
    public class InstrumentStatistics
    {
        public int TotalInstruments { get; set; }
        public int ActiveInstruments { get; set; }
        public Dictionary<string, int> IndexBreakdown { get; set; } = new Dictionary<string, int>();
        public Dictionary<string, int> ExpiryBreakdown { get; set; } = new Dictionary<string, int>();
    }
} 