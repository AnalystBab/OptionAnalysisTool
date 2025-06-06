using System;
using System.ComponentModel.DataAnnotations;

namespace OptionAnalysisTool.Models
{
    public class HistoricalPrice
    {
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public required string Symbol { get; set; }

        [Required]
        [StringLength(20)]
        public required string Exchange { get; set; }

        [Required]
        public DateTime Date { get; set; }

        public string InstrumentToken { get; set; }
        public decimal Open { get; set; }
        public decimal High { get; set; }
        public decimal Low { get; set; }
        public decimal Close { get; set; }
        public long Volume { get; set; }
        public long OpenInterest { get; set; }
        public DateTime TimeStamp { get; set; }

        public HistoricalPrice()
        {
            Symbol = string.Empty;
            Exchange = string.Empty;
            InstrumentToken = string.Empty;
            Date = DateTime.UtcNow;
            TimeStamp = DateTime.UtcNow;
        }
    }
} 