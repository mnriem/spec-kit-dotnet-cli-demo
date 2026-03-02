using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using NodaTime;
using NodaTime.Serialization.SystemTextJson;
using Spectre.Console;
using TimezoneUtility.Models;

namespace TimezoneUtility.Output;

/// <summary>
/// Formats output for human-readable or JSON formats.
/// </summary>
public sealed class OutputFormatter
{
    private readonly bool _jsonOutput;
    private readonly string _timeFormat;
    private static readonly JsonSerializerOptions JsonOptions;

    static OutputFormatter()
    {
        JsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };
        JsonOptions.ConfigureForNodaTime(DateTimeZoneProviders.Tzdb);
    }

    /// <summary>
    /// Creates a new OutputFormatter.
    /// </summary>
    /// <param name="jsonOutput">Whether to output JSON</param>
    /// <param name="timeFormat">Time format (12h or 24h)</param>
    public OutputFormatter(bool jsonOutput = false, string timeFormat = "24h")
    {
        _jsonOutput = jsonOutput;
        _timeFormat = timeFormat;
    }

    /// <summary>
    /// Outputs the current time for multiple locations.
    /// </summary>
    public void WriteNowResults(IReadOnlyList<NowResult> results)
    {
        if (_jsonOutput)
        {
            var output = new
            {
                timestamp = SystemClock.Instance.GetCurrentInstant().ToString(),
                results = results.Select(r => new
                {
                    query = r.Query,
                    location = r.Location != null ? new
                    {
                        displayName = r.Location.DisplayName,
                        timezoneId = r.Location.TimezoneId,
                        countryCode = r.Location.CountryCode
                    } : null,
                    time = r.TimeSlot != null ? new
                    {
                        iso8601 = r.TimeSlot.ZonedDateTime.ToString(),
                        date = r.TimeSlot.Date.ToString(),
                        time = r.TimeSlot.Time.ToString(),
                        offset = r.TimeSlot.OffsetString,
                        abbreviation = GetAbbreviation(r.TimeSlot)
                    } : null,
                    status = r.Status?.ToString().ToLowerInvariant().Replace("business", "business_hours"),
                    error = r.Error
                })
            };
            Console.WriteLine(JsonSerializer.Serialize(output, JsonOptions));
        }
        else
        {
            WriteHumanNowResults(results);
        }
    }

    private void WriteHumanNowResults(IReadOnlyList<NowResult> results)
    {
        var table = new Table();
        table.AddColumn("Location");
        table.AddColumn("Time");
        table.AddColumn("Offset");
        table.AddColumn("Status");

        foreach (var result in results)
        {
            if (result.Error != null)
            {
                table.AddRow(
                    $"[red]{Markup.Escape(result.Query)}[/]",
                    $"[red]{Markup.Escape(result.Error)}[/]",
                    "-",
                    "-");
            }
            else if (result.TimeSlot != null && result.Location != null)
            {
                var timeStr = FormatTime(result.TimeSlot);
                var statusStr = FormatStatus(result.Status);
                table.AddRow(
                    Markup.Escape(result.Location.DisplayName),
                    Markup.Escape(timeStr),
                    result.TimeSlot.OffsetString,
                    statusStr);
            }
        }

        AnsiConsole.Write(table);
    }

    /// <summary>
    /// Outputs time conversion results.
    /// </summary>
    public void WriteConvertResults(ConvertResult result)
    {
        if (_jsonOutput)
        {
            var output = new
            {
                source = new
                {
                    time = result.SourceTime.TimeOfDay.ToString(),
                    date = result.SourceTime.Date.ToString(),
                    location = result.SourceLocation?.DisplayName,
                    timezoneId = result.SourceLocation?.TimezoneId,
                    iso8601 = result.SourceSlot?.ZonedDateTime.ToString()
                },
                conversions = result.Conversions.Select(c => new
                {
                    location = c.Location?.DisplayName,
                    timezoneId = c.Location?.TimezoneId,
                    iso8601 = c.TimeSlot?.ZonedDateTime.ToString(),
                    dayDelta = c.DayDelta
                })
            };
            Console.WriteLine(JsonSerializer.Serialize(output, JsonOptions));
        }
        else
        {
            WriteHumanConvertResults(result);
        }
    }

    private void WriteHumanConvertResults(ConvertResult result)
    {
        AnsiConsole.MarkupLine($"Converting [cyan]{FormatTime(result.SourceSlot!)}[/] from [cyan]{result.SourceLocation?.DisplayName}[/] ({result.SourceLocation?.TimezoneId})");
        AnsiConsole.WriteLine();

        var table = new Table();
        table.AddColumn("Location");
        table.AddColumn("Time");
        table.AddColumn("Date");
        table.AddColumn("Offset");

        foreach (var conversion in result.Conversions)
        {
            if (conversion.Error != null)
            {
                table.AddRow(
                    $"[red]{Markup.Escape(conversion.Query)}[/]",
                    $"[red]{Markup.Escape(conversion.Error)}[/]",
                    "-",
                    "-");
            }
            else if (conversion.TimeSlot != null && conversion.Location != null)
            {
                var dayDeltaStr = conversion.DayDelta switch
                {
                    > 0 => $" [yellow](+{conversion.DayDelta})[/]",
                    < 0 => $" [yellow]({conversion.DayDelta})[/]",
                    _ => ""
                };
                table.AddRow(
                    Markup.Escape(conversion.Location.DisplayName),
                    Markup.Escape(FormatTimeOnly(conversion.TimeSlot)),
                    $"{conversion.TimeSlot.Date:MMM d}{dayDeltaStr}",
                    conversion.TimeSlot.OffsetString);
            }
        }

        AnsiConsole.Write(table);
    }

    /// <summary>
    /// Outputs meeting time suggestions.
    /// </summary>
    public void WriteMeetingResults(MeetingResult result)
    {
        if (_jsonOutput)
        {
            var output = new
            {
                request = new
                {
                    participants = result.ParticipantQueries,
                    duration = result.Duration.ToString(),
                    workingHours = new
                    {
                        start = result.WorkingHours.Start.ToString(),
                        end = result.WorkingHours.End.ToString()
                    }
                },
                ideal = result.IdealSlots.Select(FormatMeetingSlot),
                compromise = result.CompromiseSlots.Select(FormatMeetingSlot)
            };
            Console.WriteLine(JsonSerializer.Serialize(output, JsonOptions));
        }
        else
        {
            WriteHumanMeetingResults(result);
        }
    }

    private static object FormatMeetingSlot(MeetingSlot slot)
    {
        return new
        {
            startTime = slot.StartTime.ToString(),
            duration = slot.Duration.ToString(),
            score = slot.Score,
            participants = slot.Participants.Select(p => new
            {
                location = p.Location.DisplayName,
                timezoneId = p.Location.TimezoneId,
                localTime = p.LocalStartTime.ToString(),
                inWorkingHours = p.InWorkingHours
            })
        };
    }

    private void WriteHumanMeetingResults(MeetingResult result)
    {
        AnsiConsole.MarkupLine($"Finding [cyan]{result.Duration}[/] meeting times for: [cyan]{string.Join(", ", result.ParticipantQueries)}[/]");
        AnsiConsole.MarkupLine($"Working hours: [dim]{result.WorkingHours.Start}-{result.WorkingHours.End}[/]");
        AnsiConsole.WriteLine();

        if (result.IdealSlots.Count > 0)
        {
            AnsiConsole.MarkupLine("[green]Best Times (all in working hours):[/]");
            WriteMeetingSlotTable(result.IdealSlots);
        }

        if (result.CompromiseSlots.Count > 0)
        {
            if (result.IdealSlots.Count > 0)
            {
                AnsiConsole.WriteLine();
            }
            AnsiConsole.MarkupLine("[yellow]Compromise Times (minimizing impact):[/]");
            WriteMeetingSlotTable(result.CompromiseSlots);
        }

        if (result.IdealSlots.Count == 0 && result.CompromiseSlots.Count == 0)
        {
            AnsiConsole.MarkupLine("[red]No suitable meeting times found in the specified range.[/]");
        }
    }

    private void WriteMeetingSlotTable(IReadOnlyList<MeetingSlot> slots)
    {
        var table = new Table();
        table.AddColumn("#");

        // Add columns for each participant
        if (slots.Count > 0 && slots[0].Participants.Count > 0)
        {
            foreach (var participant in slots[0].Participants)
            {
                table.AddColumn(participant.Location.DisplayName);
            }
        }

        var index = 1;
        foreach (var slot in slots)
        {
            var row = new List<string> { index.ToString(CultureInfo.InvariantCulture) };
            foreach (var p in slot.Participants)
            {
                var marker = p.InWorkingHours ? "✓" : "⚠";
                row.Add($"{p.LocalDate:MMM d} {FormatTimeOnly(p.LocalStartTime)} {marker}");
            }
            table.AddRow(row.Select(Markup.Escape).ToArray());
            index++;
        }

        AnsiConsole.Write(table);
    }

    /// <summary>
    /// Outputs dashboard view.
    /// </summary>
    public void WriteDashboard(IReadOnlyList<NowResult> results, string? profileName = null)
    {
        if (_jsonOutput)
        {
            WriteNowResults(results);
            return;
        }

        var title = profileName != null
            ? $"World Clock Dashboard - {profileName}"
            : "World Clock Dashboard";

        AnsiConsole.MarkupLine($"[bold]{Markup.Escape(title)}[/]");
        AnsiConsole.MarkupLine($"[dim]Updated: {SystemClock.Instance.GetCurrentInstant():yyyy-MM-dd HH:mm:ss} UTC[/]");
        AnsiConsole.WriteLine();

        var table = new Table();
        table.Border(TableBorder.Rounded);
        table.AddColumn("Location");
        table.AddColumn("Time");
        table.AddColumn("Offset");
        table.AddColumn("Status");

        foreach (var result in results)
        {
            if (result.TimeSlot != null && result.Location != null)
            {
                var timeStr = $"{result.TimeSlot.Date:ddd} {FormatTimeOnly(result.TimeSlot)}";
                var statusStr = FormatStatusWithEmoji(result.Status);
                table.AddRow(
                    Markup.Escape(result.Location.DisplayName),
                    Markup.Escape(timeStr),
                    result.TimeSlot.OffsetString,
                    statusStr);
            }
        }

        AnsiConsole.Write(table);
    }

    /// <summary>
    /// Outputs an error message.
    /// </summary>
    public void WriteError(string message)
    {
        if (_jsonOutput)
        {
            var output = new { error = message };
            Console.WriteLine(JsonSerializer.Serialize(output, JsonOptions));
        }
        else
        {
            AnsiConsole.MarkupLine($"[red]Error: {Markup.Escape(message)}[/]");
        }
    }

    private string FormatTime(TimeSlot slot)
    {
        var dateStr = slot.Date.ToString("yyyy-MM-dd", null);
        var timeStr = FormatTimeOnly(slot);
        var abbr = GetAbbreviation(slot);
        return $"{dateStr} {timeStr} {abbr}";
    }

    private string FormatTimeOnly(TimeSlot slot)
    {
        return FormatTimeOnly(slot.Time);
    }

    private string FormatTimeOnly(LocalTime time)
    {
        if (_timeFormat == "12h")
        {
            var hour = time.Hour;
            var amPm = hour >= 12 ? "PM" : "AM";
            hour = hour % 12;
            if (hour == 0) hour = 12;
            return $"{hour}:{time.Minute:D2} {amPm}";
        }
        return $"{time.Hour:D2}:{time.Minute:D2}";
    }

    private static string GetAbbreviation(TimeSlot slot)
    {
        // Get timezone abbreviation from the zone interval
        var interval = slot.Zone.GetZoneInterval(slot.Instant);
        return interval.Name;
    }

    private static string FormatStatus(TimeOfDayStatus? status)
    {
        return status switch
        {
            TimeOfDayStatus.Business => "[green]Business Hours[/]",
            TimeOfDayStatus.Evening => "[yellow]Evening[/]",
            TimeOfDayStatus.Night => "[red]Night[/]",
            _ => "[dim]Unknown[/]"
        };
    }

    private static string FormatStatusWithEmoji(TimeOfDayStatus? status)
    {
        return status switch
        {
            TimeOfDayStatus.Business => "[green]🟢 Business[/]",
            TimeOfDayStatus.Evening => "[yellow]🟡 Evening[/]",
            TimeOfDayStatus.Night => "[red]🔴 Night[/]",
            _ => "[dim]Unknown[/]"
        };
    }
}

