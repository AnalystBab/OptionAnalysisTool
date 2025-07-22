# 🔍 CIRCUIT LIMIT FORMULA REVERSE ENGINEERING GUIDE

> **Comprehensive approach to deduce the NSE/BSE circuit limit calculation formula for Indian index options**

## 🎯 **OBJECTIVE**

Reverse engineer the exact formula used by NSE/BSE to calculate circuit limits for index option contracts (NIFTY, BANKNIFTY, FINNIFTY, etc.) through systematic data analysis and pattern recognition.

---

## 📋 **CURRENT UNDERSTANDING**

### What We Know:
- Circuit limits change **dynamically** during market hours
- **Upper Circuit Limit**: Maximum price an option can reach
- **Lower Circuit Limit**: Minimum price an option can fall to
- Limits vary by **strike price**, **time to expiry**, and **market conditions**
- NSE/BSE don't publicly disclose the exact calculation formula

### What We Observe:
- Circuit ranges typically between **10-20%** of option price
- **ITM options** often have different ranges than **OTM options**
- **Weekly options** behave differently from **monthly options**
- Circuit limits **adjust in real-time** based on market volatility

---

## 🔬 **REVERSE ENGINEERING METHODOLOGY**

### 🎯 **Phase 1: Data Collection & Enrichment**

**Our Data Collection System Already Captures:**
```sql
-- Circuit Limit Tracking Data
SELECT 
    Symbol, StrikePrice, OptionType, ExpiryDate,
    PreviousLowerLimit, NewLowerLimit,
    PreviousUpperLimit, NewUpperLimit,
    CurrentPrice, UnderlyingPrice,
    Volume, OpenInterest, DetectedAt
FROM CircuitLimitTrackers
WHERE DetectedAt >= DATEADD(day, -60, GETDATE())
```

**Additional Context Data:**
- Implied Volatility (IV)
- Option Greeks (Delta, Gamma, Theta, Vega)
- Market timing (morning vs afternoon)
- Volatility regime (high vs normal)
- Index momentum

### 🔗 **Phase 2: Correlation Analysis**

**Key Factors to Analyze:**

1. **Implied Volatility Impact**
   ```
   Hypothesis: Circuit Range ∝ Implied Volatility
   Test: Correlation(IV, Circuit Range %)
   Expected: Strong positive correlation (r > 0.6)
   ```

2. **Time to Expiry Effect**
   ```
   Hypothesis: Circuit Range ∝ √(Time to Expiry)
   Test: Correlation(√T, Circuit Range %)
   Expected: Moderate positive correlation (r > 0.4)
   ```

3. **Moneyness (Strike/Spot Ratio)**
   ```
   Hypothesis: OTM options have wider circuits than ITM
   Test: Compare ATM vs OTM vs ITM circuit ranges
   ```

4. **Delta Sensitivity**
   ```
   Hypothesis: Higher Delta = Tighter circuit limits
   Test: Correlation(|Delta|, Circuit Range %)
   Expected: Negative correlation for high Delta options
   ```

5. **Option Price Impact**
   ```
   Hypothesis: Lower priced options have wider percentage circuits
   Test: Correlation(Option Price, Circuit Range %)
   ```

### 📊 **Phase 3: Pattern Recognition**

**Strike-wise Analysis:**
```python
# Pseudocode for pattern analysis
for each_strike in active_strikes:
    circuit_pattern = analyze_circuit_changes(strike)
    classify_pattern(circuit_pattern)
    identify_triggers(circuit_pattern)
```

**Time-based Patterns:**
- **Market Open (9:15-10:00)**: Initial circuit setting
- **Mid-day (11:00-14:00)**: Volatility adjustments
- **Market Close (15:00-15:30)**: Final adjustments

**Event-driven Changes:**
- RBI policy announcements
- Index rebalancing
- F&O expiry days
- Market volatility spikes

---

## 🧮 **MATHEMATICAL MODEL HYPOTHESES**

### **Hypothesis A: Volatility-Based Formula**
```
Upper Circuit = Option Price × (1 + Circuit Range Factor)
Lower Circuit = Option Price × (1 - Circuit Range Factor)

Where:
Circuit Range Factor = Base Range × Volatility Multiplier × Time Factor

Base Range = 0.15 (15% default)
Volatility Multiplier = (1 + k₁ × IV/100)
Time Factor = √(Time to Expiry in days / 30)
```

