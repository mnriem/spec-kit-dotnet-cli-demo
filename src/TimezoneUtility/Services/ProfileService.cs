using NodaTime;
using TimezoneUtility.Models;

namespace TimezoneUtility.Services;

/// <summary>
/// Service for managing user profiles.
/// </summary>
public sealed class ProfileService
{
    private readonly ConfigManager _configManager;
    private readonly IClock _clock;

    /// <summary>
    /// Creates a new ProfileService.
    /// </summary>
    public ProfileService(ConfigManager configManager, IClock? clock = null)
    {
        _configManager = configManager;
        _clock = clock ?? SystemClock.Instance;
    }

    /// <summary>
    /// Gets all profiles.
    /// </summary>
    public IReadOnlyList<Profile> GetAll()
    {
        var config = _configManager.GetConfig();
        return config.Profiles.Select(ToProfile).ToList();
    }

    /// <summary>
    /// Gets a profile by name.
    /// </summary>
    public Profile? Get(string name)
    {
        var config = _configManager.GetConfig();
        var profileConfig = config.Profiles.Find(p =>
            p.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

        return profileConfig != null ? ToProfile(profileConfig) : null;
    }

    /// <summary>
    /// Creates a new profile.
    /// </summary>
    public bool Create(string name, IEnumerable<string> locations, string? description = null)
    {
        var config = _configManager.GetConfig();

        // Check for duplicate name
        if (config.Profiles.Any(p => p.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
        {
            return false;
        }

        // Validate name
        if (!IsValidProfileName(name))
        {
            return false;
        }

        var newProfile = new ProfileConfig
        {
            Name = name,
            Description = description,
            Locations = locations.Select(q => new ProfileLocationConfig { Query = q }).ToList(),
            CreatedAt = _clock.GetCurrentInstant()
        };

        var updatedProfiles = new List<ProfileConfig>(config.Profiles) { newProfile };
        var updatedConfig = config with { Profiles = updatedProfiles };
        _configManager.SaveConfig(updatedConfig);

        return true;
    }

    /// <summary>
    /// Deletes a profile.
    /// </summary>
    public bool Delete(string name)
    {
        var config = _configManager.GetConfig();
        var index = config.Profiles.FindIndex(p =>
            p.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

        if (index < 0)
        {
            return false;
        }

        var updatedProfiles = new List<ProfileConfig>(config.Profiles);
        updatedProfiles.RemoveAt(index);
        var updatedConfig = config with { Profiles = updatedProfiles };
        _configManager.SaveConfig(updatedConfig);

        return true;
    }

    /// <summary>
    /// Adds a location to a profile.
    /// </summary>
    public bool AddLocation(string profileName, string location)
    {
        var config = _configManager.GetConfig();
        var index = config.Profiles.FindIndex(p =>
            p.Name.Equals(profileName, StringComparison.OrdinalIgnoreCase));

        if (index < 0)
        {
            return false;
        }

        var profile = config.Profiles[index];
        var updatedLocations = new List<ProfileLocationConfig>(profile.Locations)
        {
            new() { Query = location }
        };
        var updatedProfile = profile with
        {
            Locations = updatedLocations,
            ModifiedAt = _clock.GetCurrentInstant()
        };

        var updatedProfiles = new List<ProfileConfig>(config.Profiles);
        updatedProfiles[index] = updatedProfile;
        var updatedConfig = config with { Profiles = updatedProfiles };
        _configManager.SaveConfig(updatedConfig);

        return true;
    }

    /// <summary>
    /// Removes a location from a profile.
    /// </summary>
    public bool RemoveLocation(string profileName, string location)
    {
        var config = _configManager.GetConfig();
        var index = config.Profiles.FindIndex(p =>
            p.Name.Equals(profileName, StringComparison.OrdinalIgnoreCase));

        if (index < 0)
        {
            return false;
        }

        var profile = config.Profiles[index];
        var locationIndex = profile.Locations.FindIndex(l =>
            l.Query.Equals(location, StringComparison.OrdinalIgnoreCase));

        if (locationIndex < 0)
        {
            return false;
        }

        var updatedLocations = new List<ProfileLocationConfig>(profile.Locations);
        updatedLocations.RemoveAt(locationIndex);
        var updatedProfile = profile with
        {
            Locations = updatedLocations,
            ModifiedAt = _clock.GetCurrentInstant()
        };

        var updatedProfiles = new List<ProfileConfig>(config.Profiles);
        updatedProfiles[index] = updatedProfile;
        var updatedConfig = config with { Profiles = updatedProfiles };
        _configManager.SaveConfig(updatedConfig);

        return true;
    }

    private static Profile ToProfile(ProfileConfig config)
    {
        return new Profile
        {
            Name = config.Name,
            Description = config.Description,
            Locations = config.Locations.Select(l => new ProfileLocation
            {
                DisplayName = l.DisplayName,
                Query = l.Query
            }).ToList(),
            CreatedAt = config.CreatedAt,
            ModifiedAt = config.ModifiedAt
        };
    }

    private static bool IsValidProfileName(string name)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Length > 50)
        {
            return false;
        }

        return name.All(c => char.IsLetterOrDigit(c) || c == '-' || c == '_');
    }
}
