using NodaTime;
using TimezoneUtility.Models;

namespace TimezoneUtility.Services.MeetingOptimizer;

/// <summary>
/// Service for finding optimal meeting times across timezones.
/// </summary>
public sealed class MeetingOptimizer : IMeetingOptimizer
{
    private readonly IClock _clock;

    /// <summary>
    /// Creates a new MeetingOptimizer.
    /// </summary>
    public MeetingOptimizer(IClock? clock = null)
    {
        _clock = clock ?? SystemClock.Instance;
    }

    /// <inheritdoc />
    public IReadOnlyList<MeetingSlot> FindOptimalTimes(
        IReadOnlyList<ResolvedLocation> participants,
        Duration duration,
        DateInterval dateRange,
        WorkingHours workingHours,
        int limit = 5)
    {
        var slots = new List<MeetingSlot>();
        var slotDuration = Duration.FromMinutes(30); // Check every 30 minutes

        // Iterate through each day in the range
        for (var date = dateRange.Start; date <= dateRange.End; date = date.PlusDays(1))
        {
            // Skip non-working days
            if (!workingHours.IsWorkDay(date.DayOfWeek))
            {
                continue;
            }

            // Check each 30-minute slot throughout the working hours of the "anchor" timezone
            // Use first participant as anchor for generating candidate times
            var anchorZone = participants[0].TimeZone;
            var startTime = workingHours.Start;
            var endTime = workingHours.End;

            for (var time = startTime; time < endTime; time = time.PlusMinutes(30))
            {
                var localDateTime = date + time;
                var zonedDateTime = localDateTime.InZoneLeniently(anchorZone);
                var instant = zonedDateTime.ToInstant();

                // Check if this slot works for all participants
                var participantAvailability = EvaluateSlot(instant, duration, participants, workingHours);
                var score = participantAvailability.Count(p => p.InWorkingHours);

                var slot = new MeetingSlot
                {
                    StartTime = instant,
                    Duration = duration,
                    Score = score,
                    Participants = participantAvailability
                };

                slots.Add(slot);
            }
        }

        // Sort by score (descending) then by time (ascending)
        // Return ideal slots first (all participants in working hours), then compromise slots
        return slots
            .OrderByDescending(s => s.Score)
            .ThenBy(s => s.StartTime)
            .Take(limit * 2) // Get extra to ensure we have both ideal and compromise
            .Take(limit)
            .ToList();
    }

    private static List<ParticipantAvailability> EvaluateSlot(
        Instant startTime,
        Duration duration,
        IReadOnlyList<ResolvedLocation> participants,
        WorkingHours workingHours)
    {
        var availability = new List<ParticipantAvailability>();

        foreach (var participant in participants)
        {
            var zonedStart = startTime.InZone(participant.TimeZone);
            var zonedEnd = (startTime + duration).InZone(participant.TimeZone);

            var localStartTime = zonedStart.TimeOfDay;
            var localEndTime = zonedEnd.TimeOfDay;
            var localDate = zonedStart.Date;

            // Check if the entire meeting duration falls within working hours
            var inWorkingHours = workingHours.Contains(localStartTime) &&
                                 workingHours.Contains(localEndTime.PlusMinutes(-1)) &&
                                 workingHours.IsWorkDay(zonedStart.DayOfWeek);

            availability.Add(new ParticipantAvailability
            {
                Location = participant.Location,
                LocalStartTime = localStartTime,
                LocalDate = localDate,
                InWorkingHours = inWorkingHours,
                WorkingHours = workingHours
            });
        }

        return availability;
    }
}
