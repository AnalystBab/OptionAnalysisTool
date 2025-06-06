using System;

namespace OptionAnalysisTool.KiteConnect
{
    public class Quote
    {
        public long InstrumentToken { get; set; }
        public DateTime Timestamp { get; set; }
        public decimal LastPrice { get; set; }
        public decimal LastQuantity { get; set; }
        public decimal AveragePrice { get; set; }
        public decimal Volume { get; set; }
        public decimal BuyQuantity { get; set; }
        public decimal SellQuantity { get; set; }
        public decimal Open { get; set; }
        public decimal High { get; set; }
        public decimal Low { get; set; }
        public decimal Close { get; set; }
        public decimal Change { get; set; }
        public decimal LowerCircuitLimit { get; set; }
        public decimal UpperCircuitLimit { get; set; }
        public decimal OI { get; set; }  // Open Interest
    }
} 