# Quickstart: Timezone Utility

**Feature**: 001-timezone-utility  
**Date**: March 2, 2026

This guide covers how to set up, build, test, and run the timezone utility.

---

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) (8.0.100 or later)
- Git
- IDE: Visual Studio 2022, VS Code with C# extension, or JetBrains Rider

**Verify installation**:
```bash
dotnet --version  # Should output 8.0.x
```

---

## Quick Setup

```bash
# Clone and enter the repository
git clone <repository-url>
cd spec-kit-demo-1

# Restore dependencies
dotnet restore

# Build
dotnet build

# Run tests
dotnet test

# Run the application
dotnet run --project src/TimezoneUtility -- now "America/New_York"
```

---

## Project Structure

```
spec-kit-demo-1/
├── src/
│   └── TimezoneUtility/           # Main CLI application
│       ├── Commands/              # Command implementations
│       ├── Models/                # Domain models
│       ├── Services/              # Business logic
│       ├── Data/                  # Bundled reference data
│       └── Program.cs             # Entry point
├── tests/
│   ├── TimezoneUtility.Unit/      # Unit tests
│   ├── TimezoneUtility.Integration/ # Integration tests
│   └── TimezoneUtility.Contract/  # CLI contract tests
├── specs/                         # Feature specifications
└── TimezoneUtility.sln            # Solution file
```

---

## Development Tasks

### Build

```bash
# Debug build (default)
dotnet build

# Release build
dotnet build -c Release
```

### Test

```bash
# Run all tests
dotnet test

# Run with coverage report
dotnet test --collect:"XPlat Code Coverage"

# Run specific test project
dotnet test tests/TimezoneUtility.Unit

# Run tests matching a filter
dotnet test --filter "FullyQualifiedName~LocationResolver"
```

### Run

```bash
# Run via dotnet run
dotnet run --project src/TimezoneUtility -- <command> [args]

# Examples
dotnet run --project src/TimezoneUtility -- now "Tokyo"
dotnet run --project src/TimezoneUtility -- convert "3pm" --from NYC --to London
dotnet run --project src/TimezoneUtility -- --help
```

### Format & Lint

```bash
# Check formatting
dotnet format --verify-no-changes

# Fix formatting
dotnet format
```

---

## Building Release Binaries

### Single Platform

```bash
# Build self-contained single-file binary
dotnet publish src/TimezoneUtility -c Release -r osx-arm64 --self-contained -o dist/osx-arm64
```

### All Platforms

```bash
# Build for all supported platforms
RIDS=("win-x64" "linux-x64" "linux-arm64" "osx-x64" "osx-arm64")
for rid in "${RIDS[@]}"; do
    dotnet publish src/TimezoneUtility -c Release -r $rid --self-contained -o dist/$rid
done
```

### Platform Matrix

| RID | Platform | Architecture | Binary Name |
|-----|----------|--------------|-------------|
| `win-x64` | Windows | x64 | `tzutil.exe` |
| `linux-x64` | Linux | x64 | `tzutil` |
| `linux-arm64` | Linux | ARM64 | `tzutil` |
| `osx-x64` | macOS | Intel | `tzutil` |
| `osx-arm64` | macOS | Apple Silicon | `tzutil` |

---

## Key Dependencies

| Package | Purpose | Version |
|---------|---------|---------|
| NodaTime | Timezone/datetime handling | 3.1+ |
| NodaTime.Serialization.SystemTextJson | JSON serialization | 3.1+ |
| System.CommandLine | CLI parsing | 2.0+ |
| Spectre.Console | Rich terminal output | 0.48+ |

**Test Dependencies**:
| Package | Purpose |
|---------|---------|
| xUnit | Test framework |
| FluentAssertions | Readable assertions |
| Coverlet | Code coverage |
| NSubstitute | Mocking |
| NodaTime.Testing | Time mocking |

---

## Configuration

User configuration is stored at `~/.tzutil/config.json`:

```json
{
  "version": 1,
  "preferences": {
    "defaultTimeFormat": "24h",
    "defaultWorkingHours": {
      "start": "09:00",
      "end": "18:00"
    }
  },
  "profiles": []
}
```

---

## Common Development Tasks

### Adding a New Command

1. Create command class in `src/TimezoneUtility/Commands/`
2. Register command in `Program.cs`
3. Add unit tests in `tests/TimezoneUtility.Unit/Commands/`
4. Add contract tests in `tests/TimezoneUtility.Contract/`

### Updating Timezone Data

The bundled TZDB data comes from NodaTime. To update:

```bash
# Update NodaTime package to get latest TZDB
dotnet add src/TimezoneUtility package NodaTime --version <latest>
```

### Updating Location Data

Location data (cities, ZIP codes) is bundled as embedded resources:

1. Download latest GeoNames data
2. Run preprocessing script: `scripts/update-geodata.sh`
3. Rebuild the project

---

## Testing Guidelines

### Unit Tests

- Test services and models in isolation
- Use `FakeClock` from NodaTime.Testing for time-dependent tests
- Use NSubstitute for mocking interfaces

```csharp
[Fact]
public void GetCurrentTime_ReturnsCorrectTimezone()
{
    // Arrange
    var clock = new FakeClock(Instant.FromUtc(2026, 3, 2, 12, 0, 0));
    var service = new TimeService(clock);
    
    // Act
    var result = service.GetCurrentTime("America/New_York");
    
    // Assert
    result.LocalDateTime.Hour.Should().Be(7); // 12:00 UTC = 07:00 EST
}
```

### Integration Tests

- Test full command execution
- Verify output format and exit codes
- Use real services, mock external dependencies

### Contract Tests

- Verify CLI interface stability
- Test JSON output schema
- Test error message format

---

## Troubleshooting

### "SDK not found"

Ensure .NET 8 SDK is installed and in PATH:
```bash
dotnet --list-sdks
```

### Test Coverage Below 80%

Check uncovered lines:
```bash
dotnet test --collect:"XPlat Code Coverage"
# View coverage report in tests/**/coverage.cobertura.xml
```

### Build Warnings

Treat warnings as errors in CI:
```bash
dotnet build -warnaserror
```

---

## Useful Commands Reference

```bash
# Quick test cycle
dotnet test --no-build

# Watch mode (rerun on changes)
dotnet watch test --project tests/TimezoneUtility.Unit

# Clean build artifacts
dotnet clean && dotnet build

# Check for package updates
dotnet list package --outdated
```
