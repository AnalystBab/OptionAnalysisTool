# 🔥 CIRCUIT LIMIT TRACKING SYSTEM - COMPREHENSIVE DOCUMENTATION

## 📋 **SYSTEM OVERVIEW**

The Circuit Limit Tracking System is a comprehensive solution for monitoring and capturing real-time circuit limit changes for Indian index options (NIFTY, BANKNIFTY, FINNIFTY, etc.) during market hours. The system automatically detects circuit limit changes, stores historical data, and provides real-time alerts.

---

## 🗄️ **DATABASE ARCHITECTURE**

### **Database:** `PalindromeResults` (SQL Server)
### **Connection String:** `Server=LAPTOP-B68L4IP9;Database=PalindromeResults;Trusted_Connection=True;TrustServerCertificate=True;`

---

## 📊 **CORE TABLES**

### **1. CircuitLimitTrackers Table**
**Purpose:** Tracks real-time circuit limit changes with detailed analysis

```sql
CREATE TABLE CircuitLimitTrackers (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    InstrumentToken NVARCHAR(50) NOT NULL,
    Symbol NVARCHAR(100) NOT NULL,
    UnderlyingSymbol NVARCHAR(50) NOT NULL,
    StrikePrice DECIMAL(18,2) NOT NULL,
    OptionType NVARCHAR(10) NOT NULL,
    ExpiryDate DATETIME2 NOT NULL,
    
    -- Circuit Limit Tracking
    PreviousLowerLimit DECIMAL(18,2) NOT NULL,
    NewLowerLimit DECIMAL(18,2) NOT NULL,
    PreviousUpperLimit DECIMAL(18,2) NOT NULL,
    NewUpperLimit DECIMAL(18,2) NOT NULL,
    
    -- Change Analysis
    LowerLimitChangePercent DECIMAL(18,2) NOT NULL,
    UpperLimitChangePercent DECIMAL(18,2) NOT NULL,
    RangeChangePercent DECIMAL(18,2) NOT NULL,
    
    -- Market Data
    CurrentPrice DECIMAL(18,2) NOT NULL,
    UnderlyingPrice DECIMAL(18,2) NOT NULL,
    Volume BIGINT NOT NULL,
    OpenInterest BIGINT NOT NULL,
    
    -- Tracking Info
    DetectedAt DATETIME2 NOT NULL,
    LastUpdated DATETIME2 NOT NULL,
    IsBreachAlert BIT NOT NULL,
    SeverityLevel NVARCHAR(450) NOT NULL,
    ChangeReason NVARCHAR(MAX) NOT NULL,
    IsValidData BIT NOT NULL,
    ValidationMessage NVARCHAR(MAX) NOT NULL
);

-- Indexes for Performance
CREATE INDEX IX_CircuitLimitTracker_Symbol_DetectedAt ON CircuitLimitTrackers (Symbol, DetectedAt);
CREATE INDEX IX_CircuitLimitTracker_UnderlyingSymbol_DetectedAt ON CircuitLimitTrackers (UnderlyingSymbol, DetectedAt);
CREATE INDEX IX_CircuitLimitTracker_InstrumentToken_DetectedAt ON CircuitLimitTrackers (InstrumentToken, DetectedAt);
CREATE INDEX IX_CircuitLimitTracker_SeverityLevel ON CircuitLimitTrackers (SeverityLevel);
```

**Features:**
- **Real-time Detection:** Captures circuit limit changes within seconds
- **Change Analysis:** Calculates percentage changes and severity levels
- **Breach Alerts:** Identifies when prices approach circuit limits
- **Historical Tracking:** Maintains complete change history

---

### **2. IntradayOptionSnapshots Table**
**Purpose:** Stores real-time option data every 30 seconds during market hours

