# 🔥 OPTION MARKET MONITOR - WINDOWS SERVICE INSTALLER
# This script installs and configures the Indian Option Analysis Tool as a Windows Service
# The service will automatically start at boot and manage 9 AM market preparation

param(
    [Parameter(Mandatory=$false)]
    [ValidateSet("Install", "Uninstall", "Start", "Stop", "Restart", "Status")]
    [string]$Action = "Install"
)

# Service configuration
$serviceName = "IndianOptions_MarketMonitor"
$displayName = "Indian Options - Market Monitor Service"
$description = "Indian Options Analysis Tool - Real-time market data collection and circuit limit monitoring service for index options."

# Get the current directory
$currentDir = Split-Path -Parent $MyInvocation.MyCommand.Path

# Build the service executable path
$exePath = Join-Path $currentDir "OptionAnalysisTool.Console.exe"

Write-Host "🔥 === OPTION MARKET MONITOR SERVICE MANAGER ===" -ForegroundColor Yellow
Write-Host "🎯 Action: $Action" -ForegroundColor Cyan
Write-Host "📊 Service: $displayName" -ForegroundColor Green
Write-Host "📋 Service Details:" -ForegroundColor Cyan
Write-Host "   Name: $serviceName" -ForegroundColor White
Write-Host "   Display Name: $displayName" -ForegroundColor White
Write-Host "   Description: $description" -ForegroundColor White
Write-Host "   Path: $exePath" -ForegroundColor White

function Test-AdminRights {
    $currentUser = [Security.Principal.WindowsIdentity]::GetCurrent()
    $principal = New-Object Security.Principal.WindowsPrincipal($currentUser)
    return $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
}

function Install-Service {
    Write-Host "🔧 Installing Windows Service..."
    
    # Check if running as administrator
    if (-not (Test-AdminRights)) {
        Write-Host "❌ ERROR: This script must be run as Administrator to install services" -ForegroundColor Red
        Write-Host "💡 Right-click PowerShell and select 'Run as Administrator'" -ForegroundColor Yellow
        exit 1
    }
    
    # Check if executable exists
    if (-not (Test-Path $exePath)) {
        Write-Host "❌ ERROR: Executable not found at $exePath" -ForegroundColor Red
        Write-Host "💡 Please build the solution first using: dotnet build -c Release" -ForegroundColor Yellow
        exit 1
    }
    
    # Check if service already exists
    $existingService = Get-Service -Name $serviceName -ErrorAction SilentlyContinue
    if ($existingService) {
        Write-Host "⚠️ Service already exists. Stopping and removing..."
        Stop-Service -Name $serviceName -Force -ErrorAction SilentlyContinue
        Start-Sleep -Seconds 2
        sc.exe delete $serviceName
        Start-Sleep -Seconds 2
    }
    
    try {
        # Create the service
        $result = New-Service -Name $serviceName `
            -DisplayName $displayName `
            -Description $description `
            -BinaryPathName $exePath `
            -StartupType Automatic
        
        if ($result) {
            Write-Host "✅ Service installed successfully!"
            
            # Set recovery options (restart on failure)
            $action = sc.exe failure $serviceName reset= 86400 actions= restart/60000/restart/60000/restart/60000
            
            Write-Host "✅ Service recovery options configured"
            Write-Host
            Write-Host "Service will:"
            Write-Host "- Start automatically with Windows"
            Write-Host "- Restart automatically on failure"
            Write-Host "- Reset failure count after 24 hours"
            
            # Start the service
            Write-Host "🚀 Starting service..." -ForegroundColor Cyan
            Start-Service -Name $serviceName
            
            $service = Get-Service -Name $serviceName
            Write-Host "✅ Service Status: $($service.Status)" -ForegroundColor Green
            
            Write-Host "🎯 === INSTALLATION COMPLETED ===" -ForegroundColor Green
            Write-Host "📋 Service Details:" -ForegroundColor Cyan
            Write-Host "   Name: $serviceName" -ForegroundColor White
            Write-Host "   Display Name: $displayName" -ForegroundColor White
            Write-Host "   Status: $($service.Status)" -ForegroundColor White
            Write-Host "   Startup Type: Automatic" -ForegroundColor White
            Write-Host "   Executable: $exePath" -ForegroundColor White
            
            Write-Host ""
            Write-Host "💡 Next Steps:" -ForegroundColor Yellow
            Write-Host "1. Configure KiteConnect API credentials in appsettings.json" -ForegroundColor White
            Write-Host "2. Test the service: $PSCommandPath -Action Status" -ForegroundColor White
            Write-Host "3. View logs in Windows Event Viewer (Application log)" -ForegroundColor White
            Write-Host "4. Service will automatically manage market hours" -ForegroundColor White
            
        } else {
            Write-Host "❌ Failed to install service"
        }
    }
    catch {
        Write-Host "❌ Error installing service: $($_.Exception.Message)" -ForegroundColor Red
    }
}

