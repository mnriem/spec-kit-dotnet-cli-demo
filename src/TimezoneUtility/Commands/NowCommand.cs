using System.CommandLine;
using TimezoneUtility.Models;
using TimezoneUtility.Output;
using TimezoneUtility.Services;
using TimezoneUtility.Services.LocationResolver;
using TimezoneUtility.Services.TimeConversion;

namespace TimezoneUtility.Commands;

/// <summary>
/// Command handler for 'tzutil now' - get current time for locations.
/// </summary>
public static class NowCommand
{
    /// <summary>
    /// Creates the 'now' command.
    /// </summary>
    public static Command Create()
    {
        var locationsArg = new Argument<string[]>("locations", "One or more locations (timezone, city, zip, country)")
        {
            Arity = ArgumentArity.ZeroOrMore
        };

        var formatOption = new Option<string>(
            ["--format", "-f"],
            () => "24h",
            "Time format: 12h or 24h");

        var sortOption = new Option<string>(
            ["--sort", "-s"],
            () => "input",
            "Sort order: input, time, or name");

        var profileOption = new Option<string?>(
            ["--profile", "-p"],
            "Use a saved profile instead of locations");

        var jsonOption = new Option<bool>(
            ["--json", "-j"],
            "Output as JSON");

        var command = new Command("now", "Get current time for one or more locations")
        {
            locationsArg,
            formatOption,
            sortOption,
            profileOption,
            jsonOption
        };

        command.SetHandler(Execute, locationsArg, formatOption, sortOption, profileOption, jsonOption);

        return command;
    }

    private static void Execute(string[] locations, string format, string sort, string? profile, bool json)
    {
        var resolver = new CompositeLocationResolver();
        var timeService = new TimeService();
        var configManager = new ConfigManager();
        var profileService = new ProfileService(configManager);
        var formatter = new OutputFormatter(json, format);

        // Get locations from profile if specified
        var queries = locations;
        if (!string.IsNullOrEmpty(profile))
        {
            var profileData = profileService.Get(profile);
            if (profileData == null)
            {
                formatter.WriteError($"Profile not found: {profile}");
                Environment.ExitCode = 2;
                return;
            }
            queries = profileData.Locations.Select(l => l.Query).ToArray();
        }

        if (queries.Length == 0)
        {
            formatter.WriteError("No locations specified");
            Environment.ExitCode = 1;
            return;
        }

        var results = new List<NowResult>();
        var hasErrors = false;

        foreach (var query in queries)
        {
            var resolved = resolver.Resolve(query);
            if (resolved == null)
            {
                results.Add(new NowResult
                {
                    Query = query,
                    Error = $"Location not found: {query}"
                });
                hasErrors = true;
                continue;
            }

            // Warn about ambiguous matches
            if (!resolved.IsExactMatch && resolved.Alternatives != null && !json)
            {
                ErrorHandler.WriteAmbiguousLocationWarning(
                    query,
                    resolved.Alternatives.Select(a => a.DisplayName).ToList());
            }

            var timeSlot = timeService.GetCurrentTime(resolved);
            var status = timeService.GetTimeOfDayStatus(timeSlot.Time);

            results.Add(new NowResult
            {
                Query = query,
                Location = resolved.Location,
                TimeSlot = timeSlot,
                Status = status
            });
        }

        // Sort results
        var sortedResults = sort.ToLowerInvariant() switch
        {
            "time" => results.OrderBy(r => r.TimeSlot?.Instant).ToList(),
            "name" => results.OrderBy(r => r.Location?.DisplayName ?? r.Query).ToList(),
            _ => results // "input" - keep original order
        };

        formatter.WriteNowResults(sortedResults);

        if (hasErrors)
        {
            // Exit code 2 if some locations weren't found, but still show partial results
            Environment.ExitCode = 2;
        }
    }
}
