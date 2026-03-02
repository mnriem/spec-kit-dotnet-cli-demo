# CLI Interface Contract: tzutil

**Version**: 1.0.0  
**Date**: March 2, 2026  
**Status**: Draft

This document defines the command-line interface contract for the timezone utility.

---

## Global Options

All commands support these options:

| Option | Short | Type | Default | Description |
|--------|-------|------|---------|-------------|
| `--help` | `-h` | flag | - | Show command help |
| `--version` | - | flag | - | Show version information |
| `--json` | `-j` | flag | false | Output as JSON (machine-readable) |
| `--verbose` | `-v` | flag | false | Show detailed output |

---

## Commands

### `tzutil now`

Get current time for one or more locations.

```
tzutil now <locations...> [options]
```

**Arguments**:
| Name | Type | Required | Description |
|------|------|----------|-------------|
| `locations` | string[] | Yes | One or more locations (timezone, city, zip, country) |

**Options**:
| Option | Type | Default | Description |
|--------|------|---------|-------------|
| `--format` | enum | 24h | Time format: `12h` or `24h` |
| `--sort` | enum | input | Sort order: `input`, `time`, `name` |

**Exit Codes**:
| Code | Meaning |
|------|---------|
| 0 | Success |
| 1 | Invalid input (malformed location) |
| 2 | Location not found |
| 3 | Internal error |

**Human Output Example**:
```
$ tzutil now "America/New_York" "London" "90210"

Location              Time                    Offset   Status
────────────────────────────────────────────────────────────────
New York, NY          2026-03-02 14:30 EST    -05:00   Business Hours
London, UK            2026-03-02 19:30 GMT    +00:00   Evening
Los Angeles, CA       2026-03-02 11:30 PST    -08:00   Business Hours
```

**JSON Output Example**:
```json
{
  "timestamp": "2026-03-02T19:30:00Z",
  "results": [
    {
      "query": "America/New_York",
      "location": {
        "displayName": "New York, NY",
        "timezoneId": "America/New_York",
        "countryCode": "US"
      },
      "time": {
        "iso8601": "2026-03-02T14:30:00-05:00",
        "date": "2026-03-02",
        "time": "14:30:00",
        "offset": "-05:00",
        "abbreviation": "EST"
      },
      "status": "business_hours"
    }
  ]
}
```

---

### `tzutil convert`

Convert a time from one timezone to others.

```
tzutil convert <time> --from <zone> --to <zones...> [options]
```

**Arguments**:
| Name | Type | Required | Description |
|------|------|----------|-------------|
| `time` | string | Yes | Time to convert (ISO 8601 or natural: "3pm", "15:00", "2026-03-02 15:00") |

**Options**:
| Option | Type | Required | Default | Description |
|--------|------|----------|---------|-------------|
| `--from` | string | Yes | - | Source timezone/location |
| `--to` | string[] | Yes | - | Target timezone(s)/location(s) |
| `--date` | date | No | today | Date for the conversion |
| `--format` | enum | No | 24h | Output format: `12h` or `24h` |

**Exit Codes**:
| Code | Meaning |
|------|---------|
| 0 | Success |
| 1 | Invalid time format |
| 2 | Timezone not found |
| 3 | Internal error |

**Human Output Example**:
```
$ tzutil convert "15:00" --from "New York" --to "London" "Tokyo" "Sydney"

Converting 15:00 from New York (America/New_York)

Location              Time                    Date         Offset
──────────────────────────────────────────────────────────────────────
London, UK            20:00 GMT               Mar 2        +00:00
Tokyo, Japan          05:00 JST               Mar 3 (+1)   +09:00
Sydney, Australia     07:00 AEDT              Mar 3 (+1)   +11:00
```

**JSON Output Example**:
```json
{
  "source": {
    "time": "15:00:00",
    "date": "2026-03-02",
    "location": "New York",
    "timezoneId": "America/New_York",
    "iso8601": "2026-03-02T15:00:00-05:00"
  },
  "conversions": [
    {
      "location": "London, UK",
      "timezoneId": "Europe/London",
      "iso8601": "2026-03-02T20:00:00+00:00",
      "dayDelta": 0
    },
    {
      "location": "Tokyo, Japan",
      "timezoneId": "Asia/Tokyo",
      "iso8601": "2026-03-03T05:00:00+09:00",
      "dayDelta": 1
    }
  ]
}
```

---

### `tzutil meeting`

Find optimal meeting times across timezones.

```
tzutil meeting <locations...> [options]
```

**Arguments**:
| Name | Type | Required | Description |
|------|------|----------|-------------|
| `locations` | string[] | Yes | Participant locations (min 2) |

**Options**:
| Option | Type | Default | Description |
|--------|------|---------|-------------|
| `--duration` | duration | 1h | Meeting duration (e.g., "30m", "1h", "90m") |
| `--date` | date | today | Date to find times for |
| `--range` | int | 7 | Number of days to search |
| `--work-start` | time | 09:00 | Default working hours start |
| `--work-end` | time | 18:00 | Default working hours end |
| `--limit` | int | 5 | Maximum suggestions to show |

**Exit Codes**:
| Code | Meaning |
|------|---------|
| 0 | Success (found ideal times) |
| 0 | Success (found compromise times, with warning) |
| 1 | Invalid input |
| 2 | Location not found |
| 3 | Internal error |

