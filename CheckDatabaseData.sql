-- CHECK CIRCUIT LIMIT DATA IN DATABASE
-- Execute this script against the PalindromeResults database

USE PalindromeResults;
GO

-- Check if circuit limit tables exist
SELECT 
    TABLE_NAME, 
    TABLE_TYPE
FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_NAME LIKE '%Circuit%' 
   OR TABLE_NAME LIKE '%Intraday%' 
   OR TABLE_NAME LIKE '%Historical%'
ORDER BY TABLE_NAME;

-- Check circuit limit trackers count
SELECT 'CircuitLimitTrackers' as TableName, COUNT(*) as RecordCount
FROM CircuitLimitTrackers
UNION ALL
-- Check intraday snapshots count
SELECT 'IntradayOptionSnapshots' as TableName, COUNT(*) as RecordCount
FROM IntradayOptionSnapshots
UNION ALL
-- Check historical option data count
SELECT 'HistoricalOptionData' as TableName, COUNT(*) as RecordCount
FROM HistoricalOptionData
UNION ALL
-- Check circuit limit changes count
SELECT 'CircuitLimitChanges' as TableName, COUNT(*) as RecordCount
FROM CircuitLimitChanges;

-- Check latest circuit limit trackers (if any exist)
IF EXISTS (SELECT 1 FROM CircuitLimitTrackers)
BEGIN
    SELECT 'LATEST CIRCUIT LIMIT TRACKERS' as Info;
    SELECT TOP 10
        Symbol,
        StrikePrice,
        OptionType,
        PreviousLowerLimit,
        NewLowerLimit,
        PreviousUpperLimit,
        NewUpperLimit,
        SeverityLevel,
        DetectedAt,
        ChangeReason
    FROM CircuitLimitTrackers
    ORDER BY DetectedAt DESC;
END
ELSE
BEGIN
    SELECT 'NO CIRCUIT LIMIT TRACKER RECORDS FOUND' as Info;
END

-- Check latest intraday snapshots (if any exist)
IF EXISTS (SELECT 1 FROM IntradayOptionSnapshots)
BEGIN
    SELECT 'LATEST INTRADAY SNAPSHOTS' as Info;
    SELECT TOP 10
        Symbol,
        StrikePrice,
        OptionType,
        LastPrice,
        LowerCircuitLimit,
        UpperCircuitLimit,
        CircuitLimitStatus,
        Timestamp,
        Volume,
        OpenInterest
    FROM IntradayOptionSnapshots
    ORDER BY Timestamp DESC;
END
ELSE
BEGIN
    SELECT 'NO INTRADAY SNAPSHOT RECORDS FOUND' as Info;
END

-- Check latest historical data (if any exist)
IF EXISTS (SELECT 1 FROM HistoricalOptionData)
BEGIN
    SELECT 'LATEST HISTORICAL DATA' as Info;
    SELECT TOP 10
        Symbol,
        StrikePrice,
        OptionType,
        TradingDate,
        Close,
        LowerCircuitLimit,
        UpperCircuitLimit,
        CircuitLimitChanged,
        Volume,
        OpenInterest
    FROM HistoricalOptionData
    ORDER BY TradingDate DESC;
END
ELSE
BEGIN
    SELECT 'NO HISTORICAL DATA RECORDS FOUND' as Info;
END

-- Check latest circuit limit changes (if any exist)
IF EXISTS (SELECT 1 FROM CircuitLimitChanges)
BEGIN
    SELECT 'LATEST CIRCUIT LIMIT CHANGES' as Info;
    SELECT TOP 10
        Symbol,
        ISNULL(CAST(StrikePrice AS VARCHAR(10)), 'N/A') as StrikePrice,
        OldLowerCircuitLimit,
        NewLowerCircuitLimit,
        OldUpperCircuitLimit,
        NewUpperCircuitLimit,
        Timestamp,
        ChangeReason
    FROM CircuitLimitChanges
    ORDER BY Timestamp DESC;
END
ELSE
BEGIN
    SELECT 'NO CIRCUIT LIMIT CHANGE RECORDS FOUND' as Info;
END

-- Summary
SELECT 
    'SUMMARY REPORT' as Info,
    (SELECT COUNT(*) FROM CircuitLimitTrackers) as CircuitTrackers,
    (SELECT COUNT(*) FROM IntradayOptionSnapshots) as IntradaySnapshots,
    (SELECT COUNT(*) FROM HistoricalOptionData) as HistoricalData,
    (SELECT COUNT(*) FROM CircuitLimitChanges) as CircuitChanges,
    (SELECT COUNT(*) FROM CircuitLimitTrackers) + 
    (SELECT COUNT(*) FROM IntradayOptionSnapshots) + 
    (SELECT COUNT(*) FROM HistoricalOptionData) + 
    (SELECT COUNT(*) FROM CircuitLimitChanges) as TotalRecords;

-- Check if tables exist but are empty
IF (SELECT COUNT(*) FROM CircuitLimitTrackers) = 0 
   AND (SELECT COUNT(*) FROM IntradayOptionSnapshots) = 0 
   AND (SELECT COUNT(*) FROM HistoricalOptionData) = 0 
   AND (SELECT COUNT(*) FROM CircuitLimitChanges) = 0
BEGIN
    SELECT 'ISSUE IDENTIFIED: ALL CIRCUIT LIMIT TABLES ARE EMPTY' as Issue,
           'Your WPF grid is empty because no circuit limit data has been captured yet.' as Explanation,
           'SOLUTIONS: 1) Click "Test NIFTY Circuit Limits" button in WPF app, 2) Run data collection during market hours, 3) Check if services are working properly' as Solutions;
END
ELSE
BEGIN
    SELECT 'DATA EXISTS: Circuit limit data found in database' as Status,
           'If WPF grid is still empty, there may be a data binding or display issue.' as Note;
END 