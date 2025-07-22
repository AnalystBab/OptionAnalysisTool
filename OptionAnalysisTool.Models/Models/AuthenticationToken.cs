using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OptionAnalysisTool.Models
{
    /// <summary>
    /// 🔐 DATABASE TABLE FOR ACCESS TOKEN STORAGE
    /// Central database storage for Kite Connect access tokens
    /// Used by data services to fetch current valid tokens
    /// </summary>
    [Table("AuthenticationTokens")]
    public class AuthenticationToken
    {
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// Current active access token from Kite Connect
        /// </summary>
        [Required]
        [MaxLength(500)]
        public string AccessToken { get; set; } = string.Empty;

        /// <summary>
        /// API Key used for this token
        /// </summary>
        [Required]
        [MaxLength(100)]
        public string ApiKey { get; set; } = string.Empty;

        /// <summary>
        /// API Secret (encrypted)
        /// </summary>
        [MaxLength(200)]
        public string? ApiSecret { get; set; }

        /// <summary>
        /// User ID from Kite Connect
        /// </summary>
        [MaxLength(100)]
        public string? UserId { get; set; }

        /// <summary>
        /// User name from Kite Connect profile
        /// </summary>
        [MaxLength(200)]
        public string? UserName { get; set; }

        /// <summary>
        /// When this token was created/obtained
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// When this token expires (6 AM IST next day)
        /// </summary>
        public DateTime ExpiresAt { get; set; }

        /// <summary>
        /// Last time this token was successfully used
        /// </summary>
        public DateTime? LastUsedAt { get; set; }

        /// <summary>
        /// Is this token currently active
        /// </summary>
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// How this token was obtained (Manual, WPF, QuickAuth, etc.)
        /// </summary>
        [MaxLength(50)]
        public string Source { get; set; } = "Unknown";

        /// <summary>
        /// IP address where authentication occurred
        /// </summary>
        [MaxLength(50)]
        public string? SourceIpAddress { get; set; }

        /// <summary>
        /// Any additional metadata (JSON format)
        /// </summary>
        public string? Metadata { get; set; }

        /// <summary>
        /// When this record was last updated
        /// </summary>
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Check if token is still valid
        /// </summary>
        [NotMapped]
        public bool IsValid => IsActive && ExpiresAt > DateTime.UtcNow;

        /// <summary>
        /// Minutes until expiry
        /// </summary>
        [NotMapped]
        public double MinutesUntilExpiry => (ExpiresAt - DateTime.UtcNow).TotalMinutes;

        /// <summary>
        /// Format for display
        /// </summary>
        [NotMapped]
        public string DisplayToken => AccessToken.Length > 10 ? $"{AccessToken.Substring(0, 10)}..." : AccessToken;


    }
} 