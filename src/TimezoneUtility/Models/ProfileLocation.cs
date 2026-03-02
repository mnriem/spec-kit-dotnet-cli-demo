namespace TimezoneUtility.Models;

/// <summary>
/// A location entry within a profile.
/// </summary>
public sealed record ProfileLocation
{
    /// <summary>Display name override (or null to use resolved name)</summary>
    public string? DisplayName { get; init; }

    /// <summary>The query to resolve (IANA ID, city, zip, etc.)</summary>
    public required string Query { get; init; }

    /// <summary>Custom working hours for this location in this profile</summary>
    public WorkingHours? CustomWorkingHours { get; init; }
}
