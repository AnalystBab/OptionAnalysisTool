using System;

namespace OptionAnalysisTool.Models.KiteConnect
{
    public class Historical
    {
        public DateTime Date { get; set; }
        public decimal Open { get; set; }
        public decimal High { get; set; }
        public decimal Low { get; set; }
        public decimal Close { get; set; }
        public long Volume { get; set; }
        public long Oi { get; set; }
    }
} 