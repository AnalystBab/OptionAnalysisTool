using System;

namespace OptionAnalysisTool.Common.Models
{
    public class OptionPrice
    {
        public int Id { get; set; }
        public string Symbol { get; set; }
        public string OptionType { get; set; }  // CE or PE
        public decimal Strike { get; set; }
        public DateTime Date { get; set; }
        public decimal Open { get; set; }
        public decimal High { get; set; }
        public decimal Low { get; set; }
        public decimal Close { get; set; }
        public long Volume { get; set; }
        public long OpenInterest { get; set; }

        public OptionPrice()
        {
            Symbol = string.Empty;
            OptionType = string.Empty;
        }
    }
} 