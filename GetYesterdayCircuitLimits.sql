-- 🎯 GET YESTERDAY'S CIRCUIT LIMITS FOR ALL INDEX OPTION STRIKES
-- This query gets LC/UC data only for strikes that were last traded yesterday
-- Uses LTT (Last Traded Time) to filter for latest data only

DECLARE @YesterdayDate DATE = CAST(DATEADD(day, -1, GETDATE()) AS DATE);

-- Get yesterday's circuit limit data for all index option strikes
WITH YesterdayStrikes AS (
    -- Get all strikes that were traded yesterday
    SELECT DISTINCT
        oc.InstrumentToken,
        oc.TradingSymbol,
        oc.StrikePrice,
        oc.OptionType,
        oc.ExpiryDate,
        oc.IndexName
    FROM OptionContracts oc
    INNER JOIN HistoricalOptionData hod ON oc.InstrumentToken = hod.InstrumentToken
    WHERE CAST(hod.LastTradedTime AS DATE) = @YesterdayDate
      AND oc.IndexName IN ('NIFTY', 'BANKNIFTY', 'SENSEX', 'BANKEX') -- Index options only
),
LatestCircuitLimits AS (
    -- Get the latest circuit limits for each strike from yesterday
    SELECT 
        ys.InstrumentToken,
        ys.TradingSymbol,
        ys.StrikePrice,
        ys.OptionType,
        ys.ExpiryDate,
        ys.IndexName,
        hod.LastTradedTime,
        hod.LowerCircuitLimit AS LC,
        hod.UpperCircuitLimit AS UC,
        hod.OpenPrice,
        hod.HighPrice,
        hod.LowPrice,
        hod.ClosePrice,
        hod.Volume,
        hod.OpenInterest,
        -- Rank by latest time for each instrument
        ROW_NUMBER() OVER (
            PARTITION BY ys.InstrumentToken 
            ORDER BY hod.LastTradedTime DESC
        ) as rn
    FROM YesterdayStrikes ys
    INNER JOIN HistoricalOptionData hod ON ys.InstrumentToken = hod.InstrumentToken
    WHERE CAST(hod.LastTradedTime AS DATE) = @YesterdayDate
      AND hod.LowerCircuitLimit IS NOT NULL 
      AND hod.UpperCircuitLimit IS NOT NULL
)
-- Final result: Only the latest record for each strike
SELECT 
    IndexName,
    TradingSymbol,
    StrikePrice,
    OptionType,
    ExpiryDate,
    LastTradedTime,
    LC as LowerCircuitLimit,
    UC as UpperCircuitLimit,
    OpenPrice as [Open],
    HighPrice as [High], 
    LowPrice as [Low],
    ClosePrice as [Close],
    Volume,
    OpenInterest,
    -- Calculate circuit limit range
    (UC - LC) as CircuitRange,
    -- Calculate percentage from current price
    CASE 
        WHEN ClosePrice > 0 THEN ROUND(((LC / ClosePrice) - 1) * 100, 2)
        ELSE NULL 
    END as LowerCircuitPct,
    CASE 
        WHEN ClosePrice > 0 THEN ROUND(((UC / ClosePrice) - 1) * 100, 2)
        ELSE NULL 
    END as UpperCircuitPct
FROM LatestCircuitLimits
WHERE rn = 1  -- Only latest record for each strike
ORDER BY 
    IndexName,
    OptionType,
    StrikePrice;

-- Summary by Index
SELECT 
    'SUMMARY' as QueryType,
    IndexName,
    OptionType,
    COUNT(*) as TotalStrikes,
    AVG(LC) as AvgLowerCircuit,
    AVG(UC) as AvgUpperCircuit,
    MIN(StrikePrice) as MinStrike,
    MAX(StrikePrice) as MaxStrike
FROM LatestCircuitLimits
WHERE rn = 1
GROUP BY IndexName, OptionType
ORDER BY IndexName, OptionType;

-- Quick validation query
SELECT 
    'VALIDATION' as QueryType,
    @YesterdayDate as YesterdayDate,
    COUNT(*) as TotalRecordsFound,
    COUNT(DISTINCT InstrumentToken) as UniqueStrikes,
    MIN(LastTradedTime) as EarliestTime,
    MAX(LastTradedTime) as LatestTime
FROM LatestCircuitLimits
WHERE rn = 1; 