using Spectre.Console;

namespace TimezoneUtility.Output;

/// <summary>
/// Handles error formatting and display.
/// </summary>
public static class ErrorHandler
{
    /// <summary>
    /// Writes an error message for an unrecognized location.
    /// </summary>
    public static void WriteUnknownLocationError(string query, bool jsonOutput = false)
    {
        var suggestions = GetLocationSuggestions(query);
        var message = $"Could not find location: '{query}'";

        if (jsonOutput)
        {
            var errorObj = new
            {
                error = message,
                code = "LOCATION_NOT_FOUND",
                query,
                suggestions = suggestions.Count > 0 ? suggestions : null
            };
            Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(errorObj));
        }
        else
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] {Markup.Escape(message)}");
            if (suggestions.Count > 0)
            {
                AnsiConsole.MarkupLine("[dim]Did you mean:[/]");
                foreach (var suggestion in suggestions)
                {
                    AnsiConsole.MarkupLine($"  [cyan]{Markup.Escape(suggestion)}[/]");
                }
            }
            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine("[dim]Accepted formats:[/]");
            AnsiConsole.MarkupLine("  [cyan]IANA timezone[/]: America/New_York, Europe/London");
            AnsiConsole.MarkupLine("  [cyan]City name[/]: Tokyo, \"New York\", Paris");
            AnsiConsole.MarkupLine("  [cyan]US ZIP code[/]: 90210, 10001");
            AnsiConsole.MarkupLine("  [cyan]Country name[/]: Japan, Germany, UK");
        }
    }

    /// <summary>
    /// Writes an error message for invalid input.
    /// </summary>
    public static void WriteInvalidInputError(string message, bool jsonOutput = false)
    {
        if (jsonOutput)
        {
            var errorObj = new
            {
                error = message,
                code = "INVALID_INPUT"
            };
            Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(errorObj));
        }
        else
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] {Markup.Escape(message)}");
        }
    }

    /// <summary>
    /// Writes an error message for internal errors.
    /// </summary>
    public static void WriteInternalError(Exception ex, bool jsonOutput = false, bool verbose = false)
    {
        if (jsonOutput)
        {
            var errorObj = new
            {
                error = "An internal error occurred",
                code = "INTERNAL_ERROR",
                details = verbose ? ex.Message : null
            };
            Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(errorObj));
        }
        else
        {
            AnsiConsole.MarkupLine("[red]Error:[/] An internal error occurred");
            if (verbose)
            {
                AnsiConsole.WriteException(ex);
            }
        }
    }

    /// <summary>
    /// Writes a warning about ambiguous location match.
    /// </summary>
    public static void WriteAmbiguousLocationWarning(string query, IReadOnlyList<string> alternatives, bool jsonOutput = false)
    {
        if (jsonOutput)
        {
            var warningObj = new
            {
                warning = $"Ambiguous location: '{query}'",
                code = "AMBIGUOUS_LOCATION",
                alternatives
            };
            Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(warningObj));
        }
        else
        {
            AnsiConsole.MarkupLine($"[yellow]Warning:[/] Multiple locations match '{Markup.Escape(query)}':");
            foreach (var alt in alternatives)
            {
                AnsiConsole.MarkupLine($"  - {Markup.Escape(alt)}");
            }
            AnsiConsole.MarkupLine("[dim]Using the first match. Be more specific to avoid ambiguity.[/]");
        }
    }

    private static List<string> GetLocationSuggestions(string query)
    {
        // Simple suggestions based on common patterns
        var suggestions = new List<string>();
        var normalized = query.Trim().ToLowerInvariant();

        // Suggest IANA format if looks like a partial timezone
        if (normalized.Contains(' ') && !normalized.Contains('/'))
        {
            var parts = query.Split(' ');
            if (parts.Length == 2)
            {
                suggestions.Add($"Try: \"{parts[0]}_{parts[1]}\" or wrap in quotes");
            }
        }

        // Common typo suggestions
        var typoMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["newyork"] = "New York",
            ["losangeles"] = "Los Angeles",
            ["sanfrancisco"] = "San Francisco",
            ["hongkong"] = "Hong Kong",
            ["newyorkcity"] = "New York",
        };

        var withoutSpaces = normalized.Replace(" ", "");
        if (typoMap.TryGetValue(withoutSpaces, out var suggestion))
        {
            suggestions.Add(suggestion);
        }

        return suggestions;
    }
}
