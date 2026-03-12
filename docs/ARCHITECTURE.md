# Architecture Overview

This document describes the solution architecture of IssueTrackerApp, including project responsibilities, data flow, and key design decisions.

## Solution Structure

```
IssueTrackerApp/
├── src/
│   ├── AppHost/                  # .NET Aspire Orchestration
│   ├── ServiceDefaults/          # Shared Infrastructure
│   ├── Web/                      # Blazor Server Application
│   ├── Domain/                   # Business Logic Layer
│   └── Persistence.MongoDb/      # Data Access Layer
├── tests/                        # Test Projects
├── docs/                         # Documentation
└── Directory.Packages.props      # Centralized Package Versioning
```

---

## Project Responsibilities

### AppHost (Aspire Orchestration)

**Purpose**: Orchestrates all services and resources using .NET Aspire.

**Responsibilities**:

- Define and configure application resources (Web, MongoDB, Redis)
- Manage service discovery and connection strings
- Configure environment-specific settings
- Provide local development dashboard

**Key Files**:

- `AppHost.cs` - Resource definitions and orchestration

### ServiceDefaults (Cross-Cutting Concerns)

**Purpose**: Provides shared infrastructure services used by all projects.

**Responsibilities**:

- OpenTelemetry configuration (tracing, metrics, logging)
- Health check registration (MongoDB, Redis)
- HTTP resilience policies (retries, circuit breakers)
- Service discovery configuration
- Azure Application Insights integration

**Extension Methods**:

- `AddServiceDefaults()` - Registers all shared services

### Web (Blazor Server Application)

**Purpose**: User interface and authentication layer.

**Responsibilities**:

- Blazor Interactive Server rendering
- Authentication endpoints (Auth0 integration)
- Theme management (dark mode, color schemes)
- TailwindCSS styling with MSBuild integration
- API endpoint exposure

**Key Components**:

- `Components/Theme/ThemeProvider.razor` - Cascading theme state
- `Components/Theme/ThemeToggle.razor` - UI for theme switching
- `Auth/` - Login/logout endpoints with security hardening

**Frontend Stack**:

- TailwindCSS v4 with `@tailwindcss/cli`
- JavaScript interop for localStorage persistence
- Responsive design with dark mode support

### Domain (Business Logic Layer)

**Purpose**: Core business entities, abstractions, and DTOs.

**Responsibilities**:

- Define domain models (Issue, Category, Status, Comment, User)
- Define data transfer objects (DTOs)
- Provide Result&lt;T&gt; pattern for error handling
- Define repository interfaces (IRepository&lt;T&gt;)

**Models**:

| Model | Description |
|-------|-------------|
| `Issue` | Main entity with title, description, status, category |
| `Category` | Issue categorization |
| `Status` | Issue workflow states |
| `Comment` | Issue comments with embedded user |
| `User` | Embedded user document (not standalone collection) |

**Abstractions**:

- `Result<T>` - Success/failure result with error codes
- `ResultErrorCode` - Enum for error classification (NotFound, Validation, Concurrency, Conflict)
- `IRepository<T>` - Generic repository interface

**DTOs**:

- `IssueDto`, `CategoryDto`, `StatusDto`, `CommentDto`, `UserDto`
- `PaginatedResponse<T>` - Pagination wrapper

### Persistence.MongoDb (Data Access Layer)

**Purpose**: MongoDB data persistence using Entity Framework Core.

**Responsibilities**:

- `IssueTrackerDbContext` - EF Core context for MongoDB
- Entity configurations (Fluent API)
- Repository implementations
- Service registration extensions

**Key Files**:

- `IssueTrackerDbContext.cs` - DbContext with MongoDB provider
- `Configurations/` - Entity type configurations
- `Repositories/` - Repository implementations
- `ServiceCollectionExtensions.cs` - DI registration

---

## Data Flow Diagram

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                                  AppHost                                     │
│                        (.NET Aspire Orchestration)                          │
│  ┌─────────────┐    ┌─────────────┐    ┌─────────────┐    ┌─────────────┐   │
│  │   Web App   │    │   MongoDB   │    │    Redis    │    │  Dashboard  │   │
│  └─────────────┘    └─────────────┘    └─────────────┘    └─────────────┘   │
└─────────────────────────────────────────────────────────────────────────────┘
         │                    │                 │
         │                    │                 │
         ▼                    │                 │
