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

### Issue #33 - Email Notification System (2026-03-12)

**Email Service Architecture:**
- Created `IEmailService` abstraction in `src/Domain/Abstractions/` with two methods:
  - `SendAsync(EmailMessage)` - Send simple email
  - `SendTemplatedAsync<T>(templateName, model, toEmail)` - Send templated email (basic implementation)
- Implemented two service providers:
  - `SendGridEmailService` - Primary service using SendGrid API (requires API key)
  - `SmtpEmailService` - Fallback service using System.Net.Mail (for development)
- Conditional registration in Program.cs based on SendGrid API key presence
- Added SendGrid NuGet package (v9.29.3) to Directory.Packages.props

**Email Queue System:**
- Created `EmailQueueItem` model with MongoDB entity configuration:
  - Tracks attempts, status (Pending/Sending/Sent/Failed), retry schedule
  - Max 3 attempts with exponential backoff (1, 2, 4 minutes)
  - Stores email metadata (to, subject, body, HTML flag, from name)
- Created `QueueEmailCommand` MediatR command to queue emails for background processing
- Added EmailQueue DbSet to IssueTrackerDbContext
- Emails stored in MongoDB `emailqueue` collection

**Background Processing:**
- Implemented `EmailQueueBackgroundService` (IHostedService):
  - Processes queue every 10 seconds
  - Retrieves up to 10 pending emails per batch
  - Handles send failures with exponential backoff retry
  - Logs all operations for diagnostics
  - Updates email status and retry schedule in database
- Registered as hosted service in Program.cs

**Domain Events Integration:**
- Enhanced existing events to implement INotification:
  - `IssueAssignedEvent` - Added IssueTitle property
  - `CommentAddedEvent` - Added IssueTitle and IssueOwner properties
- Created new event:
  - `IssueStatusChangedEvent` - Tracks old status, new status, issue owner
- Updated INotificationService interface signatures to pass additional data (issueTitle, issueOwner)

**Notification Handlers:**
- Created MediatR notification handlers in `src/Domain/Features/Notifications/`:
  - `IssueAssignedNotificationHandler` - Queues email when issue assigned
  - `CommentAddedNotificationHandler` - Queues email when comment added (skips if author is owner)
  - `IssueStatusChangedNotificationHandler` - Queues email when status changes
- All handlers use inline HTML email templates (clean, simple styling)
- Handlers follow Result<T> pattern and log operations

**Event Publishing:**
- Integrated event publishing in command handlers:
  - `ChangeIssueStatusCommand` publishes `IssueStatusChangedEvent`
  - `AddCommentCommand` publishes `CommentAddedEvent`
- Used IMediator.Publish for notification pattern (one event, multiple handlers)
- Events published after successful database operations

**Configuration:**
- Updated appsettings.Development.json with email settings:
  ```json
  "Smtp": {
    "Host": "localhost",
    "Port": 587,
    "EnableSsl": false,
    "FromEmail": "noreply@issuetracker.local",
    "FromName": "IssueTracker Dev"
  },
  "SendGrid": {
    "ApiKey": "",
    "FromEmail": "noreply@issuetracker.com",
    "FromName": "IssueTracker"
  }
  ```
- Production should set SendGrid:ApiKey via environment variable or user secrets

**User Preferences Model:**
- Created `NotificationPreferences` record in `src/Domain/Models/`:
  - EmailOnAssigned, EmailOnComment, EmailOnStatusChange, EmailOnMention (all default true)
  - Note: Users are NOT stored in database (Auth0 managed)
  - Preferences would be stored in Auth0 metadata or browser local storage
  - Placeholder for future implementation

**Email Templates:**
- Used inline HTML templates in notification handlers (no external files)
- Simple, clean styling (works in most email clients):
  - Issue Assignment: Blue border-left accent
  - Comment Added: Green border-left accent
  - Status Changed: Yellow border-left accent
- All templates include:
  - Issue title and relevant details
  - Action-specific information (assignee, comment text, status change)
  - Automated notification footer

**Key Technical Patterns:**
- CQRS with MediatR for email queuing
- Repository pattern for email queue storage
- Background service with IHostedService for async processing
- Notification pattern for event handling (INotification, INotificationHandler)
- Result<T> pattern for error handling throughout
- Dependency injection for service selection (SendGrid vs SMTP)
- Exponential backoff retry logic for resilience

**Service Integration Updates:**
- Updated `NotificationService` (SignalR service) method signatures to accept issueTitle and issueOwner
- Updated `CommentService` call to NotifyCommentAddedAsync with additional parameters
- Both SignalR (real-time) and email notifications work together seamlessly

**Build & Testing:**
- All projects build successfully with no errors
- Email sending requires SMTP server or SendGrid API key to function
- Without configuration, emails are queued but remain pending
- Check MongoDB EmailQueue collection to verify queuing works
- Background service logs all email operations at Information level

**Commit Details:**
- Branch: `squad/33-email-notifications`
- Commit: "feat(email): Add email notification system"
- 24 files changed, 1066 insertions, 10 deletions
- Push successful, PR URL: https://github.com/mpaulosky/IssueTrackerApp/pull/new/squad/33-email-notifications

**Future Enhancements:**
- Implement RazorLight or similar for proper template rendering
- Add user preference management UI (Blazor component)
- Create external email template files (.cshtml)
- Implement @mention detection in comments
- Add email digest feature (batch multiple notifications)
- Add unsubscribe functionality
- Consider using Azure Communication Services Email instead of SendGrid

