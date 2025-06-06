using System;
using System.ComponentModel.DataAnnotations;

namespace OptionAnalysisTool.Models
{
    public class LTP
    {
        public int Id { get; set; }
        
        [Required]
        [StringLength(50)]
        public required string InstrumentToken { get; set; }
        
        public decimal LastPrice { get; set; }
        public DateTime TimeStamp { get; set; }

        public LTP()
        {
            InstrumentToken = string.Empty;
            LastPrice = 0m;
            TimeStamp = DateTime.UtcNow;
        }
    }
} 