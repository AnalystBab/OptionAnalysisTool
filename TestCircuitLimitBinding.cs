using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using OptionAnalysisTool.Models;

namespace OptionAnalysisTool.App.Test
{
    /// <summary>
    /// Simple test to verify UI binding works with dummy data
    /// </summary>
    public static class CircuitLimitBindingTest
    {
        public static ObservableCollection<CircuitLimitTracker> CreateTestData()
        {
            var testData = new ObservableCollection<CircuitLimitTracker>();
            
            // Add some dummy circuit limit data
            testData.Add(new CircuitLimitTracker
            {
                Id = 1,
                InstrumentToken = "12345",
                Symbol = "NIFTY2560524500CE",
                UnderlyingSymbol = "NIFTY",
                StrikePrice = 24500,
                OptionType = "CE",
                ExpiryDate = DateTime.Today.AddDays(30),
                PreviousLowerLimit = 0.05m,
                NewLowerLimit = 0.05m,
                PreviousUpperLimit = 200.00m,
                NewUpperLimit = 250.00m,
                CurrentPrice = 125.50m,
                Volume = 45000,
                OpenInterest = 120000,
                DetectedAt = DateTime.Now.AddMinutes(-15),
                SeverityLevel = "Medium",
                ChangeReason = "Market volatility adjustment",
                IsBreachAlert = false
            });
            
            testData.Add(new CircuitLimitTracker
            {
                Id = 2,
                InstrumentToken = "12346",
                Symbol = "NIFTY2560524500PE",
                UnderlyingSymbol = "NIFTY",
                StrikePrice = 24500,
                OptionType = "PE",
                ExpiryDate = DateTime.Today.AddDays(30),
                PreviousLowerLimit = 0.05m,
                NewLowerLimit = 0.05m,
                PreviousUpperLimit = 180.00m,
                NewUpperLimit = 220.00m,
                CurrentPrice = 95.75m,
                Volume = 32000,
                OpenInterest = 98000,
                DetectedAt = DateTime.Now.AddMinutes(-10),
                SeverityLevel = "High",
                ChangeReason = "Upper circuit limit breach",
                IsBreachAlert = true
            });
            
            testData.Add(new CircuitLimitTracker
            {
                Id = 3,
                InstrumentToken = "12347",
                Symbol = "NIFTY2560524750CE",
                UnderlyingSymbol = "NIFTY",
                StrikePrice = 24750,
                OptionType = "CE",
                ExpiryDate = DateTime.Today.AddDays(30),
                PreviousLowerLimit = 0.05m,
                NewLowerLimit = 0.05m,
                PreviousUpperLimit = 150.00m,
                NewUpperLimit = 175.00m,
                CurrentPrice = 78.25m,
                Volume = 28000,
                OpenInterest = 85000,
                DetectedAt = DateTime.Now.AddMinutes(-5),
                SeverityLevel = "Low",
                ChangeReason = "Regular adjustment",
                IsBreachAlert = false
            });
            
            return testData;
        }
        
        public static ObservableCollection<ActiveStrikeData> CreateActiveStrikesTestData()
        {
            var testData = new ObservableCollection<ActiveStrikeData>();
            
            testData.Add(new ActiveStrikeData
            {
                Symbol = "NIFTY2560524500CE",
                StrikePrice = 24500,
                OptionType = "CE",
                LastPrice = 125.50m,
                Volume = 45000,
                OpenInterest = 120000,
                LowerCircuitLimit = 0.05m,
                UpperCircuitLimit = 250.00m,
                CircuitLimitStatus = "Normal",
                CapturedAt = DateTime.Now
            });
            
            testData.Add(new ActiveStrikeData
            {
                Symbol = "NIFTY2560524500PE",
                StrikePrice = 24500,
                OptionType = "PE",
                LastPrice = 95.75m,
                Volume = 32000,
                OpenInterest = 98000,
                LowerCircuitLimit = 0.05m,
                UpperCircuitLimit = 220.00m,
                CircuitLimitStatus = "Near Upper Circuit",
                CapturedAt = DateTime.Now
            });
            
            return testData;
        }
    }
} 