┌─────────────────────────────│─────────────────│──────────────────────────────┐
│                          Web│(Blazor Server)  │                              │
│  ┌─────────────────┐       │                 │                              │
│  │   Browser       │       │                 │                              │
│  │  (User Agent)   │       │                 │                              │
│  └────────┬────────┘       │                 │                              │
│           │                │                 │                              │
│           ▼                │                 │                              │
│  ┌─────────────────┐       │                 │                              │
│  │  Auth Endpoints │       │                 │                              │
│  │  (Auth0 PKCE)   │       │                 │                              │
│  └────────┬────────┘       │                 │                              │
│           │                │                 │                              │
│           ▼                │                 │                              │
│  ┌─────────────────┐       │                 │                              │
│  │ Blazor Server   │       │                 │                              │
│  │   Components    │◄──────┼─────────────────┤  Redis Cache                 │
│  │  + ThemeProvider│       │                 │  (Session/Data)              │
│  └────────┬────────┘       │                 │                              │
│           │                │                 │                              │
└───────────┼────────────────┼─────────────────┼──────────────────────────────┘
            │                │                 │
            ▼                │                 │
┌───────────────────────────┐│                 │
│        Domain             ││                 │
│  ┌─────────────────┐      ││                 │
│  │    Models       │      ││                 │
│  │ Issue, Category │      ││                 │
│  │ Status, Comment │      ││                 │
│  └────────┬────────┘      ││                 │
│           │               ││                 │
│  ┌────────▼────────┐      ││                 │
│  │    Result<T>    │      ││                 │
│  │  Error Handling │      ││                 │
│  └────────┬────────┘      ││                 │
│           │               ││                 │
│  ┌────────▼────────┐      ││                 │
│  │  IRepository<T> │      ││                 │
│  │   (Interface)   │      ││                 │
│  └────────┬────────┘      ││                 │
└───────────┼───────────────┘│                 │
            │                │                 │
            ▼                ▼                 │
┌───────────────────────────────────────────┐  │
│     Persistence.MongoDb                   │  │
│  ┌─────────────────────────────────────┐  │  │
│  │     IssueTrackerDbContext           │  │  │
│  │   (MongoDB.EntityFrameworkCore)     │  │  │
│  └─────────────────────────────────────┘  │  │
│                    │                      │  │
│                    ▼                      │  │
│  ┌─────────────────────────────────────┐  │  │
│  │       MongoDB Atlas                 │◄─┼──┘
│  │    (Document Database)              │  │
│  └─────────────────────────────────────┘  │
└───────────────────────────────────────────┘
```

---

## Key Design Patterns

### Result&lt;T&gt; Pattern

Used throughout the application for explicit error handling:

```csharp
public class Result<T>
{
    public bool Success { get; }
    public T? Value { get; }
    public string? Error { get; }
    public ResultErrorCode ErrorCode { get; }
}

public enum ResultErrorCode
{
    None = 0,
    Concurrency = 1,
    NotFound = 2,
    Validation = 3,
    Conflict = 4
}
```

**Benefits**:

- No exceptions for expected failures
- Explicit error handling at call sites
- Structured error information for logging/UI

### Repository Pattern

Generic repository interface for data access abstraction:

```csharp
public interface IRepository<T> where T : class
{
    Task<Result<T>> GetByIdAsync(string id, CancellationToken ct = default);
    Task<Result<IEnumerable<T>>> GetAllAsync(CancellationToken ct = default);
    Task<Result> AddAsync(T entity, CancellationToken ct = default);
    Task<Result> UpdateAsync(T entity, CancellationToken ct = default);
    Task<Result> DeleteAsync(string id, CancellationToken ct = default);
}
```

### Embedded Documents

User information is embedded within documents rather than referenced:

```csharp
public class Comment
{
    public string Id { get; set; }
    public string Text { get; set; }
    public User Author { get; set; }  // Embedded, not referenced
    public DateTime CreatedAt { get; set; }
}
```

**Benefits**:

- Single query retrieves all needed data
- No joins required
- Denormalized for read performance

---

## Security Architecture

### Authentication Flow

```
┌─────────┐    ┌─────────┐    ┌─────────┐    ┌─────────┐
│ Browser │───►│  /login │───►│  Auth0  │───►│/callback│
└─────────┘    └─────────┘    └─────────┘    └─────────┘
                                   │              │
                                   │  PKCE Code   │
                                   │  Exchange    │
                                   ▼              ▼
                              ┌─────────────────────┐
                              │  Session Cookie     │
                              │  (HttpOnly, Secure) │
                              └─────────────────────┘
