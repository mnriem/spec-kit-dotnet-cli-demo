# Research Summary: Timezone Meeting Utility

**Feature**: 001-timezone-utility  
**Date**: March 2, 2026  
**Purpose**: Technical research for cross-platform timezone CLI utility in .NET 8

---

## 1. NodaTime vs DateTimeOffset for Timezone Handling

### Decision: **NodaTime**

NodaTime is the recommended library for timezone handling in this utility.

### Rationale

| Capability | NodaTime | DateTimeOffset/TimeZoneInfo |
|------------|----------|----------------------------|
| IANA timezone IDs | ✅ Native support (`America/New_York`) | ⚠️ Windows uses Windows IDs, requires mapping |
| DST handling | ✅ Explicit, handles ambiguous/skipped times | ⚠️ Implicit, can produce incorrect results |
| Time arithmetic | ✅ Separate types for concepts (Instant, LocalDateTime, ZonedDateTime) | ⚠️ Single DateTime type overloaded |
| Timezone database | ✅ Bundled TZDB, can update independently | ⚠️ OS-dependent, varies by platform |
| Cross-platform | ✅ Consistent behavior everywhere | ⚠️ Different timezone IDs on Windows vs Unix |

**Key NodaTime Types for This Project**:
- `Instant`: Moment in time (UTC), use for storage/APIs
- `ZonedDateTime`: Instant + timezone, use for display
- `LocalDateTime`: Date/time without timezone, use for user input
- `DateTimeZone`: Timezone definition, use for conversions
- `Duration`/`Period`: Time spans (machine vs calendar)

**IANA Timezone Lookup Pattern**:
```csharp
var tzdb = DateTimeZoneProviders.Tzdb;
DateTimeZone zone = tzdb.GetZoneOrNull("America/New_York") 
    ?? throw new UnknownTimezoneException("America/New_York");
ZonedDateTime now = SystemClock.Instance.GetCurrentInstant().InZone(zone);
```

**DST Handling Example**:
```csharp
// NodaTime explicitly handles DST transitions
var zone = DateTimeZoneProviders.Tzdb["America/New_York"];
var local = new LocalDateTime(2026, 3, 8, 2, 30, 0); // During DST gap
var resolved = local.InZoneLeniently(zone); // Resolves ambiguity predictably
```

### Alternatives Considered

| Alternative | Why Not Chosen |
|-------------|----------------|
| **System.DateTimeOffset** | No direct IANA support; Windows timezone IDs differ from Unix; DST handling is implicit and error-prone |
| **TimeZoneConverter** | Useful as a supplement for Windows↔IANA mapping, but doesn't provide NodaTime's type safety |
| **Humanizer** | Good for relative time formatting, can complement NodaTime but not replace it |

### Implementation Notes

- Bundle TZDB data with the application for offline operation
- Use `NodaTime.Serialization.SystemTextJson` for JSON serialization
- Consider `NodaTime.Testing` for time-dependent unit tests (fake clocks)
- NodaTime package size: ~2MB (acceptable within 50MB constraint)

---

## 2. System.CommandLine for CLI Application

### Decision: **System.CommandLine**

Use Microsoft's System.CommandLine library for CLI parsing and structure.

### Rationale

| Feature | System.CommandLine | Benefit |
|---------|-------------------|---------|
| Argument parsing | Strongly-typed, automatic validation | Type-safe command handlers |
| Help generation | Automatic `--help` for all commands | Consistent UX, no maintenance |
| Tab completion | Built-in support | Better developer experience |
| Middleware | Request pipeline pattern | Cross-cutting concerns (logging, error handling) |
| Testing | `CommandLineBuilder` testable | Easy integration testing |
| AOT compatibility | ✅ Supported in .NET 8 | Meets single-binary requirement |

**Recommended Command Structure**:
```
tzutil <command> [options]

Commands:
  now <location>           Get current time for location(s)
  convert <time> <from> <to>   Convert time between timezones
  meeting <locations>      Find optimal meeting times
  dashboard               Show world clock dashboard
  profile <action>        Manage saved location profiles

Global Options:
  --json                  Output as JSON (machine-readable)
  --verbose              Show detailed output
  --help                 Show help
  --version              Show version
```

