using System;

namespace OptionAnalysisTool.Common.Models
{
    public class OptionDataPoint
    {
        public int Id { get; set; }
        public int ContractId { get; set; }
        public DateTime Timestamp { get; set; }
        public decimal Open { get; set; }
        public decimal High { get; set; }
        public decimal Low { get; set; }
        public decimal Close { get; set; }
        public long Volume { get; set; }
        public long OI { get; set; }
        public bool IsLive { get; set; }
        public DateTime CreatedAt { get; set; }

        public OptionDataPoint()
        {
            CreatedAt = DateTime.UtcNow;
        }
    }
} 