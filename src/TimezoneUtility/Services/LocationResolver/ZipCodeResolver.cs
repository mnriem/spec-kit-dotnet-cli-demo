using NodaTime;
using TimezoneUtility.Models;

namespace TimezoneUtility.Services.LocationResolver;

/// <summary>
/// Resolver for US ZIP code lookups.
/// </summary>
public sealed class ZipCodeResolver : ILocationResolver
{
    private readonly IDateTimeZoneProvider _tzdb;
    private readonly Dictionary<string, ZipCodeData> _zipCodes;

    /// <summary>
    /// Creates a new ZipCodeResolver.
    /// </summary>
    public ZipCodeResolver()
    {
        _tzdb = DateTimeZoneProviders.Tzdb;
        _zipCodes = InitializeZipCodeData();
    }

    /// <inheritdoc />
    public bool CanHandle(string query)
    {
        // US ZIP codes are 5 digits
        return !string.IsNullOrWhiteSpace(query) &&
               query.Trim().Length == 5 &&
               query.Trim().All(char.IsDigit);
    }

    /// <inheritdoc />
    public ResolvedLocation? Resolve(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return null;
        }

        var normalizedQuery = query.Trim();

        if (!CanHandle(normalizedQuery))
        {
            return null;
        }

        if (_zipCodes.TryGetValue(normalizedQuery, out var zipData))
        {
            return CreateResolvedLocation(zipData, query);
        }

        // Try to infer timezone from ZIP code prefix
        var prefix = normalizedQuery[..3];
        var inferredTimezone = InferTimezoneFromPrefix(prefix);
        if (inferredTimezone != null)
        {
            var inferredData = new ZipCodeData($"ZIP {normalizedQuery}", inferredTimezone, "US");
            return CreateResolvedLocation(inferredData, query);
        }

        return null;
    }

    private ResolvedLocation? CreateResolvedLocation(ZipCodeData zipData, string query)
    {
        var zone = _tzdb.GetZoneOrNull(zipData.TimezoneId);
        if (zone == null)
        {
            return null;
        }

        var now = SystemClock.Instance.GetCurrentInstant();
        var offset = zone.GetUtcOffset(now);

        var location = new Location
        {
            DisplayName = zipData.DisplayName,
            TimezoneId = zipData.TimezoneId,
            CurrentOffset = offset,
            CountryCode = zipData.CountryCode,
            OriginalQuery = query
        };

        return new ResolvedLocation
        {
            Location = location,
            TimeZone = zone,
            IsExactMatch = true
        };
    }

    private static string? InferTimezoneFromPrefix(string prefix)
    {
        // US ZIP code timezone inference based on prefix ranges
        // This is a simplified mapping - real implementation would be more comprehensive
        if (!int.TryParse(prefix, out var prefixNum))
        {
            return null;
        }

        return prefixNum switch
        {
            // Eastern
            >= 10 and <= 99 => "America/New_York",    // MA, CT, etc.
            >= 100 and <= 149 => "America/New_York",  // NY
            >= 150 and <= 196 => "America/New_York",  // PA, NJ
            >= 197 and <= 199 => "America/New_York",  // DE
            >= 200 and <= 219 => "America/New_York",  // DC, VA
            >= 220 and <= 246 => "America/New_York",  // VA, WV
            >= 270 and <= 289 => "America/New_York",  // NC
            >= 290 and <= 299 => "America/New_York",  // SC
            >= 300 and <= 319 => "America/New_York",  // GA
            >= 320 and <= 339 => "America/New_York",  // FL (most)
            >= 430 and <= 459 => "America/New_York",  // OH

            // Central
            >= 370 and <= 385 => "America/Chicago",   // TN
            >= 460 and <= 479 => "America/Chicago",   // IN
            >= 480 and <= 499 => "America/Chicago",   // MI
            >= 500 and <= 528 => "America/Chicago",   // IA
            >= 530 and <= 549 => "America/Chicago",   // WI
            >= 550 and <= 567 => "America/Chicago",   // MN
            >= 600 and <= 629 => "America/Chicago",   // IL
            >= 630 and <= 658 => "America/Chicago",   // MO
            >= 660 and <= 679 => "America/Chicago",   // KS
            >= 680 and <= 693 => "America/Chicago",   // NE
            >= 700 and <= 714 => "America/Chicago",   // LA
            >= 716 and <= 729 => "America/Chicago",   // AR
            >= 730 and <= 749 => "America/Chicago",   // OK
            >= 750 and <= 799 => "America/Chicago",   // TX (most)

            // Mountain
            >= 570 and <= 577 => "America/Denver",    // SD
            >= 580 and <= 588 => "America/Denver",    // ND
            >= 590 and <= 599 => "America/Denver",    // MT
            >= 800 and <= 816 => "America/Denver",    // CO
            >= 820 and <= 831 => "America/Denver",    // WY
            >= 832 and <= 838 => "America/Denver",    // ID
            >= 840 and <= 847 => "America/Denver",    // UT
            >= 850 and <= 865 => "America/Phoenix",   // AZ (no DST)
            >= 870 and <= 884 => "America/Denver",    // NM

            // Pacific
            >= 889 and <= 898 => "America/Los_Angeles", // NV
            >= 900 and <= 966 => "America/Los_Angeles", // CA
            >= 970 and <= 979 => "America/Los_Angeles", // OR
            >= 980 and <= 994 => "America/Los_Angeles", // WA

            // Alaska
            >= 995 and <= 999 => "America/Anchorage",

            // Hawaii
            >= 967 and <= 968 => "Pacific/Honolulu",

            _ => null
        };
    }

    private static Dictionary<string, ZipCodeData> InitializeZipCodeData()
    {
        // Common ZIP codes - in a real implementation, this would be loaded from embedded resource
        return new Dictionary<string, ZipCodeData>
        {
            ["10001"] = new("New York, NY", "America/New_York", "US"),
            ["10010"] = new("New York, NY", "America/New_York", "US"),
            ["10019"] = new("New York, NY", "America/New_York", "US"),
            ["90210"] = new("Beverly Hills, CA", "America/Los_Angeles", "US"),
            ["90001"] = new("Los Angeles, CA", "America/Los_Angeles", "US"),
            ["60601"] = new("Chicago, IL", "America/Chicago", "US"),
            ["77001"] = new("Houston, TX", "America/Chicago", "US"),
            ["85001"] = new("Phoenix, AZ", "America/Phoenix", "US"),
            ["19101"] = new("Philadelphia, PA", "America/New_York", "US"),
            ["78201"] = new("San Antonio, TX", "America/Chicago", "US"),
            ["92101"] = new("San Diego, CA", "America/Los_Angeles", "US"),
            ["75201"] = new("Dallas, TX", "America/Chicago", "US"),
            ["94102"] = new("San Francisco, CA", "America/Los_Angeles", "US"),
            ["78701"] = new("Austin, TX", "America/Chicago", "US"),
            ["98101"] = new("Seattle, WA", "America/Los_Angeles", "US"),
            ["80201"] = new("Denver, CO", "America/Denver", "US"),
            ["02101"] = new("Boston, MA", "America/New_York", "US"),
            ["33101"] = new("Miami, FL", "America/New_York", "US"),
            ["30301"] = new("Atlanta, GA", "America/New_York", "US"),
        };
    }

    private sealed record ZipCodeData(string DisplayName, string TimezoneId, string CountryCode);
}
