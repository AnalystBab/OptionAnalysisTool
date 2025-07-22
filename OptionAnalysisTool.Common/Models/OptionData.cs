using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OptionAnalysisTool.Common.Models
{
    // Base class for common option data fields
    public abstract class BaseOptionData
    {
        [Key]
        public int Id { get; set; }
        public string InstrumentToken { get; set; }
        public string Symbol { get; set; }
        public string UnderlyingSymbol { get; set; }
        public decimal StrikePrice { get; set; }
        public string OptionType { get; set; }  // CE or PE
        public DateTime ExpiryDate { get; set; }
        public decimal LastPrice { get; set; }
        public decimal Open { get; set; }
        public decimal High { get; set; }
        public decimal Low { get; set; }
        public decimal Close { get; set; }
        public decimal Change { get; set; }
        public decimal Volume { get; set; }
        public decimal OpenInterest { get; set; }
        public DateTime Timestamp { get; set; }
        public DateTime LastUpdated { get; set; }
    }

    // Current state of option contracts
    [Table("OptionContracts")]
    public class OptionContract : BaseOptionData
    {
        public decimal LowerCircuitLimit { get; set; }
        public decimal UpperCircuitLimit { get; set; }
        public bool IsActive { get; set; }
        public bool IsLiveData { get; set; }
    }

    // Historical daily data for options
    [Table("HistoricalOptionData")]
    public class HistoricalOptionData : BaseOptionData
    {
        public decimal PreviousClose { get; set; }
        public decimal VWAP { get; set; }
        public long DeliveryQuantity { get; set; }
        public decimal DeliveryPercentage { get; set; }
    }

    // Intraday 1-minute data
    [Table("IntradayOption1MinData")]
    public class IntradayOption1MinData : BaseOptionData
    {
        public decimal BuyQuantity { get; set; }
        public decimal SellQuantity { get; set; }
        public decimal ImpliedVolatility { get; set; }
        public decimal Delta { get; set; }
        public decimal Gamma { get; set; }
        public decimal Theta { get; set; }
        public decimal Vega { get; set; }
    }

    // Intraday 5-minute data
    [Table("IntradayOption5MinData")]
    public class IntradayOption5MinData : BaseOptionData
    {
        public decimal BuyQuantity { get; set; }
        public decimal SellQuantity { get; set; }
        public decimal ImpliedVolatility { get; set; }
        public decimal Delta { get; set; }
        public decimal Gamma { get; set; }
        public decimal Theta { get; set; }
        public decimal Vega { get; set; }
    }

    // Intraday 15-minute data
    [Table("IntradayOption15MinData")]
    public class IntradayOption15MinData : BaseOptionData
    {
        public decimal BuyQuantity { get; set; }
        public decimal SellQuantity { get; set; }
        public decimal ImpliedVolatility { get; set; }
        public decimal Delta { get; set; }
        public decimal Gamma { get; set; }
        public decimal Theta { get; set; }
        public decimal Vega { get; set; }
    }

    // Historical daily data points
    [Table("HistoricalOptionDataPoints")]
    public class HistoricalOptionDataPoint
    {
        [Key]
        public int Id { get; set; }
        public int ContractId { get; set; }
        public DateTime Date { get; set; }
        public decimal Open { get; set; }
        public decimal High { get; set; }
        public decimal Low { get; set; }
        public decimal Close { get; set; }
        public decimal Volume { get; set; }
        public decimal OpenInterest { get; set; }
        public decimal VWAP { get; set; }
        public decimal DeliveryQuantity { get; set; }
        public decimal DeliveryPercentage { get; set; }
        public DateTime LastUpdated { get; set; }

        [ForeignKey("ContractId")]
        public virtual OptionContract Contract { get; set; }
    }

    // Option Greeks tracking
    [Table("OptionGreeks")]
    public class OptionGreeks
    {
        [Key]
        public int Id { get; set; }
        public int ContractId { get; set; }
        public DateTime Timestamp { get; set; }
        public decimal ImpliedVolatility { get; set; }
        public decimal Delta { get; set; }
        public decimal Gamma { get; set; }
        public decimal Theta { get; set; }
        public decimal Vega { get; set; }
        public decimal UnderlyingPrice { get; set; }
        public DateTime LastUpdated { get; set; }

        [ForeignKey("ContractId")]
        public virtual OptionContract Contract { get; set; }
    }

    public class OptionMonitoringStats
    {
        [Key]
        public long Id { get; set; }

        public DateTime Timestamp { get; set; }

        public int ActiveContractsCount { get; set; }

        public int NewContractsFound { get; set; }

        public int SnapshotsSaved { get; set; }

        public int ErrorCount { get; set; }

        [StringLength(1000)]
        public string LastError { get; set; }

        public decimal AverageProcessingTime { get; set; }

        public bool IsMarketOpen { get; set; }

        public DateTime? NextUpdateTime { get; set; }
    }

    public class OptionData
    {
        public string InstrumentToken { get; set; } = string.Empty;
        public string Symbol { get; set; } = string.Empty;
        public string UnderlyingSymbol { get; set; } = string.Empty;
        public decimal StrikePrice { get; set; }
        public string OptionType { get; set; } = string.Empty;
        public DateTime? ExpiryDate { get; set; }
        public decimal LastPrice { get; set; }
        public int Volume { get; set; }
        public int OpenInterest { get; set; }
        public string? LastError { get; set; } = string.Empty;
        public OptionContract? Contract { get; set; } = new OptionContract();
    }
} 