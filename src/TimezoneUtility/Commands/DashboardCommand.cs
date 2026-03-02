using System.CommandLine;
using Spectre.Console;
using TimezoneUtility.Models;
using TimezoneUtility.Output;
using TimezoneUtility.Services;
using TimezoneUtility.Services.LocationResolver;
using TimezoneUtility.Services.TimeConversion;

namespace TimezoneUtility.Commands;

/// <summary>
/// Command handler for 'tzutil dashboard' - world clock dashboard view.
/// </summary>
public static class DashboardCommand
{
    /// <summary>
    /// Creates the 'dashboard' command.
    /// </summary>
    public static Command Create()
    {
        var locationsArg = new Argument<string[]>("locations", "Locations to display")
        {
            Arity = ArgumentArity.ZeroOrMore
        };

        var profileOption = new Option<string?>(
            ["--profile", "-p"],
            "Use a saved profile");

        var watchOption = new Option<bool>(
            ["--watch", "-w"],
            "Continuously update display");

        var intervalOption = new Option<int>(
            ["--interval", "-i"],
            () => 60,
            "Update interval in seconds (with --watch)");

        var jsonOption = new Option<bool>(
            ["--json", "-j"],
            "Output as JSON");

        var command = new Command("dashboard", "Show a world clock dashboard view")
        {
            locationsArg,
            profileOption,
            watchOption,
            intervalOption,
            jsonOption
        };

        command.SetHandler(Execute, locationsArg, profileOption, watchOption, intervalOption, jsonOption);

        return command;
    }

    private static void Execute(string[] locations, string? profile, bool watch, int interval, bool json)
    {
        var resolver = new CompositeLocationResolver();
        var timeService = new TimeService();
        var configManager = new ConfigManager();
        var profileService = new ProfileService(configManager);
        var formatter = new OutputFormatter(json);

        // Get locations from profile if specified
        var queries = locations;
        string? profileName = null;

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
            profileName = profileData.Name;
        }

        if (queries.Length == 0)
        {
            formatter.WriteError("No locations specified. Provide locations or use --profile.");
            Environment.ExitCode = 1;
            return;
        }

        if (watch && !json)
        {
            RunWatchMode(queries, resolver, timeService, profileName, interval);
        }
        else
        {
            var results = GetCurrentTimes(queries, resolver, timeService);
            formatter.WriteDashboard(results, profileName);
        }
    }

    private static void RunWatchMode(
        string[] queries,
        CompositeLocationResolver resolver,
        TimeService timeService,
        string? profileName,
        int interval)
    {
        AnsiConsole.MarkupLine("[dim]Press Ctrl+C to exit[/]");
        AnsiConsole.WriteLine();

        try
        {
            while (true)
            {
                // Clear screen and move cursor to top
                AnsiConsole.Clear();

                var results = GetCurrentTimes(queries, resolver, timeService);
                var formatter = new OutputFormatter(false);
                formatter.WriteDashboard(results, profileName);

                Thread.Sleep(interval * 1000);
            }
        }
        catch (ThreadInterruptedException)
        {
            // Exit gracefully
        }
    }

    private static List<NowResult> GetCurrentTimes(
        string[] queries,
        CompositeLocationResolver resolver,
        TimeService timeService)
    {
        var results = new List<NowResult>();

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
                continue;
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

        return results;
    }
}
