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

### Issue #37 - SignalR Hub Infrastructure (2026-03-12)

**SignalR Architecture:**
- Created `IssueHub` at `src/Web/Hubs/IssueHub.cs` with connection lifecycle management
- Hub endpoint: `/hubs/issues` (configured in Program.cs)
- Group-based notifications: `all` (broadcast) and `issue-{issueId}` (targeted)
- Client methods: `JoinIssueGroup(issueId)` and `LeaveIssueGroup(issueId)` for subscription management

**Event System:**
- Created event DTOs in `src/Domain/Events/`:
  - `IssueCreatedEvent` - Broadcast to `all` group
  - `IssueUpdatedEvent` - Sent to `issue-{issueId}` and `all` groups
  - `CommentAddedEvent` - Sent to `issue-{issueId}` group
  - `IssueAssignedEvent` - Sent to `issue-{issueId}` and `all` groups
- Each event includes timestamp and relevant DTOs (IssueDto, CommentDto)

**Service Integration:**
- Created `INotificationService` interface in `src/Domain/Abstractions/`
- Implemented `NotificationService` in `src/Web/Services/` using `IHubContext<IssueHub>`
- Integrated notifications into `IssueService` (create, update, status change operations)
- Integrated notifications into `CommentService` (comment additions)
- Notifications are sent after successful operations (Result.Success check)

**Configuration:**
- SignalR registered in Program.cs with `builder.Services.AddSignalR()`
- Hub mapped with `app.MapHub<IssueHub>("/hubs/issues")`
- NotificationService registered as scoped service
- No additional NuGet packages needed (SignalR is part of ASP.NET Core framework)

**Documentation:**
- Created comprehensive README at `src/Web/Hubs/README.md`:
  - Client event descriptions and usage
  - Azure SignalR Service production setup guide
  - Aspire integration instructions
  - Security, monitoring, and troubleshooting guidance
  - JavaScript client connection examples

**Key Patterns:**
- SignalR notifications follow existing Result<T> pattern
- Service layer maintains separation of concerns (business logic in handlers, notifications in services)
- Hub keeps minimal logic (connection management only)
- Group-based targeting enables efficient message delivery
- NotificationService uses structured logging for diagnostics

**Azure SignalR Service (Production):**
- Documented Azure SignalR Service setup steps
- Configuration via appsettings.Production.json or environment variables
- Requires `Microsoft.Azure.SignalR` package (not yet added)
- Connection string format: `Endpoint=https://...;AccessKey=...;Version=1.0;`
- Supports Aspire integration via `Aspire.Hosting.Azure.SignalR` component

**Next Steps:**
- Frontend integration required (Legolas will add Blazor SignalR client)
- Optional: Add Azure SignalR Service package for production deployment
- PR #39 created as draft (awaiting frontend completion)