**Command Implementation Pattern**:
```csharp
var nowCommand = new Command("now", "Get current time for a location")
{
    new Argument<string[]>("locations", "Location(s) to check")
};
nowCommand.SetHandler(async (string[] locations, bool json, IConsole console) =>
{
    var service = new TimeService();
    var results = await service.GetCurrentTimeAsync(locations);
    OutputFormatter.Write(console, results, json);
}, locationsArg, jsonOption, Bind.FromServiceProvider<IConsole>());
```

**Output Formatting Strategy**:
```csharp
// Human-readable (default)
Location          Current Time              Offset    Status
New York          2026-03-02 14:30 EST     -05:00    Business Hours
London            2026-03-02 19:30 GMT     +00:00    Evening
Tokyo             2026-03-03 04:30 JST     +09:00    Night

// Machine-readable (--json)
[{"location":"New York","datetime":"2026-03-02T14:30:00-05:00",...}]
```

### Alternatives Considered

| Alternative | Why Not Chosen |
|-------------|----------------|
| **Spectre.Console** | Excellent for rich TUI, but System.CommandLine better for command parsing; can complement for output |
| **CommandLineParser** | Popular but less actively maintained; no native AOT support |
| **ConsoleAppFramework** | Good performance but smaller ecosystem; System.CommandLine has Microsoft backing |
| **Raw args parsing** | Too error-prone; no automatic help generation |

### Implementation Notes

- Use `CommandLineBuilder` with `.UseDefaults()` for standard behaviors
- Implement `IConsole` for testable output
- Consider Spectre.Console for table formatting (compatible with System.CommandLine)
- Exit codes: 0 (success), 1 (invalid input), 2 (not found), 3 (internal error)

---

## 3. .NET 8 Single-File Publishing

### Decision: **Standard Single-File with ReadyToRun, Not Native AOT**

Use single-file publishing with ReadyToRun compilation instead of Native AOT.

### Rationale

| Aspect | Native AOT | Single-File + R2R | Choice |
|--------|-----------|-------------------|--------|
| Startup time | ~10-50ms | ~100-200ms | Both meet <200ms goal |
| Binary size | ~10-20MB | ~30-50MB | Both within 50MB limit |
| Reflection support | ⚠️ Limited | ✅ Full | **Single-File wins** |
| NodaTime compatibility | ⚠️ Requires trimming config | ✅ Works out of box | **Single-File wins** |
| JSON serialization | ⚠️ Source generators required | ✅ Reflection works | **Single-File wins** |
| Build complexity | Higher (trimming analysis) | Lower | **Single-File wins** |
| Runtime debugging | Limited | Full | **Single-File wins** |

**Native AOT Trade-offs**:
- NodaTime uses reflection for timezone data loading
- System.CommandLine middleware uses reflection
- JSON serialization with dynamic types requires source generators
- Significant trimming configuration needed

**Recommended csproj Configuration**:
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <OutputType>Exe</OutputType>
    <PublishSingleFile>true</PublishSingleFile>
    <SelfContained>true</SelfContained>
    <PublishReadyToRun>true</PublishReadyToRun>
    <EnableCompressionInSingleFile>true</EnableCompressionInSingleFile>
    <IncludeNativeLibrariesForSelfExtract>true</IncludeNativeLibrariesForSelfExtract>
    <InvariantGlobalization>false</InvariantGlobalization>
    <DebugType>embedded</DebugType>
  </PropertyGroup>
</Project>
```

### Platform/Architecture Matrix

| RID | Platform | Architecture | Priority | Notes |
|-----|----------|--------------|----------|-------|
| `win-x64` | Windows | x64 | P1 | Most common Windows |
| `linux-x64` | Linux | x64 | P1 | Servers, WSL |
| `osx-arm64` | macOS | Apple Silicon | P1 | Modern Macs |
| `osx-x64` | macOS | Intel | P2 | Older Macs |
| `linux-arm64` | Linux | ARM64 | P2 | Raspberry Pi, cloud ARM |
| `win-arm64` | Windows | ARM64 | P3 | Surface Pro X, etc. |

**Build Script Pattern**:
```bash
#!/bin/bash
RIDS=("win-x64" "linux-x64" "linux-arm64" "osx-x64" "osx-arm64")
for rid in "${RIDS[@]}"; do
  dotnet publish -c Release -r $rid -o ./dist/$rid
