using NodaTime;

namespace TimezoneUtility.Models;

/// <summary>
/// Availability information for a single participant at a specific meeting time.
/// </summary>
public sealed record ParticipantAvailability
{
    /// <summary>The participant's location</summary>
    public required Location Location { get; init; }

    /// <summary>Local time for the participant when the meeting starts</summary>
    public required LocalTime LocalStartTime { get; init; }

    /// <summary>Local date for the participant</summary>
    public required LocalDate LocalDate { get; init; }

    /// <summary>Whether the meeting time falls within the participant's working hours</summary>
    public bool InWorkingHours { get; init; }

    /// <summary>The working hours used for this participant</summary>
    public required WorkingHours WorkingHours { get; init; }
}
