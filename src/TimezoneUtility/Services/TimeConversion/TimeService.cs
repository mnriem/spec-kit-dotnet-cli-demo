using NodaTime;
using TimezoneUtility.Models;

namespace TimezoneUtility.Services.TimeConversion;

/// <summary>
/// Service for time-related operations using NodaTime.
/// </summary>
public sealed class TimeService : ITimeService
{
    private readonly IClock _clock;

    /// <summary>
    /// Creates a new TimeService with the specified clock.
    /// </summary>
    /// <param name="clock">The clock to use for current time (defaults to system clock)</param>
    public TimeService(IClock? clock = null)
    {
        _clock = clock ?? SystemClock.Instance;
    }

    /// <inheritdoc />
    public Instant Now => _clock.GetCurrentInstant();

    /// <inheritdoc />
    public TimeSlot GetCurrentTime(ResolvedLocation location)
    {
        var instant = _clock.GetCurrentInstant();
        return new TimeSlot
        {
            Instant = instant,
            Zone = location.TimeZone
        };
    }

    /// <inheritdoc />
    public TimeSlot ConvertTime(LocalDateTime sourceTime, DateTimeZone sourceZone, DateTimeZone targetZone)
    {
        // Resolve the local time in the source zone (handles DST leniently)
        var zonedSource = sourceTime.InZoneLeniently(sourceZone);
        var instant = zonedSource.ToInstant();

        return new TimeSlot
        {
            Instant = instant,
            Zone = targetZone
        };
    }

    /// <inheritdoc />
    public TimeOfDayStatus GetTimeOfDayStatus(LocalTime time, WorkingHours? workingHours = null)
    {
        var hours = workingHours ?? WorkingHours.Default;
        var eveningEnd = new LocalTime(22, 0);

        if (hours.Contains(time))
        {
            return TimeOfDayStatus.Business;
        }

        if (time >= hours.End && time < eveningEnd)
        {
            return TimeOfDayStatus.Evening;
        }

        return TimeOfDayStatus.Night;
    }
}
