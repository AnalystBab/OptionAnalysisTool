using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using OptionAnalysisTool.Models;
using OptionAnalysisTool.Models.KiteConnect;
using OptionAnalysisTool.KiteConnect.Models;
using KiteConnect;

namespace OptionAnalysisTool.KiteConnect.Services
{
    public interface IKiteConnectService
    {
        string LastStatusMessage { get; }
        bool IsInitialized { get; }
        
        // Initialization and Authentication
        Task<bool> InitializeAsync();
        Task<string> GetLoginUrl();
        Task<bool> GenerateSession(string requestToken, string apiSecret);
        Task<bool> SetAccessToken(string accessToken);
        Task<string> GetAccessToken();
        Task<bool> LoginAsync();
        Task<bool> IsLoggedInAsync();
        
        // Connection Management
        Task<bool> ConnectAsync();
        Task<bool> DisconnectAsync();
        Task<bool> IsConnectedAsync();
        
        // Instrument Data
        Task<List<KiteInstrument>> GetInstrumentsAsync(string exchange);
        Task<List<KiteInstrument>> GetInstrumentsAsync();
        Task<KiteInstrument?> GetInstrumentAsync(string symbol, string exchange);
        Task<List<OptionAnalysisTool.Models.Instrument>> GetInstruments(string exchange = null, string segment = null);
        
        // Quote Data
        Task<Dictionary<string, KiteQuote>> GetQuotesAsync(string[] instrumentTokens);
        Task<KiteQuote> GetQuoteAsync(string instrumentToken);
        Task<List<OptionAnalysisTool.Models.Quote>> GetQuotes(List<OptionAnalysisTool.Models.Instrument> instruments);
        Task<OptionAnalysisTool.Models.Quote> GetQuote(OptionAnalysisTool.Models.Instrument instrument);
        
        // Historical Data
        Task<List<KiteHistorical>> GetHistoricalDataAsync(string instrumentToken, DateTime fromDate, DateTime toDate, string interval);
        
        // OHLC and LTP Data
        Task<Dictionary<string, KiteOHLC>> GetOHLCAsync(string[] instrumentTokens);
        Task<Dictionary<string, KiteLTP>> GetLTPAsync(string[] instrumentTokens);
        Task<KiteLTP> GetLTP(string instrumentToken);
        
        // Option Data
        Task<List<IntradayOptionSnapshot>> GetIntradayData(string symbol, DateTime date);
        Task<DailyOptionContract?> GetDailyData(string symbol, DateTime date);

        /// <summary>
        /// Validates if the current session is active and valid
        /// </summary>
        /// <returns>True if session is valid, false otherwise</returns>
        Task<bool> ValidateSessionAsync();

        /// <summary>
        /// Validates if the session is valid for today by checking the trading date from a live quote
        /// </summary>
        /// <returns>True if session is valid and trading date is today, false otherwise</returns>
        Task<bool> IsSessionValidForTodayAsync();
    }
}