<!--
  ============================================================================
  SYNC IMPACT REPORT
  ============================================================================
  Version change: N/A → 1.0.0 (initial creation)
  
  Modified principles: None (initial creation)
  
  Added sections:
    - Core Principles (4 principles)
    - Quality Gates
    - Development Workflow
    - Governance
  
  Removed sections: None
  
  Templates requiring updates:
    ✅ plan-template.md - Compatible (Constitution Check section exists)
    ✅ spec-template.md - Compatible (Success Criteria aligns with principles)
    ✅ tasks-template.md - Compatible (Phase structure supports quality gates)
  
  Follow-up TODOs: None
  ============================================================================
-->

# Project Constitution

## Core Principles

### I. Code Quality

All code MUST meet these non-negotiable quality standards:

- **Readability**: Code MUST be self-documenting with clear naming conventions. Variable and function names MUST express intent without requiring comments to understand basic functionality.
- **Maintainability**: Functions MUST have a single responsibility. Files MUST NOT exceed 300 lines without architectural justification. Cyclomatic complexity MUST remain below 10 per function.
- **Consistency**: All code MUST follow the project's established style guide and formatting rules. Linting MUST pass with zero warnings before merge.
- **Documentation**: Public APIs MUST have documentation. Complex algorithms MUST include inline comments explaining the "why" not the "what".
- **No Dead Code**: Unused imports, variables, and functions MUST be removed. Commented-out code MUST NOT be committed.

**Rationale**: High code quality reduces technical debt, accelerates onboarding, and minimizes bug introduction during changes.

### II. Testing Standards

Testing is mandatory and MUST follow these requirements:

- **Coverage Threshold**: New code MUST have minimum 80% line coverage. Critical paths (authentication, data mutations, payments) MUST have 100% coverage.
- **Test Types Required**:
  - Unit tests for all business logic and utility functions
  - Integration tests for API endpoints and database operations
  - Contract tests for external service integrations
- **Test Quality**: Tests MUST be deterministic (no flaky tests). Tests MUST be independent and runnable in isolation. Test names MUST clearly describe the scenario being tested.
- **Test-First Encouraged**: For complex features, write tests before implementation (Red-Green-Refactor). All bug fixes MUST include a regression test.
- **CI Gate**: All tests MUST pass before merge. Test failures block deployment.

**Rationale**: Comprehensive testing catches bugs early, enables confident refactoring, and serves as living documentation of expected behavior.

### III. User Experience Consistency

All user-facing features MUST maintain consistent experience:

- **Interface Patterns**: Similar operations MUST use similar interaction patterns. Users MUST NOT have to relearn the interface for each feature.
- **Error Handling**: All errors MUST provide clear, actionable messages. Error messages MUST NOT expose internal implementation details. Users MUST always have a clear path forward after an error.
- **Response Formatting**: Support both human-readable and machine-parseable output formats (text and JSON). Default to human-readable; use flags for structured output.
- **Input Flexibility**: Accept common input variations where reasonable (e.g., timezone names, abbreviations, offsets). Provide helpful suggestions for invalid input.
- **Predictability**: Operations MUST produce consistent results given the same inputs. Side effects MUST be clearly documented and expected.

**Rationale**: Consistent UX reduces cognitive load, increases user confidence, and accelerates feature adoption.

### IV. Performance Requirements

All features MUST meet baseline performance standards:

- **Response Time**: CLI commands MUST respond within 500ms for typical operations. Network-dependent operations MUST show progress indication if exceeding 1 second.
- **Resource Efficiency**: Memory usage MUST remain bounded and proportional to input size. No memory leaks in long-running operations.
- **Startup Time**: Application startup MUST complete within 200ms. Lazy-load non-critical dependencies.
- **Scalability**: Design MUST support reasonable growth without architectural changes. Document known scalability limits.
- **Degradation**: Performance degradation MUST be graceful under load. Features MUST NOT block on non-critical operations.

**Rationale**: Performance directly impacts user productivity and satisfaction. Slow tools get abandoned.

## Quality Gates

All pull requests and code changes MUST pass these gates:

| Gate | Requirement | Enforcement |
|------|-------------|-------------|
| Linting | Zero errors, zero warnings | CI automated check |
| Type Safety | No type errors (if applicable) | CI automated check |
| Test Coverage | Minimum 80% on new code | CI automated check |
| Test Pass | 100% tests passing | CI automated check |
| Documentation | Public APIs documented | Code review |
| Performance | No regression on benchmarks | Code review + CI |

## Development Workflow

### Code Review Requirements

- All changes MUST be reviewed by at least one other contributor
- Reviews MUST verify compliance with all four core principles
- Reviewers MUST check for: correctness, readability, test coverage, and performance implications
- Self-merges are NOT permitted except for critical hotfixes with post-merge review

### Implementation Standards

- Start with the simplest solution that meets requirements (YAGNI)
- Add complexity only when justified and documented
- Prefer composition over inheritance
- Prefer explicit over implicit behavior

## Governance

This constitution is the authoritative guide for all technical decisions in this project.

### Amendment Process

1. Propose changes via pull request to this file
2. Changes require approval from project maintainers
3. Breaking changes (principle removal/redefinition) require migration plan
4. All amendments MUST be documented with rationale

### Versioning Policy

- **MAJOR**: Backward-incompatible principle changes or removals
- **MINOR**: New principles added or existing ones materially expanded
- **PATCH**: Clarifications, wording improvements, non-semantic changes

### Compliance

- All PRs MUST reference relevant principles when applicable
- Constitution violations MUST be justified and documented in Complexity Tracking
- Regular audits SHOULD occur to ensure ongoing compliance
- Ambiguity in principles SHOULD be resolved via amendment, not interpretation

### Decision Authority

When technical decisions conflict:
1. This constitution takes precedence over personal preference
2. Explicit requirements take precedence over implicit conventions
3. User experience takes precedence over implementation convenience
4. Simplicity takes precedence over premature optimization

**Version**: 1.0.0 | **Ratified**: 2026-03-02 | **Last Amended**: 2026-03-02
