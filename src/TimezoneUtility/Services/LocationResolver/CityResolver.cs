using NodaTime;
using TimezoneUtility.Models;

namespace TimezoneUtility.Services.LocationResolver;

/// <summary>
/// Resolver for city name lookups.
/// </summary>
public sealed class CityResolver : ILocationResolver
{
    private readonly IDateTimeZoneProvider _tzdb;
    private readonly Dictionary<string, CityData> _cities;

    /// <summary>
    /// Creates a new CityResolver.
    /// </summary>
    public CityResolver()
    {
        _tzdb = DateTimeZoneProviders.Tzdb;
        _cities = InitializeCityData();
    }

    /// <inheritdoc />
    public bool CanHandle(string query)
    {
        // City names are typically alphabetic without numbers or slashes
        return !string.IsNullOrWhiteSpace(query) &&
               !query.Contains('/') &&
               !query.All(char.IsDigit);
    }

    /// <inheritdoc />
    public ResolvedLocation? Resolve(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return null;
        }

        var normalizedQuery = query.Trim().ToLowerInvariant();

        // Try exact match first
        if (_cities.TryGetValue(normalizedQuery, out var cityData))
        {
            return CreateResolvedLocation(cityData, query);
        }

        // Try partial match
        // For query.Contains(key), require key to be at least 3 chars to avoid
        // short abbreviations like "la" matching unrelated queries like "holland"
        var matches = _cities
            .Where(kvp => kvp.Key.Contains(normalizedQuery) || 
                         (kvp.Key.Length >= 3 && normalizedQuery.Contains(kvp.Key)))
            .Select(kvp => kvp.Value)
            .ToList();

        if (matches.Count == 1)
        {
            return CreateResolvedLocation(matches[0], query);
        }

        if (matches.Count > 1)
        {
            var primary = matches[0];
            var alternatives = matches.Skip(1)
                .Select(m => new Location
                {
                    DisplayName = m.DisplayName,
                    TimezoneId = m.TimezoneId,
                    OriginalQuery = query,
                    CountryCode = m.CountryCode
                })
                .ToList();

            var resolved = CreateResolvedLocation(primary, query);
            if (resolved == null)
            {
                return null;
            }

            return resolved with
            {
                IsExactMatch = false,
                Alternatives = alternatives
            };
        }

