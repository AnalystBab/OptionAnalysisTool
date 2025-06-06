using System;

namespace OptionAnalysisTool.Common.Models
{
    public class OpenInterestChange
    {
        public int Id { get; set; }
        public int ContractId { get; set; }
        public required string InstrumentToken { get; set; }
        public required string Symbol { get; set; }
        public long OldOpenInterest { get; set; }
        public long NewOpenInterest { get; set; }
        public decimal PercentageChange { get; set; }
        public decimal LastPrice { get; set; }
        public DateTime Timestamp { get; set; }
        public required string InstrumentType { get; set; }
        public decimal StrikePrice { get; set; }
        public DateTime ExpiryDate { get; set; }
        public long Volume { get; set; }
        public decimal TotalTradedValue { get; set; }

        // Navigation property
        public virtual OptionContract Contract { get; set; }
    }
} 