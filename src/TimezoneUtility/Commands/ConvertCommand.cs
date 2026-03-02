using System.CommandLine;
using NodaTime;
using TimezoneUtility.Output;
using TimezoneUtility.Services.LocationResolver;
using TimezoneUtility.Services.TimeConversion;

namespace TimezoneUtility.Commands;

/// <summary>
/// Command handler for 'tzutil convert' - convert time between timezones.
/// </summary>
public static class ConvertCommand
{
    /// <summary>
    /// Creates the 'convert' command.
    /// </summary>
    public static Command Create()
    {
        var timeArg = new Argument<string>("time", "Time to convert (e.g., '3pm', '15:00', '2026-03-02 15:00')");

        var fromOption = new Option<string>(
            ["--from"],
            "Source timezone/location")
        {
            IsRequired = true
        };

        var toOption = new Option<string[]>(
            ["--to"],
            "Target timezone(s)/location(s)")
        {
            IsRequired = true,
            AllowMultipleArgumentsPerToken = true
        };

        var dateOption = new Option<string?>(
            ["--date", "-d"],
            "Date for the conversion (defaults to today)");

        var formatOption = new Option<string>(
            ["--format", "-f"],
            () => "24h",
            "Output format: 12h or 24h");

        var jsonOption = new Option<bool>(
            ["--json", "-j"],
            "Output as JSON");

        var command = new Command("convert", "Convert a time from one timezone to others")
        {
            timeArg,
            fromOption,
            toOption,
            dateOption,
            formatOption,
            jsonOption
        };

        command.SetHandler(Execute, timeArg, fromOption, toOption, dateOption, formatOption, jsonOption);

        return command;
    }

    private static void Execute(string timeInput, string from, string[] to, string? dateInput, string format, bool json)
    {
        var resolver = new CompositeLocationResolver();
        var timeService = new TimeService();
        var formatter = new OutputFormatter(json, format);

        // Parse the time
        var parsedTime = TimeParser.ParseTime(timeInput);
        if (parsedTime == null)
        {
            formatter.WriteError($"Invalid time format: {timeInput}");
            Environment.ExitCode = 1;
            return;
        }

        // Parse the date (default to today)
        var date = LocalDate.FromDateTime(DateTime.Today);
        if (!string.IsNullOrEmpty(dateInput))
        {
            var parsedDate = TimeParser.ParseDate(dateInput);
            if (parsedDate == null)
            {
                formatter.WriteError($"Invalid date format: {dateInput}");
                Environment.ExitCode = 1;
                return;
            }
            date = parsedDate.Value;
        }

        var sourceDateTime = date + parsedTime.Value;

        // Resolve source location
        var sourceResolved = resolver.Resolve(from);
        if (sourceResolved == null)
        {
            formatter.WriteError($"Source location not found: {from}");
            Environment.ExitCode = 2;
            return;
        }

        // Get source time slot
        var sourceZonedTime = sourceDateTime.InZoneLeniently(sourceResolved.TimeZone);
        var sourceSlot = new Models.TimeSlot
        {
            Instant = sourceZonedTime.ToInstant(),
            Zone = sourceResolved.TimeZone
        };

        // Convert to each target
        var conversions = new List<ConversionTarget>();
        foreach (var targetQuery in to)
        {
            var targetResolved = resolver.Resolve(targetQuery);
            if (targetResolved == null)
            {
                conversions.Add(new ConversionTarget
                {
                    Query = targetQuery,
                    Error = $"Location not found: {targetQuery}"
                });
                continue;
            }

            var targetSlot = sourceSlot.InZone(targetResolved.TimeZone);
            var dayDelta = Period.DaysBetween(sourceSlot.Date, targetSlot.Date);

            conversions.Add(new ConversionTarget
            {
                Query = targetQuery,
                Location = targetResolved.Location,
                TimeSlot = targetSlot,
                DayDelta = dayDelta
            });
        }

        var result = new ConvertResult
        {
            SourceTime = sourceDateTime,
            SourceLocation = sourceResolved.Location,
            SourceSlot = sourceSlot,
            Conversions = conversions
        };

        formatter.WriteConvertResults(result);
    }
}
