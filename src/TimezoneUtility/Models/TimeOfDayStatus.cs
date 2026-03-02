namespace TimezoneUtility.Models;

/// <summary>
/// Status indicator for time of day (business hours, evening, night).
/// </summary>
public enum TimeOfDayStatus
{
    /// <summary>During business hours (default 9:00-18:00)</summary>
    Business,

    /// <summary>Evening hours (default 18:00-22:00)</summary>
    Evening,

    /// <summary>Night hours (default 22:00-9:00)</summary>
    Night
}
