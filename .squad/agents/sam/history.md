# Sam — Learnings for IssueTrackerApp

**Role:** Backend - API & Data Layer
**Project:** IssueTrackerApp
**Initialized:** 2026-03-12

---

## Core Context

Sam (Backend Developer) has established core architectural patterns for IssueTrackerApp:

**Aspire & Infrastructure:**
- Orchestrated .NET 10 solution with MongoDB/Redis containers and OpenTelemetry tracing
- Configured health checks, service discovery, and Application Insights integration
- MongoDB.EntityFrameworkCore provider with Result<T> pattern for error handling
- AppHost configuration: `ManagePackageVersionsCentrally=false` (Aspire SDK requirement)

**Data Access Patterns:**
- Generic `IRepository<T>` with async-first operations and Result<T> error handling
- MongoDB settings validation on startup via `IValidateOptions`
- DbContext pooling and factory patterns for flexible context usage
- Static mappers for entity ↔ DTO conversion (sealed classes for value objects)

**Domain Architecture:**
- CQRS pattern with MediatR for feature-based organization
- FluentValidation for command/query validation
- Feature-based vertical slice folder structure
- Strongly-typed configuration objects (MongoDB, Redis settings)

**Key Decisions:**
- DTO-Model separation enforced across persistence and API layers
- `Comment.Issue` → `Comment.IssueId` (ObjectId reference) breaks circular dependencies
- Issue creation resolves "Open" status from database via `IRepository<Status>`
- All git commands in MSBuild targets redirect stderr to prevent output contamination

---

## Recent Learnings (2026-03-18 to 2026-03-19)

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
### WI-1 — Value Objects and Mapper Infrastructure (2026-03-14)

**Value Objects Created (src/Domain/Models/):**
- `UserInfo.cs` — Sealed class with Id/Name/Email, BsonElement attributes matching UserDto serialization
- `CategoryInfo.cs` — Sealed class mirroring CategoryDto fields, nested UserInfo for ArchivedBy
- `StatusInfo.cs` — Sealed class mirroring StatusDto fields, nested UserInfo for ArchivedBy

**Mapper Classes Created (src/Domain/Mappers/):**
- `UserMapper.cs` — User ↔ UserDto, UserInfo ↔ UserDto, with ToInfo conversion
- `CategoryMapper.cs` — Category ↔ CategoryDto, CategoryInfo ↔ CategoryDto, with ToInfo
- `StatusMapper.cs` — Status ↔ StatusDto, StatusInfo ↔ StatusDto, with ToInfo
- `IssueMapper.cs` — Issue ↔ IssueDto, uses existing embedded DTOs directly
- `CommentMapper.cs` — Comment ↔ CommentDto, handles IssueDto/UserDto embeds
- `AttachmentMapper.cs` — Attachment ↔ AttachmentDto, ObjectId ↔ string parsing

**Architecture Decisions:**
- Value objects are `sealed class` (not records) for EF Core/MongoDB mutable property support
- BsonElement attributes explicitly match current DTO serialization (PascalCase) for zero-migration compatibility
- Mappers are static classes with null-safe patterns returning Empty/default instances
- Value objects use `static Empty => new()` pattern (new instance per call) matching existing DTO convention
- Collection overloads use `Select(lambda).ToList()` to avoid overload ambiguity
- AttachmentMapper uses `ObjectId.TryParse` for safe string-to-ObjectId conversion

**Key Patterns:**
- Value objects in `Domain.Models` namespace (same as models, ready for embedding)
- Mappers in `Domain.Mappers` namespace (new directory)
- CategoryInfo/StatusInfo nest UserInfo (not UserDto) for proper DDD value object composition
- All mappers compile against CURRENT codebase without modifications to existing files

**Build/Test Results:**
- Domain project: 0 errors, 0 warnings
- Domain.Tests: 255 passed, 0 failed
- Architecture.Tests: 38 passed, 0 failed
- Full solution: pre-existing TailwindCSS/npm issue in Web project (unrelated)

### WI-3 + WI-4 — Model Refactoring to Value Objects + DTO/Mapper Updates (2026-03-14)

