using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using OptionAnalysisTool.Models;
using OptionAnalysisTool.Common.Repositories;
using Microsoft.Extensions.Logging;
using OptionAnalysisTool.KiteConnect.Services;
using KiteConnect;
using Historical = KiteConnect.Historical;
using KiteHistorical = OptionAnalysisTool.Models.KiteConnect.Historical;
using DomainHistorical = OptionAnalysisTool.Models.Historical;

namespace OptionAnalysisTool.Common.Services
{
    public class DataDownloadService : IDataDownloadService
    {
        private readonly ILogger<DataDownloadService> _logger;
        private readonly IMarketDataRepository _marketDataRepository;
        private readonly IntradayDataService _intradayDataService;
        private readonly HistoricalOptionDataService _historicalOptionDataService;
        private readonly IKiteConnectService _kiteConnectService;
        private readonly MarketDataService _marketDataService;

        public DataDownloadService(
            ILogger<DataDownloadService> logger,
            IMarketDataRepository marketDataRepository,
            IntradayDataService intradayDataService,
            HistoricalOptionDataService historicalOptionDataService,
            IKiteConnectService kiteConnectService,
            MarketDataService marketDataService)
        {
            _logger = logger;
            _marketDataRepository = marketDataRepository;
            _intradayDataService = intradayDataService;
            _historicalOptionDataService = historicalOptionDataService;
            _kiteConnectService = kiteConnectService;
            _marketDataService = marketDataService;
        }

        public async Task DownloadHistoricalDataAsync(string symbol, DateTime startDate, DateTime endDate, IProgress<(int value, string message)>? progress = null)
        {
            try
            {
                _logger.LogInformation("Downloading historical data for {symbol} from {startDate} to {endDate}", symbol, startDate, endDate);

                var status = await GetOrCreateDownloadStatus(symbol, "NSE");
                status.Status = "In Progress";
                status.StartDate = startDate;
                status.EndDate = endDate;
                status.LastUpdated = DateTime.UtcNow;
                await _marketDataRepository.UpdateDownloadStatusAsync(status);

                var historicalData = await _kiteConnectService.GetHistoricalDataAsync(symbol, startDate, endDate, "day");
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

                await _historicalOptionDataService.SaveHistoricalDataAsync(symbol, startDate, domainHistoricals);

                status.Status = "Completed";
                status.TotalRecords = domainHistoricals.Count;
                status.ProcessedRecords = domainHistoricals.Count;
                status.LastUpdated = DateTime.UtcNow;
                await _marketDataRepository.UpdateDownloadStatusAsync(status);

                _logger.LogInformation("Successfully downloaded historical data for {symbol}", symbol);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error downloading historical data for {symbol}", symbol);
                throw;
            }
        }

        public async Task DownloadOptionDataAsync(string symbol, DateTime startDate, DateTime endDate, IProgress<(int value, string message)>? progress = null)
        {
            try
            {
                _logger.LogInformation("Downloading option data for {symbol} from {startDate} to {endDate}", symbol, startDate, endDate);

                var status = await GetOrCreateDownloadStatus(symbol, "NFO");
                status.Status = "In Progress";
                status.StartDate = startDate;
                status.EndDate = endDate;
                status.LastUpdated = DateTime.UtcNow;
                await _marketDataRepository.UpdateDownloadStatusAsync(status);

                var instruments = await _kiteConnectService.GetInstrumentsAsync("NFO");
                var optionInstruments = instruments
                    .Where(i => i.Name == symbol &&
                               (i.InstrumentType == "CE" || i.InstrumentType == "PE") &&
                               i.Expiry >= startDate && i.Expiry <= endDate)
                    .ToList();

                int totalRecords = optionInstruments.Count;
                int processedRecords = 0;

                foreach (var instrument in optionInstruments)
                {
                    try
                    {
                        var historicalData = await _kiteConnectService.GetHistoricalDataAsync(
                            instrument.InstrumentToken.ToString(), startDate, endDate, "day");

                        var domainHistoricals = historicalData.Select(h => new DomainHistorical
                        {
                            InstrumentToken = instrument.InstrumentToken.ToString(),
                            Timestamp = h.TimeStamp,
                            Open = h.Open,
                            High = h.High,
                            Low = h.Low,
                            Close = h.Close,
                            Volume = h.Volume,
                            OpenInterest = h.OI
                        }).ToList();

                        await _historicalOptionDataService.SaveHistoricalDataAsync(
                            instrument.InstrumentToken.ToString(), startDate, domainHistoricals);

                        processedRecords++;
                        progress?.Report((processedRecords * 100 / totalRecords, $"Processed {processedRecords} of {totalRecords} instruments"));
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error downloading data for instrument {symbol}", instrument.TradingSymbol);
                        status.FailedRecords++;
                    }
                }

                status.Status = "Completed";
                status.TotalRecords = totalRecords;
                status.ProcessedRecords = processedRecords;
                status.LastUpdated = DateTime.UtcNow;
                await _marketDataRepository.UpdateDownloadStatusAsync(status);

                _logger.LogInformation("Successfully downloaded option data for {symbol}", symbol);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error downloading option data for {symbol}", symbol);
                throw;
            }
        }

