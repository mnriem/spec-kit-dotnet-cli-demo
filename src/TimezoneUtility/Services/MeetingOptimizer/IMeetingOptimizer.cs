using NodaTime;
using TimezoneUtility.Models;

namespace TimezoneUtility.Services.MeetingOptimizer;

/// <summary>
/// Interface for finding optimal meeting times across timezones.
/// </summary>
public interface IMeetingOptimizer
{
    /// <summary>
    /// Finds optimal meeting times for a set of participants.
    /// </summary>
    /// <param name="participants">The participant locations</param>
    /// <param name="duration">The meeting duration</param>
    /// <param name="dateRange">The date range to search within</param>
    /// <param name="workingHours">Default working hours (can be overridden per participant)</param>
    /// <param name="limit">Maximum number of suggestions to return</param>
    /// <returns>List of meeting slot suggestions, ordered by score (best first)</returns>
    IReadOnlyList<MeetingSlot> FindOptimalTimes(
        IReadOnlyList<ResolvedLocation> participants,
        Duration duration,
        DateInterval dateRange,
        WorkingHours workingHours,
        int limit = 5);
}