function Uninstall-Service {
    Write-Host "🗑️ Uninstalling Option Market Monitor Service..." -ForegroundColor Yellow
    
    if (-not (Test-AdminRights)) {
        Write-Host "❌ ERROR: This script must be run as Administrator to uninstall services" -ForegroundColor Red
        exit 1
    }
    
    try {
        # Stop the service if running
        $service = Get-Service -Name $serviceName -ErrorAction SilentlyContinue
        if ($service -and $service.Status -eq 'Running') {
            Write-Host "🛑 Stopping service..." -ForegroundColor Cyan
            Stop-Service -Name $serviceName -Force
            Start-Sleep -Seconds 5
        }
        
        # Delete the service
        $result = sc.exe delete $serviceName
        
        if ($LASTEXITCODE -eq 0) {
            Write-Host "✅ Service uninstalled successfully" -ForegroundColor Green
        } else {
            Write-Host "⚠️ Service may not exist or could not be deleted. Exit code: $LASTEXITCODE" -ForegroundColor Yellow
        }
    }
    catch {
        Write-Host "❌ Error uninstalling service: $($_.Exception.Message)" -ForegroundColor Red
    }
}

function Start-ServiceAction {
    Write-Host "🚀 Starting Option Market Monitor Service..." -ForegroundColor Yellow
    
    try {
        Start-Service -Name $serviceName
        $service = Get-Service -Name $serviceName
        Write-Host "✅ Service started successfully. Status: $($service.Status)" -ForegroundColor Green
    }
    catch {
        Write-Host "❌ Error starting service: $($_.Exception.Message)" -ForegroundColor Red
    }
}

function Stop-ServiceAction {
    Write-Host "🛑 Stopping Option Market Monitor Service..." -ForegroundColor Yellow
    
    try {
        Stop-Service -Name $serviceName -Force
        $service = Get-Service -Name $serviceName
        Write-Host "✅ Service stopped successfully. Status: $($service.Status)" -ForegroundColor Green
    }
    catch {
        Write-Host "❌ Error stopping service: $($_.Exception.Message)" -ForegroundColor Red
    }
}

function Restart-ServiceAction {
    Write-Host "🔄 Restarting Option Market Monitor Service..." -ForegroundColor Yellow
    Stop-ServiceAction
    Start-Sleep -Seconds 3
    Start-ServiceAction
}

function Show-ServiceStatus {
    Write-Host "📊 === SERVICE STATUS ===" -ForegroundColor Cyan
    
    try {
        $service = Get-Service -Name $serviceName -ErrorAction SilentlyContinue
        
        if ($service) {
            Write-Host "✅ Service Found" -ForegroundColor Green
            Write-Host "   Name: $($service.Name)" -ForegroundColor White
            Write-Host "   Display Name: $($service.DisplayName)" -ForegroundColor White
            Write-Host "   Status: $($service.Status)" -ForegroundColor $(if ($service.Status -eq 'Running') { 'Green' } else { 'Yellow' })
            Write-Host "   Startup Type: $($service.StartType)" -ForegroundColor White
            
            # Show recent events
            Write-Host ""
            Write-Host "📋 Recent Events (last 10):" -ForegroundColor Cyan
            try {
                $events = Get-EventLog -LogName Application -Source "*OptionMarket*" -Newest 10 -ErrorAction SilentlyContinue
                if ($events) {
                    $events | ForEach-Object {
                        $color = switch ($_.EntryType) {
                            'Error' { 'Red' }
                            'Warning' { 'Yellow' }
                            default { 'White' }
                        }
                        Write-Host "   [$($_.TimeGenerated.ToString('MM/dd HH:mm:ss'))] $($_.EntryType): $($_.Message.Substring(0, [Math]::Min(100, $_.Message.Length)))" -ForegroundColor $color
                    }
                } else {
                    Write-Host "   No recent events found" -ForegroundColor Gray
                }
            }
            catch {
                Write-Host "   Could not retrieve events: $($_.Exception.Message)" -ForegroundColor Gray
            }
            
        } else {
            Write-Host "❌ Service '$serviceName' not found" -ForegroundColor Red
            Write-Host "💡 Use -Action Install to install the service" -ForegroundColor Yellow
        }
    }
    catch {
        Write-Host "❌ Error checking service status: $($_.Exception.Message)" -ForegroundColor Red
    }
}

# Execute the requested action
switch ($Action) {
    "Install" { Install-Service }
    "Uninstall" { Uninstall-Service }
    "Start" { Start-ServiceAction }
    "Stop" { Stop-ServiceAction }
    "Restart" { Restart-ServiceAction }
    "Status" { Show-ServiceStatus }
    default { 
        Write-Host "❌ Invalid action: $Action" -ForegroundColor Red
        Write-Host "Valid actions: Install, Uninstall, Start, Stop, Restart, Status" -ForegroundColor Yellow
    }
}

Write-Host ""
Write-Host "🔥 === OPERATION COMPLETED ===" -ForegroundColor Yellow 