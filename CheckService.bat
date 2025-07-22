@echo off
echo 🔍 === CHECKING OPTION MARKET MONITOR SERVICE ===
echo.

:: Check service status
echo 📊 Service Status:
sc.exe query OptionMarketMonitor

if %errorLevel% == 0 (
    echo.
    echo ✅ Service is installed!
    echo.
    
    :: Check if service is running
    sc.exe query OptionMarketMonitor | findstr "RUNNING" >nul
    if %errorLevel% == 0 (
        echo 🟢 Status: SERVICE IS RUNNING ✅
        echo.
        echo 📈 Data collection is active!
        echo 🕘 Market Hours: 9:15 AM - 3:30 PM
        echo 📊 Circuit limit monitoring enabled
    ) else (
        sc.exe query OptionMarketMonitor | findstr "STOPPED" >nul
        if %errorLevel% == 0 (
            echo 🔴 Status: SERVICE IS STOPPED ❌
            echo.
            echo 🚀 To start the service, run:
            echo    sc.exe start OptionMarketMonitor
            echo.
            echo 💡 Or right-click and "Run as Administrator":
            echo    StartService.bat
        ) else (
            echo 🟡 Status: SERVICE IN TRANSITION
            echo Check again in a few seconds...
        )
    )
) else (
    echo ❌ Service is NOT installed!
    echo.
    echo 📋 To install the service:
    echo 1. Right-click InstallService.bat
    echo 2. Select "Run as Administrator"
)

echo.
echo 📅 Daily Requirements:
echo 1. Login using "Daily Kite Login" desktop shortcut (8:30 AM)
echo 2. Service will collect data during market hours
echo 3. EOD processing happens automatically after 3:30 PM
echo.

:: Show recent log entries if available
if exist "C:\Users\%USERNAME%\AppData\Local\OptionAnalysisTool\logs\*.log" (
    echo 📋 Recent Log Activity:
    forfiles /p "C:\Users\%USERNAME%\AppData\Local\OptionAnalysisTool\logs" /m "*.log" /c "cmd /c echo @path - @fdate @ftime" 2>nul | tail -5
    echo.
)

pause 