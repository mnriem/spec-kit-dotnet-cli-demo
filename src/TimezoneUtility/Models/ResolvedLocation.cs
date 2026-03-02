using NodaTime;

namespace TimezoneUtility.Models;

/// <summary>
/// Result of resolving a location query to a timezone.
/// </summary>
public sealed record ResolvedLocation
{
    /// <summary>The resolved location details</summary>
    public required Location Location { get; init; }

    /// <summary>The NodaTime timezone for this location</summary>
    public required DateTimeZone TimeZone { get; init; }

    /// <summary>Whether this was an exact match or required disambiguation</summary>
    public bool IsExactMatch { get; init; } = true;

    /// <summary>Alternative locations if the query was ambiguous</summary>
    public IReadOnlyList<Location>? Alternatives { get; init; }
}
