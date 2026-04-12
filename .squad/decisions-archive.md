# IssueTrackerApp Decisions

This file records team decisions that affect architecture, scope, and process.

---

## Decisions

### Process & Planning
# IssueTrackerApp Decisions

This file records team decisions that affect architecture, scope, and process.

---

## Decisions

### Process & Planning

#### /plan Command Directive (2026-03-29)

**By:** Matthew Paulosky (via Copilot)
**What:** When the `/plan` command is used, the plan process must always include creating a GitHub milestone and defining sprints to complete the planned work.
**Why:** User request — standardize planning output so every plan produces a trackable GitHub milestone + sprint structure, not just a plan.md file.

---

#### Plan Ceremony — Milestone + Sprint Standard Process (2026-03-29)

**Author:** Aragorn (Lead)
**Requested by:** Matthew Paulosky

**Decision:** All `/plan` sessions must produce GitHub milestones and sprints before work begins.

**Process:**
1. Plan mode produces plan.md (existing behavior)
2. After user approves the plan, Aragorn runs the Plan Ceremony
3. Plan Ceremony creates a GitHub milestone, groups todos into sprints (5-8 issues), creates GitHub issues, assigns sprint labels and routing labels
4. No issue is worked without milestone + sprint assignment

**Sprint sizing:** Default 5–8 issues per sprint, or by logical dependency grouping.
**Milestone naming:** "{Epic/Feature} — Sprint N" or as specified by user.
**Sprint labels:** `sprint-1`, `sprint-2`, etc. (auto-created if missing)

**Why:** Provides traceable, time-boxed structure for all planned work. GitHub milestones give burn-down visibility; sprint labels enable filtering by iteration.

---

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

### Test Quality & Semantics

#### Test Fixes #78, #79, #80 (2026-03-27)

**Author:** Pippin (E2E & Aspire Tester)
**PR:** #84
**Issues Closed:** #78, #79, #80

**Fixes:**

1. **#78 — TimeoutException semantics in WaitForWebReadyAsync**
   - **Problem:** Polling loop in `BasePlaywrightTests.cs` let `OperationCanceledException` escape on deadline expiry
   - **Fix:** Wrap loop body in `try/catch(OperationCanceledException)` to throw `TimeoutException`
   - **Rationale:** `OperationCanceledException` signals cooperative cancellation; `TimeoutException` signals deadline expiry—distinct concerns

2. **#79 — EnvVarTests.cs missing DisableDashboard configuration**
   - **Problem:** Only test missing `DisableDashboard = true` in `DistributedApplicationTestingBuilder.CreateAsync`
   - **Fix:** Added config pattern used by `AspireManager.cs`
   - **Rationale:** Prevents Aspire dashboard resource waste in CI environments

3. **#80 — Admin dashboard heading assertion too weak**
   - **Problem:** Used `Should().NotBeNullOrWhiteSpace()` (any non-empty string passes, not specific)
   - **Fix:** Replaced with `Should().Be("Admin Dashboard")` (exact match)
   - **Rationale:** Per charter rule—assertions must be specific, not permissive

**Consequence:** E2E test suite now has correct exception semantics, consistent env configuration, and specific assertions.

---

### Frontend: Authorization Error Handling

#### Add /Account/AccessDenied Blazor Page (2026-03-27)

**Author:** Legolas (Frontend Developer)
**PR:** #83
**Issue Closed:** #77

**Context:** Auth0 redirects users failing authorization to `/Account/AccessDenied` (ASP.NET Core `AccessDeniedPath` convention). App had no Blazor component at this route, causing 404 UX.

**Decision:** Create `src/Web/Components/Pages/Account/AccessDenied.razor`:
- Route: `@page "/Account/AccessDenied"`
- Layout: `@layout MainLayout` (consistent with non-auth pages like `NotFound.razor`)
- Auth: No `[Authorize]` attribute (user just denied; would create redirect loop)
- Styling: Tailwind `neutral-*` palette
- Copy: Friendly error message + link to home

**Alternatives Rejected:**
- Razor Page (`.cshtml`): Inconsistent with Blazor-first architecture
- Redirect + toast: Loses explicit "denied" signal; not accessible to bots/screenreaders

**Consequences:**
- Users denied access now see branded error page instead of 404
- `src/Web/Components/Pages/Account/` directory ready for future pages (login callbacks, etc.)
- Zero middleware changes—purely UI addition

# PR #76 Fix — Gimli Review Blockers Resolved

**Author:** Aragorn (Lead Developer)  
**Date:** 2026-07-23  
**Branch:** `squad/apphost-tests-clean`  
**PR:** #76 `feat(tests): AppHost.Tests — Aspire integration + Playwright E2E tests`

