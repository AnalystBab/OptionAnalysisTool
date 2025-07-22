# Database Tables Analysis for Indian Option Analysis Tool

## 📊 **ACTUALLY USED TABLES** (Defined in ApplicationDbContext.cs)

### 🔥 **Core Circuit Limit Tracking Tables**

#### 1. **CircuitLimitTracker** 
- **Purpose:** Primary table for tracking circuit limit changes and breaches
- **Usage:** Stores every circuit limit change detected for all indices and strikes
- **Key Fields:** Symbol, UnderlyingSymbol, InstrumentToken, LowerCircuitLimit, UpperCircuitLimit, DetectedAt, SeverityLevel
- **Importance:** **CRITICAL** - This is the main table for your circuit limit tracking requirement

#### 2. **CircuitLimitChange**
- **Purpose:** Historical record of all circuit limit changes
- **Usage:** Tracks when and how circuit limits changed for any instrument
- **Key Fields:** Symbol, InstrumentToken, Timestamp, OldLowerLimit, NewLowerLimit, OldUpperLimit, NewUpperLimit
- **Importance:** **HIGH** - Essential for historical analysis

#### 3. **CircuitBreaker**
- **Purpose:** Tracks circuit breaker events and trading halts
- **Usage:** Records when trading is halted due to circuit limits
- **Key Fields:** Symbol, TradingDate, CircuitBreachType, BreachTime
- **Importance:** **MEDIUM** - Important for regulatory compliance

### 📈 **Real-time Data Tables**

#### 4. **IntradayOptionSnapshot**
- **Purpose:** Real-time option data with circuit limits
- **Usage:** Stores live option quotes including circuit limits for all strikes
- **Key Fields:** Symbol, StrikePrice, OptionType, LowerCircuitLimit, UpperCircuitLimit, Timestamp
- **Importance:** **CRITICAL** - Primary source for real-time circuit limit data

#### 5. **Quote**
- **Purpose:** Raw quote data from Kite Connect API
- **Usage:** Stores all quote responses including circuit limits
- **Key Fields:** InstrumentToken, TimeStamp, LowerCircuitLimit, UpperCircuitLimit, LastPrice
- **Importance:** **HIGH** - Source data for circuit limit tracking

#### 6. **SpotData**
- **Purpose:** Real-time index prices (NIFTY, BANKNIFTY, etc.)
- **Usage:** Tracks underlying index prices and their circuit limits
- **Key Fields:** Symbol, Exchange, LowerCircuitLimit, UpperCircuitLimit, Timestamp
- **Importance:** **HIGH** - Essential for index circuit limit tracking

### 📊 **Historical Data Tables**

#### 7. **HistoricalOptionData**
- **Purpose:** End-of-day option data storage
- **Usage:** Stores daily closing data including final circuit limits
- **Key Fields:** Symbol, TradingDate, LowerCircuitLimit, UpperCircuitLimit, CircuitLimitChanged
- **Importance:** **HIGH** - Historical analysis and EOD processing

#### 8. **DailyOptionContract**
- **Purpose:** Daily summary of option contracts
- **Usage:** Daily aggregated data including circuit limit breaches
- **Key Fields:** Symbol, TradingDate, LowerCircuitLimit, UpperCircuitLimit, CircuitBreachCount
- **Importance:** **MEDIUM** - Daily summaries

#### 9. **HistoricalPrice**
- **Purpose:** Historical price data for instruments
- **Usage:** Historical price analysis
- **Key Fields:** Symbol, Date, Open, High, Low, Close
- **Importance:** **MEDIUM** - Historical analysis

#### 10. **StockHistoricalPrice**
- **Purpose:** Historical price data for stocks
- **Usage:** Stock price history
- **Key Fields:** Symbol, Date, OHLC data
- **Importance:** **LOW** - Not directly related to options

### 🔧 **System Tables**