**What Changed:**
- **Models refactored:** Replaced all DTO types in domain models with value objects:
  - Issue.cs: `CategoryDto→CategoryInfo`, `UserDto Author→UserInfo`, `StatusDto→StatusInfo`, `UserDto ArchivedBy→UserInfo`
  - Category.cs: `UserDto ArchivedBy→UserInfo`
  - Status.cs: `UserDto ArchivedBy→UserInfo`
  - Comment.cs: `IssueDto Issue→ObjectId IssueId` (breaks circular dep), `UserDto Author/ArchivedBy/AnswerSelectedBy→UserInfo`
  - Attachment.cs: `UserDto UploadedBy→UserInfo`
- **DTOs updated:** All 6 DTO constructors now convert value objects via Mappers (UserMapper.ToDto, CategoryMapper.ToDto, etc.)
- **CommentDto:** Changed from `IssueDto Issue` to `ObjectId IssueId` to match the model change
- **UserDto:** Added `UserDto(UserInfo info)` constructor for direct value object conversion
- **Mappers updated:** All 6 mappers now use UserMapper.ToInfo/ToDto, CategoryMapper.ToInfo/ToDto, StatusMapper.ToInfo/ToDto for conversions
- **EF Core configs:** CommentConfiguration removed the nested IssueDto OwnsOne block; other configs unchanged (property names match)
- **GlobalUsings:** Added `global using Domain.Mappers;` to Domain project

**Remaining Compile Errors (29 errors in 15 Feature handler files — expected, for WI-5):**
- Comment handlers: `AddCommentCommand`, `UpdateCommentCommand`, `DeleteCommentCommand` — need `UserMapper.ToInfo()` + `IssueId` instead of `Issue`

---

### 2026-03-19: Team Auth & DI Coordination (2026-03-17T18:54:25Z)

**From Gandalf — Auth0 Role Claim Mapping:**
- Fixed "Access Denied" authorization issue via `Auth0ClaimsTransformation`
- Maps Auth0's custom namespaced role claims to standard `ClaimTypes.Role`
- Configurable via `Auth0:RoleClaimNamespace` in user secrets
- Includes idempotency check and structured logging
- **For Sam:** Claims transformation integrates cleanly with existing DI; registered as scoped service in auth pipeline

**From Legolas — Navigation Menu:**
- Created `NavMenuComponent.razor` with role-based visibility
- Updated layout and landing page
- **For Sam:** Navigation respects authorization policies; integrates with your policy-based authorization pattern

**DI Alignment Reminder:**
- When combining `AddDbContext` + `AddDbContextFactory`, ensure lifetimes match (both scoped or both explicit)
- Background services (singletons) must resolve scoped dependencies via `IServiceScopeFactory` — never inject scoped services directly into constructor

---
- Comment queries: `GetIssueCommentsQuery` — needs `IssueId` filter instead of `Issue.Id`
- Issue commands: `CreateIssueCommand`, `UpdateIssueCommand`, `DeleteIssueCommand`, `ChangeIssueStatusCommand` — need mapper conversions
- Bulk commands: `BulkAssignCommand`, `BulkDeleteCommand`, `BulkUpdateCategoryCommand`, `BulkUpdateStatusCommand`, `UndoBulkOperationCommand` — need mapper conversions
- Category commands: `ArchiveCategoryCommand`, `CreateCategoryCommand` — need `UserMapper.ToInfo()`
- Status commands: `ArchiveStatusCommand`, `CreateStatusCommand` — need `UserMapper.ToInfo()`
- Attachment commands: `AddAttachmentCommand` — needs `UserMapper.ToInfo()`

**Key Decision:** Changed `Comment.Issue` (IssueDto) → `Comment.IssueId` (ObjectId) to break circular dependency. CommentDto also changed to hold `ObjectId IssueId` instead of embedded `IssueDto`. Downstream handlers that need full issue data must load it separately.

### WI-5 - Update All CQRS Handlers to Use Value Objects (2025-06-25)

**What Was Done:**
- Fixed all 18 CQRS handler files that were assigning DTOs to model properties now typed as value objects
- Domain project builds with zero errors, zero warnings

