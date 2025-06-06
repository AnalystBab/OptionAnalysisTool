using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using OptionAnalysisTool.Models;
using OptionAnalysisTool.Common.Data;
using Microsoft.Extensions.Logging;

namespace OptionAnalysisTool.Common.Repositories
{
    public class MarketDataRepository : IMarketDataRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<MarketDataRepository> _logger;

        public MarketDataRepository(ApplicationDbContext context, ILogger<MarketDataRepository> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<List<IntradayOptionSnapshot>> GetIntradaySnapshotsAsync(string symbol, DateTime startTime, DateTime endTime)
        {
            return await _context.IntradayOptionSnapshots
                .Where(s => s.Symbol == symbol && s.Timestamp >= startTime && s.Timestamp <= endTime)
                .OrderBy(s => s.Timestamp)
                .ToListAsync();
        }

        public async Task<List<DailyOptionContract>> GetDailyContractsAsync(string symbol, DateTime startDate, DateTime endDate)
        {
            return await _context.DailyOptionContracts
                .Where(c => c.Symbol == symbol && c.TradingDate >= startDate && c.TradingDate <= endDate)
                .OrderBy(c => c.TradingDate)
                .ToListAsync();
        }

        public async Task<List<CircuitLimitChange>> GetCircuitLimitChangesAsync(string symbol, DateTime startTime, DateTime endTime)
        {
            return await _context.CircuitLimitChanges
                .Where(c => c.Symbol == symbol && c.Timestamp >= startTime && c.Timestamp <= endTime)
                .OrderBy(c => c.Timestamp)
                .ToListAsync();
        }

        public async Task<DailyOptionContract?> GetDailyContractAsync(string symbol, DateTime date)
        {
            return await _context.DailyOptionContracts
                .FirstOrDefaultAsync(c => c.Symbol == symbol && c.TradingDate.Date == date.Date);
        }

        public async Task<List<DailyOptionContract>> GetDailyContractsByUnderlyingAsync(string underlyingSymbol, DateTime startDate, DateTime endDate)
        {
            return await _context.DailyOptionContracts
                .Where(c => c.UnderlyingSymbol == underlyingSymbol && c.TradingDate >= startDate && c.TradingDate <= endDate)
                .OrderBy(c => c.TradingDate)
                .ToListAsync();
        }

        public async Task<List<IntradayOptionSnapshot>> GetIntradaySnapshotsByUnderlyingAsync(string underlyingSymbol, DateTime startTime, DateTime endTime)
        {
            return await _context.IntradayOptionSnapshots
                .Where(s => s.UnderlyingSymbol == underlyingSymbol && s.Timestamp >= startTime && s.Timestamp <= endTime)
                .OrderBy(s => s.Timestamp)
                .ToListAsync();
        }

        public async Task SaveIntradaySnapshotAsync(IntradayOptionSnapshot snapshot)
        {
            var existingSnapshot = await _context.IntradayOptionSnapshots
                .FirstOrDefaultAsync(s => s.Symbol == snapshot.Symbol && s.Timestamp == snapshot.Timestamp);

            if (existingSnapshot == null)
            {
                await _context.IntradayOptionSnapshots.AddAsync(snapshot);
            }
            else
            {
                _context.Entry(existingSnapshot).CurrentValues.SetValues(snapshot);
            }

            await _context.SaveChangesAsync();
        }

        public async Task SaveIntradaySnapshotsAsync(List<IntradayOptionSnapshot> snapshots)
        {
            foreach (var snapshot in snapshots)
            {
                var existingSnapshot = await _context.IntradayOptionSnapshots
                    .FirstOrDefaultAsync(s => s.Symbol == snapshot.Symbol && s.Timestamp == snapshot.Timestamp);

                if (existingSnapshot == null)
                {
                    await _context.IntradayOptionSnapshots.AddAsync(snapshot);
                }
                else
                {
                    _context.Entry(existingSnapshot).CurrentValues.SetValues(snapshot);
                }
            }

            await _context.SaveChangesAsync();
        }

        public async Task SaveDailyContractAsync(DailyOptionContract contract)
        {
            var existingContract = await _context.DailyOptionContracts
                .FirstOrDefaultAsync(c => c.Symbol == contract.Symbol && c.TradingDate.Date == contract.TradingDate.Date);

            if (existingContract == null)
            {
                await _context.DailyOptionContracts.AddAsync(contract);
            }
            else
            {
                _context.Entry(existingContract).CurrentValues.SetValues(contract);
            }

            await _context.SaveChangesAsync();
        }

        public async Task SaveCircuitLimitChangeAsync(CircuitLimitChange change)
        {
            await _context.CircuitLimitChanges.AddAsync(change);
            await _context.SaveChangesAsync();
        }

        public async Task<IndexSnapshot> GetLatestIndexSnapshotAsync(string symbol)
        {
            return await _context.IndexSnapshots
                .Where(s => s.Symbol == symbol)
                .OrderByDescending(s => s.Timestamp)
                .FirstOrDefaultAsync();
        }

        public async Task<List<IndexSnapshot>> GetIndexSnapshotsAsync(string symbol, DateTime startDate, DateTime endDate)
        {
            return await _context.IndexSnapshots
                .Where(s => s.Symbol == symbol && s.Timestamp >= startDate && s.Timestamp <= endDate)
                .OrderBy(s => s.Timestamp)
                .ToListAsync();
        }

        public async Task<bool> AddIndexSnapshotAsync(IndexSnapshot snapshot)
        {
            try
            {
                await _context.IndexSnapshots.AddAsync(snapshot);
                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> AddIndexSnapshotsAsync(IEnumerable<IndexSnapshot> snapshots)
        {
            try
            {
                await _context.IndexSnapshots.AddRangeAsync(snapshots);
                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<OptionContract> GetLatestOptionContractAsync(string symbol, decimal strikePrice, string optionType)
        {
            return await _context.OptionContracts
                .Where(c => c.Symbol == symbol && c.StrikePrice == strikePrice && c.OptionType == optionType)
                .OrderByDescending(c => c.Timestamp)
                .FirstOrDefaultAsync();
        }

        public async Task<List<OptionContract>> GetOptionContractsAsync(string symbol, DateTime startDate, DateTime endDate)
        {
            return await _context.OptionContracts
                .Where(c => c.Symbol == symbol && c.Timestamp >= startDate && c.Timestamp <= endDate)
                .OrderBy(c => c.Timestamp)
                .ToListAsync();
        }

        public async Task<bool> AddOptionContractAsync(OptionContract contract)
        {
            try
            {
                await _context.OptionContracts.AddAsync(contract);
                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> AddOptionContractsAsync(IEnumerable<OptionContract> contracts)
        {
            try
            {
                await _context.OptionContracts.AddRangeAsync(contracts);
                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<CircuitBreaker> GetLatestCircuitBreakerAsync(string symbol)
        {
            return await _context.CircuitBreakers
                .Where(c => c.Symbol == symbol)
                .OrderByDescending(c => c.TradingDate)
                .FirstOrDefaultAsync();
        }

        public async Task<List<CircuitBreaker>> GetCircuitBreakersAsync(string symbol, DateTime startDate, DateTime endDate)
        {
            return await _context.CircuitBreakers
                .Where(c => c.Symbol == symbol && c.TradingDate >= startDate && c.TradingDate <= endDate)
                .OrderBy(c => c.TradingDate)
                .ToListAsync();
        }

        public async Task<bool> AddCircuitBreakerAsync(CircuitBreaker circuitBreaker)
        {
            try
            {
                await _context.CircuitBreakers.AddAsync(circuitBreaker);
                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<DataDownloadStatus> GetDownloadStatusAsync(string symbol, string dataType)
        {
            return await _context.DataDownloadStatuses
                .FirstOrDefaultAsync(s => s.Symbol == symbol && s.DataType == dataType);
        }

        public async Task<List<DataDownloadStatus>> GetAllDownloadStatusesAsync()
        {
            return await _context.DataDownloadStatuses
                .OrderBy(s => s.Symbol)
                .ToListAsync();
        }

        public async Task UpdateDownloadStatusAsync(DataDownloadStatus status)
        {
            var existingStatus = await _context.DataDownloadStatuses
                .FirstOrDefaultAsync(s => s.Symbol == status.Symbol && s.DataType == status.DataType);

            if (existingStatus != null)
            {
                _context.Entry(existingStatus).CurrentValues.SetValues(status);
                await _context.SaveChangesAsync();
            }
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }

        public async Task<bool> SaveHistoricalPriceAsync(HistoricalPrice historicalPrice)
        {
            try
            {
                var existingPrice = await _context.HistoricalPrices
                    .FirstOrDefaultAsync(p => p.Symbol == historicalPrice.Symbol && 
                                            p.Exchange == historicalPrice.Exchange && 
                                            p.Date == historicalPrice.Date);

                if (existingPrice == null)
                {
                    await _context.HistoricalPrices.AddAsync(historicalPrice);
                }
                else
                {
                    _context.Entry(existingPrice).CurrentValues.SetValues(historicalPrice);
                }

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving historical price for {symbol}", historicalPrice.Symbol);
                return false;
            }
        }

        public async Task<bool> SaveHistoricalPricesAsync(IEnumerable<HistoricalPrice> historicalPrices)
        {
            try
            {
                foreach (var price in historicalPrices)
                {
                    var existingPrice = await _context.HistoricalPrices
                        .FirstOrDefaultAsync(p => p.Symbol == price.Symbol && 
                                                p.Exchange == price.Exchange && 
                                                p.Date == price.Date);

                    if (existingPrice == null)
                    {
                        await _context.HistoricalPrices.AddAsync(price);
                    }
                    else
                    {
                        _context.Entry(existingPrice).CurrentValues.SetValues(price);
                    }
                }

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving historical prices");
                return false;
            }
        }

        public async Task<bool> SaveStockHistoricalPricesAsync(IEnumerable<StockHistoricalPrice> stockHistoricalPrices)
        {
            try
            {
                foreach (var price in stockHistoricalPrices)
                {
                    var existingPrice = await _context.StockHistoricalPrices
                        .FirstOrDefaultAsync(p => p.Symbol == price.Symbol && 
                                                p.Exchange == price.Exchange && 
                                                p.Date == price.Date);

                    if (existingPrice == null)
                    {
                        await _context.StockHistoricalPrices.AddAsync(price);
                    }
                    else
                    {
                        _context.Entry(existingPrice).CurrentValues.SetValues(price);
                    }
                }

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving stock historical prices");
                return false;
            }
        }

        public async Task SaveOptionMonitoringStatsAsync(OptionMonitoringStats stats)
        {
            try
            {
                await _context.OptionMonitoringStats.AddAsync(stats);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving option monitoring stats");
                throw;
            }
        }

        public async Task<int> GetActiveContractsCountAsync(string symbol)
        {
            try
            {
                return await _context.IntradayOptionSnapshots
                    .Where(s => s.UnderlyingSymbol == symbol)
                    .Select(s => s.InstrumentToken)
                    .Distinct()
                    .CountAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting active contracts count for {symbol}", symbol);
                return 0;
            }
        }
    }
} 