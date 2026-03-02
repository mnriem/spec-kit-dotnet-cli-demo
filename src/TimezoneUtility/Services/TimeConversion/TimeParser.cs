using System.Globalization;
using System.Text.RegularExpressions;
using NodaTime;

namespace TimezoneUtility.Services.TimeConversion;

/// <summary>
/// Parser for various time input formats.
/// </summary>
public static partial class TimeParser
{
    /// <summary>
    /// Parses a time string into a LocalTime.
    /// Supports formats: "3pm", "3:00pm", "15:00", "3:00 PM", etc.
    /// </summary>
    /// <param name="input">The time string to parse</param>
    /// <returns>The parsed LocalTime, or null if parsing failed</returns>
    public static LocalTime? ParseTime(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return null;
        }

        input = input.Trim().ToLowerInvariant();

        // Try ISO format first (HH:mm or HH:mm:ss)
        var isoMatch = IsoTimeRegex().Match(input);
        if (isoMatch.Success)
        {
            var hour = int.Parse(isoMatch.Groups[1].Value, CultureInfo.InvariantCulture);
            var minute = int.Parse(isoMatch.Groups[2].Value, CultureInfo.InvariantCulture);
            var second = isoMatch.Groups[3].Success ? int.Parse(isoMatch.Groups[3].Value, CultureInfo.InvariantCulture) : 0;

            if (hour >= 0 && hour <= 23 && minute >= 0 && minute <= 59 && second >= 0 && second <= 59)
            {
                return new LocalTime(hour, minute, second);
            }
        }

        // Try 12-hour format (e.g., "3pm", "3:00pm", "3:00 pm")
        var amPmMatch = AmPmTimeRegex().Match(input);
        if (amPmMatch.Success)
        {
            var hour = int.Parse(amPmMatch.Groups[1].Value, CultureInfo.InvariantCulture);
            var minute = amPmMatch.Groups[2].Success ? int.Parse(amPmMatch.Groups[2].Value, CultureInfo.InvariantCulture) : 0;
            var isPm = amPmMatch.Groups[3].Value == "pm";

            if (hour >= 1 && hour <= 12 && minute >= 0 && minute <= 59)
            {
                if (isPm && hour != 12)
                {
                    hour += 12;
                }
                else if (!isPm && hour == 12)
                {
                    hour = 0;
                }

                return new LocalTime(hour, minute);
            }
        }

        return null;
    }

    /// <summary>
    /// Parses a date string into a LocalDate.
    /// Supports formats: "2026-03-02", "Mar 2", "March 2, 2026", etc.
    /// </summary>
    /// <param name="input">The date string to parse</param>
    /// <param name="referenceDate">Reference date for relative parsing</param>
    /// <returns>The parsed LocalDate, or null if parsing failed</returns>
    public static LocalDate? ParseDate(string input, LocalDate? referenceDate = null)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return null;
        }

        input = input.Trim();

        // Special keywords
        var today = referenceDate ?? LocalDate.FromDateTime(DateTime.Today);
        if (input.Equals("today", StringComparison.OrdinalIgnoreCase))
        {
            return today;
        }

        if (input.Equals("tomorrow", StringComparison.OrdinalIgnoreCase))
        {
            return today.PlusDays(1);
        }

        // Try ISO format (YYYY-MM-DD)
        var isoMatch = IsoDateRegex().Match(input);
        if (isoMatch.Success)
        {
            var year = int.Parse(isoMatch.Groups[1].Value, CultureInfo.InvariantCulture);
            var month = int.Parse(isoMatch.Groups[2].Value, CultureInfo.InvariantCulture);
            var day = int.Parse(isoMatch.Groups[3].Value, CultureInfo.InvariantCulture);

            try
            {
                return new LocalDate(year, month, day);
            }
            catch
            {
                return null;
            }
        }

        return null;
    }

    /// <summary>
    /// Parses a duration string into a NodaTime Duration.
    /// Supports formats: "30m", "1h", "90m", "1h30m", "1.5h"
    /// </summary>
    /// <param name="input">The duration string to parse</param>
    /// <returns>The parsed Duration, or null if parsing failed</returns>
    public static Duration? ParseDuration(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return null;
        }

        input = input.Trim().ToLowerInvariant();

        // Try combined format (1h30m)
        var combinedMatch = CombinedDurationRegex().Match(input);
        if (combinedMatch.Success)
        {
            var hours = int.Parse(combinedMatch.Groups[1].Value, CultureInfo.InvariantCulture);
            var minutes = combinedMatch.Groups[2].Success ? int.Parse(combinedMatch.Groups[2].Value, CultureInfo.InvariantCulture) : 0;
            return Duration.FromMinutes(hours * 60 + minutes);
        }

        // Try simple hour format (1h, 1.5h)
        var hourMatch = HourDurationRegex().Match(input);
        if (hourMatch.Success)
        {
            var hours = double.Parse(hourMatch.Groups[1].Value, CultureInfo.InvariantCulture);
            return Duration.FromMinutes((int)(hours * 60));
        }

        // Try simple minute format (30m, 90m)
        var minuteMatch = MinuteDurationRegex().Match(input);
        if (minuteMatch.Success)
        {
            var minutes = int.Parse(minuteMatch.Groups[1].Value, CultureInfo.InvariantCulture);
            return Duration.FromMinutes(minutes);
        }

        return null;
    }

    [GeneratedRegex(@"^(\d{1,2}):(\d{2})(?::(\d{2}))?$")]
    private static partial Regex IsoTimeRegex();

    [GeneratedRegex(@"^(\d{1,2})(?::(\d{2}))?\s*(am|pm)$")]
    private static partial Regex AmPmTimeRegex();

    [GeneratedRegex(@"^(\d{4})-(\d{2})-(\d{2})$")]
    private static partial Regex IsoDateRegex();

    [GeneratedRegex(@"^(\d+)h(?:(\d+)m)?$")]
    private static partial Regex CombinedDurationRegex();

    [GeneratedRegex(@"^(\d+(?:\.\d+)?)h$")]
    private static partial Regex HourDurationRegex();

    [GeneratedRegex(@"^(\d+)m$")]
    private static partial Regex MinuteDurationRegex();
}
