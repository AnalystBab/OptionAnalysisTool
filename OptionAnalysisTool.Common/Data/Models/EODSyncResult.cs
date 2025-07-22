using System;
using System.ComponentModel.DataAnnotations;

namespace OptionAnalysisTool.Common.Data.Models
{
    /// <summary>
    /// 📊 EOD SYNC RESULT - Database model for tracking EOD data synchronization history
    /// </summary>
    public class EODSyncResult
    {
        public int Id { get; set; }
        
        [Required]
        public DateTime TradingDate { get; set; }
        
        [Required]
        [StringLength(50)]
        public string UnderlyingSymbol { get; set; } = string.Empty;
        
        public int ContractsProcessed { get; set; }
        public int SuccessfulSaves { get; set; }
        public int TotalErrors { get; set; }
        
        [StringLength(20)]
        public string Duration { get; set; } = string.Empty;
        
        [StringLength(100)]
        public string Status { get; set; } = string.Empty;
        
        public DateTime CreatedAt { get; set; }
        public DateTime LastUpdated { get; set; }
        
        public EODSyncResult()
        {
            CreatedAt = DateTime.Now;
            LastUpdated = DateTime.Now;
        }
    }
} 