@echo off
echo 🔥 === STARTING LIVE CIRCUIT LIMIT MONITORING ===
echo 📈 Market is OPEN - Starting real-time data collection
echo.

echo ⏰ Current time: %time%
echo 📊 Starting monitoring service...
echo.

REM Start the console application in the background
echo 🚀 Launching monitoring application...
start "Circuit Limit Monitor" /MIN cmd /c "cd OptionAnalysisTool.Console && dotnet run"

echo ✅ Monitoring started in background window
echo.

echo ⏳ Waiting 30 seconds for initial data collection...
timeout /t 30 /nobreak > nul

echo.
echo 📊 === CHECKING DATA COLLECTION ===
echo.

echo 🔍 Checking authentication tokens...
sqlcmd -S ".\SQLEXPRESS" -E -d OptionAnalysisToolDb -Q "SELECT COUNT(*) as ActiveTokens FROM AuthenticationTokens WHERE IsActive = 1" -h -1 2>nul
if errorlevel 1 (
    echo ⚠️ Trying LocalDB...
    sqlcmd -S "(LocalDB)\MSSQLLocalDB" -E -d OptionAnalysisToolDb -Q "SELECT COUNT(*) as ActiveTokens FROM AuthenticationTokens WHERE IsActive = 1" -h -1 2>nul
)

echo.
echo 🎯 Checking circuit limit data...
sqlcmd -S ".\SQLEXPRESS" -E -d OptionAnalysisToolDb -Q "SELECT COUNT(*) as TodayRecords FROM CircuitLimitTrackers WHERE CAST(DetectedAt AS DATE) = CAST(GETDATE() AS DATE)" -h -1 2>nul
if errorlevel 1 (
    echo ⚠️ Trying LocalDB...
    sqlcmd -S "(LocalDB)\MSSQLLocalDB" -E -d OptionAnalysisToolDb -Q "SELECT COUNT(*) as TodayRecords FROM CircuitLimitTrackers WHERE CAST(DetectedAt AS DATE) = CAST(GETDATE() AS DATE)" -h -1 2>nul
)

echo.
echo 📈 Checking intraday snapshots...
sqlcmd -S ".\SQLEXPRESS" -E -d OptionAnalysisToolDb -Q "SELECT COUNT(*) as TodaySnapshots FROM IntradayOptionSnapshots WHERE CAST(CapturedAt AS DATE) = CAST(GETDATE() AS DATE)" -h -1 2>nul
if errorlevel 1 (
    echo ⚠️ Trying LocalDB...
    sqlcmd -S "(LocalDB)\MSSQLLocalDB" -E -d OptionAnalysisToolDb -Q "SELECT COUNT(*) as TodaySnapshots FROM IntradayOptionSnapshots WHERE CAST(CapturedAt AS DATE) = CAST(GETDATE() AS DATE)" -h -1 2>nul
)

echo.
echo 🎉 === MONITORING STATUS COMPLETE ===
echo 💡 Check the background window for live monitoring logs
echo 📊 Data should be collecting every 30 seconds during market hours
echo.
pause 