        public async Task DownloadIndexDataAsync(string symbol, DateTime startDate, DateTime endDate, IProgress<(int value, string message)>? progress = null)
        {
            try
            {
                _logger.LogInformation("Downloading index data for {symbol} from {startDate} to {endDate}", 
                    symbol, startDate, endDate);

                var status = await _marketDataRepository.GetDownloadStatusAsync(symbol, "Index");
                if (status == null)
                {
                    status = new DataDownloadStatus
                    {
                        Symbol = symbol,
                        Exchange = "NSE",
                        DataType = "Index",
                        Status = "Not Started",
                        LastUpdated = DateTime.UtcNow,
                        LastDownloadedDate = DateTime.UtcNow
                    };
                    await _marketDataRepository.UpdateDownloadStatusAsync(status);
                }

                // Update status
                status.Status = "Downloading";
                status.LastUpdated = DateTime.UtcNow;
                await _marketDataRepository.UpdateDownloadStatusAsync(status);

                // Download data
                var currentDate = startDate;
                var totalDays = (endDate - startDate).Days + 1;
                var currentDay = 0;

                while (currentDate <= endDate)
                {
                    try
                    {
                        await _marketDataRepository.AddIndexSnapshotAsync(new IndexSnapshot
                        {
                            Symbol = symbol,
                            LastPrice = 0,
                            Open = 0,
                            High = 0,
                            Low = 0,
                            Close = 0,
                            Volume = 0,
                            Timestamp = currentDate
                        });

                        currentDay++;
                        progress?.Report((currentDay * 100 / totalDays, $"Downloaded data for {currentDate:yyyy-MM-dd}"));
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error downloading data for {symbol} on {date}", symbol, currentDate);
                    }

                    currentDate = currentDate.AddDays(1);
                }

                // Update status
                status.Status = "Completed";
                status.LastDownloadedDate = endDate;
                status.LastUpdated = DateTime.UtcNow;
                await _marketDataRepository.UpdateDownloadStatusAsync(status);

                _logger.LogInformation("Successfully downloaded index data for {symbol}", symbol);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error downloading index data for {symbol}", symbol);
                throw;
            }
        }

        public async Task<List<DataDownloadStatus>> GetDownloadStatusesAsync()
        {
            return await _marketDataRepository.GetAllDownloadStatusesAsync();
        }

        public async Task UpdateDownloadStatusAsync(DataDownloadStatus status)
        {
            await _marketDataRepository.UpdateDownloadStatusAsync(status);
        }

        public async Task DownloadHistoricalData(string symbol, string exchange, DateTime fromDate, DateTime toDate)
        {
            try
            {
                var status = await _marketDataService.GetOrCreateDownloadStatus(symbol, exchange);
                status.Status = "In Progress";
                status.LastUpdated = DateTime.UtcNow;
                await _marketDataRepository.UpdateDownloadStatusAsync(status);

                var historicalData = await _kiteConnectService.GetHistoricalDataAsync(symbol, fromDate, toDate, "day");
                if (historicalData != null && historicalData.Any())
                {
                    var prices = historicalData.Select(h => new HistoricalPrice
                    {
                        Symbol = symbol,
                        Exchange = exchange,
                        Date = h.TimeStamp,
                        Open = h.Open,
                        High = h.High,
                        Low = h.Low,
                        Close = h.Close,
                        Volume = h.Volume
                    }).ToList();

                    await _marketDataRepository.SaveHistoricalPricesAsync(prices);
                    status.Status = "Completed";
                    status.LastUpdated = DateTime.UtcNow;
                    await _marketDataRepository.UpdateDownloadStatusAsync(status);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error downloading historical data for {Symbol}", symbol);
                var status = await _marketDataService.GetOrCreateDownloadStatus(symbol, exchange);
                status.Status = "Failed";
                status.LastUpdated = DateTime.UtcNow;
                await _marketDataRepository.UpdateDownloadStatusAsync(status);
                throw;
            }
        }

        public async Task DownloadStockHistoricalData(string symbol, string exchange, DateTime fromDate, DateTime toDate)
        {
            try
            {
                var status = await _marketDataService.GetOrCreateDownloadStatus(symbol, exchange);
                status.Status = "In Progress";
                status.LastUpdated = DateTime.UtcNow;
                await _marketDataRepository.UpdateDownloadStatusAsync(status);

                var historicalData = await _kiteConnectService.GetHistoricalDataAsync(symbol, fromDate, toDate, "day");
                if (historicalData != null && historicalData.Any())
                {
                    var prices = historicalData.Select(h => new StockHistoricalPrice
                    {
                        Symbol = symbol,
                        Exchange = exchange,
                        Date = h.TimeStamp,
                        Open = h.Open,
                        High = h.High,
                        Low = h.Low,
                        Close = h.Close,
                        Volume = h.Volume
                    }).ToList();

                    await _marketDataRepository.SaveStockHistoricalPricesAsync(prices);
                    status.Status = "Completed";
                    status.LastUpdated = DateTime.UtcNow;
                    await _marketDataRepository.UpdateDownloadStatusAsync(status);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error downloading stock historical data for {Symbol}", symbol);
                var status = await _marketDataService.GetOrCreateDownloadStatus(symbol, exchange);
                status.Status = "Failed";
                status.LastUpdated = DateTime.UtcNow;
                await _marketDataRepository.UpdateDownloadStatusAsync(status);
                throw;
            }
        }

        private async Task<DataDownloadStatus> GetOrCreateDownloadStatus(string symbol, string exchange)
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