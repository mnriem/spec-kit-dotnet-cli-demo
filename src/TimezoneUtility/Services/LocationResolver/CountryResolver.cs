using NodaTime;
using TimezoneUtility.Models;

namespace TimezoneUtility.Services.LocationResolver;

/// <summary>
/// Resolver for country name lookups.
/// </summary>
public sealed class CountryResolver : ILocationResolver
{
    private readonly IDateTimeZoneProvider _tzdb;
    private readonly Dictionary<string, CountryData> _countries;

    /// <summary>
    /// Creates a new CountryResolver.
    /// </summary>
    public CountryResolver()
    {
        _tzdb = DateTimeZoneProviders.Tzdb;
        _countries = InitializeCountryData();
    }

    /// <inheritdoc />
    public bool CanHandle(string query)
    {
        // Country names are alphabetic, don't contain digits or slashes
        return !string.IsNullOrWhiteSpace(query) &&
               !query.Contains('/') &&
               !query.Any(char.IsDigit);
    }

    /// <inheritdoc />
    public ResolvedLocation? Resolve(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return null;
        }

        var normalizedQuery = query.Trim().ToLowerInvariant();

        if (_countries.TryGetValue(normalizedQuery, out var countryData))
        {
            return CreateResolvedLocation(countryData, query);
        }

        return null;
    }

    private ResolvedLocation? CreateResolvedLocation(CountryData countryData, string query)
    {
        var zone = _tzdb.GetZoneOrNull(countryData.TimezoneId);
        if (zone == null)
        {
            return null;
        }

        var now = SystemClock.Instance.GetCurrentInstant();
        var offset = zone.GetUtcOffset(now);

        var location = new Location
        {
            DisplayName = countryData.DisplayName,
            TimezoneId = countryData.TimezoneId,
            CurrentOffset = offset,
            CountryCode = countryData.CountryCode,
            OriginalQuery = query
        };

        return new ResolvedLocation
        {
            Location = location,
            TimeZone = zone,
            IsExactMatch = true
        };
    }

    private static Dictionary<string, CountryData> InitializeCountryData()
    {
        // Countries mapped to their primary/capital timezone
        return new Dictionary<string, CountryData>(StringComparer.OrdinalIgnoreCase)
        {
            // North America
            ["united states"] = new("United States", "America/New_York", "US"),
            ["usa"] = new("United States", "America/New_York", "US"),
            ["us"] = new("United States", "America/New_York", "US"),
            ["canada"] = new("Canada", "America/Toronto", "CA"),
            ["mexico"] = new("Mexico", "America/Mexico_City", "MX"),

            // Europe
            ["united kingdom"] = new("United Kingdom", "Europe/London", "GB"),
            ["uk"] = new("United Kingdom", "Europe/London", "GB"),
            ["england"] = new("England", "Europe/London", "GB"),
            ["france"] = new("France", "Europe/Paris", "FR"),
            ["germany"] = new("Germany", "Europe/Berlin", "DE"),
            ["italy"] = new("Italy", "Europe/Rome", "IT"),
            ["spain"] = new("Spain", "Europe/Madrid", "ES"),
            ["netherlands"] = new("Netherlands", "Europe/Amsterdam", "NL"),
            ["holland"] = new("Netherlands", "Europe/Amsterdam", "NL"),
            ["belgium"] = new("Belgium", "Europe/Brussels", "BE"),
            ["switzerland"] = new("Switzerland", "Europe/Zurich", "CH"),
            ["austria"] = new("Austria", "Europe/Vienna", "AT"),
            ["sweden"] = new("Sweden", "Europe/Stockholm", "SE"),
            ["norway"] = new("Norway", "Europe/Oslo", "NO"),
            ["denmark"] = new("Denmark", "Europe/Copenhagen", "DK"),
            ["finland"] = new("Finland", "Europe/Helsinki", "FI"),
            ["poland"] = new("Poland", "Europe/Warsaw", "PL"),
            ["ireland"] = new("Ireland", "Europe/Dublin", "IE"),
            ["portugal"] = new("Portugal", "Europe/Lisbon", "PT"),
            ["greece"] = new("Greece", "Europe/Athens", "GR"),
            ["czech republic"] = new("Czech Republic", "Europe/Prague", "CZ"),
            ["czechia"] = new("Czech Republic", "Europe/Prague", "CZ"),
            ["hungary"] = new("Hungary", "Europe/Budapest", "HU"),
            ["russia"] = new("Russia", "Europe/Moscow", "RU"),
            ["ukraine"] = new("Ukraine", "Europe/Kiev", "UA"),

            // Asia
            ["japan"] = new("Japan", "Asia/Tokyo", "JP"),
            ["china"] = new("China", "Asia/Shanghai", "CN"),
            ["india"] = new("India", "Asia/Kolkata", "IN"),
            ["south korea"] = new("South Korea", "Asia/Seoul", "KR"),
            ["korea"] = new("South Korea", "Asia/Seoul", "KR"),
            ["singapore"] = new("Singapore", "Asia/Singapore", "SG"),
            ["hong kong"] = new("Hong Kong", "Asia/Hong_Kong", "HK"),
            ["taiwan"] = new("Taiwan", "Asia/Taipei", "TW"),
            ["thailand"] = new("Thailand", "Asia/Bangkok", "TH"),
            ["indonesia"] = new("Indonesia", "Asia/Jakarta", "ID"),
            ["malaysia"] = new("Malaysia", "Asia/Kuala_Lumpur", "MY"),
            ["philippines"] = new("Philippines", "Asia/Manila", "PH"),
            ["vietnam"] = new("Vietnam", "Asia/Ho_Chi_Minh", "VN"),
            ["israel"] = new("Israel", "Asia/Jerusalem", "IL"),
            ["united arab emirates"] = new("UAE", "Asia/Dubai", "AE"),
            ["uae"] = new("UAE", "Asia/Dubai", "AE"),
            ["saudi arabia"] = new("Saudi Arabia", "Asia/Riyadh", "SA"),
            ["pakistan"] = new("Pakistan", "Asia/Karachi", "PK"),
            ["bangladesh"] = new("Bangladesh", "Asia/Dhaka", "BD"),

            // Oceania
            ["australia"] = new("Australia", "Australia/Sydney", "AU"),
            ["new zealand"] = new("New Zealand", "Pacific/Auckland", "NZ"),

            // South America
            ["brazil"] = new("Brazil", "America/Sao_Paulo", "BR"),
            ["argentina"] = new("Argentina", "America/Argentina/Buenos_Aires", "AR"),
            ["chile"] = new("Chile", "America/Santiago", "CL"),
            ["colombia"] = new("Colombia", "America/Bogota", "CO"),
            ["peru"] = new("Peru", "America/Lima", "PE"),

            // Africa
            ["south africa"] = new("South Africa", "Africa/Johannesburg", "ZA"),
            ["egypt"] = new("Egypt", "Africa/Cairo", "EG"),
            ["nigeria"] = new("Nigeria", "Africa/Lagos", "NG"),
            ["kenya"] = new("Kenya", "Africa/Nairobi", "KE"),
            ["morocco"] = new("Morocco", "Africa/Casablanca", "MA"),
        };
    }

    private sealed record CountryData(string DisplayName, string TimezoneId, string CountryCode);
}
