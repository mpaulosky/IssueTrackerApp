# Frodo — Learnings for IssueTrackerApp

**Role:** Tech Writer - Documentation
**Project:** IssueTrackerApp
**Initialized:** 2026-03-12

---

## Learnings

### Documentation Structure Decision (March 2025)

**Context**: Project needed comprehensive documentation to reflect current architecture with .NET Aspire, Blazor Interactive Server Rendering, MongoDB Atlas, and Redis caching.

**Actions Taken**:
1. **README.md Update**: Completely refreshed to showcase modern tech stack
   - Added clear project overview and key features
   - Documented project structure with AppHost, ServiceDefaults, and Blazor web app
   - Included development prerequisites and getting started guide
   - Emphasized Aspire orchestration as central to architecture
   - Added architecture section explaining ServiceDefaults pattern

2. **docs/LIBRARIES.md Creation**: New authoritative package reference
   - Categorized all 22 NuGet packages by domain (Aspire, Data Access, Authentication, etc.)
   - Sourced from centralized `Directory.Packages.props` for single source of truth
   - Included version and purpose for each package
   - Added notes on Aspire integration, OpenTelemetry strategy, and testing approach

**Key Insights**:
- Project uses modern Aspire patterns: ServiceDefaults eliminate boilerplate for OpenTelemetry, health checks, and resilience
- Comprehensive test coverage spans unit (xUnit), component (bUnit), E2E (Playwright), and integration (TestContainers)
- Redis + MongoDB provide distributed caching + persistence; both have health checks integrated
- Auth0 is authentication standard; MediatR provides CQRS pattern for scalability

**Documentation Decisions Made**:
- LIBRARIES.md organizes packages by architectural concern, not alphabetically (easier to find related packages)
- README focuses on "getting started" rather than exhaustive API details (API docs via Scalar at `/api/docs`)
- Emphasized Aspire + ServiceDefaults as core to understanding the architecture

---

## Notes

- Team transferred from IssueManager squad
- Same tech stack: .NET 10, Blazor, Aspire, MongoDB, Redis, Auth0, MediatR
- Ready to begin development