using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Concurrent;
using System.Threading;
using Microsoft.Extensions.Logging;
using OptionAnalysisTool.Common.Repositories;
using OptionAnalysisTool.KiteConnect.Services;
using OptionAnalysisTool.Models;
using KiteInstrument = OptionAnalysisTool.Models.KiteConnect.Instrument;
using KiteQuote = OptionAnalysisTool.Models.KiteConnect.Quote;
using KiteHistorical = OptionAnalysisTool.Models.KiteConnect.Historical;
using DomainInstrument = OptionAnalysisTool.Models.Instrument;
using DomainQuote = OptionAnalysisTool.Models.Quote;
using DomainHistorical = OptionAnalysisTool.Models.Historical;
using OptionAnalysisTool.Models.KiteConnect;
using OptionAnalysisTool.KiteConnect.Models;

namespace OptionAnalysisTool.Common.Services
{
    public class MarketDataService : IMarketDataService
    {
        private readonly ILogger<MarketDataService> _logger;
        private readonly IMarketDataRepository _marketDataRepository;
        private readonly IKiteConnectService _kiteConnectService;
        private readonly IntradayDataService _intradayDataService;
        private readonly ConcurrentDictionary<string, Timer> _monitoringTimers = new();
        private readonly ConcurrentDictionary<string, List<MonitoredOptionContract>> _monitoredContracts = new();
        private const int MONITORING_INTERVAL_MS = 1000; // 1 second

        private readonly string[] SUPPORTED_INDICES = new[]
        {
            "NIFTY", "BANKNIFTY", "FINNIFTY", "MIDCPNIFTY", "SENSEX", "BANKEX"
        };

        private class MonitoredOptionContract
        {
            public required string InstrumentToken { get; set; }
            public required string TradingSymbol { get; set; }
            public DateTime ExpiryDate { get; set; }
            public decimal StrikePrice { get; set; }
            public required string OptionType { get; set; }  // CE or PE
            public required string UnderlyingSymbol { get; set; }
        }

        public MarketDataService(
            ILogger<MarketDataService> logger,
            IMarketDataRepository marketDataRepository,
            IKiteConnectService kiteConnectService,
            IntradayDataService intradayDataService)
        {
            _logger = logger;
            _marketDataRepository = marketDataRepository;
            _kiteConnectService = kiteConnectService;
            _intradayDataService = intradayDataService;
        }

        public async Task DownloadHistoricalDataAsync(string symbol, DateTime startDate, DateTime endDate, IProgress<(int value, string message)>? progress = null)
        {
            _logger.LogInformation("Downloading historical data for {symbol} from {startDate} to {endDate}", symbol, startDate, endDate);
            // Implementation placeholder
            await Task.CompletedTask;
        }

        public async Task DownloadOptionDataAsync(string symbol, DateTime startDate, DateTime endDate, IProgress<(int value, string message)>? progress = null)
        {
            _logger.LogInformation("Downloading option data for {symbol} from {startDate} to {endDate}", symbol, startDate, endDate);
            // Implementation placeholder
            await Task.CompletedTask;
        }

