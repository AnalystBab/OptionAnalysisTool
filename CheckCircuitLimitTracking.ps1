#!/usr/bin/env pwsh

# Circuit Limit Tracking Verification Script
Write-Host "🔥 CIRCUIT LIMIT TRACKING VERIFICATION" -ForegroundColor Red
Write-Host "=====================================" -ForegroundColor Red
Write-Host ""

$connectionString = "Server=(LocalDB)\MSSQLLocalDB;Database=OptionAnalysisToolDb;Trusted_Connection=True;TrustServerCertificate=True;"

try {
    # Load SQL Server assembly
    Add-Type -AssemblyName "System.Data"
    
    # Create connection
    $connection = New-Object System.Data.SqlClient.SqlConnection($connectionString)
    $connection.Open()
    Write-Host "✅ Connected to OptionAnalysisToolDb database" -ForegroundColor Green
    
    # Check Authentication Status
    Write-Host ""
    Write-Host "🔐 AUTHENTICATION STATUS:" -ForegroundColor Yellow
    Write-Host "========================" -ForegroundColor Yellow
    
    $authQuery = "SELECT TOP 1 UserType, ExpiryTime, CASE WHEN ExpiryTime > GETDATE() THEN 'VALID' ELSE 'EXPIRED' END AS Status FROM AuthenticationTokens WHERE IsActive = 1 ORDER BY CreatedAt DESC"
    
    $authCommand = New-Object System.Data.SqlClient.SqlCommand($authQuery, $connection)
    $authReader = $authCommand.ExecuteReader()
    
    if ($authReader.Read()) {
        $status = $authReader["Status"]
        $expiry = $authReader["ExpiryTime"]
        $userType = $authReader["UserType"]
        
        if ($status -eq "VALID") {
            Write-Host "   Status: $status" -ForegroundColor Green
        } else {
            Write-Host "   Status: $status" -ForegroundColor Red
        }
        Write-Host "   User Type: $userType"
        Write-Host "   Expires: $expiry"
        
        if ($status -eq "VALID") {
            Write-Host "   ✅ Authentication is valid - services can access Kite API" -ForegroundColor Green
        } else {
            Write-Host "   ❌ Authentication expired - need to re-authenticate" -ForegroundColor Red
        }
    } else {
        Write-Host "   ❌ No authentication tokens found" -ForegroundColor Red
    }
    $authReader.Close()
    
    # Check Circuit Limit Tracking Data
    Write-Host ""
    Write-Host "🎯 CIRCUIT LIMIT TRACKING DATA:" -ForegroundColor Yellow
    Write-Host "===============================" -ForegroundColor Yellow
    
    $trackerQuery = "SELECT COUNT(*) as TotalRecords, COUNT(CASE WHEN CAST(DetectedAt AS DATE) = CAST(GETDATE() AS DATE) THEN 1 END) as TodayRecords, MAX(DetectedAt) as LastUpdate FROM CircuitLimitTrackers"
    
    $trackerCommand = New-Object System.Data.SqlClient.SqlCommand($trackerQuery, $connection)
    $trackerReader = $trackerCommand.ExecuteReader()
    
    if ($trackerReader.Read()) {
        $total = $trackerReader["TotalRecords"]
        $today = $trackerReader["TodayRecords"]
        $lastUpdate = $trackerReader["LastUpdate"]
        
        Write-Host "   Total Circuit Limit Records: $total"
        Write-Host "   Today's Records: $today"
        Write-Host "   Last Update: $lastUpdate"
        
        if ([int]$today -gt 0) {
            Write-Host "   ✅ Circuit limit tracking is working today!" -ForegroundColor Green
        } elseif ([int]$total -gt 0) {
            Write-Host "   ⚠️ Has historical data but no data today (market may be closed)" -ForegroundColor Yellow
        } else {
            Write-Host "   ❌ No circuit limit data found - service may not be running" -ForegroundColor Red
        }
    }
    $trackerReader.Close()
    
    # Show recent circuit limit changes if any exist
    $countQuery = "SELECT COUNT(*) FROM CircuitLimitTrackers"
    $countCommand = New-Object System.Data.SqlClient.SqlCommand($countQuery, $connection)
    $totalRecords = $countCommand.ExecuteScalar()
    
    if ([int]$totalRecords -gt 0) {
        Write-Host ""
        Write-Host "   Recent Circuit Limit Changes:" -ForegroundColor Cyan
        Write-Host "   ================================" -ForegroundColor Cyan
        
        $recentQuery = "SELECT TOP 5 Symbol, UnderlyingSymbol, StrikePrice, OptionType, NewLowerLimit, NewUpperLimit, DetectedAt, SeverityLevel FROM CircuitLimitTrackers ORDER BY DetectedAt DESC"
        
        $recentCommand = New-Object System.Data.SqlClient.SqlCommand($recentQuery, $connection)
        $recentReader = $recentCommand.ExecuteReader()
        
        while ($recentReader.Read()) {
            $detectedAt = $recentReader["DetectedAt"]
            $symbol = $recentReader["Symbol"]
            $strike = $recentReader["StrikePrice"]
            $lower = $recentReader["NewLowerLimit"]
            $upper = $recentReader["NewUpperLimit"]
            $severity = $recentReader["SeverityLevel"]
            
            Write-Host "   $detectedAt | $symbol | Strike: $strike | Limits: $lower-$upper | Severity: $severity"
        }
        $recentReader.Close()
    }
    
    # Check Real-time Data Collection
    Write-Host ""
    Write-Host "📊 REAL-TIME DATA COLLECTION:" -ForegroundColor Yellow
    Write-Host "=============================" -ForegroundColor Yellow
    
    $realtimeQuery = "SELECT COUNT(*) as TotalSnapshots, COUNT(CASE WHEN CAST(CapturedAt AS DATE) = CAST(GETDATE() AS DATE) THEN 1 END) as TodaySnapshots, MAX(CapturedAt) as LastSnapshot FROM IntradayOptionSnapshots"
    
    $realtimeCommand = New-Object System.Data.SqlClient.SqlCommand($realtimeQuery, $connection)
    $realtimeReader = $realtimeCommand.ExecuteReader()
    
    if ($realtimeReader.Read()) {
        $totalSnaps = $realtimeReader["TotalSnapshots"]
        $todaySnaps = $realtimeReader["TodaySnapshots"]
        $lastSnap = $realtimeReader["LastSnapshot"]
        
        Write-Host "   Total Intraday Snapshots: $totalSnaps"
        Write-Host "   Today's Snapshots: $todaySnaps"
        Write-Host "   Last Snapshot: $lastSnap"
        
        if ([int]$todaySnaps -gt 0) {
            Write-Host "   ✅ Real-time data collection is working!" -ForegroundColor Green
        } else {
            Write-Host "   ❌ No real-time data today - service may not be running" -ForegroundColor Red
        }
    }
    $realtimeReader.Close()
    
    # Check if market is currently open
    Write-Host ""
    Write-Host "⏰ MARKET STATUS CHECK:" -ForegroundColor Yellow
    Write-Host "======================" -ForegroundColor Yellow
    
    $currentTime = Get-Date
    $marketOpen = Get-Date -Hour 9 -Minute 15 -Second 0
    $marketClose = Get-Date -Hour 15 -Minute 30 -Second 0
    
    $isWeekday = $currentTime.DayOfWeek -ge [System.DayOfWeek]::Monday -and $currentTime.DayOfWeek -le [System.DayOfWeek]::Friday
    $isMarketHours = $currentTime -ge $marketOpen -and $currentTime -le $marketClose
    
    Write-Host "   Current Time: $currentTime"
    Write-Host "   Market Hours: 9:15 AM - 3:30 PM (Mon-Fri)"
    
    if ($isWeekday -and $isMarketHours) {
        Write-Host "   🟢 Market is OPEN - Circuit limit tracking should be active" -ForegroundColor Green
    } elseif ($isWeekday) {
        Write-Host "   🟡 Market is CLOSED - Tracking will resume during market hours" -ForegroundColor Yellow
    } else {
        Write-Host "   🔴 Weekend - Market is closed" -ForegroundColor Red
    }
    
    # Final Summary
    Write-Host ""
    Write-Host "🎉 VERIFICATION COMPLETE!" -ForegroundColor Green
    Write-Host "=========================" -ForegroundColor Green
    
    if ([int]$today -gt 0) {
        Write-Host "✅ Circuit limit tracking is functioning properly" -ForegroundColor Green
    } elseif ([int]$total -gt 0) {
        Write-Host "⚠️ Historical data exists but no new data today" -ForegroundColor Yellow
        Write-Host "💡 If market is open, check if the monitoring service is running" -ForegroundColor Yellow
    } else {
        Write-Host "❌ No circuit limit data found" -ForegroundColor Red
        Write-Host "💡 Start the monitoring service during market hours" -ForegroundColor Yellow
    }
    
} catch {
    Write-Host "❌ Error: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host ""
    Write-Host "💡 Make sure:" -ForegroundColor Yellow
    Write-Host "   1. Database exists: (LocalDB)\MSSQLLocalDB" -ForegroundColor Yellow
    Write-Host "   2. Authentication was completed today" -ForegroundColor Yellow
    Write-Host "   3. Data service is running during market hours" -ForegroundColor Yellow
} finally {
    if ($connection -and $connection.State -eq "Open") {
        $connection.Close()
    }
}

Write-Host ""
Write-Host "Press any key to exit..." -ForegroundColor Cyan
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown") 