**Lessons Learned:**
- MongoDB repository uses `AddAsync` not `CreateAsync` (IRepository<T> interface)
- UserDto has `Id` property (not `ObjectIdentifier`)
- CommentDto has `Description` property (not `Content`)
- IRepository.UpdateAsync takes only entity parameter (not id + entity)
- GetAllAsync returns Result<IEnumerable<T>>, must check Success and extract Value
- MediatR notifications (INotification) are different from SignalR notifications
- Required properties on events must be set in object initializers (C# 11 feature)
- Background services should use IServiceProvider.CreateScope() for scoped dependencies
- SignalR NotificationService and email NotificationHandlers serve different purposes (real-time vs async)

### Issue #34 - Analytics Backend (2026-03-12)

**Analytics DTOs:**
- Created 6 DTOs in `src/Domain/DTOs/Analytics/`:
  - `IssuesByStatusDto` - Status name and count
  - `IssuesByCategoryDto` - Category name and count
  - `IssuesOverTimeDto` - Date, created count, closed count
  - `ResolutionTimeDto` - Category and average resolution hours
  - `TopContributorDto` - User ID, name, issues closed, comments count
  - `AnalyticsSummaryDto` - Comprehensive dashboard summary (totals + all metrics)
- All DTOs use records with JsonConstructor and JsonPropertyName attributes
- Namespace: `Domain.DTOs.Analytics` (matches existing pattern)

**MediatR Query Handlers:**
- Created 7 queries in `src/Domain/Features/Analytics/Queries/`:
  - `GetIssuesByStatusQuery` - Group issues by status with counts
  - `GetIssuesByCategoryQuery` - Group issues by category with counts
  - `GetIssuesOverTimeQuery` - Time series of created/closed issues by date
  - `GetResolutionTimesQuery` - Average resolution time by category (closed issues only)
  - `GetTopContributorsQuery` - Top N contributors by issues closed + comments (default 10)
  - `GetAnalyticsSummaryQuery` - Comprehensive summary (executes all queries in parallel)
  - `ExportAnalyticsQuery` - CSV export of all issues with metadata
- All queries support optional StartDate/EndDate filtering
- Use LINQ queries with EF Core (MongoDB.EntityFrameworkCore provider)
- Follow Result<T> pattern with proper error handling and logging

**Analytics Service:**
- Created `IAnalyticsService` interface in `src/Web/Services/`
- Implemented `AnalyticsService` with IMemoryCache integration:
  - 5-minute cache expiration for all queries except exports
  - Cache keys include date range parameters
  - Facade pattern wrapping MediatR queries
  - Provides clean API for frontend consumption
- Registered as scoped service in Program.cs

**CSV Export:**
- Created `CsvExportHelper` in `src/Web/Helpers/`:
  - Generic ExportToCsv<T> method using reflection
  - Proper CSV escaping (double quotes, commas, newlines)
  - Returns UTF-8 byte array for file download
- ExportAnalyticsQuery handler generates CSV inline with custom formatting:
  - Headers: ID, Title, Status, Category, Author, Created, Modified, ResolutionHours
  - Includes resolution time calculation (N/A if not closed)

**Technical Implementation:**
- MongoDB.EntityFrameworkCore LINQ queries (no raw aggregation pipelines needed)
- Grouping, filtering, and aggregation using standard LINQ
- Closed issues identified by: `Status.StatusName == "Closed" || Archived`
- Resolution time: `DateModified - DateCreated` for closed issues
- Top contributors: Combined metric (issues closed + comments count)
- Time series: Issues grouped by date (Date property of DateCreated/DateModified)

**Key Patterns:**
- CQRS with MediatR for all analytics queries
- Repository pattern (IRepository<Issue>, IRepository<Comment>)
- Result<T> pattern for error handling throughout
- Service facade with caching for performance
- Date range filtering on all queries
- Admin-only access ready (patterns in place, frontend integration needed)

**Configuration:**
- Service registered in Program.cs: `builder.Services.AddScoped<IAnalyticsService, AnalyticsService>()`
- Uses existing IMemoryCache (no additional configuration needed)
- No new NuGet packages required (uses existing dependencies)

**Frontend Integration Notes:**
- Frontend components (Analytics.razor, Charts, DateRangePicker, SummaryCard) were created by another squad member
- Frontend has compilation errors (Chart components, DateRangePicker, _Imports namespace issues)
- Backend is complete and independent of frontend errors
- _Imports.razor namespace corrected: `Domain.DTOs.Analytics` (not `IssueTrackerApp.Domain.DTOs.Analytics`)

**Commit Details:**
- Branch: `squad/34-analytics-dashboard`
- Commit: "feat(analytics): Add analytics backend with MongoDB aggregations"
- 17 files changed, 1109 insertions
- Push successful, PR #42 created as draft
- PR URL: https://github.com/mpaulosky/IssueTrackerApp/pull/42

**Query Performance Considerations:**
- Caching reduces database load (5-minute TTL)
- LINQ queries translate to efficient MongoDB queries
- Date range filtering applied at database level
- Summary query executes sub-queries in parallel with Task.WhenAll
- Export query does NOT use caching (generates fresh CSV each time)

**Lessons Learned:**
- Domain DTOs namespace is `Domain.DTOs.Analytics` (not `IssueTrackerApp.Domain.DTOs.Analytics`)
- MongoDB.EntityFrameworkCore uses standard EF Core LINQ (no need for BsonDocument aggregation pipelines)
- IRepository<T>.FindAsync accepts Expression<Func<T, bool>> predicate for filtering
- LINQ GroupBy and aggregations work seamlessly with MongoDB EF provider
- ExportAnalyticsQuery handler generates CSV directly (no need for separate CsvExportHelper in query)
- Frontend team members may use incorrect namespaces - always check and fix
- Chart components (.razor files) with uninitialized fields cause CS0649 warnings (frontend issue)
- Backend components can build successfully even when Web project has frontend errors

---