---

## What Was Fixed

### 1 — False "skip gracefully" documentation (3 files)

**Files:** `AdminPageTests.cs`, `LayoutAdminTests.cs`, `LayoutAuthenticatedTests.cs`

The file-top comments and class-level XML summaries in all three files claimed that tests
"skip gracefully when `PLAYWRIGHT_TEST_ADMIN_EMAIL` / `PLAYWRIGHT_TEST_PASSWORD` are not set."
This was incorrect — the tests use `/test/login?role=admin|user` cookie-based authentication
and always run regardless of environment variables. Removed the false comments and rewrote
the docstrings to accurately describe the cookie-auth testing mechanism.

**Why it matters:** Misleading docs cause future developers to misunderstand test dependencies
and may give false confidence that tests are skippable in CI environments.

---

### 2 — `InteractWithPageAsync` visibility changed to `protected`

**File:** `BasePlaywrightTests.cs`

The method was `public`, which exposed a base-class helper as part of the public API of all
derived test classes. Changed to `protected` to match the access level of all sibling methods
(`InteractWithAuthenticatedPageAsync`, `InteractWithAdminPageAsync`, `InteractWithRolePageAsync`).

---

### 3 — `IBrowserContext` leak fixed

**File:** `BasePlaywrightTests.cs`

The original implementation stored browser contexts in a single `private IBrowserContext? _context`
field. Every call to `CreatePageAsync` overwrote the field, leaking all previous contexts (only the
final one was disposed). Fixed by replacing the single field with `private readonly List<IBrowserContext> _contexts`
and iterating over all contexts in `DisposeAsync`.

**Decision:** All `IBrowserContext` instances created during a test class's lifetime must be tracked
and disposed in `DisposeAsync`. Use a `List<T>` for tracking when multiple contexts may be created.

---

### 4 — Redirect assertion made specific

**File:** `AdminPageTests.cs` — `AdminPage_RedirectsNonAdminUser`

The assertion `page.Url.Should().NotContain("/admin")` was fragile — it only verified what the URL
was NOT, not what it actually IS. Replaced with `page.Url.Should().Contain("/Account/AccessDenied")`.

**Rationale:** ASP.NET Core's `CookieAuthenticationOptions.AccessDeniedPath` defaults to
`/Account/AccessDenied` when not explicitly configured. The Testing-environment cookie auth in
`Program.cs` sets only `LoginPath = "/test/login"` and leaves `AccessDeniedPath` at its default.
When a non-admin user hits an `[Authorize(Policy = AdminPolicy)]` page, cookie auth issues a
302 redirect to `/Account/AccessDenied?ReturnUrl=%2Fadmin`.

---

### 5 — Missing EOF newline in `EnvVarTests.cs`

The file was missing a trailing newline character. Added one. This is a POSIX convention and
prevents diff noise in git when editors append content.

---

### 6 — `DisableDashboard = true` in `AspireManager`

**File:** `AspireManager.cs`

The Aspire dashboard was mistakenly set to `DisableDashboard = false` in tests. The dashboard
is unnecessary during E2E tests — it consumes resources and may compete for ports. Changed to
`DisableDashboard = true`.

---

## Verification

Build result: `dotnet build tests/AppHost.Tests/AppHost.Tests.csproj --no-restore` — **0 errors, 0 warnings**
# Decision: AppHost.Tests Aspire E2E Test Architecture

**Date:** 2026-07-23  
**Author:** Aragorn (Lead Developer)  
**PR:** #76 — feat(tests): AppHost.Tests — Aspire integration + Playwright E2E tests

---

## Decision

Approve the Aspire + Playwright E2E testing architecture introduced in PR #76 as the team standard for integration/E2E tests that require a live Aspire host.

## Rationale

### Testing-environment seam in `Program.cs`
Using `IsEnvironment("Testing")` guards to swap:
- Auth0 OIDC → Cookie authentication
- MongoDB repositories → in-memory `FakeRepository<T>`
- Background services (email queue, bulk worker) → disabled

This is the correct pattern for Aspire E2E testing where the web app runs as a real subprocess. The seam is cleanly bounded in `Program.cs` and does not leak into domain or persistence layers.

### xUnit Collection fixture pattern
`[Collection]` placed on the abstract `BasePlaywrightTests` class (inherited by all derived test classes) is the canonical way to share a single `AspireManager` (and therefore a single AppHost instance) across an entire test suite. This prevents port-binding conflicts and keeps the test run fast.

### Fake repositories in `src/Web/Testing/`
`FakeRepository<T>` and `FakeSeedData` are compiled into the production assembly but are only reachable in `Testing` environment mode. This is an accepted pattern when the application under test must run as a real process. Both classes should carry `[ExcludeFromCodeCoverage]` to prevent coverage metric distortion.

