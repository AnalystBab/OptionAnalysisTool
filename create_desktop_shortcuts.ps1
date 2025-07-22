# Create Desktop Shortcuts for Indian Option Analysis Tool
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
        [string]$WorkingDirectory,
        [string]$Description = ""
    )
    
    $shortcutPath = Join-Path $folderPath "$ShortcutName.lnk"
    $shell = New-Object -ComObject WScript.Shell
    $shortcut = $shell.CreateShortcut($shortcutPath)
    $shortcut.TargetPath = $TargetPath
    $shortcut.Arguments = $Arguments
    $shortcut.WorkingDirectory = $WorkingDirectory
    $shortcut.Description = $Description
    $shortcut.Save()
    Write-Host "   ✅ Created: $ShortcutName" -ForegroundColor Green
}

Write-Host ""
Write-Host "🔐 Creating Authentication Shortcuts..." -ForegroundColor Yellow

# 1. Daily Authentication
Create-Shortcut -ShortcutName "1. Daily Authentication" -TargetPath "cmd.exe" -Arguments "/c DailyAuth.bat" -WorkingDirectory $currentDir -Description "Daily Kite Connect authentication"

Write-Host ""
Write-Host "📊 Creating Data Service Shortcuts..." -ForegroundColor Yellow

# 2. Start Data Service
Create-Shortcut -ShortcutName "2. Start Data Service" -TargetPath "cmd.exe" -Arguments "/c StartDataService.bat" -WorkingDirectory $currentDir -Description "Start the data collection service"

# 3. Data Service Console
Create-Shortcut -ShortcutName "3. Data Service Console" -TargetPath "cmd.exe" -Arguments "/c dotnet run --project OptionAnalysisTool.Console" -WorkingDirectory $currentDir -Description "Run data service in console"

Write-Host ""
Write-Host "🛠️ Creating Utility Shortcuts..." -ForegroundColor Yellow

# 4. Database Query Tool
Create-Shortcut -ShortcutName "4. Database Query Tool" -TargetPath "cmd.exe" -Arguments "/c dotnet run --project DatabaseQueryScript.csproj" -WorkingDirectory $currentDir -Description "Query database data"

# 5. Project Folder
Create-Shortcut -ShortcutName "5. Open Project Folder" -TargetPath "explorer.exe" -Arguments $currentDir -WorkingDirectory $currentDir -Description "Open project directory"

# 6. Check Service Status
Create-Shortcut -ShortcutName "6. Check Service Status" -TargetPath "cmd.exe" -Arguments "/c CheckService.bat" -WorkingDirectory $currentDir -Description "Check service status"

Write-Host ""
Write-Host "📝 Creating README file..." -ForegroundColor Yellow

# Create README
$readmeContent = @"
🚀 INDIAN OPTION ANALYSIS TOOL - DESKTOP SHORTCUTS
==================================================

📅 DAILY ROUTINE:
1. Run "1. Daily Authentication" every morning at 9:00 AM
2. Run "2. Start Data Service" to begin data collection
3. Use "6. Check Service Status" to monitor

🔐 AUTHENTICATION:
- "1. Daily Authentication" - Main authentication process

📊 DATA SERVICES:
- "2. Start Data Service" - Main service with logging
- "3. Data Service Console" - Direct console access

🛠️ UTILITIES:
- "4. Database Query Tool" - Check stored data
- "5. Open Project Folder" - Access all files
- "6. Check Service Status" - Service health check

⏰ SCHEDULE:
- 9:00 AM: Daily Authentication
- 9:15 AM: Start Data Service (Market opens)
- 3:30 PM: Market closes

🎯 GOAL:
Collect circuit limit values for all index option strikes.
"@

$readmePath = Join-Path $folderPath "README.txt"
$readmeContent | Out-File -FilePath $readmePath -Encoding UTF8
Write-Host "   ✅ Created: README.txt" -ForegroundColor Green

Write-Host ""
Write-Host "🎉 SUCCESS! Desktop shortcuts created!" -ForegroundColor Green
Write-Host "📁 Location: $folderPath" -ForegroundColor Cyan
Write-Host ""
Write-Host "Next: Go to Desktop > Indian Option Analysis Tool folder" -ForegroundColor Yellow 