        public async Task StartLiveDataMonitoringAsync(string symbol)
        {
            try
            {
                if (!SUPPORTED_INDICES.Contains(symbol))
                {
                    throw new ArgumentException($"Unsupported index: {symbol}. Supported indices are: {string.Join(", ", SUPPORTED_INDICES)}");
                }

                _logger.LogInformation("Starting live data monitoring for index {symbol}", symbol);

                // Determine exchange based on symbol
                string exchange = symbol == "SENSEX" ? "BSE" : "NFO";
                _logger.LogInformation("Using exchange {exchange} for {symbol}", exchange, symbol);

                // Get all option contracts for the index
                var allInstruments = await _kiteConnectService.GetInstrumentsAsync(exchange);
                var optionContracts = allInstruments
                    .Where(i => i.TradingSymbol.Contains(symbol) &&
                               i.InstrumentType != null &&
                               (i.InstrumentType == "CE" || i.InstrumentType == "PE"))
                    .Select(i => new MonitoredOptionContract
                    {
                        InstrumentToken = i.InstrumentToken,
                        TradingSymbol = i.TradingSymbol,
                        ExpiryDate = DateTime.Now.AddDays(30), // Simplified
                        StrikePrice = i.Strike,
                        OptionType = i.InstrumentType,
                        UnderlyingSymbol = i.Name
                    })
                    .Take(100) // Limit for testing
                    .ToList();

                if (!optionContracts.Any())
                {
                    _logger.LogWarning("No option contracts found for {symbol} on {exchange}", symbol, exchange);
                    return;
                }

                // Store contracts for monitoring
                _monitoredContracts.AddOrUpdate(symbol, optionContracts, (_, __) => optionContracts);

                // Start monitoring timer
                var timer = new Timer(async _ => await MonitorContractsAsync(symbol),
                    null, 0, MONITORING_INTERVAL_MS);
                
                _monitoringTimers.AddOrUpdate(symbol, timer, (_, oldTimer) =>
                {
                    oldTimer.Dispose();
                    return timer;
                });

                _logger.LogInformation("Successfully started monitoring {count} option contracts for {symbol}",
                    optionContracts.Count, symbol);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting live data monitoring for {symbol}", symbol);
                throw;
            }
        }

        public async Task StopLiveDataMonitoringAsync(string symbol)
        {
            try
            {
                _logger.LogInformation("Stopping live data monitoring for {symbol}", symbol);

                // Remove and dispose timer
                if (_monitoringTimers.TryRemove(symbol, out var timer))
                {
                    timer.Dispose();
                }

                // Remove monitored contracts
                _monitoredContracts.TryRemove(symbol, out _);

                _logger.LogInformation("Successfully stopped monitoring for {symbol}", symbol);
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error stopping live data monitoring for {symbol}", symbol);
                throw;
            }
        }

        public void StopLiveDataMonitoring()
        {
            foreach (var timer in _monitoringTimers.Values)
            {
                timer.Dispose();
            }
            _monitoringTimers.Clear();
            _monitoredContracts.Clear();
        }

        public async Task<List<DataDownloadStatus>> GetDownloadStatusesAsync()
        {
            return await _marketDataRepository.GetAllDownloadStatusesAsync();
        }

        public async Task<IndexSnapshot> SaveIndexSnapshotAsync(string symbol, string exchange)
        {
            try
            {
                // Create a placeholder snapshot for now
                var snapshot = new IndexSnapshot
                {
                    Symbol = symbol,
                    LastPrice = 0,
                    Open = 0,
                    High = 0,
                    Low = 0,
                    Close = 0,
                    Volume = 0,
                    Timestamp = DateTime.UtcNow
                };

                await _marketDataRepository.AddIndexSnapshotAsync(snapshot);
                return snapshot;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving index snapshot for {symbol}", symbol);
                throw;
            }
        }

        public async Task<DomainQuote> GetQuoteAsync(string instrumentToken)
        {
            try
            {
                var kiteQuote = await _kiteConnectService.GetQuoteAsync(instrumentToken);
                return DomainQuote.FromKiteQuote(kiteQuote) ?? new DomainQuote { InstrumentToken = instrumentToken };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting quote for instrument {InstrumentToken}", instrumentToken);
                throw;
            }
        }

        public async Task<Dictionary<string, DomainQuote>> GetQuotesAsync(string[] instrumentTokens)
        {
            try
            {
                var kiteQuotes = await _kiteConnectService.GetQuotesAsync(instrumentTokens);
                var result = new Dictionary<string, DomainQuote>();
                
                foreach (var kvp in kiteQuotes)
                {
                    var quote = DomainQuote.FromKiteQuote(kvp.Value);
                    if (quote != null)
                    {
                        result[kvp.Key] = quote;
                    }
                }
                
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting quotes for {Count} instruments", instrumentTokens.Length);
                throw;
            }
        }

