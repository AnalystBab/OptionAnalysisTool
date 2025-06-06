using System;
using System.ComponentModel.DataAnnotations;

namespace OptionAnalysisTool.Models
{
    public class CircuitLimitChange
    {
        public int Id { get; set; }
        
        [Required]
        [StringLength(50)]
        public string InstrumentToken { get; set; }
        
        [Required]
        [StringLength(50)]
        public string Symbol { get; set; }
        
        public decimal OldLowerCircuitLimit { get; set; }
        public decimal NewLowerCircuitLimit { get; set; }
        public decimal OldUpperCircuitLimit { get; set; }
        public decimal NewUpperCircuitLimit { get; set; }
        public decimal LastPrice { get; set; }
        public DateTime Timestamp { get; set; }
        
        [Required]
        [StringLength(100)]
        public string ChangeReason { get; set; }
        
        [StringLength(20)]
        public string? InstrumentType { get; set; }
        public decimal? StrikePrice { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public DateTime CreatedAt { get; set; }

        public CircuitLimitChange()
        {
            InstrumentToken = string.Empty;
            Symbol = string.Empty;
            ChangeReason = string.Empty;
            CreatedAt = DateTime.UtcNow;
        }
    }
} 