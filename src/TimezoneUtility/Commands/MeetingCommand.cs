using System.CommandLine;
using NodaTime;
using TimezoneUtility.Models;
using TimezoneUtility.Output;
using TimezoneUtility.Services;
using TimezoneUtility.Services.LocationResolver;
using TimezoneUtility.Services.MeetingOptimizer;
using TimezoneUtility.Services.TimeConversion;

namespace TimezoneUtility.Commands;

/// <summary>
/// Command handler for 'tzutil meeting' - find optimal meeting times.
/// </summary>
public static class MeetingCommand
{
    /// <summary>
    /// Creates the 'meeting' command.
    /// </summary>
    public static Command Create()
    {
        var locationsArg = new Argument<string[]>("locations", "Participant locations (min 2)")
        {
            Arity = ArgumentArity.OneOrMore
        };

        var durationOption = new Option<string>(
            ["--duration", "-d"],
            () => "1h",
            "Meeting duration (e.g., '30m', '1h', '90m')");

        var dateOption = new Option<string?>(
            ["--date"],
            "Date to find times for (defaults to today)");

        var rangeOption = new Option<int>(
            ["--range", "-r"],
            () => 7,
            "Number of days to search");

        var workStartOption = new Option<string>(
            ["--work-start"],
            () => "09:00",
            "Default working hours start");

        var workEndOption = new Option<string>(
            ["--work-end"],
            () => "18:00",
            "Default working hours end");

        var limitOption = new Option<int>(
            ["--limit", "-l"],
            () => 5,
            "Maximum suggestions to show");

        var jsonOption = new Option<bool>(
            ["--json", "-j"],
            "Output as JSON");

        var command = new Command("meeting", "Find optimal meeting times across timezones")
        {
            locationsArg,
            durationOption,
            dateOption,
            rangeOption,
            workStartOption,
            workEndOption,
            limitOption,
            jsonOption
        };

        command.SetHandler(Execute, locationsArg, durationOption, dateOption, rangeOption,
            workStartOption, workEndOption, limitOption, jsonOption);

        return command;
    }

    private static void Execute(
        string[] locations,
        string durationInput,
        string? dateInput,
        int range,
        string workStartInput,
        string workEndInput,
        int limit,
        bool json)
    {
        var formatter = new OutputFormatter(json);
        var resolver = new CompositeLocationResolver();

        // Validate minimum locations
        if (locations.Length < 2)
        {
            formatter.WriteError("At least 2 participant locations are required");
            Environment.ExitCode = 1;
            return;
        }

        // Parse duration
        var duration = TimeParser.ParseDuration(durationInput);
        if (duration == null)
        {
            formatter.WriteError($"Invalid duration format: {durationInput}");
            Environment.ExitCode = 1;
            return;
        }

        // Parse working hours
        var workStart = TimeParser.ParseTime(workStartInput);
        var workEnd = TimeParser.ParseTime(workEndInput);
        if (workStart == null || workEnd == null)
        {
            formatter.WriteError("Invalid working hours format");
            Environment.ExitCode = 1;
            return;
        }

        var workingHours = new WorkingHours
        {
            Start = workStart.Value,
            End = workEnd.Value,
            WorkDays = new HashSet<IsoDayOfWeek>
            {
                IsoDayOfWeek.Monday, IsoDayOfWeek.Tuesday, IsoDayOfWeek.Wednesday,
                IsoDayOfWeek.Thursday, IsoDayOfWeek.Friday
            }
        };

        // Parse start date
        var startDate = LocalDate.FromDateTime(DateTime.Today);
        if (!string.IsNullOrEmpty(dateInput))
        {
            var parsedDate = TimeParser.ParseDate(dateInput);
            if (parsedDate == null)
            {
                formatter.WriteError($"Invalid date format: {dateInput}");
                Environment.ExitCode = 1;
                return;
            }
            startDate = parsedDate.Value;
        }

        // Resolve all locations
        var resolved = new List<ResolvedLocation>();
        foreach (var location in locations)
        {
            var r = resolver.Resolve(location);
            if (r == null)
            {
                formatter.WriteError($"Location not found: {location}");
                Environment.ExitCode = 2;
                return;
            }
            resolved.Add(r);
        }

        // Find optimal times
        var optimizer = new MeetingOptimizer();
        var dateRange = new DateInterval(startDate, startDate.PlusDays(range - 1));
        var slots = optimizer.FindOptimalTimes(resolved, duration.Value, dateRange, workingHours, limit);

        var idealSlots = slots.Where(s => s.IsIdeal).ToList();
        var compromiseSlots = slots.Where(s => !s.IsIdeal).ToList();

        var result = new MeetingResult
        {
            ParticipantQueries = locations,
            Duration = duration.Value,
            WorkingHours = workingHours,
            IdealSlots = idealSlots,
            CompromiseSlots = compromiseSlots
        };

        formatter.WriteMeetingResults(result);
    }
}