done
```

**CI/CD Considerations**:
- GitHub Actions: Use `matrix` strategy for parallel builds
- Each platform builds on native runner (windows-latest, ubuntu-latest, macos-latest)
- Cross-compilation works but native build preferred for testing

### Alternatives Considered

| Alternative | Why Not Chosen |
|-------------|----------------|
| **Native AOT** | Reflection limitations with NodaTime/System.CommandLine; complexity not justified for ~100ms startup improvement |
| **Framework-dependent** | Requires .NET runtime installed; violates single-binary constraint |
| **Docker container** | Not suitable for CLI tool distribution; platform-specific binaries simpler |

### Implementation Notes

- Test on all platforms before release (especially ARM64)
- Use GitHub Actions matrix for automated multi-platform builds
- Consider using `dotnet-releaser` for automated releases
- Binary naming convention: `tzutil-{rid}` (e.g., `tzutil-osx-arm64`)

---

## 4. Location Resolution to IANA Timezones

### Decision: **Bundled Static Data with GeoNames Source**

Bundle preprocessed location data from GeoNames for offline operation.

### Rationale

| Approach | Pros | Cons |
|----------|------|------|
| **Bundled static data** | Offline, fast, predictable, no API keys | Needs updates, larger binary |
| **Online API (Google/etc.)** | Always current, comprehensive | Requires internet, API costs, latency |
| **OS location services** | No bundling needed | Platform-specific, requires permissions |

**Data Sources**:

1. **GeoNames** (geonames.org) - *Chosen*
   - Free, open database with CC-BY license
   - Contains cities, admin regions, countries with timezones
   - ~15,000 cities with population >5,000
   - Includes lat/long for timezone calculation

2. **US ZIP Codes** (USPS/Census)
   - ZIP code to timezone mapping
   - ~42,000 ZIP codes
   - Map to IANA timezone via geographic centroid

3. **Country to Timezone** (CLDR)
   - Country codes to primary timezone
   - Handles multi-timezone countries (show options or use capital)

**Data Structure**:
```csharp
// Bundled as embedded JSON, loaded at startup
public record CityEntry(
    string Name,
    string[] AlternateNames,
    string Country,
    string AdminRegion,
    string IanaTimezone,
    int Population
);

public record ZipEntry(
    string ZipCode,
    string IanaTimezone,
    string PrimaryCity
);
```

**Resolution Priority**:
```
1. Exact IANA timezone match ("America/New_York")
2. US ZIP code match ("90210" → "America/Los_Angeles")
3. City name match ("Tokyo" → "Asia/Tokyo")
4. Country match ("Japan" → "Asia/Tokyo")
5. Fuzzy match with suggestions
```

**Ambiguity Handling**:
```csharp
// "Portland" returns multiple matches
var matches = resolver.Resolve("Portland");
// Returns: [Portland, OR → America/Los_Angeles, Portland, ME → America/New_York]
// CLI prompts user to select or shows all with disambiguation
```

**Data Size Estimates**:
| Dataset | Entries | Compressed Size |
|---------|---------|-----------------|
| Cities (pop >5k) | ~15,000 | ~500KB |
| US ZIP codes | ~42,000 | ~800KB |
| Countries | ~250 | ~5KB |
| Total | ~57,000 | **~1.5MB** |

### Alternatives Considered

| Alternative | Why Not Chosen |
|-------------|----------------|
| **Google Geocoding API** | Requires API key, internet, costs at scale |
| **GeoNames API** | Runtime dependency, rate limits, latency |
| **MaxMind GeoIP** | Designed for IP lookup, not name resolution |
| **TimeZoneDB API** | Good but requires internet connectivity |
| **Build-time code generation** | Considered for AOT, but JSON simpler |

### Implementation Notes

- Use `System.Text.Json` source generators for fast deserialization
- Lazy-load data on first location query (not at startup) for <200ms startup
- Consider trie or prefix tree for fast city name lookup
- Update data quarterly (automate with GitHub Action)
- Support `--update-data` command for manual updates (downloads latest)
- Embed as gzipped JSON resources

---

## 5. xUnit Testing Best Practices

### Decision: **xUnit + FluentAssertions + Coverlet + NSubstitute**

Use xUnit as the test framework with supporting libraries for assertions, coverage, and mocking.

### Rationale

| Library | Purpose | Why Chosen |
|---------|---------|------------|
| **xUnit** | Test framework | Modern, parallel execution, good .NET integration |
| **FluentAssertions** | Readable assertions | Better error messages, fluent syntax |
| **Coverlet** | Code coverage | Cross-platform, integrates with dotnet test |
| **NSubstitute** | Mocking | Simpler syntax than Moq, good for interfaces |
| **NodaTime.Testing** | Time mocking | FakeClock for deterministic time tests |

### Test Organization

```
tests/
├── TimezoneUtility.Unit/           # Fast, isolated tests
│   ├── Services/
│   │   ├── LocationResolverTests.cs
│   │   ├── TimeConversionTests.cs
│   │   └── MeetingOptimizerTests.cs
│   ├── Models/
│   │   └── WorkingHoursTests.cs
│   └── TimezoneUtility.Unit.csproj
│
├── TimezoneUtility.Integration/    # Full-stack tests
│   ├── Commands/
│   │   ├── NowCommandTests.cs
│   │   ├── ConvertCommandTests.cs
│   │   └── MeetingCommandTests.cs
│   └── TimezoneUtility.Integration.csproj
│
└── TimezoneUtility.Contract/       # CLI interface tests
    ├── CliOutputTests.cs           # Verifies output format
    ├── ExitCodeTests.cs            # Verifies exit codes
    └── TimezoneUtility.Contract.csproj
