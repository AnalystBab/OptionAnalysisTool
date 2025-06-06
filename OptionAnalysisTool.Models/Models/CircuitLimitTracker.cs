using System;
using System.ComponentModel.DataAnnotations;

namespace OptionAnalysisTool.Models
{
    /// <summary>
    /// 🔥 CIRCUIT LIMIT TRACKING - VERY IMPORTANT FOR TRADING LOGIC
    /// Tracks any changes in circuit limits for option contracts
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
        
        public bool HasLowerLimitChanged => Math.Abs(PreviousLowerLimit - NewLowerLimit) > 0.01m;
        public bool HasUpperLimitChanged => Math.Abs(PreviousUpperLimit - NewUpperLimit) > 0.01m;
        public bool HasAnyLimitChanged => HasLowerLimitChanged || HasUpperLimitChanged;
        
        // Change Analysis
        public decimal LowerLimitChangePercent { get; set; }
        public decimal UpperLimitChangePercent { get; set; }
        public decimal RangeChangePercent { get; set; }
        
        // Market Context
        public decimal CurrentPrice { get; set; }
        public decimal UnderlyingPrice { get; set; }
        public long Volume { get; set; }
        public long OpenInterest { get; set; }
        
        // Timestamps
        public DateTime DetectedAt { get; set; }
        public DateTime LastUpdated { get; set; }
        
        // Analysis Flags - TRADING SIGNALS
        public bool IsBreachAlert { get; set; }
        public string SeverityLevel { get; set; } = "Normal"; // Low, Medium, High, Critical
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