using NodaTime;

namespace TimezoneUtility.Models;

/// <summary>
/// A potential meeting time slot with participant availability information.
/// </summary>
public sealed record MeetingSlot
{
    /// <summary>Start time of the meeting in UTC</summary>
    public required Instant StartTime { get; init; }

    /// <summary>Duration of the meeting</summary>
    public required Duration Duration { get; init; }

    /// <summary>End time of the meeting in UTC</summary>
    public Instant EndTime => StartTime + Duration;

    /// <summary>Score indicating how good this slot is (higher = better, all in working hours)</summary>
    public int Score { get; init; }

    /// <summary>Participant availability details</summary>
    public required IReadOnlyList<ParticipantAvailability> Participants { get; init; }

    /// <summary>Whether all participants are within working hours</summary>
    public bool IsIdeal => Participants.All(p => p.InWorkingHours);
}
