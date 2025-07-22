@echo off
echo.
echo 🔥 STORING OPTION SNAPSHOTS FOR 26-JUNE-2025
echo ==========================================
echo.

REM Change to the correct directory
cd /d "%~dp0"

REM Build and run the script
echo 🚀 Building and running snapshot storage script...
echo.
dotnet run --project Store26JuneSnapshots.csproj

echo.
echo 📊 Verifying stored data...
sqlcmd -S "(LocalDB)\MSSQLLocalDB" -E -d OptionAnalysisToolDb -Q "SELECT COUNT(*) as TotalSnapshots, COUNT(DISTINCT Symbol) as UniqueStrikes FROM IntradayOptionSnapshots WHERE CAST(Timestamp AS DATE) = '2025-06-26'"

echo.
echo ✅ Process complete!
echo.
pause