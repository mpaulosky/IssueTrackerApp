# Sam — Learnings for IssueTrackerApp

**Role:** Backend - API & Data Layer
**Project:** IssueTrackerApp
**Initialized:** 2026-03-12

---

## Learnings

### Issue #2 - Project Structure Setup (2026-03-12)

**Architecture Decisions:**
- Used .NET Aspire 13.1.0 for orchestration (AppHost project)
- Aspire.AppHost.Sdk projects must disable central package management (`ManagePackageVersionsCentrally=false`)
- ServiceDefaults project reference in AppHost requires `IsAspireProjectResource="false"` to avoid warning
- MongoDB.Driver 3.6.0 required to match MongoDB.EntityFrameworkCore 10.0.0 dependency

**Project Structure:**
- `src/AppHost/` - Aspire orchestration with MongoDB and Redis containers
- `src/ServiceDefaults/` - Shared Aspire configurations and extensions
- `src/Web/` - Blazor Server with Interactive Server rendering
- `src/Domain/` - Domain entities, CQRS with MediatR, FluentValidation
- `src/Persistence.MongoDb/` - MongoDB repositories with EF Core

**Key Patterns:**
- Vertical Slice Architecture folder structure (Features/ folders)
- Repository pattern (Repositories/ in Persistence)
- CQRS with MediatR (Domain layer)
- Result<T> pattern for error handling
- `public partial class Program {}` in Web/Program.cs for WebApplicationFactory testing

**Package Management:**
- Centralized versions in Directory.Packages.props
- Aspire hosting packages (MongoDB, Redis) added to AppHost
- OpenTelemetry packages (1.14.0) for ServiceDefaults

**File Paths:**
- Domain entities: `src/Domain/Entities/`
- Domain features: `src/Domain/Features/`
- Repositories: `src/Persistence.MongoDb/Repositories/`
- Blazor pages: `src/Web/Components/Pages/`
- GlobalUsings.cs in each project for common imports

---

## Notes

- Team transferred from IssueManager squad
- Same tech stack: .NET 10, Blazor, Aspire, MongoDB, Redis, Auth0, MediatR
- Ready to begin development