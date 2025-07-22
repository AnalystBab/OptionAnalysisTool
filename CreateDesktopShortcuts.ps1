# Create Desktop Shortcuts for Indian Option Analysis Tool
# Run this script as Administrator

Write-Host "🚀 Creating Desktop Shortcuts for Indian Option Analysis Tool" -ForegroundColor Green
Write-Host "================================================================" -ForegroundColor Green

# Get current directory and desktop path
$currentDir = Get-Location
$desktopPath = [Environment]::GetFolderPath("Desktop")
$folderName = "Indian Option Analysis Tool"
$folderPath = Join-Path $desktopPath $folderName

# Create the main folder
Write-Host "📁 Creating folder: $folderName" -ForegroundColor Yellow
if (!(Test-Path $folderPath)) {
    New-Item -ItemType Directory -Path $folderPath -Force | Out-Null
    Write-Host "   ✅ Folder created successfully" -ForegroundColor Green
} else {
    Write-Host "   ℹ️  Folder already exists" -ForegroundColor Cyan
}

# Function to create shortcuts
function Create-Shortcut {
    param(
        [string]$ShortcutName,
        [string]$TargetPath,
        [string]$Arguments = "",
        [string]$WorkingDirectory = $currentDir,
        [string]$Description = "",
        [string]$IconPath = ""
    )
    
    $shortcutPath = Join-Path $folderPath "$ShortcutName.lnk"
    $shell = New-Object -ComObject WScript.Shell
    $shortcut = $shell.CreateShortcut($shortcutPath)
    $shortcut.TargetPath = $TargetPath
    $shortcut.Arguments = $Arguments
    $shortcut.WorkingDirectory = $WorkingDirectory
    $shortcut.Description = $Description
    if ($IconPath) {
        $shortcut.IconLocation = $IconPath
    }
    $shortcut.Save()
    Write-Host "   ✅ Created: $ShortcutName" -ForegroundColor Green
}

Write-Host ""
Write-Host "🔐 Creating Authentication Shortcuts..." -ForegroundColor Yellow

# 1. Daily Authentication
Create-Shortcut -ShortcutName "1. Daily Authentication" `
                -TargetPath "cmd.exe" `
                -Arguments "/c `"$currentDir\DailyAuth.bat`"" `
                -Description "Daily Kite Connect authentication - Run every morning at 9 AM"

Write-Host ""
Write-Host "📊 Creating Data Service Shortcuts..." -ForegroundColor Yellow

# 2. Start Data Service
Create-Shortcut -ShortcutName "2. Start Data Service" `
                -TargetPath "cmd.exe" `
                -Arguments "/c `"$currentDir\StartDataService.bat`"" `
                -Description "Start the option data collection service"

# 3. Data Service (Direct)
Create-Shortcut -ShortcutName "3. Data Service (Console)" `
                -TargetPath "dotnet.exe" `
                -Arguments "run --project OptionAnalysisTool.Console" `
                -Description "Run data service directly in console"

Write-Host ""
Write-Host "🛠️ Creating Utility Shortcuts..." -ForegroundColor Yellow

# 4. Database Query Tool
Create-Shortcut -ShortcutName "4. Database Query Tool" `
                -TargetPath "dotnet.exe" `
                -Arguments "run --project DatabaseQueryScript.csproj" `
                -Description "Query and check database data"

# 5. Quick Database Auth (Backup)
Create-Shortcut -ShortcutName "5. Quick DB Auth (Backup)" `
                -TargetPath "dotnet.exe" `
                -Arguments "run --project QuickDbAuth.csproj" `
                -Description "Alternative authentication method"

Write-Host ""
Write-Host "📋 Creating Management Shortcuts..." -ForegroundColor Yellow

# 6. Check Service Status
Create-Shortcut -ShortcutName "6. Check Service Status" `
                -TargetPath "cmd.exe" `
                -Arguments "/c `"$currentDir\CheckService.bat`"" `
                -Description "Check if services are running properly"

# 7. Project Folder
Create-Shortcut -ShortcutName "7. Open Project Folder" `
                -TargetPath "explorer.exe" `
                -Arguments "`"$currentDir`"" `
                -Description "Open the main project directory"

