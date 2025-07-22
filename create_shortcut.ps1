$WshShell = New-Object -comObject WScript.Shell
$Shortcut = $WshShell.CreateShortcut("$env:USERPROFILE\Desktop\Daily Kite Login.lnk")
$Shortcut.TargetPath = "C:\Users\babu\Documents\Medha\DailyLogin.bat"
$Shortcut.WorkingDirectory = "C:\Users\babu\Documents\Medha"
$Shortcut.Description = "Daily Kite Connect Login for Market Data Service"
$Shortcut.Save()

Write-Host "✅ Desktop shortcut created: Daily Kite Login.lnk" 