# AGENTS.md

- Follow `.github/copilot-instructions.md`, `.squad/team.md`, and `.squad/routing.md` before autonomous work. Squad branches use `squad/{issue-number}-{slug}`; do not commit `.squad/` files on `feature/*` branches.

## Architecture map
- `src/AppHost/AppHost.cs` is the local-dev entry point: it wires `web` to `ConnectionStrings:mongodb`, provisions `redis` (with RedisCommander in Development), waits for Redis, and health-checks `/health`.
- `src/Web/Program.cs` is the real composition root: DI, Auth0, MediatR, minimal APIs, SignalR, background services, and startup seeding all start here.
- `src/ServiceDefaults/Extensions.cs` centralizes OpenTelemetry, service discovery, resilience, and the `/health` + `/alive` endpoints.
- `src/Domain/` holds business logic in vertical slices under `Features/*`; shared contracts live in `DTOs/`, `Models/`, and `Abstractions/` (`Result<T>`, `IRepository<T>`). Feature slices: Issues, Comments, Attachments, Categories, Statuses, Analytics, Dashboard, Notifications.
- `src/Domain/Events/` holds domain event records (`IssueCreatedEvent`, `IssueUpdatedEvent`, `IssueAssignedEvent`, `IssueStatusChangedEvent`, `CommentAddedEvent`) published via MediatR `INotificationHandler`.
- `src/Domain/Features/Notifications/` holds `INotificationHandler` implementations that react to domain events and enqueue emails via `QueueEmailCommand`.
- `src/Persistence.MongoDb/` is the Mongo persistence layer: `IssueTrackerDbContext`, EF Core Mongo configurations, and the generic `Repository<T>` implementation.
- `src/Persistence.AzureStorage/` is optional file storage; `Web` falls back to `LocalFileStorageService` when `BlobStorage:ConnectionString` is missing.
- `src/Web/Components/Theme/` holds `ThemeProvider.razor` and `ThemeSelector.razor` for system-aware dark mode with 4 colour schemes.
- `src/Web/Components/Charts/` holds `BarChart.razor`, `LineChart.razor`, and `PieChart.razor` Blazor components used in the Analytics admin page.
- `src/Web/Helpers/` holds `ObjectIdJsonConverter` (registered globally in `Program.cs` for MongoDB `ObjectId` ↔ JSON string) and `CsvExportHelper` (reflection-based CSV export used by bulk-export).

## Request and data flow
- Typical flow is `Web` page/endpoint -> `src/Web/Services/*` facade -> MediatR command/query in `src/Domain/Features/*` -> `IRepository<T>` -> MongoDB.
- Example: `src/Web/Services/IssueService.cs` sends `CreateIssueCommand`; `src/Domain/Features/Issues/Commands/CreateIssueCommand.cs` maps DTOs to models and persists through repositories.
- Minimal API endpoints live in **two** directories: `src/Web/Endpoints/` (`CategoryEndpoints.cs`, `StatusEndpoints.cs`) and `src/Web/Features/` (`CommentEndpoints.cs`, `AttachmentEndpoints.cs`). Both follow the same route-group + `AdminPolicy` / `RequireAuthorization` pattern shown in `CategoryEndpoints.cs`.
- Startup seeds categories and statuses via `src/Web/Data/DataSeeder.cs`; `InitializeMongoDbAsync()` and seeding are skipped when `Environment == "Testing"`.
- Comment flow: `CommentEndpoints` → `CommentService` → MediatR → `IRepository<Comment>` → MongoDB.
- Attachment flow: `AttachmentEndpoints` → `AttachmentService` → MediatR → `IFileStorageService` (Azure Blob or `LocalFileStorageService`).
- Analytics flow: `AnalyticsService` → MediatR `Domain/Features/Analytics` queries → MongoDB aggregation; results are cached in `IMemoryCache` with a 5-minute TTL.
- Dashboard flow: `DashboardService` → `GetUserDashboardQuery` → MongoDB; Dashboard page uses `@attribute [StreamRendering]`.
- Bulk operations: `IBulkOperationService` enqueues operations on `InMemoryBulkOperationQueue` (`IBulkOperationQueue`); `BulkOperationBackgroundService` dequeues and dispatches MediatR commands from `Domain/Features/Issues/Commands/Bulk/`. Undo is provided by `InMemoryUndoService`.
- Email pipeline: domain events (`src/Domain/Events/`) are published with MediatR `IPublisher`; `Domain/Features/Notifications/` handlers react and dispatch `QueueEmailCommand`; `EmailQueueBackgroundService` polls the queue every 10 s and sends via `IEmailService` (SendGrid if `SendGrid:ApiKey` is set, SMTP otherwise).

## Conventions enforced by tests
- `tests/Architecture.Tests/LayerDependencyTests.cs` enforces boundaries: `Domain` cannot depend on `Web` or `Persistence.*`; `Persistence.*` cannot depend on `Web`.
- `tests/Architecture.Tests/NamingConventionTests.cs`, `AdvancedArchitectureTests.cs`, and `CodeStructureTests.cs` enforce the repo's shape: commands end with `Command`, queries with `Query`, validators with `Validator`, handlers with `Handler` and should be `sealed`.
- CQRS types belong under `Domain.Features.*`; validators live in `Validators` or next to commands; DTOs in `Domain.DTOs` should be records.
- Repositories must implement `IRepository<>`; preserve `Result<T>` / `ResultErrorCode` for expected failures instead of switching to exception-driven control flow.
- Commands may reside in either a `Commands` or a `Notifications` namespace; `AdvancedArchitectureTests.cs` explicitly allows both locations.

## Integrations to respect
- Auth0 is configured in `src/Web/Program.cs`; role claim remapping happens in `src/Web/Auth/Auth0ClaimsTransformation.cs`, and policy names/constants live in `src/Web/Auth/*`.
- `src/Persistence.MongoDb/ServiceCollectionExtensions.cs` falls back from `MongoDB:ConnectionString` to `ConnectionStrings:mongodb`, which matters under Aspire and in tests.
- Redis is provisioned in `AppHost` and health-check helpers exist in `ServiceDefaults`, but current app code leans on `IMemoryCache` more than distributed cache.
- SignalR updates flow through `src/Web/Hubs/IssueHub` and notification-aware services such as `IssueService`.

## Developer workflow
- From repo root: `dotnet restore`, `dotnet build`, `dotnet test IssueTrackerApp.slnx`.
- Run locally through Aspire: `dotnet run --project src/AppHost/AppHost.csproj`.
- UI changes: `src/Web/Web.csproj` auto-runs `npm install` (if needed) and `npm run css:build`; use `npm run css:watch` in `src/Web` for Tailwind iteration.
- Integration tests need Docker; `tests/Persistence.MongoDb.Tests.Integration/MongoDbFixture.cs` and `tests/Web.Tests.Integration/CustomWebApplicationFactory.cs` use `mongo:7.0` with replica set `rs0`. CI can override with `MONGODB_CONNECTION_STRING`.
- NuGet versions are centralized in `Directory.Packages.props`; do not add package versions in individual `.csproj` files.
