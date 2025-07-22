using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using OptionAnalysisTool.Common.Data;
using OptionAnalysisTool.Models;
using System.ComponentModel.DataAnnotations;

namespace OptionAnalysisTool.Common.Services
{
    /// <summary>
    /// 🔍 CIRCUIT LIMIT FORMULA ANALYZER
    /// Reverse engineers the NSE/BSE circuit limit calculation formula through data analysis
    /// </summary>
    public class CircuitLimitFormulaAnalyzer
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<CircuitLimitFormulaAnalyzer> _logger;

        public CircuitLimitFormulaAnalyzer(
            ApplicationDbContext context,
            ILogger<CircuitLimitFormulaAnalyzer> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// 🎯 MAIN ANALYSIS: Reverse engineer circuit limit formula
        /// </summary>
        public async Task<CircuitLimitFormulaAnalysis> AnalyzeCircuitLimitFormula(
            string underlyingSymbol = "NIFTY",
            DateTime? fromDate = null,
            DateTime? toDate = null)
        {
            var startDate = fromDate ?? DateTime.Today.AddDays(-30);
            var endDate = toDate ?? DateTime.Today;

            _logger.LogInformation("🔍 Starting circuit limit formula analysis for {underlying} from {startDate} to {endDate}",
                underlyingSymbol, startDate, endDate);

            var analysis = new CircuitLimitFormulaAnalysis
            {
                UnderlyingSymbol = underlyingSymbol,
                AnalysisDate = DateTime.UtcNow,
                FromDate = startDate,
                ToDate = endDate
            };

            // 1. Collect comprehensive dataset
            var dataset = await CollectAnalysisDataset(underlyingSymbol, startDate, endDate);
            analysis.TotalDataPoints = dataset.Count;

            if (dataset.Count < 100)
            {
                _logger.LogWarning("⚠️ Insufficient data for analysis: {count} points", dataset.Count);
                analysis.FormulaConfidence = 0.0;
                return analysis;
            }

            // 2. Analyze correlation with known factors
            await AnalyzeCorrelationFactors(analysis, dataset);

            // 3. Pattern recognition for different option characteristics
            await AnalyzeOptionCharacteristicPatterns(analysis, dataset);

            // 4. Time-based pattern analysis
            await AnalyzeTimeBasedPatterns(analysis, dataset);

            // 5. Mathematical model fitting
            await FitMathematicalModels(analysis, dataset);

            // 6. Formula hypothesis generation
            await GenerateFormulaHypotheses(analysis, dataset);

            // 7. Validation testing
            await ValidateFormulaAccuracy(analysis, dataset);

            _logger.LogInformation("✅ Circuit limit formula analysis completed with {confidence}% confidence",
                analysis.FormulaConfidence * 100);

            return analysis;
        }

        /// <summary>
        /// Collect comprehensive dataset for analysis
        /// </summary>
        private async Task<List<CircuitLimitDataPoint>> CollectAnalysisDataset(
            string underlyingSymbol, DateTime startDate, DateTime endDate)
        {
            var dataset = new List<CircuitLimitDataPoint>();

            // Get circuit limit changes with market context
            var trackers = await _context.CircuitLimitTrackers
                .Where(t => t.UnderlyingSymbol.Contains(underlyingSymbol) &&
                           t.DetectedAt >= startDate && t.DetectedAt <= endDate)
                .OrderBy(t => t.DetectedAt)
                .ToListAsync();

            // Get intraday snapshots for additional context
            var snapshots = await _context.IntradayOptionSnapshots
                .Where(s => s.UnderlyingSymbol.Contains(underlyingSymbol) &&
                           s.Timestamp >= startDate && s.Timestamp <= endDate)
                .OrderBy(s => s.Timestamp)
                .ToListAsync();

            // Merge and enrich data
            foreach (var tracker in trackers)
            {
                var correspondingSnapshot = snapshots
                    .FirstOrDefault(s => s.Symbol == tracker.Symbol &&
                                   Math.Abs((s.Timestamp - tracker.DetectedAt).TotalMinutes) < 5);

                var dataPoint = new CircuitLimitDataPoint
                {
                    Timestamp = tracker.DetectedAt,
                    Symbol = tracker.Symbol,
                    StrikePrice = tracker.StrikePrice,
                    OptionType = tracker.OptionType,
                    ExpiryDate = tracker.ExpiryDate,
                    UnderlyingPrice = tracker.UnderlyingPrice,
                    OptionPrice = tracker.CurrentPrice,
                    LowerCircuitLimit = tracker.NewLowerLimit,
                    UpperCircuitLimit = tracker.NewUpperLimit,
                    Volume = tracker.Volume,
                    OpenInterest = tracker.OpenInterest,
                    
                    // Calculate derived metrics
                    Moneyness = tracker.UnderlyingPrice > 0 ? tracker.StrikePrice / tracker.UnderlyingPrice : 0,
                    TimeToExpiry = (tracker.ExpiryDate - tracker.DetectedAt.Date).TotalDays,
                    CircuitRangePercent = tracker.CurrentPrice > 0 ? 
                        ((tracker.NewUpperLimit - tracker.NewLowerLimit) / tracker.CurrentPrice) * 100 : 0,
                    
                    // Add implied volatility if available
                    ImpliedVolatility = correspondingSnapshot?.ImpliedVolatility ?? 0,
                    
                    // Market conditions
                    IsITM = tracker.OptionType == "CE" ? 
                        tracker.UnderlyingPrice > tracker.StrikePrice : 
                        tracker.UnderlyingPrice < tracker.StrikePrice,
                    
                    // Calculate Delta approximation (simplified Black-Scholes)
                    DeltaApprox = CalculateApproximateDelta(tracker)
                };

                dataset.Add(dataPoint);
            }

            return dataset;
        }

        /// <summary>
        /// Analyze correlation with known factors
        /// </summary>
        private async Task AnalyzeCorrelationFactors(CircuitLimitFormulaAnalysis analysis, 
            List<CircuitLimitDataPoint> dataset)
        {
            analysis.CorrelationFactors = new Dictionary<string, double>();

            if (dataset.Count < 10) return;

            // Calculate correlations with various factors
            var circuitRanges = dataset.Select(d => (double)d.CircuitRangePercent).ToList();

            // Correlation with implied volatility
            var ivValues = dataset.Where(d => d.ImpliedVolatility > 0)
                                 .Select(d => (double)d.ImpliedVolatility).ToList();
            if (ivValues.Count > 10)
            {
                var ivRanges = dataset.Where(d => d.ImpliedVolatility > 0)
                                    .Select(d => (double)d.CircuitRangePercent).ToList();
                analysis.CorrelationFactors["ImpliedVolatility"] = CalculateCorrelation(ivValues, ivRanges);
            }

            // Correlation with time to expiry
            var tteValues = dataset.Select(d => d.TimeToExpiry).ToList();
            analysis.CorrelationFactors["TimeToExpiry"] = CalculateCorrelation(tteValues, circuitRanges);

            // Correlation with moneyness
            var moneynessValues = dataset.Where(d => d.Moneyness > 0)
                                        .Select(d => (double)d.Moneyness).ToList();
            if (moneynessValues.Count > 10)
            {
                var moneynessRanges = dataset.Where(d => d.Moneyness > 0)
                                            .Select(d => (double)d.CircuitRangePercent).ToList();
                analysis.CorrelationFactors["Moneyness"] = CalculateCorrelation(moneynessValues, moneynessRanges);
            }

            // Correlation with Delta
            var deltaValues = dataset.Where(d => d.DeltaApprox > 0)
                                    .Select(d => (double)d.DeltaApprox).ToList();
            if (deltaValues.Count > 10)
            {
                var deltaRanges = dataset.Where(d => d.DeltaApprox > 0)
                                        .Select(d => (double)d.CircuitRangePercent).ToList();
                analysis.CorrelationFactors["Delta"] = CalculateCorrelation(deltaValues, deltaRanges);
            }

            // Correlation with option price
            var priceValues = dataset.Where(d => d.OptionPrice > 0)
                                    .Select(d => (double)d.OptionPrice).ToList();
            if (priceValues.Count > 10)
            {
                var priceRanges = dataset.Where(d => d.OptionPrice > 0)
                                        .Select(d => (double)d.CircuitRangePercent).ToList();
                analysis.CorrelationFactors["OptionPrice"] = CalculateCorrelation(priceValues, priceRanges);
            }
        }

        /// <summary>
        /// Analyze patterns based on option characteristics
        /// </summary>
        private async Task AnalyzeOptionCharacteristicPatterns(CircuitLimitFormulaAnalysis analysis,
            List<CircuitLimitDataPoint> dataset)
        {
            analysis.CharacteristicPatterns = new Dictionary<string, object>();

            // ITM vs OTM patterns
            var itmData = dataset.Where(d => d.IsITM).ToList();
            var otmData = dataset.Where(d => !d.IsITM).ToList();

            if (itmData.Count > 5 && otmData.Count > 5)
            {
                analysis.CharacteristicPatterns["ITM_AvgCircuitRange"] = itmData.Average(d => (double)d.CircuitRangePercent);
                analysis.CharacteristicPatterns["OTM_AvgCircuitRange"] = otmData.Average(d => (double)d.CircuitRangePercent);
                analysis.CharacteristicPatterns["ITM_vs_OTM_Ratio"] = 
                    (double)analysis.CharacteristicPatterns["ITM_AvgCircuitRange"] / 
                    (double)analysis.CharacteristicPatterns["OTM_AvgCircuitRange"];
            }

            // CE vs PE patterns
            var ceData = dataset.Where(d => d.OptionType == "CE").ToList();
            var peData = dataset.Where(d => d.OptionType == "PE").ToList();

            if (ceData.Count > 5 && peData.Count > 5)
            {
                analysis.CharacteristicPatterns["CE_AvgCircuitRange"] = ceData.Average(d => (double)d.CircuitRangePercent);
                analysis.CharacteristicPatterns["PE_AvgCircuitRange"] = peData.Average(d => (double)d.CircuitRangePercent);
            }

            // Time to expiry buckets
            var weeklyData = dataset.Where(d => d.TimeToExpiry <= 7).ToList();
            var monthlyData = dataset.Where(d => d.TimeToExpiry > 7 && d.TimeToExpiry <= 30).ToList();
            var quarterlyData = dataset.Where(d => d.TimeToExpiry > 30).ToList();

            if (weeklyData.Count > 5)
                analysis.CharacteristicPatterns["Weekly_AvgCircuitRange"] = weeklyData.Average(d => (double)d.CircuitRangePercent);
            if (monthlyData.Count > 5)
                analysis.CharacteristicPatterns["Monthly_AvgCircuitRange"] = monthlyData.Average(d => (double)d.CircuitRangePercent);
            if (quarterlyData.Count > 5)
                analysis.CharacteristicPatterns["Quarterly_AvgCircuitRange"] = quarterlyData.Average(d => (double)d.CircuitRangePercent);
        }

        /// <summary>
        /// Analyze time-based patterns
        /// </summary>
        private async Task AnalyzeTimeBasedPatterns(CircuitLimitFormulaAnalysis analysis,
            List<CircuitLimitDataPoint> dataset)
        {
            analysis.TimePatterns = new Dictionary<string, object>();

            // Market open vs close patterns
            var morningData = dataset.Where(d => d.Timestamp.Hour >= 9 && d.Timestamp.Hour <= 12).ToList();
            var afternoonData = dataset.Where(d => d.Timestamp.Hour > 12 && d.Timestamp.Hour <= 15).ToList();

            if (morningData.Count > 5)
                analysis.TimePatterns["Morning_AvgCircuitRange"] = morningData.Average(d => (double)d.CircuitRangePercent);
            if (afternoonData.Count > 5)
                analysis.TimePatterns["Afternoon_AvgCircuitRange"] = afternoonData.Average(d => (double)d.CircuitRangePercent);

            // Volatility clustering analysis
            var dailyVariance = dataset.GroupBy(d => d.Timestamp.Date)
                                     .Where(g => g.Count() > 3)
                                     .Select(g => new {
                                         Date = g.Key,
                                         Variance = g.Select(x => (double)x.CircuitRangePercent).Variance()
                                     }).ToList();

            if (dailyVariance.Count > 5)
            {
                analysis.TimePatterns["DailyVariancePattern"] = dailyVariance.Average(d => d.Variance);
            }
        }

        /// <summary>
        /// Fit mathematical models to the data
        /// </summary>
        private async Task FitMathematicalModels(CircuitLimitFormulaAnalysis analysis,
            List<CircuitLimitDataPoint> dataset)
        {
            analysis.MathematicalModels = new Dictionary<string, object>();

            // Simple linear regression: CircuitRange = α + β₁*IV + β₂*TimeToExpiry + β₃*Moneyness
            var validData = dataset.Where(d => d.ImpliedVolatility > 0 && d.Moneyness > 0 && d.TimeToExpiry > 0).ToList();

            if (validData.Count > 20)
            {
                // Multiple regression analysis would go here
                // For now, calculate simple relationships
                
                analysis.MathematicalModels["BaseCircuitRange"] = validData.Average(d => (double)d.CircuitRangePercent);
                analysis.MathematicalModels["VolatilityMultiplier"] = 
                    validData.Where(d => d.ImpliedVolatility > 0).Average(d => (double)d.CircuitRangePercent / (double)d.ImpliedVolatility);
            }
        }

        /// <summary>
        /// Generate formula hypotheses based on analysis
        /// </summary>
        private async Task GenerateFormulaHypotheses(CircuitLimitFormulaAnalysis analysis,
            List<CircuitLimitDataPoint> dataset)
        {
            analysis.FormulaHypotheses = new List<string>();

            // Based on common option pricing theory and our correlations
            if (analysis.CorrelationFactors.ContainsKey("ImpliedVolatility"))
            {
                var ivCorr = analysis.CorrelationFactors["ImpliedVolatility"];
                if (Math.Abs(ivCorr) > 0.5)
                {
                    analysis.FormulaHypotheses.Add($"Circuit Range strongly correlates with IV (r={ivCorr:F3})");
                    analysis.FormulaHypotheses.Add("Hypothesis 1: CircuitRange = BaseRange * (1 + k₁ * ImpliedVolatility)");
                }
            }

            if (analysis.CorrelationFactors.ContainsKey("TimeToExpiry"))
            {
                var tteCorr = analysis.CorrelationFactors["TimeToExpiry"];
                if (Math.Abs(tteCorr) > 0.3)
                {
                    analysis.FormulaHypotheses.Add($"Circuit Range correlates with Time to Expiry (r={tteCorr:F3})");
                    analysis.FormulaHypotheses.Add("Hypothesis 2: CircuitRange = BaseRange * (1 + k₂ * sqrt(TimeToExpiry))");
                }
            }

            // Delta-based hypothesis
            if (analysis.CorrelationFactors.ContainsKey("Delta"))
            {
                var deltaCorr = analysis.CorrelationFactors["Delta"];
                analysis.FormulaHypotheses.Add($"Circuit Range vs Delta correlation: {deltaCorr:F3}");
                analysis.FormulaHypotheses.Add("Hypothesis 3: CircuitRange = BaseRange * (1 + k₃ * |Delta|)");
            }

            // Combined formula hypothesis
            analysis.FormulaHypotheses.Add("Combined Hypothesis: CircuitRange = BaseRange * VolatilityFactor * TimeFactor * DeltaFactor");
        }

        /// <summary>
        /// Validate formula accuracy
        /// </summary>
        private async Task ValidateFormulaAccuracy(CircuitLimitFormulaAnalysis analysis,
            List<CircuitLimitDataPoint> dataset)
        {
            // Split data into training and validation sets
            var trainData = dataset.Take(dataset.Count * 3 / 4).ToList();
            var validData = dataset.Skip(dataset.Count * 3 / 4).ToList();

            if (validData.Count < 10)
            {
                analysis.FormulaConfidence = 0.5;
                return;
            }

            // Test predictive accuracy using simple model
            var baseRange = trainData.Average(d => (double)d.CircuitRangePercent);
            var predictions = new List<double>();
            var actuals = new List<double>();

            foreach (var point in validData)
            {
                // Simple prediction model based on IV
                var predictedRange = baseRange;
                if (point.ImpliedVolatility > 0 && analysis.CorrelationFactors.ContainsKey("ImpliedVolatility"))
                {
                    var ivMultiplier = 1 + (analysis.CorrelationFactors["ImpliedVolatility"] * (double)point.ImpliedVolatility / 100);
                    predictedRange *= ivMultiplier;
                }

                predictions.Add(predictedRange);
                actuals.Add((double)point.CircuitRangePercent);
            }

            // Calculate R-squared
            var meanActual = actuals.Average();
            var totalSumSquares = actuals.Sum(a => Math.Pow(a - meanActual, 2));
            var residualSumSquares = predictions.Zip(actuals, (p, a) => Math.Pow(a - p, 2)).Sum();
            
            analysis.FormulaConfidence = Math.Max(0, 1 - (residualSumSquares / totalSumSquares));
        }

        /// <summary>
        /// Calculate approximate Delta for options
        /// </summary>
        private decimal CalculateApproximateDelta(CircuitLimitTracker tracker)
        {
            if (tracker.UnderlyingPrice <= 0 || tracker.StrikePrice <= 0) return 0;

            // Simplified Delta approximation
            var moneyness = tracker.StrikePrice / tracker.UnderlyingPrice;
            var timeToExpiry = Math.Max(0.01, (tracker.ExpiryDate - tracker.DetectedAt.Date).TotalDays / 365.0);

            // Very simplified Black-Scholes Delta approximation
            if (tracker.OptionType == "CE")
            {
                return (decimal)(0.5 + 0.3 * Math.Log((double)moneyness) / Math.Sqrt(timeToExpiry));
            }
            else
            {
                return (decimal)(0.5 - 0.3 * Math.Log((double)moneyness) / Math.Sqrt(timeToExpiry));
            }
        }

        /// <summary>
        /// Calculate correlation coefficient
        /// </summary>
        private double CalculateCorrelation(List<double> x, List<double> y)
        {
            if (x.Count != y.Count || x.Count < 2) return 0;

            var meanX = x.Average();
            var meanY = y.Average();
            
            var numerator = x.Zip(y, (xi, yi) => (xi - meanX) * (yi - meanY)).Sum();
            var denomX = Math.Sqrt(x.Sum(xi => Math.Pow(xi - meanX, 2)));
            var denomY = Math.Sqrt(y.Sum(yi => Math.Pow(yi - meanY, 2)));

            return denomX * denomY == 0 ? 0 : numerator / (denomX * denomY);
        }
    }

