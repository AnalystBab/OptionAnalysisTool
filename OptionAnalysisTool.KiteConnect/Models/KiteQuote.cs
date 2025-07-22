using ExternalKite = KiteConnect;
using System;

namespace OptionAnalysisTool.KiteConnect.Models
{
    public class KiteQuote
    {
        private readonly ExternalKite.Quote _quote;

        public KiteQuote(ExternalKite.Quote quote)
        {
            _quote = quote;
            
            // Populate circuit limits from the source quote
            LowerCircuitLimit = quote.LowerCircuitLimit;
            UpperCircuitLimit = quote.UpperCircuitLimit;
        }

        public ExternalKite.Quote Quote => _quote;

        public string InstrumentToken => _quote.InstrumentToken.ToString();
        public DateTime Timestamp => _quote.Timestamp ?? DateTime.MinValue;
        public decimal LastPrice => _quote.LastPrice;
        public int LastQuantity => (int)_quote.LastQuantity;
        public decimal AveragePrice => _quote.AveragePrice;
        public int Volume => (int)_quote.Volume;
        public int BuyQuantity => (int)_quote.BuyQuantity;
        public int SellQuantity => (int)_quote.SellQuantity;
        public decimal Open => _quote.Open;
        public decimal High => _quote.High;
        public decimal Low => _quote.Low;
        public decimal Close => _quote.Close;
        public decimal Change => _quote.Change;
        public int OpenInterest => (int)_quote.OI;
        public decimal LowerCircuitLimit { get; set; }
        public decimal UpperCircuitLimit { get; set; }
        public decimal ImpliedVolatility { get; set; }
        public DateTime TimeStamp { get; set; }

        // Add any additional properties or methods needed
    }

    public static class KiteQuoteExtensions
    {
        public static OptionAnalysisTool.KiteConnect.Quote ToQuote(this ExternalKite.Quote source)
        {
            return new OptionAnalysisTool.KiteConnect.Quote
            {
                InstrumentToken = source.InstrumentToken,
                Timestamp = source.Timestamp ?? DateTime.MinValue,
                LastPrice = source.LastPrice,
                LastQuantity = source.LastQuantity,
                AveragePrice = source.AveragePrice,
                Volume = source.Volume,
                BuyQuantity = source.BuyQuantity,
                SellQuantity = source.SellQuantity,
                Open = source.Open,
                High = source.High,
                Low = source.Low,
                Close = source.Close,
                Change = source.Change,
                LowerCircuitLimit = source.LowerCircuitLimit,
                UpperCircuitLimit = source.UpperCircuitLimit,
                OI = source.OI
            };
        }
    }
} 