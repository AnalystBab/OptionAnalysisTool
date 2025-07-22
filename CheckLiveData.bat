@echo off
echo 🔥 === LIVE CIRCUIT LIMIT DATA VERIFICATION ===
echo 📊 Database: PalindromeResults
echo ⏰ Current Time: %time%
echo.

echo ✅ Authentication Status:
sqlcmd -S "." -E -d PalindromeResults -Q "SELECT TOP 1 UserType, ExpiryTime, CASE WHEN ExpiryTime > GETDATE() THEN 'VALID' ELSE 'EXPIRED' END AS Status FROM AuthenticationTokens WHERE IsActive = 1 ORDER BY CreatedAt DESC" -h -1

echo.
echo 🎯 Circuit Limit Tracking Data:
sqlcmd -S "." -E -d PalindromeResults -Q "SELECT COUNT(*) as TotalRecords, COUNT(CASE WHEN CAST(DetectedAt AS DATE) = CAST(GETDATE() AS DATE) THEN 1 END) as TodayRecords FROM CircuitLimitTrackers" -h -1

echo.
echo 📈 Recent Circuit Limit Changes:
sqlcmd -S "." -E -d PalindromeResults -Q "SELECT TOP 5 Symbol, StrikePrice, NewLowerLimit, NewUpperLimit, DetectedAt FROM CircuitLimitTrackers ORDER BY DetectedAt DESC" -h -1

echo.
echo 📊 Intraday Snapshots:
sqlcmd -S "." -E -d PalindromeResults -Q "SELECT COUNT(*) as TotalSnapshots, COUNT(CASE WHEN CAST(CapturedAt AS DATE) = CAST(GETDATE() AS DATE) THEN 1 END) as TodaySnapshots FROM IntradayOptionSnapshots" -h -1

echo.
echo 🕐 Latest Activity:
sqlcmd -S "." -E -d PalindromeResults -Q "SELECT MAX(DetectedAt) as LastCircuitUpdate, (SELECT MAX(CapturedAt) FROM IntradayOptionSnapshots) as LastSnapshot" -h -1

echo.
echo 🎉 === VERIFICATION COMPLETE ===
pause 