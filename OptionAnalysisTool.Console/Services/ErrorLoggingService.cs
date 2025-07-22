using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;

namespace OptionAnalysisTool.Console.Services
{
    /// <summary>
    /// 🔍 COMPREHENSIVE ERROR LOGGING SERVICE
    /// Captures all errors with detailed context and provides step-by-step debugging information
    /// </summary>
    public class ErrorLoggingService
    {
        private readonly ILogger<ErrorLoggingService> _logger;
        private readonly string _errorLogPath;
        private readonly string _debugLogPath;
        private readonly string _authLogPath;

        public ErrorLoggingService(ILogger<ErrorLoggingService> logger, IConfiguration configuration)
        {
            _logger = logger;
            
            // Create logs directory in the application directory
            var logsDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");
            Directory.CreateDirectory(logsDir);
            
            _errorLogPath = Path.Combine(logsDir, $"ERROR_LOG_{DateTime.Now:yyyyMMdd}.txt");
            _debugLogPath = Path.Combine(logsDir, $"DEBUG_LOG_{DateTime.Now:yyyyMMdd}.txt");
            _authLogPath = Path.Combine(logsDir, $"AUTH_LOG_{DateTime.Now:yyyyMMdd}.txt");
            
            _logger.LogInformation("🔍 Error Logging Service initialized. Log files:");
            _logger.LogInformation("   Error Log: {errorLog}", _errorLogPath);
            _logger.LogInformation("   Debug Log: {debugLog}", _debugLogPath);
            _logger.LogInformation("   Auth Log: {authLog}", _authLogPath);
        }

        /// <summary>
        /// Log authentication-related errors with detailed context
        /// </summary>
        public async Task LogAuthenticationError(string operation, Exception ex, object? context = null)
        {
            var errorMessage = new StringBuilder();
            errorMessage.AppendLine($"🔐 AUTHENTICATION ERROR - {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}");
            errorMessage.AppendLine($"Operation: {operation}");
            errorMessage.AppendLine($"Error Type: {ex.GetType().Name}");
            errorMessage.AppendLine($"Error Message: {ex.Message}");
            errorMessage.AppendLine($"Stack Trace: {ex.StackTrace}");
            
            if (context != null)
            {
                errorMessage.AppendLine($"Context: {context}");
            }
            
            errorMessage.AppendLine($"Environment: {Environment.OSVersion}");
            errorMessage.AppendLine($"Working Directory: {Environment.CurrentDirectory}");
            errorMessage.AppendLine($"Machine Name: {Environment.MachineName}");
            errorMessage.AppendLine($"User Name: {Environment.UserName}");
            errorMessage.AppendLine(new string('-', 80));

            await File.AppendAllTextAsync(_authLogPath, errorMessage.ToString());
            _logger.LogError(ex, "🔐 Authentication error in {operation}: {message}", operation, ex.Message);
        }

        /// <summary>
        /// Log database-related errors with connection details
        /// </summary>
        public async Task LogDatabaseError(string operation, Exception ex, string? connectionString = null)
        {
            var errorMessage = new StringBuilder();
            errorMessage.AppendLine($"🗄️ DATABASE ERROR - {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}");
            errorMessage.AppendLine($"Operation: {operation}");
            errorMessage.AppendLine($"Error Type: {ex.GetType().Name}");
            errorMessage.AppendLine($"Error Message: {ex.Message}");
            errorMessage.AppendLine($"Stack Trace: {ex.StackTrace}");
            
            if (!string.IsNullOrEmpty(connectionString))
            {
                // Mask sensitive information in connection string
                var maskedConnection = MaskConnectionString(connectionString);
                errorMessage.AppendLine($"Connection String: {maskedConnection}");
            }
            
            errorMessage.AppendLine($"Environment: {Environment.OSVersion}");
            errorMessage.AppendLine($"Working Directory: {Environment.CurrentDirectory}");
            errorMessage.AppendLine(new string('-', 80));

            await File.AppendAllTextAsync(_errorLogPath, errorMessage.ToString());
            _logger.LogError(ex, "🗄️ Database error in {operation}: {message}", operation, ex.Message);
        }

        /// <summary>
        /// Log API-related errors with request details
        /// </summary>
        public async Task LogApiError(string operation, Exception ex, object? requestData = null)
        {
            var errorMessage = new StringBuilder();
            errorMessage.AppendLine($"🌐 API ERROR - {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}");
            errorMessage.AppendLine($"Operation: {operation}");
            errorMessage.AppendLine($"Error Type: {ex.GetType().Name}");
            errorMessage.AppendLine($"Error Message: {ex.Message}");
            errorMessage.AppendLine($"Stack Trace: {ex.StackTrace}");
            
            if (requestData != null)
            {
                errorMessage.AppendLine($"Request Data: {requestData}");
            }
            
            errorMessage.AppendLine($"Environment: {Environment.OSVersion}");
            errorMessage.AppendLine($"Working Directory: {Environment.CurrentDirectory}");
            errorMessage.AppendLine(new string('-', 80));

            await File.AppendAllTextAsync(_errorLogPath, errorMessage.ToString());
            _logger.LogError(ex, "🌐 API error in {operation}: {message}", operation, ex.Message);
        }

