using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OptionAnalysisTool.Common.Data;
using OptionAnalysisTool.Models;
using OptionAnalysisTool.KiteConnect.Services;
using KiteConnect;
using OptionAnalysisTool.Common.Repositories;
using Historical = KiteConnect.Historical;

namespace OptionAnalysisTool.Common.Services
{
    public class HistoricalOptionDataService
    {
        private readonly ApplicationDbContext _context;
        private readonly IKiteConnectService _kiteConnectService;
        private readonly ILogger<HistoricalOptionDataService> _logger;
        private readonly IntradayDataService _intradayDataService;
        private readonly IMarketDataRepository _marketDataRepository;

        public HistoricalOptionDataService(
            ApplicationDbContext context,
            IKiteConnectService kiteConnectService,
            IntradayDataService intradayDataService,
            ILogger<HistoricalOptionDataService> logger,
            IMarketDataRepository marketDataRepository)
        {
            _context = context;
            _kiteConnectService = kiteConnectService;
            _intradayDataService = intradayDataService;
            _logger = logger;
            _marketDataRepository = marketDataRepository;
        }

        public async Task ConsolidateDailyData(DateTime tradingDate)
        {
            try
            {
                _logger.LogInformation($"Starting daily data consolidation for {tradingDate:yyyy-MM-dd}");

                // Get all intraday snapshots for the trading date
                var snapshots = await _context.IntradayOptionSnapshots
                    .Where(s => s.Timestamp.Date == tradingDate.Date)
                    .OrderBy(s => s.Symbol)
                    .ThenBy(s => s.Timestamp)
                    .ToListAsync();

                var contractGroups = snapshots.GroupBy(s => new { s.InstrumentToken, s.Symbol });

                foreach (var group in contractGroups)
                {
                    await ProcessDailyContract(group.ToList(), tradingDate);
                }

                _logger.LogInformation($"Completed daily data consolidation for {tradingDate:yyyy-MM-dd}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error consolidating daily data for {tradingDate:yyyy-MM-dd}");
                throw;
            }
        }

        private async Task ProcessDailyContract(List<IntradayOptionSnapshot> snapshots, DateTime tradingDate)
        {
            if (!snapshots.Any()) return;

            var firstSnapshot = snapshots.First();
            var lastSnapshot = snapshots.Last();

            // Check if we already have a record for this contract and date
            var existingContract = await _context.DailyOptionContracts
                .FirstOrDefaultAsync(d => d.InstrumentToken == firstSnapshot.InstrumentToken && 
                                        d.TradingDate.Date == tradingDate.Date);

            if (existingContract != null)
            {
                _logger.LogInformation($"Updating existing daily record for {firstSnapshot.Symbol} on {tradingDate:yyyy-MM-dd}");
                await UpdateExistingContract(existingContract, snapshots);
            }
            else
            {
                _logger.LogInformation($"Creating new daily record for {firstSnapshot.Symbol} on {tradingDate:yyyy-MM-dd}");
                await CreateNewDailyContract(snapshots, tradingDate);
            }
        }

        private async Task UpdateExistingContract(DailyOptionContract contract, List<IntradayOptionSnapshot> snapshots)
        {
            var lastSnapshot = snapshots.Last();
            
            // Update circuit limit information
            var circuitBreaches = snapshots.Where(s => s.CircuitLimitStatus == "Upper Circuit" || s.CircuitLimitStatus == "Lower Circuit").ToList();
            if (circuitBreaches.Any())
            {
                contract.CircuitLimitBreached = true;
                contract.CircuitBreachCount = circuitBreaches.Count;
                var firstBreach = circuitBreaches.First();
                contract.CircuitBreachTime = firstBreach.Timestamp;
                contract.CircuitBreachType = DetermineCircuitBreachType(firstBreach);

                // Create circuit limit change record
                await CreateCircuitLimitChangeRecord(firstBreach);
            }

            // Update current circuit limits
            contract.LowerCircuitLimit = lastSnapshot.LowerCircuitLimit;
            contract.UpperCircuitLimit = lastSnapshot.UpperCircuitLimit;

            // Update OHLC and other data
            UpdateContractData(contract, snapshots);
            
            contract.LastUpdated = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }

        private async Task CreateNewDailyContract(List<IntradayOptionSnapshot> snapshots, DateTime tradingDate)
        {
            var firstSnapshot = snapshots.First();
            var lastSnapshot = snapshots.Last();

            var contract = new DailyOptionContract
            {
                InstrumentToken = firstSnapshot.InstrumentToken,
                Symbol = firstSnapshot.Symbol,
                UnderlyingSymbol = firstSnapshot.UnderlyingSymbol,
                StrikePrice = firstSnapshot.StrikePrice,
                OptionType = firstSnapshot.OptionType,
                ExpiryDate = firstSnapshot.ExpiryDate,
                TradingDate = tradingDate,

                // Circuit Limits
                LowerCircuitLimit = lastSnapshot.LowerCircuitLimit,
                UpperCircuitLimit = lastSnapshot.UpperCircuitLimit,
                CircuitLimitBreached = snapshots.Any(s => s.CircuitLimitStatus == "Upper Circuit" || s.CircuitLimitStatus == "Lower Circuit"),
                CircuitBreachCount = snapshots.Count(s => s.CircuitLimitStatus == "Upper Circuit" || s.CircuitLimitStatus == "Lower Circuit"),
                CircuitBreachType = "None", // Default value

                // Updated data
                OpenInterest = lastSnapshot.OpenInterest,
                TotalVolume = lastSnapshot.Volume,
                ImpliedVolatility = lastSnapshot.ImpliedVolatility,
                LastUpdated = lastSnapshot.Timestamp,
                DataQualityNotes = string.Empty // Initialize with empty string
            };

            if (contract.CircuitLimitBreached)
            {
                var firstBreach = snapshots.First(s => s.CircuitLimitStatus == "Upper Circuit" || s.CircuitLimitStatus == "Lower Circuit");
                contract.CircuitBreachTime = firstBreach.Timestamp;
                contract.CircuitBreachType = DetermineCircuitBreachType(firstBreach);

                // Create circuit limit change record
                await CreateCircuitLimitChangeRecord(firstBreach);
            }

            // Update other data
            UpdateContractData(contract, snapshots);

            contract.CreatedAt = DateTime.UtcNow;
            contract.LastUpdated = DateTime.UtcNow;

            _context.DailyOptionContracts.Add(contract);
            await _context.SaveChangesAsync();
        }

        private void UpdateContractData(DailyOptionContract contract, List<IntradayOptionSnapshot> snapshots)
        {
            var orderedSnapshots = snapshots.OrderBy(s => s.Timestamp).ToList();
            var firstSnapshot = orderedSnapshots.First();
            var lastSnapshot = orderedSnapshots.Last();

            // OHLC
            contract.DayOpen = firstSnapshot.Open;
            contract.DayHigh = snapshots.Max(s => s.High);
            contract.DayLow = snapshots.Min(s => s.Low);
            contract.DayClose = lastSnapshot.Close;

            // Volume and OI
            contract.TotalVolume = snapshots.Sum(s => s.Volume);
            contract.OpenInterest = lastSnapshot.OpenInterest;
            contract.ImpliedVolatility = lastSnapshot.ImpliedVolatility;
            contract.LastUpdated = lastSnapshot.Timestamp;
        }

        private string DetermineCircuitBreachType(IntradayOptionSnapshot snapshot)
        {
            if (snapshot.LastPrice <= snapshot.LowerCircuitLimit)
                return "Lower";
            
            if (snapshot.LastPrice >= snapshot.UpperCircuitLimit)
                return "Upper";

            return "None";
        }

        private async Task CreateCircuitLimitChangeRecord(IntradayOptionSnapshot snapshot)
        {
            // Get the previous circuit limits from the most recent change record
            var previousChange = await _context.CircuitLimitChanges
                .Where(c => c.Symbol == snapshot.Symbol && c.Timestamp < snapshot.Timestamp)
                .OrderByDescending(c => c.Timestamp)
                .FirstOrDefaultAsync();

            var circuitChange = new CircuitLimitChange
            {
                Symbol = snapshot.Symbol,
                OldLowerCircuitLimit = previousChange?.NewLowerCircuitLimit ?? snapshot.LowerCircuitLimit,
                OldUpperCircuitLimit = previousChange?.NewUpperCircuitLimit ?? snapshot.UpperCircuitLimit,
                NewLowerCircuitLimit = snapshot.LowerCircuitLimit,
                NewUpperCircuitLimit = snapshot.UpperCircuitLimit,
                LastPrice = snapshot.LastPrice,
                Timestamp = snapshot.Timestamp,
                ChangeReason = "Circuit Breach"
            };

            _context.CircuitLimitChanges.Add(circuitChange);
            await _context.SaveChangesAsync();
        }

