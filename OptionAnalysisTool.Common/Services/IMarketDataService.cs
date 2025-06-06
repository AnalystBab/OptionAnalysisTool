using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using OptionAnalysisTool.Models;

namespace OptionAnalysisTool.Common.Services
{
    public interface IMarketDataService
    {
        /// <summary>
        /// Downloads historical index data for the specified symbol and date range
        /// </summary>
        Task DownloadHistoricalDataAsync(string symbol, DateTime startDate, DateTime endDate, IProgress<(int value, string message)>? progress = null);

        /// <summary>
        /// Downloads historical option data for the specified symbol and date range
        /// </summary>
        Task DownloadOptionDataAsync(string symbol, DateTime startDate, DateTime endDate, IProgress<(int value, string message)>? progress = null);

        /// <summary>
        /// Starts monitoring live data for the specified symbol
        /// </summary>
        Task StartLiveDataMonitoringAsync(string symbol);

        /// <summary>
        /// Stops monitoring live data for the specified symbol
        /// </summary>
        Task StopLiveDataMonitoringAsync(string symbol);

        /// <summary>
        /// Gets the download status for all symbols
        /// </summary>
        Task<List<DataDownloadStatus>> GetDownloadStatusesAsync();

        /// <summary>
        /// Saves a snapshot of index data for the specified instrument
        /// </summary>
        Task<IndexSnapshot> SaveIndexSnapshotAsync(string symbol, string exchange);

        /// <summary>
        /// Saves a snapshot of option data for the specified instrument
        /// </summary>
        Task<OptionContract> SaveOptionSnapshotAsync(Instrument instrument);

        Task<DataDownloadStatus> GetOrCreateDownloadStatus(string symbol, string exchange);
        Task DownloadIndexDataAsync(string symbol, DateTime startDate, DateTime endDate, IProgress<(int value, string message)>? progress = null);
        void StopLiveDataMonitoring();

        Task<List<Instrument>> GetInstrumentsAsync(string? exchange = null);
        Task<List<Quote>> GetQuotes(List<string> instrumentTokens);
        Task<Quote> GetQuote(Instrument instrument);
        Task<Dictionary<string, Quote>> GetQuotesAsync(string[] instrumentTokens);
        
        /// <summary>
        /// Gets historical data for the specified instrument and time range
        /// </summary>
        Task<List<Historical>> GetHistoricalDataAsync(Instrument instrument, DateTime from, DateTime to, string interval);
        
        Task<Quote> GetQuoteAsync(string instrumentToken);
        Task<List<Quote>> GetQuotesAsync(List<Instrument> instruments);
        Task UpdateDownloadStatusAsync(DataDownloadStatus status);
        Task<Instrument> GetInstrumentAsync(string instrumentToken);

        /// <summary>
        /// Gets the count of active contracts for the specified symbol
        /// </summary>
        Task<int> GetActiveContractsCountAsync(string symbol);
    }
} 