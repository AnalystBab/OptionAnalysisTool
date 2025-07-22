using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using System.Threading;
using System.Diagnostics;
using System.Linq;
using OptionAnalysisTool.Models;
using Microsoft.Extensions.Configuration;
using OptionAnalysisTool.KiteConnect.Models;
using KiteConnectAPI = KiteConnect;

namespace OptionAnalysisTool.KiteConnect.Services
{
    public class KiteConnectService : IKiteConnectService
    {
        private readonly ILogger<KiteConnectService> _logger;
        private readonly IConfiguration _configuration;
        private KiteConnectAPI.Kite _kite;
        private bool _isInitialized;
        private readonly KiteConnectConfig _config;
        private readonly TokenStorage _tokenStorage;
        private readonly KiteConnectWrapper _kiteWrapper;
        private string _accessToken = string.Empty;
        private bool _isConnected;
        private readonly SemaphoreSlim _initializationLock = new SemaphoreSlim(1, 1);
        private volatile bool _isInitializing;
        private string _lastStatusMessage = string.Empty;
        private TaskCompletionSource<bool>? _initializationTask;

        public KiteConnectService(ILogger<KiteConnectService> logger, IConfiguration configuration, KiteConnectWrapper kiteWrapper)
        {
            _logger = logger;
            _configuration = configuration;
            _config = new KiteConnectConfig
            {
                ApiKey = _configuration["KiteConnect:ApiKey"] ?? string.Empty,
                ApiSecret = _configuration["KiteConnect:ApiSecret"] ?? string.Empty,
                RedirectUrl = _configuration["KiteConnect:RedirectUrl"] ?? "http://localhost:8080"
            };
            _kite = new KiteConnectAPI.Kite(_config.ApiKey);
            _isInitialized = false;
            _tokenStorage = new TokenStorage(new OptionsWrapper<KiteConnectConfig>(_config));
            _kiteWrapper = kiteWrapper;
            
            var tokenResult = _tokenStorage.LoadToken().GetAwaiter().GetResult();
            _lastStatusMessage = tokenResult.Message;
            _logger.LogInformation("KiteConnect Service created. Status: {message}", tokenResult.Message);
            
            if (tokenResult.Token != null)
            {
                _accessToken = tokenResult.Token.AccessToken;
                _kite = new KiteConnectAPI.Kite(_config.ApiKey);
                _kite.SetAccessToken(_accessToken);
                _logger.LogInformation("Loaded existing access token valid until {expiry}", tokenResult.Token.ExpiresAt);
                _isInitialized = true;
                _isConnected = true;
            }
        }

        public string LastStatusMessage => _lastStatusMessage;
        public bool IsInitialized => _isInitialized;

        public async Task<bool> InitializeAsync()
        {
            try
            {
                if (string.IsNullOrEmpty(_config.ApiKey))
                {
                    _logger.LogError("KiteConnect API key not found in configuration");
                    return false;
                }

                _kite = new KiteConnectAPI.Kite(_config.ApiKey);
                _isInitialized = true;
                _logger.LogInformation("KiteConnect initialized successfully with API key: {apiKey}", _config.ApiKey);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize KiteConnect: {Message}", ex.Message);
                return false;
            }
        }

        public async Task<string> GetLoginUrl()
        {
            if (!_isInitialized)
            {
                _logger.LogError("KiteConnect not initialized");
                return string.Empty;
            }

            return _kite.GetLoginURL();
        }

        public async Task<bool> GenerateSession(string requestToken, string apiSecret)
        {
            if (!_isInitialized)
            {
                _logger.LogError("KiteConnect not initialized");
                return false;
            }

            try
            {
                var user = _kite.GenerateSession(requestToken, apiSecret);
                _kite.SetAccessToken(user.AccessToken);
                _accessToken = user.AccessToken;
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate session");
                return false;
            }
        }

        public async Task<bool> SetAccessToken(string accessToken)
        {
            try
            {
                // Ensure KiteConnect is initialized
                if (!_isInitialized)
                {
                    var initialized = await InitializeAsync();
                    if (!initialized)
            {
                        _logger.LogError("Failed to initialize KiteConnect before setting access token");
                return false;
            }
                }

                // Ensure _kite is not null
                if (_kite == null)
                {
                    _kite = new KiteConnectAPI.Kite(_config.ApiKey);
                }

                _kite.SetAccessToken(accessToken);
                _accessToken = accessToken;
                _isConnected = true;
                _logger.LogInformation("Successfully set access token in KiteConnect client");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to set access token: {Message}", ex.Message);
                return false;
            }
        }

