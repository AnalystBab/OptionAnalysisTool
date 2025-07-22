@echo off
echo 🚀 Starting Indian Option Analysis Desktop Widget...
echo 📊 Real-time system status will appear on right side of screen
echo.

REM Start the PowerShell widget
powershell.exe -ExecutionPolicy Bypass -File ".\SystemStatusWidget.ps1"

echo.
echo 🎯 Widget started! Check the right side of your desktop.
pause 