        /// <summary>
        /// Log build/compilation errors with project details
        /// </summary>
        public async Task LogBuildError(string projectName, Exception ex, string? buildOutput = null)
        {
            var errorMessage = new StringBuilder();
            errorMessage.AppendLine($"🔨 BUILD ERROR - {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}");
            errorMessage.AppendLine($"Project: {projectName}");
            errorMessage.AppendLine($"Error Type: {ex.GetType().Name}");
            errorMessage.AppendLine($"Error Message: {ex.Message}");
            errorMessage.AppendLine($"Stack Trace: {ex.StackTrace}");
            
            if (!string.IsNullOrEmpty(buildOutput))
            {
                errorMessage.AppendLine($"Build Output: {buildOutput}");
            }
            
            errorMessage.AppendLine($"Environment: {Environment.OSVersion}");
            errorMessage.AppendLine($"Working Directory: {Environment.CurrentDirectory}");
            errorMessage.AppendLine($"NET Version: {Environment.Version}");
            errorMessage.AppendLine(new string('-', 80));

            await File.AppendAllTextAsync(_errorLogPath, errorMessage.ToString());
            _logger.LogError(ex, "🔨 Build error in {project}: {message}", projectName, ex.Message);
        }

        /// <summary>
        /// Log general errors with full context
        /// </summary>
        public async Task LogGeneralError(string operation, Exception ex, object? context = null)
        {
            var errorMessage = new StringBuilder();
            errorMessage.AppendLine($"❌ GENERAL ERROR - {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}");
            errorMessage.AppendLine($"Operation: {operation}");
            errorMessage.AppendLine($"Error Type: {ex.GetType().Name}");
            errorMessage.AppendLine($"Error Message: {ex.Message}");
            errorMessage.AppendLine($"Stack Trace: {ex.StackTrace}");
            
            if (context != null)
            {
                errorMessage.AppendLine($"Context: {context}");
            }
            
            errorMessage.AppendLine($"Environment: {Environment.OSVersion}");
            errorMessage.AppendLine($"Working Directory: {Environment.CurrentDirectory}");
            errorMessage.AppendLine($"Machine Name: {Environment.MachineName}");
            errorMessage.AppendLine($"User Name: {Environment.UserName}");
            errorMessage.AppendLine(new string('-', 80));

            await File.AppendAllTextAsync(_errorLogPath, errorMessage.ToString());
            _logger.LogError(ex, "❌ General error in {operation}: {message}", operation, ex.Message);
        }

        /// <summary>
        /// Log debug information for troubleshooting
        /// </summary>
        public async Task LogDebugInfo(string operation, string message, object? data = null)
        {
            var debugMessage = new StringBuilder();
            debugMessage.AppendLine($"🔍 DEBUG INFO - {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}");
            debugMessage.AppendLine($"Operation: {operation}");
            debugMessage.AppendLine($"Message: {message}");
            
            if (data != null)
            {
                debugMessage.AppendLine($"Data: {data}");
            }
            
            debugMessage.AppendLine(new string('-', 80));

            await File.AppendAllTextAsync(_debugLogPath, debugMessage.ToString());
            _logger.LogDebug("🔍 Debug info for {operation}: {message}", operation, message);
        }

        /// <summary>
        /// Get the current log file paths for user reference
        /// </summary>
        public (string ErrorLog, string DebugLog, string AuthLog) GetLogFilePaths()
        {
            return (_errorLogPath, _debugLogPath, _authLogPath);
        }

        /// <summary>
        /// Mask sensitive information in connection strings
        /// </summary>
        private string MaskConnectionString(string connectionString)
        {
            if (string.IsNullOrEmpty(connectionString))
                return string.Empty;

            var masked = connectionString;
            
            // Mask password
            if (masked.Contains("Password="))
            {
                var passwordIndex = masked.IndexOf("Password=");
                var endIndex = masked.IndexOf(';', passwordIndex);
                if (endIndex == -1) endIndex = masked.Length;
                var passwordLength = endIndex - passwordIndex - 9; // "Password=" is 9 chars
                masked = masked.Substring(0, passwordIndex + 9) + new string('*', Math.Max(3, passwordLength)) + masked.Substring(endIndex);
            }

            // Mask user ID if present
            if (masked.Contains("User ID="))
            {
                var userIndex = masked.IndexOf("User ID=");
                var endIndex = masked.IndexOf(';', userIndex);
                if (endIndex == -1) endIndex = masked.Length;
                var userLength = endIndex - userIndex - 8; // "User ID=" is 8 chars
                masked = masked.Substring(0, userIndex + 8) + new string('*', Math.Max(3, userLength)) + masked.Substring(endIndex);
            }

            return masked;
        }
    }
} 