### **Hypothesis B: Black-Scholes Derived**
```
Circuit Range = k × σ × √T × Adjustment Factors

Where:
k = Base multiplier (0.8 to 1.2)
σ = Implied Volatility (annualized)
T = Time to expiry (in years)
Adjustment Factors = Delta Factor × Moneyness Factor
```

### **Hypothesis C: Regulatory Bounds Formula**
```
Raw Circuit Range = Theoretical Range × Market Conditions
Final Circuit Range = Max(Min Circuit, Min(Max Circuit, Raw Range))

Min Circuit = 10% (regulatory minimum)
Max Circuit = 20% (regulatory maximum)
```

### **Hypothesis D: Piecewise Function**
```
Circuit Range = {
    High Range (>15%)  if  IV > 30% OR Time < 7 days
    Medium Range (12-15%)  if  Standard conditions
    Low Range (<12%)  if  IV < 20% AND Time > 30 days
}
```

---

## 🔍 **VALIDATION METHODOLOGY**

### **1. Historical Backtesting**
- Split data: 75% training, 25% validation
- Test formula accuracy on unseen data
- Measure prediction error rates

### **2. Real-time Validation**
- Deploy formula in paper trading mode
- Compare predictions vs actual NSE/BSE limits
- Monitor accuracy during different market conditions

### **3. Cross-Index Validation**
- Test formula across NIFTY, BANKNIFTY, FINNIFTY
- Ensure consistency across different underlyings
- Validate parameter stability

### **4. Stress Testing**
- Test during high volatility events
- Validate during market crashes/rallies
- Check performance during expiry weeks

---

## 📈 **EXPECTED CORRELATION PATTERNS**

Based on option pricing theory, we expect:

| Factor | Expected Correlation | Rationale |
|--------|---------------------|-----------|
| **Implied Volatility** | **Strong Positive (r > 0.7)** | Higher IV = Wider price swings = Wider circuits |
| **Time to Expiry** | **Moderate Positive (r > 0.4)** | More time = More uncertainty = Wider circuits |
| **Moneyness** | **U-shaped Pattern** | Deep ITM/OTM have wider circuits than ATM |
| **Delta** | **Negative for High Delta** | High Delta options more predictable |
| **Option Price** | **Negative** | Lower priced options need wider % protection |
| **Volume** | **Weak Negative** | High volume = Better price discovery = Tighter circuits |

---

## 🚀 **IMPLEMENTATION STRATEGY**

### **Phase 1: Data Analysis (Week 1-2)**
```bash
# Run comprehensive analysis
dotnet run --project CircuitLimitFormulaReverseEngineering.cs

# Generate correlation report
./analyze_correlations.sh --period 60days --indices "NIFTY,BANKNIFTY,FINNIFTY"
```

### **Phase 2: Formula Development (Week 3-4)**
```csharp
// Implement top hypotheses
var formulaA = new VolatilityBasedFormula(baseRange: 0.15, volMultiplier: 0.5);
var formulaB = new BlackScholesFormula(baseMultiplier: 1.0, adjustments: true);
var formulaC = new RegulatoryBoundsFormula(minRange: 0.10, maxRange: 0.20);

// Cross-validate
var accuracy = await ValidateFormulas(new[] { formulaA, formulaB, formulaC });
```

### **Phase 3: Real-time Testing (Week 5-6)**
```csharp
// Deploy in monitoring mode
var monitor = new CircuitLimitFormulaMonitor();
await monitor.StartRealtimeValidation(selectedFormula);

// Track accuracy
var metrics = await monitor.GetAccuracyMetrics();
Console.WriteLine($"Prediction Accuracy: {metrics.Accuracy:P2}");
```

---

## 📊 **SUCCESS METRICS**

### **Primary Metrics:**
- **Prediction Accuracy**: >85% for circuit limit values
- **Direction Accuracy**: >90% for increase/decrease predictions
- **Timing Accuracy**: Predict changes within 5 minutes

