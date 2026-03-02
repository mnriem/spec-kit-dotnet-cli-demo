using System.CommandLine;
using System.Text.Json;
using Spectre.Console;
using TimezoneUtility.Services;

namespace TimezoneUtility.Commands;

/// <summary>
/// Command handler for 'tzutil config' - manage user configuration.
/// </summary>
public static class ConfigCommand
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    /// <summary>
    /// Creates the 'config' command with subcommands.
    /// </summary>
    public static Command Create()
    {
        var command = new Command("config", "Manage user configuration");

        command.AddCommand(CreateShowCommand());
        command.AddCommand(CreateSetCommand());
        command.AddCommand(CreateResetCommand());

        return command;
    }

    private static Command CreateShowCommand()
    {
        var keyArg = new Argument<string?>("key", () => null, "Configuration key to show (optional)");
        var jsonOption = new Option<bool>(["--json", "-j"], "Output as JSON");

        var command = new Command("show", "Show configuration values");
        command.AddArgument(keyArg);
        command.AddOption(jsonOption);

        command.SetHandler((key, json) =>
        {
            var configManager = new ConfigManager();
            var config = configManager.GetConfig();

            if (key != null)
            {
                var value = configManager.GetValue(key);
                if (value == null)
                {
                    AnsiConsole.MarkupLine($"[red]Unknown configuration key: {Markup.Escape(key)}[/]");
                    Environment.ExitCode = 1;
                    return;
                }

                if (json)
                {
                    Console.WriteLine(JsonSerializer.Serialize(new { key, value }));
                }
                else
                {
                    AnsiConsole.MarkupLine($"[cyan]{Markup.Escape(key)}[/]: {Markup.Escape(value)}");
                }
            }
            else
            {
                if (json)
                {
                    var output = new
                    {
                        format = config.Preferences.DefaultTimeFormat,
                        workStart = config.Preferences.DefaultWorkingHours.Start.ToString(),
                        workEnd = config.Preferences.DefaultWorkingHours.End.ToString()
                    };
                    Console.WriteLine(JsonSerializer.Serialize(output, JsonOptions));
                }
                else
                {
                    var table = new Table();
                    table.AddColumn("Setting");
                    table.AddColumn("Value");

                    table.AddRow("format", config.Preferences.DefaultTimeFormat);
                    table.AddRow("work-start", config.Preferences.DefaultWorkingHours.Start.ToString());
                    table.AddRow("work-end", config.Preferences.DefaultWorkingHours.End.ToString());

                    AnsiConsole.Write(table);
                }
            }
        }, keyArg, jsonOption);

        return command;
    }

    private static Command CreateSetCommand()
    {
        var keyArg = new Argument<string>("key", "Configuration key to set");
        var valueArg = new Argument<string>("value", "Value to set");

        var command = new Command("set", "Set a configuration value");
        command.AddArgument(keyArg);
        command.AddArgument(valueArg);

        command.SetHandler((key, value) =>
        {
            var configManager = new ConfigManager();

            if (!configManager.SetValue(key, value))
            {
                AnsiConsole.MarkupLine($"[red]Failed to set configuration. Invalid key or value.[/]");
                AnsiConsole.MarkupLine("[dim]Valid keys: format (12h/24h), work-start (HH:MM), work-end (HH:MM)[/]");
                Environment.ExitCode = 1;
                return;
            }

            AnsiConsole.MarkupLine($"[green]Set[/] {Markup.Escape(key)} = {Markup.Escape(value)}");
        }, keyArg, valueArg);

        return command;
    }

    private static Command CreateResetCommand()
    {
        var command = new Command("reset", "Reset configuration to defaults");

        command.SetHandler(() =>
        {
            var configManager = new ConfigManager();
            configManager.Reset();
            AnsiConsole.MarkupLine("[green]Configuration reset to defaults[/]");
        });

        return command;
    }
}
