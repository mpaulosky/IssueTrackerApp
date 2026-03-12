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

### Issue #35 - Attachment Backend Infrastructure (2026-03-12)

**Backend Architecture:**
- Created `Attachment` model in `src/Domain/Models/Attachment.cs` with MongoDB entity configuration
- Created `AttachmentDto` with file size formatting and helper properties
- Created `FileValidationConstants` for validation rules (10MB max, allowed file types)
- Implemented CQRS pattern:
  - `AddAttachmentCommand` - Upload file and save metadata
  - `DeleteAttachmentCommand` - Delete file and metadata (owner/admin authorization)
  - `GetIssueAttachmentsQuery` - Get all attachments for an issue
- Added FluentValidation validators for all commands/queries

**File Storage Services:**
- Created `IFileStorageService` interface in `src/Domain/Abstractions/`
- Implemented `BlobStorageService` in new `Persistence.AzureStorage` project:
  - Uses Azure.Storage.Blobs (v12.25.0)
  - Implements thumbnail generation using SixLabors.ImageSharp (v3.1.12)
  - Stores files in `issue-attachments` container
  - Stores thumbnails in `issue-attachments-thumbnails` container
  - Generates unique blob names with Guid prefixes
- Implemented `LocalFileStorageService` in `src/Web/Services/`:
  - Fallback for development without Azure
  - Stores files in `wwwroot/uploads` directory
  - Stores thumbnails in `wwwroot/uploads/thumbnails` directory

**Database Integration:**
- Added `Attachments` DbSet to `IssueTrackerDbContext`
- Created `AttachmentConfiguration` for entity mapping
- Uses generic `Repository<Attachment>` for CRUD operations
- Attachment metadata stored in MongoDB `attachments` collection

**Service Layer:**
- `AttachmentService` already existed (created by Legolas)
- Updated to use Result<T> pattern correctly
- Integrated with MediatR for CQRS operations
- File validation helpers included

**Configuration:**
- Added package versions to `Directory.Packages.props`:
  - Azure.Storage.Blobs v12.25.0
  - SixLabors.ImageSharp v3.1.12 (latest non-vulnerable version)
- Registered services in `Program.cs`:
  - Conditional registration: Azure Blob if connection string exists, else local storage
  - AttachmentService already registered
- Updated `Web.csproj` to reference `Persistence.AzureStorage` project

**Key Patterns:**
- Generic repository pattern for attachment CRUD
- Result<T> pattern with ResultErrorCode enum for error handling
- Admin authorization in DeleteAttachmentCommand (IsAdmin parameter)
- Automatic thumbnail generation for images
- Cleanup on failure (deletes uploaded files if metadata save fails)

**Validation Rules:**
- Maximum file size: 10MB
- Allowed image types: JPEG, PNG, GIF, WebP
- Allowed document types: PDF, Plain text, Markdown
- File name length: 255 characters max
- Thumbnail size: 200x200px (max dimensions, aspect ratio preserved)

**Build Status:**
- All backend projects build successfully (Domain, Persistence.MongoDb, Persistence.AzureStorage)
- Web project has frontend dependency on AttachmentList component (handled by Legolas)
- Changes committed to existing branch `squad/35-issue-attachments`
- PR #40 already exists as draft (created by Legolas with frontend code)

**Azure Storage Setup:**
- Configure `BlobStorage:ConnectionString` in appsettings for Azure Blob Storage
- If not configured, falls back to local file storage automatically
- Production deployment should use Azure Blob Storage for scalability
- Connection string format: `DefaultEndpointsProtocol=https;AccountName=...;AccountKey=...;EndpointSuffix=core.windows.net`

**Collaboration Notes:**
- Legolas (frontend) already created UI components (AttachmentList, AttachmentCard, FileUpload)
- Backend infrastructure completes the feature implementation
- Both frontend and backend changes in same PR (#40)
- Ready for review and testing once frontend builds successfully