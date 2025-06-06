using System;
using System.ComponentModel.DataAnnotations;

namespace OptionAnalysisTool.Models
{
    public class CircuitBreaker
    {
        public int Id { get; set; }
        public required string Symbol { get; set; }
        public required string UnderlyingSymbol { get; set; }
        public decimal StrikePrice { get; set; }
        public required string OptionType { get; set; }
        public DateTime ExpiryDate { get; set; }
        public DateTime TradingDate { get; set; }
        public decimal LowerCircuitLimit { get; set; }
        public decimal UpperCircuitLimit { get; set; }
        public bool IsBreached { get; set; }
        public int BreachCount { get; set; }
        public DateTime? FirstBreachTime { get; set; }
        public DateTime? LastBreachTime { get; set; }
        public string? BreachType { get; set; }
        public string? BreachReason { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime LastUpdated { get; set; }
    }
} 