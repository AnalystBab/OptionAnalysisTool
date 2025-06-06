using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OptionAnalysisTool.Common.Data;
using OptionAnalysisTool.Models;
using OptionAnalysisTool.KiteConnect.Services;
using OptionAnalysisTool.KiteConnect.Models;
using KiteConnect;
using OptionAnalysisTool.Common.Repositories;
using Instrument = OptionAnalysisTool.Models.Instrument;
using Quote = OptionAnalysisTool.Models.Quote;
using Historical = KiteConnect.Historical;
using OptionAnalysisTool.Common.Models;
using OptionMonitoringStats = OptionAnalysisTool.Common.Models.OptionMonitoringStats;
using DomainOptionMonitoringStats = OptionAnalysisTool.Models.OptionMonitoringStats;
using DomainInstrument = OptionAnalysisTool.Models.Instrument;
using DomainQuote = OptionAnalysisTool.Models.Quote;
using DomainHistorical = OptionAnalysisTool.Models.Historical;
using KiteHistorical = OptionAnalysisTool.KiteConnect.Models.KiteHistorical;
using DomainOptionContract = OptionAnalysisTool.Models.OptionContract;
using CommonOptionContract = OptionAnalysisTool.Common.Models.OptionContract;
using CommonOptionMonitoringStats = OptionAnalysisTool.Common.Models.OptionMonitoringStats;

namespace OptionAnalysisTool.Common.Services
{
    public class OptionDataCollectionService : IOptionDataCollectionService
    {
        private readonly ILogger<OptionDataCollectionService> _logger;
        private readonly ApplicationDbContext _context;
        private readonly IKiteConnectService _kiteConnectService;
        private readonly MarketHoursService _marketHoursService;
        private readonly CancellationTokenSource _cancellationTokenSource;
        private bool _isCollecting;
        private readonly HashSet<string> _knownInstruments;
        private readonly List<string> _indexSymbols = new() 
        { 
            "NIFTY", "BANKNIFTY", "FINNIFTY", "MIDCPNIFTY" 
        };
        private readonly IMarketDataRepository _marketDataRepository;
        private readonly IntradayDataService _intradayDataService;
        private readonly HistoricalOptionDataService _historicalOptionDataService;
        private readonly IMarketDataService _marketDataService;

        public OptionDataCollectionService(
            ILogger<OptionDataCollectionService> logger,
            ApplicationDbContext context,
            IKiteConnectService kiteConnectService,
            MarketHoursService marketHoursService,
            IMarketDataRepository marketDataRepository,
            IntradayDataService intradayDataService,
            HistoricalOptionDataService historicalOptionDataService,
            IMarketDataService marketDataService)
        {
            _logger = logger;
            _context = context;
            _kiteConnectService = kiteConnectService;
            _marketHoursService = marketHoursService;
            _cancellationTokenSource = new CancellationTokenSource();
            _knownInstruments = new HashSet<string>();
            _marketDataRepository = marketDataRepository;
            _intradayDataService = intradayDataService;
            _historicalOptionDataService = historicalOptionDataService;
            _marketDataService = marketDataService;
        }

        public async Task StartCollectionAsync()
        {
            if (_isCollecting)
            {
                _logger.LogWarning("Data collection is already running");
                return;
            }

            _isCollecting = true;
            _logger.LogInformation("Starting option data collection");

            try
            {
                while (!_cancellationTokenSource.Token.IsCancellationRequested)
                {
                    var sw = Stopwatch.StartNew();
                    await CollectDataAsync();
                    sw.Stop();

                    _logger.LogInformation($"Data collection cycle completed in {sw.ElapsedMilliseconds}ms");
                    await Task.Delay(TimeSpan.FromMinutes(1), _cancellationTokenSource.Token);
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Data collection cancelled");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in data collection");
            }
            finally
            {
                _isCollecting = false;
            }
        }

        public void StopCollection()
        {
            _cancellationTokenSource.Cancel();
        }