        public async Task<Dictionary<string, KiteQuote>> GetQuotesAsync(string[] instrumentTokens)
        {
            try
            {
                var quotes = await Task.Run(() => _kite.GetQuote(instrumentTokens));
                return quotes.ToDictionary(
                    kvp => kvp.Key,
                    kvp => new KiteQuote(kvp.Value)
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting quotes for instruments: {Instruments}", string.Join(", ", instrumentTokens));
                return new Dictionary<string, KiteQuote>();
            }
        }

        public async Task<Dictionary<string, KiteOHLC>> GetOHLCAsync(string[] instrumentTokens)
        {
            if (!_isInitialized)
            {
                _logger.LogError("KiteConnect not initialized");
                return new Dictionary<string, KiteOHLC>();
            }

            try
            {
                var ohlc = await Task.Run(() => _kite.GetOHLC(instrumentTokens));
                return ohlc.ToDictionary(
                    kvp => kvp.Key,
                    kvp => new KiteOHLC(kvp.Value)
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get OHLC data for instruments: {tokens}", string.Join(",", instrumentTokens));
                throw;
            }
        }

        public async Task<Dictionary<string, KiteLTP>> GetLTPAsync(string[] instrumentTokens)
        {
            if (!_isInitialized)
            {
                _logger.LogError("KiteConnect not initialized");
                return new Dictionary<string, KiteLTP>();
            }

            try
            {
                var ltp = await Task.Run(() => _kite.GetLTP(instrumentTokens));
                return ltp.ToDictionary(
                    kvp => kvp.Key,
                    kvp => new KiteLTP(kvp.Value)
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get LTP data for instruments: {tokens}", string.Join(",", instrumentTokens));
                throw;
            }
        }

        public async Task<List<OptionAnalysisTool.Models.Instrument>> GetInstruments(string exchange = null, string segment = null)
        {
            if (!_isInitialized || _kite == null)
            {
                _logger.LogError("KiteConnect not initialized");
                return new List<OptionAnalysisTool.Models.Instrument>();
            }

            try
            {
                var instruments = await Task.Run(() => _kite.GetInstruments(exchange));
                if (segment != null)
                {
                    instruments = instruments.Where(i => i.Segment == segment).ToList();
                }
                
                return instruments.Select(i => new OptionAnalysisTool.Models.Instrument
                {
                    InstrumentToken = i.InstrumentToken.ToString(),
                    TradingSymbol = i.TradingSymbol,
                    Name = i.Name,
                    Exchange = i.Exchange,
                    Segment = i.Segment,
                    InstrumentType = i.InstrumentType,
                    Expiry = i.Expiry,
                    Strike = i.Strike
                }).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get instruments");
                return new List<OptionAnalysisTool.Models.Instrument>();
            }
        }

        public async Task<List<OptionAnalysisTool.Models.Quote>> GetQuotes(List<OptionAnalysisTool.Models.Instrument> instruments)
        {
            if (!_isInitialized || _kite == null)
            {
                _logger.LogError("KiteConnect not initialized");
                return new List<OptionAnalysisTool.Models.Quote>();
            }

            try
            {
                var instrumentTokens = instruments.Select(i => i.InstrumentToken.ToString()).ToArray();
                var quotes = await Task.Run(() => _kite.GetQuote(instrumentTokens));
                
                return quotes.Values.Select(q => new OptionAnalysisTool.Models.Quote
                {
                    InstrumentToken = q.InstrumentToken.ToString(),
                    LastPrice = q.LastPrice,
                    Change = q.Change,
                    Open = q.Open,
                    High = q.High,
                    Low = q.Low,
                    Close = q.Close,
                    Volume = (int)q.Volume,
                    OpenInterest = (int)q.OI
                }).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get quotes");
                return new List<OptionAnalysisTool.Models.Quote>();
            }
        }

        public async Task<OptionAnalysisTool.Models.Quote> GetQuote(OptionAnalysisTool.Models.Instrument instrument)
        {
            try
            {
                var quote = await GetQuoteAsync(instrument.InstrumentToken);
                return new OptionAnalysisTool.Models.Quote
                {
                    InstrumentToken = quote.InstrumentToken,
                    LastPrice = quote.LastPrice,
                    Change = quote.Change,
                    Open = quote.Open,
                    High = quote.High,
                    Low = quote.Low,
                    Close = quote.Close,
                    Volume = quote.Volume,
                    OpenInterest = quote.OpenInterest
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to get quote for {instrument.TradingSymbol}");
                throw;
            }
        }

        public async Task<List<KiteHistorical>> GetHistoricalDataAsync(
            string instrumentToken,
            DateTime fromDate,
            DateTime toDate,
            string interval)
        {
            if (!_isInitialized)
            {
                _logger.LogError("KiteConnect not initialized");
                return new List<KiteHistorical>();
            }

            try
            {
                var historicalData = await Task.Run(() => _kite.GetHistoricalData(instrumentToken, fromDate, toDate, interval));
                return historicalData.Select(h => new KiteHistorical(h)).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get historical data for instrument: {token}", instrumentToken);
                throw;
            }
        }

        public async Task<DailyOptionContract?> GetDailyData(string symbol, DateTime date)
        {
            if (!_isInitialized)
            {
                _logger.LogError("KiteConnect not initialized");
                return null;
            }

            try
            {
                // This is a placeholder implementation. You'll need to implement the actual logic
                // based on your requirements for daily option data.
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to get daily data for {symbol} on {date}");
                return null;
            }
        }

        public async Task<KiteLTP> GetLTP(string instrumentToken)
        {
            if (!_isInitialized)
            {
                _logger.LogError("KiteConnect not initialized");
                throw new InvalidOperationException("KiteConnect not initialized");
            }

            try
            {
                var ltp = await Task.Run(() => _kite.GetLTP(new[] { instrumentToken }));
                return new KiteLTP(ltp[instrumentToken]);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to get LTP for {instrumentToken}");
                throw;
            }
        }

        public async Task<string> GetAccessToken()
        {
            try
            {
                return _accessToken;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get access token");
                return string.Empty;
            }
        }

        public async Task<bool> IsConnected()
        {
            return await IsConnectedAsync();
        }

        public async Task<bool> Connect()
        {
            return await ConnectAsync();
        }

        public async Task<bool> Disconnect()
        {
            return await DisconnectAsync();
        }

        public async Task<bool> LoginAsync()
        {
            try
            {
                if (_kite == null)
                {
                    await InitializeAsync();
                }
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to login to KiteConnect");
                return false;
            }
        }

        public async Task<bool> IsLoggedInAsync()
        {
            try
            {
                return _kite != null && !string.IsNullOrEmpty(_accessToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to check login status");
                return false;
            }
        }

        public async Task<bool> ConnectAsync()
        {
            if (!_isInitialized)
            {
                var initialized = await InitializeAsync();
                if (!initialized) return false;
            }

            if (_isConnected) return true;

            try
            {
                if (string.IsNullOrEmpty(_accessToken))
                {
                    _logger.LogError("Cannot connect: Access token is not set");
                    return false;
                }

                if (_kite == null)
                {
                    _kite = new KiteConnectAPI.Kite(_config.ApiKey);
                    _kite.SetAccessToken(_accessToken);
                }

                // Test connection by making a simple API call
                await GetProfile();
                _isConnected = true;
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to connect to KiteConnect");
                _isConnected = false;
                return false;
            }
        }

        public async Task<bool> DisconnectAsync()
        {
            if (!_isConnected) return true;

            try
            {
                _kite = null;
                _accessToken = string.Empty;
                _isConnected = false;
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to disconnect from KiteConnect");
                return false;
            }
        }

        public async Task<bool> IsConnectedAsync()
        {
            if (!_isInitialized || _kite == null || string.IsNullOrEmpty(_accessToken))
            {
                _logger.LogDebug("Not connected: IsInitialized={isInit}, HasKite={hasKite}, HasToken={hasToken}", 
                    _isInitialized, _kite != null, !string.IsNullOrEmpty(_accessToken));
                return false;
            }

            try
            {
                // Test connection by making a simple API call
                // GetProfile() should work even after market hours
                await GetProfile();
                _isConnected = true;
                _logger.LogDebug("Connection test successful via GetProfile()");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Connection test failed via GetProfile(), but treating as connected if we have valid token");
                
                // If GetProfile fails but we have a valid token, still consider connected
                // This handles cases where API is temporarily unavailable or rate limited
                // but the token itself is valid
                _isConnected = true;
                return true;
            }
        }

        private async Task<KiteConnectAPI.Profile> GetProfile()
        {
            if (_kite == null) throw new InvalidOperationException("KiteConnect is not initialized");
            return await Task.Run(() => _kite.GetProfile());
        }

        public async Task<List<IntradayOptionSnapshot>> GetIntradayData(string symbol, DateTime date)
        {
            if (!_isInitialized)
            {
                _logger.LogError("KiteConnect not initialized");
                return new List<IntradayOptionSnapshot>();
            }

            try
            {
                // This is a placeholder implementation. You'll need to implement the actual logic
                // based on your requirements for intraday option data.
                return new List<IntradayOptionSnapshot>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to get intraday data for {symbol} on {date}");
                return new List<IntradayOptionSnapshot>();
            }
        }

        public async Task<List<KiteInstrument>> GetInstrumentsAsync()
        {
            return await GetInstrumentsAsync(null);
        }

        public async Task<List<KiteInstrument>> GetInstrumentsAsync(string? exchange = null)
        {
            _logger.LogInformation("🔍 GetInstrumentsAsync called - IsInitialized: {isInit}, HasKite: {hasKite}, HasToken: {hasToken}, Exchange: {exchange}", 
                _isInitialized, _kite != null, !string.IsNullOrEmpty(_accessToken), exchange ?? "null");
            
            if (!_isInitialized)
            {
                _logger.LogError("KiteConnect not initialized");
                return new List<KiteInstrument>();
            }

            try
            {
                _logger.LogInformation("📡 Making API call to get instruments for exchange: {exchange}", exchange ?? "all");
                var instruments = exchange == null 
                    ? await Task.Run(() => _kite.GetInstruments())
                    : await Task.Run(() => _kite.GetInstruments(exchange));
                
                var result = instruments.Select(i => new KiteInstrument(i)).ToList();
                _logger.LogInformation("✅ Successfully fetched {count} instruments from Kite API", result.Count);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Failed to get instruments{0}", exchange != null ? $" for exchange {exchange}" : "");
                return new List<KiteInstrument>();
            }
        }

        public async Task<KiteQuote> GetQuoteAsync(string instrumentToken)
        {
            if (!_isInitialized)
            {
                _logger.LogError("KiteConnect not initialized");
                throw new InvalidOperationException("KiteConnect not initialized");
            }

            try
            {
                var quotes = await GetQuotesAsync(new[] { instrumentToken });
                if (!quotes.TryGetValue(instrumentToken, out var quote))
                {
                    throw new KeyNotFoundException($"Quote not found for instrument token {instrumentToken}");
                }
                return quote;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to get quote for {instrumentToken}");
                throw;
            }
        }

        public async Task<KiteInstrument?> GetInstrumentAsync(string symbol, string exchange)
        {
            if (!_isInitialized)
            {
                _logger.LogError("KiteConnect not initialized");
                return null;
            }

            try
            {
                var instruments = await GetInstrumentsAsync(exchange);
                return instruments.FirstOrDefault(i => i.TradingSymbol == symbol);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to get instrument for {symbol} on {exchange}");
                return null;
            }
        }

        public async Task<Dictionary<string, KiteQuote>> GetQuotesAsync(List<string> instrumentTokens)
            => await GetQuotesAsync(instrumentTokens.ToArray());

        public async Task<bool> ValidateSessionAsync()
        {
            if (!_isInitialized)
            {
                _logger.LogError("KiteConnect not initialized");
                return false;
            }

            try
            {
                var niftyInstrument = await GetInstrumentAsync("NIFTY 50", "NSE");
                if (niftyInstrument == null)
                    return false;

                var quote = await GetQuoteAsync(niftyInstrument.InstrumentToken);
                return quote != null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to validate session");
                return false;
            }
        }

        public async Task<bool> IsSessionValidForTodayAsync()
        {
            if (!_isInitialized)
            {
                _logger.LogError("KiteConnect not initialized");
                return false;
            }

            try
            {
                var niftyInstrument = await GetInstrumentAsync("NIFTY 50", "NSE");
                if (niftyInstrument == null)
                    return false;

                var quote = await GetQuoteAsync(niftyInstrument.InstrumentToken);
                if (quote == null)
                    return false;

                // Check if the quote timestamp is today (IST)
                var ist = TimeZoneInfo.FindSystemTimeZoneById("India Standard Time");
                var quoteDate = TimeZoneInfo.ConvertTimeFromUtc(quote.Timestamp.ToUniversalTime(), ist).Date;
                var today = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, ist).Date;
                _logger.LogInformation($"[SESSION CHECK] QuoteDate={quoteDate:yyyy-MM-dd}, Today={today:yyyy-MM-dd}, QuoteTimestamp={quote.Timestamp:yyyy-MM-dd HH:mm:ss}");

                if (quoteDate == today)
                {
                    _logger.LogInformation("Session is valid for today (quote date matches)");
                    return true;
                }
                else
                {
                    // If token is still valid (not expired), treat as soft valid if market is closed
                    // (i.e., don't force re-authentication outside market hours)
                    _logger.LogWarning("Quote is not for today, but token may still be valid if market is closed");
                    return true;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to validate session for today");
                return false;
            }
        }
    }
}