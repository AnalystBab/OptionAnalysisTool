# Check service status with detailed information
Write-Host "🔍 === OPTION MARKET MONITOR STATUS CHECK ===" -ForegroundColor Cyan
Write-Host

# Check service status
$service = Get-Service -Name "OptionMarketMonitor" -ErrorAction SilentlyContinue

if ($service) {
    Write-Host "📊 Service Status:" -ForegroundColor Yellow
    Write-Host "Name: $($service.Name)"
    Write-Host "Status: $($service.Status)"
    Write-Host "StartType: $($service.StartType)"
    Write-Host

    if ($service.Status -eq "Running") {
        Write-Host "🟢 Service is RUNNING" -ForegroundColor Green
        Write-Host "- Data collection is active" -ForegroundColor Green
        Write-Host "- Circuit limit monitoring enabled" -ForegroundColor Green
        Write-Host "- Market Hours: 9:15 AM - 3:30 PM" -ForegroundColor Green
    }
    else {
        Write-Host "🔴 Service is NOT RUNNING" -ForegroundColor Red
        Write-Host "To start the service, run:" -ForegroundColor Yellow
        Write-Host "Start-Service OptionMarketMonitor" -ForegroundColor Yellow
    }
}
else {
    Write-Host "❌ Service is not installed!" -ForegroundColor Red
}

Write-Host
Write-Host "📋 Recent Logs:" -ForegroundColor Yellow
$logPath = "$env:LOCALAPPDATA\OptionAnalysisTool\logs"
if (Test-Path $logPath) {
    Get-ChildItem $logPath -Filter "*.log" | 
        Sort-Object LastWriteTime -Descending | 
        Select-Object -First 5 | 
        ForEach-Object {
            Write-Host "$($_.Name) - Last Updated: $($_.LastWriteTime)"
        }
}
else {
    Write-Host "No logs found at $logPath"
}

Write-Host
Write-Host "Press any key to continue..."
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown") 