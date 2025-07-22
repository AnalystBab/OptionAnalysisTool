# Indian Option Analysis Tool - Desktop Shortcuts Creator
Write-Host "Creating Desktop Shortcuts..." -ForegroundColor Green

$currentDir = Get-Location
$desktopPath = [Environment]::GetFolderPath("Desktop")
$folderName = "Indian Option Analysis Tool"
$folderPath = Join-Path $desktopPath $folderName

# Create folder
if (!(Test-Path $folderPath)) {
    New-Item -ItemType Directory -Path $folderPath -Force
    Write-Host "Folder created: $folderPath" -ForegroundColor Yellow
}

# Function to create shortcuts
function Create-Shortcut {
    param($Name, $Target, $Args, $WorkDir, $Desc, $Icon)
    $shortcutPath = Join-Path $folderPath "$Name.lnk"
    $shell = New-Object -ComObject WScript.Shell
    $shortcut = $shell.CreateShortcut($shortcutPath)
    $shortcut.TargetPath = $Target
    $shortcut.Arguments = $Args
    $shortcut.WorkingDirectory = $WorkDir
    $shortcut.Description = $Desc
    if ($Icon) { $shortcut.IconLocation = $Icon }
    $shortcut.Save()
    Write-Host "   Created: $Name" -ForegroundColor Green
}

Write-Host "Creating shortcuts..." -ForegroundColor Yellow

# 1. Daily Authentication
Create-Shortcut -Name "1. Daily Authentication" `
    -Target "cmd.exe" `
    -Args "/c `"$currentDir\DailyAuth.bat`"" `
    -WorkDir $currentDir `
    -Desc "Daily Kite Connect authentication - Run every morning at 9 AM" `
    -Icon "C:\Windows\System32\imageres.dll,77"

# 2. Data Service
Create-Shortcut -Name "2. Start Data Service" `
    -Target "cmd.exe" `
    -Args "/c `"$currentDir\StartDataService.bat`"" `
    -WorkDir $currentDir `
    -Desc "Start the option data collection service" `
    -Icon "C:\Windows\System32\imageres.dll,165"

# 3. Console Access
Create-Shortcut -Name "3. Data Service Console" `
    -Target "dotnet.exe" `
    -Args "run --project OptionAnalysisTool.Console" `
    -WorkDir $currentDir `
    -Desc "Direct console access to data collection service" `
    -Icon "C:\Windows\System32\imageres.dll,109"

# 4. Database Query
Create-Shortcut -Name "4. Database Query Tool" `
    -Target "dotnet.exe" `
    -Args "run --project DatabaseQueryScript.csproj" `
    -WorkDir $currentDir `
    -Desc "Query stored option data and circuit limits" `
    -Icon "C:\Windows\System32\imageres.dll,174"

# 5. Project Folder
Create-Shortcut -Name "5. Open Project Folder" `
    -Target "explorer.exe" `
    -Args "`"$currentDir`"" `
    -WorkDir $currentDir `
    -Desc "Open the main project directory" `
    -Icon "C:\Windows\System32\imageres.dll,3"

# 6. SQL Query
Create-Shortcut -Name "6. Circuit Limit Query" `
    -Target "notepad.exe" `
    -Args "`"$currentDir\GetYesterdayCircuitLimits.sql`"" `
    -WorkDir $currentDir `
    -Desc "SQL query to get yesterday's circuit limit data" `
    -Icon "C:\Windows\System32\imageres.dll,176"

Write-Host ""
Write-Host "SUCCESS! Desktop shortcuts created in:" -ForegroundColor Green
Write-Host "$folderPath" -ForegroundColor Cyan
Write-Host ""
Write-Host "SHORTCUTS:" -ForegroundColor Yellow
Write-Host "   1. Daily Authentication (Key icon)"
Write-Host "   2. Start Data Service (Chart icon)"  
Write-Host "   3. Data Service Console (Terminal icon)"
Write-Host "   4. Database Query Tool (Database icon)"
Write-Host "   5. Open Project Folder (Folder icon)"
Write-Host "   6. Circuit Limit Query (Report icon)"
Write-Host ""
Write-Host "READY TO USE! Start with Daily Authentication every morning at 9 AM" -ForegroundColor Green 