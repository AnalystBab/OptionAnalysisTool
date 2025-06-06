using System;
using KiteConnect;

namespace OptionAnalysisTool.Common.Models
{
    public class CommonHistorical
    {
        public DateTime TimeStamp { get; set; }
        public decimal Open { get; set; }
        public decimal High { get; set; }
        public decimal Low { get; set; }
        public decimal Close { get; set; }
        public long Volume { get; set; }
        public long OpenInterest { get; set; }

        public static CommonHistorical FromKiteHistorical(Historical historical)
        {
            return new CommonHistorical
            {
                TimeStamp = historical.TimeStamp,
                Open = historical.Open,
                High = historical.High,
                Low = historical.Low,
                Close = historical.Close,
                Volume = (long)historical.Volume,
                OpenInterest = (long)historical.OI
            };
        }
    }
}