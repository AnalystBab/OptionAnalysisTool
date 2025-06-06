using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using OptionAnalysisTool.Models;

namespace OptionAnalysisTool.Common.Services
{
    public interface IOptionDataCollectionService
    {
        Task CollectIntradayDataAsync(string symbol, DateTime date);
        Task CollectDailyDataAsync(string symbol, DateTime date);
        Task<List<DataDownloadStatus>> GetDownloadStatusesAsync();
        Task UpdateDownloadStatusAsync(DataDownloadStatus status);
    }
} 