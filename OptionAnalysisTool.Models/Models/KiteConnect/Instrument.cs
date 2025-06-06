using System;

namespace OptionAnalysisTool.Models.KiteConnect
{
    public class Instrument
    {
        public long InstrumentToken { get; set; }
        public long ExchangeToken { get; set; }
        public string TradingSymbol { get; set; }
        public string Name { get; set; }
        public decimal Strike { get; set; }
        public DateTime? Expiry { get; set; }
        public string InstrumentType { get; set; }
        public string Segment { get; set; }
        public string Exchange { get; set; }

        public Instrument()
        {
            TradingSymbol = string.Empty;
            Name = string.Empty;
            InstrumentType = string.Empty;
            Segment = string.Empty;
            Exchange = string.Empty;
        }
    }
} 