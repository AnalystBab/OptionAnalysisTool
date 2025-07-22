@echo off
echo.
echo 📊 INDIAN OPTION ANALYSIS - STORE 26-JUNE-2025 DATA
echo ===============================================
echo.

REM Change to the correct directory
cd /d "%~dp0"

echo 🔄 Starting data storage process...
echo 📅 Target Date: 26-06-2025
echo.

REM Run the console app with specific date parameter
dotnet run --project OptionAnalysisTool.Console -- --store-historical-data --date 2025-06-26

echo.
echo ✅ Data storage process completed
echo.
pause 