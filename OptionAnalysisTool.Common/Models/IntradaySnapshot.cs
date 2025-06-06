using System;

namespace OptionAnalysisTool.Common.Models
{
    public class IntradaySnapshot
    {
        public int Id { get; set; }
        public string InstrumentToken { get; set; } = string.Empty;
        public string Symbol { get; set; } = string.Empty;
        public string UnderlyingSymbol { get; set; } = string.Empty;
        public decimal StrikePrice { get; set; }
        public string OptionType { get; set; } = string.Empty;
        public DateTime ExpiryDate { get; set; }
        public decimal LastPrice { get; set; }
        public decimal Open { get; set; }
        public decimal High { get; set; }
        public decimal Low { get; set; }
        public decimal Close { get; set; }
        public decimal Change { get; set; }
        public decimal PctChange { get; set; }
        public int Volume { get; set; }
        public int OpenInterest { get; set; }
        public decimal LowerCircuitLimit { get; set; }
        public decimal UpperCircuitLimit { get; set; }
        public decimal ImpliedVolatility { get; set; }
        public DateTime Timestamp { get; set; }
        public DateTime LastUpdated { get; set; }
        public string CircuitLimitStatus { get; set; } = string.Empty;
        public string ValidationMessage { get; set; } = string.Empty;
        public string TradingStatus { get; set; } = string.Empty;
        public bool IsValidData { get; set; }
        public string InstrumentType { get; set; } = string.Empty;
    }
} 