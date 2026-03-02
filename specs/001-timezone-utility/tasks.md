# Tasks: Timezone Meeting Utility

**Input**: Design documents from `/specs/001-timezone-utility/`  
**Prerequisites**: plan.md ✅, spec.md ✅, research.md ✅, data-model.md ✅, contracts/ ✅

**Tests**: Not explicitly requested in feature specification - test tasks omitted.

**Organization**: Tasks are grouped by user story to enable independent implementation and testing.

## Format: `[ID] [P?] [Story?] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3)
- Include exact file paths in descriptions

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Project initialization and .NET 8 solution structure

- [X] T001 Create solution and project structure with `dotnet new sln` and `dotnet new console` in src/TimezoneUtility/
- [X] T002 Configure TimezoneUtility.csproj with NodaTime, System.CommandLine, Spectre.Console, single-file publishing settings
- [X] T003 [P] Create test projects: tests/TimezoneUtility.Unit/, tests/TimezoneUtility.Integration/, tests/TimezoneUtility.Contract/
- [X] T004 [P] Configure Directory.Build.props with nullable reference types, implicit usings, warning-as-error
- [X] T005 [P] Add .editorconfig and dotnet format configuration

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core infrastructure that MUST be complete before ANY user story can be implemented

**⚠️ CRITICAL**: No user story work can begin until this phase is complete

- [X] T006 Create Program.cs with System.CommandLine root command structure in src/TimezoneUtility/Program.cs
- [X] T007 [P] Create Location record model in src/TimezoneUtility/Models/Location.cs
- [X] T008 [P] Create TimeSlot record model in src/TimezoneUtility/Models/TimeSlot.cs  
- [X] T009 [P] Create WorkingHours record model in src/TimezoneUtility/Models/WorkingHours.cs
- [X] T010 [P] Create ResolvedLocation record model in src/TimezoneUtility/Models/ResolvedLocation.cs
- [X] T011 Create ILocationResolver interface in src/TimezoneUtility/Services/LocationResolver/ILocationResolver.cs
- [X] T012 Create ITimeService interface in src/TimezoneUtility/Services/TimeConversion/ITimeService.cs
- [X] T013 [P] Bundle cities.json.gz reference data as embedded resource in src/TimezoneUtility/Data/ (N/A - using built-in mappings)
- [X] T014 [P] Bundle zipcodes.json.gz reference data as embedded resource in src/TimezoneUtility/Data/ (N/A - using built-in mappings)
- [X] T015 [P] Bundle countries.json.gz reference data as embedded resource in src/TimezoneUtility/Data/ (N/A - using built-in mappings)
- [X] T016 Create GeoDataLoader service for loading embedded data in src/TimezoneUtility/Services/LocationResolver/GeoDataLoader.cs (N/A - using built-in mappings)
- [X] T017 Create OutputFormatter for human/JSON output switching in src/TimezoneUtility/Output/OutputFormatter.cs
- [X] T018 [P] Create ErrorHandler for consistent error formatting in src/TimezoneUtility/Output/ErrorHandler.cs
- [X] T019 Create config file manager for ~/.tzutil/config.json in src/TimezoneUtility/Services/ConfigManager.cs

**Checkpoint**: Foundation ready - user story implementation can now begin

---

## Phase 3: User Story 1 - Get Current Time for Location (Priority: P1) 🎯 MVP

**Goal**: Users can query current time for any location using IANA timezone, city name, US zip code, or country

**Independent Test**: Run `tzutil now "America/New_York"`, `tzutil now "Tokyo"`, `tzutil now "90210"` and verify correct times displayed

### Implementation for User Story 1

- [X] T020 [US1] Implement IanaTimezoneResolver for direct IANA ID lookups in src/TimezoneUtility/Services/LocationResolver/IanaTimezoneResolver.cs
- [X] T021 [US1] Implement CityResolver for city name to timezone mapping in src/TimezoneUtility/Services/LocationResolver/CityResolver.cs
- [X] T022 [US1] Implement ZipCodeResolver for US ZIP code lookups in src/TimezoneUtility/Services/LocationResolver/ZipCodeResolver.cs
- [X] T023 [US1] Implement CountryResolver for country name lookups in src/TimezoneUtility/Services/LocationResolver/CountryResolver.cs
- [X] T024 [US1] Implement CompositeLocationResolver combining all resolvers with priority chain in src/TimezoneUtility/Services/LocationResolver/CompositeLocationResolver.cs
- [X] T025 [US1] Implement TimeService using NodaTime for current time retrieval in src/TimezoneUtility/Services/TimeConversion/TimeService.cs
- [X] T026 [US1] Create NowCommand handler with single location support in src/TimezoneUtility/Commands/NowCommand.cs
- [X] T027 [US1] Implement human-readable table output for now command using Spectre.Console
- [X] T028 [US1] Implement JSON output format for now command per cli-interface.md contract
- [X] T029 [US1] Add ambiguous location handling with disambiguation prompts
- [X] T030 [US1] Add helpful error messages for unrecognized locations with suggestions

