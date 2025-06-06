using System;
using System.ComponentModel.DataAnnotations;

namespace OptionAnalysisTool.Models
{
    /// <summary>
    /// 📈 SPOT DATA - REAL-TIME INDEX PRICES
    /// Stores real-time spot prices for indices with circuit monitoring
    /// </summary>
    public class SpotData
    {
        public int Id { get; set; }
        
        [Required]
        [StringLength(50)]
        public string Symbol { get; set; } = string.Empty; // NIFTY, BANKNIFTY, etc.
        
        [Required]
        [StringLength(20)]
        public string Exchange { get; set; } = string.Empty; // NSE, BSE
        
        // Price Data
        public decimal LastPrice { get; set; }
        public decimal Open { get; set; }
        public decimal High { get; set; }
        public decimal Low { get; set; }
        public decimal Close { get; set; }
        public decimal Change { get; set; }
        public decimal PercentageChange { get; set; }
        
        // Volume
        public long Volume { get; set; }
        
        // Circuit Limits for Index
        public decimal LowerCircuitLimit { get; set; }
        public decimal UpperCircuitLimit { get; set; }
        public string CircuitStatus { get; set; } = "Normal";
        
        // Timestamps
        public DateTime Timestamp { get; set; }
        public DateTime CapturedAt { get; set; }
        
        // Data Quality
        public bool IsValidData { get; set; } = true;
        public string ValidationMessage { get; set; } = string.Empty;
        
        public SpotData()
        {
            Timestamp = DateTime.UtcNow;
            CapturedAt = DateTime.UtcNow;
        }
    }
} 