```

### Test Patterns

**Unit Test Pattern** (AAA - Arrange, Act, Assert):
```csharp
public class TimeConversionServiceTests
{
    private readonly FakeClock _clock;
    private readonly TimeConversionService _sut;
    
    public TimeConversionServiceTests()
    {
        _clock = new FakeClock(Instant.FromUtc(2026, 3, 2, 12, 0, 0));
        _sut = new TimeConversionService(_clock);
    }
    
    [Fact]
    public void Convert_FromNewYorkToLondon_ReturnsCorrectOffset()
    {
        // Arrange
        var sourceTime = new LocalDateTime(2026, 3, 2, 10, 0, 0);
        var sourceZone = "America/New_York";
        var targetZone = "Europe/London";
        
        // Act
        var result = _sut.Convert(sourceTime, sourceZone, targetZone);
        
        // Assert
        result.LocalDateTime.Should().Be(new LocalDateTime(2026, 3, 2, 15, 0, 0));
        result.Zone.Id.Should().Be("Europe/London");
    }
    
    [Theory]
    [InlineData("America/New_York", "EST", -5)]
    [InlineData("America/Los_Angeles", "PST", -8)]
    [InlineData("Europe/London", "GMT", 0)]
    public void GetCurrentTime_ReturnsCorrectOffset(string zone, string abbr, int hours)
    {
        var result = _sut.GetCurrentTime(zone);
        result.Offset.Should().Be(Offset.FromHours(hours));
    }
}
```

**Integration Test Pattern**:
```csharp
public class NowCommandIntegrationTests : IClassFixture<CliFixture>
{
    private readonly CliFixture _fixture;
    
    [Fact]
    public async Task Now_ValidTimezone_ReturnsCurrentTime()
    {
        // Act
        var result = await _fixture.RunAsync("now", "America/New_York");
        
        // Assert
        result.ExitCode.Should().Be(0);
        result.Output.Should().Contain("New York");
        result.Output.Should().MatchRegex(@"\d{4}-\d{2}-\d{2}");
    }
    
    [Fact]
    public async Task Now_InvalidLocation_ReturnsError()
    {
        var result = await _fixture.RunAsync("now", "InvalidPlace123");
        
        result.ExitCode.Should().Be(2); // Not found
        result.Error.Should().Contain("not found");
    }
}
```

**Contract Test Pattern** (CLI output format):
```csharp
public class JsonOutputContractTests
{
    [Fact]
    public async Task Now_JsonFlag_ReturnsValidJson()
    {
        var result = await Cli.RunAsync("now", "America/New_York", "--json");
        
        var action = () => JsonDocument.Parse(result.Output);
        action.Should().NotThrow();
        
        using var doc = JsonDocument.Parse(result.Output);
        doc.RootElement.GetProperty("location").GetString().Should().NotBeEmpty();
        doc.RootElement.GetProperty("datetime").GetString().Should().NotBeEmpty();
    }
}
```

### Coverage Configuration

**Coverlet Settings** (in test csproj):
```xml
<PropertyGroup>
  <CollectCoverage>true</CollectCoverage>
  <CoverletOutputFormat>cobertura,lcov</CoverletOutputFormat>
  <CoverletOutput>./coverage/</CoverletOutput>
  <ExcludeByAttribute>GeneratedCode,ExcludeFromCodeCoverage</ExcludeByAttribute>
  <Threshold>80</Threshold>
  <ThresholdType>line,branch</ThresholdType>
