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

### Issue Creation Resolves Status from Database (2026-03-18)

**Author:** Sam (Backend Developer)

**Context:** `CreateIssueCommandHandler` hardcoded a `StatusInfo` with `ObjectId.Empty` and `StatusName = "Open"` when creating new issues. This meant issues were not linked to actual Status documents in MongoDB, breaking status filtering and reporting.

**Decision:**

- Inject `IRepository<Status>` into `CreateIssueCommandHandler`
- Query for a non-archived Status with `StatusName == "Open"` via `FirstOrDefaultAsync`
- Map the result using `StatusMapper.ToInfo(Status)` (new overload)
- Fall back to the original hardcoded `StatusInfo` with a logged warning if the DB lookup fails

**Consequences:**

- ✅ Issues now reference real Status documents from the database
- ✅ Backward compatible — fallback ensures no crash if Status collection is empty
- ✅ Added `StatusMapper.ToInfo(Status?)` overload for direct model-to-value-object conversion
- ⚠️ Requires an "Open" status to exist in the database for full functionality
- ⚠️ Team should ensure seed data includes an "Open" status record

**Files Changed:**

- `src/Domain/Features/Issues/Commands/CreateIssueCommand.cs`
- `src/Domain/Mappers/StatusMapper.cs`

---

### Theme-Aware Layout Backgrounds & Inline SignalR Indicator (2026-03-18)

**Author:** Legolas (Frontend Developer)

**Decision — Theme-Aware Backgrounds:** MainLayout and header backgrounds now use `bg-primary-*` utilities instead of static `bg-gray-*`. The page body uses primary-950 (light mode) / primary-50 (dark mode); the header uses primary-900 / primary-100.

**Rationale:** Backgrounds visually respond to the selected color theme (blue/red/green/yellow). The extreme ends of the palette (950/50) produce a subtle tint without overwhelming content. This leverages the existing CSS custom property system — no new infrastructure needed.

**Decision — SignalR Indicator Relocation:** `<SignalRConnection />` moved from a fixed bottom-right floating card into the header's right-side utility bar (after LoginDisplay). The component is now a compact inline dot with optional short text label.

**Rationale:** A floating card in the bottom-right corner overlapped content and felt disconnected from the UI. An inline status dot in the nav bar is less intrusive, immediately visible, and consistent with common SaaS UI patterns.

**Impact on Team:**

- **Gimli (Tester):** bUnit tests for MainLayout and SignalRConnection updated to reflect theme-aware class names and inline positioning. CSS class assertions changed from `bg-gray-*` to `bg-primary-*`. SignalR component tests no longer query for `.fixed` positioning or floating card structure.
- **Frodo (Docs):** README screenshots may need refreshing to show themed backgrounds.
- No backend changes needed — the component still uses the same `SignalRClientService`.

**Files Changed:**

- `src/Web/Components/Layout/MainLayout.razor`
- `src/Web/Components/Shared/SignalRConnection.razor`
- `src/Web/Styles/app.css`

---

### Test Update Pattern: CreateIssueCommandHandler Status Resolution Mocking (2026-03-18)

**Author:** Gimli (Tester)

**Context:** The `CreateIssueCommandHandler` now accepts `IRepository<Status>` and resolves the "Open" status from the database at issue creation time. This introduces a new test mocking pattern for status resolution.

**Decision:** All tests for `CreateIssueCommandHandler` must mock `IRepository<Status>.FirstOrDefaultAsync` with a default "not found" return (`Result.Ok<Status?>(null)`). Tests verifying specific status resolution behavior should override this default with the appropriate response.

**Pattern:**

```csharp
// Default in constructor: status not found → fallback
_statusRepository.FirstOrDefaultAsync(Arg.Any<Expression<Func<Status, bool>>>(), Arg.Any<CancellationToken>())
    .Returns(Result.Ok<Status?>(null));

// Override for specific test: status found in DB
_statusRepository.FirstOrDefaultAsync(Arg.Any<Expression<Func<Status, bool>>>(), Arg.Any<CancellationToken>())
    .Returns(Result.Ok<Status?>(dbStatus));
```

---

### Redirect Git Command Stderr in MSBuild Targets (2026-03-19)

**Author:** Boromir (DevOps)  
**Status:** Implemented

The `GetGitBuildInfo` MSBuild target in `src/Web/Web.csproj` runs `git describe --tags --abbrev=0` to capture the latest git tag for build metadata. When no tags exist, git writes `fatal: No names found, cannot describe anything.` to stderr. With `ConsoleToMSBuild="true"`, MSBuild captures stderr into `ConsoleOutput`, causing the error message to leak into the `_RawGitTag` property. This prevents the fallback `v0.0.0` value from being set, and the footer displayed the raw error text instead of a version.

