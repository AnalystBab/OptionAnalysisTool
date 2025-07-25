-- SQL Query for Yesterday's Circuit Limits

-- 🎯 GET YESTERDAY'S CIRCUIT LIMITS FOR SPECIFIC INDEX AND STRIKE
-- Updated query for IntradayOptionSnapshots table (your new data structure)
-- Gets LC/UC data for a specific index and strike price

-- ========================================
-- 🔥 QUERY 1: SPECIFIC STRIKE LC/UC DATA
-- ========================================
DECLARE @YesterdayDate DATE = CAST(DATEADD(day, -1, GETDATE()) AS DATE);
DECLARE @IndexName VARCHAR(50) = 'NIFTY';        -- Change this: NIFTY, BANKNIFTY, SENSEX, BANKEX, etc.
DECLARE @StrikePrice DECIMAL(10,2) = 25000;      -- Change this: specific strike price
DECLARE @OptionType VARCHAR(5) = 'CE';           -- Change this: CE or PE

-- Get yesterday's circuit limits for specific strike
SELECT 
    UnderlyingSymbol as IndexName,
    Symbol as TradingSymbol,
    StrikePrice,
    OptionType,
    ExpiryDate,
    Timestamp as DataTime,
    LowerCircuitLimit as LC,
    UpperCircuitLimit as UC,
    LastPrice as LTP,
    Volume,
    OpenInterest,
    -- Calculate circuit limit range
    (UpperCircuitLimit - LowerCircuitLimit) as CircuitRange,
    -- Calculate percentage from LTP
    CASE 
        WHEN LastPrice > 0 THEN ROUND(((LowerCircuitLimit / LastPrice) - 1) * 100, 2)
        ELSE NULL 
    END as LowerCircuitPct,
    CASE 
        WHEN LastPrice > 0 THEN ROUND(((UpperCircuitLimit / LastPrice) - 1) * 100, 2)
        ELSE NULL 
    END as UpperCircuitPct,
    CircuitLimitStatus,
    TradingStatus
FROM IntradayOptionSnapshots
WHERE CAST(Timestamp AS DATE) = @YesterdayDate
  AND UnderlyingSymbol = @IndexName
  AND StrikePrice = @StrikePrice
  AND OptionType = @OptionType
  AND LowerCircuitLimit > 0 
  AND UpperCircuitLimit > 0
ORDER BY Timestamp DESC;

-- ========================================
-- 🔥 QUERY 2: LATEST LC/UC FOR SPECIFIC STRIKE
-- ========================================
-- Get the most recent circuit limits for a specific strike
SELECT TOP 1
    UnderlyingSymbol as IndexName,
    Symbol as TradingSymbol,
    StrikePrice,
    OptionType,
    ExpiryDate,
    Timestamp as LatestDataTime,
    LowerCircuitLimit as LC,
    UpperCircuitLimit as UC,
    LastPrice as LTP,
    Volume,
    OpenInterest,
    CircuitLimitStatus
FROM IntradayOptionSnapshots
WHERE CAST(Timestamp AS DATE) = @YesterdayDate
  AND UnderlyingSymbol = @IndexName
  AND StrikePrice = @StrikePrice
  AND OptionType = @OptionType
  AND LowerCircuitLimit > 0 
  AND UpperCircuitLimit > 0
ORDER BY Timestamp DESC;

-- ========================================
-- 🔥 QUERY 3: ALL STRIKES FOR AN INDEX
-- ========================================
-- Get yesterday's latest circuit limits for ALL strikes of a specific index
WITH LatestData AS (
    SELECT 
        *,
        ROW_NUMBER() OVER (
            PARTITION BY Symbol 
            ORDER BY Timestamp DESC
        ) as rn
    FROM IntradayOptionSnapshots
    WHERE CAST(Timestamp AS DATE) = @YesterdayDate
      AND UnderlyingSymbol = @IndexName
      AND LowerCircuitLimit > 0 
      AND UpperCircuitLimit > 0
)
SELECT 
    UnderlyingSymbol as IndexName,
    Symbol as TradingSymbol,
    StrikePrice,
    OptionType,
    ExpiryDate,
    Timestamp as LatestDataTime,
    LowerCircuitLimit as LC,
    UpperCircuitLimit as UC,
    LastPrice as LTP,
    Volume,
    OpenInterest,
    CircuitLimitStatus
FROM LatestData
WHERE rn = 1
ORDER BY StrikePrice, OptionType;

-- ========================================
-- 🔥 QUERY 4: NEAR-THE-MONEY STRIKES ONLY
-- ========================================
-- Get yesterday's circuit limits for strikes near current spot price
DECLARE @SpotPrice DECIMAL(10,2) = 25044;  -- Update with current spot price
DECLARE @StrikeRange DECIMAL(10,2) = 1000; -- Range around spot price

WITH LatestData AS (
    SELECT 
        *,
        ROW_NUMBER() OVER (
            PARTITION BY Symbol 
            ORDER BY Timestamp DESC
        ) as rn
    FROM IntradayOptionSnapshots
    WHERE CAST(Timestamp AS DATE) = @YesterdayDate
      AND UnderlyingSymbol = @IndexName
      AND StrikePrice BETWEEN (@SpotPrice - @StrikeRange) AND (@SpotPrice + @StrikeRange)
      AND LowerCircuitLimit > 0 
      AND UpperCircuitLimit > 0
)
SELECT 
    UnderlyingSymbol as IndexName,
    Symbol as TradingSymbol,
    StrikePrice,
    OptionType,
    ExpiryDate,
    Timestamp as LatestDataTime,
    LowerCircuitLimit as LC,
    UpperCircuitLimit as UC,
    LastPrice as LTP,
    Volume,
    OpenInterest,
    ABS(StrikePrice - @SpotPrice) as DistanceFromSpot
FROM LatestData
WHERE rn = 1
ORDER BY ABS(StrikePrice - @SpotPrice), OptionType;

-- ========================================
-- 📊 SUMMARY STATISTICS
-- ========================================
SELECT 
    'SUMMARY' as QueryType,
    @YesterdayDate as YesterdayDate,
    @IndexName as IndexName,
    COUNT(DISTINCT Symbol) as TotalUniqueStrikes,
    COUNT(*) as TotalDataPoints,
    MIN(Timestamp) as EarliestTime,
    MAX(Timestamp) as LatestTime,
    AVG(LowerCircuitLimit) as AvgLC,
    AVG(UpperCircuitLimit) as AvgUC,
    MIN(StrikePrice) as MinStrike,
    MAX(StrikePrice) as MaxStrike
FROM IntradayOptionSnapshots
WHERE CAST(Timestamp AS DATE) = @YesterdayDate
  AND UnderlyingSymbol = @IndexName
  AND LowerCircuitLimit > 0 
  AND UpperCircuitLimit > 0;
