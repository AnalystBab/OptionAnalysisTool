using System;
using Microsoft.Extensions.Logging;

namespace OptionAnalysisTool.Common.Services
{
    /// <summary>
    /// 🕘 TIMESTAMP DISPLAY SERVICE
    /// Converts UTC timestamps to IST for user-friendly display
    /// All database timestamps are stored in UTC, but displayed in IST
    /// </summary>
    public class TimestampDisplayService
    {
        private readonly ILogger<TimestampDisplayService> _logger;
        private static readonly TimeZoneInfo IST_TIMEZONE = TimeZoneInfo.FindSystemTimeZoneById("India Standard Time");

        public TimestampDisplayService(ILogger<TimestampDisplayService> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Convert UTC timestamp to IST for display
        /// </summary>
        public DateTime ConvertToIST(DateTime utcTimestamp)
        {
            try
            {
                if (utcTimestamp.Kind == DateTimeKind.Unspecified)
                {
                    // Assume it's UTC if unspecified
                    utcTimestamp = DateTime.SpecifyKind(utcTimestamp, DateTimeKind.Utc);
                }

                return TimeZoneInfo.ConvertTimeFromUtc(utcTimestamp, IST_TIMEZONE);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error converting timestamp to IST: {timestamp}", utcTimestamp);
                return utcTimestamp; // Return original if conversion fails
            }
        }

        /// <summary>
        /// Format timestamp for display with IST timezone indicator
        /// </summary>
        public string FormatForDisplay(DateTime utcTimestamp, string format = "yyyy-MM-dd HH:mm:ss")
        {
            try
            {
                var istTime = ConvertToIST(utcTimestamp);
                return $"{istTime.ToString(format)} IST";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error formatting timestamp for display: {timestamp}", utcTimestamp);
                return utcTimestamp.ToString(format);
            }
        }

        /// <summary>
        /// Get current IST time
        /// </summary>
        public DateTime GetCurrentIST()
        {
            return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, IST_TIMEZONE);
        }

        /// <summary>
        /// Check if given UTC time is during market hours (IST)
        /// </summary>
        public bool IsDuringMarketHours(DateTime utcTimestamp)
        {
            try
            {
                var istTime = ConvertToIST(utcTimestamp);
                var timeOfDay = istTime.TimeOfDay;
                
                // Market hours: 9:15 AM to 3:30 PM IST
                return timeOfDay >= TimeSpan.FromHours(9).Add(TimeSpan.FromMinutes(15)) &&
                       timeOfDay <= TimeSpan.FromHours(15).Add(TimeSpan.FromMinutes(30)) &&
                       istTime.DayOfWeek >= DayOfWeek.Monday &&
                       istTime.DayOfWeek <= DayOfWeek.Friday;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking market hours for timestamp: {timestamp}", utcTimestamp);
                return false;
            }
        }

        /// <summary>
        /// Get time difference between two timestamps in IST
        /// </summary>
        public TimeSpan GetTimeDifference(DateTime utcTimestamp1, DateTime utcTimestamp2)
        {
            try
            {
                var istTime1 = ConvertToIST(utcTimestamp1);
                var istTime2 = ConvertToIST(utcTimestamp2);
                return istTime2 - istTime1;
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
    }
} 