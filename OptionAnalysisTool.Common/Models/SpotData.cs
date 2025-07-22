using System;

namespace OptionAnalysisTool.Common.Models
{
    public class SpotData
    {
        public string Symbol { get; set; } = string.Empty;
        public string Exchange { get; set; } = string.Empty;
        public decimal LastPrice { get; set; }
        public decimal Change { get; set; }
        public decimal PercentageChange { get; set; }
        public decimal Open { get; set; }
        public decimal High { get; set; }
        public decimal Low { get; set; }
        public decimal Close { get; set; }
        public long Volume { get; set; }
        public DateTime Timestamp { get; set; }
        public DateTime LastUpdated { get; set; }
        public DateTime CapturedAt { get; set; }
        public bool IsValidData { get; set; }
        public string ValidationMessage { get; set; } = string.Empty;
    }
} 