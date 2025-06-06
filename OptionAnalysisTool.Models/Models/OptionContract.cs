using System;
using System.ComponentModel.DataAnnotations;

namespace OptionAnalysisTool.Models
{
    public class OptionContract
    {
        public int Id { get; set; }
        
        [Required]
        [StringLength(50)]
        public required string InstrumentToken { get; set; }
        
        [Required]
        [StringLength(50)]
        public required string Symbol { get; set; }
        
        [Required]
        [StringLength(50)]
        public required string UnderlyingSymbol { get; set; }
        
        public decimal StrikePrice { get; set; }
        
        [Required]
        [StringLength(10)]
        public required string OptionType { get; set; }
        
        public DateTime ExpiryDate { get; set; }
        public decimal LastPrice { get; set; }
        public decimal Open { get; set; }
        public decimal High { get; set; }
        public decimal Low { get; set; }
        public decimal Close { get; set; }
        public decimal Change { get; set; }
        public int Volume { get; set; }
        public int OpenInterest { get; set; }
        public decimal ImpliedVolatility { get; set; }
        public DateTime Timestamp { get; set; }
        public DateTime LastUpdated { get; set; }
        public bool IsActive { get; set; }
        public bool IsLiveData { get; set; }

        public OptionContract()
        {
            InstrumentToken = string.Empty;
            Symbol = string.Empty;
            UnderlyingSymbol = string.Empty;
            OptionType = string.Empty;
            Timestamp = DateTime.UtcNow;
            LastUpdated = DateTime.UtcNow;
        }
    }
} 