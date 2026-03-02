using System.Text.Json;
using System.Text.Json.Serialization;
using NodaTime;
using NodaTime.Serialization.SystemTextJson;
using TimezoneUtility.Models;

namespace TimezoneUtility.Services;

/// <summary>
/// Manages user configuration stored at ~/.tzutil/config.json.
/// </summary>
public sealed class ConfigManager
{
    private readonly string _configDir;
    private readonly string _configPath;
    private readonly JsonSerializerOptions _jsonOptions;
    private UserConfig? _cachedConfig;

    /// <summary>
    /// Creates a new ConfigManager.
    /// </summary>
    public ConfigManager()
    {
        var homeDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        _configDir = Path.Combine(homeDir, ".tzutil");
        _configPath = Path.Combine(_configDir, "config.json");

        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };
        _jsonOptions.ConfigureForNodaTime(DateTimeZoneProviders.Tzdb);
    }

    /// <summary>
    /// Gets the current configuration, loading from disk if necessary.
    /// </summary>
    public UserConfig GetConfig()
    {
        if (_cachedConfig != null)
        {
            return _cachedConfig;
        }

        if (File.Exists(_configPath))
        {
            try
            {
                var json = File.ReadAllText(_configPath);
                _cachedConfig = JsonSerializer.Deserialize<UserConfig>(json, _jsonOptions) ?? CreateDefaultConfig();
            }
            catch
            {
                _cachedConfig = CreateDefaultConfig();
            }
        }
        else
        {
            _cachedConfig = CreateDefaultConfig();
        }

        return _cachedConfig;
    }

    /// <summary>
    /// Saves the configuration to disk.
    /// </summary>
    public void SaveConfig(UserConfig config)
    {
        Directory.CreateDirectory(_configDir);
        var json = JsonSerializer.Serialize(config, _jsonOptions);
        File.WriteAllText(_configPath, json);
        _cachedConfig = config;
    }

    /// <summary>
    /// Gets a configuration value.
    /// </summary>
    public string? GetValue(string key)
    {
        var config = GetConfig();
        return key.ToLowerInvariant() switch
        {
            "format" or "defaulttimeformat" => config.Preferences.DefaultTimeFormat,
            "work-start" or "workstart" => config.Preferences.DefaultWorkingHours.Start.ToString(),
            "work-end" or "workend" => config.Preferences.DefaultWorkingHours.End.ToString(),
            _ => null
        };
    }

    /// <summary>
    /// Sets a configuration value.
    /// </summary>
    public bool SetValue(string key, string value)
    {
        var config = GetConfig();

        switch (key.ToLowerInvariant())
        {
            case "format" or "defaulttimeformat":
                if (value != "12h" && value != "24h")
                {
                    return false;
                }
                config = config with
                {
                    Preferences = config.Preferences with { DefaultTimeFormat = value }
                };
                break;

            case "work-start" or "workstart":
                if (!TryParseTime(value, out var startTime))
                {
                    return false;
                }
                config = config with
                {
                    Preferences = config.Preferences with
                    {
                        DefaultWorkingHours = config.Preferences.DefaultWorkingHours with { Start = startTime }
                    }
                };
                break;

            case "work-end" or "workend":
                if (!TryParseTime(value, out var endTime))
                {
                    return false;
                }
                config = config with
                {
                    Preferences = config.Preferences with
                    {
                        DefaultWorkingHours = config.Preferences.DefaultWorkingHours with { End = endTime }
                    }
                };
                break;

            default:
                return false;
        }

        SaveConfig(config);
        return true;
    }

    /// <summary>
    /// Resets configuration to defaults.
    /// </summary>
    public void Reset()
    {
        var defaultConfig = CreateDefaultConfig();
        SaveConfig(defaultConfig);
    }

    private static UserConfig CreateDefaultConfig()
    {
        return new UserConfig
        {
            Version = 1,
            Preferences = new UserPreferences
            {
                DefaultTimeFormat = "24h",
                DefaultWorkingHours = WorkingHoursConfig.Default
            },
            Profiles = []
        };
    }

    private static bool TryParseTime(string value, out LocalTime time)
    {
        time = default;
        var parts = value.Split(':');
        if (parts.Length != 2)
        {
            return false;
        }

        if (!int.TryParse(parts[0], out var hour) || !int.TryParse(parts[1], out var minute))
        {
            return false;
        }

        if (hour < 0 || hour > 23 || minute < 0 || minute > 59)
        {
            return false;
        }

        time = new LocalTime(hour, minute);
        return true;
    }
}

/// <summary>
/// User configuration structure.
/// </summary>
public sealed record UserConfig
{
    public int Version { get; init; } = 1;
    public required UserPreferences Preferences { get; init; }
    public required List<ProfileConfig> Profiles { get; init; }
}

/// <summary>
/// User preferences.
/// </summary>
public sealed record UserPreferences
{
    public required string DefaultTimeFormat { get; init; }
    public required WorkingHoursConfig DefaultWorkingHours { get; init; }
}

/// <summary>
/// Working hours configuration (JSON-serializable).
/// </summary>
public sealed record WorkingHoursConfig
{
    public required LocalTime Start { get; init; }
    public required LocalTime End { get; init; }

    public static WorkingHoursConfig Default => new()
    {
        Start = new LocalTime(9, 0),
        End = new LocalTime(18, 0)
    };

    public WorkingHours ToWorkingHours() => new()
    {
        Start = Start,
        End = End,
        WorkDays = new HashSet<IsoDayOfWeek>
        {
            IsoDayOfWeek.Monday,
            IsoDayOfWeek.Tuesday,
            IsoDayOfWeek.Wednesday,
            IsoDayOfWeek.Thursday,
            IsoDayOfWeek.Friday
        }
    };
}

/// <summary>
/// Profile configuration (JSON-serializable).
/// </summary>
public sealed record ProfileConfig
{
    public required string Name { get; init; }
    public string? Description { get; init; }
    public required List<ProfileLocationConfig> Locations { get; init; }
    public Instant CreatedAt { get; init; }
    public Instant? ModifiedAt { get; init; }
}

/// <summary>
/// Profile location configuration (JSON-serializable).
/// </summary>
public sealed record ProfileLocationConfig
{
    public string? DisplayName { get; init; }
    public required string Query { get; init; }
}
