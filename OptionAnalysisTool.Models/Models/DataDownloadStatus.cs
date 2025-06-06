using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OptionAnalysisTool.Models
{
    public class DataDownloadStatus
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        [StringLength(50)]
        public required string Symbol { get; set; }
        
        [Required]
        [StringLength(50)]
        public required string DataType { get; set; }
        
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public DateTime LastUpdated { get; set; }
        public int TotalRecords { get; set; }
        public int ProcessedRecords { get; set; }
        public int FailedRecords { get; set; }
        public string Status { get; set; }
        public string ErrorMessage { get; set; }
        public bool IsCompleted { get; set; }
        public bool HasErrors { get; set; }
        public bool IsInProgress { get; set; }
        public DateTime LastDownloadTime { get; set; }
        public DateTime LastSuccessfulDownload { get; set; }
        public DateTime LastProcessedDate { get; set; }
        public DateTime LastDownloadedDate { get; set; }

        [Required]
        [StringLength(20)]
        public required string Exchange { get; set; }

        public DataDownloadStatus()
        {
            Symbol = string.Empty;
            DataType = string.Empty;
            Exchange = string.Empty;
            Status = string.Empty;
            ErrorMessage = string.Empty;
            StartDate = DateTime.UtcNow;
            EndDate = DateTime.UtcNow;
            LastUpdated = DateTime.UtcNow;
            LastDownloadTime = DateTime.UtcNow;
            LastSuccessfulDownload = DateTime.UtcNow;
            LastProcessedDate = DateTime.UtcNow;
            LastDownloadedDate = DateTime.UtcNow;
            TotalRecords = 0;
            ProcessedRecords = 0;
            FailedRecords = 0;
            IsCompleted = false;
            HasErrors = false;
            IsInProgress = false;
        }
    }
} 