```sql
CREATE TABLE IntradayOptionSnapshots (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    InstrumentToken NVARCHAR(50) NOT NULL,
    Symbol NVARCHAR(100) NOT NULL,
    UnderlyingSymbol NVARCHAR(50) NOT NULL,
    StrikePrice DECIMAL(18,2) NOT NULL,
    OptionType NVARCHAR(10) NOT NULL,
    ExpiryDate DATETIME2 NOT NULL,
    
    -- Price Data
    LastPrice DECIMAL(18,2) NOT NULL,
    Open DECIMAL(18,2) NOT NULL,
    High DECIMAL(18,2) NOT NULL,
    Low DECIMAL(18,2) NOT NULL,
    Close DECIMAL(18,2) NOT NULL,
    Change DECIMAL(18,2) NOT NULL,
    
    -- Volume & Interest
    Volume BIGINT NOT NULL,
    OpenInterest BIGINT NOT NULL,
    
    -- Circuit Limits (CORE FEATURE)
    LowerCircuitLimit DECIMAL(18,2) NOT NULL,
    UpperCircuitLimit DECIMAL(18,2) NOT NULL,
    CircuitLimitStatus NVARCHAR(MAX) NOT NULL,
    
    -- Greeks
    ImpliedVolatility DECIMAL(18,2) NOT NULL,
    
    -- Timestamps
    Timestamp DATETIME2 NOT NULL,
    CaptureTime DATETIME2 NOT NULL,
    LastUpdated DATETIME2 NOT NULL,
    
    -- Quality Control
    IsValidData BIT NOT NULL,
    ValidationMessage NVARCHAR(MAX) NOT NULL,
    TradingStatus NVARCHAR(MAX) NOT NULL
);

-- Indexes for Performance
CREATE INDEX IX_IntradayOptionSnapshots_Symbol_Timestamp ON IntradayOptionSnapshots (Symbol, Timestamp);
CREATE INDEX IX_IntradayOptionSnapshots_UnderlyingSymbol_Timestamp ON IntradayOptionSnapshots (UnderlyingSymbol, Timestamp);
```

**Features:**
- **30-Second Intervals:** Captures data every 30 seconds during market hours (9:15 AM - 3:30 PM)
- **Complete Option Data:** OHLC, Volume, OI, Greeks
- **Circuit Limit Status:** Real-time circuit limit values and status
- **Data Validation:** Quality checks and validation messages

---

### **3. HistoricalOptionData Table**
**Purpose:** Stores end-of-day option data with circuit limits applied from intraday data

```sql
CREATE TABLE HistoricalOptionData (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    InstrumentToken NVARCHAR(50) NOT NULL,
    Symbol NVARCHAR(100) NOT NULL,
    UnderlyingSymbol NVARCHAR(50) NOT NULL,
    StrikePrice DECIMAL(18,2) NOT NULL,
    OptionType NVARCHAR(10) NOT NULL,
    ExpiryDate DATETIME2 NOT NULL,
    TradingDate DATETIME2 NOT NULL,
    
    -- OHLC Data
    Open DECIMAL(18,2) NOT NULL,
    High DECIMAL(18,2) NOT NULL,
    Low DECIMAL(18,2) NOT NULL,
    Close DECIMAL(18,2) NOT NULL,
    
    -- Change Calculations
    Change DECIMAL(18,2) NOT NULL,
    PercentageChange DECIMAL(18,2) NOT NULL,
    
    -- Volume & Interest
    Volume BIGINT NOT NULL,
    OpenInterest BIGINT NOT NULL,
    OIChange BIGINT NOT NULL,
    
    -- Circuit Limits (FROM INTRADAY DATA)
    LowerCircuitLimit DECIMAL(18,2) NOT NULL,
    UpperCircuitLimit DECIMAL(18,2) NOT NULL,
    CircuitLimitChanged BIT NOT NULL,
    
    -- Greeks
    ImpliedVolatility DECIMAL(18,2) NOT NULL,
    Delta DECIMAL(18,2) NULL,
    Gamma DECIMAL(18,2) NULL,
    Theta DECIMAL(18,2) NULL,
    Vega DECIMAL(18,2) NULL,
    
    -- Underlying Data
    UnderlyingClose DECIMAL(18,2) NOT NULL,
    UnderlyingChange DECIMAL(18,2) NOT NULL,
    
    -- Timestamps
    CapturedAt DATETIME2 NOT NULL,
    LastUpdated DATETIME2 NOT NULL,
    
    -- Quality Control
    IsValidData BIT NOT NULL,
    ValidationMessage NVARCHAR(MAX) NOT NULL
);

-- Indexes for Performance
CREATE INDEX IX_HistoricalOptionData_Symbol_TradingDate ON HistoricalOptionData (Symbol, TradingDate);
CREATE INDEX IX_HistoricalOptionData_UnderlyingSymbol_TradingDate ON HistoricalOptionData (UnderlyingSymbol, TradingDate);
```