        public async Task<List<DomainQuote>> GetQuotes(List<string> instrumentTokens)
        {
            try
            {
                var kiteQuotes = await _kiteConnectService.GetQuotesAsync(instrumentTokens.ToArray());
                var result = new List<DomainQuote>();
                
                foreach (var kiteQuote in kiteQuotes.Values)
                {
                    var quote = DomainQuote.FromKiteQuote(kiteQuote);
                    if (quote != null)
                    {
                        result.Add(quote);
                    }
                }
                
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting quotes for {Count} instruments", instrumentTokens.Count);
                throw;
            }
        }

        public async Task<List<DomainInstrument>> GetInstrumentsAsync(string? exchange = null)
        {
            try
            {
                var kiteInstruments = await _kiteConnectService.GetInstrumentsAsync(exchange ?? "NFO");
                return kiteInstruments.Select(i => new DomainInstrument
                {
                    InstrumentToken = i.InstrumentToken,
                    TradingSymbol = i.TradingSymbol,
                    Name = i.Name ?? string.Empty,
                    Exchange = i.Exchange ?? string.Empty,
                    InstrumentType = i.InstrumentType ?? string.Empty,
                    Strike = i.Strike,
                    Expiry = null
                }).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting instruments for exchange {Exchange}", exchange);
                throw;
            }
        }

        public async Task<DomainInstrument> GetInstrumentAsync(string instrumentToken)
        {
            try
            {
                var instruments = await GetInstrumentsAsync();
                var instrument = instruments.FirstOrDefault(i => i.InstrumentToken == instrumentToken);
                
                if (instrument == null)
                {
                    throw new KeyNotFoundException($"Instrument not found for token {instrumentToken}");
                }
                
                return instrument;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting instrument for token {instrumentToken}", instrumentToken);
                throw;
            }
        }

        public async Task UpdateDownloadStatusAsync(DataDownloadStatus status)
        {
            await _marketDataRepository.UpdateDownloadStatusAsync(status);
        }

        public async Task<int> GetActiveContractsCountAsync(string symbol)
        {
            return await _marketDataRepository.GetActiveContractsCountAsync(symbol);
        }

        public async Task DownloadIndexDataAsync(string symbol, DateTime startDate, DateTime endDate, IProgress<(int value, string message)>? progress = null)
        {
            _logger.LogInformation("Downloading index data for {symbol} from {startDate} to {endDate}", symbol, startDate, endDate);
            await Task.CompletedTask;
        }

