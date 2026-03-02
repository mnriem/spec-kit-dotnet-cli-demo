using TimezoneUtility.Models;

namespace TimezoneUtility.Services.LocationResolver;

/// <summary>
/// Interface for resolving location queries to timezone information.
/// </summary>
public interface ILocationResolver
{
    /// <summary>
    /// Attempts to resolve a location query to a timezone.
    /// </summary>
    /// <param name="query">The location query (IANA ID, city name, ZIP code, country)</param>
    /// <returns>The resolved location, or null if not found</returns>
    ResolvedLocation? Resolve(string query);

    /// <summary>
    /// Checks if this resolver can handle the given query.
    /// </summary>
    /// <param name="query">The location query to check</param>
    /// <returns>True if this resolver might be able to handle the query</returns>
    bool CanHandle(string query);
}
