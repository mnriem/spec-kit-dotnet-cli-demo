using NodaTime;

namespace TimezoneUtility.Models;

/// <summary>
/// A user-defined named collection of locations for quick access.
/// </summary>
public sealed record Profile
{
    /// <summary>Unique identifier for the profile</summary>
    public required string Name { get; init; }

    /// <summary>Display description (optional)</summary>
    public string? Description { get; init; }

    /// <summary>Ordered list of locations in this profile</summary>
    public required IReadOnlyList<ProfileLocation> Locations { get; init; }

    /// <summary>When the profile was created</summary>
    public Instant CreatedAt { get; init; }

    /// <summary>When the profile was last modified</summary>
    public Instant? ModifiedAt { get; init; }
}
