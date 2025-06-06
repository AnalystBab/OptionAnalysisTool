using System;
using KiteConnect;

namespace OptionAnalysisTool.Common.Models
{
    public class CommonInstrument
    {
        public string InstrumentToken { get; set; } = string.Empty;
        public string ExchangeToken { get; set; } = string.Empty;
        public string TradingSymbol { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public decimal LastPrice { get; set; }
        public DateTime? Expiry { get; set; }
        public decimal Strike { get; set; }
        public decimal TickSize { get; set; }
        public int LotSize { get; set; }
        public string InstrumentType { get; set; } = string.Empty;
        public string Segment { get; set; } = string.Empty;
        public string Exchange { get; set; } = string.Empty;

        public static CommonInstrument FromKiteInstrument(Instrument instrument)
        {
            return new CommonInstrument
            {
                InstrumentToken = instrument.InstrumentToken.ToString(),
                ExchangeToken = instrument.ExchangeToken.ToString(),
                TradingSymbol = instrument.TradingSymbol,
                Name = instrument.Name,
                LastPrice = instrument.LastPrice,
                Expiry = instrument.Expiry,
                Strike = instrument.Strike,
                TickSize = instrument.TickSize,
                LotSize = (int)instrument.LotSize,
                InstrumentType = instrument.InstrumentType,
                Segment = instrument.Segment,
                Exchange = instrument.Exchange
            };
        }
    }
} 