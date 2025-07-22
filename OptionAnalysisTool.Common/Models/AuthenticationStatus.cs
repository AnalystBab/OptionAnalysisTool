using System;

namespace OptionAnalysisTool.Common.Models
{
    /// <summary>
    /// SHARED AUTHENTICATION STATUS MODEL
    /// Used by all authentication services for consistent status reporting
    /// </summary>
    public class AuthenticationStatus
    {
        // Core authentication state
        public bool IsAuthenticated { get; set; }
        public bool HasAccessToken { get; set; }
        public bool HasStoredCredentials { get; set; }
        public bool ServiceInitialized { get; set; }
        public bool IsAutonomous { get; set; }
        public string StatusMessage { get; set; } = string.Empty;
        
        // Timing information
        public DateTime LastCheckTime { get; set; }
        public DateTime? SessionExpiryTime { get; set; }
        public DateTime? TokenExpiryTime { get; set; }
        public DateTime LastAuthTime { get; set; }
        public DateTime NextAuthRequired { get; set; }
        
        // OTP and user interaction state
        public bool WaitingForOTP { get; set; }
    }

    /// <summary>
    /// SHARED AUTHENTICATION DATA MODEL
    /// Used for secure storage of authentication data
    /// </summary>
    public class AuthenticationData
    {
        public string AccessToken { get; set; } = string.Empty;
        public string ApiKey { get; set; } = string.Empty;
        public string ApiSecret { get; set; } = string.Empty;
        public DateTime ExpiryTime { get; set; }
        public DateTime StoredAt { get; set; }
        public bool IsFromWpfApp { get; set; }
    }
} 