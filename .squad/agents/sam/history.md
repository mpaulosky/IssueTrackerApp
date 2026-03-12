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

### Issue #3 - Aspire AppHost Configuration (2026-03-12)

**Aspire Orchestration Setup:**
- Enhanced AppHost with MongoDB (with MongoExpress UI) and Redis (with RedisCommander UI)
- Configured service discovery between Web project and backing resources
- Added health check endpoints: `/health` (all checks) and `/alive` (liveness only)
- Health checks enabled in Development environment by default

**OpenTelemetry Integration:**
- Integrated OpenTelemetry with OTLP exporter (configurable via `OTEL_EXPORTER_OTLP_ENDPOINT`)
- Added Azure Monitor/Application Insights support (via `APPLICATIONINSIGHTS_CONNECTION_STRING`)
- Configured tracing, metrics, and logging exporters
- Excludes health check endpoints from tracing to reduce noise

**ServiceDefaults Enhancements:**
- Added Azure.Monitor.OpenTelemetry.AspNetCore package (v1.3.0)
- Enabled Azure Monitor exporter when connection string is configured
- Service discovery and resilience patterns already configured
- HTTP client defaults include standard resilience handler and service discovery

**Environment Configuration:**
- Created environment-specific appsettings for Development, Staging, and Production
- Development: OTLP endpoint set to `http://localhost:4317`
- Staging/Production: OTLP and Application Insights configured via appsettings or environment variables
- Logging levels adjusted per environment (Development = Information, Production = Warning)

**Web Project Integration:**
- Updated Program.cs to use `builder.AddServiceDefaults()` and `app.MapDefaultEndpoints()`
- ServiceDefaults automatically configures OpenTelemetry, service discovery, resilience, and health checks
- Health check endpoints exposed in Development mode

**Key Patterns:**
- Aspire resources use `.WaitFor()` to ensure dependencies start in correct order
- AppHost manages MongoDB and Redis as containerized resources
- Web project references resources via service discovery
- Cross-cutting concerns centralized in ServiceDefaults project

**Testing Notes:**
- Solution builds successfully
- AppHost initializes correctly (requires Docker running for containers)
- Service discovery configured and ready for use
- Health check endpoints functional in Development mode