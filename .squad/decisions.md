# IssueTrackerApp Decisions

This file records team decisions that affect architecture, scope, and process.

---

## Decisions

### Project Structure & Setup

#### .NET Aspire Project Structure (2026-03-12)

**Author:** Sam (Backend Developer)

Implemented an Aspire-based solution structure:

- **AppHost**: Orchestration with MongoDB and Redis containers
- **ServiceDefaults**: Shared configurations for OpenTelemetry, service discovery, resilience
- **Web**: Blazor Server with Interactive Server rendering
- **Domain**: CQRS with MediatR and FluentValidation
- **Persistence.MongoDb**: MongoDB data access with Entity Framework Core provider

**Rationale:** Aspire orchestration simplifies local development; vertical slice architecture enables clean feature organization.

---

#### Aspire AppHost Configuration (2026-03-12)

**Author:** Sam (Backend Developer)

Enhanced AppHost with comprehensive orchestration:

- Containerized MongoDB with MongoExpress UI
- Containerized Redis with RedisCommander UI
- OpenTelemetry configured with OTLP exporter for distributed tracing
- Azure Monitor optional integration via Application Insights
- Health checks on `/health` (readiness) and `/alive` (liveness) endpoints

**Rationale:** Simplified local development with containerized dependencies; production-ready telemetry from day one.

---

### Data Persistence

#### MongoDB Persistence Setup (2026-03-12)

**Author:** Sam (Backend Developer)

Established MongoDB persistence patterns:

1. **Result<T> pattern** for all repository operations (no exception-based control flow)
2. **Generic IRepository<T>** with base implementation
3. **MongoDB.EntityFrameworkCore** provider for EF Core patterns and LINQ support
4. **Strongly-typed MongoDbSettings** with validation on startup
5. **DbContext and DbContextFactory** registration for flexible context usage
6. **Structured logging** in repositories for observability

**Rationale:** Result pattern enables explicit error handling; generic repository reduces duplication; structured logging integrates with OpenTelemetry.

---

#### Value Object & Mapper Infrastructure (2026-03-14)

**Author:** Sam (Backend Developer)

Foundation for DTO-Model separation:

- **Value objects** (`UserInfo`, `CategoryInfo`, `StatusInfo`) as `sealed class` in `Domain.Models`
- **Static mappers** in `Domain.Mappers` for entity ↔ DTO conversions
- BSON attributes match current DTO serialization — no MongoDB migration needed
- Value objects nest for clean DDD composition

**Consequence:** Enables DTO-Model separation sprint without data migration risk.

---

#### DTO–Model Separation (2026-03-14)

**Author:** Aragorn (Lead Developer)

Enforced strict DTO–Model separation across all layers:

- **Models** interact with database (only persistence concern)
- **DTOs** for inter-layer data transfer (immutable records)
- **Mappers** provide explicit, testable bidirectional conversion
- **Value Objects** replace embedded DTO properties in Models

**Conversion Flow:** UI → DTO → Mapper.ToInfo() → Model → Repository → MongoDB

**Notable Change:** `Comment.Issue` → `Comment.IssueId` (ObjectId reference) breaks circular dependency.

**Scope:** ~140 files affected; implementation tracked in sprint plan.

---

#### Comment.Issue → Comment.IssueId Refactoring (2026-03-14)

**Author:** Sam (Backend Developer)

Replaced `IssueDto Issue` with `ObjectId IssueId` in Comment model:

- Breaks circular dependency between Comment and Issue DTOs
- Follows MongoDB best practice (reference by ID, not embedding full documents)
- Simplifies serialization (ObjectId is primitive, no nested owned type config)
- Consistent with Attachment model pattern

**Impact:** Comment handlers must use `comment.IssueId` directly; handlers needing full issue data must load separately.

---

### Security & Authentication

#### Auth0 Authentication Implementation (2026-03-12)

**Author:** Gandalf (Security Officer)

Implemented Auth0 authentication with:

- **OAuth2 Authorization Code flow** with PKCE
- **JWT tokens** from Auth0
- **Policy-based authorization** with roles (AdminPolicy, UserPolicy)
- **HTTPS enforcement**, antiforgery protection, secure cookies
- **Strongly-typed Auth0Options** configuration
- **Blazor CascadingAuthenticationState** for component-level auth