        private async Task MonitorContractsAsync(string symbol)
        {
            try
            {
                if (!_monitoredContracts.TryGetValue(symbol, out var contracts))
                {
                    return;
                }

                // Get quotes for a subset of contracts to avoid API limits
                var contractsToMonitor = contracts.Take(10).ToList();
                var instrumentTokens = contractsToMonitor.Select(c => c.InstrumentToken).ToArray();
                
                var quotes = await _kiteConnectService.GetQuotesAsync(instrumentTokens);
                
                foreach (var quote in quotes)
                {
                    var contract = contractsToMonitor.FirstOrDefault(c => c.InstrumentToken == quote.Key);
                    if (contract == null) continue;

                    var domainQuote = DomainQuote.FromKiteQuote(quote.Value);
                    if (domainQuote == null) continue;

                    // Create and save snapshot
                    var snapshot = new IntradayOptionSnapshot
                    {
                        InstrumentToken = contract.InstrumentToken,
                        Symbol = contract.TradingSymbol,
                        UnderlyingSymbol = contract.UnderlyingSymbol,
                        StrikePrice = contract.StrikePrice,
                        OptionType = contract.OptionType,
                        ExpiryDate = contract.ExpiryDate,
                        LastPrice = domainQuote.LastPrice,
                        Open = domainQuote.Open,
                        High = domainQuote.High,
                        Low = domainQuote.Low,
                        Close = domainQuote.Close,
                        Change = domainQuote.Change,
                        Volume = domainQuote.Volume,
                        OpenInterest = domainQuote.OpenInterest,
                        LowerCircuitLimit = domainQuote.LowerCircuitLimit,
                        UpperCircuitLimit = domainQuote.UpperCircuitLimit,
                        ImpliedVolatility = domainQuote.ImpliedVolatility,
                        Timestamp = domainQuote.TimeStamp,
                        CircuitLimitStatus = DetermineCircuitLimitStatus(domainQuote),
                        ValidationMessage = string.Empty,
                        TradingStatus = "Normal",
                        IsValidData = true
                    };

                    await _intradayDataService.SaveSnapshotAsync(snapshot);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error monitoring contracts for {symbol}", symbol);
            }
        }

        private string DetermineCircuitLimitStatus(DomainQuote quote)
        {
            if (quote.LastPrice <= quote.LowerCircuitLimit)
                return "Lower Circuit";
            if (quote.LastPrice >= quote.UpperCircuitLimit)
                return "Upper Circuit";
            return "Normal";
        }

        // Additional required interface methods with simplified implementations
        public async Task<DomainQuote> GetQuote(DomainInstrument instrument)
        {
            return await GetQuoteAsync(instrument.InstrumentToken);
        }

        public async Task<List<DomainQuote>> GetQuotesAsync(List<DomainInstrument> instruments)
        {
            try
            {
                var instrumentTokens = instruments.Select(i => i.InstrumentToken).ToArray();
                var quotes = await GetQuotesAsync(instrumentTokens);
                return quotes.Values.ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting quotes for {Count} instruments", instruments.Count);
                throw;
            }
        }

        public async Task<List<DomainHistorical>> GetHistoricalDataAsync(string instrumentToken, DateTime fromDate, DateTime toDate, string interval)
        {
            try
            {
                var kiteHistoricals = await _kiteConnectService.GetHistoricalDataAsync(instrumentToken, fromDate, toDate, interval);
                return kiteHistoricals.Select(h => new DomainHistorical
                {
                    InstrumentToken = instrumentToken,
                    Timestamp = h.TimeStamp,
                    Open = h.Open,
                    High = h.High,
                    Low = h.Low,
                    Close = h.Close,
                    Volume = h.Volume,
                    OpenInterest = h.OI
                }).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting historical data for instrument {InstrumentToken}", instrumentToken);
                throw;
            }
        }

        public async Task<List<DomainHistorical>> GetHistoricalDataAsync(DomainInstrument instrument, DateTime from, DateTime to, string interval)
        {
            return await GetHistoricalDataAsync(instrument.InstrumentToken, from, to, interval);
        }

        public async Task<OptionContract> SaveOptionSnapshotAsync(DomainInstrument instrument)
        {
            try
            {
                var kiteQuote = await _kiteConnectService.GetQuoteAsync(instrument.InstrumentToken);
                if (kiteQuote == null)
                {
                    _logger.LogWarning("Failed to get quote for {Symbol}", instrument.TradingSymbol);
                    return null;
                }

                return new OptionContract
                {
                    InstrumentToken = instrument.InstrumentToken,
                    Symbol = instrument.TradingSymbol,
                    UnderlyingSymbol = instrument.Name,
                    StrikePrice = instrument.Strike,
                    OptionType = instrument.InstrumentType,
                    ExpiryDate = instrument.Expiry ?? DateTime.MinValue,
                    LastPrice = kiteQuote.LastPrice,
                    Open = kiteQuote.Open,
                    High = kiteQuote.High,
                    Low = kiteQuote.Low,
                    Close = kiteQuote.Close,
                    Change = kiteQuote.Change,
                    Volume = (int)kiteQuote.Volume,
                    OpenInterest = (int)kiteQuote.OpenInterest,
                    Timestamp = DateTime.UtcNow,
                    LastUpdated = DateTime.UtcNow,
                    IsActive = true,
                    IsLiveData = true
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving option snapshot for {Symbol}", instrument.TradingSymbol);
                throw;
            }
        }

        public async Task<DataDownloadStatus> GetOrCreateDownloadStatus(string symbol, string exchange)
        {
            var status = await _marketDataRepository.GetDownloadStatusAsync(symbol, "Historical");
            if (status == null)
            {
                status = new DataDownloadStatus
                {
                    Symbol = symbol,
                    Exchange = exchange,
                    DataType = "Historical",
                    Status = "Not Started",
                    LastUpdated = DateTime.UtcNow
                };
                await _marketDataRepository.UpdateDownloadStatusAsync(status);
            }
            return status;
        }
    }
} 