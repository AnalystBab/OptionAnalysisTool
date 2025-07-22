@echo off
echo.
echo 📊 INDIAN OPTION ANALYSIS - DATA COLLECTION SERVICE
echo ==================================================
echo 🔄 This service continuously collects market data
echo 💾 Stores circuit limits, prices, and option data
echo.
echo ⚠️  REQUIREMENT: Authentication must be completed first
echo 💡 Run "DailyAuth.bat" if you haven't authenticated today
echo.

REM Change to the correct directory
cd /d "%~dp0"

echo 🚀 Starting data collection service...
echo 📈 Will collect data for all index option strikes
echo 🕘 Service runs continuously during market hours
echo.

REM Run the data service
dotnet run --project OptionAnalysisTool.Console

echo.
echo 📊 Data service has stopped.
echo.
pause 