**Fix Patterns Applied:**
- `UserDto → UserInfo` via `UserMapper.ToInfo()` (most common — Author, ArchivedBy, UploadedBy)
- `CategoryDto → CategoryInfo` via `CategoryMapper.ToInfo()` (Issue.Category)
- `StatusDto → StatusInfo` via `StatusMapper.ToInfo()` (Issue.Status)
- `Comment.Issue = issueDto` → `Comment.IssueId = issue.Id` (property renamed from embedded DTO to ObjectId)
- `UserDto.Empty` → `UserInfo.Empty` for default empty values on model properties
- Bulk snapshot creation: model value objects → DTOs via `ToDto()` (snapshots store DTOs)
- Bulk undo restoration: snapshot DTOs → value objects via `ToInfo()` (restoring to models)
- `CreateIssueCommand`: replaced `new StatusDto(...)` with `new StatusInfo { ... }` for default Open status

**Remaining Downstream Errors (for other WIs):**
- `tests/Domain.Tests/` — 29 test files with ~187 errors (same DTO→value object pattern in test Issue/Comment setup code)
- `src/Web/Services/CommentService.cs` — 3 errors referencing `CommentDto.Issue` which no longer exists (now `IssueId`)
- Total: ~190 errors across 30 files outside Domain handlers

**Key Learning:** When models switch from DTOs to value objects, the conversion ripples through: handlers (fixed here), tests (need mapper calls in setup), and consuming services (need property name updates).


---

## 2025-06-12 — Azure Storage Test Projects Scaffolded

**Task:** Created two new test projects for `Persistence.AzureStorage` which previously had zero test coverage.

**Files Created:**
1. `tests/Persistence.AzureStorage.Tests/` (Unit Tests)
   - `Persistence.AzureStorage.Tests.csproj` — follows Domain.Tests pattern, includes NSubstitute for mocking
   - `GlobalUsings.cs` — includes Azure.Storage.Blobs, testing frameworks, required Microsoft.Extensions packages

2. `tests/Persistence.AzureStorage.Tests.Integration/` (Integration Tests)
   - `Persistence.AzureStorage.Tests.Integration.csproj` — uses Testcontainers.Azurite for real Azure Blob Storage emulation
   - `GlobalUsings.cs` — includes Testcontainers.Azurite, Azure.Storage.Blobs, testing frameworks

**Package Management:**
- Added `Testcontainers.Azurite` version `4.11.0` to `Directory.Packages.props` (aligns with Testcontainers.MongoDb version)
- All PackageReference entries in csproj files use centralized versioning (NO version attributes in project files)

**Solution Updates:**
- Added `src/Persistence.AzureStorage/Persistence.AzureStorage.csproj` to `/src/` folder in `IssueTrackerApp.slnx`
- Added both test projects to `/tests/` folder in `IssueTrackerApp.slnx`

**Build Verification:**
- `dotnet restore` succeeded for both projects
- `dotnet build` succeeded for both projects with zero errors

**Key Patterns:**
- Copyright headers follow exact format: `// Copyright (c) 2025. All rights reserved.` with full file metadata
- File-scoped namespaces enforced
- Tab indentation (2 spaces per tab)
- GlobalUsings organize imports by category: Testing → System → Microsoft Extensions → Azure → Project/Domain
- Integration tests use Testcontainers for real Azure Storage emulation (not in-memory mocks)
- Unit tests use NSubstitute for mocking BlobServiceClient and related Azure SDK types

**Architecture Decision:** Integration tests use Azurite via Testcontainers rather than Azure Storage Emulator because Testcontainers provides cross-platform Docker-based testing with consistent behavior across dev/CI environments.

---

### Session: Azure Storage Test Coverage (2026-03-14)

**Outcome:** ✅ Scaffolded and verified `Persistence.AzureStorage` test projects

**Deliverables:**
- `tests/Persistence.AzureStorage.Tests/` — Unit test project (ready for 33 tests by Gimli)
- `tests/Persistence.AzureStorage.Tests.Integration/` — Integration test project (ready for 25+ tests by Gimli)
- Updated `IssueTrackerApp.slnx` with new projects
- Updated `Directory.Packages.props` with all dependencies

**Learnings:**
- Both test projects scaffold and build cleanly before test implementation

## Learnings

