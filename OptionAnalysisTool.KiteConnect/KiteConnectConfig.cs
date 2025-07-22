using System;

namespace OptionAnalysisTool.KiteConnect
{
    public class KiteConnectConfig
    {
        public required string ApiKey { get; set; }
        public required string ApiSecret { get; set; }
        public required string RedirectUrl { get; set; }
        public string? AccessToken { get; set; }
    }
} 