**Security Features:**
✅ JWT validation (audience/issuer)  
✅ PKCE prevents authorization code interception  
✅ HttpOnly, Secure, SameSite cookie attributes  
✅ Placeholder configuration (no secrets in git)  

**Alternatives Rejected:**

- ASP.NET Core Identity (more maintenance burden)
- Azure AD B2C (more complex configuration)
- Self-hosted IdentityServer (operational overhead)

---

### Testing

#### Azure Storage Test Projects (2026-03-14)

**Author:** Sam (Backend Developer)

Chose **Testcontainers.Azurite** for integration testing:

**Why Testcontainers.Azurite:**

- ✅ Cross-platform (Linux, macOS, Windows)
- ✅ Docker-based containers, clean isolation
- ✅ Works in CI/CD pipelines
- ✅ Actual Azure SDK against real emulator
- ✅ Consistent with existing Testcontainers.MongoDb pattern

**Alternatives Rejected:**

- Azure Storage Emulator (Windows-only, deprecated)
- In-memory mocks (doesn't test real SDK behavior)
- Real Azure Storage (requires credentials, costs, slower)

---

#### Azure Storage Unit Test Strategy (2026-03-14)

**Author:** Gimli (Tester)

**Focus unit tests on mockable code paths; defer unmockable happy paths to integration tests.**

Unit test coverage:

1. Constructor validation (ArgumentNullException paths)
2. Settings class defaults and property setters
3. Upload operations with full mocking
4. Download/Delete/Thumbnail exception handling and logging
5. DI registration with various configuration scenarios

**Key Pattern:** `DownloadAsync` and `DeleteAsync` create `new BlobClient(Uri)` directly — bypass injected mocks. Focus on exception paths; integration tests cover happy paths.

**Result:** 33 unit tests across 7 files, all passing.

---

#### Azure Blob Storage Integration Test Strategy (2026-03-14)

**Author:** Gimli (Tester)

**Chosen Approach:** Azurite TestContainers with xUnit shared fixture pattern

Test isolation via unique container names per test. Coverage:

- **Upload Tests:** 5 tests (blob creation, auto-creation, content-type, unique naming)
- **Download Tests:** 4 tests (roundtrip, content verification, error handling)
- **Delete Tests:** 4 tests (idempotent deletes, selective deletion)
- **Thumbnail Tests:** 7 tests (ImageSharp integration, resize, aspect ratio, format conversion)
- **Concurrency Tests:** 6 tests (parallel operations, 10+ concurrent)

**Result:** 25+ tests, build successful. Requires Docker/Azurite to run.

---

### Process & Team Dynamics

#### PR Review Process (2026-03-12)

**Directive:** When reviewing PRs for merge, valid suggestions from reviewers (human or automated) must be implemented before merging. Invalid suggestions require a response explaining why they weren't applied. Never ignore suggestions.

---

#### Documentation Structure (2026-03-14)

**Author:** Frodo (Tech Writer)

Implemented **category-based organization** for `docs/LIBRARIES.md` package reference:

**Categories:**

- .NET Aspire Integration
- Data Access
- Application Patterns
- Authentication & Security
- Observability & Monitoring
- Health Checks
- Testing
- Blazor Component Testing
- End-to-End Testing
- Integration Testing Infrastructure

**Rationale:** Developers think in architectural domains, not alphabetically. Single source of truth from centralized `Directory.Packages.props`.

---

#### Frodo's Documentation Responsibilities (2026-03-12)

**Directive:** Frodo (Tech Writer) monitors and documents project changes:

1. Monitor changes and document them
2. Update README.md with significant changes
3. Maintain document listing all libraries and references used

---

### Architectural Directives

#### DTO-Model Separation Architectural Pattern (2026-03-14)

**Directive:** DTOs should only transfer records between application layers. Mappers must convert DTO ↔ Model. Only models interact with the database. This is a **mandatory architectural pattern** going forward.

---

#### bUnit Test Suite Optimization (2026-03-17)

**Author:** Gimli (Tester)

Diagnosed performance issues in bUnit test suite (595 tests):

**Problem:** Full suite execution hangs (~2+ minutes), while individual projects run in 1-7 seconds.

**Solution Implemented:**

- Created `tests/Web.Tests.Bunit/xunit.runner.json` with parallelization controls
- Disabled cross-collection parallelization to reduce BunitContext state conflicts
- Set `maxParallelThreads: 4` to balance throughput with resource usage

**Outstanding Issue:** Two delete tests in DetailsPageTests fail due to EventCallback chain not completing when modal is embedded in Details page. Investigation ongoing.

**Rationale:** Explicit parallelism control reduces test contention. EventCallback bug may reveal underlying resource leak affecting suite performance.

**Consequence:** bUnit tests are more stable; full suite optimization deferred pending bug fix.

---

#### bUnit Modal Button Selector Pattern (2026-03-15)

**Author:** Legolas (Frontend Dev)

**Problem:** When testing components with modals that share CSS classes with parent page buttons (e.g., both a header Delete button and a modal Confirm button use `bg-red-600`), `FindAll("button").FirstOrDefault(b => b.ClassList.Contains("bg-red-600"))` returns the first match in DOM order — typically the parent button, not the modal button.

**Decision:** Always scope bUnit element queries for modal buttons to the modal's container element using structural selectors like `[role='dialog']`:

```csharp
// ✅ Scoped — finds the confirm button inside the modal dialog
var confirmButton = cut.Find("[role='dialog'] .bg-red-600");
```

**Rationale:** Modal buttons often reuse Tailwind utility classes as page-level buttons; DOM order puts page buttons before modal buttons. Scoping to `[role='dialog']` is semantically correct and resilient to DOM changes.

**Impact:** Pattern established for all future bUnit tests involving modals.

---

### DI Lifetime & Dependency Resolution

#### DI Lifetime Alignment for DbContextFactory and Background Services (2026-03-17)

**Author:** Sam (Backend Developer)

**Context:** Application crashed on startup with `System.AggregateException` due to two DI lifetime validation failures:

1. `AddDbContext` registers options as scoped; `AddDbContextFactory` defaults to singleton → singleton factory cannot consume scoped options
2. `BulkOperationBackgroundService` (singleton) injected `INotificationService` (scoped) directly via constructor

**Decision:**

*Fix 1: Scoped DbContextFactory*
Pass `lifetime: ServiceLifetime.Scoped` to `AddDbContextFactory<IssueTrackerDbContext>()` so the factory matches the scoped options.

*Fix 2: Remove unused scoped dependency from singleton*
Removed `INotificationService` from constructor — it was stored as a field but never referenced. Service already uses `IServiceScopeFactory` to resolve scoped dependencies per-operation.

**Consequences:**

- App starts successfully without DI validation errors
- **Team rule:** When combining `AddDbContext` + `AddDbContextFactory`, always align lifetimes explicitly
- **Team rule:** Background services (singletons) must never inject scoped services directly; always resolve from `IServiceScopeFactory` within per-operation scopes

---

### Auth0 Role Claim Mapping via IClaimsTransformation (2026-03-19)

**Author:** Gandalf (Security Officer)

Implement **IClaimsTransformation** to map Auth0's custom role claims to ASP.NET Core's standard `ClaimTypes.Role` claim type.

**Problem:** Auth0 users with Admin and User roles were getting "Access Denied" when accessing protected pages despite having correct roles assigned. Root cause: Auth0 sends roles in a custom namespaced claim (e.g., `https://issuetracker.com/roles`), but ASP.NET Core's `RequireRole()` checks for claims with type `ClaimTypes.Role`.

**Solution:** Created `Auth0ClaimsTransformation` service that:

- Reads Auth0's custom role claim using configurable namespace
- Handles multiple role formats (JSON arrays, CSV, single values)
- Maps each role to standard `ClaimTypes.Role`
- Includes idempotency check and detailed logging
- Registered as scoped service in authentication pipeline

**Consequences:**

- ✅ Role-based authorization now works for Auth0 users
- ✅ Claims transformation is reusable and testable
- ✅ Configuration-driven design supports multiple environments
- ⚠️ Requires manual configuration of `RoleClaimNamespace` per environment
- ⚠️ Misconfiguration results in silent authorization failures (logs warning)

**Team Guidelines:**

1. Always configure `Auth0:RoleClaimNamespace` in user secrets (dev) or Key Vault (prod)
2. Match the namespace to Auth0 tenant's role claim
3. Check logs if users report "Access Denied"
4. Test with real Auth0 users assigned to Admin and User roles

---

### Navigation Menu Architecture (2026-03-13)

**Author:** Legolas (Frontend Developer)

Implemented a role-based sidebar navigation menu.

**Decision:** Built navigation around these patterns:

- **Sidebar Navigation:** Fixed 256px width left sidebar (only shown when authenticated)
- **Responsive Container:** Flex layout with header (top), sidebar (left), main content (right)
- **Role-Based Visibility:** Menu items filtered by authorization policies

**Technical Implementation:**

- Created `NavMenuComponent.razor` as standalone navigation component
- Integrated into `MainLayout.razor` within `<AuthorizeView>`
- Uses nested `AuthorizeView` components with custom context naming to avoid Razor conflicts
- User Policy items: Home, Dashboard, Issues, Create Issue
- Admin Policy items: Admin Dashboard, Categories, Statuses, Analytics
- Emoji icons for visual clarity (no icon library dependency)
- Full dark mode support via TailwindCSS

**Consequences:**

- ✅ Users can now navigate the application
- ✅ Clear separation between user and admin features
- ✅ Consistent with Blazor conventions
- ✅ Scalable pattern for adding more navigation items
- ⚠️ Sidebar always visible when authenticated (could add collapse in future)

---

### Switch AppHost MongoDB from Container to Atlas Connection String (2026-03-18)

**Author:** Boromir (DevOps)

Replaced container-based MongoDB orchestration with connection string from Atlas.

**Decision:** Replaced `AddMongoDB("mongodb")` with `builder.AddConnectionString("mongodb")` which reads `ConnectionStrings:mongodb` from AppHost User Secrets.

**Changes Made:**

1. Removed `AddMongoDB` + `WithMongoExpress` + `AddDatabase` from AppHost.cs
2. Removed `.WaitFor(mongodb)` (no container to wait for)
3. Removed `Aspire.Hosting.MongoDB` package reference

**Configuration Required:**
The Web project has two MongoDB connection paths that both need configuration:

**AppHost project** (for Aspire service discovery):

```
dotnet user-secrets set "ConnectionStrings:mongodb" "mongodb+srv://<user>:<pass>@<cluster>.mongodb.net/issuetracker-db" --project src/AppHost
```

**Web project** (for `MongoDbSettings` → EF Core provider):

```
dotnet user-secrets set "MongoDB:ConnectionString" "mongodb+srv://<user>:<pass>@<cluster>.mongodb.net" --project src/Web
dotnet user-secrets set "MongoDB:DatabaseName" "issuetracker-db" --project src/Web
```

**Consequences:**

- ✅ No Docker dependency for MongoDB in local development
- ✅ Can use shared MongoDB Atlas cluster for team
- ⚠️ MongoExpress UI no longer available (use MongoDB Compass or Atlas UI instead)
- ⚠️ Both `ConnectionStrings:mongodb` and `MongoDB:ConnectionString` must be configured
- 🔄 Future improvement: unify the two config paths to need only one connection string

---

### User Directive: MongoDB Atlas Connection (2026-03-17)

**By:** Matthew Paulosky (via Copilot)

**Directive:** MongoDB in AppHost must NOT use a container. Use a connection string to Atlas stored in User Secrets. Database names stay the same.

**Rationale:** User request to simplify local development and enable shared cluster usage across team.

---

## Summary of Key Principles

1. **DTO-Model Separation:** Clear boundaries between persistence and API contracts
2. **Result<T> Pattern:** Explicit error handling without exceptions
3. **Testcontainers for Integration:** Realistic testing without cloud dependencies
4. **Aspire Orchestration:** Simplified local development with containerized dependencies
5. **OpenTelemetry Observability:** Production-ready monitoring from day one
6. **Auth0 Identity:** Enterprise-grade security without maintenance burden
7. **Category-Based Documentation:** Developer-centric organization of resources
8. **bUnit Test Optimization:** Explicit parallelism control; defer full suite optimization until failing tests fixed