### MongoDB Connection String Fallback (2025-07-17)
- **Problem:** EF Core MongoDB provider reads `MongoDB:ConnectionString` from appsettings, but Aspire injects Atlas connection string into `ConnectionStrings:mongodb`. These two config paths never intersect, causing `TimeoutException` against localhost.
- **Solution:** Added fallback logic in `AddMongoDbPersistence` that checks `ConnectionStrings:mongodb` when `MongoDB:ConnectionString` is empty or equals the localhost default. Set via `IConfigurationSection` indexer before Options binding.
- **Key files:** `src/Persistence.MongoDb/ServiceCollectionExtensions.cs`, `src/Web/appsettings.Development.json`
- **Pattern:** When two config systems disagree (Aspire vs raw appsettings), bridge them at the DI registration layer using configuration overlay before binding Options.

---

### Session: MongoDB Config Fallback Fix (2025-03-21)

**Outcome:** ✅ Fixed TimeoutException in Web project startup

**Problem:** EF Core MongoDB provider reads `MongoDB:ConnectionString` from appsettings.Development.json (hardcoded to localhost), but Aspire injects real Atlas connection string into `ConnectionStrings:mongodb`. These paths never intersect, causing timeout when connecting to localhost.

**Solution:** Added fallback logic in `AddMongoDbPersistence`:
1. Check if `MongoDB:ConnectionString` is empty or equals `mongodb://localhost:27017`
2. If so, read `ConnectionStrings:mongodb` and overlay into MongoDB config
3. Cleared localhost default from `appsettings.Development.json`

**Impact:**
- AppHost runs clean — Aspire injection works
- Standalone + user secrets works  
- Explicit config takes priority
- Tests unaffected (TestContainers)

**Deliverables:**
- Updated `src/Persistence.MongoDb/ServiceCollectionExtensions.cs`
- Updated `src/Web/appsettings.Development.json`
- Added decision entry to `.squad/decisions.md`

**Pattern Established:** When two config systems disagree, bridge them at DI registration layer using config overlay before binding Options.

### Issue #90 - Auth0ClaimsTransformation Pass 3 Auto-Detect (2026-03-29)

**Problem:** `TransformAsync` had two passes for mapping Auth0 role claims to `ClaimTypes.Role`.
If `Auth0:RoleClaimNamespace` was misconfigured (missing/empty/wrong), AND Auth0 sent roles under
a custom namespace (not the bare `"roles"` claim), both passes missed the roles — users appeared
to have no roles, breaking Profile display and NavMenu admin visibility.

**Solution:** Added Pass 3 to `TransformAsync` that triggers only when `rolesAdded == 0` after
Passes 1 & 2. It scans all claims on the principal for any type ending in `/roles` using the
helper `IsLikelyRoleClaimType`. Logs an informational message pointing developers to configure
`Auth0:RoleClaimNamespace` for best-practice explicit mapping.

**Impact:**
- Admins with misconfigured namespace still see their roles (silent failure eliminated)
- Pass 3 is fully idempotent — `MapRoleClaims` deduplicates via `identity.HasClaim()`
- Updated two tests that expected roles NOT to be added with missing/empty namespace config

**Deliverables:**
- Updated `src/Web/Auth/Auth0ClaimsTransformation.cs` (Pass 3 + `IsLikelyRoleClaimType` helper)
- Updated `tests/Web.Tests.Bunit/Auth/Auth0ClaimsTransformationTests.cs` (2 test renames + assertion flip)
- Decision inbox: `.squad/decisions/inbox/sam-pass3-auto-detect.md`

**Pattern Established:** Defensive claim scanning (Pass 3) should come *after* explicit config-driven
passes and fire only as a fallback. Always log a hint when auto-detection fires so operators know to
set the proper config key.

### 2026-03-29 — Auth0 Role Claim Auto-Detect (Pass 3) (Sprint 2 Complete)

**Role:** Backend - API & Data Layer

