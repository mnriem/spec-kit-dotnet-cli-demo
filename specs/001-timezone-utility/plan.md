# Implementation Plan: Timezone Meeting Utility

**Branch**: `001-timezone-utility` | **Date**: 2026-03-02 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `/specs/001-timezone-utility/spec.md`

**Note**: This template is filled in by the `/speckit.plan` command. See `.specify/templates/plan-template.md` for the execution workflow.

## Summary

A cross-platform CLI utility that enables users to determine current date/time for any location using timezone identifiers, city names, US zip codes, or country names. Supports multi-timezone display, time conversion, and optimal meeting time suggestions. Built with .NET 8 for single-binary deployment across Windows, macOS, and Linux on x64 and ARM64 architectures.

## Technical Context

**Language/Version**: .NET 8 (C#) - Latest LTS with native AOT and single-file publishing support  
**Primary Dependencies**: NodaTime (timezone handling), System.CommandLine (CLI parsing), System.Text.Json (serialization)  
**Storage**: JSON file for user preferences and profiles (~/.tzutil/config.json)  
**Testing**: xUnit with Coverlet for code coverage, FluentAssertions for readable tests  
**Target Platform**: Cross-platform single binary - Windows (x64, arm64), macOS (x64, arm64), Linux (x64, arm64)  
**Project Type**: CLI  
**Performance Goals**: <500ms command response, <200ms startup time (per constitution)  
**Constraints**: Single self-contained binary, offline-capable with bundled IANA timezone data, <50MB binary size  
**Scale/Scope**: Individual user tool, support 10+ locations simultaneously, 400+ IANA timezones

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

### Pre-Research Gate (Phase 0 Entry)

| Principle | Gate Status | Notes |
|-----------|-------------|-------|
| **I. Code Quality** | ✅ PASS | Single responsibility functions, <300 LOC files, clear naming conventions planned |
| **II. Testing Standards** | ✅ PASS | xUnit + Coverlet for 80%+ coverage, unit/integration/contract tests defined |
| **III. UX Consistency** | ✅ PASS | Consistent CLI patterns, clear error messages, --json flag for machine output |
| **IV. Performance** | ✅ PASS | <500ms response, <200ms startup via ReadyToRun, bounded memory |

### Post-Design Gate (Phase 1 Exit) ✅

| Principle | Gate Status | Verification |
|-----------|-------------|--------------|
| **I. Code Quality** | ✅ PASS | Models use C# records for immutability, services organized by domain, data-model.md defines clear entity boundaries |
| **II. Testing Standards** | ✅ PASS | Test structure defined (Unit/Integration/Contract), NodaTime.Testing for time mocking, coverage patterns documented in quickstart.md |
| **III. UX Consistency** | ✅ PASS | CLI interface contract defines consistent command patterns, error formats, --json output schema in contracts/cli-interface.md |
| **IV. Performance** | ✅ PASS | Lazy-load location data (not at startup), ReadyToRun compilation, bounded data sets (~1.5MB compressed) |

**Quality Gates Alignment**:
- Linting: dotnet format --verify-no-changes
- Type Safety: C# strong typing, nullable reference types enabled
- Test Coverage: Coverlet with 80% minimum threshold
- Documentation: XML docs for public APIs

## Project Structure

### Documentation (this feature)

```text
specs/[###-feature]/
├── plan.md              # This file (/speckit.plan command output)
├── research.md          # Phase 0 output (/speckit.plan command)
├── data-model.md        # Phase 1 output (/speckit.plan command)
├── quickstart.md        # Phase 1 output (/speckit.plan command)
├── contracts/           # Phase 1 output (/speckit.plan command)
└── tasks.md             # Phase 2 output (/speckit.tasks command - NOT created by /speckit.plan)
```

### Source Code (repository root)

```text
src/
├── TimezoneUtility/              # Main CLI application
│   ├── Commands/                 # Command handlers (Now, Convert, Meeting, Dashboard)
│   ├── Models/                   # Domain models (Location, TimeSlot, Profile, WorkingHours)
│   ├── Services/                 # Business logic services
│   │   ├── LocationResolver/     # Location resolution (city, zip, country → timezone)
│   │   ├── TimeConversion/       # Time conversion logic
│   │   └── MeetingOptimizer/     # Optimal meeting time finder
│   ├── Data/                     # Bundled data (zip codes, city mappings)
│   └── Program.cs                # Entry point
└── TimezoneUtility.sln           # Solution file

tests/
├── TimezoneUtility.Unit/         # Unit tests for services and models
├── TimezoneUtility.Integration/  # Integration tests for full command flows
└── TimezoneUtility.Contract/     # Contract tests for CLI interface
```

**Structure Decision**: Single .NET solution with one main project and organized test projects. Services are domain-organized (LocationResolver, TimeConversion, MeetingOptimizer) to maintain single responsibility. Tests follow the xUnit convention with separate projects for unit, integration, and contract testing.

## Complexity Tracking

> **No constitution violations identified.** The design follows all core principles without requiring justifications.
