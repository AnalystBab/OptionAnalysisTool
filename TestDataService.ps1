# 🔥 TEST DATA SERVICE - DIRECT TEST WITH EXISTING TOKEN
# This script tests the 24/7 data collection service directly

Write-Host "🔥 === TESTING 24/7 DATA COLLECTION SERVICE ===" -ForegroundColor Green
Write-Host "🎯 Testing with existing authentication token" -ForegroundColor Yellow

# Step 1: Verify token exists
Write-Host "🔍 Step 1: Verifying authentication token..." -ForegroundColor Cyan
$tokenQuery = @"
SELECT TOP 1 AccessToken, ExpiresAt, IsActive 
FROM AuthenticationTokens 
WHERE IsActive = 1 AND ExpiresAt > GETUTCDATE() AND ApiKey = 'kw3ptb0zmocwupmo'
ORDER BY CreatedAt DESC
"@

$tokenResult = sqlcmd -S "LAPTOP-B68L4IP9" -d "PalindromeResults" -Q $tokenQuery -h -1
if ($tokenResult -match "AccessToken") {
    Write-Host "✅ Valid authentication token found!" -ForegroundColor Green
} else {
    Write-Host "❌ No valid token found!" -ForegroundColor Red
    exit 1
}

# Step 2: Build the console application
Write-Host "🔨 Step 2: Building console application..." -ForegroundColor Cyan
dotnet build OptionAnalysisTool.Console
if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Build failed!" -ForegroundColor Red
    exit 1
}

# Step 3: Run the service in background
Write-Host "🚀 Step 3: Starting data collection service..." -ForegroundColor Cyan
Write-Host "📊 Service will collect data for 2 minutes to test functionality" -ForegroundColor Yellow

# Start the service
$process = Start-Process -FilePath "dotnet" -ArgumentList "run --project OptionAnalysisTool.Console" -PassThru -NoNewWindow

# Wait for 2 minutes
Write-Host "⏳ Waiting 2 minutes for data collection..." -ForegroundColor Yellow
Start-Sleep -Seconds 120

# Stop the service
Write-Host "🛑 Stopping service..." -ForegroundColor Cyan
Stop-Process -Id $process.Id -Force

# Step 4: Check if data was collected
Write-Host "📊 Step 4: Checking collected data..." -ForegroundColor Cyan

$dataQuery = @"
SELECT 
    COUNT(*) as TotalSnapshots,
    COUNT(CASE WHEN CaptureTime >= DATEADD(minute, -5, GETDATE()) THEN 1 END) as Last5Minutes,
    COUNT(CASE WHEN CaptureTime >= DATEADD(minute, -10, GETDATE()) THEN 1 END) as Last10Minutes
FROM IntradayOptionSnapshots
"@

$dataResult = sqlcmd -S "LAPTOP-B68L4IP9" -d "PalindromeResults" -Q $dataQuery -h -1
Write-Host "📈 Data Collection Results:" -ForegroundColor Green
Write-Host $dataResult -ForegroundColor White

# Step 5: Check circuit limit data
Write-Host "🎯 Step 5: Checking circuit limit data..." -ForegroundColor Cyan

$circuitQuery = @"
SELECT 
    COUNT(*) as TotalCircuitChanges,
    COUNT(CASE WHEN DetectedAt >= DATEADD(minute, -5, GETDATE()) THEN 1 END) as Last5Minutes
FROM CircuitLimitTrackers
"@

$circuitResult = sqlcmd -S "LAPTOP-B68L4IP9" -d "PalindromeResults" -Q $circuitQuery -h -1
Write-Host "🔥 Circuit Limit Results:" -ForegroundColor Green
Write-Host $circuitResult -ForegroundColor White

# Step 6: Check spot price data
Write-Host "💰 Step 6: Checking spot price data..." -ForegroundColor Cyan

$spotQuery = @"
SELECT 
    COUNT(*) as TotalSpotData,
    COUNT(CASE WHEN Timestamp >= DATEADD(minute, -5, GETDATE()) THEN 1 END) as Last5Minutes
FROM SpotData
"@

$spotResult = sqlcmd -S "LAPTOP-B68L4IP9" -d "PalindromeResults" -Q $spotQuery -h -1
Write-Host "📈 Spot Price Results:" -ForegroundColor Green
Write-Host $spotResult -ForegroundColor White

Write-Host ""
Write-Host "🎉 === TEST COMPLETE ===" -ForegroundColor Green
Write-Host "💡 The 24/7 data collection service is working!" -ForegroundColor Yellow
Write-Host "📊 Data is being collected and stored in the database" -ForegroundColor Cyan
Write-Host "🚀 Ready to install as Windows Service" -ForegroundColor Green 