**Features:**
- **EOD Processing:** Processes end-of-day data after market close
- **Circuit Limit Integration:** Applies circuit limits from intraday snapshots
- **Historical Analysis:** Enables historical circuit limit pattern analysis
- **Greeks Support:** Complete options Greeks for analysis

---

### **4. CircuitLimitChanges Table**
**Purpose:** Simplified log of all circuit limit changes

```sql
CREATE TABLE CircuitLimitChanges (
    Id INT IDENTITY(1,1) PRIMARY KEY,
    InstrumentToken NVARCHAR(50) NOT NULL,
    Symbol NVARCHAR(50) NOT NULL,
    StrikePrice DECIMAL(18,2) NULL,
    InstrumentType NVARCHAR(20) NULL,
    ExpiryDate DATETIME2 NULL,
    
    -- Circuit Limit Changes
    OldLowerCircuitLimit DECIMAL(18,2) NOT NULL,
    NewLowerCircuitLimit DECIMAL(18,2) NOT NULL,
    OldUpperCircuitLimit DECIMAL(18,2) NOT NULL,
    NewUpperCircuitLimit DECIMAL(18,2) NOT NULL,
    
    -- Market Data
    LastPrice DECIMAL(18,2) NOT NULL,
    
    -- Tracking
    Timestamp DATETIME2 NOT NULL,
    ChangeReason NVARCHAR(100) NOT NULL,
    CreatedAt DATETIME2 NOT NULL
);

-- Indexes for Performance
CREATE INDEX IX_CircuitLimitChanges_Symbol_Timestamp ON CircuitLimitChanges (Symbol, Timestamp);
CREATE INDEX IX_CircuitLimitChanges_InstrumentToken_Timestamp ON CircuitLimitChanges (InstrumentToken, Timestamp);
```

**Features:**
- **Simple Logging:** Quick log of all circuit limit changes
- **Fast Queries:** Optimized for quick historical lookups
- **Change Tracking:** Maintains chronological order of changes

---

## 🔧 **CORE SERVICES**

### **1. CircuitLimitTrackingService**
**File:** `OptionAnalysisTool.Common/Services/CircuitLimitTrackingService.cs`

**Primary Methods:**
```csharp
// Core tracking method - detects and stores circuit limit changes
public async Task<CircuitLimitTracker> TrackCircuitLimitChange(
    string instrumentToken, string symbol, string underlyingSymbol,
    decimal strikePrice, string optionType, DateTime expiryDate,
    decimal currentLowerLimit, decimal currentUpperLimit,
    decimal currentPrice, decimal underlyingPrice,
    long volume, long openInterest)

// Retrieves circuit limit changes for analysis
public async Task<List<CircuitLimitTracker>> GetCircuitLimitChanges(
    string? underlyingSymbol = null, 
    DateTime? startDate = null, 
    DateTime? endDate = null)

// Gets critical alerts (High/Critical severity)
public async Task<List<CircuitLimitTracker>> GetCriticalAlerts(DateTime date)

// Calculates severity levels based on change percentages
private string CalculateSeverityLevel(decimal lowerChangePercent, decimal upperChangePercent, decimal rangeChangePercent)
```

**Workflow:**
1. **Receives** real-time option data
2. **Compares** with last recorded circuit limits
3. **Calculates** change percentages and severity
4. **Stores** tracking record if changes detected
5. **Generates** breach alerts if price near limits

