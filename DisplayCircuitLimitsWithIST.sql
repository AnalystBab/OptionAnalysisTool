-- 🔥 CIRCUIT LIMIT ANALYSIS WITH IST TIMESTAMPS
-- Displays your data with proper Indian Standard Time conversion

SELECT 
    Id,
    InstrumentToken,
    TradingSymbol,
    Symbol,
    UnderlyingSymbol,
    StrikePrice,
    OptionType,
    ExpiryDate,
    LastPrice,
    Open,
    High,
    Low,
    Close,
    Change,
    Volume,
    OpenInterest,
    LowerCircuitLimit,
    UpperCircuitLimit,
    CircuitLimitStatus,
    ImpliedVolatility,
    
    -- 🔥 TIMESTAMP CONVERSION TO IST
    DATEADD(HOUR, 5, DATEADD(MINUTE, 30, Timestamp)) AS Timestamp_IST,
    DATEADD(HOUR, 5, DATEADD(MINUTE, 30, CaptureTime)) AS CaptureTime_IST,
    DATEADD(HOUR, 5, DATEADD(MINUTE, 30, LastUpdated)) AS LastUpdated_IST,
    
    -- 📊 MARKET HOURS ANALYSIS
    CASE 
        WHEN DATEADD(HOUR, 5, DATEADD(MINUTE, 30, Timestamp)).TimeOfDay >= '09:15:00' 
         AND DATEADD(HOUR, 5, DATEADD(MINUTE, 30, Timestamp)).TimeOfDay <= '15:30:00'
         AND DATEPART(WEEKDAY, DATEADD(HOUR, 5, DATEADD(MINUTE, 30, Timestamp))) BETWEEN 2 AND 6
        THEN 'MARKET HOURS'
        ELSE 'OUTSIDE MARKET HOURS'
    END AS MarketStatus,
    
    -- 🎯 CIRCUIT LIMIT CHANGE ANALYSIS
    CASE 
        WHEN LowerCircuitLimit = 0.05 AND UpperCircuitLimit = 777.70 THEN 'CURRENT LIMITS'
        WHEN LowerCircuitLimit = 0.05 AND UpperCircuitLimit = 507.00 THEN 'PREVIOUS LIMITS'
        ELSE 'OTHER LIMITS'
    END AS LimitStatus

FROM IntradayOptionSnapshots 
WHERE TradingSymbol = 'NIFTY2572424950CE'
  AND Timestamp >= '2025-07-21 00:00:00'
ORDER BY Timestamp DESC;

-- 📈 CIRCUIT LIMIT CHANGE SUMMARY
SELECT 
    'CIRCUIT LIMIT CHANGE SUMMARY' AS Analysis,
    MIN(DATEADD(HOUR, 5, DATEADD(MINUTE, 30, Timestamp))) AS First_Record_IST,
    MAX(DATEADD(HOUR, 5, DATEADD(MINUTE, 30, Timestamp))) AS Last_Record_IST,
    COUNT(*) AS Total_Records,
    COUNT(DISTINCT UpperCircuitLimit) AS Unique_Circuit_Limits,
    STRING_AGG(CAST(UpperCircuitLimit AS VARCHAR(10)), ', ') AS All_Circuit_Limits_Found
FROM IntradayOptionSnapshots 
WHERE TradingSymbol = 'NIFTY2572424950CE'
  AND Timestamp >= '2025-07-21 00:00:00';

-- ⏰ MARKET HOURS BREAKDOWN
SELECT 
    MarketStatus,
    COUNT(*) AS Record_Count,
    MIN(DATEADD(HOUR, 5, DATEADD(MINUTE, 30, Timestamp))) AS Earliest_Time_IST,
    MAX(DATEADD(HOUR, 5, DATEADD(MINUTE, 30, Timestamp))) AS Latest_Time_IST
FROM (
    SELECT 
        Timestamp,
        CASE 
            WHEN DATEADD(HOUR, 5, DATEADD(MINUTE, 30, Timestamp)).TimeOfDay >= '09:15:00' 
             AND DATEADD(HOUR, 5, DATEADD(MINUTE, 30, Timestamp)).TimeOfDay <= '15:30:00'
             AND DATEPART(WEEKDAY, DATEADD(HOUR, 5, DATEADD(MINUTE, 30, Timestamp))) BETWEEN 2 AND 6
            THEN 'MARKET HOURS'
            ELSE 'OUTSIDE MARKET HOURS'
        END AS MarketStatus
    FROM IntradayOptionSnapshots 
    WHERE TradingSymbol = 'NIFTY2572424950CE'
      AND Timestamp >= '2025-07-21 00:00:00'
) AS MarketAnalysis
GROUP BY MarketStatus
ORDER BY MarketStatus; 