using System;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using OptionAnalysisTool.Common.Data;
using OptionAnalysisTool.Models;

namespace OptionAnalysisTool.Common.Services
{
    /// <summary>
    /// 🔐 DATABASE TOKEN SERVICE
    /// Centralized access token management in database
    /// Replaces file-based and config-based token storage
    /// </summary>
    public class DatabaseTokenService
    {
        private readonly ILogger<DatabaseTokenService> _logger;
        private readonly ApplicationDbContext _dbContext;

        public DatabaseTokenService(
            ILogger<DatabaseTokenService> logger,
            ApplicationDbContext dbContext)
        {
            _logger = logger;
            _dbContext = dbContext;
        }

        /// <summary>
        /// 💾 STORE ACCESS TOKEN IN DATABASE
        /// Called after successful authentication to save token for data services
        /// </summary>
        public async Task<bool> StoreAccessTokenAsync(
            string accessToken, 
            string apiKey, 
            string? apiSecret = null,
            string source = "Manual",
            string? userId = null,
            string? userName = null)
        {
            try
            {
                _logger.LogInformation("💾 Storing access token in database (Source: {source})", source);

                // 🚨 DEACTIVATE ALL EXISTING TOKENS - Keep only one active token
                var allExistingTokens = await _dbContext.AuthenticationTokens
                    .Where(t => t.IsActive)
                    .ToListAsync();

                foreach (var token in allExistingTokens)
                {
                    token.IsActive = false;
                    token.UpdatedAt = DateTime.UtcNow;
                    _logger.LogDebug("Deactivated old token ID: {tokenId} (Source: {source})", token.Id, token.Source);
                }

                // Calculate expiry time (6 AM IST next day)
                var istNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, 
                    TimeZoneInfo.FindSystemTimeZoneById("India Standard Time"));
                var expiryDate = istNow.Date.AddDays(1).AddHours(6);
                if (istNow.Hour < 6) // If current time is before 6 AM, use today at 6 AM
                    expiryDate = istNow.Date.AddHours(6);

                var expiryUtc = TimeZoneInfo.ConvertTimeToUtc(expiryDate, 
                    TimeZoneInfo.FindSystemTimeZoneById("India Standard Time"));

                // Create new token record
                var newToken = new AuthenticationToken
                {
                    AccessToken = accessToken,
                    ApiKey = apiKey,
                    ApiSecret = apiSecret, // Could be encrypted if needed
                    UserId = userId,
                    UserName = userName,
                    CreatedAt = DateTime.UtcNow,
                    ExpiresAt = expiryUtc,
                    LastUsedAt = DateTime.UtcNow,
                    IsActive = true,
                    Source = source,
                    UpdatedAt = DateTime.UtcNow
                };

                _dbContext.AuthenticationTokens.Add(newToken);
                await _dbContext.SaveChangesAsync();

                _logger.LogInformation("✅ Access token stored successfully in database");
                _logger.LogInformation("   Token ID: {tokenId}", newToken.Id);
                _logger.LogInformation("   Expires: {expiry:yyyy-MM-dd HH:mm} IST", expiryDate);
                _logger.LogInformation("   Source: {source}", source);
                _logger.LogInformation("   Deactivated {count} old tokens", allExistingTokens.Count);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Failed to store access token in database");
                return false;
            }
        }

        /// <summary>
        /// 🔍 GET CURRENT VALID ACCESS TOKEN FROM DATABASE
        /// Used by ALL data services to fetch the current active token
        /// </summary>
        public async Task<string?> GetCurrentAccessTokenAsync(string? apiKey = null)
        {
            try
            {
                var query = _dbContext.AuthenticationTokens
                    .Where(t => t.IsActive && t.ExpiresAt > DateTime.UtcNow);

                if (!string.IsNullOrEmpty(apiKey))
                {
                    query = query.Where(t => t.ApiKey == apiKey);
                }

                var currentToken = await query
                    .OrderByDescending(t => t.CreatedAt)
                    .FirstOrDefaultAsync();

                if (currentToken != null)
                {
                    // Update last used timestamp
                    currentToken.LastUsedAt = DateTime.UtcNow;
                    currentToken.UpdatedAt = DateTime.UtcNow;
                    await _dbContext.SaveChangesAsync();

                    _logger.LogDebug("✅ Retrieved active access token from database (ID: {tokenId})", currentToken.Id);
                    return currentToken.AccessToken;
                }

                _logger.LogWarning("⚠️ No active access token found in database");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error retrieving access token from database");
                return null;
            }
        }