```

### Authorization Policies

| Policy | Description |
|--------|-------------|
| `Admin` | Full access to all operations |
| `User` | Read access and own resource modifications |

### Security Features

- **Authorization Code + PKCE**: Secure OAuth 2.0 flow
- **Open Redirect Protection**: Validates redirect URLs
- **CSRF Protection**: Antiforgery tokens on state-changing operations
- **Secure Cookies**: HttpOnly, Secure, SameSite=Strict

---

## Observability

### OpenTelemetry Integration

```
┌─────────────────────────────────────────────────────────────────┐
│                     ServiceDefaults                              │
│  ┌────────────┐  ┌────────────┐  ┌────────────┐  ┌────────────┐ │
│  │  Tracing   │  │  Metrics   │  │  Logging   │  │  Health    │ │
│  │  (OTLP)    │  │  (OTLP)    │  │  (OTLP)    │  │  Checks    │ │
│  └─────┬──────┘  └─────┬──────┘  └─────┬──────┘  └─────┬──────┘ │
└────────┼───────────────┼───────────────┼───────────────┼────────┘
         │               │               │               │
         ▼               ▼               ▼               ▼
   ┌─────────────────────────────────────────────────────────────┐
   │                Azure Application Insights                    │
   │           (or any OTLP-compatible backend)                   │
   └─────────────────────────────────────────────────────────────┘
```

### Health Endpoints

| Endpoint | Purpose |
|----------|---------|
| `/health` | Overall application health |
| `/health/ready` | Readiness probe (all dependencies) |
| `/health/live` | Liveness probe (application running) |

---

## Caching Strategy

### Redis Integration

- **Session State**: User session data
- **Data Caching**: Frequently accessed entities
- **Distributed Locking**: Concurrency control

### Cache Invalidation

- Write-through on updates
- TTL-based expiration
- Manual invalidation on deletes

---

## Testing Strategy

| Test Type | Framework | Purpose |
|-----------|-----------|---------|
| Unit | xUnit + NSubstitute | Business logic isolation |
| Integration | xUnit + TestContainers | Full stack with real MongoDB |
| Component | bUnit | Blazor component testing |
| E2E | Playwright | Browser-based scenarios |
| Architecture | xUnit | Project dependency validation |

---

## DevOps Pipeline

### GitHub Workflows (14 configured)

| Workflow | Trigger | Purpose |
|----------|---------|---------|
| `squad-ci.yml` | Push/PR | Build and test |
| `squad-test.yml` | Push/PR | Comprehensive test suite |
| `squad-docs.yml` | Push | DocFX documentation |
| `squad-release.yml` | Tag | Production release |
| `squad-preview.yml` | PR | Preview deployments |
| `squad-promote.yml` | Manual | Environment promotion |
| Others | Various | Triage, labeling, etc. |

---

## Future Considerations

- **API Versioning**: Implement URL-based API versioning
- **Event Sourcing**: Consider for audit trail requirements
- **GraphQL**: Evaluate for complex query scenarios
- **Microservices**: Potential split as application grows

---

## CQRS Implementation

The application uses MediatR to implement the Command Query Responsibility Segregation (CQRS) pattern. Each feature is organized in vertical slices under `Domain/Features/`.

### Feature Structure

```
Domain/Features/
├── Issues/
│   ├── Commands/
│   │   ├── CreateIssueCommand.cs
│   │   ├── UpdateIssueCommand.cs
│   │   ├── DeleteIssueCommand.cs
│   │   └── ChangeIssueStatusCommand.cs
│   ├── Queries/
│   │   ├── GetIssueByIdQuery.cs
│   │   ├── GetIssuesQuery.cs
│   │   └── SearchIssuesQuery.cs
│   └── Validators/
│       ├── CreateIssueCommandValidator.cs
│       └── UpdateIssueCommandValidator.cs
├── Comments/
│   ├── Commands/
│   │   ├── AddCommentCommand.cs
│   │   ├── UpdateCommentCommand.cs
│   │   └── DeleteCommentCommand.cs
│   ├── Queries/
│   │   └── GetIssueCommentsQuery.cs
│   └── Validators/
│       ├── AddCommentCommandValidator.cs
│       └── UpdateCommentCommandValidator.cs
├── Categories/
│   ├── Commands/
│   │   ├── CreateCategoryCommand.cs
│   │   ├── UpdateCategoryCommand.cs
│   │   └── ArchiveCategoryCommand.cs
│   ├── Queries/
│   │   ├── GetCategoryByIdQuery.cs
│   │   └── GetCategoriesQuery.cs
│   └── Validators/
│       ├── CreateCategoryCommandValidator.cs
│       └── UpdateCategoryCommandValidator.cs
├── Statuses/
│   ├── Commands/
│   │   ├── CreateStatusCommand.cs
│   │   ├── UpdateStatusCommand.cs
│   │   └── ArchiveStatusCommand.cs
│   ├── Queries/
│   │   ├── GetStatusByIdQuery.cs
│   │   └── GetStatusesQuery.cs
│   └── Validators/
│       ├── CreateStatusCommandValidator.cs
│       └── UpdateStatusCommandValidator.cs
└── Dashboard/
    └── Queries/
        └── GetUserDashboardQuery.cs
