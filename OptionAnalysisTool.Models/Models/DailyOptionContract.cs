using System;

namespace OptionAnalysisTool.Models
{
    public class DailyOptionContract
    {
        public int Id { get; set; }

        // Instrument details
        public required string InstrumentToken { get; set; }
        public required string Symbol { get; set; }
        public required string UnderlyingSymbol { get; set; }
        public decimal StrikePrice { get; set; }
        public required string OptionType { get; set; }
        public DateTime ExpiryDate { get; set; }
        public DateTime TradingDate { get; set; }

        // Circuit Limits
        public decimal LowerCircuitLimit { get; set; }
        public decimal UpperCircuitLimit { get; set; }
        public bool CircuitLimitBreached { get; set; }
        public int CircuitBreachCount { get; set; }
        public DateTime? CircuitBreachTime { get; set; }
        public string CircuitBreachType { get; set; } = "None";

        // OHLC Data
        public decimal DayOpen { get; set; }
        public decimal DayHigh { get; set; }
        public decimal DayLow { get; set; }
        public decimal DayClose { get; set; }
        public decimal PreviousClose { get; set; }

        // Volume and OI
        public long TotalVolume { get; set; }
        public long OpenInterest { get; set; }
        public decimal? ImpliedVolatility { get; set; }

        // Metadata
        public string DataQualityNotes { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime LastUpdated { get; set; }
    }
} 