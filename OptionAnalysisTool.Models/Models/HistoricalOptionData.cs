using System;
using System.ComponentModel.DataAnnotations;

namespace OptionAnalysisTool.Models
{
    /// <summary>
    /// 📊 HISTORICAL OPTION DATA - EOD STORAGE
    /// Stores end-of-day option data for ALL INDEX OPTIONS automatically
    /// </summary>
    public class HistoricalOptionData
    {
        public int Id { get; set; }
        
        [Required]
        [StringLength(50)]
        public string InstrumentToken { get; set; } = string.Empty;
        
        [Required]
        [StringLength(100)]
        public string Symbol { get; set; } = string.Empty;
        
        [Required]
        [StringLength(50)]
        public string UnderlyingSymbol { get; set; } = string.Empty;
        
        public decimal StrikePrice { get; set; }
        
        [Required]
        [StringLength(10)]
        public string OptionType { get; set; } = string.Empty; // CE or PE
        
        public DateTime ExpiryDate { get; set; }
        public DateTime TradingDate { get; set; }
        
        // OHLC Data
        public decimal Open { get; set; }
        public decimal High { get; set; }
        public decimal Low { get; set; }
        public decimal Close { get; set; }
        public decimal Change { get; set; }
        public decimal PercentageChange { get; set; }
        
        // Volume and Interest
        public long Volume { get; set; }
        public long OpenInterest { get; set; }
        public long OIChange { get; set; }
        
        // Circuit Limits - STORED DAILY
        public decimal LowerCircuitLimit { get; set; }
        public decimal UpperCircuitLimit { get; set; }
        public bool CircuitLimitChanged { get; set; }
        
        // Greeks (if available)
        public decimal ImpliedVolatility { get; set; }
        public decimal? Delta { get; set; }
        public decimal? Gamma { get; set; }
        public decimal? Theta { get; set; }
        public decimal? Vega { get; set; }
        
        // Underlying Context
        public decimal UnderlyingClose { get; set; }
        public decimal UnderlyingChange { get; set; }
        
        // Timestamps
        public DateTime CapturedAt { get; set; }
        public DateTime LastUpdated { get; set; }
        
        // Data Quality
        public bool IsValidData { get; set; } = true;
        public string ValidationMessage { get; set; } = string.Empty;
        public string DataSource { get; set; } = "Kite";
        
        public HistoricalOptionData()
        {
            CapturedAt = DateTime.UtcNow;
            LastUpdated = DateTime.UtcNow;
        }
    }
} 