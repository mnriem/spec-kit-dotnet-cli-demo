# Data Model: Timezone Meeting Utility

**Feature**: 001-timezone-utility  
**Date**: March 2, 2026  
**Source**: Extracted from [spec.md](spec.md) Key Entities

---

## Core Domain Entities

### Location

A place that can be resolved to a timezone. Can be specified by IANA timezone ID, city name, country name, or US zip code.

```csharp
public sealed record Location
{
    /// <summary>Display name for the location (e.g., "New York, NY" or "Tokyo, Japan")</summary>
    public required string DisplayName { get; init; }
    
    /// <summary>IANA timezone identifier (e.g., "America/New_York")</summary>
    public required string TimezoneId { get; init; }
    
    /// <summary>Current UTC offset (changes with DST)</summary>
    public Offset CurrentOffset { get; init; }
    
    /// <summary>Country code (ISO 3166-1 alpha-2)</summary>
    public string? CountryCode { get; init; }
    
    /// <summary>Original query that resolved to this location</summary>
    public string? OriginalQuery { get; init; }
}
```

**Invariants**:
- `TimezoneId` must be a valid IANA timezone identifier from TZDB
- `DisplayName` must be non-empty
- `CurrentOffset` is computed at query time, not stored

**Relationships**:
- One Location has one Timezone (resolved)
- One Location may belong to one Profile (many-to-many via Profile)

---

### Timezone

An IANA timezone identifier with associated rules for UTC offset and DST transitions. Uses NodaTime's `DateTimeZone` internally.

```csharp
/// <summary>
/// Thin wrapper around NodaTime.DateTimeZone providing domain operations
/// </summary>
public sealed class Timezone
{
    private readonly DateTimeZone _zone;
    
    /// <summary>IANA timezone identifier (e.g., "America/New_York")</summary>
    public string Id => _zone.Id;
    
    /// <summary>Standard abbreviation (e.g., "EST", "PST")</summary>
    public string Abbreviation { get; }
    
    /// <summary>Standard offset from UTC (ignoring DST)</summary>
    public Offset StandardOffset => _zone.GetZoneInterval(SystemClock.Instance.GetCurrentInstant()).StandardOffset;
    
    /// <summary>Whether timezone currently observes DST</summary>
    public bool IsDaylightSavingTime { get; }
    
    public ZonedDateTime GetCurrentTime(IClock clock) => clock.GetCurrentInstant().InZone(_zone);
    public ZonedDateTime AtInstant(Instant instant) => instant.InZone(_zone);
}
```

**Invariants**:
- `Id` must exist in the TZDB provider
- `Abbreviation` derived from current zone interval

---

### TimeSlot

A specific datetime in a specific timezone, with ability to convert to other zones.

```csharp
public sealed record TimeSlot
{
    /// <summary>The underlying instant in UTC</summary>
    public required Instant Instant { get; init; }
    
    /// <summary>The timezone for display</summary>
    public required DateTimeZone Zone { get; init; }
    
    /// <summary>The zoned representation</summary>
    public ZonedDateTime ZonedDateTime => Instant.InZone(Zone);
    
    /// <summary>Local date and time in the zone</summary>
    public LocalDateTime LocalDateTime => ZonedDateTime.LocalDateTime;
    
    /// <summary>Date component only</summary>
    public LocalDate Date => LocalDateTime.Date;
    
    /// <summary>Time component only</summary>
    public LocalTime Time => LocalDateTime.TimeOfDay;
    
    /// <summary>Formatted offset (e.g., "-05:00")</summary>
    public string OffsetString => ZonedDateTime.Offset.ToString();
    
    /// <summary>Convert to another timezone</summary>
    public TimeSlot InZone(DateTimeZone targetZone) => this with { Zone = targetZone };
    
    /// <summary>Check if time falls within working hours</summary>
    public bool IsWithinWorkingHours(WorkingHours hours) => hours.Contains(Time);
}
```

**Invariants**:
- `Instant` is timezone-agnostic (UTC-based)
- `Zone` determines local representation
- Converting zones changes `Zone` but preserves `Instant`

---

### Profile

A user-defined named collection of locations for quick access.

```csharp
public sealed record Profile
{
    /// <summary>Unique identifier for the profile</summary>
    public required string Name { get; init; }
    
    /// <summary>Display description (optional)</summary>
    public string? Description { get; init; }
    
    /// <summary>Ordered list of locations in this profile</summary>
    public required IReadOnlyList<ProfileLocation> Locations { get; init; }
    
    /// <summary>When the profile was created</summary>
    public Instant CreatedAt { get; init; }
    
    /// <summary>When the profile was last modified</summary>
    public Instant? ModifiedAt { get; init; }
}

public sealed record ProfileLocation
{
    /// <summary>Display name override (or null to use resolved name)</summary>
    public string? DisplayName { get; init; }
    
    /// <summary>The query to resolve (IANA ID, city, zip, etc.)</summary>
    public required string Query { get; init; }
    
    /// <summary>Custom working hours for this location in this profile</summary>
    public WorkingHours? CustomWorkingHours { get; init; }
}
```

**Invariants**:
- `Name` must be unique across all profiles (case-insensitive)
- `Name` must be a valid identifier (alphanumeric + hyphens, 1-50 chars)
- `Locations` must have at least one entry

---

### WorkingHours

A range of acceptable hours (start time, end time) for a location, used in meeting optimization.