**Checkpoint**: User Story 1 complete - can look up time for any single location

---

## Phase 4: User Story 2 - View Multiple Timezones Simultaneously (Priority: P2)

**Goal**: Users can query multiple locations at once and save/recall location groups as profiles

**Independent Test**: Run `tzutil now "New York" "London" "Tokyo"` and `tzutil profile create team NYC London Tokyo; tzutil now --profile team`

### Implementation for User Story 2

- [X] T031 [US2] Create Profile record model in src/TimezoneUtility/Models/Profile.cs
- [X] T032 [US2] Create ProfileLocation record model in src/TimezoneUtility/Models/ProfileLocation.cs
- [X] T033 [US2] Implement ProfileService for CRUD operations on profiles in src/TimezoneUtility/Services/ProfileService.cs
- [X] T034 [US2] Extend NowCommand to accept multiple locations with table display
- [X] T035 [US2] Add --profile option to NowCommand for using saved profiles
- [X] T036 [US2] Create ProfileCommand handler with list, show, create, delete, add, remove subcommands in src/TimezoneUtility/Commands/ProfileCommand.cs
- [X] T037 [US2] Implement partial failure handling (show valid results + errors for invalid locations)
- [X] T038 [US2] Add --sort option to NowCommand (input, time, name)

**Checkpoint**: User Story 2 complete - can view multiple timezones and manage profiles

---

## Phase 5: User Story 3 - Convert Time Between Zones (Priority: P3)

**Goal**: Users can convert a specific datetime from one timezone to others

**Independent Test**: Run `tzutil convert "15:00" --from "New York" --to "London" "Tokyo"` and verify correct conversions

### Implementation for User Story 3

- [X] T039 [US3] Create TimeParser for parsing various time input formats in src/TimezoneUtility/Services/TimeConversion/TimeParser.cs
- [X] T040 [US3] Extend TimeService with ConvertTime method handling DST transitions
- [X] T041 [US3] Create ConvertCommand handler in src/TimezoneUtility/Commands/ConvertCommand.cs
- [X] T042 [US3] Implement --from and --to options for convert command
- [X] T043 [US3] Implement --date option for specifying conversion date
- [X] T044 [US3] Add day boundary display (e.g., "+1" for next day) in output
- [X] T045 [US3] Implement --format option (12h/24h) for convert command

**Checkpoint**: User Story 3 complete - can convert times between any timezones

---

## Phase 6: User Story 4 - Find Optimal Meeting Times (Priority: P4)

**Goal**: Users can find time slots that work for all participants based on working hours

**Independent Test**: Run `tzutil meeting "New York" "London" "Tokyo" --duration 1h` and verify suggested slots

### Implementation for User Story 4

- [X] T046 [US4] Create MeetingSlot record model in src/TimezoneUtility/Models/MeetingSlot.cs
- [X] T047 [US4] Create ParticipantAvailability record model in src/TimezoneUtility/Models/ParticipantAvailability.cs
- [X] T048 [US4] Create IMeetingOptimizer interface in src/TimezoneUtility/Services/MeetingOptimizer/IMeetingOptimizer.cs
- [X] T049 [US4] Implement MeetingOptimizer service with working hours overlap algorithm in src/TimezoneUtility/Services/MeetingOptimizer/MeetingOptimizer.cs
- [X] T050 [US4] Create MeetingCommand handler in src/TimezoneUtility/Commands/MeetingCommand.cs
- [X] T051 [US4] Implement --duration option parsing (30m, 1h, 90m)
- [X] T052 [US4] Implement --work-start and --work-end options for custom working hours
- [X] T053 [US4] Implement --date and --range options for date range search
- [X] T054 [US4] Add compromise time suggestions when no ideal times exist
- [X] T055 [US4] Implement --limit option for max suggestions

