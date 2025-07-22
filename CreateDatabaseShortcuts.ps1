# Indian Option Analysis Tool - Desktop Shortcuts Creator
# Creates organized shortcuts with custom icons in a desktop folder

Write-Host "🚀 Creating Desktop Shortcuts for Indian Option Analysis Tool" -ForegroundColor Green
Write-Host "================================================================" -ForegroundColor Green

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

# Function to create shortcuts with icons
function Create-Shortcut {
    param($Name, $Target, $Args, $WorkDir, $Desc, $IconPath)
    
    $shortcutPath = Join-Path $folderPath "$Name.lnk"
    $shell = New-Object -ComObject WScript.Shell
    $shortcut = $shell.CreateShortcut($shortcutPath)
    $shortcut.TargetPath = $Target
    $shortcut.Arguments = $Args
    $shortcut.WorkingDirectory = $WorkDir
    $shortcut.Description = $Desc
    
    # Set custom icon if provided
    if ($IconPath -and (Test-Path $IconPath)) {
        $shortcut.IconLocation = $IconPath
    }
    
    $shortcut.Save()
    Write-Host "   ✅ $Name" -ForegroundColor Green
}

Write-Host ""
Write-Host "🔧 Creating shortcuts..." -ForegroundColor Yellow

# 1. DAILY AUTHENTICATION - 🔐 Key Icon
Create-Shortcut -Name "1. 🔐 Daily Authentication" `
    -Target "cmd.exe" `
    -Args "/c `"$currentDir\DailyAuth.bat`"" `
    -WorkDir $currentDir `
    -Desc "Daily Kite Connect authentication - Run every morning at 9 AM" `
    -IconPath "C:\Windows\System32\imageres.dll,77"

# 2. DATA SERVICE - 📊 Chart Icon  
Create-Shortcut -Name "2. 📊 Start Data Service" `
    -Target "cmd.exe" `
    -Args "/c `"$currentDir\StartDataService.bat`"" `
    -WorkDir $currentDir `
    -Desc "Start the option data collection service" `
    -IconPath "C:\Windows\System32\imageres.dll,165"

# 3. DATA SERVICE CONSOLE - 💻 Terminal Icon
Create-Shortcut -Name "3. 💻 Data Service Console" `
    -Target "dotnet.exe" `
    -Args "run --project OptionAnalysisTool.Console" `
    -WorkDir $currentDir `
    -Desc "Direct console access to data collection service" `
    -IconPath "C:\Windows\System32\imageres.dll,109"

# 4. DATABASE QUERY - 🗃️ Database Icon
Create-Shortcut -Name "4. 🗃️ Database Query Tool" `
    -Target "dotnet.exe" `
    -Args "run --project DatabaseQueryScript.csproj" `
    -WorkDir $currentDir `
    -Desc "Query stored option data and circuit limits" `
    -IconPath "C:\Windows\System32\imageres.dll,174"

# 5. PROJECT FOLDER - 📂 Folder Icon
Create-Shortcut -Name "5. 📂 Open Project Folder" `
    -Target "explorer.exe" `
    -Args "`"$currentDir`"" `
    -WorkDir $currentDir `
    -Desc "Open the main project directory" `
    -IconPath "C:\Windows\System32\imageres.dll,3"

# 6. SQL QUERY - 📈 Report Icon
Create-Shortcut -Name "6. 📈 Circuit Limit Query" `
    -Target "notepad.exe" `
    -Args "`"$currentDir\GetYesterdayCircuitLimits.sql`"" `
    -WorkDir $currentDir `
    -Desc "SQL query to get yesterday's circuit limit data" `
    -IconPath "C:\Windows\System32\imageres.dll,176"

# Create a README text file in the folder
$readmeContent = @"
🔥 INDIAN OPTION ANALYSIS TOOL - DAILY WORKFLOW
===============================================

📋 MORNING ROUTINE (9:00 AM):
1. Run "1. 🔐 Daily Authentication"
   - Login with VDZ315 / 30045497
   - Copy request_token from browser
   - System stores access_token in database

2. Data collection starts automatically
   - OR manually run "2. 📊 Start Data Service"

📊 ANALYSIS WORKFLOW:
- Use "6. 📈 Circuit Limit Query" to analyze data
- Query yesterday's LC/UC values for all index strikes
- Perfect for developing option strategies

🔧 TROUBLESHOOTING:
- Use "4. 🗃️ Database Query Tool" to check stored data
- Use "3. 💻 Data Service Console" for detailed monitoring
- Check "5. 📂 Open Project Folder" for logs and files

⏰ FREQUENCY:
- Run authentication daily (token expires every 24 hours)
- Data collection runs continuously during market hours
- Circuit limit data available even when market closed

🎯 PURPOSE:
- Track circuit limit changes throughout the day
- Store comprehensive option market data
- Enable systematic strategy development
"@

$readmePath = Join-Path $folderPath "README - How to Use.txt"
$readmeContent | Out-File -FilePath $readmePath -Encoding UTF8
Write-Host "   ✅ README - How to Use.txt" -ForegroundColor Green

Write-Host ""
Write-Host "🎉 SUCCESS! Desktop shortcuts created successfully!" -ForegroundColor Green
Write-Host "📁 Location: $folderPath" -ForegroundColor Cyan
Write-Host ""
Write-Host "📋 SHORTCUTS CREATED:" -ForegroundColor Yellow
Write-Host "   🔐 Daily Authentication (Run every morning)" -ForegroundColor White
Write-Host "   📊 Data Service (Collect market data)" -ForegroundColor White  
Write-Host "   💻 Console Access (Direct monitoring)" -ForegroundColor White
Write-Host "   🗃️ Database Query (Check stored data)" -ForegroundColor White
Write-Host "   📂 Project Folder (Access all files)" -ForegroundColor White
Write-Host "   📈 Circuit Limit Query (Analysis tool)" -ForegroundColor White
Write-Host "   📄 README (Usage instructions)" -ForegroundColor White
Write-Host ""
Write-Host "⭐ READY TO USE! Start with Daily Authentication every morning at 9 AM" -ForegroundColor Green 