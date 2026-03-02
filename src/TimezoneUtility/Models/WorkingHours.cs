using NodaTime;

namespace TimezoneUtility.Models;

/// <summary>
/// A range of acceptable hours (start time, end time) for a location, used in meeting optimization.
/// </summary>
public sealed record WorkingHours
{
    /// <summary>Start of working hours (inclusive)</summary>
    public required LocalTime Start { get; init; }

    /// <summary>End of working hours (exclusive)</summary>
    public required LocalTime End { get; init; }

    /// <summary>Days of the week when these hours apply</summary>
    public required IReadOnlySet<IsoDayOfWeek> WorkDays { get; init; }

    /// <summary>Default working hours: 9:00 AM - 6:00 PM, Monday-Friday</summary>
    public static WorkingHours Default => new()
    {
        Start = new LocalTime(9, 0),
        End = new LocalTime(18, 0),
        WorkDays = new HashSet<IsoDayOfWeek>
        {
            IsoDayOfWeek.Monday,
            IsoDayOfWeek.Tuesday,
            IsoDayOfWeek.Wednesday,
            IsoDayOfWeek.Thursday,
            IsoDayOfWeek.Friday
        }
    };

    /// <summary>Check if a time falls within working hours</summary>
    public bool Contains(LocalTime time) => time >= Start && time < End;

    /// <summary>Check if a day is a work day</summary>
    public bool IsWorkDay(IsoDayOfWeek day) => WorkDays.Contains(day);
}
