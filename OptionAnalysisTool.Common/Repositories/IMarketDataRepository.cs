using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using OptionAnalysisTool.Models;

namespace OptionAnalysisTool.Common.Repositories
{
    public interface IMarketDataRepository
    {
        Task<IndexSnapshot> GetLatestIndexSnapshotAsync(string symbol);
        Task<List<IndexSnapshot>> GetIndexSnapshotsAsync(string symbol, DateTime startDate, DateTime endDate);
        Task<bool> AddIndexSnapshotAsync(IndexSnapshot snapshot);
        Task<bool> AddIndexSnapshotsAsync(IEnumerable<IndexSnapshot> snapshots);
        
        Task<OptionContract> GetLatestOptionContractAsync(string symbol, decimal strikePrice, string optionType);
        Task<List<OptionContract>> GetOptionContractsAsync(string symbol, DateTime startDate, DateTime endDate);
        Task<bool> AddOptionContractAsync(OptionContract contract);
        Task<bool> AddOptionContractsAsync(IEnumerable<OptionContract> contracts);
        
        Task<CircuitBreaker> GetLatestCircuitBreakerAsync(string symbol);
        Task<List<CircuitBreaker>> GetCircuitBreakersAsync(string symbol, DateTime startDate, DateTime endDate);
        Task<bool> AddCircuitBreakerAsync(CircuitBreaker circuitBreaker);
        
        Task<DataDownloadStatus> GetDownloadStatusAsync(string symbol, string dataType);
        Task<List<DataDownloadStatus>> GetAllDownloadStatusesAsync();
        Task UpdateDownloadStatusAsync(DataDownloadStatus status);
        
        Task SaveChangesAsync();

        Task<bool> SaveHistoricalPriceAsync(HistoricalPrice historicalPrice);
        Task<bool> SaveHistoricalPricesAsync(IEnumerable<HistoricalPrice> historicalPrices);
        Task<bool> SaveStockHistoricalPricesAsync(IEnumerable<StockHistoricalPrice> stockHistoricalPrices);

        Task SaveOptionMonitoringStatsAsync(OptionMonitoringStats stats);
        Task<int> GetActiveContractsCountAsync(string symbol);

        Task<bool> SaveSpotDataAsync(SpotData spotData);
        
        Task<List<IntradayOptionSnapshot>> GetSnapshotsByDateRangeAsync(DateTime startDate, DateTime endDate, string? symbol = null);
    }
} 