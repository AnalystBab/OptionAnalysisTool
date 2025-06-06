using System;
using System.ComponentModel.DataAnnotations;

namespace OptionAnalysisTool.Models
{
    public class OptionMonitoringStats
    {
        public int Id { get; set; }
        
        [Required]
        [StringLength(50)]
        public string Symbol { get; set; }
        
        [Required]
        [StringLength(50)]
        public string UnderlyingSymbol { get; set; }
        
        [Required]
        [StringLength(10)]
        public string OptionType { get; set; }
        
        public DateTime Timestamp { get; set; }
        public bool IsMarketOpen { get; set; }
        public int ActiveContractsCount { get; set; }
        public int NewContractsFound { get; set; }
        public int SnapshotsSaved { get; set; }
        public int ErrorCount { get; set; }
        public string LastError { get; set; }
        public double AverageProcessingTime { get; set; }
        public DateTime NextUpdateTime { get; set; }
        public DateTime LastUpdated { get; set; }

        public OptionMonitoringStats()
        {
            Symbol = string.Empty;
            UnderlyingSymbol = string.Empty;
            OptionType = string.Empty;
            Timestamp = DateTime.UtcNow;
            LastUpdated = DateTime.UtcNow;
        }
    }
} 