/// <summary>
/// Result for a single location in the 'now' command.
/// </summary>
public sealed record NowResult
{
    public required string Query { get; init; }
    public Location? Location { get; init; }
    public TimeSlot? TimeSlot { get; init; }
    public TimeOfDayStatus? Status { get; init; }
    public string? Error { get; init; }
}

/// <summary>
/// Result for the 'convert' command.
/// </summary>
public sealed record ConvertResult
{
    public required LocalDateTime SourceTime { get; init; }
    public Location? SourceLocation { get; init; }
    public TimeSlot? SourceSlot { get; init; }
    public required IReadOnlyList<ConversionTarget> Conversions { get; init; }
}

/// <summary>
/// A single conversion target result.
/// </summary>
public sealed record ConversionTarget
{
    public required string Query { get; init; }
    public Location? Location { get; init; }
    public TimeSlot? TimeSlot { get; init; }
    public int DayDelta { get; init; }
    public string? Error { get; init; }
}

/// <summary>
/// Result for the 'meeting' command.
/// </summary>
public sealed record MeetingResult
{
    public required IReadOnlyList<string> ParticipantQueries { get; init; }
    public required Duration Duration { get; init; }
    public required WorkingHours WorkingHours { get; init; }
    public required IReadOnlyList<MeetingSlot> IdealSlots { get; init; }
    public required IReadOnlyList<MeetingSlot> CompromiseSlots { get; init; }
}
