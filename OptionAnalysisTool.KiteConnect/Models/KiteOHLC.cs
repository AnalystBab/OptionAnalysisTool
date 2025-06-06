using KiteConnect;

namespace OptionAnalysisTool.KiteConnect.Models
{
    public class KiteOHLC
    {
        public OHLC OHLC { get; set; }

        public KiteOHLC(OHLC ohlc)
        {
            OHLC = ohlc;
        }

        // Add any additional properties or methods needed
    }
} 