**Work:**
- Added Pass 3 to Auth0ClaimsTransformation.TransformAsync (Issue #90)
- Pass 3 scans all claims for types ending in `/roles` when Passes 1–2 find nothing
- Updated Auth0ClaimsTransformationTests.cs with 2 test cases
- Belt-and-suspenders safety net for misconfigured namespace

**Implementation Details:**
- Auto-detect pattern: `claim.Type.EndsWith("/roles", StringComparison.OrdinalIgnoreCase)`
- Scans all claims when Pass 1 (namespace lookup) and Pass 2 (bare "roles") both return empty
- Maps detected claim to `ClaimTypes.Role`
- Prevents silent Admin role failure in NavMenu when namespace is misconfigured

**Integration:** Works with Aragorn's namespace configuration + Legolas's Profile.razor hardening for layered defense.

**Test Coverage:** 2 new test cases verify Pass 3 auto-detect catches namespaced role claims.

**Outcome:** ✓ Build clean, all Auth0 transformation tests passing.

### 2026-04-01 — PR #158 Review Feedback (UserManagementService Architecture Issues)

**Role:** Backend - API & Data Layer

**Status:** 🔴 **BLOCKED** — Architecture test failures, awaiting fixes before re-review

**PR:** #158 — feat: Implement UserManagementService wrapping Auth0 Management API  
**Branch:** squad/131-user-management-service  
**Reviewer:** Aragorn (architecture), Gandalf (security)

**Blocking Issues:**

1. **Architecture.Tests Failure — AuditLogRepository**
   - **Problem:** `AuditLogRepository` is named like a repository but does NOT implement `IRepository<T>` interface
   - **Tests Failing:**
     - `Architecture.Tests.CodeStructureTests.Repositories_ShouldImplementIRepository`
     - `Architecture.Tests.AdvancedArchitectureTests.AllRepositories_ShouldImplementIRepository`
   - **Team Convention:** Any class named `*Repository` MUST implement `IRepository<T>` per Architecture.Tests enforcement
   - **Fix Options:**
     - **(Option A)** Make `AuditLogRepository` implement `IRepository<RoleChangeAuditEntry>` (if truly a repository CRUD pattern)
     - **(Option B)** Rename to `AuditLogWriter` or `AuditLogService` (recommended — audit logs are write-only append operations, not CRUD)
   - **Recommendation:** Option B is likely correct — write-only services should not be named `*Repository`

2. **Duplicate .squad/ File in Diff**
   - **Problem:** `.squad/decisions/inbox/gandalf-auth0-management-api.md` appears in PR #158's diff, but was already added in PR #146
   - **Root Cause:** PR #158's branch was created before PR #146 merged to main
   - **Fix:** Rebase on latest main after PR #146 merges; the ADR file will disappear from the diff

**Next Steps:**
1. Fix `AuditLogRepository` naming/implementation issue (choose Option A or Option B)
2. Rebase on main after PR #146 merges
3. Re-run full CI (`dotnet test IssueTrackerApp.slnx`) to confirm Architecture.Tests pass
4. Submit PR for re-review

**Security Review (Gandalf):** ✅ **APPROVED WITH NOTES**
- Strong security fundamentals across the board
- M2M token caching, input validation, secrets hygiene all pass
- Rate limit retry is acceptable technical debt (not a security blocker)
- See `.squad/decisions.md` for full security review details

**Quality Notes (Aragorn):**
- Excellent implementation of ADR #130 strategy
- M2M token caching correctly implements 24h TTL − 5 min safety margin
- Role ID mapping cache (30 min TTL) is well-designed
- File headers and VSA compliance all present
- Implementation quality is high — just need architecture convention fix

**Pattern Established:** When naming classes, respect team conventions enforced by Architecture.Tests. If a class violates naming patterns (e.g., `*Repository` but doesn't implement `IRepository<T>`), rename it to reflect its true purpose (`*Writer`, `*Service`, etc.) rather than forcing the pattern.

## Learnings — Issue #149 (Labels Domain Model)

**Date:** 2026-04-01
**Branch:** squad/149-labels-domain-model
**PR:** #168

### What was done
Added `Labels` as `List<string>` to `Issue` model and `IReadOnlyList<string>` to `IssueDto` positional record. Updated `IssueMapper` (both ToDto and ToModel), `IssueConfiguration` (EF Core Mongo `builder.Property(i => i.Labels)`), `IssueDto.Empty`, and all 5 test helper `CreateTestIssueDto()` methods across Domain.Tests, Web.Tests, and Web.Tests.Bunit.

### Key decisions
- Appended `Labels` **after** `VotedBy` in the positional record to avoid breaking any existing positional construction calls — safest approach for positional records.
- Used `[..dto.Labels]` spread syntax in `IssueMapper.ToModel` for consistent defensive copy (same pattern as VotedBy).

### Pitfall encountered
Local branch name mismatch: `git checkout -b squad/149-labels-domain-model` left HEAD on a different branch because `squad/149-labels-domain-model` already existed locally. Always verify `git branch --show-current` after checkout before committing.