</PropertyGroup>
```

**Coverage Commands**:
```bash
# Run tests with coverage
dotnet test --collect:"XPlat Code Coverage"

# Generate HTML report
reportgenerator -reports:./coverage/*.xml -targetdir:./coverage/html

# Fail build if below 80%
dotnet test /p:Threshold=80 /p:ThresholdType=line
```

### What to Test (80% Coverage Strategy)

| Layer | Coverage Target | What to Test |
|-------|-----------------|--------------|
| **Services** | 90%+ | All public methods, edge cases, error conditions |
| **Models** | 85%+ | Validation logic, computed properties |
| **Commands** | 75%+ | Happy path, error handling, output format |
| **Data loading** | 70%+ | Parse success, malformed data handling |
| **Program.cs** | Exclude | Entry point, DI setup (tested via integration) |

**Exclude from Coverage**:
- Generated code (JSON serializers)
- Program.cs entry point
- Exception message formatting
- Debug-only code

### Alternatives Considered

| Alternative | Why Not Chosen |
|-------------|----------------|
| **NUnit** | Good but xUnit has better parallel execution |
| **MSTest** | Less community adoption, fewer features |
| **Moq** | More verbose than NSubstitute, similar capabilities |
| **Shouldly** | Good but FluentAssertions has richer API |
| **Fine Code Coverage** | VS-only, Coverlet is cross-platform |

### Implementation Notes

- Use `[Trait("Category", "Unit")]` and `[Trait("Category", "Integration")]` for filtering
- Run unit tests on every commit, integration tests on PR
- Use `TestServer` or process spawning for E2E CLI tests
- Consider mutation testing (Stryker.NET) for critical paths
- Set up coverage gates in CI (fail PR if coverage drops below 80%)

---

## Summary: Technology Stack

| Component | Choice | Package |
|-----------|--------|---------|
| **Framework** | .NET 8 | `net8.0` |
| **Timezone handling** | NodaTime | `NodaTime` (3.1+) |
| **CLI parsing** | System.CommandLine | `System.CommandLine` (2.0+) |
| **JSON serialization** | System.Text.Json | Built-in |
| **Console output** | Spectre.Console | `Spectre.Console` (0.48+) |
| **Testing framework** | xUnit | `xunit` (2.6+) |
| **Assertions** | FluentAssertions | `FluentAssertions` (6.12+) |
| **Mocking** | NSubstitute | `NSubstitute` (5.1+) |
| **Coverage** | Coverlet | `coverlet.collector` (6.0+) |
| **Location data** | GeoNames | Bundled JSON (~1.5MB) |

## Package References (csproj)

```xml
<!-- Main project -->
<ItemGroup>
  <PackageReference Include="NodaTime" Version="3.1.11" />
  <PackageReference Include="NodaTime.Serialization.SystemTextJson" Version="1.1.2" />
  <PackageReference Include="System.CommandLine" Version="2.0.0-beta4.22272.1" />
  <PackageReference Include="Spectre.Console" Version="0.48.0" />
</ItemGroup>

<!-- Test projects -->
<ItemGroup>
  <PackageReference Include="xunit" Version="2.6.6" />
  <PackageReference Include="xunit.runner.visualstudio" Version="2.5.6" />
  <PackageReference Include="FluentAssertions" Version="6.12.0" />
  <PackageReference Include="NSubstitute" Version="5.1.0" />
  <PackageReference Include="NodaTime.Testing" Version="3.1.11" />
  <PackageReference Include="coverlet.collector" Version="6.0.0" />
</ItemGroup>
```

---

## Open Questions / Risks

| Item | Risk Level | Mitigation |
|------|------------|------------|
| System.CommandLine still in beta | Low | Stable for years, widely used, Microsoft-backed |
| GeoNames data currency | Low | Quarterly updates, user can override |
| Binary size with bundled data | Low | ~35MB estimated, well under 50MB limit |
| ARM64 testing coverage | Medium | Use GitHub Actions ARM runners, manual testing |
| NodaTime TZDB updates | Low | NodaTime publishes regular updates, can bundle |
