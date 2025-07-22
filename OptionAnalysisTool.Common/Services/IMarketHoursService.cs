using System;
using System.Threading.Tasks;

namespace OptionAnalysisTool.Common.Services
{
    public interface IMarketHoursService
    {
        bool IsMarketOpen();
        bool IsTradingDay(DateTime date);
        bool IsWithinTradingHours(DateTime time);
        DateTime GetNextTradingDay(DateTime fromDate);
        Task WaitForNextTradingSession();
        bool IsEndOfDay();
        TimeSpan GetTimeToMarketOpen();
        TimeSpan GetTimeToMarketClose();
    }
} 