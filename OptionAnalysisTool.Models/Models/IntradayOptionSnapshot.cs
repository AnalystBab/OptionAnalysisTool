using System;
using System.ComponentModel.DataAnnotations;

namespace OptionAnalysisTool.Models
{
    /// <summary>
    /// ⚡ INTRADAY OPTION SNAPSHOT - REAL-TIME DATA
    /// Captures real-time option data with circuit limit tracking
    /// </summary>
    public class IntradayOptionSnapshot
    {
        public int Id { get; set; }
        
        [Required]
        [StringLength(50)]
        public string InstrumentToken { get; set; } = string.Empty;
        
        [Required]
        [StringLength(100)]
        public string TradingSymbol { get; set; } = string.Empty;
        
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
        
        // Price Data
        public decimal LastPrice { get; set; }
        public decimal Open { get; set; }
        public decimal High { get; set; }
        public decimal Low { get; set; }
        public decimal Close { get; set; }
        public decimal Change { get; set; }
        
        // Volume and Interest
        public long Volume { get; set; }
        public long OpenInterest { get; set; }
        
        // Circuit Limits - VERY IMPORTANT
        public decimal LowerCircuitLimit { get; set; }
        public decimal UpperCircuitLimit { get; set; }
        public string CircuitLimitStatus { get; set; } = "Normal"; // Normal, Upper Circuit, Lower Circuit
        
        [StringLength(200)]
        public string ChangeReason { get; set; } = string.Empty;
        
        // Greeks
        public decimal ImpliedVolatility { get; set; }
        
        // Timestamps
        public DateTime Timestamp { get; set; }
        public DateTime CaptureTime { get; set; }
        public DateTime LastUpdated { get; set; }
        
        // OHLC Date - The date this OHLC data belongs to
        public DateTime OHLCDate { get; set; }
        
        // Data Quality
        public bool IsValidData { get; set; } = true;
        public string ValidationMessage { get; set; } = string.Empty;
        public string TradingStatus { get; set; } = "Normal";
        
        public IntradayOptionSnapshot()
        {
            // Use IST timestamps (same as Kite API format)
            var ist = TimeZoneInfo.FindSystemTimeZoneById("India Standard Time");
            var nowIST = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, ist);
            Timestamp = nowIST;
            CaptureTime = nowIST;
            LastUpdated = nowIST;
        }
    }
} 