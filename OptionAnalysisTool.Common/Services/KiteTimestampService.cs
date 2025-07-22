using System;
using Microsoft.Extensions.Logging;

namespace OptionAnalysisTool.Common.Services
{
    /// <summary>
    /// 🕘 KITE TIMESTAMP SERVICE
    /// Handles Kite API timestamps properly - preserves original IST format
    /// Kite API returns timestamps in IST (Indian Standard Time)
    /// We should store the exact same format, not convert to UTC
    /// </summary>
    public class KiteTimestampService
    {
        private readonly ILogger<KiteTimestampService> _logger;

        public KiteTimestampService(ILogger<KiteTimestampService> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Get current IST time (same format as Kite API)
        /// </summary>
        public DateTime GetCurrentIST()
        {
            try
            {
                var ist = TimeZoneInfo.FindSystemTimeZoneById("India Standard Time");
                return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, ist);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting current IST time");
                return DateTime.Now; // Fallback to local time
            }
        }

        /// <summary>
        /// Preserve Kite API timestamp as-is (IST format)
        /// </summary>
        public DateTime PreserveKiteTimestamp(DateTime kiteTimestamp)
        {
            try
            {
                // If timestamp is already in IST (from Kite API), return as-is
                if (kiteTimestamp.Kind == DateTimeKind.Unspecified)
                {
                    // Assume it's IST if unspecified (Kite API format)
                    return kiteTimestamp;
                }
                else if (kiteTimestamp.Kind == DateTimeKind.Utc)
                {
                    // Convert UTC to IST (if somehow we got UTC)
                    var ist = TimeZoneInfo.FindSystemTimeZoneById("India Standard Time");
                    return TimeZoneInfo.ConvertTimeFromUtc(kiteTimestamp, ist);
                }
                else
                {
                    // Local time - assume it's IST
                    return kiteTimestamp;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error preserving Kite timestamp: {timestamp}", kiteTimestamp);
                return kiteTimestamp; // Return original if conversion fails
            }
        }

        /// <summary>
        /// Create timestamp for database storage (IST format)
        /// </summary>
        public DateTime CreateDatabaseTimestamp()
        {
            return GetCurrentIST();
        }

        /// <summary>
        /// Check if timestamp is during market hours (IST)
        /// </summary>
        public bool IsDuringMarketHours(DateTime timestamp)
        {
            try
            {
                var timeOfDay = timestamp.TimeOfDay;
                
                // Market hours: 9:15 AM to 3:30 PM IST
                return timeOfDay >= TimeSpan.FromHours(9).Add(TimeSpan.FromMinutes(15)) &&
                       timeOfDay <= TimeSpan.FromHours(15).Add(TimeSpan.FromMinutes(30)) &&
                       timestamp.DayOfWeek >= DayOfWeek.Monday &&
                       timestamp.DayOfWeek <= DayOfWeek.Friday;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking market hours for timestamp: {timestamp}", timestamp);
                return false;
            }
        }

        /// <summary>
        /// Format timestamp for display (IST format)
        /// </summary>
        public string FormatForDisplay(DateTime timestamp, string format = "yyyy-MM-dd HH:mm:ss")
        {
            try
            {
                return $"{timestamp.ToString(format)} IST";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error formatting timestamp for display: {timestamp}", timestamp);
                return timestamp.ToString(format);
            }
        }

        /// <summary>
        /// Get time difference between two timestamps
        /// </summary>
        public TimeSpan GetTimeDifference(DateTime timestamp1, DateTime timestamp2)
        {
            try
            {
                return timestamp2 - timestamp1;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating time difference between timestamps");
                return TimeSpan.Zero;
            }
        }

        /// <summary>
        /// Format time difference for display
        /// </summary>
        public string FormatTimeDifference(TimeSpan difference)
        {
            if (difference.TotalMinutes < 1)
                return $"{difference.TotalSeconds:F0} seconds";
            else if (difference.TotalHours < 1)
                return $"{difference.TotalMinutes:F0} minutes";
            else if (difference.TotalDays < 1)
                return $"{difference.TotalHours:F1} hours";
            else
                return $"{difference.TotalDays:F1} days";
        }

        /// <summary>
        /// Validate if timestamp is reasonable (not too old or future)
        /// </summary>
        public bool IsValidTimestamp(DateTime timestamp)
        {
            try
            {
                var now = GetCurrentIST();
                var maxFuture = now.AddHours(1); // Allow 1 hour in future
                var maxPast = now.AddDays(-30); // Allow 30 days in past

                return timestamp >= maxPast && timestamp <= maxFuture;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating timestamp: {timestamp}", timestamp);
                return false;
            }
        }
    }
} 