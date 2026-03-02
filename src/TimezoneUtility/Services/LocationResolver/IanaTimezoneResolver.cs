using NodaTime;
using TimezoneUtility.Models;

namespace TimezoneUtility.Services.LocationResolver;

/// <summary>
/// Resolver for direct IANA timezone ID lookups.
/// </summary>
public sealed class IanaTimezoneResolver : ILocationResolver
{
    private readonly IDateTimeZoneProvider _tzdb;

    /// <summary>
    /// Creates a new IanaTimezoneResolver.
    /// </summary>
    public IanaTimezoneResolver()
    {
        _tzdb = DateTimeZoneProviders.Tzdb;
    }

    /// <inheritdoc />
    public bool CanHandle(string query)
    {
        // IANA IDs contain a "/" (e.g., "America/New_York")
        return !string.IsNullOrWhiteSpace(query) && query.Contains('/');
    }

    /// <inheritdoc />
    public ResolvedLocation? Resolve(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return null;
        }

        var zone = _tzdb.GetZoneOrNull(query);
        if (zone == null)
        {
            return null;
        }

        var now = SystemClock.Instance.GetCurrentInstant();
        var offset = zone.GetUtcOffset(now);

        var location = new Location
        {
            DisplayName = FormatTimezoneDisplayName(query),
            TimezoneId = zone.Id,
            CurrentOffset = offset,
            OriginalQuery = query
        };

        return new ResolvedLocation
        {
            Location = location,
            TimeZone = zone,
            IsExactMatch = true
        };
    }

    private static string FormatTimezoneDisplayName(string timezoneId)
    {
        // Convert "America/New_York" to "New York"
        var parts = timezoneId.Split('/');
        var city = parts.Length > 1 ? parts[^1] : timezoneId;
        return city.Replace("_", " ");
    }
}
