using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable MemberCanBePrivate.Global

namespace IctBaden.Stonehenge.Extension;

public class ChartCategoryTimeseriesAxis : ChartAxis
{
    [JsonPropertyName("type")] public string Type { get; }

    [JsonPropertyName("tick")] public Dictionary<string, object>? Tick { get; }

    /// <summary>
    /// Unix Time Milliseconds
    /// </summary>
    [JsonIgnore]
    public long[] Values { get; }


    /// <summary>
    /// Constructs time series X axis
    /// </summary>
    /// <param name="format">
    /// %a - abbreviated weekday name.
    /// %A - full weekday name.
    /// %b - abbreviated month name.
    /// %B - full month name.
    /// %c - date and time, as "%a %b %e %H:%M:%S %Y".
    /// %d - zero-padded day of the month as a decimal number [01,31].
    /// %e - space-padded day of the month as a decimal number [ 1,31].
    /// %H - hour (24-hour clock) as a decimal number [00,23].
    /// %I - hour (12-hour clock) as a decimal number [01,12].
    /// %j - day of the year as a decimal number [001,366].
    /// %m - month as a decimal number [01,12].
    /// %M - minute as a decimal number [00,59].
    /// %p - either AM or PM.
    /// %S - second as a decimal number [00,61].
    /// %U - week number of the year (Sunday as the first day of the week) as a decimal number [00,53].
    /// %w - weekday as a decimal number [0(Sunday),6].
    /// %W - week number of the year (Monday as the first day of the week) as a decimal number [00,53].
    /// %x - date, as "%m/%d/%y".
    /// %X - time, as "%H:%M:%S".
    /// %y - year without century as a decimal number [00,99].
    /// %Y - year with century as a decimal number.
    /// %Z - time zone offset, such as "-0700".
    /// %% - a literal "%" character.
    /// </param>
    /// <param name="count">The number of x axis ticks to show</param>
    /// <param name="values"></param>
    public ChartCategoryTimeseriesAxis(string format, int count, DateTime[] values)
        : base("x")
    {
        // time series, category, indexed
        Type = "timeseries";
        // explicit values (category)
        Tick = new Dictionary<string, object>
        {
            ["format"] = format,
            ["count"] = count
        };
        Values = values.Select(t => new DateTimeOffset(t).ToUnixTimeMilliseconds()).ToArray();
    }
}