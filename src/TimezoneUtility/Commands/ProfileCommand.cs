using System.CommandLine;
using System.Globalization;
using System.Text.Json;
using Spectre.Console;
using TimezoneUtility.Services;

namespace TimezoneUtility.Commands;

/// <summary>
/// Command handler for 'tzutil profile' - manage saved location profiles.
/// </summary>
public static class ProfileCommand
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    /// <summary>
    /// Creates the 'profile' command with subcommands.
    /// </summary>
    public static Command Create()
    {
        var command = new Command("profile", "Manage saved location profiles");

        command.AddCommand(CreateListCommand());
        command.AddCommand(CreateShowCommand());
        command.AddCommand(CreateCreateCommand());
        command.AddCommand(CreateDeleteCommand());
        command.AddCommand(CreateAddCommand());
        command.AddCommand(CreateRemoveCommand());

        return command;
    }

    private static Command CreateListCommand()
    {
        var jsonOption = new Option<bool>(["--json", "-j"], "Output as JSON");

        var command = new Command("list", "List all profiles");
        command.AddOption(jsonOption);

        command.SetHandler(json =>
        {
            var configManager = new ConfigManager();
            var profileService = new ProfileService(configManager);
            var profiles = profileService.GetAll();

            if (json)
            {
                var output = profiles.Select(p => new
                {
                    name = p.Name,
                    description = p.Description,
                    locationCount = p.Locations.Count
                });
                Console.WriteLine(JsonSerializer.Serialize(output, JsonOptions));
            }
            else
            {
                if (profiles.Count == 0)
                {
                    AnsiConsole.MarkupLine("[dim]No profiles saved. Create one with 'tzutil profile create <name> <locations...>'[/]");
                    return;
                }

                var table = new Table();
                table.AddColumn("Name");
                table.AddColumn("Locations");
                table.AddColumn("Description");

                foreach (var profile in profiles)
                {
                    table.AddRow(
                        Markup.Escape(profile.Name),
                        profile.Locations.Count.ToString(CultureInfo.InvariantCulture),
                        Markup.Escape(profile.Description ?? "-"));
                }

                AnsiConsole.Write(table);
            }
        }, jsonOption);

        return command;
    }

    private static Command CreateShowCommand()
    {
        var nameArg = new Argument<string>("name", "Profile name");
        var jsonOption = new Option<bool>(["--json", "-j"], "Output as JSON");

        var command = new Command("show", "Show profile details");
        command.AddArgument(nameArg);
        command.AddOption(jsonOption);

        command.SetHandler((name, json) =>
        {
            var configManager = new ConfigManager();
            var profileService = new ProfileService(configManager);
            var profile = profileService.Get(name);

            if (profile == null)
            {
                AnsiConsole.MarkupLine($"[red]Profile not found: {Markup.Escape(name)}[/]");
                Environment.ExitCode = 2;
                return;
            }

            if (json)
            {
                var output = new
                {
                    name = profile.Name,
                    description = profile.Description,
                    locations = profile.Locations.Select(l => new
                    {
                        query = l.Query,
                        displayName = l.DisplayName
                    })
                };
                Console.WriteLine(JsonSerializer.Serialize(output, JsonOptions));
            }
            else
            {
                AnsiConsole.MarkupLine($"[bold]Profile:[/] {Markup.Escape(profile.Name)}");
                if (profile.Description != null)
                {
                    AnsiConsole.MarkupLine($"[dim]Description:[/] {Markup.Escape(profile.Description)}");
                }
                AnsiConsole.WriteLine();
                AnsiConsole.MarkupLine("[bold]Locations:[/]");
                foreach (var location in profile.Locations)
                {
                    var display = location.DisplayName != null
                        ? $"{location.Query} ({location.DisplayName})"
                        : location.Query;
                    AnsiConsole.MarkupLine($"  - {Markup.Escape(display)}");
                }
            }
        }, nameArg, jsonOption);

        return command;
    }

    private static Command CreateCreateCommand()
    {
        var nameArg = new Argument<string>("name", "Profile name (alphanumeric, hyphens allowed)");
        var locationsArg = new Argument<string[]>("locations", "Locations to add to the profile")
        {
            Arity = ArgumentArity.OneOrMore
        };
        var descriptionOption = new Option<string?>(["--description", "-d"], "Profile description");

        var command = new Command("create", "Create a new profile");
        command.AddArgument(nameArg);
        command.AddArgument(locationsArg);
        command.AddOption(descriptionOption);

        command.SetHandler((name, locations, description) =>
        {
            var configManager = new ConfigManager();
            var profileService = new ProfileService(configManager);

            if (!profileService.Create(name, locations, description))
            {
                AnsiConsole.MarkupLine($"[red]Failed to create profile. Name may be invalid or already exists: {Markup.Escape(name)}[/]");
                Environment.ExitCode = 1;
                return;
            }

            AnsiConsole.MarkupLine($"[green]Created profile:[/] {Markup.Escape(name)} with {locations.Length} location(s)");
        }, nameArg, locationsArg, descriptionOption);

        return command;
    }

    private static Command CreateDeleteCommand()
    {
        var nameArg = new Argument<string>("name", "Profile name to delete");

        var command = new Command("delete", "Delete a profile");
        command.AddArgument(nameArg);

        command.SetHandler(name =>
        {
            var configManager = new ConfigManager();
            var profileService = new ProfileService(configManager);

            if (!profileService.Delete(name))
            {
                AnsiConsole.MarkupLine($"[red]Profile not found: {Markup.Escape(name)}[/]");
                Environment.ExitCode = 2;
                return;
            }

            AnsiConsole.MarkupLine($"[green]Deleted profile:[/] {Markup.Escape(name)}");
        }, nameArg);

        return command;
    }

    private static Command CreateAddCommand()
    {
        var nameArg = new Argument<string>("name", "Profile name");
        var locationArg = new Argument<string>("location", "Location to add");

        var command = new Command("add", "Add a location to a profile");
        command.AddArgument(nameArg);
        command.AddArgument(locationArg);

        command.SetHandler((name, location) =>
        {
            var configManager = new ConfigManager();
            var profileService = new ProfileService(configManager);

            if (!profileService.AddLocation(name, location))
            {
                AnsiConsole.MarkupLine($"[red]Failed to add location. Profile may not exist: {Markup.Escape(name)}[/]");
                Environment.ExitCode = 2;
                return;
            }

            AnsiConsole.MarkupLine($"[green]Added[/] {Markup.Escape(location)} to profile {Markup.Escape(name)}");
        }, nameArg, locationArg);

        return command;
    }

    private static Command CreateRemoveCommand()
    {
        var nameArg = new Argument<string>("name", "Profile name");
        var locationArg = new Argument<string>("location", "Location to remove");

        var command = new Command("remove", "Remove a location from a profile");
        command.AddArgument(nameArg);
        command.AddArgument(locationArg);

        command.SetHandler((name, location) =>
        {
            var configManager = new ConfigManager();
            var profileService = new ProfileService(configManager);

            if (!profileService.RemoveLocation(name, location))
            {
                AnsiConsole.MarkupLine($"[red]Failed to remove location. Profile or location may not exist.[/]");
                Environment.ExitCode = 2;
                return;
            }

            AnsiConsole.MarkupLine($"[green]Removed[/] {Markup.Escape(location)} from profile {Markup.Escape(name)}");
        }, nameArg, locationArg);

        return command;
    }
}
