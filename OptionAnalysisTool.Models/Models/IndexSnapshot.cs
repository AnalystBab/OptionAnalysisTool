using System;
using System.ComponentModel.DataAnnotations;

namespace OptionAnalysisTool.Models
{
    public class IndexSnapshot
    {
        public int Id { get; set; }
        
        [Required]
        [StringLength(50)]
        public string Symbol { get; set; }
        
        public decimal LastPrice { get; set; }
        public decimal Open { get; set; }
        public decimal High { get; set; }
        public decimal Low { get; set; }
        public decimal Close { get; set; }
        public long Volume { get; set; }
        public DateTime Timestamp { get; set; }
        public DateTime LastUpdated { get; set; }

        public IndexSnapshot()
        {
            Symbol = string.Empty;
            Timestamp = DateTime.UtcNow;
            LastUpdated = DateTime.UtcNow;
        }
    }
} 