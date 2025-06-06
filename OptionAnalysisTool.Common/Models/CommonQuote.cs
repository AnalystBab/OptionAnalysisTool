using System;
using KiteConnect;

namespace OptionAnalysisTool.Common.Models
{
    public class CommonQuote
    {
        public string InstrumentToken { get; set; } = string.Empty;
        public decimal LastPrice { get; set; }
        public decimal Change { get; set; }
        public decimal Open { get; set; }
        public decimal High { get; set; }
        public decimal Low { get; set; }
        public decimal Close { get; set; }
        public long Volume { get; set; }
        public decimal AveragePrice { get; set; }
        public long OpenInterest { get; set; }
        public long BuyQuantity { get; set; }
        public long SellQuantity { get; set; }
        public long LastQuantity { get; set; }
        public DateTime TimeStamp { get; set; }
        public decimal LowerCircuitLimit { get; set; }
        public decimal UpperCircuitLimit { get; set; }
        public decimal? ImpliedVolatility { get; set; }
        public string TradingStatus { get; set; } = string.Empty;
        public bool IsValidData { get; set; }

        public static CommonQuote FromKiteQuote(Quote quote)
        {
            return new CommonQuote
            {
                InstrumentToken = quote.InstrumentToken.ToString(),
                LastPrice = quote.LastPrice,
                Change = quote.Change,
                Open = quote.Open,
                High = quote.High,
                Low = quote.Low,
                Close = quote.Close,
                Volume = quote.Volume,
                AveragePrice = quote.AveragePrice,
                OpenInterest = (long)quote.OI,
                BuyQuantity = quote.BuyQuantity,
                SellQuantity = quote.SellQuantity,
                LastQuantity = quote.LastQuantity,
                TimeStamp = quote.LastTradeTime ?? DateTime.Now,
                LowerCircuitLimit = quote.LowerCircuitLimit,
                UpperCircuitLimit = quote.UpperCircuitLimit,
                ImpliedVolatility = null // KiteConnect Quote doesn't have IV
            };
        }
    }
} 