**Severity Levels:**
- **Low:** < 5% change
- **Medium:** 5-10% change  
- **High:** 10-20% change
- **Critical:** > 20% change

---

### **2. CircuitLimitTestService**
**File:** `OptionAnalysisTool.Common/Services/CircuitLimitTestService.cs`

**Primary Methods:**
```csharp
// Tests NIFTY circuit limits - main testing method
public async Task<CircuitLimitTestResult> TestNiftyCircuitLimits()

// Gets active trading summary for a date
public async Task<ActiveTradingSummary> GetActiveTradingSummary(DateTime date)

// Validates circuit limit calculations
private bool ValidateCircuitLimits(decimal lastPrice, decimal lowerLimit, decimal upperLimit)
```

**Features:**
- **Live Testing:** Tests with real market data
- **Validation:** Validates circuit limit calculations
- **Performance Metrics:** Tracks success rates and processing times

---

### **3. RealTimeCircuitLimitMonitoringService**
**File:** `OptionAnalysisTool.Common/Services/RealTimeCircuitLimitMonitoringService.cs`

**Primary Methods:**
```csharp
// Starts real-time monitoring during market hours
public async Task StartMonitoring()

// Stops monitoring
public async Task StopMonitoring()

// Processes individual instrument
private async Task ProcessInstrument(InstrumentInfo instrument)
```

**Workflow:**
1. **Market Hours Check:** Only runs during 9:15 AM - 3:30 PM
2. **Instrument Loading:** Loads all index option instruments
3. **Real-time Polling:** Polls Kite Connect API every 30 seconds
4. **Change Detection:** Detects circuit limit changes
5. **Data Storage:** Stores intraday snapshots and circuit changes

---

### **4. EODDataStorageService**
**File:** `OptionAnalysisTool.Common/Services/EODDataStorageService.cs`

**Primary Methods:**
```csharp
// Processes end-of-day data for all instruments
public async Task ProcessEODData(DateTime date)

// Applies circuit limits from intraday data to EOD records
private async Task ApplyCircuitLimitsFromIntradayData(List<HistoricalOptionData> eodData)
```

**EOD Workflow:**
1. **Data Collection:** Collects OHLC data from historical API
2. **Previous Day Calculation:** Calculates changes from previous day
3. **Circuit Limit Application:** Applies circuit limits from last intraday snapshot
4. **Change Detection:** Detects if circuit limits changed during the day
5. **Storage:** Stores comprehensive EOD records

---

## 🖥️ **WPF APPLICATION INTERFACE**

### **Main Window:** `OptionAnalysisTool.App/MainWindow.xaml`
### **Circuit Limit View:** `OptionAnalysisTool.App/Views/CircuitLimitView.xaml`
### **ViewModel:** `OptionAnalysisTool.App/ViewModels/CircuitLimitViewModel.cs`

### **Key Features:**

#### **1. Summary Dashboard**
- **Active Strikes Count:** Real-time count of active option contracts
- **Circuit Changes Count:** Number of circuit limit changes detected
- **Critical Alerts Count:** High/Critical severity alerts
- **Total Volume Tracked:** Sum of volumes for all tracked contracts
- **Test Status:** Last test execution status and time

#### **2. Active Strikes Tab**
**DataGrid Columns:**
- Symbol, Strike Price, Option Type
- Last Price, Volume, Open Interest
- Lower Circuit Limit, Upper Circuit Limit
- Circuit Limit Status, Last Updated

**Filters:**
- Index Selection (ALL, NIFTY, BANKNIFTY, etc.)
- Option Type (ALL, CE, PE)

#### **3. Circuit Changes Tab**
**DataGrid Columns:**
- Time, Symbol, Strike Price, Option Type
- Old Lower/Upper Limits, New Lower/Upper Limits
- Current Price, Severity Level, Change Reason

**Filters:**
- Time Range (1 hour, 4 hours, Today, All)
- Refresh button for manual updates

