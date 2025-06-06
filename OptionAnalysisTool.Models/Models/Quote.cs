using System;
using System.ComponentModel.DataAnnotations;

namespace OptionAnalysisTool.Models
{
    public class Quote
    {
        public int Id { get; set; }
        
        [Required]
        [StringLength(50)]
        public string InstrumentToken { get; set; }
        
        // Price Data
        public decimal LastPrice { get; set; }
        public decimal Open { get; set; }
        public decimal High { get; set; }
        public decimal Low { get; set; }
        public decimal Close { get; set; }
        public decimal PreviousClose { get; set; }
        public decimal Change { get; set; }
        public decimal PercentageChange { get; set; }
        
        // Volume and Interest
        public long Volume { get; set; }
        public long OpenInterest { get; set; }
        public int LastQuantity { get; set; }
        public decimal AveragePrice { get; set; }
        public int BuyQuantity { get; set; }
        public int SellQuantity { get; set; }
        
        // Circuit Limits - VERY IMPORTANT
        public decimal LowerCircuitLimit { get; set; }
        public decimal UpperCircuitLimit { get; set; }
        public bool IsInCircuitRange => LastPrice >= LowerCircuitLimit && LastPrice <= UpperCircuitLimit;
        public string CircuitStatus => 
            LastPrice <= LowerCircuitLimit ? "Lower Circuit" :
            LastPrice >= UpperCircuitLimit ? "Upper Circuit" : "Normal";
        
        // Greeks and Volatility
        public decimal ImpliedVolatility { get; set; }
        public decimal? Delta { get; set; }
        public decimal? Gamma { get; set; }
        public decimal? Theta { get; set; }
        public decimal? Vega { get; set; }
        
        // Bid-Ask Data
        public decimal BidPrice { get; set; }
        public decimal AskPrice { get; set; }
        public long BidQuantity { get; set; }
        public long AskQuantity { get; set; }
        public decimal Spread => AskPrice - BidPrice;
        
        // Market Depth (Top 5 levels)
        public string? MarketDepthJson { get; set; } // Serialized market depth data
        
        // Timestamps
        public DateTime TimeStamp { get; set; }
        public DateTime CaptureTime { get; set; }
        public DateTime LastUpdated { get; set; }
        
        // Data Quality
        public bool IsValidData { get; set; }
        public string? ValidationMessage { get; set; }

        public Quote()
        {
            InstrumentToken = string.Empty;
            TimeStamp = DateTime.UtcNow;
            CaptureTime = DateTime.UtcNow;
            LastUpdated = DateTime.UtcNow;
            LastPrice = 0m;
            Open = 0m;
            High = 0m;
            Low = 0m;
            Close = 0m;
            PreviousClose = 0m;
            Change = 0m;
            PercentageChange = 0m;
            Volume = 0;
            OpenInterest = 0;
            LastQuantity = 0;
            AveragePrice = 0m;
            BuyQuantity = 0;
            SellQuantity = 0;
            LowerCircuitLimit = 0m;
            UpperCircuitLimit = 0m;
            ImpliedVolatility = 0m;
            BidPrice = 0m;
            AskPrice = 0m;
            BidQuantity = 0;
            AskQuantity = 0;
            IsValidData = true;
        }

        // Factory method - will be used by the KiteConnect service layer
        public static Quote CreateFromKiteData(
            string instrumentToken,
            decimal lastPrice,
            decimal open,
            decimal high,
            decimal low,
            decimal close,
            decimal change,
            long volume,
            long openInterest,
            decimal lowerCircuitLimit,
            decimal upperCircuitLimit,
            decimal impliedVolatility = 0m,
            int lastQuantity = 0,
            decimal averagePrice = 0m,
            int buyQuantity = 0,
            int sellQuantity = 0)
        {
            return new Quote
            {
                InstrumentToken = instrumentToken,
                LastPrice = lastPrice,
                Open = open,
                High = high,
                Low = low,
                Close = close,
                Change = change,
                Volume = volume,
                OpenInterest = openInterest,
                LowerCircuitLimit = lowerCircuitLimit,
                UpperCircuitLimit = upperCircuitLimit,
                ImpliedVolatility = impliedVolatility,
                LastQuantity = lastQuantity,
                AveragePrice = averagePrice,
                BuyQuantity = buyQuantity,
                SellQuantity = sellQuantity,
                TimeStamp = DateTime.UtcNow,
                CaptureTime = DateTime.UtcNow,
                LastUpdated = DateTime.UtcNow,
                IsValidData = true
            };
        }

        // Backward compatibility method for existing services
        public static Quote? FromKiteQuote(object kiteQuote)
        {
            if (kiteQuote == null) return null;

            // Use reflection to access properties since we don't want hard coupling
            var kiteQuoteType = kiteQuote.GetType();
            
            try
            {
                return new Quote
                {
                    InstrumentToken = GetPropertyValue<string>(kiteQuote, "InstrumentToken") ?? "",
                    LastPrice = GetPropertyValue<decimal>(kiteQuote, "LastPrice"),
                    Open = GetPropertyValue<decimal>(kiteQuote, "Open"),
                    High = GetPropertyValue<decimal>(kiteQuote, "High"),
                    Low = GetPropertyValue<decimal>(kiteQuote, "Low"),
                    Close = GetPropertyValue<decimal>(kiteQuote, "Close"),
                    Change = GetPropertyValue<decimal>(kiteQuote, "Change"),
                    Volume = GetPropertyValue<long>(kiteQuote, "Volume"),
                    OpenInterest = GetPropertyValue<long>(kiteQuote, "OpenInterest"),
                    LowerCircuitLimit = GetPropertyValue<decimal>(kiteQuote, "LowerCircuitLimit"),
                    UpperCircuitLimit = GetPropertyValue<decimal>(kiteQuote, "UpperCircuitLimit"),
                    ImpliedVolatility = GetPropertyValue<decimal>(kiteQuote, "ImpliedVolatility"),
                    TimeStamp = GetPropertyValue<DateTime>(kiteQuote, "TimeStamp"),
                    CaptureTime = DateTime.UtcNow,
                    LastUpdated = DateTime.UtcNow,
                    LastQuantity = GetPropertyValue<int>(kiteQuote, "LastQuantity"),
                    AveragePrice = GetPropertyValue<decimal>(kiteQuote, "AveragePrice"),
                    BuyQuantity = GetPropertyValue<int>(kiteQuote, "BuyQuantity"),
                    SellQuantity = GetPropertyValue<int>(kiteQuote, "SellQuantity"),
                    IsValidData = true
                };
            }
            catch
            {
                return null;
            }
        }

        private static T GetPropertyValue<T>(object obj, string propertyName)
        {
            var property = obj.GetType().GetProperty(propertyName);
            if (property == null) return default(T);
            
            var value = property.GetValue(obj);
            if (value == null) return default(T);
            
            if (typeof(T) == typeof(string) && value.ToString() != null)
                return (T)(object)value.ToString()!;
                
            return (T)Convert.ChangeType(value, typeof(T));
        }
    }
} 