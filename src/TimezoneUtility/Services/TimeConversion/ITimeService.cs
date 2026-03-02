using NodaTime;
using TimezoneUtility.Models;

namespace TimezoneUtility.Services.TimeConversion;

/// <summary>
/// Interface for time-related operations.
/// </summary>
public interface ITimeService
{
    /// <summary>
    /// Gets the current time for a location.
    /// </summary>
    /// <param name="location">The resolved location</param>
    /// <returns>A TimeSlot representing the current time in that location</returns>
    TimeSlot GetCurrentTime(ResolvedLocation location);

    /// <summary>
    /// Converts a time from one timezone to another.
    /// </summary>
    /// <param name="sourceTime">The time to convert</param>
    /// <param name="sourceZone">The source timezone</param>
    /// <param name="targetZone">The target timezone</param>
    /// <returns>The converted TimeSlot</returns>
    TimeSlot ConvertTime(LocalDateTime sourceTime, DateTimeZone sourceZone, DateTimeZone targetZone);

    /// <summary>
    /// Gets the time-of-day status for a given time.
    /// </summary>
    /// <param name="time">The local time to check</param>
    /// <param name="workingHours">The working hours to use for determining status</param>
    /// <returns>The time of day status</returns>
    TimeOfDayStatus GetTimeOfDayStatus(LocalTime time, WorkingHours? workingHours = null);

    /// <summary>
    /// Gets the current instant (UTC).
    /// </summary>
    Instant Now { get; }
}