        private async Task CollectDataAsync()
        {
            try
            {
                if (!_marketHoursService.IsMarketOpen())
                {
                    _logger.LogInformation("Market is closed. Skipping data collection.");
                    return;
                }

                var instruments = await _kiteConnectService.GetInstrumentsAsync();
                var optionInstruments = instruments
                    .Where(i => i.InstrumentType == "CE" || i.InstrumentType == "PE")
                    .ToList();

                foreach (var instrument in optionInstruments)
                {
                    try
                    {
                        var kiteQuote = await _kiteConnectService.GetQuoteAsync(instrument.InstrumentToken);
                        if (kiteQuote != null)
                        {
                            var snapshot = new IntradayOptionSnapshot
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
                                Volume = kiteQuote.Volume,
                                OpenInterest = kiteQuote.OpenInterest,
                                LowerCircuitLimit = kiteQuote.LowerCircuitLimit,
                                UpperCircuitLimit = kiteQuote.UpperCircuitLimit,
                                ImpliedVolatility = kiteQuote.ImpliedVolatility,
                                Timestamp = DateTime.UtcNow,
                                LastUpdated = DateTime.UtcNow,
                                CircuitLimitStatus = DetermineCircuitLimitStatus(kiteQuote),
                                ValidationMessage = string.Empty,
                                TradingStatus = "Normal",
                                IsValidData = true
                            };

                            await _intradayDataService.SaveSnapshotAsync(snapshot);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Error collecting data for {instrument.TradingSymbol}");
                    }
                }

                await SaveMonitoringStats(optionInstruments.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in data collection cycle");
            }
        }

        private string DetermineCircuitLimitStatus(KiteQuote quote)
        {
            if (quote.LastPrice <= quote.LowerCircuitLimit)
                return "Lower Circuit";
            if (quote.LastPrice >= quote.UpperCircuitLimit)
                return "Upper Circuit";
            return "Normal";
        }

        private async Task SaveMonitoringStats(int activeContractsCount)
        {
            try
            {
                var stats = new DomainOptionMonitoringStats
                {
                    Timestamp = DateTime.UtcNow,
                    ActiveContractsCount = activeContractsCount,
                    NewContractsFound = 0,
                    SnapshotsSaved = activeContractsCount,
                    ErrorCount = 0,
                    LastError = string.Empty
                };

                await _marketDataRepository.SaveOptionMonitoringStatsAsync(stats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving monitoring stats");
            }
        }

        public async Task<DomainOptionMonitoringStats> GetLatestStatsAsync()
        {
            try
            {
                var stats = await _context.OptionMonitoringStats
                    .OrderByDescending(s => s.Timestamp)
                    .FirstOrDefaultAsync();
                return stats ?? new DomainOptionMonitoringStats();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting latest stats");
                throw;
            }
        }

        public async Task CollectOptionDataAsync(string symbol)
        {
            try
            {
                var instruments = await _marketDataService.GetInstrumentsAsync();
                var instrument = instruments.FirstOrDefault(i => i.TradingSymbol == symbol);
                if (instrument == null)
                {
                    _logger.LogWarning($"Instrument not found for symbol {symbol}");
                    return;
                }

                var kiteQuote = await _marketDataService.GetQuoteAsync(instrument.InstrumentToken);
                if (kiteQuote == null)
                {
                    _logger.LogWarning($"Quote not found for symbol {symbol}");
                    return;
                }

                var optionContract = new DomainOptionContract
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

                await _marketDataRepository.AddOptionContractAsync(optionContract);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error collecting option data for {symbol}");
                throw;
            }
        }

        public async Task CollectIntradayDataAsync(string symbol, DateTime date)
        {
            try
            {
                _logger.LogInformation("Collecting intraday data for {symbol} on {date}", symbol, date);

                var historicalData = await _kiteConnectService.GetHistoricalDataAsync(
                    symbol, date, date, "minute");

                var domainHistoricals = historicalData.Select(h => new DomainHistorical
                {
                    InstrumentToken = symbol,
                    Timestamp = h.TimeStamp,
                    Open = h.Open,
                    High = h.High,
                    Low = h.Low,
                    Close = h.Close,
                    Volume = h.Volume,
                    OpenInterest = h.OI
                }).ToList();

                await _historicalOptionDataService.SaveHistoricalDataAsync(symbol, date, domainHistoricals);

                _logger.LogInformation("Successfully collected intraday data for {symbol} on {date}", symbol, date);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error collecting intraday data for {symbol} on {date}", symbol, date);
                throw;
            }
        }

        public async Task CollectDailyDataAsync(string symbol, DateTime date)
        {
            try
            {
                _logger.LogInformation("Collecting daily data for {symbol} on {date}", symbol, date);

                var historicalData = await _kiteConnectService.GetHistoricalDataAsync(
                    symbol, date, date, "day");

                var domainHistoricals = historicalData.Select(h => new DomainHistorical
                {
                    InstrumentToken = symbol,
                    Timestamp = h.TimeStamp,
                    Open = h.Open,
                    High = h.High,
                    Low = h.Low,
                    Close = h.Close,
                    Volume = h.Volume,
                    OpenInterest = h.OI
                }).ToList();

                await _historicalOptionDataService.SaveHistoricalDataAsync(symbol, date, domainHistoricals);

                _logger.LogInformation("Successfully collected daily data for {symbol} on {date}", symbol, date);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error collecting daily data for {symbol} on {date}", symbol, date);
                throw;
            }
        }

        public async Task<List<DataDownloadStatus>> GetDownloadStatusesAsync()
        {
            try
            {
                return await _marketDataRepository.GetAllDownloadStatusesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting download statuses");
                return new List<DataDownloadStatus>();
            }
        }

        public async Task UpdateDownloadStatusAsync(DataDownloadStatus status)
        {
            try
            {
                await _marketDataRepository.UpdateDownloadStatusAsync(status);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating download status for {symbol}", status.Symbol);
                throw;
            }
        }

        public async Task<OptionData> GetOptionDataAsync(string instrumentToken)
        {
            try
            {
                var instrument = await _marketDataService.GetInstrumentAsync(instrumentToken);
                if (instrument == null)
                {
                    throw new ArgumentException($"Instrument not found: {instrumentToken}");
                }

                var kiteQuote = await _marketDataService.GetQuoteAsync(instrumentToken);
                if (kiteQuote == null)
                {
                    throw new Exception($"Failed to get quote for {instrumentToken}");
                }

                return new OptionData
                {
                    InstrumentToken = instrumentToken,
                    Symbol = instrument.TradingSymbol,
                    UnderlyingSymbol = instrument.Name,
                    StrikePrice = instrument.Strike,
                    OptionType = instrument.InstrumentType,
                    ExpiryDate = instrument.Expiry ?? DateTime.MinValue,
                    LastPrice = kiteQuote.LastPrice,
                    Volume = (int)kiteQuote.Volume,
                    OpenInterest = (int)kiteQuote.OpenInterest
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting option data for {InstrumentToken}", instrumentToken);
                throw;
            }
        }

        public async Task<List<OptionData>> GetOptionDataAsync(List<string> instrumentTokens)
        {
            try
            {
                var instruments = await _marketDataService.GetInstrumentsAsync();
                var quotes = await _marketDataService.GetQuotesAsync(instrumentTokens.ToArray());

                return instrumentTokens.Select(token =>
                {
                    var instrument = instruments.FirstOrDefault(i => i.InstrumentToken == token);
                    if (instrument == null) return null;

                    quotes.TryGetValue(token, out var kiteQuote);
                    if (kiteQuote == null) return null;

                    return new OptionData
                    {
                        InstrumentToken = token,
                        Symbol = instrument.TradingSymbol,
                        UnderlyingSymbol = instrument.Name,
                        StrikePrice = instrument.Strike,
                        OptionType = instrument.InstrumentType,
                        ExpiryDate = instrument.Expiry ?? DateTime.MinValue,
                        LastPrice = kiteQuote.LastPrice,
                        Volume = (int)kiteQuote.Volume,
                        OpenInterest = (int)kiteQuote.OpenInterest
                    };
                })
                .Where(data => data != null)
                .ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting option data for multiple instruments");
                throw;
            }
        }

        private async Task<DomainInstrument> GetInstrumentFromToken(string instrumentToken)
        {
            var instruments = await _marketDataService.GetInstrumentsAsync();
            return instruments.FirstOrDefault(i => i.InstrumentToken == instrumentToken) 
                ?? throw new ArgumentException($"Instrument not found: {instrumentToken}");
        }

        public async Task<DomainOptionMonitoringStats> GetOptionMonitoringStatsAsync(string symbol)
        {
            try
            {
                var instrument = await GetInstrumentFromToken(symbol);
                var kiteQuote = await _marketDataService.GetQuoteAsync(symbol);
                if (kiteQuote == null)
                {
                    _logger.LogWarning("Failed to get quote for {Symbol}", symbol);
                    return null;
                }

                var stats = new DomainOptionMonitoringStats
                {
                    Timestamp = DateTime.UtcNow,
                    Symbol = symbol,
                    UnderlyingSymbol = instrument.Name,
                    OptionType = instrument.InstrumentType,
                    IsMarketOpen = _marketHoursService.IsMarketOpen(),
                    ActiveContractsCount = 1,
                    NewContractsFound = 0,
                    SnapshotsSaved = 1,
                    ErrorCount = 0,
                    LastError = string.Empty,
                    AverageProcessingTime = 0,
                    NextUpdateTime = DateTime.UtcNow.AddMinutes(1),
                    LastUpdated = DateTime.UtcNow
                };

                await _marketDataRepository.SaveOptionMonitoringStatsAsync(stats);
                return stats;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting option monitoring stats for {Symbol}", symbol);
                return null;
            }
        }

        public async Task SaveHistoricalDataAsync(string symbol, DateTime date, List<KiteHistorical> historicals)
        {
            try
            {
                var domainHistoricals = historicals.Select(h => new DomainHistorical
                {
                    InstrumentToken = symbol,
                    Timestamp = h.TimeStamp,
                    Open = h.Open,
                    High = h.High,
                    Low = h.Low,
                    Close = h.Close,
                    Volume = h.Volume,
                    OpenInterest = h.OI
                }).ToList();

                await _historicalOptionDataService.SaveHistoricalDataAsync(symbol, date, domainHistoricals);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error saving historical data for {symbol} on {date}");
                throw;
            }
        }

        private DomainOptionMonitoringStats ConvertToCommonStats(DomainOptionMonitoringStats stats)
        {
            if (stats == null) return new DomainOptionMonitoringStats();

            return new DomainOptionMonitoringStats
            {
                Timestamp = stats.Timestamp,
                ActiveContractsCount = stats.ActiveContractsCount,
                NewContractsFound = stats.NewContractsFound,
                SnapshotsSaved = stats.SnapshotsSaved,
                ErrorCount = stats.ErrorCount,
                LastError = stats.LastError,
                Symbol = stats.Symbol,
                UnderlyingSymbol = stats.UnderlyingSymbol,
                OptionType = stats.OptionType,
                IsMarketOpen = stats.IsMarketOpen,
                AverageProcessingTime = stats.AverageProcessingTime,
                NextUpdateTime = stats.NextUpdateTime,
                LastUpdated = stats.LastUpdated
            };
        }
    }
} 