using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace OptionAnalysisTool.Common.Services
{
    public class MarketHoursService : IMarketHoursService
    {
        private readonly ILogger<MarketHoursService> _logger;
        private static readonly TimeSpan MarketOpenTime = new(9, 15, 0);
        private static readonly TimeSpan MarketCloseTime = new(15, 30, 0);
        private static readonly HashSet<DateTime> TradingHolidays = new();

        public MarketHoursService(ILogger<MarketHoursService> logger)
        {
            _logger = logger;
            InitializeTradingHolidays();
        }

        private void InitializeTradingHolidays()
        {
            // 2024 NSE Trading Holidays
            TradingHolidays.Add(new DateTime(2024, 1, 26));  // Republic Day
            TradingHolidays.Add(new DateTime(2024, 3, 8));   // Maha Shivaratri
            TradingHolidays.Add(new DateTime(2024, 3, 25));  // Holi
            TradingHolidays.Add(new DateTime(2024, 3, 29));  // Good Friday
            TradingHolidays.Add(new DateTime(2024, 4, 11));  // Eid-Ul-Fitr
            TradingHolidays.Add(new DateTime(2024, 4, 17));  // Ram Navami
            TradingHolidays.Add(new DateTime(2024, 5, 1));   // Maharashtra Day
            TradingHolidays.Add(new DateTime(2024, 8, 15));  // Independence Day
            TradingHolidays.Add(new DateTime(2024, 10, 2));  // Gandhi Jayanti
            TradingHolidays.Add(new DateTime(2024, 11, 1));  // Diwali-Laxmi Pujan
            TradingHolidays.Add(new DateTime(2024, 11, 15)); // Gurunanak Jayanti
            TradingHolidays.Add(new DateTime(2024, 12, 25)); // Christmas
        }

        public bool IsMarketOpen()
        {
            var now = DateTime.Now;
            return IsTradingDay(now) && IsWithinTradingHours(now);
        }

        public bool IsTradingDay(DateTime date)
        {
            return date.DayOfWeek != DayOfWeek.Saturday && 
                   date.DayOfWeek != DayOfWeek.Sunday && 
                   !TradingHolidays.Contains(date.Date);
        }

        public bool IsWithinTradingHours(DateTime time)
        {
            var currentTime = time.TimeOfDay;
            return currentTime >= MarketOpenTime && currentTime <= MarketCloseTime;
        }

        public DateTime GetNextTradingDay(DateTime fromDate)
        {
            var nextDay = fromDate.AddDays(1);
            while (!IsTradingDay(nextDay))
            {
                nextDay = nextDay.AddDays(1);
            }
            return nextDay;
        }

        public async Task WaitForNextTradingSession()
        {
            var now = DateTime.Now;
            DateTime nextSession;

            if (!IsTradingDay(now))
            {
                // Wait for next trading day
                nextSession = GetNextTradingDay(now).Date + MarketOpenTime;
            }
            else if (now.TimeOfDay < MarketOpenTime)
            {
                // Wait for market open today
                nextSession = now.Date + MarketOpenTime;
            }
            else
            {
                // Wait for next day's market open
                nextSession = GetNextTradingDay(now).Date + MarketOpenTime;
            }

            var delay = nextSession - now;
            _logger.LogInformation($"Waiting {delay.TotalHours:F1} hours for next trading session at {nextSession}");
            await Task.Delay(delay);
        }

        public bool IsEndOfDay()
        {
            var now = DateTime.Now;
            return IsTradingDay(now) && now.TimeOfDay >= MarketCloseTime;
        }

        public TimeSpan GetTimeToMarketOpen()
        {
            var now = DateTime.Now;
            if (IsMarketOpen()) return TimeSpan.Zero;

            DateTime nextOpen;
            if (!IsTradingDay(now))
            {
                nextOpen = GetNextTradingDay(now).Date + MarketOpenTime;
            }
            else if (now.TimeOfDay < MarketOpenTime)
            {
                nextOpen = now.Date + MarketOpenTime;
            }
            else
            {
                nextOpen = GetNextTradingDay(now).Date + MarketOpenTime;
            }

            return nextOpen - now;
        }

        public TimeSpan GetTimeToMarketClose()
        {
            var now = DateTime.Now;
            if (!IsMarketOpen()) return TimeSpan.Zero;

            var closeTime = now.Date + MarketCloseTime;
            return closeTime - now;
        }
    }
} 