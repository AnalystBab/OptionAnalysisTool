using ExternalKite = KiteConnect;

namespace OptionAnalysisTool.KiteConnect.Models
{
    public class KiteInstrument
    {
        private readonly ExternalKite.Instrument _instrument;

        public KiteInstrument(ExternalKite.Instrument instrument)
        {
            _instrument = instrument;
        }

        public string InstrumentToken => _instrument.InstrumentToken.ToString();
        public string TradingSymbol => _instrument.TradingSymbol;
        public string Name => _instrument.Name;
        public string Exchange => _instrument.Exchange;
        public string InstrumentType => _instrument.InstrumentType;
        public string Segment => _instrument.Segment;
        public DateTime? Expiry => _instrument.Expiry;
        public decimal Strike => _instrument.Strike;
        public decimal TickSize => _instrument.TickSize;
        public int LotSize => (int)_instrument.LotSize;

        // Add any additional properties or methods needed
    }

    public static class KiteInstrumentExtensions
    {
        public static OptionAnalysisTool.KiteConnect.Instrument ToInstrument(this KiteConnect.Instrument source)
        {
            return new OptionAnalysisTool.KiteConnect.Instrument
            {
                InstrumentToken = source.InstrumentToken,
                TradingSymbol = source.TradingSymbol,
                Name = source.Name,
                Exchange = source.Exchange,
                Segment = source.Segment,
                InstrumentType = source.InstrumentType,
                Expiry = source.Expiry,
                Strike = source.Strike,
                TickSize = source.TickSize,
                LotSize = source.LotSize
            };
        }
    }
} 