#### 11. **AuthenticationToken**
- **Purpose:** Stores Kite Connect access tokens
- **Usage:** Authentication and session management
- **Key Fields:** ApiKey, AccessToken, ExpiresAt, IsActive
- **Importance:** **CRITICAL** - Required for API access

#### 12. **Instrument**
- **Purpose:** Master data for all instruments
- **Usage:** Reference data for symbols, strikes, expiry dates
- **Key Fields:** InstrumentToken, TradingSymbol, Strike, Expiry, InstrumentType
- **Importance:** **HIGH** - Reference data

#### 13. **OptionContract**
- **Purpose:** Current state of option contracts
- **Usage:** Active contract tracking
- **Key Fields:** Symbol, StrikePrice, OptionType, IsActive
- **Importance:** **MEDIUM** - Contract management

#### 14. **IndexSnapshot**
- **Purpose:** Index price snapshots
- **Usage:** Index price tracking
- **Key Fields:** Symbol, Timestamp, OHLC data
- **Importance:** **MEDIUM** - Index tracking

#### 15. **DataDownloadStatus**
- **Purpose:** Tracks data download progress
- **Usage:** System monitoring and status tracking
- **Key Fields:** Symbol, Exchange, Status, LastDownloadTime
- **Importance:** **MEDIUM** - System monitoring

#### 16. **OptionMonitoringStats**
- **Purpose:** System performance and monitoring statistics
- **Usage:** Tracks data collection performance
- **Key Fields:** Timestamp, ActiveContractsCount, ErrorCount, ProcessingTime
- **Importance:** **MEDIUM** - System monitoring

---

## ❌ **UNUSED TABLES** (Not in ApplicationDbContext.cs)

The following tables are **NOT used** by your application and can be safely deleted:

1. **OptionDataPoint** - Not in DbContext
2. **CommonQuote** - Not in DbContext (CommonQuote is a model class, not a table)
3. **OpenInterestChange** - Not in DbContext
4. **OptionPrice** - Not in DbContext
5. **LTP** - Not in DbContext
6. **OHLC** - Not in DbContext

---

## 🗑️ **SQL TO TRUNCATE ONLY USED TABLES**

```sql
-- Core Circuit Limit Tables
TRUNCATE TABLE CircuitLimitTracker;
TRUNCATE TABLE CircuitLimitChange;
TRUNCATE TABLE CircuitBreaker;

-- Real-time Data Tables
TRUNCATE TABLE IntradayOptionSnapshots;
TRUNCATE TABLE Quotes;
TRUNCATE TABLE SpotData;

-- Historical Data Tables
TRUNCATE TABLE HistoricalOptionData;
TRUNCATE TABLE DailyOptionContracts;
TRUNCATE TABLE HistoricalPrices;
TRUNCATE TABLE StockHistoricalPrices;

-- System Tables
TRUNCATE TABLE AuthenticationTokens;
TRUNCATE TABLE Instruments;
TRUNCATE TABLE OptionContracts;
TRUNCATE TABLE IndexSnapshots;
TRUNCATE TABLE DataDownloadStatuses;
TRUNCATE TABLE OptionMonitoringStats;
```

---

## 🎯 **CRITICAL TABLES FOR CIRCUIT LIMIT TRACKING**

For your specific requirement of tracking **every circuit limit change for all indices and strikes**, these are the most important tables:

1. **CircuitLimitTracker** - Primary tracking table
2. **CircuitLimitChange** - Historical changes
3. **IntradayOptionSnapshot** - Real-time data with circuit limits
4. **Quote** - Raw API data with circuit limits
5. **SpotData** - Index circuit limits
6. **HistoricalOptionData** - EOD circuit limit data

---

## 📝 **RECOMMENDATION**

1. **Use the SQL above** to truncate only the tables that are actually used by your application
2. **Focus on CircuitLimitTracker and IntradayOptionSnapshot** for your circuit limit tracking
3. **The data service will automatically populate these tables** with circuit limit data from the Kite Quote API
4. **No duplicate prevention is needed** as the service already handles this through proper indexing and logic 