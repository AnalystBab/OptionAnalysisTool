@echo off
echo 📊 === EXPORT CIRCUIT LIMITS TO EXCEL ===
echo.

echo 🔍 Checking authentication status...
if not exist "appsettings.json" (
    echo ❌ ERROR: Configuration file not found
    echo 🔧 SOLUTION: Run from project directory
    echo.
    pause
    exit /b 1
)

echo ✅ Configuration found
echo.

echo 📊 Starting Excel export...
echo 💡 This will create Excel files for all indices with current circuit limit data
echo 📁 Files will be saved to: Desktop\CircuitLimitExcelReports
echo.

REM Run the console application to trigger Excel export
dotnet run --project OptionAnalysisTool.Console -- --export-excel

if %errorLevel% == 0 (
    echo.
    echo ✅ === EXCEL EXPORT COMPLETED SUCCESSFULLY ===
    echo.
    echo 📊 CREATED FILES:
    echo    📄 NIFTY_Summary.csv
    echo    📄 BANKNIFTY_Summary.csv  
    echo    📄 SENSEX_Summary.csv
    echo    📄 BANKEX_Summary.csv
    echo    📄 FINNIFTY_Summary.csv
    echo    📄 MIDCPNIFTY_Summary.csv
    echo.
    echo    📄 [INDEX]_YYYY-MM-DD.csv (One file per expiry date)
    echo.
    echo 💡 FILES INCLUDE:
    echo    ✅ All strikes for each expiry
    echo    ✅ Current and previous circuit limits
    echo    ✅ Change percentages and severity levels
    echo    ✅ Volume and open interest data
    echo    ✅ Timestamps for all changes
    echo.
    echo 🔔 AUTOMATIC UPDATES:
    echo    ✅ Files will be updated automatically when circuit limits change
    echo    ✅ Backup files created for version history
    echo    ✅ Index-wise organization for easy analysis
    echo.
    echo 📂 Opening export folder...
    start "" "%USERPROFILE%\Desktop\CircuitLimitExcelReports"
) else (
    echo.
    echo ❌ ERROR: Excel export failed
    echo 🔧 SOLUTION: Check authentication and try again
    echo 💡 TIP: Run DailyAuth.bat first if needed
)

echo.
pause 