### Port pinning
Fixing the Aspire web endpoint to HTTPS port 7043 with `IsProxied = false` is required for stable Playwright navigation and is safe for test-only environments.

## Follow-up items (non-blocking)
1. Add `[ExcludeFromCodeCoverage]` to `FakeRepository.cs` and `FakeSeedData.cs`
2. Add a TODO comment beside `#pragma warning disable CS0618` in `EnvVarTests.cs`
3. Remove duplicate home-page tests from `WebPlaywrightTests.cs` (superseded by `HomePageTests.cs`)
### 2026-03-27: PR Merge Protocol — Team Review Gate

**By:** Matthew Paulosky (via Copilot)
**What:** All PRs must follow this sequence before merge:
1. All CI checks pass
2. Team review: Aragorn (always) + domain specialists (Boromir=DevOps/CI, Gandalf=security, Gimli/Pippin=tests, Sam=backend, Legolas=frontend)
3. Rejected → different agent fixes (lockout enforced) → push → CI re-passes → re-review
4. Approved + CI green → `gh pr merge {N} --squash --delete-branch`
5. `git checkout main && git pull origin main`
6. Delete any orphan local branches
**Why:** User directive — captures the process demonstrated in PR #76 and #81 reviews
---
date: 2026-03-27
author: Bilbo
title: Blog Setup and First Post Format
---

## Decision: Blog Structure and Jekyll Configuration

### Context
Set up the project blog for GitHub Pages to document features, architecture decisions, and notable PRs.

### Decisions Made

1. **Jekyll Theme**: Selected `minima` (GitHub Pages default)
   - Minimal, clean, developer-focused
   - Zero configuration needed beyond `_config.yml`
   - Built-in support for YAML front matter

2. **Blog Location**: `docs/blog/` directory
   - Follows GitHub Pages convention (`docs/` is served directly)
   - Clear separation from root documentation (`docs/ARCHITECTURE.md`, etc.)

3. **Post Format**:
   - File naming: `YYYY-MM-DD-kebab-slug.md` (ISO date prefix for sorting and archives)
   - YAML front matter: title, date, author, tags, summary
   - Structure: Summary → Context → Key Details → What's Next
   - Code snippets use GFM fenced blocks with language identifiers

4. **Blog Index**: `docs/blog/index.md`
   - Acts as landing page and table of contents
   - Lists recent posts in reverse chronological order
   - Jekyll `layout: page` for consistent styling

5. **GitHub Pages URL**:
   - Base: `https://mpaulosky.github.io/IssueTrackerApp`
   - Blog: `https://mpaulosky.github.io/IssueTrackerApp/blog/`
   - Configured in `_config.yml` with `baseurl: "/IssueTrackerApp"`

### First Post Content
Topic: PR #76 (AppHost.Tests — Aspire integration + Playwright E2E tests)
- Documented the new `AppHost.Tests` project: 3 Aspire integration tests, 29 Playwright E2E tests
- Explained key architecture decisions: cookie auth for tests, `EnvironmentCallbackAnnotation`, `WaitForWebReadyAsync`, fixed port 7043
- Outlined test categories: Layout, Home, Dashboard, NotFound, Issues, Theme, Color scheme
- Noted follow-up work (3 nits from Aragorn's review)

### Dependencies
- Boromir (DevOps) to configure GitHub Pages Actions workflow (not yet done)
- Blog will be published once workflow is set up

### Status
✅ Complete — ready for GitHub Pages deployment when workflow is configured
### 2026-03-27T21:34:31Z: User directive
**By:** Matthew Paulosky (via Copilot)
**What:** Blog uses plain Markdown only — no Jekyll, no _config.yml. Files live in `docs/`. Matthew will configure GitHub Pages to point to the folder himself.
**Why:** User request — captured for team memory
---
title: "Auth0 Role Claim Fallback Implementation"
agent: gandalf
date: 2026-03-20
status: implemented
---

## Decision: Add Fallback Role Reading for Standard "roles" JWT Claim

### Context
The `Auth0ClaimsTransformation` service was skipping role mapping entirely when `Auth0:RoleClaimNamespace` was empty (the default configuration). This caused:
- Users with Admin/User roles in Auth0 to be denied access to protected pages
- `RequireRole("Admin")` and `AuthorizeView Policy="AdminPolicy"` to fail silently
- A less flexible setup that required namespace configuration in all scenarios

### Problem
Auth0 supports multiple role claim patterns:
1. **Custom namespaced claims** (per tenant configuration): `"https://issuetracker.com/roles"`
2. **Standard OpenID Connect claims** (OIDC spec): `"roles"`