# 8. Database Management
Create-Shortcut -ShortcutName "8. Database Management" `
                -TargetPath "sqlcmd.exe" `
                -Arguments "-S LAPTOP-B68L4IP9 -E -d PalindromeResults" `
                -Description "Open SQL command line for database"

Write-Host ""
Write-Host "📖 Creating Documentation Shortcuts..." -ForegroundColor Yellow

# 9. Quick Start Guide
if (Test-Path "QUICK_START.md") {
    Create-Shortcut -ShortcutName "9. Quick Start Guide" `
                    -TargetPath "notepad.exe" `
                    -Arguments "`"$currentDir\QUICK_START.md`"" `
                    -Description "Read the quick start guide"
}

# 10. System Status
if (Test-Path "SystemStatusWidget.ps1") {
    Create-Shortcut -ShortcutName "10. System Status Widget" `
                    -TargetPath "powershell.exe" `
                    -Arguments "-ExecutionPolicy Bypass -File `"$currentDir\SystemStatusWidget.ps1`"" `
                    -Description "Show system status widget"
}

Write-Host ""
Write-Host "🔧 Creating Build and Maintenance Shortcuts..." -ForegroundColor Yellow

# 11. Build All Projects
Create-Shortcut -ShortcutName "11. Build All Projects" `
                -TargetPath "cmd.exe" `
                -Arguments "/c `"cd /d `"$currentDir`" `& dotnet build `& pause`"" `
                -Description "Build all projects in the solution"

# 12. Clean Database
if (Test-Path "ClearDatabaseFreshStart.csproj") {
    Create-Shortcut -ShortcutName "12. Clean Database (Fresh Start)" `
                    -TargetPath "dotnet.exe" `
                    -Arguments "run --project ClearDatabaseFreshStart.csproj" `
                    -Description "Clear database for fresh start"
}

Write-Host ""
Write-Host "📝 Creating README file..." -ForegroundColor Yellow

# Create a README file in the folder
$readmeContent = @"
🚀 INDIAN OPTION ANALYSIS TOOL - DESKTOP SHORTCUTS
==================================================

📅 DAILY ROUTINE:
1. Run "1. Daily Authentication" every morning at 9:00 AM
2. Run "2. Start Data Service" to begin data collection
3. Use "6. Check Service Status" to monitor

🔐 AUTHENTICATION:
- Primary: "1. Daily Authentication" (Automatic browser opening)
- Backup: "5. Quick DB Auth (Backup)" (Manual process)

📊 DATA SERVICES:
- "2. Start Data Service" - Main service with logging
- "3. Data Service (Console)" - Direct console access

🛠️ UTILITIES:
- "4. Database Query Tool" - Check stored data
- "8. Database Management" - Direct SQL access
- "11. Build All Projects" - Rebuild if needed

📋 MONITORING:
- "6. Check Service Status" - Service health check
- "10. System Status Widget" - Real-time status

🔧 MAINTENANCE:
- "12. Clean Database" - Fresh start if needed
- "7. Open Project Folder" - Access all files

📖 HELP:
- "9. Quick Start Guide" - Detailed instructions

⏰ SCHEDULE:
- 9:00 AM: Daily Authentication
- 9:15 AM: Start Data Service (Market opens)
- 3:30 PM: Market closes (Service continues for EOD processing)

🎯 GOAL:
Collect upper/lower circuit limit values for all index option strikes
before market opens and throughout the trading day.

For support, check the project documentation files.
"@

$readmePath = Join-Path $folderPath "README.txt"
$readmeContent | Out-File -FilePath $readmePath -Encoding UTF8
Write-Host "   ✅ Created: README.txt" -ForegroundColor Green

Write-Host ""
Write-Host "🎉 SUCCESS! Desktop shortcuts created successfully!" -ForegroundColor Green
Write-Host "📁 Location: $folderPath" -ForegroundColor Cyan
Write-Host ""
Write-Host "🚀 NEXT STEPS:" -ForegroundColor Yellow
Write-Host "1. Go to your Desktop" -ForegroundColor White
Write-Host "2. Open the 'Indian Option Analysis Tool' folder" -ForegroundColor White
Write-Host "3. Read the README.txt file" -ForegroundColor White
Write-Host "4. Run '1. Daily Authentication' to get started" -ForegroundColor White
Write-Host ""
Write-Host "Press any key to exit..." -ForegroundColor Gray
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown") 