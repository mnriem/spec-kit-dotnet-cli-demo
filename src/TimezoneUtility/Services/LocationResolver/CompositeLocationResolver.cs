using TimezoneUtility.Models;

namespace TimezoneUtility.Services.LocationResolver;

/// <summary>
/// Composite resolver that combines multiple location resolvers with priority chain.
/// </summary>
public sealed class CompositeLocationResolver : ILocationResolver
{
    private readonly IReadOnlyList<ILocationResolver> _resolvers;

    /// <summary>
    /// Creates a new CompositeLocationResolver with default resolver chain.
    /// </summary>
    public CompositeLocationResolver()
        : this(
            new IanaTimezoneResolver(),
            new ZipCodeResolver(),
            new CountryResolver(),
            new CityResolver())
    {
    }

    /// <summary>
    /// Creates a new CompositeLocationResolver with specified resolvers.
    /// </summary>
    /// <param name="resolvers">The resolvers to use, in priority order</param>
    public CompositeLocationResolver(params ILocationResolver[] resolvers)
    {
        _resolvers = resolvers;
    }

    /// <inheritdoc />
    public bool CanHandle(string query)
    {
        return _resolvers.Any(r => r.CanHandle(query));
    }

    /// <inheritdoc />
    public ResolvedLocation? Resolve(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return null;
        }

        // Try each resolver in priority order
        foreach (var resolver in _resolvers)
        {
            if (resolver.CanHandle(query))
            {
                var result = resolver.Resolve(query);
                if (result != null)
                {
                    return result;
                }
            }
        }

        return null;
    }

    /// <summary>
    /// Resolves multiple location queries.
    /// </summary>
    /// <param name="queries">The location queries to resolve</param>
    /// <returns>List of results, with null entries for failed resolutions</returns>
    public IReadOnlyList<ResolvedLocation?> ResolveAll(IEnumerable<string> queries)
    {
        return queries.Select(Resolve).ToList();
    }
}
