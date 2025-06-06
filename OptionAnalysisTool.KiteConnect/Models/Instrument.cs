using System;

namespace OptionAnalysisTool.KiteConnect
{
    public class Instrument
    {
        public string TradingSymbol { get; set; } = string.Empty;
        public long InstrumentToken { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Exchange { get; set; } = string.Empty;
        public string Segment { get; set; } = string.Empty;
        public decimal Strike { get; set; }
        public string? InstrumentType { get; set; }
        public DateTime? Expiry { get; set; }
        public decimal LotSize { get; set; }
        public decimal TickSize { get; set; }
    }
} 