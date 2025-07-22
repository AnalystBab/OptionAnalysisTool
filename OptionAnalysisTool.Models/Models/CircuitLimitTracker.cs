using System;
using System.ComponentModel.DataAnnotations;

namespace OptionAnalysisTool.Models
{
    /// <summary>
    /// 🔥 CIRCUIT LIMIT TRACKING - VERY IMPORTANT FOR TRADING LOGIC
    /// Tracks any changes in circuit limits for option contracts
    /// ENHANCED: Now includes underlying index OHLC data for complete market context
    /// SIMPLE: No calculations - just records when values actually change
    /// </summary>
    public class CircuitLimitTracker
    {
        public int Id { get; set; }
        
        [Required]
        [StringLength(50)]
        public string InstrumentToken { get; set; } = string.Empty;
        
        [Required]
        [StringLength(100)]
        public string Symbol { get; set; } = string.Empty;
        
        [Required]
        [StringLength(50)]
        public string UnderlyingSymbol { get; set; } = string.Empty;
        
        public decimal StrikePrice { get; set; }
        
        [Required]
        [StringLength(10)]
        public string OptionType { get; set; } = string.Empty; // CE or PE
        
        public DateTime ExpiryDate { get; set; }
        
        // Circuit Limit Data - CORE TRACKING
        public decimal PreviousLowerLimit { get; set; }
        public decimal NewLowerLimit { get; set; }
        public decimal PreviousUpperLimit { get; set; }
        public decimal NewUpperLimit { get; set; }
        
        // 🔥 SIMPLE: Change Tracking (no calculations needed)
        public decimal LowerLimitChangePercent { get; set; } = 0; // Default to 0, no calculation needed
        public decimal UpperLimitChangePercent { get; set; } = 0; // Default to 0, no calculation needed
        public decimal RangeChangePercent { get; set; } = 0; // Default to 0, no calculation needed
        
        // 🔥 SIMPLE: Breach Alert Tracking
        public bool IsBreachAlert { get; set; } = false; // Default to false
        public string SeverityLevel { get; set; } = "Normal"; // Default to Normal
        
        public bool HasLowerLimitChanged => PreviousLowerLimit != NewLowerLimit;
        public bool HasUpperLimitChanged => PreviousUpperLimit != NewUpperLimit;
        public bool HasAnyLimitChanged => HasLowerLimitChanged || HasUpperLimitChanged;
        
        // Market Context
        public decimal CurrentPrice { get; set; }
        public decimal UnderlyingPrice { get; set; }
        public long Volume { get; set; }
        public long OpenInterest { get; set; }
        
        // 🔥 ENHANCED: Underlying Index OHLC Data at Circuit Limit Recording Time
        public decimal UnderlyingOpen { get; set; }
        public decimal UnderlyingHigh { get; set; }
        public decimal UnderlyingLow { get; set; }
        public decimal UnderlyingClose { get; set; }
        public decimal UnderlyingChange { get; set; }
        public decimal UnderlyingPercentageChange { get; set; }
        public long UnderlyingVolume { get; set; }
        
        // Underlying Index Circuit Limits (if applicable)
        public decimal UnderlyingLowerCircuitLimit { get; set; }
        public decimal UnderlyingUpperCircuitLimit { get; set; }
        public string UnderlyingCircuitStatus { get; set; } = "Normal"; // Normal, Upper Circuit, Lower Circuit
        
        // Timestamps
        public DateTime DetectedAt { get; set; }
        public DateTime LastUpdated { get; set; }
        
        // Simple Change Tracking
        public string ChangeReason { get; set; } = string.Empty;
        
        // Data Quality
        public bool IsValidData { get; set; } = true;
        public string ValidationMessage { get; set; } = string.Empty;
        
        public CircuitLimitTracker()
        {
            DetectedAt = DateTime.UtcNow;
            LastUpdated = DateTime.UtcNow;
        }
    }
} 