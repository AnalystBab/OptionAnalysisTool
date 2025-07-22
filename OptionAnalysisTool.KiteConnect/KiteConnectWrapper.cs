using OptionAnalysisTool.KiteConnect.Models;

namespace OptionAnalysisTool.KiteConnect
{
    public class KiteConnectWrapper
    {
        private readonly global::KiteConnect.Kite _kite;

        public KiteConnectWrapper(global::KiteConnect.Kite kite)
        {
            _kite = kite;
        }

        public Models.KiteQuote GetQuote(string instrumentToken)
        {
            var quotes = _kite.GetQuote(new[] { instrumentToken });
            return new Models.KiteQuote(quotes[instrumentToken]);
        }

        public KiteOHLC GetOHLC(string instrumentToken)
        {
            var ohlcs = _kite.GetOHLC(new[] { instrumentToken });
            return new KiteOHLC(ohlcs[instrumentToken]);
        }

        public KiteLTP GetLTP(string instrumentToken)
        {
            var ltps = _kite.GetLTP(new[] { instrumentToken });
            return new KiteLTP(ltps[instrumentToken]);
        }

        // The following methods need to be updated based on the new API or commented out if not available
        // public KiteHistorical GetHistorical(...) { ... }
        // public KiteInstrument GetInstrument(...) { ... }
    }
} 