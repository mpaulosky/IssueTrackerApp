# Documentation Structure Decision — Frodo

**Date**: March 2025  
**Stakeholder**: Frodo (Tech Writer)  
**Status**: Approved (implemented)

## Decision

Implement a **category-based organization** for the new `docs/LIBRARIES.md` package reference document, rather than alphabetical listing.

## Rationale

1. **Developer Cognitive Load**: When developers need to understand a dependency, they think in architectural domains (e.g., "What observability packages do we use?") rather than alphabetically.

2. **Single Source of Truth**: Packages are sourced from centralized `Directory.Packages.props`, ensuring version consistency and making updates straightforward.

3. **Scalability**: As new packages are added, they can be categorized clearly without disrupting existing references.

## Categories Chosen

- **.NET Aspire Integration**: Aspire-specific integration packages
- **Data Access**: MongoDB driver and EF Core provider
- **Application Patterns**: CQRS (MediatR) and validation (FluentValidation)
- **Authentication & Security**: Auth0 integration
- **Observability & Monitoring**: OpenTelemetry, Azure Application Insights, resilience
- **Health Checks**: MongoDB and Redis health checks
- **Testing**: xUnit, FluentAssertions, NSubstitute
- **Blazor Component Testing**: bUnit
- **End-to-End Testing**: Playwright
- **Integration Testing Infrastructure**: TestContainers, ASP.NET Core test hosting

## README.md Structure

The README focuses on **narrative flow** for new developers:

1. Overview + quick tech stack bullets
2. Project structure with ASCII diagram
3. Key features
4. Getting started (prerequisites, clone, build, run, test)
5. Configuration guidance
6. Deep dives: Architecture, caching, testing
7. API documentation reference
8. License and contributing

This avoids overwhelming readers with exhaustive API details; those belong in Scalar at `/api/docs`.

## Impact

- **Developers**: Easier navigation of dependency ecosystem
- **Tech Writer**: Clearer mental model for maintaining documentation
- **Maintainers**: Faster onboarding when evaluating package upgrades
- **New Contributors**: Clear documentation of "what we use and why"

## Related Files

- `docs/LIBRARIES.md` — Implements this decision
- `README.md` — Updated to reflect architecture priorities
- `Directory.Packages.props` — Source of truth for package versions
