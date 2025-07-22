# 🚀 SETUP WINDOWS STARTUP FOR OPTION DATA SERVICE
# This script configures the service to start automatically with Windows

Write-Host "🔧 SETTING UP WINDOWS STARTUP" -ForegroundColor Green
Write-Host "==============================" -ForegroundColor Green
Write-Host ""

# Get current directory
$currentDir = Get-Location
$servicePath = Join-Path $currentDir "StartOptionDataService.bat"

Write-Host "📂 Service location: $servicePath" -ForegroundColor Yellow

if (-not (Test-Path $servicePath)) {
    Write-Host "❌ ERROR: StartOptionDataService.bat not found!" -ForegroundColor Red
    Write-Host "Please run this script from the OptionAnalysisTool directory." -ForegroundColor Red
    Read-Host "Press Enter to exit"
    exit 1
}

# Create startup registry entry
$regPath = "HKCU:\SOFTWARE\Microsoft\Windows\CurrentVersion\Run"
$regName = "OptionDataService"

try {
    Write-Host "🔧 Adding to Windows startup registry..." -ForegroundColor Yellow
    
    # Add registry entry
    Set-ItemProperty -Path $regPath -Name $regName -Value $servicePath -Force
    
    Write-Host "✅ Windows startup configured successfully!" -ForegroundColor Green
    Write-Host ""
    Write-Host "📋 STARTUP CONFIGURATION COMPLETE" -ForegroundColor Green
    Write-Host "==================================" -ForegroundColor Green
    Write-Host ""
    Write-Host "✅ Service will now start automatically when Windows starts" -ForegroundColor White
    Write-Host "✅ Desktop status widget will appear in system tray" -ForegroundColor White
    Write-Host "✅ Daily authentication prompts will appear as needed" -ForegroundColor White
    Write-Host ""
    Write-Host "🔄 NEXT STEPS:" -ForegroundColor Cyan
    Write-Host "1. Restart Windows to test automatic startup" -ForegroundColor White
    Write-Host "2. Look for the status widget in system tray" -ForegroundColor White
    Write-Host "3. Complete daily authentication when prompted" -ForegroundColor White
    Write-Host ""
    Write-Host "💡 TIP: To disable startup, delete the registry entry:" -ForegroundColor Yellow
    Write-Host "   Registry Path: $regPath" -ForegroundColor Gray
    Write-Host "   Entry Name: $regName" -ForegroundColor Gray
    
}
catch {
    Write-Host "❌ ERROR: Failed to configure startup!" -ForegroundColor Red
    Write-Host "Error details: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host ""
    Write-Host "🔧 MANUAL SETUP INSTRUCTIONS:" -ForegroundColor Yellow
    Write-Host "1. Press Win+R" -ForegroundColor White
    Write-Host "2. Type: shell:startup" -ForegroundColor White
    Write-Host "3. Copy StartOptionDataService.bat to the startup folder" -ForegroundColor White
}

Write-Host ""
Read-Host "Press Enter to continue"

# Optional: Create scheduled task for better control
Write-Host ""
Write-Host "🕘 OPTIONAL: SETUP SCHEDULED TASK" -ForegroundColor Cyan
Write-Host "=================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Would you like to setup a scheduled task instead?" -ForegroundColor Yellow
Write-Host "Benefits of scheduled task:" -ForegroundColor White
Write-Host "  ✅ More reliable startup" -ForegroundColor White
Write-Host "  ✅ Runs even if user not logged in" -ForegroundColor White
Write-Host "  ✅ Better error handling" -ForegroundColor White
Write-Host "  ✅ Easier to manage" -ForegroundColor White
Write-Host ""

$choice = Read-Host "Setup scheduled task? (y/n)"

if ($choice -eq "y" -or $choice -eq "Y") {
    try {
        Write-Host "🔧 Creating scheduled task..." -ForegroundColor Yellow
        
        # Create scheduled task
        $taskName = "OptionDataService"
        $action = New-ScheduledTaskAction -Execute $servicePath
        $trigger = New-ScheduledTaskTrigger -AtLogOn
        $settings = New-ScheduledTaskSettingsSet -AllowStartIfOnBatteries -DontStopIfGoingOnBatteries
        $principal = New-ScheduledTaskPrincipal -UserId $env:USERNAME -LogonType Interactive
        
        Register-ScheduledTask -TaskName $taskName -Action $action -Trigger $trigger -Settings $settings -Principal $principal -Force
        
        Write-Host "✅ Scheduled task created successfully!" -ForegroundColor Green
        Write-Host ""
        Write-Host "📋 TASK DETAILS:" -ForegroundColor Green
        Write-Host "Task Name: $taskName" -ForegroundColor White
        Write-Host "Trigger: At user logon" -ForegroundColor White
        Write-Host "Action: Start Option Data Service" -ForegroundColor White
        Write-Host ""
        Write-Host "🔧 To manage the task:" -ForegroundColor Yellow
        Write-Host "1. Open Task Scheduler (taskschd.msc)" -ForegroundColor White
        Write-Host "2. Look for '$taskName' in Task Scheduler Library" -ForegroundColor White
        
    }
    catch {
        Write-Host "❌ ERROR: Failed to create scheduled task!" -ForegroundColor Red
        Write-Host "Error details: $($_.Exception.Message)" -ForegroundColor Red
        Write-Host ""
        Write-Host "🔧 You can create it manually:" -ForegroundColor Yellow
        Write-Host "1. Open Task Scheduler" -ForegroundColor White
        Write-Host "2. Create Basic Task" -ForegroundColor White
        Write-Host "3. Trigger: At log on" -ForegroundColor White
        Write-Host "4. Action: Start program -> $servicePath" -ForegroundColor White
    }
}

Write-Host ""
Write-Host "🎉 SETUP COMPLETE!" -ForegroundColor Green
Write-Host "==================" -ForegroundColor Green
Write-Host ""
Write-Host "Your Option Data Service is now configured to start automatically!" -ForegroundColor White
Write-Host ""
Read-Host "Press Enter to exit" 