**Checkpoint**: User Story 4 complete - can find optimal meeting times across timezones

---

## Phase 7: User Story 5 - World Clock Dashboard View (Priority: P5)

**Goal**: Users can see an at-a-glance dashboard of multiple locations with status indicators

**Independent Test**: Run `tzutil dashboard "New York" "London" "Tokyo"` and verify formatted display with business/evening/night status

### Implementation for User Story 5

- [X] T056 [US5] Create TimeOfDayStatus enum in src/TimezoneUtility/Models/TimeOfDayStatus.cs
- [X] T057 [US5] Add GetTimeOfDayStatus method to TimeService
- [X] T058 [US5] Create DashboardCommand handler in src/TimezoneUtility/Commands/DashboardCommand.cs
- [X] T059 [US5] Implement dashboard table with status indicators (🟢 Business, 🟡 Evening, 🔴 Night)
- [X] T060 [US5] Add --profile option to dashboard command
- [X] T061 [US5] Implement --watch option with periodic refresh using Spectre.Console live display
- [X] T062 [US5] Implement --interval option for refresh rate

**Checkpoint**: User Story 5 complete - can view world clock dashboard

---

## Phase 8: Polish & Cross-Cutting Concerns

**Purpose**: Configuration, documentation, and cross-platform builds

- [X] T063 [P] Create ConfigCommand handler for preferences in src/TimezoneUtility/Commands/ConfigCommand.cs
- [X] T064 Implement config set/show/reset subcommands for format, work-start, work-end
- [X] T065 [P] Add --version global option showing version, TZDB version, platform
- [X] T066 [P] Add XML documentation to all public APIs
- [X] T067 [P] Create build script for multi-platform publishing in scripts/build-all.sh
- [X] T068 Run quickstart.md validation - verify all commands work as documented
- [X] T069 [P] Add GitHub Actions workflow for CI/CD with multi-platform matrix in .github/workflows/build.yml

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies - can start immediately
- **Foundational (Phase 2)**: Depends on Setup completion - BLOCKS all user stories
- **User Stories (Phase 3-7)**: All depend on Foundational phase completion
  - Can proceed in parallel or sequentially in priority order (P1 → P2 → P3 → P4 → P5)
- **Polish (Phase 8)**: Can start after US1, benefits from all stories complete

### User Story Dependencies

- **User Story 1 (P1)**: Can start after Foundational - No dependencies on other stories
- **User Story 2 (P2)**: Can start after Foundational - Extends US1's NowCommand
- **User Story 3 (P3)**: Can start after Foundational - Independent of US1/US2
- **User Story 4 (P4)**: Can start after Foundational - Uses WorkingHours from models
- **User Story 5 (P5)**: Can start after Foundational - Uses profiles from US2 optionally

### Within Each User Story

- Models/interfaces before services
- Services before commands
- Core implementation before options/features
- Output formatting last

### Parallel Opportunities

**Phase 2 - Foundational**:
```
T007, T008, T009, T010 (all models) - parallel
T013, T014, T015 (all data files) - parallel
T017, T018 (output utilities) - parallel
```

**Phase 3 - US1**:
```
T020, T021, T022, T023 (all resolvers) - parallel, then T024 (composite)
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup
2. Complete Phase 2: Foundational (CRITICAL - blocks all stories)
3. Complete Phase 3: User Story 1
4. **STOP and VALIDATE**: Test single location lookup works
5. Deploy/share MVP - users can look up time for any location

### Incremental Delivery

| Delivery | Stories | Value Added |
|----------|---------|-------------|
| MVP | US1 | Single location time lookup |
| v1.1 | US1 + US2 | Multiple locations, profiles |
| v1.2 | US1-US3 | Time conversion |
| v1.3 | US1-US4 | Meeting scheduling |
| v1.4 | US1-US5 | Dashboard view |

### Suggested MVP Scope

**Minimal viable product**: Complete Phase 1 + Phase 2 + Phase 3 (US1)
- 30 tasks for MVP
- Users can: `tzutil now "Tokyo"`, `tzutil now "90210"`, `tzutil now "America/New_York"`
- Full error handling and JSON output support

---

## Notes

- All tasks include exact file paths per plan.md project structure
- [P] tasks can run in parallel (different files)
- [US#] label maps task to specific user story
- Each user story independently testable after completion
- NodaTime handles all timezone/DST complexity
- System.CommandLine handles all CLI parsing
- Spectre.Console handles rich terminal output
