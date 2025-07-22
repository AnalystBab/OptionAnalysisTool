using System;
using System.IO;
using System.Text.Json;
using Microsoft.Extensions.Options;
using System.Threading.Tasks;

namespace OptionAnalysisTool.KiteConnect
{
    public class TokenStorage
    {
        private readonly string _tokenFile;
        private readonly KiteConnectConfig _config;

        public TokenStorage(IOptions<KiteConnectConfig> config)
        {
            _config = config.Value;
            _tokenFile = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "OptionAnalysisTool",
                "kite_token.json"
            );
        }

        public class TokenData
        {
            public string AccessToken { get; set; } = string.Empty;
            public DateTime ExpiresAt { get; set; }
            public DateTime LastLoginTime { get; set; }
        }

        public async Task<(TokenData? Token, string Message)> LoadToken()
        {
            try
            {
                if (!File.Exists(_tokenFile))
                    return (null, "No saved login found. Please log in.");

                var json = await File.ReadAllTextAsync(_tokenFile);
                var token = JsonSerializer.Deserialize<TokenData>(json);

                if (token == null)
                    return (null, "Failed to deserialize token data.");

                // Check if token is expired (tokens expire at 6 AM IST the next day)
                if (token.ExpiresAt <= DateTime.UtcNow)
                {
                    var istTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, 
                        TimeZoneInfo.FindSystemTimeZoneById("India Standard Time"));
                    
                    return (null, $"Session expired at {token.ExpiresAt:HH:mm} IST. Daily login is required by Zerodha.");
                }

                // Calculate time until expiry
                var timeUntilExpiry = token.ExpiresAt - DateTime.UtcNow;
                if (timeUntilExpiry.TotalHours < 2)
                {
                    return (token, $"Session will expire in {timeUntilExpiry.TotalMinutes:0} minutes. Consider logging in again.");
                }

                return (token, $"Session valid until {token.ExpiresAt:HH:mm} IST");
            }
            catch (Exception ex)
            {
                return (null, $"Error loading saved login: {ex.Message}");
            }
        }

        public async Task SaveToken(string accessToken)
        {
            try
            {
                var istNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, 
                    TimeZoneInfo.FindSystemTimeZoneById("India Standard Time"));
                
                // Calculate expiry time (6 AM IST next day)
                var expiryDate = istNow.Date.AddDays(1).AddHours(6);
                if (istNow.Hour < 6) // If current time is before 6 AM, use today at 6 AM
                    expiryDate = istNow.Date.AddHours(6);

                var tokenData = new TokenData
                {
                    AccessToken = accessToken,
                    ExpiresAt = TimeZoneInfo.ConvertTimeToUtc(expiryDate, 
                        TimeZoneInfo.FindSystemTimeZoneById("India Standard Time")),
                    LastLoginTime = DateTime.UtcNow
                };

                var directory = Path.GetDirectoryName(_tokenFile);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                    Directory.CreateDirectory(directory);

                var json = JsonSerializer.Serialize(tokenData, new JsonSerializerOptions 
                { 
                    WriteIndented = true 
                });
                await File.WriteAllTextAsync(_tokenFile, json);
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to save login session: {ex.Message}", ex);
            }
        }

        public void ClearToken()
        {
            try
            {
                if (File.Exists(_tokenFile))
                    File.Delete(_tokenFile);
            }
            catch
            {
                // Ignore deletion errors
            }
        }
    }
} 