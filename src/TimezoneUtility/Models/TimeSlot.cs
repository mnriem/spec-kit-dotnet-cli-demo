using NodaTime;

namespace TimezoneUtility.Models;

/// <summary>
/// A specific datetime in a specific timezone, with ability to convert to other zones.
/// </summary>
public sealed record TimeSlot
{
    /// <summary>The underlying instant in UTC</summary>
    public required Instant Instant { get; init; }

    /// <summary>The timezone for display</summary>
    public required DateTimeZone Zone { get; init; }

    /// <summary>The zoned representation</summary>
    public ZonedDateTime ZonedDateTime => Instant.InZone(Zone);

    /// <summary>Local date and time in the zone</summary>
    public LocalDateTime LocalDateTime => ZonedDateTime.LocalDateTime;

    /// <summary>Date component only</summary>
    public LocalDate Date => LocalDateTime.Date;

    /// <summary>Time component only</summary>
    public LocalTime Time => LocalDateTime.TimeOfDay;

    /// <summary>Formatted offset (e.g., "-05:00")</summary>
    public string OffsetString => ZonedDateTime.Offset.ToString();

    /// <summary>Convert to another timezone</summary>
    public TimeSlot InZone(DateTimeZone targetZone) => this with { Zone = targetZone };

    /// <summary>Check if time falls within working hours</summary>
    public bool IsWithinWorkingHours(WorkingHours hours) => hours.Contains(Time);
}