**Decision:** All git commands in MSBuild `Exec` tasks that use `ConsoleToMSBuild="true"` must redirect stderr to `/dev/null` to prevent error messages from polluting output properties.

**Implementation:**
- Changed: `git describe --tags --abbrev=0` → `git describe --tags --abbrev=0 2>/dev/null`
- Changed: `git rev-parse --short HEAD` → `git rev-parse --short HEAD 2>/dev/null`
- Created initial tag: `v0.1.0`

**Rationale:**
1. `IgnoreExitCode="true"` handles command failures but doesn't suppress stderr
2. Stderr contamination breaks fallback logic that depends on empty output
3. Redirecting stderr is the standard Unix pattern for suppressing error messages
4. This ensures `_RawGitTag` and `_RawGitCommit` are truly empty on failure, allowing fallbacks to work correctly

**Impact:**
- Footer now correctly displays `v0.1.0` instead of error messages
- Future repos without tags will show `v0.0.0` as designed
- BuildInfo.g.cs generates clean constants

**Files Changed:** `src/Web/Web.csproj`, Git tag created


---

### MongoDB Connection String Fallback (2025-03-21)

**Author:** Sam (Backend Developer)  
**Status:** Implemented

The Web project crashed at startup with `System.TimeoutException` because the EF Core MongoDB provider reads `MongoDB:ConnectionString` (hardcoded to `mongodb://localhost:27017` in appsettings.Development.json), while the actual Atlas connection string lives in `ConnectionStrings:mongodb` (user secrets / Aspire injection). These two config paths never intersect.

**Decision:** Added fallback logic in `AddMongoDbPersistence` that bridges the gap:

1. Before binding `MongoDbSettings`, check if `MongoDB:ConnectionString` is empty or equals `mongodb://localhost:27017`
2. If so, read `ConnectionStrings:mongodb` and overlay it into the MongoDB config section  
3. Changed `appsettings.Development.json` to use empty string instead of the localhost default

**Priority order:**
- Explicit `MongoDB:ConnectionString` → used as-is
- Empty/localhost → falls back to `ConnectionStrings:mongodb` (Aspire-injected or user secrets)

**Impact:**
- **Aspire AppHost:** Works — Aspire injects `ConnectionStrings:mongodb` as env var, fallback picks it up
- **Standalone + user secrets:** Works — user secret `ConnectionStrings:mongodb` is read as fallback
- **Explicit config:** Works — non-empty, non-localhost `MongoDB:ConnectionString` takes priority
- **Tests:** Unaffected — `Testing` environment skips `AddMongoDBClient` and tests use TestContainers

**Files Changed:** `src/Persistence.MongoDb/ServiceCollectionExtensions.cs`, `src/Web/appsettings.Development.json`

**Rationale:** When two config systems disagree (Aspire vs raw appsettings), bridge them at the DI registration layer using configuration overlay before binding Options.

1. **DTO-Model Separation:** Clear boundaries between persistence and API contracts
2. **Result<T> Pattern:** Explicit error handling without exceptions
3. **Testcontainers for Integration:** Realistic testing without cloud dependencies
4. **Aspire Orchestration:** Simplified local development with containerized dependencies
5. **OpenTelemetry Observability:** Production-ready monitoring from day one
6. **Auth0 Identity:** Enterprise-grade security without maintenance burden
7. **Category-Based Documentation:** Developer-centric organization of resources
8. **bUnit Test Optimization:** Explicit parallelism control; defer full suite optimization until failing tests fixed


---

# Decision: Pre-Commit Review — Package Bumps, CSS Migration, ThemeToggle

**Author:** Aragorn (Lead Developer)
**Date:** 2025-07-23
**Status:** APPROVED

## Summary

Reviewed all uncommitted working directory changes. The changeset includes NuGet package version bumps, Aspire SDK bump, CSS class migration (`gray-*` → `neutral-*`), ThemeToggle component extraction, dead CSS cleanup, and test updates.

## Verdict: APPROVE

All changes are architecturally sound, complete, and consistent. The `gray-*` → `neutral-*` migration has zero remaining references. The ThemeToggle extraction follows proper Blazor patterns (IDisposable, CascadingParameter, event lifecycle).

## Action Required Before Commit

Add these to `.gitignore`:
- `.agents/`
- `.claude/`
- `.junie/`
- `skills-lock.json`

These are IDE/agent configuration directories that must not be committed to source control.

## Decision: `docs/research/` Commitment

Team should decide whether `docs/research/` (contains `github-github-sdk.md`, `tailwindcss-com.md`) should be committed or gitignored. These are research notes, not production code.

## Patterns Reinforced

- Tailwind neutral-* is the project standard for neutral/gray colors
- ThemeProvider cascading parameter pattern is the approved theme mechanism
- AppHost manages its own Aspire package versions (`ManagePackageVersionsCentrally=false`)
- Bootstrap CSS has been fully deprecated — no references remain

