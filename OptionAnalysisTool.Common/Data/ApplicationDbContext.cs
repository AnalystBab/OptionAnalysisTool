using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using OptionAnalysisTool.Models;

namespace OptionAnalysisTool.Common.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<IntradayOptionSnapshot> IntradayOptionSnapshots { get; set; }
        public DbSet<DailyOptionContract> DailyOptionContracts { get; set; }
        public DbSet<CircuitLimitChange> CircuitLimitChanges { get; set; }
        public DbSet<IndexSnapshot> IndexSnapshots { get; set; }
        public DbSet<DataDownloadStatus> DataDownloadStatuses { get; set; }
        public DbSet<OptionContract> OptionContracts { get; set; }
        public DbSet<CircuitBreaker> CircuitBreakers { get; set; }
        public DbSet<HistoricalPrice> HistoricalPrices { get; set; }
        public DbSet<StockHistoricalPrice> StockHistoricalPrices { get; set; }
        public DbSet<OptionMonitoringStats> OptionMonitoringStats { get; set; }
        
        // New DbSets for comprehensive option analysis
        public DbSet<Quote> Quotes { get; set; }
        public DbSet<HistoricalOptionData> HistoricalOptionData { get; set; }
        public DbSet<SpotData> SpotData { get; set; }
        public DbSet<CircuitLimitTracker> CircuitLimitTrackers { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Set decimal precision for all entities
            foreach (var property in modelBuilder.Model.GetEntityTypes()
                .SelectMany(t => t.GetProperties())
                .Where(p => p.ClrType == typeof(decimal) || p.ClrType == typeof(decimal?)))
            {
                property.SetPrecision(18);
                property.SetScale(2);
            }

            // Configure existing entities
            ConfigureExistingEntities(modelBuilder);
            
            // 🔥 Configure NEW comprehensive option analysis entities
            ConfigureComprehensiveEntities(modelBuilder);
        }

        private void ConfigureExistingEntities(ModelBuilder modelBuilder)
        {
            // Configure IntradayOptionSnapshot
            modelBuilder.Entity<IntradayOptionSnapshot>()
                .HasIndex(e => new { e.Symbol, e.Timestamp });

            modelBuilder.Entity<IntradayOptionSnapshot>()
                .HasIndex(e => new { e.UnderlyingSymbol, e.Timestamp });

            // Configure DailyOptionContract
            modelBuilder.Entity<DailyOptionContract>()
                .HasIndex(e => new { e.Symbol, e.TradingDate });

            modelBuilder.Entity<DailyOptionContract>()
                .HasIndex(e => new { e.UnderlyingSymbol, e.TradingDate });

            // Configure CircuitLimitChange
            modelBuilder.Entity<CircuitLimitChange>()
                .HasIndex(e => new { e.Symbol, e.Timestamp });

            modelBuilder.Entity<CircuitLimitChange>()
                .HasIndex(e => new { e.InstrumentToken, e.Timestamp });

            // Configure IndexSnapshot
            modelBuilder.Entity<IndexSnapshot>()
                .HasIndex(e => new { e.Symbol, e.Timestamp });

            // Configure DataDownloadStatus
            modelBuilder.Entity<DataDownloadStatus>()
                .HasIndex(e => new { e.Symbol, e.Exchange });

            // Configure OptionContract
            modelBuilder.Entity<OptionContract>()
                .HasIndex(e => new { e.Symbol, e.Timestamp });

            // Configure CircuitBreaker
            modelBuilder.Entity<CircuitBreaker>()
                .HasIndex(e => new { e.Symbol, e.TradingDate });

            // Configure HistoricalPrice
            modelBuilder.Entity<HistoricalPrice>()
                .HasIndex(e => new { e.Symbol, e.Date });

            // Configure StockHistoricalPrice
            modelBuilder.Entity<StockHistoricalPrice>()
                .HasIndex(e => new { e.Symbol, e.Date });

            // Configure OptionMonitoringStats
            modelBuilder.Entity<OptionMonitoringStats>()
                .HasIndex(e => new { e.Symbol, e.Timestamp });
        }

        private void ConfigureComprehensiveEntities(ModelBuilder modelBuilder)
        {
            // 📊 Configure Quote - Real-time quote data
            modelBuilder.Entity<Quote>()
                .HasIndex(e => new { e.InstrumentToken, e.TimeStamp })
                .HasDatabaseName("IX_Quotes_InstrumentToken_TimeStamp");

            modelBuilder.Entity<Quote>()
                .HasIndex(e => e.TimeStamp)
                .HasDatabaseName("IX_Quotes_TimeStamp");

            // 📈 Configure HistoricalOptionData - EOD storage
            modelBuilder.Entity<HistoricalOptionData>()
                .HasIndex(e => new { e.Symbol, e.TradingDate })
                .HasDatabaseName("IX_HistoricalOptionData_Symbol_TradingDate");
            
            modelBuilder.Entity<HistoricalOptionData>()
                .HasIndex(e => new { e.UnderlyingSymbol, e.TradingDate, e.OptionType })
                .HasDatabaseName("IX_HistoricalOptionData_UnderlyingSymbol_TradingDate_OptionType");
            
            modelBuilder.Entity<HistoricalOptionData>()
                .HasIndex(e => new { e.UnderlyingSymbol, e.ExpiryDate, e.StrikePrice, e.OptionType })
                .HasDatabaseName("IX_HistoricalOptionData_UnderlyingSymbol_ExpiryDate_StrikePrice_OptionType");

            // 🎯 Configure SpotData - Real-time index data
            modelBuilder.Entity<SpotData>()
                .HasIndex(e => new { e.Symbol, e.Timestamp })
                .HasDatabaseName("IX_SpotData_Symbol_Timestamp");
            
            modelBuilder.Entity<SpotData>()
                .HasIndex(e => new { e.Symbol, e.Exchange, e.Timestamp })
                .HasDatabaseName("IX_SpotData_Symbol_Exchange_Timestamp");

            // 🔥 Configure CircuitLimitTracker - VERY IMPORTANT for trading logic
            modelBuilder.Entity<CircuitLimitTracker>()
                .HasIndex(e => new { e.Symbol, e.DetectedAt })
                .HasDatabaseName("IX_CircuitLimitTracker_Symbol_DetectedAt");
            
            modelBuilder.Entity<CircuitLimitTracker>()
                .HasIndex(e => new { e.UnderlyingSymbol, e.DetectedAt })
                .HasDatabaseName("IX_CircuitLimitTracker_UnderlyingSymbol_DetectedAt");
            
            modelBuilder.Entity<CircuitLimitTracker>()
                .HasIndex(e => new { e.InstrumentToken, e.DetectedAt })
                .HasDatabaseName("IX_CircuitLimitTracker_InstrumentToken_DetectedAt");

            // Ignore computed properties
            modelBuilder.Entity<CircuitLimitTracker>()
                .Ignore(e => e.HasLowerLimitChanged)
                .Ignore(e => e.HasUpperLimitChanged)
                .Ignore(e => e.HasAnyLimitChanged);

            // Performance indexes for circuit limit analysis
            modelBuilder.Entity<CircuitLimitTracker>()
                .HasIndex(e => e.SeverityLevel)
                .HasDatabaseName("IX_CircuitLimitTracker_SeverityLevel");
        }
    }
} 