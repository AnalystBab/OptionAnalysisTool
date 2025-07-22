# Create Complete Monitoring Desktop Shortcuts
Write-Host "Creating Complete Monitoring Desktop Shortcuts..." -ForegroundColor Green

$projectDir = "C:\Users\babu\Documents\Medha"
$desktopPath = [Environment]::GetFolderPath("Desktop")
$folderPath = Join-Path $desktopPath "Indian Option Analysis Tool"

# Ensure the folder exists
if (!(Test-Path $folderPath)) {
    New-Item -ItemType Directory -Path $folderPath -Force | Out-Null
}

# Function to create shortcuts
function Create-MonitoringShortcut {
    param([string]$Name, [string]$Target, [string]$Args, [string]$Desc, [string]$Icon)
    $shortcutPath = Join-Path $folderPath "$Name.lnk"
    $shell = New-Object -ComObject WScript.Shell
    $shortcut = $shell.CreateShortcut($shortcutPath)
    $shortcut.TargetPath = $Target
    if ($Args) { $shortcut.Arguments = $Args }
    $shortcut.WorkingDirectory = $projectDir
    $shortcut.Description = $Desc
    if ($Icon) { $shortcut.IconLocation = $Icon }
    $shortcut.Save()
    Write-Host "   Created: $Name" -ForegroundColor Green
}

Write-Host "Creating monitoring shortcuts..." -ForegroundColor Yellow

# 1. Authentication (Required first)
Create-MonitoringShortcut -Name "1. Daily Authentication" -Target "$projectDir\DailyAuth.bat" -Desc "Daily token authentication for Kite Connect API" -Icon "shell32.dll,48"

# 2. Start Live Monitoring
Create-MonitoringShortcut -Name "2. Start Live Monitoring" -Target "$projectDir\StartLiveMonitoring.bat" -Desc "Start real-time circuit limit monitoring with notifications" -Icon "shell32.dll,24"

# 3. One-time Data Collection
Create-MonitoringShortcut -Name "3. Collect Current Data" -Target "$projectDir\StartDataService.bat" -Desc "One-time collection of current circuit limit data" -Icon "shell32.dll,13"

# 4. Excel Export
Create-MonitoringShortcut -Name "4. Export to Excel" -Target "$projectDir\ExportCircuitLimitsToExcel.bat" -Desc "Export all circuit limit data to Excel files" -Icon "shell32.dll,16"

# 5. View Database Data
Create-MonitoringShortcut -Name "5. Database Query Tool" -Target "$projectDir\DatabaseQueryScript.exe" -Desc "Query stored circuit limit data" -Icon "shell32.dll,15"

# 6. Circuit Limit SQL Query
Create-MonitoringShortcut -Name "6. Circuit Limit Query" -Target "notepad.exe" -Args "$projectDir\GetSpecificStrikeCircuitLimits.sql" -Desc "SQL query for yesterday's circuit limits" -Icon "shell32.dll,70"

# 7. Excel Reports Folder
Create-MonitoringShortcut -Name "7. Excel Reports Folder" -Target "$ENV:USERPROFILE\Desktop\CircuitLimitExcelReports" -Desc "View Excel export files" -Icon "shell32.dll,4"

# 8. Project Folder
Create-MonitoringShortcut -Name "8. Open Project Folder" -Target "$projectDir" -Desc "Access project files and logs" -Icon "shell32.dll,4"

Write-Host ""
Write-Host "MONITORING SHORTCUTS CREATED SUCCESSFULLY" -ForegroundColor Green
Write-Host ""
Write-Host "DAILY WORKFLOW:" -ForegroundColor Yellow
Write-Host "   1. Run 'Daily Authentication' every morning at 9:00 AM"
Write-Host "   2. Run 'Start Live Monitoring' to begin real-time tracking"
Write-Host "   3. Watch for circuit limit change notifications"
Write-Host "   4. Use 'Export to Excel' for manual Excel file generation"
Write-Host "   5. View 'Excel Reports Folder' for automatically updated files"
Write-Host ""
Write-Host "EXCEL FEATURES:" -ForegroundColor Cyan
Write-Host "   - Automatic Excel file updates when circuit limits change"
Write-Host "   - One file per index (NIFTY.csv, BANKNIFTY.csv, etc.)"
Write-Host "   - One sheet per expiry date within each index"
Write-Host "   - All strikes with current and previous circuit limits"
Write-Host "   - Change percentages and severity levels"
Write-Host "   - Backup files with timestamps"
Write-Host ""
Write-Host "NOTIFICATIONS:" -ForegroundColor Cyan
Write-Host "   - Windows toast notifications for circuit limit changes"
Write-Host "   - Console alerts with index-wise summaries"
Write-Host "   - Severity-based filtering (Critical, High, Medium, Low)"
Write-Host "   - Cooldown period to prevent spam (2 minutes per index)"
Write-Host ""
Write-Host "TIMING:" -ForegroundColor Magenta
Write-Host "   - Automatic activation during market hours (9:15 AM - 3:30 PM)"
Write-Host "   - Data collection every 30 seconds during trading"
Write-Host "   - Notification checks every 10 seconds"
Write-Host "   - Excel files updated immediately when changes detected"
Write-Host "   - Supports all indices: NIFTY, BANKNIFTY, SENSEX, BANKEX, etc."
Write-Host ""
Write-Host "DATA STORAGE:" -ForegroundColor Green
Write-Host "   - All data stored in IntradayOptionSnapshots table"
Write-Host "   - Circuit limit changes tracked in CircuitLimitTrackers table"
Write-Host "   - Spot prices stored with timestamps"
Write-Host "   - Excel files with complete analysis data"
Write-Host "   - Complete audit trail for strategy development"
Write-Host ""
Write-Host "Ready for production use!" -ForegroundColor Green 