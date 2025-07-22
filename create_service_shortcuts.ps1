# Create desktop shortcuts for service management
$WshShell = New-Object -comObject WScript.Shell

# Install Service Shortcut (Batch)
$Shortcut = $WshShell.CreateShortcut("$env:USERPROFILE\Desktop\Install Data Service.lnk")
$Shortcut.TargetPath = "C:\Users\babu\Documents\Medha\InstallService.bat"
$Shortcut.WorkingDirectory = "C:\Users\babu\Documents\Medha"
$Shortcut.Description = "Install Option Market Monitor Windows Service"
$Shortcut.Save()

# Install Service Shortcut (PowerShell)
$Shortcut3 = $WshShell.CreateShortcut("$env:USERPROFILE\Desktop\Install Data Service (PS).lnk")
$Shortcut3.TargetPath = "powershell.exe"
$Shortcut3.Arguments = "-ExecutionPolicy Bypass -File `"C:\Users\babu\Documents\Medha\InstallService.ps1`""
$Shortcut3.WorkingDirectory = "C:\Users\babu\Documents\Medha"
$Shortcut3.Description = "Install Option Market Monitor Service (PowerShell)"
$Shortcut3.Save()

# Check Service Shortcut
$Shortcut2 = $WshShell.CreateShortcut("$env:USERPROFILE\Desktop\Check Data Service.lnk")
$Shortcut2.TargetPath = "C:\Users\babu\Documents\Medha\CheckService.bat"
$Shortcut2.WorkingDirectory = "C:\Users\babu\Documents\Medha"
$Shortcut2.Description = "Check Option Market Monitor Service Status"
$Shortcut2.Save()

# Create shortcut for service status check
$Shortcut = $WshShell.CreateShortcut("$env:USERPROFILE\Desktop\Check Service Status.lnk")
$Shortcut.TargetPath = "powershell.exe"
$Shortcut.Arguments = "-NoProfile -ExecutionPolicy Bypass -File `"$PSScriptRoot\CheckServiceStatus.ps1`""
$Shortcut.IconLocation = "shell32.dll,138"
$Shortcut.Description = "Check Option Market Monitor Service Status"
$Shortcut.Save()

Write-Host "✅ Desktop shortcuts created:"
Write-Host "  - Install Data Service.lnk (Batch version)"
Write-Host "  - Install Data Service (PS).lnk (PowerShell version)"
Write-Host "  - Check Data Service.lnk"
Write-Host "✅ Created service status check shortcut" -ForegroundColor Green 