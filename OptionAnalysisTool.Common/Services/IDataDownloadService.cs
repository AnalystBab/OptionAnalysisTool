using System;
using System.Threading.Tasks;
using OptionAnalysisTool.Models;

namespace OptionAnalysisTool.Common.Services
{
    public interface IDataDownloadService
    {
        Task DownloadHistoricalDataAsync(string symbol, DateTime startDate, DateTime endDate, IProgress<(int value, string message)>? progress = null);
        Task DownloadOptionDataAsync(string symbol, DateTime startDate, DateTime endDate, IProgress<(int value, string message)>? progress = null);
        Task DownloadIndexDataAsync(string symbol, DateTime startDate, DateTime endDate, IProgress<(int value, string message)>? progress = null);
        Task<List<DataDownloadStatus>> GetDownloadStatusesAsync();
        Task UpdateDownloadStatusAsync(DataDownloadStatus status);
        Task DownloadHistoricalData(string symbol, string exchange, DateTime fromDate, DateTime toDate);
        Task DownloadStockHistoricalData(string symbol, string exchange, DateTime fromDate, DateTime toDate);
    }
} 