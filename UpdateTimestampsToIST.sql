-- 🔥 UPDATE TIMESTAMPS TO IST FORMAT
-- Converts existing UTC timestamps to IST format (same as Kite API)

-- Update IntradayOptionSnapshots table
UPDATE IntradayOptionSnapshots 
SET 
    Timestamp = DATEADD(HOUR, 5, DATEADD(MINUTE, 30, Timestamp)),
    CaptureTime = DATEADD(HOUR, 5, DATEADD(MINUTE, 30, CaptureTime)),
    LastUpdated = DATEADD(HOUR, 5, DATEADD(MINUTE, 30, LastUpdated))
WHERE Timestamp < '2025-01-01'; -- Only update historical data

-- Update Quotes table
UPDATE Quotes 
SET 
    TimeStamp = DATEADD(HOUR, 5, DATEADD(MINUTE, 30, TimeStamp)),
    CaptureTime = DATEADD(HOUR, 5, DATEADD(MINUTE, 30, CaptureTime)),
    LastUpdated = DATEADD(HOUR, 5, DATEADD(MINUTE, 30, LastUpdated))
WHERE TimeStamp < '2025-01-01'; -- Only update historical data

-- Update CircuitLimitTrackers table
UPDATE CircuitLimitTrackers 
SET 
    DetectedAt = DATEADD(HOUR, 5, DATEADD(MINUTE, 30, DetectedAt)),
    CreatedAt = DATEADD(HOUR, 5, DATEADD(MINUTE, 30, CreatedAt)),
    UpdatedAt = DATEADD(HOUR, 5, DATEADD(MINUTE, 30, UpdatedAt))
WHERE DetectedAt < '2025-01-01'; -- Only update historical data

-- Update SpotData table
UPDATE SpotData 
SET 
    Timestamp = DATEADD(HOUR, 5, DATEADD(MINUTE, 30, Timestamp)),
    LastUpdated = DATEADD(HOUR, 5, DATEADD(MINUTE, 30, LastUpdated)),
    CapturedAt = DATEADD(HOUR, 5, DATEADD(MINUTE, 30, CapturedAt))
WHERE Timestamp < '2025-01-01'; -- Only update historical data

-- Update HistoricalOptionData table
UPDATE HistoricalOptionData 
SET 
    CapturedAt = DATEADD(HOUR, 5, DATEADD(MINUTE, 30, CapturedAt)),
    LastUpdated = DATEADD(HOUR, 5, DATEADD(MINUTE, 30, LastUpdated))
WHERE CapturedAt < '2025-01-01'; -- Only update historical data

-- Show summary of updated records
SELECT 
    'IntradayOptionSnapshots' AS TableName,
    COUNT(*) AS UpdatedRecords
FROM IntradayOptionSnapshots 
WHERE Timestamp >= '2025-01-01'

UNION ALL

SELECT 
    'Quotes' AS TableName,
    COUNT(*) AS UpdatedRecords
FROM Quotes 
WHERE TimeStamp >= '2025-01-01'

UNION ALL

SELECT 
    'CircuitLimitTrackers' AS TableName,
    COUNT(*) AS UpdatedRecords
FROM CircuitLimitTrackers 
WHERE DetectedAt >= '2025-01-01'

UNION ALL

SELECT 
    'SpotData' AS TableName,
    COUNT(*) AS UpdatedRecords
FROM SpotData 
WHERE Timestamp >= '2025-01-01'

UNION ALL

SELECT 
    'HistoricalOptionData' AS TableName,
    COUNT(*) AS UpdatedRecords
FROM HistoricalOptionData 
WHERE CapturedAt >= '2025-01-01';

-- Verify the update with sample data
SELECT TOP 5
    'IntradayOptionSnapshots' AS TableName,
    Id,
    TradingSymbol,
    Timestamp,
    CaptureTime,
    LastUpdated
FROM IntradayOptionSnapshots 
WHERE TradingSymbol = 'NIFTY2572424950CE'
ORDER BY Timestamp DESC; 