using System;

namespace OptionAnalysisTool.Common.Models
{
    public class OptionSnapshot
    {
        public long Id { get; set; }
        public string InstrumentToken { get; set; }
        public string Symbol { get; set; }
        public decimal LastPrice { get; set; }
        public decimal Change { get; set; }
        public decimal Volume { get; set; }
        public decimal OpenInterest { get; set; }
        public decimal BuyQuantity { get; set; }
        public decimal SellQuantity { get; set; }
        public decimal ImpliedVolatility { get; set; }
        public DateTime Timestamp { get; set; }

        public OptionSnapshot()
        {
            InstrumentToken = string.Empty;
            Symbol = string.Empty;
            Timestamp = DateTime.UtcNow;
        }
    }
} 