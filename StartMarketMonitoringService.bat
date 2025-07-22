@echo off
echo 🔥 === STARTING INDIAN OPTION MARKET MONITORING SERVICE ===
echo.

REM Check if running as administrator
net session >nul 2>&1
if %errorLevel% == 0 (
    echo ✅ Running as Administrator
) else (
    echo ❌ ERROR: This script requires Administrator privileges
    echo 🔧 SOLUTION: Right-click and select "Run as Administrator"
    echo.
    pause
    exit /b 1
)

echo 📊 STEP 1: Checking service status...
sc query "OptionMarketMonitor" >nul 2>&1
if %errorLevel% == 0 (
    echo ✅ Service found: Indian Option Market Monitor
) else (
    echo ❌ ERROR: Service not found. Please install the service first.
    echo 🔧 SOLUTION: Run InstallWindowsService.ps1 first
    echo.
    pause
    exit /b 1
)

echo.
echo 📊 STEP 2: Starting the service...
sc start "OptionMarketMonitor"
if %errorLevel% == 0 (
    echo ✅ Service started successfully
) else (
    echo ⚠️ Service may already be running or failed to start
    echo 📋 Checking current status...
    sc query "OptionMarketMonitor"
)

echo.
echo 📊 STEP 3: Setting service to start automatically...
sc config "OptionMarketMonitor" start= auto
if %errorLevel% == 0 (
    echo ✅ Service configured for automatic startup
) else (
    echo ⚠️ Failed to configure automatic startup
)

echo.
echo 📊 STEP 4: Verifying service status...
sc query "OptionMarketMonitor"

echo.
echo 🎉 === SERVICE STARTUP COMPLETED ===
echo 📊 The service will now:
echo    ✅ Start automatically when Windows boots
echo    ✅ Begin monitoring at 9:15 AM on trading days
echo    ✅ Collect circuit limit data every 30 seconds
echo    ✅ Send notifications when limits change
echo    ✅ Store all data in database for analysis
echo.
echo 💡 TIP: The service runs 24/7 and activates during market hours
echo 🔔 You'll receive notifications when circuit limits change
echo.
pause 