        public async Task<List<DailyOptionContract>> GetHistoricalData(
            string symbol, 
            DateTime startDate, 
            DateTime endDate,
            bool onlyCircuitBreached = false)
        {
            var query = _context.DailyOptionContracts
                .Where(d => d.Symbol == symbol && 
                           d.TradingDate >= startDate && 
                           d.TradingDate <= endDate);

            if (onlyCircuitBreached)
            {
                query = query.Where(d => d.CircuitLimitBreached);
            }

            return await query.OrderBy(d => d.TradingDate).ToListAsync();
        }

        public async Task<List<DailyOptionContract>> GetMissingCircuitLimitData(DateTime tradingDate)
        {
            return await _context.DailyOptionContracts
                .Where(d => d.TradingDate.Date == tradingDate.Date && 
                           (d.LowerCircuitLimit == 0 || d.UpperCircuitLimit == 0))
                .ToListAsync();
        }

        public async Task SaveHistoricalDataAsync(string symbol, DateTime date, List<OptionAnalysisTool.Models.Historical> historicalData)
        {
            try
            {
                _logger.LogInformation($"Saving historical data for {symbol} on {date:yyyy-MM-dd}");

                foreach (var data in historicalData)
                {
                    var snapshot = new IntradayOptionSnapshot
                    {
                        InstrumentToken = symbol, // Using symbol as token since historical data doesn't have token
                        Symbol = symbol,
                        UnderlyingSymbol = symbol.Split('-')[0],
                        StrikePrice = decimal.Parse(symbol.Split('-')[2]),
                        OptionType = symbol.Split('-')[1],
                        ExpiryDate = DateTime.Parse(symbol.Split('-')[3]),
                        LastPrice = data.Close,
                        Open = data.Open,
                        High = data.High,
                        Low = data.Low,
                        Close = data.Close,
                        Volume = (long)data.Volume,
                        OpenInterest = (long)data.OpenInterest,
                        Timestamp = data.Timestamp,
                        LastUpdated = DateTime.UtcNow,
                        CircuitLimitStatus = "Normal",
                        ValidationMessage = string.Empty,
                        TradingStatus = "Normal",
                        IsValidData = true
                    };

                    await _intradayDataService.SaveSnapshotAsync(snapshot);
                }

                _logger.LogInformation($"Successfully saved historical data for {symbol} on {date:yyyy-MM-dd}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error saving historical data for {symbol} on {date:yyyy-MM-dd}");
                throw;
            }
        }

        public async Task<List<DailyOptionContract>> GetDailyContractsAsync(string symbol, DateTime startDate, DateTime endDate)
        {
            try
            {
                return await _context.DailyOptionContracts
                    .Where(d => d.Symbol == symbol && 
                               d.TradingDate >= startDate && 
                               d.TradingDate <= endDate)
                    .OrderBy(d => d.TradingDate)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting daily contracts for {symbol}");
                return new List<DailyOptionContract>();
            }
        }

        public async Task<List<IntradayOptionSnapshot>> GetIntradaySnapshotsAsync(string symbol, DateTime startDate, DateTime endDate)
        {
            try
            {
                return await _context.IntradayOptionSnapshots
                    .Where(s => s.Symbol == symbol && 
                               s.Timestamp >= startDate && 
                               s.Timestamp <= endDate)
                    .OrderBy(s => s.Timestamp)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting intraday snapshots for {symbol}");
                return new List<IntradayOptionSnapshot>();
            }
        }

        public async Task<List<CircuitLimitChange>> GetCircuitLimitChangesAsync(string symbol, DateTime startDate, DateTime endDate)
        {
            try
            {
                return await _context.CircuitLimitChanges
                    .Where(c => c.Symbol == symbol && 
                               c.Timestamp >= startDate && 
                               c.Timestamp <= endDate)
                    .OrderBy(c => c.Timestamp)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting circuit limit changes for {symbol}");
                return new List<CircuitLimitChange>();
            }
        }
    }
} 