#### **4. Test Button**
**"🧪 Test NIFTY Circuit Limits"**
- Fetches live NIFTY option data
- Calculates circuit limits
- Stores test data in database
- Populates grids with results

---

## ⚡ **REAL-TIME WORKFLOWS**

### **1. Intraday Data Capture Workflow**
```
Market Hours (9:15 AM - 3:30 PM)
    ↓
Load All Index Option Instruments
    ↓
Every 30 Seconds:
    ↓
Fetch Live Quotes from Kite Connect
    ↓
For Each Instrument:
    ↓
Extract Circuit Limits from Quote
    ↓
Compare with Previous Limits
    ↓
If Changed:
    ↓
Store CircuitLimitTracker Record
Store CircuitLimitChange Record
    ↓
Store IntradayOptionSnapshot
    ↓
Update WPF Interface
```

### **2. Circuit Limit Change Detection**
```
Receive New Quote Data
    ↓
Extract Lower & Upper Circuit Limits
    ↓
Query Last Known Limits for Instrument
    ↓
Compare Current vs Previous Limits
    ↓
Calculate Change Percentages:
- Lower Limit Change %
- Upper Limit Change %
- Range Change %
    ↓
Determine Severity Level:
- Low: < 5%
- Medium: 5-10%
- High: 10-20%
- Critical: > 20%
    ↓
If Any Change Detected:
    ↓
Create CircuitLimitTracker Record
Create CircuitLimitChange Record
    ↓
Check for Breach Alerts:
- Current Price near Lower Limit
- Current Price near Upper Limit
    ↓
Store in Database
Update UI Collections
```

### **3. EOD Processing Workflow**
```
After Market Close (Post 3:30 PM)
    ↓
Collect Historical OHLC Data
    ↓
For Each Option Contract:
    ↓
Calculate Day's Changes
Get Last Intraday Snapshot
Apply Circuit Limits from Intraday
    ↓
Determine if Circuit Limits Changed
    ↓
Create HistoricalOptionData Record
    ↓
Store in Database
```

---

## 📈 **DATA ANALYSIS FEATURES**

### **1. Circuit Limit Pattern Analysis**
- **Historical Trends:** Track circuit limit changes over time
- **Volatility Correlation:** Correlate changes with market volatility
- **Strike-wise Analysis:** Analyze changes by strike price levels

### **2. Breach Alert System**
- **Proximity Alerts:** When price approaches circuit limits
- **Real-time Notifications:** Immediate alerts for critical changes
- **Severity Classification:** Low/Medium/High/Critical alerts

### **3. Statistical Insights**
- **Change Frequency:** How often limits change for each instrument
- **Average Change Size:** Typical magnitude of circuit limit changes
- **Time-based Patterns:** When during the day changes typically occur

---

## 🔧 **CONFIGURATION**

