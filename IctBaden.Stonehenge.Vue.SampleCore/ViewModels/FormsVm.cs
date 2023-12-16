using System;
using System.Globalization;
using System.Linq;
using IctBaden.Stonehenge.Core;
using IctBaden.Stonehenge.ViewModel;
// ReSharper disable UnusedAutoPropertyAccessor.Global

// ReSharper disable MemberCanBePrivate.Global

namespace IctBaden.Stonehenge.Vue.SampleCore.ViewModels;

public class FormsVm : ActiveViewModel
{
    public string TimeStamp => DateTime.Now.ToLongTimeString();

    public string TimeText => DateTime.Now.ToString("G");

    public string RefreshText { get; private set; } = string.Empty;

    public int RangeDays { get; private set; }
    public string RangeText { get; private set; } = string.Empty;
    public string RangeValue { get; set; } = string.Empty;
    public string[] RangeYears { get; set; }
    public string RangeStart { get; private set; } = string.Empty;
    public string RangeEnd { get; private set; } = string.Empty;

    public string Test { get; set; }

    public int CheckValue { get; set; }

    public string InputParameter { get; private set; } = string.Empty;
    public string ReceivedParameter { get; private set; } = string.Empty;

    
    public string DropEditValue1 { get; set; }
    public string DropEditValue2 { get; set; }
    public string[] DropEditValues { get; set; }

    public FormsVm(AppSession session)
        : base(session)
    {
        SetRefresh(0);
        SetRange(1);
        var year = DateTime.Now.Year;
        RangeYears = Enumerable.Range(1, 10)
            .Select(y => $"{year - y:D4}")
            .ToArray();
        
        Test = "abcd";
        CheckValue = 5;

        DropEditValue1 = "test";
        DropEditValue2 = "test-2";
        DropEditValues = new[]
        {
            "unknown",
            "test",
            "test-2",
            "test-3",
            "last"
        };
    }

    public override void OnLoad()
    {
        base.OnLoad();
        
        Test = Session.Parameters.TryGetValue("test", out var test)
            ? test
            : "0-0";

        ExecuteClientScript("");
    }

    // This presumes that weeks start with Monday.
    // Week 1 is the 1st week of the year with a Thursday in it.
    public static int GetIso8601WeekOfYear(DateTime time)
    {
        // Seriously cheat.  If its Monday, Tuesday or Wednesday, then it'll 
        // be the same week# as whatever Thursday, Friday or Saturday are,
        // and we always get those right
        DayOfWeek day = CultureInfo.InvariantCulture.Calendar.GetDayOfWeek(time);
        if (day is >= DayOfWeek.Monday and <= DayOfWeek.Wednesday)
        {
            time = time.AddDays(3);
        }

        // Return the week of our adjusted day
        return CultureInfo.InvariantCulture.Calendar
            .GetWeekOfYear(time, CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);
    }

    public static DateTime FirstDateOfWeekIso8601(int year, int weekOfYear)
    {
        DateTime jan1 = new DateTime(year, 1, 1);
        int daysOffset = DayOfWeek.Thursday - jan1.DayOfWeek;

        // Use first Thursday in January to get first week of the year as
        // it will never be in Week 52/53
        DateTime firstThursday = jan1.AddDays(daysOffset);
        var cal = CultureInfo.CurrentCulture.Calendar;
        int firstWeek = cal.GetWeekOfYear(firstThursday, CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);

        var weekNum = weekOfYear;
        // As we're adding days to a date in Week 1,
        // we need to subtract 1 in order to get the right date for week #1
        if (firstWeek == 1)
        {
            weekNum -= 1;
        }

        // Using the first Thursday as starting week ensures that we are starting in the right year
        // then we add number of weeks multiplied with days
        var result = firstThursday.AddDays(weekNum * 7);

        // Subtract 3 days from Thursday to get Monday, which is the first weekday in ISO8601
        return result.AddDays(-3);
    }

    [ActionMethod]
    public void SetRange(int days)
    {
        RangeDays = days;
        RangeText = days switch
        {
            1 => "Tag",
            7 => "Woche",
            30 => "Monat",
            365 => "Jahr",
            _ => "???"
        };

        var yesterday = DateTime.Now.Date - TimeSpan.FromDays(1);
        var lastMonth = DateTime.Now.Date - TimeSpan.FromDays(32);
        RangeValue = days switch
        {
            1 => yesterday.ToString("yyyy-MM-dd"),
            7 => $"{yesterday.Year:D4}-W{GetIso8601WeekOfYear(yesterday):D2}",
            30 => $"{lastMonth.Year:D4}-{lastMonth.Month:D2}",
            365 => $"{DateTime.Now.Year - 1:D4}",
            _ => ""
        };

        UpdateRange();
    }

    [ActionMethod]
    public void UpdateRange()
    {
        int year;
        DateTime start;
        DateTime end;
        switch (RangeDays)
        {
            case 1:
                DateTime.TryParseExact(RangeValue, new[] { "yyyy-MM-dd" }, CultureInfo.CurrentCulture,
                    DateTimeStyles.AssumeLocal, out var day);
                RangeStart = day.ToString("d");
                RangeEnd = day.ToString("d");
                break;
            case 7:
                year = int.Parse(RangeValue.Substring(0, 4));
                var week = int.Parse(RangeValue.Substring(6, 2));
                start = FirstDateOfWeekIso8601(year, week);
                end = start + TimeSpan.FromDays(6);
                RangeStart = start.ToString("d");
                RangeEnd = end.ToString("d");
                break;
            case 30:
                year = int.Parse(RangeValue.Substring(0, 4));
                var month = int.Parse(RangeValue.Substring(5, 2));
                start = new DateTime(year, month, 1);
                end = start + TimeSpan.FromDays(32);
                end = new DateTime(year, end.Month, 1) - TimeSpan.FromDays(1);
                RangeStart = start.ToString("d");
                RangeEnd = end.ToString("d");
                break;
            case 365:
                year = int.Parse(RangeValue);
                start = new DateTime(year, 1, 1);
                end = new DateTime(year, 12, 31);
                RangeStart = start.ToString("d");
                RangeEnd = end.ToString("d");
                break;
        }
    }

    [ActionMethod]
    public void Refresh()
    {
    }

    [ActionMethod]
    public void SetRefresh(int seconds)
    {
        RefreshText = seconds switch
        {
            0 => "Aus",
            1 => "1s",
            10 => "10s",
            30 => "30s",
            60 => "1min",
            300 => "5min",
            _ => RefreshText
        };

        if (seconds == 0)
            StopUpdateTimer();
        else
            SetUpdateTimer(TimeSpan.FromSeconds(seconds));
    }

    public override void OnUpdateTimer()
    {
        NotifyPropertyChanged(nameof(TimeText));
    }
    
    [ActionMethod]
    public void Save(int number, string text)
    {
        Test = number + Test + text;
    }

    [ActionMethod]
    public void CopyTest()
    {
        CopyToClipboard(Test);
    }

    [ActionMethod]
    public void ToggleBit(int bit)
    {
        CheckValue ^= 1 << bit;
    }

    [ActionMethod]
    public void InputWithParameter(string parameter)
    {
        ReceivedParameter = string.IsNullOrEmpty(parameter)
            ? "EMPTY" : parameter;
    }
    
}