    /// <summary>
    /// Circuit limit formula analysis result
    /// </summary>
    public class CircuitLimitFormulaAnalysis
    {
        public string UnderlyingSymbol { get; set; } = string.Empty;
        public DateTime AnalysisDate { get; set; }
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public int TotalDataPoints { get; set; }
        public double FormulaConfidence { get; set; }
        
        public Dictionary<string, double> CorrelationFactors { get; set; } = new();
        public Dictionary<string, object> CharacteristicPatterns { get; set; } = new();
        public Dictionary<string, object> TimePatterns { get; set; } = new();
        public Dictionary<string, object> MathematicalModels { get; set; } = new();
        public List<string> FormulaHypotheses { get; set; } = new();
    }

    /// <summary>
    /// Data point for circuit limit analysis
    /// </summary>
    public class CircuitLimitDataPoint
    {
        public DateTime Timestamp { get; set; }
        public string Symbol { get; set; } = string.Empty;
        public decimal StrikePrice { get; set; }
        public string OptionType { get; set; } = string.Empty;
        public DateTime ExpiryDate { get; set; }
        public decimal UnderlyingPrice { get; set; }
        public decimal OptionPrice { get; set; }
        public decimal LowerCircuitLimit { get; set; }
        public decimal UpperCircuitLimit { get; set; }
        public long Volume { get; set; }
        public long OpenInterest { get; set; }
        public decimal Moneyness { get; set; }
        public double TimeToExpiry { get; set; }
        public decimal CircuitRangePercent { get; set; }
        public decimal ImpliedVolatility { get; set; }
        public bool IsITM { get; set; }
        public decimal DeltaApprox { get; set; }
    }
}

// Extension method for variance calculation
public static class Extensions
{
    public static double Variance(this IEnumerable<double> values)
    {
        var enumerable = values as double[] ?? values.ToArray();
        var mean = enumerable.Average();
        return enumerable.Average(v => Math.Pow(v - mean, 2));
    }
}