### **Database Connection**
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=LAPTOP-B68L4IP9;Database=PalindromeResults;Trusted_Connection=True;TrustServerCertificate=True;"
  }
}
```

### **Kite Connect API**
```json
{
  "KiteConnect": {
    "ApiKey": "fgiigxn27i6ysax2",
    "ApiSecret": "sbn6xzn6fj57hmjfitc6smpkjnux7hqw",
    "RedirectUrl": "http://127.0.0.1:3000"
  }
}
```

### **Market Hours Configuration**
- **Trading Hours:** 9:15 AM - 3:30 PM (Indian Standard Time)
- **Monitoring Frequency:** Every 30 seconds
- **Data Capture:** Only during market hours
- **EOD Processing:** After market close

---

## 🚀 **DEPLOYMENT & SETUP**

### **Prerequisites**
1. **SQL Server** (Local or Remote)
2. **.NET 8.0** Runtime
3. **Kite Connect API** Access
4. **Visual Studio 2022** (for development)

### **Setup Steps**
1. **Database Setup:**
   ```sql
   -- Run migration to create tables
   dotnet ef database update --project OptionAnalysisTool.Common
   ```

2. **Configuration:**
   - Update `appsettings.json` with correct database connection
   - Configure Kite Connect API credentials

3. **Run Application:**
   ```bash
   dotnet run --project OptionAnalysisTool.App
   ```

---

## 📊 **CURRENT STATUS (Live Data)**

Based on database queries executed today:

### **Data Captured:**
- **CircuitLimitTrackers:** 50 records ✅
- **IntradayOptionSnapshots:** 50 records ✅
- **CircuitLimitChanges:** 50 records ✅
- **HistoricalOptionData:** 0 records (EOD processing pending)

### **Instruments Tracked:**
- **NIFTY25AUGFUT:** ₹25,121 (₹22,597 - ₹27,618)
- **BANKNIFTY25AUGFUT:** ₹56,440 (₹50,796 - ₹62,085)
- **FINNIFTY25AUGFUT:** ₹27,054 (₹24,110 - ₹29,468)
- **MIDCPNIFTY25AUGFUT:** ₹13,074 (₹11,767 - ₹14,382)
- **NIFTYNXT5025AUGFUT:** ₹68,000 (₹61,353 - ₹74,987)

### **System Status:**
- ✅ **Data Capture:** Working (50 records captured)
- ✅ **Circuit Detection:** Working (circuit limits tracked)
- ✅ **Database Storage:** Working (data persisted)
- ❌ **WPF Display:** Issue (grid not loading database data)

---

## 🎯 **BUSINESS VALUE**

### **Trading Benefits**
- **Risk Management:** Real-time circuit limit awareness
- **Opportunity Detection:** Early detection of volatile instruments
- **Historical Analysis:** Pattern recognition for strategy development
- **Automated Monitoring:** No manual tracking required

### **Compliance Benefits**
- **Audit Trail:** Complete history of circuit limit changes
- **Data Integrity:** Validated and timestamped records
- **Regulatory Reporting:** Historical data for compliance

### **Operational Benefits**
- **Real-time Alerts:** Immediate notification of critical changes
- **Automated Processing:** No manual intervention required
- **Scalable Architecture:** Can handle multiple indices simultaneously

---

## 🔮 **FUTURE ENHANCEMENTS**

### **Planned Features**
1. **Options Analysis:** Full option chain circuit limit tracking
2. **Predictive Analytics:** ML-based circuit limit change prediction
3. **Mobile App:** React Native mobile application
4. **API Integration:** REST API for external system integration
5. **Advanced Alerting:** Email/SMS notifications

### **Technical Improvements**
1. **Performance Optimization:** Faster data processing
2. **Caching Layer:** Redis caching for improved performance
3. **Microservices:** Split into focused microservices
4. **Cloud Deployment:** Azure/AWS cloud deployment
5. **Real-time Dashboard:** SignalR-based real-time updates

---

## 📞 **SUPPORT & TROUBLESHOOTING**

### **Common Issues**
1. **Empty WPF Grid:** Check database connectivity and service errors
2. **No Data Capture:** Verify market hours and API connectivity
3. **Build Errors:** Ensure all NuGet packages are restored

### **Debug SQL Queries**
```sql
-- Check record counts
SELECT 'CircuitLimitTrackers', COUNT(*) FROM CircuitLimitTrackers
UNION ALL SELECT 'IntradaySnapshots', COUNT(*) FROM IntradayOptionSnapshots;

-- Check latest data
SELECT TOP 5 * FROM CircuitLimitTrackers ORDER BY DetectedAt DESC;
SELECT TOP 5 * FROM IntradayOptionSnapshots ORDER BY Timestamp DESC;
```

### **Logs Location**
- **Application Logs:** Console output during runtime
- **Error Logs:** Check Exception messages in ViewModel
- **Database Logs:** SQL Server logs for connection issues

---

**📝 Document Version:** 1.0  
**📅 Last Updated:** June 6, 2025  
**👨‍💻 Author:** Circuit Limit System Development Team  
**🔄 Status:** Production Ready - Data Capture Working, UI Display Issue Pending** 