        /// <summary>
        /// 📊 GET TOKEN STATUS FROM DATABASE
        /// </summary>
        public async Task<(bool IsValid, DateTime? ExpiresAt, string? Source, string? UserId)> GetTokenStatusAsync(string? apiKey = null)
        {
            try
            {
                var query = _dbContext.AuthenticationTokens
                    .Where(t => t.IsActive);

                if (!string.IsNullOrEmpty(apiKey))
                {
                    query = query.Where(t => t.ApiKey == apiKey);
                }

                var currentToken = await query
                    .OrderByDescending(t => t.CreatedAt)
                    .FirstOrDefaultAsync();

                if (currentToken != null)
                {
                    var isValid = currentToken.ExpiresAt > DateTime.UtcNow;
                    return (isValid, currentToken.ExpiresAt, currentToken.Source, currentToken.UserId);
                }

                return (false, null, null, null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting token status from database");
                return (false, null, null, null);
            }
        }

        /// <summary>
        /// 🧹 CLEANUP EXPIRED TOKENS
        /// Remove old expired tokens from database
        /// </summary>
        public async Task<int> CleanupExpiredTokensAsync(int keepDays = 1) // Reduced to 1 day since we only need recent tokens
        {
            try
            {
                var cutoffDate = DateTime.UtcNow.AddDays(-keepDays);
                
                // Remove all inactive tokens older than cutoff date
                var expiredTokens = await _dbContext.AuthenticationTokens
                    .Where(t => !t.IsActive && t.CreatedAt < cutoffDate)
                    .ToListAsync();

                if (expiredTokens.Any())
                {
                    _dbContext.AuthenticationTokens.RemoveRange(expiredTokens);
                    await _dbContext.SaveChangesAsync();
                    
                    _logger.LogInformation("🧹 Cleaned up {count} old inactive tokens", expiredTokens.Count);
                }

                return expiredTokens.Count;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cleaning up expired tokens");
                return 0;
            }
        }

        /// <summary>
        /// 🚨 FORCE CLEANUP - Remove all tokens except the most recent active one
        /// </summary>
        public async Task<int> ForceCleanupAllTokensExceptLatestAsync()
        {
            try
            {
                // Get the most recent active token
                var latestActiveToken = await _dbContext.AuthenticationTokens
                    .Where(t => t.IsActive)
                    .OrderByDescending(t => t.CreatedAt)
                    .FirstOrDefaultAsync();

                if (latestActiveToken == null)
                {
                    _logger.LogWarning("No active tokens found for cleanup");
                    return 0;
                }

                // Remove all other tokens (both active and inactive)
                var tokensToRemove = await _dbContext.AuthenticationTokens
                    .Where(t => t.Id != latestActiveToken.Id)
                    .ToListAsync();

                if (tokensToRemove.Any())
                {
                    _dbContext.AuthenticationTokens.RemoveRange(tokensToRemove);
                    await _dbContext.SaveChangesAsync();
                    
                    _logger.LogInformation("🚨 Force cleanup: Removed {count} tokens, kept only latest active token (ID: {tokenId})", 
                        tokensToRemove.Count, latestActiveToken.Id);
                }

                return tokensToRemove.Count;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in force cleanup");
                return 0;
            }
        }

        /// <summary>
        /// 📈 GET TOKEN STATISTICS
        /// </summary>
        public async Task<(int ActiveTokens, int TotalTokens, DateTime? LastTokenDate)> GetTokenStatsAsync()
        {
            try
            {
                var totalTokens = await _dbContext.AuthenticationTokens.CountAsync();
                var activeTokens = await _dbContext.AuthenticationTokens
                    .CountAsync(t => t.IsActive && t.ExpiresAt > DateTime.UtcNow);
                var lastTokenDate = await _dbContext.AuthenticationTokens
                    .OrderByDescending(t => t.CreatedAt)
                    .Select(t => t.CreatedAt)
                    .FirstOrDefaultAsync();

                return (activeTokens, totalTokens, lastTokenDate);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting token statistics");
                return (0, 0, null);
            }
        }

        /// <summary>
        /// ⚡ QUICK CHECK - Is there a valid token available?
        /// </summary>
        public async Task<bool> HasValidTokenAsync(string? apiKey = null)
        {
            try
            {
                var query = _dbContext.AuthenticationTokens
                    .Where(t => t.IsActive && t.ExpiresAt > DateTime.UtcNow);

                if (!string.IsNullOrEmpty(apiKey))
                {
                    query = query.Where(t => t.ApiKey == apiKey);
                }

                return await query.AnyAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking for valid token");
                return false;
            }
        }

        public async Task<bool> ValidateCurrentTokenAsync()
        {
            try
            {
                var latestToken = await _dbContext.AuthenticationTokens
                    .OrderByDescending(t => t.CreatedAt)
                    .FirstOrDefaultAsync();

                if (latestToken == null)
                {
                    _logger.LogWarning("No authentication token found");
                    return false;
                }

                // Check if token is expired
                if (latestToken.ExpiresAt <= DateTime.UtcNow)
                {
                    _logger.LogWarning("Authentication token has expired");
                    return false;
                }

                return latestToken.IsActive;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating current token");
                return false;
            }
        }
    }
} 