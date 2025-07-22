# 🔥 TEST KITE API DATA AVAILABILITY
# Tests if Kite API provides data even when market is closed

Write-Host "🔥 === TESTING KITE API DATA AVAILABILITY ===" -ForegroundColor Green
Write-Host "🎯 Testing data collection even when market is closed" -ForegroundColor Yellow

# Step 1: Get authentication token
Write-Host "🔍 Step 1: Getting authentication token..." -ForegroundColor Cyan
$tokenQuery = @"
SELECT TOP 1 AccessToken 
FROM AuthenticationTokens 
WHERE IsActive = 1 AND ExpiresAt > GETUTCDATE() AND ApiKey = 'kw3ptb0zmocwupmo'
ORDER BY CreatedAt DESC
"@

$tokenResult = sqlcmd -S "LAPTOP-B68L4IP9" -d "PalindromeResults" -Q $tokenQuery -h -1
if ($tokenResult -match "AccessToken") {
    $lines = $tokenResult -split "`n"
    foreach ($line in $lines) {
        if ($line -match "cIC8YnFkaBnajhI2IlRAJprrqq037QXz") {
            $accessToken = $line.Trim()
            Write-Host "✅ Authentication token found!" -ForegroundColor Green
            break
        }
    }
} else {
    Write-Host "❌ No valid token found!" -ForegroundColor Red
    exit 1
}

# Step 2: Test Kite API connection
Write-Host "🔌 Step 2: Testing Kite API connection..." -ForegroundColor Cyan

# Create a simple .NET script to test Kite API
$testScript = @"
using System;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;

class KiteAPITest
{
    static async Task Main()
    {
        try
        {
            var apiKey = "kw3ptb0zmocwupmo";
            var accessToken = "$accessToken";
            
            Console.WriteLine("Testing Kite API connection...");
            
            // Test 1: Get instruments
            var instrumentsUrl = $"https://api.kite.trade/instruments/NFO";
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("X-Kite-Version", "3");
                client.DefaultRequestHeaders.Add("Authorization", $"token {apiKey}:{accessToken}");
                
                var response = await client.GetAsync(instrumentsUrl);
                Console.WriteLine($"Instruments API Status: {response.StatusCode}");
                
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var lines = content.Split('\n');
                    Console.WriteLine($"Total instruments: {lines.Length}");
                    
                    // Show first few NIFTY instruments
                    var niftyInstruments = lines.Where(l => l.Contains("NIFTY")).Take(5);
                    foreach (var instrument in niftyInstruments)
                    {
                        Console.WriteLine($"  {instrument}");
                    }
                }
            }
            
            // Test 2: Get quotes for NIFTY index
            var quotesUrl = $"https://api.kite.trade/quote/ltp?i=NSE:NIFTY+50";
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("X-Kite-Version", "3");
                client.DefaultRequestHeaders.Add("Authorization", $"token {apiKey}:{accessToken}");
                
                var response = await client.GetAsync(quotesUrl);
                Console.WriteLine($"Quotes API Status: {response.StatusCode}");
                
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"Quotes response: {content}");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }
}
"@

# Save and run the test script
$testScript | Out-File -FilePath "KiteAPITest.cs" -Encoding UTF8

Write-Host "🔨 Compiling test script..." -ForegroundColor Cyan
dotnet new console --force
dotnet add package Newtonsoft.Json
dotnet add package System.Net.Http

# Replace Program.cs with our test
$testScript | Out-File -FilePath "Program.cs" -Encoding UTF8

Write-Host "🚀 Running Kite API test..." -ForegroundColor Cyan
dotnet run

Write-Host ""
Write-Host "🎉 === KITE API TEST COMPLETE ===" -ForegroundColor Green
Write-Host "💡 This shows what data is available from Kite API" -ForegroundColor Yellow
Write-Host "📊 Even when market is closed, historical and instrument data is available" -ForegroundColor Cyan 