### **Secondary Metrics:**
- **False Positive Rate**: <5%
- **Coverage**: Works for >95% of actively traded strikes
- **Stability**: Consistent performance across market regimes

---

## 🎯 **THEORETICAL FOUNDATION**

### **Option Pricing Theory Basis:**
The circuit limit formula likely incorporates elements from:

1. **Black-Scholes Model**: For volatility and time effects
2. **Greeks Calculation**: Especially Delta for price sensitivity
3. **Risk Management**: Regulatory bounds and market protection
4. **Market Microstructure**: Volume and liquidity considerations

### **Expected Formula Structure:**
```
Circuit Limit = f(S, K, T, σ, r, market_conditions)

Where:
S = Underlying price
K = Strike price
T = Time to expiry
σ = Implied volatility
r = Risk-free rate
market_conditions = Volume, OI, market regime
```

---

## 🔬 **ADVANCED ANALYSIS TECHNIQUES**

### **1. Machine Learning Approach**
```python
# Feature engineering
features = [
    'implied_volatility', 'time_to_expiry', 'moneyness',
    'delta', 'gamma', 'vega', 'theta',
    'volume', 'open_interest', 'underlying_volatility',
    'market_hour', 'day_of_week', 'days_to_expiry'
]

# Model training
model = RandomForestRegressor(n_estimators=100)
model.fit(X_train[features], y_train['circuit_range'])
```

### **2. Regime Detection**
```sql
-- Identify different market regimes
WITH MarketRegimes AS (
    SELECT Date,
           CASE 
               WHEN UnderlyingVolatility > 25 THEN 'High Vol'
               WHEN UnderlyingVolatility < 15 THEN 'Low Vol'
               ELSE 'Normal Vol'
           END as VolatilityRegime
    FROM MarketData
)
-- Analyze circuit patterns by regime
```

### **3. Seasonality Analysis**
```sql
-- Check for expiry week effects
SELECT 
    DaysToExpiry,
    AVG(CircuitRangePercent) as AvgCircuitRange,
    COUNT(*) as Observations
FROM CircuitLimitAnalysis
GROUP BY DaysToExpiry
ORDER BY DaysToExpiry
```

---

## 📚 **RESEARCH METHODOLOGY**

### **Literature Review:**
- NSE/BSE regulatory guidelines
- Academic papers on circuit breakers
- International exchange practices
- Option pricing and risk management theory

### **Data Sources:**
- Live market data from Kite Connect API
- Historical NSE data
- Volatility indices (India VIX)
- Economic calendar events

### **Benchmarking:**
- Compare with international practices (CME, CBOE)
- Academic theoretical models
- Industry best practices

---

## 🎯 **EXPECTED OUTCOMES**

### **Primary Deliverable:**
A validated mathematical formula that predicts NSE/BSE circuit limits with >85% accuracy across different market conditions.

### **Secondary Deliverables:**
1. **Comprehensive Analysis Report** with correlation findings
2. **Real-time Monitoring System** for circuit limit changes
3. **Predictive Model** for anticipating circuit adjustments
4. **Trading Strategy Applications** leveraging circuit limit insights

---

## 🚀 **DEPLOYMENT PLAN**

### **Production Integration:**
```csharp
// Integrate with existing system
public class EnhancedCircuitLimitMonitor : RealTimeCircuitLimitMonitoringService
{
    private readonly CircuitLimitFormulaPredictor _predictor;
    
    protected override async Task ProcessQuoteUpdate(KiteQuote quote)
    {
        // Existing monitoring
        await base.ProcessQuoteUpdate(quote);
        
        // Predictive analysis
        var prediction = await _predictor.PredictCircuitLimitChange(quote);
        if (prediction.ChangeExpected)
        {
            await AlertCircuitLimitChangeExpected(prediction);
        }
    }
}
```

### **Monitoring & Alerting:**
- Real-time accuracy tracking
- Performance degradation alerts
- Model drift detection
- Recalibration triggers

---

This comprehensive approach combines **quantitative analysis**, **machine learning**, and **domain expertise** to reverse engineer the circuit limit formula. The systematic methodology ensures we can validate our findings and deploy a production-ready solution.

**🎯 Next Step**: Run the analysis tools we've built to start collecting data and generating initial hypotheses! 