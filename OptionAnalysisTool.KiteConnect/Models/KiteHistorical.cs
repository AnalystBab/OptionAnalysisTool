using System;
using KiteConnect;

namespace OptionAnalysisTool.KiteConnect.Models
{
    public class KiteHistorical
    {
        private readonly Historical _historical;

        public KiteHistorical(Historical historical)
        {
            _historical = historical;
        }

        public DateTime TimeStamp => _historical.TimeStamp;
        public decimal Open => _historical.Open;
        public decimal High => _historical.High;
        public decimal Low => _historical.Low;
        public decimal Close => _historical.Close;
        public long Volume => (long)_historical.Volume;
        public long OI => (long)_historical.OI;

        // Add any additional properties or methods needed
    }
} 