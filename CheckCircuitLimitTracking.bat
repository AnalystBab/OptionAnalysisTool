@echo off
echo 🔥 === CIRCUIT LIMIT TRACKING VERIFICATION ===
echo.

echo 📊 Checking authentication status...
sqlcmd -S "(LocalDB)\MSSQLLocalDB" -E -d OptionAnalysisToolDb -Q "SELECT TOP 1 UserType, ExpiryTime, CASE WHEN ExpiryTime > GETDATE() THEN 'VALID' ELSE 'EXPIRED' END AS Status FROM AuthenticationTokens WHERE IsActive = 1 ORDER BY CreatedAt DESC" -h -1

echo.
echo 🎯 Checking circuit limit tracking data...
sqlcmd -S "(LocalDB)\MSSQLLocalDB" -E -d OptionAnalysisToolDb -Q "SELECT COUNT(*) as TotalRecords, COUNT(CASE WHEN CAST(DetectedAt AS DATE) = CAST(GETDATE() AS DATE) THEN 1 END) as TodayRecords, MAX(DetectedAt) as LastUpdate FROM CircuitLimitTrackers" -h -1

echo.
echo 📈 Recent circuit limit changes (last 5)...
sqlcmd -S "(LocalDB)\MSSQLLocalDB" -E -d OptionAnalysisToolDb -Q "SELECT TOP 5 Symbol, StrikePrice, NewLowerLimit, NewUpperLimit, DetectedAt FROM CircuitLimitTrackers ORDER BY DetectedAt DESC" -h -1

echo.
echo 📊 Checking real-time data collection...
sqlcmd -S "(LocalDB)\MSSQLLocalDB" -E -d OptionAnalysisToolDb -Q "SELECT COUNT(*) as TotalSnapshots, COUNT(CASE WHEN CAST(CapturedAt AS DATE) = CAST(GETDATE() AS DATE) THEN 1 END) as TodaySnapshots, MAX(CapturedAt) as LastSnapshot FROM IntradayOptionSnapshots" -h -1

echo.
echo 🎉 === VERIFICATION COMPLETE ===
echo If you see data above, circuit limit tracking is working!
echo.
pause 