```

### Command/Query Pattern

Commands modify state and return `Result<T>` for success/failure handling:

```csharp
public record AddCommentCommand(
    string IssueId,
    string Title,
    string Description,
    UserDto Author) : IRequest<Result<CommentDto>>;
```

Queries return data without side effects:

```csharp
public record GetIssueCommentsQuery(string IssueId) : IRequest<Result<IEnumerable<CommentDto>>>;
```

---

## Search Query Implementation

The `SearchIssuesQuery` provides comprehensive search functionality with multiple filter options.

### Search Request Model

```csharp
public record IssueSearchRequest
{
    public string? SearchText { get; init; }      // Full-text search on title/description
    public string? StatusFilter { get; init; }    // Filter by status name
    public string? CategoryFilter { get; init; }  // Filter by category name
    public string? AuthorId { get; init; }        // Filter by author
    public DateOnly? DateFrom { get; init; }      // Date range start
    public DateOnly? DateTo { get; init; }        // Date range end
    public bool IncludeArchived { get; init; }    // Include archived issues
    public int Page { get; init; } = 1;           // Pagination page
    public int PageSize { get; init; } = 20;      // Items per page
}
```

### Filter Pipeline

The search handler applies filters in sequence:
1. Archive filter (exclude archived by default)
2. Text search (title and description, case-insensitive)
3. Status filter (exact match)
4. Category filter (exact match)
5. Author filter (by user ID)
6. Date range filter (created date)
7. Pagination (offset-based)

### URL Parameter Persistence

Search state is synchronized with URL query parameters, enabling:
- Bookmarkable search results
- Browser back/forward navigation
- Shareable filtered views

---

## PagedResult Pattern

The `PagedResult<T>` record provides standardized pagination metadata for API responses.

### Structure

```csharp
public record PagedResult<T>
{
    public IReadOnlyList<T> Items { get; init; }
    public int TotalCount { get; init; }
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
    public bool HasPreviousPage => Page > 1;
    public bool HasNextPage => Page < TotalPages;
}
```

### Usage Example

```csharp
var result = PagedResult<IssueDto>.Create(
    items: pagedIssues,
    totalCount: totalCount,
    page: request.Page,
    pageSize: request.PageSize);
```

### Benefits

- Consistent pagination across all list endpoints
- Self-contained metadata for UI pagination controls
- Factory method for easy instantiation
- Computed properties for navigation logic
