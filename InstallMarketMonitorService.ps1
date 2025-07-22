# 🔥 INSTALL INDIAN OPTION MARKET MONITOR SERVICE
# Installs as Windows Service that runs 24/7 independently

Write-Host "🔥 === INSTALLING INDIAN OPTION MARKET MONITOR SERVICE ===" -ForegroundColor Green
Write-Host "🎯 This service will run 24/7 independently of the WPF application" -ForegroundColor Yellow

# Step 1: Check if running as administrator
if (-NOT ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole] "Administrator")) {
    Write-Host "❌ This script must be run as Administrator!" -ForegroundColor Red
    Write-Host "Right-click PowerShell and select 'Run as Administrator'" -ForegroundColor Yellow
    Read-Host "Press Enter to exit"
    exit 1
}

# Step 2: Build the console application
Write-Host "🔨 Building console application..." -ForegroundColor Cyan
dotnet publish OptionAnalysisTool.Console -c Release -o "./Published"

if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Build failed! Please fix build errors first." -ForegroundColor Red
    Read-Host "Press Enter to exit"
    exit 1
}

# Step 3: Set service paths
$ServiceName = "IndianOptions_OptionMarketMonitorService"
$ServiceDisplayName = "Indian Option Market Monitor Service"
$ServiceDescription = "Monitors Indian index option circuit limits and provides real-time tracking during market hours. Runs 24/7 independently."
$ServicePath = Join-Path $PWD "Published\OptionAnalysisTool.Console.exe"

# Step 4: Stop and remove existing service if it exists
Write-Host "🛑 Checking for existing service..." -ForegroundColor Cyan
$ExistingService = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue
if ($ExistingService) {
    Write-Host "⚠️ Existing service found. Stopping and removing..." -ForegroundColor Yellow
    Stop-Service -Name $ServiceName -Force -ErrorAction SilentlyContinue
    sc.exe delete $ServiceName
    Start-Sleep -Seconds 3
}

# Step 5: Install the new service
Write-Host "🔧 Installing new service..." -ForegroundColor Cyan
sc.exe create $ServiceName binPath=$ServicePath start=auto
if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Service installation failed!" -ForegroundColor Red
    Read-Host "Press Enter to exit"
    exit 1
}

# Step 6: Configure service settings
Write-Host "⚙️ Configuring service settings..." -ForegroundColor Cyan
sc.exe config $ServiceName DisplayName="$ServiceDisplayName"
sc.exe description $ServiceName "$ServiceDescription"
sc.exe config $ServiceName start=auto
sc.exe failure $ServiceName reset=0 actions=restart/60000/restart/60000/restart/60000

# Step 7: Start the service
Write-Host "🚀 Starting service..." -ForegroundColor Cyan
sc.exe start $ServiceName

# Step 8: Check service status
Start-Sleep -Seconds 5
$Service = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue
if ($Service -and $Service.Status -eq "Running") {
    Write-Host "✅ === SERVICE INSTALLED AND RUNNING SUCCESSFULLY ===" -ForegroundColor Green
    Write-Host ""
    Write-Host "🎯 Service Details:" -ForegroundColor Cyan
    Write-Host "   Name: $ServiceName" -ForegroundColor White
    Write-Host "   Display Name: $ServiceDisplayName" -ForegroundColor White
    Write-Host "   Status: $($Service.Status)" -ForegroundColor Green
    Write-Host "   Startup Type: Automatic" -ForegroundColor White
    Write-Host "   Path: $ServicePath" -ForegroundColor White
    Write-Host ""
    Write-Host "🔥 THE SERVICE IS NOW RUNNING 24/7 INDEPENDENTLY!" -ForegroundColor Green
    Write-Host "📊 It will automatically start with Windows" -ForegroundColor Yellow
    Write-Host "💡 Monitor with: Get-Service '$ServiceName'" -ForegroundColor Cyan
    Write-Host "🛑 Stop with: Stop-Service '$ServiceName'" -ForegroundColor Cyan
    Write-Host "🚀 Start with: Start-Service '$ServiceName'" -ForegroundColor Cyan
} else {
    Write-Host "❌ Service installation completed but failed to start!" -ForegroundColor Red
    Write-Host "Check Windows Event Viewer for error details" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "🎉 Installation complete! Service is monitoring circuit limits 24/7." -ForegroundColor Green
Read-Host "Press Enter to exit" 