        return null;
    }

    private ResolvedLocation? CreateResolvedLocation(CityData cityData, string query)
    {
        var zone = _tzdb.GetZoneOrNull(cityData.TimezoneId);
        if (zone == null)
        {
            return null;
        }

        var now = SystemClock.Instance.GetCurrentInstant();
        var offset = zone.GetUtcOffset(now);

        var location = new Location
        {
            DisplayName = cityData.DisplayName,
            TimezoneId = cityData.TimezoneId,
            CurrentOffset = offset,
            CountryCode = cityData.CountryCode,
            OriginalQuery = query
        };

        return new ResolvedLocation
        {
            Location = location,
            TimeZone = zone,
            IsExactMatch = true
        };
    }

    private static Dictionary<string, CityData> InitializeCityData()
    {
        // Common cities and US states - in a real implementation, this would be loaded from embedded resource
        return new Dictionary<string, CityData>(StringComparer.OrdinalIgnoreCase)
        {
            // US States (using primary/capital timezone)
            ["alabama"] = new("Alabama", "America/Chicago", "US"),
            ["alaska"] = new("Alaska", "America/Anchorage", "US"),
            ["arizona"] = new("Arizona", "America/Phoenix", "US"),
            ["arkansas"] = new("Arkansas", "America/Chicago", "US"),
            ["california"] = new("California", "America/Los_Angeles", "US"),
            ["colorado"] = new("Colorado", "America/Denver", "US"),
            ["connecticut"] = new("Connecticut", "America/New_York", "US"),
            ["delaware"] = new("Delaware", "America/New_York", "US"),
            ["florida"] = new("Florida", "America/New_York", "US"),
            ["georgia"] = new("Georgia", "America/New_York", "US"),
            ["hawaii"] = new("Hawaii", "Pacific/Honolulu", "US"),
            ["idaho"] = new("Idaho", "America/Boise", "US"),
            ["illinois"] = new("Illinois", "America/Chicago", "US"),
            ["indiana"] = new("Indiana", "America/Indiana/Indianapolis", "US"),
            ["iowa"] = new("Iowa", "America/Chicago", "US"),
            ["kansas"] = new("Kansas", "America/Chicago", "US"),
            ["kentucky"] = new("Kentucky", "America/Kentucky/Louisville", "US"),
            ["louisiana"] = new("Louisiana", "America/Chicago", "US"),
            ["maine"] = new("Maine", "America/New_York", "US"),
            ["maryland"] = new("Maryland", "America/New_York", "US"),
            ["massachusetts"] = new("Massachusetts", "America/New_York", "US"),
            ["michigan"] = new("Michigan", "America/Detroit", "US"),
            ["minnesota"] = new("Minnesota", "America/Chicago", "US"),
            ["mississippi"] = new("Mississippi", "America/Chicago", "US"),
            ["missouri"] = new("Missouri", "America/Chicago", "US"),
            ["montana"] = new("Montana", "America/Denver", "US"),
            ["nebraska"] = new("Nebraska", "America/Chicago", "US"),
            ["nevada"] = new("Nevada", "America/Los_Angeles", "US"),
            ["new hampshire"] = new("New Hampshire", "America/New_York", "US"),
            ["new jersey"] = new("New Jersey", "America/New_York", "US"),
            ["new mexico"] = new("New Mexico", "America/Denver", "US"),
            ["north carolina"] = new("North Carolina", "America/New_York", "US"),
            ["north dakota"] = new("North Dakota", "America/Chicago", "US"),
            ["ohio"] = new("Ohio", "America/New_York", "US"),
            ["oklahoma"] = new("Oklahoma", "America/Chicago", "US"),
            ["oregon"] = new("Oregon", "America/Los_Angeles", "US"),
            ["pennsylvania"] = new("Pennsylvania", "America/New_York", "US"),
            ["rhode island"] = new("Rhode Island", "America/New_York", "US"),
            ["south carolina"] = new("South Carolina", "America/New_York", "US"),
            ["south dakota"] = new("South Dakota", "America/Chicago", "US"),
            ["tennessee"] = new("Tennessee", "America/Chicago", "US"),
            ["texas"] = new("Texas", "America/Chicago", "US"),
            ["utah"] = new("Utah", "America/Denver", "US"),
            ["vermont"] = new("Vermont", "America/New_York", "US"),
            ["virginia"] = new("Virginia", "America/New_York", "US"),
            ["washington"] = new("Washington", "America/Los_Angeles", "US"),
            ["west virginia"] = new("West Virginia", "America/New_York", "US"),
            ["wisconsin"] = new("Wisconsin", "America/Chicago", "US"),
            ["wyoming"] = new("Wyoming", "America/Denver", "US"),

            // US Cities
            ["new york"] = new("New York, NY", "America/New_York", "US"),
            ["nyc"] = new("New York, NY", "America/New_York", "US"),
            ["los angeles"] = new("Los Angeles, CA", "America/Los_Angeles", "US"),
            ["la"] = new("Los Angeles, CA", "America/Los_Angeles", "US"),
            ["chicago"] = new("Chicago, IL", "America/Chicago", "US"),
            ["houston"] = new("Houston, TX", "America/Chicago", "US"),
            ["phoenix"] = new("Phoenix, AZ", "America/Phoenix", "US"),
            ["philadelphia"] = new("Philadelphia, PA", "America/New_York", "US"),
            ["san antonio"] = new("San Antonio, TX", "America/Chicago", "US"),
            ["san diego"] = new("San Diego, CA", "America/Los_Angeles", "US"),
            ["dallas"] = new("Dallas, TX", "America/Chicago", "US"),
            ["san francisco"] = new("San Francisco, CA", "America/Los_Angeles", "US"),
            ["sf"] = new("San Francisco, CA", "America/Los_Angeles", "US"),
            ["austin"] = new("Austin, TX", "America/Chicago", "US"),
            ["seattle"] = new("Seattle, WA", "America/Los_Angeles", "US"),
            ["denver"] = new("Denver, CO", "America/Denver", "US"),
            ["boston"] = new("Boston, MA", "America/New_York", "US"),
            ["miami"] = new("Miami, FL", "America/New_York", "US"),
            ["atlanta"] = new("Atlanta, GA", "America/New_York", "US"),

            ["london"] = new("London, UK", "Europe/London", "GB"),
            ["paris"] = new("Paris, France", "Europe/Paris", "FR"),
            ["berlin"] = new("Berlin, Germany", "Europe/Berlin", "DE"),
            ["madrid"] = new("Madrid, Spain", "Europe/Madrid", "ES"),
            ["rome"] = new("Rome, Italy", "Europe/Rome", "IT"),
            ["amsterdam"] = new("Amsterdam, Netherlands", "Europe/Amsterdam", "NL"),
            ["brussels"] = new("Brussels, Belgium", "Europe/Brussels", "BE"),
            ["vienna"] = new("Vienna, Austria", "Europe/Vienna", "AT"),
            ["zurich"] = new("Zurich, Switzerland", "Europe/Zurich", "CH"),
            ["dublin"] = new("Dublin, Ireland", "Europe/Dublin", "IE"),
            ["lisbon"] = new("Lisbon, Portugal", "Europe/Lisbon", "PT"),
            ["stockholm"] = new("Stockholm, Sweden", "Europe/Stockholm", "SE"),
            ["oslo"] = new("Oslo, Norway", "Europe/Oslo", "NO"),
            ["copenhagen"] = new("Copenhagen, Denmark", "Europe/Copenhagen", "DK"),
            ["helsinki"] = new("Helsinki, Finland", "Europe/Helsinki", "FI"),
            ["warsaw"] = new("Warsaw, Poland", "Europe/Warsaw", "PL"),
            ["prague"] = new("Prague, Czech Republic", "Europe/Prague", "CZ"),
            ["budapest"] = new("Budapest, Hungary", "Europe/Budapest", "HU"),
            ["athens"] = new("Athens, Greece", "Europe/Athens", "GR"),
            ["moscow"] = new("Moscow, Russia", "Europe/Moscow", "RU"),

            ["tokyo"] = new("Tokyo, Japan", "Asia/Tokyo", "JP"),
            ["beijing"] = new("Beijing, China", "Asia/Shanghai", "CN"),
            ["shanghai"] = new("Shanghai, China", "Asia/Shanghai", "CN"),
            ["hong kong"] = new("Hong Kong", "Asia/Hong_Kong", "HK"),
            ["singapore"] = new("Singapore", "Asia/Singapore", "SG"),
            ["seoul"] = new("Seoul, South Korea", "Asia/Seoul", "KR"),
            ["mumbai"] = new("Mumbai, India", "Asia/Kolkata", "IN"),
            ["delhi"] = new("Delhi, India", "Asia/Kolkata", "IN"),
            ["bangalore"] = new("Bangalore, India", "Asia/Kolkata", "IN"),
            ["dubai"] = new("Dubai, UAE", "Asia/Dubai", "AE"),
            ["bangkok"] = new("Bangkok, Thailand", "Asia/Bangkok", "TH"),
            ["jakarta"] = new("Jakarta, Indonesia", "Asia/Jakarta", "ID"),
            ["manila"] = new("Manila, Philippines", "Asia/Manila", "PH"),
            ["kuala lumpur"] = new("Kuala Lumpur, Malaysia", "Asia/Kuala_Lumpur", "MY"),
            ["taipei"] = new("Taipei, Taiwan", "Asia/Taipei", "TW"),
            ["tel aviv"] = new("Tel Aviv, Israel", "Asia/Jerusalem", "IL"),

            ["sydney"] = new("Sydney, Australia", "Australia/Sydney", "AU"),
            ["melbourne"] = new("Melbourne, Australia", "Australia/Melbourne", "AU"),
            ["brisbane"] = new("Brisbane, Australia", "Australia/Brisbane", "AU"),
            ["perth"] = new("Perth, Australia", "Australia/Perth", "AU"),
            ["auckland"] = new("Auckland, New Zealand", "Pacific/Auckland", "NZ"),

            ["toronto"] = new("Toronto, Canada", "America/Toronto", "CA"),
            ["vancouver"] = new("Vancouver, Canada", "America/Vancouver", "CA"),
            ["montreal"] = new("Montreal, Canada", "America/Montreal", "CA"),

            ["sao paulo"] = new("São Paulo, Brazil", "America/Sao_Paulo", "BR"),
            ["rio de janeiro"] = new("Rio de Janeiro, Brazil", "America/Sao_Paulo", "BR"),
            ["buenos aires"] = new("Buenos Aires, Argentina", "America/Argentina/Buenos_Aires", "AR"),
            ["mexico city"] = new("Mexico City, Mexico", "America/Mexico_City", "MX"),

            ["cairo"] = new("Cairo, Egypt", "Africa/Cairo", "EG"),
            ["johannesburg"] = new("Johannesburg, South Africa", "Africa/Johannesburg", "ZA"),
            ["lagos"] = new("Lagos, Nigeria", "Africa/Lagos", "NG"),
            ["nairobi"] = new("Nairobi, Kenya", "Africa/Nairobi", "KE"),
        };
    }

    private sealed record CityData(string DisplayName, string TimezoneId, string CountryCode);
}
