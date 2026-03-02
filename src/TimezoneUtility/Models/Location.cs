using NodaTime;

namespace TimezoneUtility.Models;

/// <summary>
/// A place that can be resolved to a timezone. Can be specified by IANA timezone ID,
/// city name, country name, or US zip code.
/// </summary>
public sealed record Location
{
    /// <summary>Display name for the location (e.g., "New York, NY" or "Tokyo, Japan")</summary>
    public required string DisplayName { get; init; }

    /// <summary>IANA timezone identifier (e.g., "America/New_York")</summary>
    public required string TimezoneId { get; init; }

    /// <summary>Current UTC offset (changes with DST)</summary>
    public Offset CurrentOffset { get; init; }

    /// <summary>Country code (ISO 3166-1 alpha-2)</summary>
    public string? CountryCode { get; init; }

    /// <summary>Original query that resolved to this location</summary>
    public string? OriginalQuery { get; init; }
}
