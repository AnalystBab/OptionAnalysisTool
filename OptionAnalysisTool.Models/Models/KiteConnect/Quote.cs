using System;

namespace OptionAnalysisTool.Models.KiteConnect
{
    public class Quote
    {
        public long InstrumentToken { get; set; }
        public decimal LastPrice { get; set; }
        public decimal Open { get; set; }
        public decimal High { get; set; }
        public decimal Low { get; set; }
        public decimal Close { get; set; }
        public decimal Change { get; set; }
        public long Volume { get; set; }
        public long Oi { get; set; }
        public decimal LowerCircuitLimit { get; set; }
        public decimal UpperCircuitLimit { get; set; }
        public decimal ImpliedVolatility { get; set; }
        public DateTime Timestamp { get; set; }

        public Quote()
        {
            Timestamp = DateTime.UtcNow;
        }
    }
} 