```csharp
public sealed record WorkingHours
{
    /// <summary>Start of working hours (inclusive)</summary>
    public required LocalTime Start { get; init; }
    
    /// <summary>End of working hours (exclusive)</summary>
    public required LocalTime End { get; init; }
    
    /// <summary>Days of the week when these hours apply</summary>
    public required IReadOnlySet<IsoDayOfWeek> WorkDays { get; init; }
    
    /// <summary>Default working hours: 9:00 AM - 6:00 PM, Monday-Friday</summary>
    public static WorkingHours Default => new()
    {
        Start = new LocalTime(9, 0),
        End = new LocalTime(18, 0),
        WorkDays = new HashSet<IsoDayOfWeek>
        {
            IsoDayOfWeek.Monday, IsoDayOfWeek.Tuesday, IsoDayOfWeek.Wednesday,
            IsoDayOfWeek.Thursday, IsoDayOfWeek.Friday
        }
    };
    
    /// <summary>Check if a time falls within working hours</summary>
    public bool Contains(LocalTime time) => time >= Start && time < End;
    
    /// <summary>Check if a day is a work day</summary>
    public bool IsWorkDay(IsoDayOfWeek day) => WorkDays.Contains(day);
}
```

**Invariants**:
- `Start` must be before `End` (no overnight ranges for simplicity)
- `WorkDays` must be non-empty

---

## Supporting Types

### ResolvedLocation

Result of resolving a user query to one or more locations.

```csharp
public sealed record ResolvedLocation
{
    /// <summary>The original user query</summary>
    public required string Query { get; init; }
    
    /// <summary>Resolution status</summary>
    public required ResolutionStatus Status { get; init; }
    
    /// <summary>Matched locations (may be multiple for ambiguous queries)</summary>
    public required IReadOnlyList<Location> Matches { get; init; }
    
    /// <summary>Suggestions for invalid queries</summary>
    public IReadOnlyList<string>? Suggestions { get; init; }
}

public enum ResolutionStatus
{
    /// <summary>Exact single match found</summary>
    ExactMatch,
    
    /// <summary>Multiple matches found, user must disambiguate</summary>
    Ambiguous,
    
    /// <summary>No match found</summary>
    NotFound
}
```

---

### MeetingSlot

A suggested meeting time slot with participant availability info.

```csharp
public sealed record MeetingSlot
{
    /// <summary>The proposed meeting start time (as an instant)</summary>
    public required Instant StartTime { get; init; }
    
    /// <summary>Duration of the meeting</summary>
    public required Duration Duration { get; init; }
    
    /// <summary>End time (computed)</summary>
    public Instant EndTime => StartTime + Duration;
    
    /// <summary>Availability for each participant location</summary>
    public required IReadOnlyList<ParticipantAvailability> Participants { get; init; }
    
    /// <summary>Quality score (higher = more participants in working hours)</summary>
    public int Score => Participants.Count(p => p.IsWithinWorkingHours);
    
    /// <summary>Whether all participants are within working hours</summary>
    public bool IsIdeal => Participants.All(p => p.IsWithinWorkingHours);
}

public sealed record ParticipantAvailability
{
    /// <summary>The participant's location</summary>
    public required Location Location { get; init; }
    
    /// <summary>Meeting time in participant's local timezone</summary>
    public required TimeSlot LocalTime { get; init; }
    
    /// <summary>Whether this time is within participant's working hours</summary>
    public required bool IsWithinWorkingHours { get; init; }
    
    /// <summary>Status indicator (business hours, evening, night)</summary>
    public required TimeOfDayStatus Status { get; init; }
}

public enum TimeOfDayStatus
{
    BusinessHours,  // Within working hours
    Evening,        // After work but before midnight
    Night,          // After midnight before work starts
    Weekend         // Not a work day
}
```

---

## State Transitions

### Profile Lifecycle

```
[Not Exists] --create--> [Created] --update--> [Modified] --delete--> [Not Exists]
```

- Profiles are stored in user config file
- No draft/published states - changes are immediate
- Deletion is permanent (no soft delete)

### Location Resolution Flow

```
[User Query] --> [Resolver] --> [ExactMatch] --> [Location]
                            --> [Ambiguous]  --> [User Selection] --> [Location]
                            --> [NotFound]   --> [Error + Suggestions]
```

---

## Validation Rules

| Entity | Field | Rule |
|--------|-------|------|
| Location | TimezoneId | Must be valid IANA identifier in TZDB |
| Location | DisplayName | Non-empty, max 100 chars |
| Profile | Name | 1-50 chars, alphanumeric + hyphens, case-insensitive unique |
| Profile | Locations | At least 1 location required |
| WorkingHours | Start/End | Start must be before End |
| WorkingHours | WorkDays | At least 1 day required |
| TimeSlot | Zone | Must be valid IANA timezone |

---

## Data Storage

### User Configuration File

Location: `~/.tzutil/config.json`

```json
{
  "version": 1,
  "preferences": {
    "defaultTimeFormat": "24h",
    "defaultWorkingHours": {
      "start": "09:00",
      "end": "18:00",
      "workDays": ["monday", "tuesday", "wednesday", "thursday", "friday"]
    }
  },
  "profiles": [
    {
      "name": "team-standup",
      "description": "Daily standup participants",
      "locations": [
        { "query": "America/New_York", "displayName": "NYC Office" },
        { "query": "Europe/London" },
        { "query": "Asia/Tokyo", "customWorkingHours": { "start": "10:00", "end": "19:00" } }
      ],
      "createdAt": "2026-03-02T10:00:00Z"
    }
  ]
}
```

### Bundled Reference Data

Location: Embedded resources in assembly

- `cities.json.gz` - City name to timezone mappings
- `zipcodes.json.gz` - US ZIP code mappings
- `countries.json.gz` - Country to timezone mappings
