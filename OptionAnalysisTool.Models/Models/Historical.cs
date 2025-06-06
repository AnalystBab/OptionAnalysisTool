using System;
using System.ComponentModel.DataAnnotations;
using OptionAnalysisTool.Models.KiteConnect;

namespace OptionAnalysisTool.Models
{
    public class Historical
    {
        public string InstrumentToken { get; set; }
        public DateTime Timestamp { get; set; }
        public decimal Open { get; set; }
        public decimal High { get; set; }
        public decimal Low { get; set; }
        public decimal Close { get; set; }
        public long Volume { get; set; }
        public long OpenInterest { get; set; }

        public Historical()
        {
            InstrumentToken = string.Empty;
        }

        public static Historical FromKiteHistorical(KiteConnect.Historical? kiteHistorical)
        {
            if (kiteHistorical == null)
            {
                return new Historical();
            }

            return new Historical
            {
                InstrumentToken = string.Empty,
                Timestamp = kiteHistorical.Date,
                Open = kiteHistorical.Open,
                High = kiteHistorical.High,
                Low = kiteHistorical.Low,
                Close = kiteHistorical.Close,
                Volume = kiteHistorical.Volume,
                OpenInterest = kiteHistorical.Oi
            };
        }
    }
} 