**Human Output Example**:
```
$ tzutil meeting "New York" "London" "Tokyo" --duration 1h

Finding 1h meeting times for: New York, London, Tokyo
Working hours: 09:00-18:00 (default)

Best Times (all in working hours):
──────────────────────────────────────────────────────────────────────
#1  Mar 2   New York 09:00 EST  │  London 14:00 GMT  │  Tokyo 23:00 JST ⚠
#2  Mar 2   New York 08:00 EST  │  London 13:00 GMT  │  Tokyo 22:00 JST ⚠

⚠ Tokyo is outside working hours for all suggested times.

Compromise Times (minimizing impact):
──────────────────────────────────────────────────────────────────────
#1  Mar 2   New York 22:00 EST ⚠  │  London 03:00 GMT ⚠  │  Tokyo 12:00 JST ✓
```

**JSON Output Example**:
```json
{
  "request": {
    "participants": ["New York", "London", "Tokyo"],
    "duration": "PT1H",
    "workingHours": { "start": "09:00", "end": "18:00" }
  },
  "ideal": [],
  "compromise": [
    {
      "startTime": "2026-03-02T14:00:00Z",
      "duration": "PT1H",
      "score": 2,
      "participants": [
        {
          "location": "New York",
          "timezoneId": "America/New_York",
          "localTime": "09:00",
          "inWorkingHours": true
        },
        {
          "location": "London",
          "timezoneId": "Europe/London",
          "localTime": "14:00",
          "inWorkingHours": true
        },
        {
          "location": "Tokyo",
          "timezoneId": "Asia/Tokyo",
          "localTime": "23:00",
          "inWorkingHours": false
        }
      ]
    }
  ]
}
```

---

### `tzutil dashboard`

Show a world clock dashboard view.

```
tzutil dashboard [locations...] [options]
```

**Arguments**:
| Name | Type | Required | Description |
|------|------|----------|-------------|
| `locations` | string[] | No | Locations to display (uses default profile if omitted) |

**Options**:
| Option | Type | Default | Description |
|--------|------|---------|-------------|
| `--profile` | string | - | Use a saved profile |
| `--watch` | flag | false | Continuously update display |
| `--interval` | int | 60 | Update interval in seconds (with --watch) |

**Exit Codes**:
| Code | Meaning |
|------|---------|
| 0 | Success |
| 1 | Invalid input |
| 2 | Profile/location not found |
| 3 | Internal error |

**Human Output Example**:
```
$ tzutil dashboard --profile team-standup

World Clock Dashboard - team-standup
Updated: 2026-03-02 14:30:00 UTC

┌─────────────────┬──────────────────────┬────────┬──────────────┐
│ Location        │ Time                 │ Offset │ Status       │
├─────────────────┼──────────────────────┼────────┼──────────────┤
│ NYC Office      │ Mon 09:30 EST        │ -05:00 │ 🟢 Business  │
│ London          │ Mon 14:30 GMT        │ +00:00 │ 🟢 Business  │
│ Tokyo           │ Mon 23:30 JST        │ +09:00 │ 🔴 Night     │
└─────────────────┴──────────────────────┴────────┴──────────────┘
```

---

### `tzutil profile`

Manage saved location profiles.

```
tzutil profile <action> [name] [options]
```

**Subcommands**:

#### `tzutil profile list`
List all saved profiles.

#### `tzutil profile show <name>`
Show details of a specific profile.

#### `tzutil profile create <name> <locations...>`
Create a new profile.

**Options**:
| Option | Type | Description |
|--------|------|-------------|
| `--description` | string | Profile description |

#### `tzutil profile delete <name>`
Delete a profile.

**Options**:
| Option | Type | Description |
|--------|------|-------------|
| `--force` | flag | Skip confirmation prompt |

#### `tzutil profile add <name> <locations...>`
Add locations to an existing profile.

#### `tzutil profile remove <name> <locations...>`
Remove locations from a profile.

**Exit Codes**:
| Code | Meaning |
|------|---------|
| 0 | Success |
| 1 | Invalid input |
| 2 | Profile not found |
| 4 | Profile already exists (create) |
| 3 | Internal error |

---

### `tzutil config`

Manage user configuration.

```
tzutil config <action> [key] [value]
```

**Subcommands**:

#### `tzutil config show`
Show current configuration.

#### `tzutil config set <key> <value>`
Set a configuration value.

**Configurable Keys**:
| Key | Type | Values | Default |
|-----|------|--------|---------|
| `format` | enum | `12h`, `24h` | `24h` |
| `work-start` | time | HH:MM | `09:00` |
| `work-end` | time | HH:MM | `18:00` |

#### `tzutil config reset`
Reset to default configuration.

---

## Error Response Format

**Human Output**:
```
Error: Could not find timezone for "InvalidCity"

Did you mean one of these?
  - Indianapolis, IN (America/Indiana/Indianapolis)
  - Invalid Bay, CA (does not exist)

Try using an IANA timezone ID like "America/New_York" or a US zip code.
```

**JSON Output**:
```json
{
  "error": {
    "code": "LOCATION_NOT_FOUND",
    "message": "Could not find timezone for \"InvalidCity\"",
    "query": "InvalidCity",
    "suggestions": [
      { "name": "Indianapolis, IN", "timezoneId": "America/Indiana/Indianapolis" }
    ]
  }
}
```

---

## Version Information

```
$ tzutil --version
tzutil 1.0.0 (.NET 8.0.0)
TZDB version: 2024a
Platform: osx-arm64
```
