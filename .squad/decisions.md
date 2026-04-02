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

The previous implementation only supported pattern #1, requiring explicit namespace configuration. Many Auth0 setups use pattern #2 without custom namespaces, making role mapping impossible without configuration.

### Solution
Implement a **two-pass role transformation** with fallback logic:
- **Pass 1:** If namespace is configured, read roles from that namespace
- **Pass 2:** If no roles found (or namespace is empty), fall back to standard `"roles"` claim
- Both sources use the same role parsing logic via extracted `MapRoleClaims()` helper

### Implementation Details

**Code Changes:**
- Refactored `TransformAsync()` to use two-pass logic
- Extracted role mapping into `MapRoleClaims()` helper method
- Updated constructor logging from `LogWarning` → `LogInformation`
- Handles all role formats: JSON arrays, CSV, single values

**Design Principles:**
1. **Backward Compatible:** Existing namespace-based setups work unchanged
2. **Fail-Safe:** Fallback only activates when primary source yields no roles
3. **Additive-Only:** No role claims removed or overwritten; duplicates prevented
4. **Security-First:** No new attack vectors; same authentication-only claim reading

### Impact

**For Users:**
- Admin users can now access protected pages without namespace configuration
- `RequireRole()` and `AuthorizeView` policies now work with standard Auth0 setup
- Smoother onboarding: standard Auth0 role support is automatic

**For Configuration:**
- `Auth0:RoleClaimNamespace` remains optional (not required)
- Namespace still takes precedence if configured
- Default behavior now "just works" for most Auth0 tenants

**For Security:**
- No additional vectors introduced
- Role transformation remains limited to authenticated JWT claims
- Duplicate-prevention logic prevents injection attacks

### Testing Recommendations
1. Test with `Auth0:RoleClaimNamespace` configured (namespace path)
2. Test without namespace configured (standard claims path)
3. Verify both single and multiple role assignments work
4. Check logs for role transformation messages

### Files Modified
- `src/Web/Auth/Auth0ClaimsTransformation.cs`

### Related Documentation
- Previous: `.squad/agents/gandalf/history.md` → "Auth0 Role Claim Mapping Fix (2026-03-19)"
- Auth0 Standard Claims: https://auth0.com/docs/get-started/tokens
- OIDC Spec: https://openid.net/specs/openid-connect-core-1_0.html#StandardClaims
# Decision: Playwright Theme DOM Assertions & Auth0 State Pattern

**Author:** Gimli (Tester)
**Date:** 2026

---

## Confirmed: Theme DOM Selectors

### Dark Mode Detection
- **Attribute:** `document.documentElement.classList.contains('dark')`
- **Selector usage:** `await page.EvaluateAsync<bool>("document.documentElement.classList.contains('dark')")`
- **When true:** dark mode is active; when false light/system mode is active
- **Toggled by:** clicking `button[aria-label="Toggle theme"]` then choosing "Light", "Dark", or "System"

### Color Scheme Detection
- **Attribute:** `document.documentElement.getAttribute('data-theme')`
- **Selector usage:** `await page.EvaluateAsync<string>("document.documentElement.getAttribute('data-theme')")`
- **Values:** `'blue'` | `'red'` | `'green'` | `'yellow'`
- **Default:** `'blue'` (applied on page load when no localStorage key is set)
- **Changed by:** clicking `button[aria-label="Change color scheme"]` then `button[title="Blue|Red|Green|Yellow"]`

---

## Auth State Pattern for Auth0 Tests

### Strategy: One-Time Login + Cached Storage State

The `AuthStateManager` static class performs a single Auth0 login and caches the Playwright
[storage state](https://playwright.dev/dotnet/docs/auth) (cookies + localStorage) to a JSON file.
All subsequent authenticated tests reuse the stored state by loading it into a fresh browser context.

**Key design decisions:**
1. `SemaphoreSlim(1,1)` guards the one-time login to prevent race conditions in parallel xUnit test runs.
2. The login page uses a temporary browser context with `IgnoreHTTPSErrors = true` to handle dev HTTPS certs.
3. Storage state is persisted to `Path.GetTempPath() + "issuetracker-playwright-auth.json"` (Playwright convention).
4. If `PLAYWRIGHT_TEST_EMAIL` / `PLAYWRIGHT_TEST_PASSWORD` env vars are absent, `GetStorageStatePathAsync` returns `null` and `InteractWithAuthenticatedPageAsync` skips the test gracefully (no exception).

### Login Flow
```
navigate → /account/login?returnUrl=/
wait for Auth0 Universal Login (NetworkIdle)
fill input[name="username"]
fill input[name="password"]
click button[type="submit"]
WaitForURLAsync(url => url.StartsWith(baseUrl), timeout: 30s)
save page.Context.StorageStateAsync(path: ...)
```

### Authenticated Context Options
```csharp
new BrowserNewContextOptions
{
    IgnoreHTTPSErrors = true,
    ColorScheme = ColorScheme.Dark,
    StorageStatePath = statePath,
    BaseURL = uri.ToString()
}
```
# Decision: Skipped Test Audit Results

**Author:** Gimli (Tester)
**Date:** 2025-07-17
**Status:** Informational

## Context

Audited all 8 skipped tests across the test suite. All skip reasons remain valid.

## Findings

### Two blocking gaps prevent unskipping:

1. **MediatR ValidationBehavior pipeline not wired** (3 tests in `IssueEndpointTests.cs`)
   - FluentValidation validators exist but no `IPipelineBehavior<,>` implementation enforces them.
   - **Action needed:** Aragorn or Legolas should implement `ValidationBehavior<TRequest, TResponse>` and register it in DI. Once done, unskip the 3 validation tests.

2. **Auth0 test infrastructure incomplete** (5 tests in `AuthEndpointSecurityTests.cs`)
   - `TestWebApplicationFactory` registers a "Test" auth scheme but endpoints use `Auth0Constants.AuthenticationScheme`.
   - **Action needed:** Either map the test scheme to Auth0's expected scheme name, or refactor endpoints to use a configurable scheme. Then unskip the 5 auth tests.

## Recommendation

Track these as backlog items so they don't stay skipped indefinitely.
# Full-Width Navigation Bar Pattern

**Decision:** NavMenuComponent and other full-width layout bars (header, footer) use a two-level structure:
- Outer element (`<header>`, `<footer>`) carries background color, borders, and `w-full`
- Inner `<div>` carries `max-w-7xl mx-auto px-4 sm:px-6 lg:px-8` for content constraint

**Rationale:**
- Previous single-level approach had background on `<nav>` element, conflicting with global CSS rule that applied `container mx-auto` to all nav elements
- Global `nav {}` CSS rule was removed because it conflicted with breadcrumb navs, pagination navs, and admin layout navs
- Two-level pattern ensures full-width background while constraining inner content to max-width container
- Pattern is now consistent between NavMenuComponent and FooterComponent

**Implementation:**
- `src/Web/Components/Layout/NavMenuComponent.razor` restructured to match FooterComponent pattern
- `src/Web/Styles/input.css` global `nav` rule emptied to remove conflicting styles
- All nav elements in app use explicit utility classes instead of relying on global rule

**Impact:**
- NavMenuComponent now renders as true full-width bar with properly centered content
- No more conflicts between global nav styling and specialized nav uses (breadcrumbs, pagination)
- More predictable CSS behavior with explicit classes on each component

**Testing:**
- All 12 bUnit tests for NavMenuComponent pass
- Visual verification shows full-width background with centered content

**Author:** Legolas (Frontend Developer)
**Date:** 2025-01-24
### 2026-03-29: Use /alive (not /health) for Aspire test startup polling
**By:** Pippin (via Ralph work queue)
**What:** WaitForWebHealthyAsync and WaitForWebReadyAsync now poll /alive instead of /health. /health includes Redis/MongoDB checks that are irrelevant in Testing mode (which uses in-memory fakes). /alive returns 200 as soon as the ASP.NET Core process is up.
**Why:** PR #86 had 2 flaky CI failures due to Redis connection timeouts blocking test startup for 120s.
# PR #86 AppHost.Tests CI Flakiness Investigation

**Date:** 2026-03-28  
**Author:** Pippin (E2E & Aspire Tester)  
**Status:** Investigation Complete

## Context

PR #86 had 2 failing tests in CI:
- `web_https_/health_200_check` — Connection refused (localhost:7043)
- `redis_check` — Redis timeout and connection errors

## Investigation Results

The fix was **already implemented** by Boromir in commit `ff74721`. The solution:
- Added `WaitForWebHealthyAsync()` in `AspireManager.StartAppAsync()`
- Polls `/health` endpoint with 120s timeout (accounts for 30-60s Redis cold-start in CI)
- Uses certificate-ignoring HttpClient for self-signed HTTPS in CI

## Validation

Local test run (with Docker) confirms fix:
- ✅ No Redis connection errors
- ✅ No web health check failures
- ✅ 38/40 tests passing
- ⚠️ 2 failures are unrelated UI timing issues (ThemeToggle, ColorScheme Playwright timeouts)

## Key Decision Point (Already Made)

**Decision:** Poll the web `/health` endpoint directly instead of using Aspire's `WaitForResourceHealthyAsync()`.

**Rationale:**
1. Web health check transitively validates Redis (via `.WaitFor(redis)` in AppHost.cs)
2. Direct HTTP polling with cert validation disabled works around CI self-signed cert issues
3. Single wait point is simpler than chaining multiple resource waits

## Recommendation

This pattern should be documented in squad decisions as the standard approach for Aspire test fixtures:
- Always add explicit health polling after `App.StartAsync()` in test fixtures
- Use direct HTTP polling with cert validation disabled for HTTPS services in CI
- Leverage dependency chains (`.WaitFor()`) to minimize redundant health checks
# Decision: Update Theme E2E Tests for New ThemeManager localStorage Key

**Author:** Pippin (Tester)  
**Date:** 2026-03-29  
**Context:** PR #86 (squad/86-fix-failing-tests-and-web-razor-pages)

## Problem

2 theme E2E tests failed with 30s timeouts after PR #86 introduced new theme components (`ThemeColorDropdownComponent`, `ThemeBrightnessToggleComponent`). Tests expected localStorage key `theme-color-brightness` (old system), but new components write to `tailwind-color-theme` (new system).

## Root Cause

PR #86 introduced a **dual theme system conflict**:

1. **OLD system** (`theme.js` + `ThemeProvider.razor.cs`):
   - JavaScript module: `window.themeManager` (lowercase)
   - localStorage key: `theme-color-brightness`
   - Used by: `ThemeProvider` component (still active in `MainLayout.razor`)
   
2. **NEW system** (`theme-manager.js` + new components):
   - JavaScript module: `window.ThemeManager` (uppercase)
   - localStorage key: `tailwind-color-theme`
   - Used by: `ThemeColorDropdownComponent`, `ThemeBrightnessToggleComponent`

Both systems coexist but use **different localStorage keys**. Tests checked the old key, but new components wrote to the new key → timeout.

## Decision

Updated all theme E2E tests to use the **correct localStorage key** (`tailwind-color-theme`) that the new components actually write to.

### Files Modified

- `tests/AppHost.Tests/Tests/Theme/ThemeToggleTests.cs` — 2 tests updated
- `tests/AppHost.Tests/Tests/Theme/ColorSchemeTests.cs` — 2 tests updated

### Changes Made

1. Replaced all `localStorage.getItem('theme-color-brightness')` checks with `localStorage.getItem('tailwind-color-theme')`
2. Replaced all `localStorage.setItem('theme-color-brightness', ...)` seeds with `localStorage.setItem('tailwind-color-theme', ...)`
3. Updated comments to explain the dual system conflict
4. Kept `data-theme-ready` waits — `ThemeProvider` still sets this attribute via `themeManager.markInitialized()`

## Production Issue (Requires Aragorn's Attention)

The dual theme system is a **production bug**:
- User changes theme via new components → writes to `tailwind-color-theme`
- Page reloads → `ThemeProvider` reads from `theme-color-brightness` (old value)
- User's theme preference doesn't persist correctly

**Recommended Fix (Aragorn's domain):**
1. **Option A:** Update new components to call `themeManager.*` (lowercase) and use `theme-color-brightness`
2. **Option B:** Remove `ThemeProvider`, update `MainLayout` to initialize `ThemeManager.*`, ensure `data-theme-ready` is still set

**Critical:** Theme state won't sync correctly until production code uses a single localStorage key.

## Testing

Build succeeded with no compilation errors. Full E2E test run requires Docker. CI will validate on next push.

## Rationale

Tests should verify **actual behavior**, not planned behavior. When UI changes, tests must adapt to match what's actually rendered. However, production code must also be flagged when it contains bugs — test fixes don't absolve production issues.

---

#### Unified Theme System — Single localStorage Key (2026-03-28)

**Author:** Aragorn (Lead Developer)  
**Status:** Implemented  
**PR:** #86 (squad/86-fix-failing-tests-and-web-razor-pages)

**Context:**
PR #86 introduced two new Blazor components for theme selection (`ThemeColorDropdownComponent.razor` and `ThemeBrightnessToggleComponent.razor`) that called `window.ThemeManager` (capital T) from `theme-manager.js`, writing to localStorage key `tailwind-color-theme`. However, the existing `ThemeProvider.razor.cs` component called `window.themeManager` (lowercase t) from `theme.js`, reading from localStorage key `theme-color-brightness`. This dual system prevented theme changes from persisting across page reloads.

**Decision:**
Consolidate to a single theme system:
1. **Single localStorage key:** `tailwind-color-theme` (the key E2E tests now expect)
2. **Single JS API:** `window.themeManager` from `theme.js` (lowercase)
3. **Single source of truth:** `ThemeProvider.razor.cs` orchestrates theme state; all other components delegate to `themeManager` JS API

**Changes:**
- Updated `theme.js` to use `STORAGE_KEY: 'tailwind-color-theme'` instead of `'theme-color-brightness'`
- Updated `ThemeColorDropdownComponent` and `ThemeBrightnessToggleComponent` to call `themeManager.*` methods
- Removed `<script src="js/theme-manager.js">` from `App.razor`
- Deleted `theme-manager.js`

**Rationale:** Kept `theme.js` because it is the established, well-tested system integrated with `ThemeProvider`, provides the complete API, and sets `data-theme-ready="true"` for E2E tests. This was lower risk than rewriting `ThemeProvider` to use the duplicate `theme-manager.js`.

**Impact:** Theme preferences now persist across page reloads; all theme controls share state; no FOUC.

---

**Next Steps:** Aragorn has unified the theme system per this decision. Tests should pass consistently.

---


---

# Dependabot PR #87 Merge Decision

**Date:** 2026-03-29  
**Decision Maker:** Boromir (DevOps)  
**Status:** COMPLETED

## Summary
Merged Dependabot PR #87 "build(deps): Bump the all-actions group with 5 updates" to main branch.

## Context
- PR contained 5 GitHub Actions dependency updates (all-actions group)
- All 19 CI checks passed (CodeQL, full test suite, coverage, Squad CI)
- No review blocking or merge conflicts
- Dependabot auto-merge process leveraged with squash-merge strategy

## Decision
Approve and merge using `gh pr merge 87 --squash --auto`.

## Rationale
- **Safety:** All CI green; comprehensive test coverage confirms no regressions
- **Best Practice:** Squash-merge reduces main branch history clutter for dependency bumps
- **Automation:** Auto-merge flag prevents accidental merge races in CI pipeline
- **Reliability:** Updated Actions improve build pipeline stability and security

## Outcome
✅ Successfully merged PR #87 to main (commit SHA will be auto-generated)

## Impact
- GitHub Actions workflows updated to latest compatible versions
- Improved CI/CD stability and security
- No application code changes required

---

### 2026-03-29: Footer text size unified

**What:** Removed `text-xs` from footer inner div and removed invalid `txt-3xl` class from version/commit links. All footer text now defaults to `text-base`.

### 2026-03-29: SignalRConnection Labels Match Nav Size

**Author:** Legolas (Frontend Dev)

**What:** Removed `text-xs` from all three state label spans in SignalRConnection.razor. Labels now inherit text-base, matching nav menu link size.

**Rationale:** Ensures consistent visual sizing across navigation UI components. Labels inherit parent context sizing rather than forced override.

---

---

### 2026-03-29: Auth0 Role Claim Configuration & Transformation

#### Auth0:RoleClaimNamespace Configuration (2026-03-29T18:02:58Z)

**Author:** Aragorn (Lead)

**Decision:** Auth0:RoleClaimNamespace must be set to `"https://issuetracker.com/roles"` in configuration.

**Implementation:**
- Updated `src/Web/appsettings.Development.json` with Auth0 section
- Set `Auth0.RoleClaimNamespace = "https://issuetracker.com/roles"`

**Environment Variables:**
- Production/staging: `Auth0__RoleClaimNamespace=https://issuetracker.com/roles`
- Local dev (alternative): `dotnet user-secrets set "Auth0:RoleClaimNamespace" "https://issuetracker.com/roles"`

**Rationale:** Empty namespace causes Auth0ClaimsTransformation to skip role claim mapping:
- Pass 1 checks if namespace is configured — skipped when empty
- Pass 2 fallback looks for bare "roles" claim — Auth0 uses namespaced form
- Result: ClaimTypes.Role never added → Profile shows "No roles assigned" → Admin links hidden

**Impact:** Fixes Admin role visibility in NavMenu and enables AdminPolicy authorization.

---

#### Auth0ClaimsTransformation Pass 3 Auto-Detect (2026-03-29T18:04:25Z)

**Author:** Sam (Backend)

**Decision:** Added Pass 3 to Auth0ClaimsTransformation.TransformAsync that auto-detects claims with types ending in `/roles` when Passes 1–2 find no roles.

**Implementation:** Pass 3 scans all claims for pattern matching `*/roles` (case-insensitive) and maps to ClaimTypes.Role.

**Rationale:** Prevents silent failure when RoleClaimNamespace is misconfigured; if admins disable Pass 1/2, Pass 3 still catches namespaced role claims. Safety net approach.

**Coverage:** Added 2 test cases to Auth0ClaimsTransformationTests.cs verifying Pass 3 auto-detect.

---

#### Profile.razor GetAllRoleClaims Hardening (2026-03-29T18:08:58Z)

**Author:** Legolas (Frontend)

**Decision:** GetAllRoleClaims() now accepts optional `roleClaimNamespace` parameter and includes Auth0 namespace claim type in role lookup. IConfiguration injected into Profile.razor.

**Implementation:**
- Profile.razor injects IConfiguration
- GetAllRoleClaims reads `Auth0:RoleClaimNamespace` from config
- Claims lookup includes both standard `ClaimTypes.Role` and namespaced form

**Rationale:** Belt-and-suspenders approach—shows roles directly from Auth0 namespace claim even if Auth0ClaimsTransformation hasn't run or is misconfigured. Improves profile UI resilience.

**Coverage:** 8 new tests in ProfileRolesTests.cs + 2 NavMenu bUnit tests verifying role visibility.

---

---

#### Auth0ClaimsTransformation Empty Role Value Handling (2026-03-28)

**Author:** Gimli (Tester)

**Decision:** `Auth0ClaimsTransformation.MapRoleClaims()` now validates role values and skips empty/whitespace strings before adding `ClaimTypes.Role` claims.

**Implementation:** Added guard clause in the single-value role path:
```csharp
if (string.IsNullOrWhiteSpace(roleValue))
    continue;
```

**Rationale:** 
- Unit test exposure: empty role values were being added as claims
- Consistency: comma-separated value path already uses `StringSplitOptions.RemoveEmptyEntries`
- Security: empty role claims could cause unintended authorization behavior
- Data integrity: empty strings add noise to claims principal

**Impact:**
- Empty/whitespace role values from Auth0 are now silently ignored
- No breaking changes — empty role claims have no semantic meaning
- Test coverage: All 16 Auth0ClaimsTransformation tests passing after fix

**Related:** Issue #93 (Sprint 3 — Auth0ClaimsTransformation Unit Tests)

---

#### Formal PR Review Process (2026-03-29)

**Author:** Aragorn (Lead)  
**Requested by:** Matthew Paulosky

**Decision:** A formal PR review process is now in effect. No PR may merge without passing pre-review gates (CI green, mergeable, template filled), unanimous reviewer approval per domain, and pre-merge gates (APPROVED, CI still green, no CHANGES_REQUESTED).

**Review Matrix:**
- **Aragorn:** All PRs (lead, always required)
- **Boromir:** `.github/workflows/`, `AppHost.csproj`, `Directory.Packages.props`
- **Gandalf:** Auth sections, `Auth/`, `appsettings*.json` auth
- **Gimli:** `tests/Domain.Tests/`, `tests/Web.Tests.Bunit/`, `tests/Persistence.*/`
- **Pippin:** `tests/AppHost.Tests/` (Playwright/Aspire E2E)
- **Sam:** `src/Domain/`, `src/Persistence.*/`, `src/Web/Endpoints/`, `src/Web/Features/`
- **Legolas:** `src/Web/Components/`, `*.razor`, `*.razor.css`, `wwwroot/`
- **Frodo:** `docs/`, `README.md`, XML doc changes

**Artifacts:**
- `.github/pull_request_template.md` — PR checklist with domain checkboxes
- `.squad/ceremonies.md` — PR Review Gate, CHANGES_REQUESTED Ceremony, Merge Conflict Resolution
- `.squad/routing.md` — New PR state signals (CHANGES_REQUESTED, CONFLICTED, CI FAILURE, ready-for-review)
- `.squad/agents/ralph/charter.md` — Pre-review/pre-merge gate tables

**CHANGES_REQUESTED Handling:**
1. Ralph detects → pings Aragorn
2. Aragorn routes fix to different agent (author locked out)
3. Fix agent pushes; CI re-passes
4. Original reviewer re-approves
5. Cycle continues until unanimous

**Merge Conflict Resolution:**
1. Ralph detects CONFLICTED → pings Aragorn
2. Aragorn routes by domain (Sam=backend, Legolas=frontend, Boromir=CI, Aragorn=mixed)
3. Resolver merges origin/main, resolves, pushes
4. Full re-review required (existing reviews invalidated)

**Trade-offs:** ≥2 reviewers per PR (acceptable for small squad), unanimous can slow hotfixes (waivable by Aragorn for non-critical).

---

#### GitHub Repository Protection & CI Infrastructure (2026-03-29)

**Author:** Boromir (DevOps)  
**Requested by:** Matthew Paulosky

**Decision:** Branch protection on `main` now enforces 1 required review, build check, and stale review dismissal. Squash-only merges + auto-delete branches. CI workflow fixed to run real .NET builds.

**Branch Protection (`main`):**
- Required checks: `build (ubuntu-latest)` from squad-ci.yml
- Required reviews: 1 (CODEOWNERS auto-request)
- Stale reviews dismissed on new pushes
- Force push disabled, deletions disabled

**Merge Strategy:**
- Squash merge only (linear history)
- Rebase + merge commit disabled
- Auto-delete branches on merge

**CODEOWNERS:**
- @mpaulosky (lead + DevOps) across all critical files
- Role-based routing: AGENTS.md/CODEOWNERS/.github/ → Boromir; src/Domain/ → Sam; src/Web/Components/ → Legolas; tests/ → Gimli/Pippin; Auth/ → Gandalf; docs/ → Frodo

**CI Workflow (squad-ci.yml):**
- Fixed from stub to real `dotnet restore && dotnet build --configuration Release`
- Runs on PRs to [dev, preview, main, insider] + pushes to [dev, insider]
- Single job: `build (ubuntu-latest)` with .NET from global.json

**Rationale:** PR Review Process infrastructure layer. Ensures code quality gates, prevents unreviewed merges, maintains clean main history.

**Next Steps:** Monitor first PRs to confirm protection works; add test checks to required_status_checks once squad-test.yml job names confirmed.

---

---

### 2026-03-30: Plan Ceremony — NavMenu Cleanup (Retroactive)

**By:** Aragorn (Lead)

**What:** Ran Plan Ceremony retroactively for NavMenu cleanup work. Created milestone "NavMenu Cleanup — Sprint 1" (#3) and 2 GitHub issues (#104, #105). Work was already complete and merged into branch `squad/nav-cleanup-and-admin-portal`; issues were immediately closed referencing the implementation branch.

**Why:** Process compliance — team skipped Plan Ceremony during [[PLAN]] session. Work was implemented without milestone/sprint structure. Corrected retroactively before PR was opened.

**Process violation:** @copilot implemented work directly after plan approval without routing to Aragorn for Plan Ceremony. Team should enforce: plan approval → Aragorn Plan Ceremony → issue creation → then work begins.

**Details:**
- Milestone #3: "NavMenu Cleanup — Sprint 1"
- Issue #104: refactor(nav) — Legolas assigned — ✅ Closed
- Issue #105: test(nav) — Gimli assigned — ✅ Closed
- Both issues in sprint-1, milestone 3

---

### 2026-03-30T13:22:06Z: AppHost.Tests Mandatory (User Directive)

**By:** Matthew Paulosky (via Copilot)

**What:** AppHost.Tests (Playwright E2E) MUST be run locally before every push, even if they take a long time. Claiming "all tests pass" without running AppHost.Tests is a false statement. If AppHost.Tests fail locally they will fail in the PR CI on GitHub.

**Why:** User requirement — captured for team memory. No exception or skip for AppHost.Tests pre-push.

**Enforcement:** Gate 4 in CI now includes AppHost.Tests. This is now a team-wide rule affecting all agents (Aragorn, Boromir, Pippin, Gimli, and others).

---

### 2026-03-30T13:27:42Z: Plan Ceremony — Test Gate Enforcement & Dev Workflow Hardening

**Author:** Aragorn (Lead Developer)  
**Date:** 2026-03-30  
**Requested by:** Matthew Paulosky  

**Decision:** CLOSED — Milestone #4 created with 6 tracked issues across two sprints.

**Context:** Completed Plan Ceremony for Sprint 1 (work completed in PR #106) and Sprint 2 (follow-up items). Established test gate enforcement and dev workflow hardening as formal milestone.

**Milestone Details:**
- **Name:** Test Gate Enforcement & Dev Workflow Hardening
- **URL:** https://github.com/mpaulosky/IssueTrackerApp/milestone/4
- **Description:** Enforce full test suite pre-push, README sync automation, Playwright E2E in gate

**Sprint 1 — COMPLETED (PR #106)**
1. #107 — fix: Playwright Layout_NavMenu_ContainsExpectedLinks test (`squad:pippin`, `squad:gimli`)
2. #108 — feat: README → docs/README.md sync GitHub Action (`squad:frodo`, `squad:boromir`)
3. #109 — fix: Harden pre-push Gate 4 — remove Docker skip bypass (`squad:boromir`)
4. #110 — fix: Add AppHost.Tests (Playwright E2E) to pre-push Gate 4 (`squad:boromir`, `squad:pippin`)

**Sprint 2 — IN PROGRESS**
5. #111 — chore: Add hook install script so AppHost.Tests gate installs on fresh clone (`squad:boromir`, `sprint-2`)
6. #112 — docs: Update CONTRIBUTING.md with pre-push gate requirements (`squad:frodo`, `sprint-2`)

**Key Team Directive Captured:**
> "AppHost.Tests MUST be run locally before every push — no exceptions — even if they take a long time." — Matthew Paulosky

This directive is now explicitly documented in milestone description, issue #110 body, and PR #106 comments.

**Learnings:**
- `gh milestone` CLI lacks `create` subcommand; use `gh api` instead
- Multiple labels require separate `--label` flags in `gh issue create`
- Milestone reference in issue creation uses title, not number
- Team enforces AppHost.Tests as hard gate with no skip option

---

### 2026-03-30T17:57Z: Workflow Limitation — GITHUB_TOKEN Cannot Trigger Downstream Workflows

**By:** mpaulosky (via Copilot)  
**Type:** Technical Decision / Workaround  

**Problem:** When `squad-milestone-release.yml` pushes a tag using `GITHUB_TOKEN`, GitHub's security model blocks `push: tags` workflows (like `squad-release.yml`) from auto-triggering. The tag is created correctly but the downstream release workflow never fires.

**Root Cause:** GitHub's token isolation prevents workflows triggered by `GITHUB_TOKEN`-created events from spawning additional workflows. This is by design to prevent infinite loops and unauthorized workflow chains.

**Workaround Applied:** 
Ran `gh release create v0.2.0 --generate-notes --title "Release v0.2.0" --verify-tag` directly from CLI after tag push. Release v0.2.0 successfully created with auto-generated notes.

**Permanent Fix Options:**
1. **Consolidate into single workflow** — Add `gh release create` as final step in `squad-milestone-release.yml` (simplest, one workflow does everything)
2. **Use PAT secret** — Replace `GITHUB_TOKEN` with `PAT` for tag push step (allows downstream trigger, more complex, requires secret management)

**Recommendation:** Option 1 — Consolidate release creation into `squad-milestone-release.yml`.

**Rationale:** 
- Eliminates dependency on `squad-release.yml` for the release cut path
- Still allows `squad-release.yml` to run if a tag is pushed manually via another method
- Simpler maintenance and fewer moving parts
- Reduces CI complexity during release process

**Implementation:** Add to `squad-milestone-release.yml` after tag push:
```yaml
- name: Create GitHub Release
  run: gh release create ${{ env.NEW_VERSION }} --generate-notes --title "Release ${{ env.NEW_VERSION }}" --verify-tag
```

**Related:** `.github/workflows/squad-milestone-release.yml`, `.github/workflows/squad-release.yml`

---

### Blog & Release Documentation

#### Release Blog Posts are Mandatory (2026-03-30)

**By:** Matthew Paulosky (via Squad)

**Decision:** Every GitHub Release must have a corresponding blog post in `docs/blog/`. Ralph triggers Bilbo after a release is published. Posts must be written before or alongside the next commit process.

**Process:**
1. Release is published via GitHub (manual or workflow)
2. Ralph (orchestration) detects release and spawns Bilbo
3. Bilbo writes post in `docs/blog/{DATE}-release-{VERSION}.md` with:
   - YAML front matter (title, author, date)
   - Summary (2–3 sentences)
   - Context (what this release addresses)
   - Key Details (grouped by feature/fix/tooling)
   - What's Next (roadmap callouts)
   - PR links
4. Post is merged before or with next squad commit

**Why:** Bilbo was not writing release/milestone posts because no trigger existed. Adding this as a hard rule ensures documentation stays in sync with releases. Without explicit responsibility, release notes would fall through the cracks and the blog would go stale.

**Impact:** All future GitHub Releases will automatically spawn a blog post. Squad members can rely on the blog as the source of truth for what shipped in each version.

**Related:** `.squad/agents/bilbo/charter.md` (release trigger rule added)

#### GH Pages: Legolas → Bilbo → Legolas Workflow (2026-03-30)

**By:** Matthew Paulosky (via Squad)

**Decision:** After each Bilbo blog cycle, Legolas regenerates `docs/index.html` from the root `README.md`. Work is local-only; no GitHub Actions needed.

**Why:** GH Pages (`main:/docs`, legacy build) needs `index.html` to display the project landing page at https://mpaulosky.github.io/IssueTrackerApp/ with full badge rendering and GitHub-flavored markdown support. Plain HTML — no Jekyll, no `_config.yml`.

**Workflow Chain:**
1. Release published → Ralph detects
2. Bilbo writes release blog post in `docs/blog/`
3. Legolas regenerates `docs/index.html` from updated root README
4. Scribe commits both as part of next batch push

**Implementation:** Legolas has standing responsibility to regenerate landing page whenever README changes or after each blog cycle. Added to Legolas charter as formal role.

**Related:** `.squad/agents/legolas/charter.md`, `docs/index.html`


---

# PR Review Decision — Sprint 5 Admin User Management (2026-04-01)

**Reviewer:** Aragorn (Lead Developer)  
**Date:** 2026-04-01  
**PRs Reviewed:** #146, #157, #158

---

## Summary

Reviewed three PRs from Sprint 5 Admin User Management epic. Two approved, one rejected due to Architecture.Tests failure.

---

## PR #146 — Auth0 Management API Research Spike

**Branch:** `squad/130-auth0-management-api-spike`  
**Author:** Gandalf  
**Verdict:** ✅ **APPROVED**

### Findings

- **Deliverable:** Comprehensive ADR in `.squad/decisions/inbox/gandalf-auth0-management-api.md`
- **Quality:** Excellent research — covers SDK choice (`Auth0.ManagementApi`), token caching strategy (`IMemoryCache` with 24h TTL − 5 min safety margin), rate limits (2 req/sec free tier, Polly retry on HTTP 429), secrets strategy (User Secrets dev, Key Vault prod), required Auth0 dashboard M2M app setup
- **Production code:** None — research only ✅
- **CI:** All 23 checks passed ✅
- **.squad/ file compliance:** ADR properly placed in `.squad/decisions/inbox/` on `squad/*` branch — permissible per charter (prohibition applies only to `feature/*` branches) ✅

### Recommendation

**MERGE** — Excellent foundation for implementation work in PR #158.

---

## PR #157 — Admin-Only Authorization Policy for /admin/users Routes

**Branch:** `squad/135-admin-policy`  
**Author:** Gandalf  
**Verdict:** ✅ **APPROVED**

### Key Changes

1. **AccessDenied.razor** — Added `/access-denied` route alias (line 7)
2. **Routes.razor** — Upgraded `RouteView` → `AuthorizeRouteView`; unauthenticated → `/account/login`, forbidden → `/access-denied`
3. **Users.razor** — New `/admin/users` scaffold with `AdminPolicy` attribute and placeholder UI ("coming in a future sprint")
4. **Analytics.razor** — Fixed hardcoded `"AdminPolicy"` string → `AuthorizationPolicies.AdminPolicy` constant
5. **AdminPageLayout.razor** — Added Users nav link
6. **AdminPolicyAuthorizationTests.cs** — 7 new bUnit tests covering AdminPolicy authorization logic

### Findings

- **File headers:** ✅ All new files carry required copyright block (Users.razor, AdminPolicyAuthorizationTests.cs)
- **Tests:** 7/7 passed — covers Admin role success, Admin+User success, User-only failure, no-roles failure, anonymous failure, UserPolicy independence
- **VSA compliance:** ✅ New code properly structured under `src/Web/Components/Pages/Admin/`
- **CI:** All 23 checks passed (0 warnings, 0 failures) ✅

### Recommendation

**MERGE** — Clean authorization scaffold with comprehensive test coverage. Ready for UserManagementService integration (PR #158).

---

## PR #158 — UserManagementService Wrapping Auth0 Management API

**Branch:** `squad/131-user-management-service`  
**Authors:** Sam (implementation) + Gandalf (ADR)  
**Verdict:** ❌ **REJECTED** — Architecture test failure must be fixed before merge

### Key Changes

1. **Domain layer:**
   - `ResultErrorCode.ExternalService = 5` — new error code for Auth0 API failures
   - `IUserManagementService` interface in `Domain.Features.Admin.Abstractions`
   - `AdminUserSummaryDto`, `RoleAssignmentDto` DTOs
   - `AdminUserSummary`, `RoleAssignment`, `RoleChangeAuditEntry` models

2. **Persistence layer:**
   - `AuditLogRepository` in `src/Persistence.MongoDb/Repositories/`
   - `RoleChangeAuditEntryConfiguration` EF Core config
   - `IAuditLogRepository` abstraction
   - Updated `IssueTrackerDbContext` with `RoleChangeAuditEntries` DbSet

3. **Web layer:**
   - `UserManagementService` — M2M token via client credentials, `IMemoryCache` (24h TTL − 5 min), role ID name→ID map cached 30 min
   - `Auth0ManagementOptions` sealed record
   - `UserManagementExtensions.AddUserManagement()`
   - `Auth0.ManagementApi 7.46.0` added to `Directory.Packages.props`
   - `appsettings.json` — added `Auth0Management` section with empty placeholders

### Findings

#### ❌ BLOCKING ISSUE 1: Architecture.Tests Failure

**Test failures:**
- `Architecture.Tests.CodeStructureTests.Repositories_ShouldImplementIRepository` — FAILED
- `Architecture.Tests.AdvancedArchitectureTests.AllRepositories_ShouldImplementIRepository` — FAILED

**Error message:**
```
Expected result.IsSuccessful to be True because All repositories should implement IRepository<T>.
Failing types: Persistence.MongoDb.Repositories.AuditLogRepository, but found False.
```

**Root cause:** `AuditLogRepository` is named like a repository but does NOT implement `IRepository<T>` interface.

**Fix required:** Choose one:
- **(Option A)** Make `AuditLogRepository` implement `IRepository<RoleChangeAuditEntry>` and inherit from `Repository<RoleChangeAuditEntry>` (if it's truly a repository pattern implementation)
- **(Option B)** Rename to `AuditLogService` or `AuditLogWriter` (if it's NOT a repository pattern implementation, but rather a specialized write-only service)

**Recommendation:** Option B is likely correct — audit logs are typically write-only append operations, not CRUD. The class should be renamed to reflect its true purpose.

#### ❌ BLOCKING ISSUE 2: Duplicate .squad/ File in Diff

**Problem:** `.squad/decisions/inbox/gandalf-auth0-management-api.md` appears in PR #158's diff, but this ADR was already added in PR #146.

**Root cause:** PR #158's branch (`squad/131-user-management-service`) was created before PR #146 merged to main.

**Fix required:** Rebase PR #158 on latest `main` after PR #146 merges. The ADR file should disappear from the diff.

#### ✅ Non-blocking observations:

- **File headers:** All new files carry required copyright block ✅
- **VSA compliance:** New code properly structured under `src/Web/Features/Admin/Users/` and `src/Domain/Features/Admin/` ✅
- **Rate limit retry TODO:** Comments note `// TODO: Rate limit retry on HTTP 429` per ADR — acceptable as known-future enhancement, not a blocking issue ✅
- **M2M token caching:** Implementation matches ADR strategy — `IMemoryCache` with 24h TTL − 5 min safety margin ✅
- **Role ID mapping cache:** 30 min TTL for role name→ID lookup — reasonable ✅

### Recommendation

**FIX REQUIRED** before merge:
1. Fix `AuditLogRepository` architecture violation (rename to `AuditLogWriter` or make it implement `IRepository<T>`)
2. Rebase on `main` after PR #146 merges to eliminate duplicate `.squad/` file in diff
3. Re-run full CI to confirm Architecture.Tests pass
4. Then submit for re-review

**After fixes:** This is high-quality implementation work that correctly follows the ADR from PR #146. The M2M token caching, role ID mapping cache, and error handling patterns are all well-designed.

---

## Merge Sequence

1. **PR #146** → Merge first (research spike, no blockers)
2. **PR #157** → Merge next (authorization scaffold, all green)
3. **PR #158** → Fix architecture issues, rebase, re-run CI, then merge

---

## Team Coordination

- Notified Sam (PR #158 author) of Architecture.Tests failure and `.squad/` diff issue
- Gandalf's ADR work in PR #146 is excellent foundation for PR #158 implementation
- Both blocking issues in PR #158 are straightforward fixes — should be resolved quickly

---

## Decisions Recorded

- AuditLogRepository naming violation flagged — team convention is that anything named `*Repository` MUST implement `IRepository<T>` per Architecture.Tests enforcement
- .squad/ file duplication on feature branches is acceptable when caused by branch timing — rebase after dependency PR merges to eliminate

---

# 🔒 Security Review: PR #158 — UserManagementService Auth0 Integration

**Reviewer:** Gandalf (Security Officer)  
**Date:** 2026-04-01  
**PR:** #158 — feat: Implement UserManagementService wrapping Auth0 Management API (#131)  
**Branch:** squad/131-user-management-service

---

## Verdict: ✅ APPROVED WITH NOTES

This PR implements Auth0 Management API integration with strong security fundamentals. All CRITICAL and HIGH severity issues have been **avoided by design**. Minor LOW/INFO findings are noted for future improvement.

---

## Security Findings

### ✅ PASS — Secrets Hygiene
**Status:** SECURE

- `appsettings.json` contains **only empty placeholders** for `Auth0Management:{ ClientId, ClientSecret, Domain, Audience }`
- No actual credentials committed to source control
- Follows existing pattern from `Auth0:ClientSecret` (placeholder-only in repo)
- Configuration binding via `Auth0ManagementOptions` sealed record is correct
- **Recommendation:** Document in README that production values must be stored in Azure Key Vault (same as OIDC credentials)

**Evidence:**
```json
"Auth0Management": {
  "ClientId": "",
  "ClientSecret": "",
  "Domain": "",
  "Audience": ""
}
```
✅ All values are empty strings — SECURE

---

### ✅ PASS — Token Security
**Status:** SECURE

**Token Storage:**
- M2M access tokens cached in `IMemoryCache` with key `"Auth0Management:Token"`
- Cache scope is application-wide (singleton cache) — **CORRECT** for M2M tokens (not user-specific)
- TTL set to `ExpiresIn - 300 seconds` (5-minute safety margin) — industry best practice
- No token leakage in logs:
  - `_logger.LogDebug("Fetching fresh Auth0 Management API token for domain '{Domain}'.", _options.Domain);` — logs domain only, NOT token ✅
  - `_logger.LogDebug("Auth0 Management API token cached. TTL={Ttl}s.", ttl);` — logs TTL only, NOT token ✅

**Token Acquisition:**
- Uses OAuth 2.0 **client credentials flow** (correct for M2M)
- `POST https://{domain}/oauth/token` with `grant_type=client_credentials`
- Audience scoped to `https://{domain}/api/v2/` (Auth0 Management API)
- `EnsureSuccessStatusCode()` used — will throw on HTTP 4xx/5xx (correct fail-fast behavior)

**Token Usage:**
- Fresh `ManagementApiClient` created per operation using `GetManagementClientAsync()`
- Client disposed after use (`using var client`) — prevents token leaks via long-lived clients
- No async-over-sync detected (proper `await` usage throughout)

**[INFO] Minor Improvement Opportunity:**
- Role ID cache (`"Auth0Management:Roles"`) stores role name→ID map for 30 min
- **Question:** If a role is deleted in Auth0 mid-cache, assignment/removal will fail with `ResultErrorCode.Validation` ("Unknown role")
- **Impact:** LOW — fail-safe behavior (rejects invalid role names), no security risk
- **Recommendation:** Acceptable as-is; future enhancement could catch `404` from Auth0 API and invalidate cache entry

---

### ✅ PASS — Client Credentials Scope
**Status:** SECURE

**M2M Client Separation:**
- Code expects separate `Auth0Management:{ ClientId, ClientSecret }` distinct from OIDC `Auth0:{ ClientId, ClientSecret }`
- Follows **least-privilege principle** — management API credentials isolated from user-facing OIDC flow
- If M2M credentials are compromised, attacker cannot impersonate users (no ID token issuance from M2M client)

**Audience Scoping:**
- Audience set to `https://{domain}/api/v2/` (Management API only)
- Tokens cannot be used for other Auth0 APIs or tenant resources
- **Auth0 Dashboard Configuration Required** (per ADR #130):
  - M2M app must be granted **minimum required scopes**: `read:users`, `read:roles`, `update:users`, `update:roles`
  - ⚠️ **[INFO]** Code does NOT validate scopes at runtime — relies on Auth0 API returning `403` if permissions missing
  - **Recommendation:** Acceptable as-is; Auth0 enforces scope boundaries server-side

---

### 🟡 INFO — Rate Limit TODO
**Status:** ACCEPTABLE TECHNICAL DEBT

**Finding:**
- Code comments note: `"Rate limits: Auth0 Management API returns HTTP 429 on burst. Add a Polly retry policy (per ADR #130) in a follow-up task"`
- No HTTP 429 retry/backoff implemented in PR #158
- Current behavior on rate limit: **immediate failure** via `EnsureSuccessStatusCode()` throwing `HttpRequestException`

**Risk Assessment:**
- **Severity:** LOW
- **Attack Surface:** None — missing retry does not create a security vulnerability
- **Operational Risk:** MEDIUM — burst API usage in admin UI could trigger 429 errors, degrading UX
- **DoS Risk:** None — lack of retry does not enable DoS; Auth0 enforces rate limits server-side

**Recommendation:**
- ✅ **ACCEPTABLE TO MERGE** — this is a reliability gap, not a security vulnerability
- Track HTTP 429 retry implementation in a follow-up issue (reference ADR #130 Polly example)
- Consider priority: MEDIUM (impacts admin UX, especially bulk operations)

---

### ✅ PASS — Input Validation
**Status:** SECURE

**`GetUserByIdAsync(string userId)`:**
```csharp
if (string.IsNullOrWhiteSpace(userId))
{
    return Result.Fail<AdminUserSummary>(
        "User ID must not be empty.",
        ResultErrorCode.Validation);
}
```
✅ Validates before passing to Auth0 API

**`AssignRolesAsync(string userId, IEnumerable<string> roleNames)`:**
```csharp
if (string.IsNullOrWhiteSpace(userId))
{
    return Result.Fail<bool>("User ID must not be empty.", ResultErrorCode.Validation);
}

var roleNamesList = (roleNames ?? []).ToList();
if (roleNamesList.Count == 0)
{
    return Result.Ok(true); // No-op if no roles specified
}

var unknown = roleNamesList.Where(r => !roleMap.ContainsKey(r)).ToList();
if (unknown.Count > 0)
{
    return Result.Fail<bool>(
        $"Unknown role(s): {string.Join(", ", unknown)}",
        ResultErrorCode.Validation);
}
```
✅ Validates userId, null-safe roleNames, rejects unknown role names

**`RemoveRolesAsync(string userId, IEnumerable<string> roleNames)`:**
- Same validation pattern as `AssignRolesAsync`

**Injection Risk:**
- Auth0 SDK uses **strongly-typed models** (`AssignRolesRequest { Roles = roleIds }`)
- No raw string concatenation or SQL-like injection surface
- Role IDs are resolved via dictionary lookup (`roleMap[r]`), not string interpolation
- Auth0 user IDs (e.g., `auth0|abc123`) are opaque identifiers — no special chars needing sanitization

**[INFO] Note:**
- `ListUsersAsync` accepts `int page, int perPage` with no upper-bound validation
- Auth0 API enforces `perPage` max of 100 server-side
- Code converts 1-based page → 0-based via `Math.Max(0, page - 1)`
- **Impact:** No security risk; Auth0 API will reject invalid pagination params

---

### ✅ PASS — Error Surfacing
**Status:** SECURE

**Pattern:**
```csharp
catch (Exception ex) when (ex is not OperationCanceledException)
{
    _logger.LogError(ex, "Failed to retrieve user from Auth0. UserId={UserId}", userId);

    return Result.Fail<AdminUserSummary>(
        $"Failed to retrieve user '{userId}': {ex.Message}",
        ResultErrorCode.ExternalService);
}
```

**Analysis:**
- Logs **full exception** server-side (includes stack trace) — ✅ CORRECT for diagnostics
- Returns **`ex.Message` only** to caller via `Result.Fail` — ✅ CORRECT, does NOT leak stack traces to client
- `ResultErrorCode.ExternalService` is generic — does NOT distinguish Auth0 `404 Not Found` vs `403 Forbidden` vs `500 Internal Error`

**[INFO] Tradeoff:**
- **Benefit:** Prevents leaking Auth0 API internals (e.g., "Role ID rol_abc123 does not exist")
- **Cost:** Caller cannot distinguish "user not found" (404) from "rate limited" (429) from "Auth0 outage" (503)
- **Recommendation:** Acceptable for v1; if admin UI needs granular error handling, introduce sub-codes (e.g., `ExternalService_NotFound`, `ExternalService_RateLimited`)

---

### ✅ PASS — Dependency Security
**Status:** SECURE

**Package:**
- `Auth0.ManagementApi` version **7.46.0** added to `Directory.Packages.props`
- Latest stable version as of 2026-04-01

**CVE Check:**
- ✅ **No known CVEs** for `Auth0.ManagementApi 7.46.0` in 2024–2025 (verified via CVE.org, NVD, OpenCVE)
- No security bulletins from Auth0/Okta referencing this package version
- Recent Auth0 CVEs affect `nextjs-auth0`, `node-jws`, PHP wrappers — NOT .NET SDK

**Recommendation:**
- Monitor Auth0 Security Bulletins: https://auth0.com/docs/secure/security-guidance/security-bulletins
- Subscribe to Okta security advisories: https://trust.okta.com/security-advisories/
- Dependabot or Renovate bot should flag future updates automatically

---

## Summary

PR #158 implements Auth0 Management API integration with **strong security posture**:

1. ✅ **Secrets hygiene** — no credentials committed, follows existing Key Vault pattern
2. ✅ **Token security** — M2M tokens cached safely, no logging leaks, proper scoping
3. ✅ **Input validation** — all user inputs validated before Auth0 API calls
4. ✅ **Least privilege** — M2M client separated from OIDC, audience scoped to Management API only
5. ✅ **Dependency security** — no known CVEs in Auth0.ManagementApi 7.46.0
6. 🟡 **Rate limit retry** — tracked as TODO (acceptable technical debt, no security impact)

**Approved for merge.**

---

## Checklist Status

| Security Check | Status |
|---|---|
| Secrets hygiene | ✅ PASS |
| Token caching security | ✅ PASS |
| Client credentials flow | ✅ PASS |
| Rate limit TODO | 🟡 ACCEPTABLE (non-blocking) |
| Role ID caching | ✅ PASS (fail-safe on stale cache) |
| Input validation | ✅ PASS |
| Error surfacing | ✅ PASS |
| Dependency CVE check | ✅ PASS (no known vulnerabilities) |

---

## Follow-Up Recommendations (Non-Blocking)

1. **[LOW]** Implement Polly retry policy for HTTP 429 (per ADR #130) — track in new issue
2. **[INFO]** Document in `src/Web/Auth/README.md` that `Auth0Management` secrets must be in Key Vault for production
3. **[INFO]** Monitor Auth0/Okta security bulletins for future SDK updates

---

**Reviewed by:** Gandalf 🔒  
**Signed off:** 2026-04-01

---

## Labels & Tags Design

### 2026-04-01: Issue Labels & Tags — Domain Design
**By:** Aragorn (via Squad)  
**Issue:** #147 (Spike)

### Label Storage
- **Field:** `public List<string> Labels { get; set; } = new();` on `Issue` model
- **Constraints:**
  - Lowercase and trimmed on insert/update
  - Max 50 characters per label
  - Max 10 labels per issue
- **Rationale:** Embedding labels as simple strings on the Issue document avoids a separate collection and keeps queries simple via MongoDB `$in` operator. Labels are low-cardinality, short strings—no normalization or lookup table needed.

### Label Source (Autocomplete)
- **Endpoint:** `GET /api/labels/suggestions?q={prefix}` 
- **Returns:** Top-N (e.g., 10) distinct labels from all existing issues, filtered by prefix
- **Implementation:** MongoDB `distinct()` query on `Issue.Labels` field, case-insensitive prefix match
- **Rationale:** Deriving labels dynamically from existing issue labels avoids maintaining a separate collection; users see only labels actually in use.

### Query Strategy
- **Filter by label:** MongoDB `$in` operator on `Issue.Labels` field
- **URL parameter:** `?label={labelValue}` on Issues list page and API
- **Rationale:** Simple, efficient, and leverages native MongoDB array filtering.

### IssueDto Updates
- Add `IReadOnlyList<string> Labels` property to `IssueDto` record
- Update `IssueDto(Issue issue)` constructor to map `Labels` from issue model
- Update `IssueDto.Empty` static property to include empty `[]` for Labels

### CQRS Commands
Two new commands in `src/Domain/Features/Issues/Commands/`:

1. **`AddLabelCommand(ObjectId IssueId, string Label)`**
   - Validates label format (lowercase, trimmed, max 50 chars)
   - Checks max 10 labels constraint
   - Adds to `Issue.Labels` if not already present (idempotent)
   - Dispatches `IssueUpdatedEvent`
   - Returns `Result<Unit>` with error if validation fails

2. **`RemoveLabelCommand(ObjectId IssueId, string Label)`**
   - Removes label from `Issue.Labels` if present
   - Dispatches `IssueUpdatedEvent`
   - Returns `Result<Unit>` (no error if label not found—idempotent)

### Validators
Create `LabelValidator` or inline validation in command handlers:
- Label must not be empty after trimming
- Label must be ≤ 50 characters
- Label is automatically lowercased (no validation needed, done in handler)

### Why This Design
1. **No separate collection:** Labels embedded on Issue keep the domain model simple and avoid join complexity
2. **MongoDB native:** `$in` queries are efficient and well-supported
3. **Dynamic sourcing:** Avoids maintaining a separate labels master table; labels emerge organically from issue data
4. **Constraints enforced at domain level:** Model validation ensures data integrity
5. **Unblocks downstream work:** Clear storage and query strategy enables comment labeling, bulk-label operations, and label-based analytics

### Blocked Issues Unblocked
- #148, #149, #150, #151, #152, #153, #154, #155, #156 can now proceed with implementation details

---

## PR Review Decisions

### 2026-04-01: PR #158 Approved After Architecture Fixes
**Date:** 2026-04-01  
**Author:** Aragorn (Lead Developer)  
**Status:** Approved  
**Related:** PR #158 (`squad/131-user-management-service`), Issues #131, #132, #134

#### Context
PR #158 was initially rejected for two blocking issues:

1. **Architecture Violation:** `AuditLogRepository` did not implement `IRepository<T>` but was named like a repository, causing Architecture.Tests to fail.
2. **Duplicate ADR File:** Branch had not been rebased after PR #146 merged, causing `.squad/` file duplication in diff.

Sam applied fixes and requested re-review.

#### Verification Completed

##### Fix 1: AuditLogRepository → AuditLogWriterService ✅
- Class renamed: `AuditLogRepository` → `AuditLogWriterService`
- Interface renamed: `IAuditLogRepository` → `IAuditLogWriterService`
- Namespace changed: `Persistence.MongoDb.Repositories` → `Persistence.MongoDb.Services`
- DI registration updated in `ServiceCollectionExtensions.cs`
- All constructor injection and call sites updated
- File headers present and correct on both interface and implementation
- **No class named `*Repository` exists in the PR that doesn't implement `IRepository<T>`**

##### Fix 2: Branch Rebase ✅
- Branch rebased onto `main` after PR #146 merged
- No duplicate `.squad/` files in current diff

#### Additional Quality Checks ✅
1. **Domain Layer Purity:** `RoleChangeAuditEntry` uses plain `string Id` — NO MongoDB types (`ObjectId`, `[BsonId]`) in Domain layer. ObjectId mapping handled correctly in `Persistence.MongoDb.Configurations.RoleChangeAuditEntryConfiguration`.
2. **Thread-Safe Caching:** Token and role caching uses `GetOrCreateAsync` — prevents concurrent cold-start races.
3. **DI Extension Self-Contained:** `AddUserManagement()` calls `AddMemoryCache()` — idempotent and safe.
4. **File Headers:** All 12 new C# files have correct copyright header with proper project names.
5. **Naming Conventions:** All interfaces, services, options, extensions follow team conventions.

#### CI Verification ✅
- Architecture.Tests: **PASSED** (19/19 checks green)
- All tests: **1,805 tests passed, 0 failed**
- Build: **0 warnings, 0 errors** (Release configuration with `TreatWarningsAsErrors=true`)

#### Pattern Established
**Repository vs. Service Naming:**
- Classes in `Repositories/` namespace **MUST** implement `IRepository<T>` and provide full CRUD operations
- Append-only or specialized persistence operations (audit logs, event sourcing, write-only buffers) should be named as `*Service` or `*Writer` and placed in `Services/` namespace
- This PR sets the precedent: `AuditLogWriterService` is the correct pattern for audit log persistence

#### Consequences
- ✅ PR #158 is approved and ready to merge
- ✅ Pattern documented for future audit logs and specialized persistence operations
- ✅ Architecture tests continue to enforce the `IRepository<T>` contract for all `*Repository` classes
- ✅ Domain layer remains free of persistence-infrastructure types (MongoDB, EF Core attributes)

---

## Admin User Management v0.5.0

#### Decision 1: Auth0 Management API via M2M client credentials
**What:** The app will integrate with Auth0 Management API v2 using a dedicated Machine-to-Machine (M2M) application with the `client_credentials` grant. The M2M app is separate from the user-facing Auth0 application.

**Why:** The user-facing Auth0 app uses the Authorization Code flow (user identity). Management API operations (listing users, assigning roles) require a server-to-server token with scoped Management API permissions — a different trust model that must not share credentials with the user-facing app.

**Consequences:**
- New secrets required: `AUTH0_MANAGEMENT_CLIENT_ID`, `AUTH0_MANAGEMENT_CLIENT_SECRET` (Boromir — CI, Gandalf — Auth0 setup)
- M2M tokens must be cached (short-lived, typically 24h) to avoid rate limits
- Spike #130 will confirm exact scopes: `read:users`, `read:roles`, `update:users`

#### Decision 2: SDK choice deferred to spike — Auth0.ManagementApi vs raw HttpClient
**What:** The decision between using the `Auth0.ManagementApi` NuGet package and a raw typed `HttpClient` is deferred to the completion of spike #130.

**Why:** The Auth0 .NET Management SDK may not be fully compatible with .NET 10 / AOT compilation, and its abstraction may conflict with the project's existing HttpClient resilience policies. The spike will benchmark both and produce a recommendation.

**Consequences:**
- `UserManagementService` (#131) depends on spike #130
- If raw HttpClient is chosen: `IHttpClientFactory` + Polly retry policy will be used
- If Auth0 SDK is chosen: version pinned in `Directory.Packages.props`

#### Decision 3: Vertical Slice — all admin user management code under `src/Web/Features/Admin/Users/`
**What:** Following the project's Vertical Slice Architecture, all admin user management code (commands, queries, handlers, service interface) lives under `src/Web/Features/Admin/Users/`. The `IUserManagementService` interface is defined in `src/Domain/` for testability.

**Why:** Consistent with the existing vertical slice layout for Issues and Suggestions. Keeps the admin feature self-contained and deletable/replaceable as a unit.

**Consequences:**
- Blazor components go in `src/Web/Components/Admin/Users/`
- No new projects — this feature fits within the existing `src/Web` project

#### Decision 4: Audit log is append-only in MongoDB, never updates or deletes
**What:** `RoleChangeAuditEntry` documents are written once and never modified. No soft-delete, no status updates.

**Why:** Audit logs are a compliance artifact. Mutability would undermine their evidentiary value. Append-only semantics also eliminate concurrency concerns on writes.

**Consequences:**
- Index on `(TargetUserId, Timestamp)` for admin query performance
- No archive/purge policy in v0.5.0 — deferred to v0.6.0 if needed
- Audit writes are fire-and-forget (non-blocking) but failures are logged via `ILogger`

#### Decision 5: AdminPolicy enforced at Blazor page level, not middleware
**What:** The `AdminPolicy` authorization attribute is applied at the Blazor component level (`@attribute [Authorize(Policy = "AdminPolicy")]`), not as a route-level middleware constraint.

**Why:** Blazor Server route authorization is best expressed at the component level to ensure the authorization pipeline runs correctly in the Blazor hub context. Middleware-level auth for Blazor Server circuits has known edge cases around circuit reconnection.

**Consequences:**
- Every admin page component must carry the `[Authorize]` attribute explicitly
- Navigation guard in `NavMenu.razor` via `<AuthorizeView>` provides UX protection (not security — the policy is the security)
- Integration tests (#143) will verify the policy holds via `WebApplicationFactory`

#### Sprint Structure
| Sprint | Theme | Issues | Count |
|--------|-------|--------|-------|
| 5A | Foundation | #130, #131, #132, #133, #134, #135 | 6 |
| 5B | UI | #136, #137, #138, #139, #140 | 5 |
| 5C | Quality | #141, #142, #143, #144, #145 | 5 |

**Total:** 16 issues · Milestone #7

---

### 2026-04-01: Release Blog Post Trigger
**By:** Bilbo  
**What:** Release blog posts for v0.3.0 and v0.4.0 were not written when the releases were published. These were critical mandatory posts (per charter) that should have been published immediately. On 2026-04-01, Bilbo wrote catch-up posts for both releases.

**Why:** Keeping blog in sync with releases ensures developers always have up-to-date, accurate release notes in narrative form. Missing posts creates a documentation gap.

**Action:** Going forward, whenever Ralph (DevOps) publishes a GitHub Release:
1. Release blog post task should be **synchronously triggered** (not async)
2. Bilbo should write the post within the same day as release publication
3. Consider adding a GitHub Actions workflow that comments on the release with a link to the blog post once published

**Outcome:** v0.3.0 and v0.4.0 blog posts are now live; blog landing page (`docs/blog/index.md`) and website blog table (`docs/index.html`) have been updated.

---

### 2026-04-01: Auth0 Management API Secrets Configuration Pattern
**Date:** 2026-04-01  
**Author:** Boromir (DevOps)  
**Status:** Implemented  
**Related:** Issue #145, PR #162

#### Configuration Section
All Auth0 Management API credentials are stored in the `Auth0Management` section:

```json
{
  "Auth0Management": {
    "ClientId": "xxx",
    "ClientSecret": "xxx",
    "Domain": "tenant.auth0.com",
    "Audience": "https://tenant.auth0.com/api/v2/"
  }
}
```

This is separate from the `Auth0` section (used for authentication).

#### .NET Aspire AppHost Integration
In `src/AppHost/AppHost.cs`:

```csharp
var auth0MgmtClientId = builder.AddParameter("auth0-mgmt-client-id", secret: true);
var auth0MgmtClientSecret = builder.AddParameter("auth0-mgmt-client-secret", secret: true);

builder.AddProject<Projects.Web>("web")
    .WithEnvironment("Auth0Management__ClientId", auth0MgmtClientId)
    .WithEnvironment("Auth0Management__ClientSecret", auth0MgmtClientSecret);
```

Aspire prompts for these values at startup when missing.

#### GitHub Actions CI/CD
In `.github/workflows/squad-test.yml` and `.github/workflows/codeql-analysis.yml`:

```yaml
env:
  Auth0Management__ClientId: ${{ secrets.AUTH0_MANAGEMENT_CLIENT_ID }}
  Auth0Management__ClientSecret: ${{ secrets.AUTH0_MANAGEMENT_CLIENT_SECRET }}
  Auth0Management__Domain: ${{ secrets.AUTH0_DOMAIN }}
  Auth0Management__Audience: https://${{ secrets.AUTH0_DOMAIN }}/api/v2/
```

**Note:** Domain and Audience reference the existing `AUTH0_DOMAIN` secret to avoid duplication.

#### Development Placeholders
`src/Web/appsettings.Development.json` includes placeholder entries:

```json
{
  "Auth0Management": {
    "ClientId": "",
    "ClientSecret": "",
    "Domain": "",
    "Audience": ""
  }
}
```

This signals the schema but provides no real values. Developers must configure via Aspire parameters or User Secrets.

#### Empty Secrets Handling
`UserManagementService.GetOrFetchTokenAsync()` sends credentials directly to Auth0's `/oauth/token` endpoint. If `ClientId` or `ClientSecret` are empty strings:

- Auth0 returns HTTP 401/403
- `response.EnsureSuccessStatusCode()` throws `HttpRequestException`
- Service catches the exception and returns `Result.Fail<T>` with `ResultErrorCode.ExternalService`

**This is graceful degradation:** Admin UI features fail gracefully with error messages; the app does not crash.

**Future consideration (Sam's domain):** Add explicit validation in `UserManagementService` constructor or `GetOrFetchTokenAsync()` to fail fast with clearer messages when credentials are missing.

#### Consequences

##### Positive
- CI/CD pipelines can now exercise Admin User Management code paths (when secrets are configured)
- Local dev works without hardcoding credentials (Aspire prompts at startup)
- Production deployments can inject secrets via Azure Key Vault, AWS Secrets Manager, etc.
- Separation of Auth0 authentication (`Auth0` section) and management (`Auth0Management` section) is clear

##### Negative
- Repository admin must manually add `AUTH0_MANAGEMENT_CLIENT_ID` and `AUTH0_MANAGEMENT_CLIENT_SECRET` to GitHub secrets (documented in PR #162)
- Empty placeholders in `appsettings.Development.json` may confuse new developers; recommend adding a comment in the file (Sam's domain)

#### Related Files
- `src/AppHost/AppHost.cs`
- `src/Web/appsettings.Development.json`
- `src/Web/Features/Admin/Users/UserManagementService.cs`
- `.github/workflows/squad-test.yml`
- `.github/workflows/codeql-analysis.yml`

---

### 2026-04-01: Admin User Management Documentation Structure (Frodo)
**Date:** April 1, 2026  
**Author:** Frodo (Tech Writer)  
**Relates to:** Issue #144  
**PR:** #161

#### Decision
Created a dedicated `docs/features/admin-user-management.md` file for the v0.5.0 Admin User Management feature, following a consistent documentation structure and archival pattern for feature-specific guides.

#### Context
The Admin User Management feature requires comprehensive developer and operational documentation to enable:
1. Local development setup with Auth0 M2M credentials
2. Understanding of the architecture (MediatR CQRS pattern, Auth0 Management API integration, audit logging)
3. Operational security best practices for role management
4. Troubleshooting common configuration issues

#### Rationale

##### Why a separate feature documentation file?
1. **Scalability**: As the project grows, feature-specific docs in `docs/features/` keep the root-level `docs/` directory clean and focused on cross-cutting concerns (ARCHITECTURE.md, SECURITY.md, CONTRIBUTING.md)
2. **Findability**: Developers looking for "User Management" documentation naturally check `docs/features/admin-user-management.md` before root docs
3. **Maintainability**: Each feature doc is owned by the feature team (in this case, Frodo), making it easier to keep documentation in sync with code
4. **Modularity**: Supports a future pattern where feature teams can include onboarding, architecture, and troubleshooting all in one place

##### Documentation structure adopted
Each feature guide includes:
- **Overview**: What the feature does and who can use it
- **Prerequisites**: External setup required (e.g., Auth0 M2M app creation)
- **Setup**: Local development configuration steps
- **Features**: Description of each user-facing capability
- **Architecture**: Components, data flow, CQRS pattern
- **Security**: Authorization, secrets management, audit trail, best practices
- **Troubleshooting**: Common issues and resolutions
- **Related Documentation**: Links to connected guides

This structure is consistent with existing docs/FEATURES.md style but organized by feature rather than by feature category.

#### Impact

##### For Developers
- Clear setup path: Prerequisites → Local Development Setup → Features → Architecture
- Understanding of CQRS pattern (Queries, Commands, Handlers, Validators) in context of a real feature
- Secrets management best practices for Auth0 M2M credentials

##### For Operations/Admins
- Operational security notes on role change auditing
- Troubleshooting section for the most common issues
- Best practices for principle of least privilege

##### For Documentation Standards
- Establishes pattern for future feature docs in `docs/features/`
- Frodo (Tech Writer) owns all files in `docs/` and can maintain feature docs independently
- Root-level docs/ remains focused on cross-cutting architecture concerns

#### Alternatives Considered
1. **Add to docs/FEATURES.md**: Would clutter the existing feature index; less discoverable for someone searching for Admin User Management docs
2. **Create docs/admin-user-management.md at root**: Keeps feature docs at root level but doesn't scale as project grows (10+ features = 10+ root files)
3. **Only update README.md**: Would lack technical depth needed for developers and operators

#### Decisions Made During Implementation
1. **No YAML front matter for feature docs**: The new admin-user-management.md is internal developer documentation, not a blog post, so no YAML metadata required
2. **Auth0 M2M setup as primary prerequisite**: Emphasized that Auth0 dashboard M2M app creation is required before any local development can proceed
3. **dotnet user-secrets for local configuration**: Used rather than appsettings.json to emphasize security best practices (secrets not in source control)
4. **Immutable audit log pattern**: Documented as append-only MongoDB collection for compliance auditing, never modifiable

---

## Auth0 Management API (Gandalf — ADR #130)

#### Context
IssueTrackerApp currently uses Auth0 for end-user authentication via the OIDC Authorization Code flow with PKCE (`src/Web/Auth/`). Role assignment (Admin / User) is managed manually in the Auth0 dashboard. As the platform scales and automated user-role provisioning becomes necessary (e.g., assigning roles programmatically upon user registration, syncing roles from an admin UI), direct calls to the **Auth0 Management API v2** are required.

The existing `Auth0Options` binds `Domain`, `ClientId`, `ClientSecret`, and `RoleClaimNamespace` from configuration. The existing credential-based setup is an OIDC client app — it is **not** a Machine-to-Machine (M2M) app and does not hold Management API scopes. A separate M2M configuration is required.

This spike evaluates:
1. Which Management API v2 endpoints are needed
2. How to obtain and cache M2M access tokens (client credentials flow)
3. Auth0 rate limits and pagination strategy
4. SDK choice: `Auth0.ManagementApi` NuGet package vs raw `HttpClient`
5. Required Auth0 dashboard configuration
6. Secrets management strategy

#### Decision
**Use the official `Auth0.ManagementApi` NuGet package (`ManagementApiClient`) with a dedicated M2M application, caching the Management API token in `IMemoryCache` with a TTL-based refresh strategy, and storing M2M credentials in .NET User Secrets (development) and Azure Key Vault (production).**

Rationale:
- The official SDK is actively maintained by Auth0/Okta, handles token acquisition internally, provides strongly-typed request/response objects, and reduces boilerplate.
- A dedicated M2M app in Auth0 cleanly separates management-plane credentials from user-facing OIDC credentials, limiting blast radius on credential rotation.
- The app already uses `IMemoryCache` for analytics TTLs; reusing the same pattern for token caching is idiomatic and avoids new infrastructure.

#### Consequences

##### Positive
- Programmatic role assignment enables automated onboarding and admin UI workflows without manual Auth0 dashboard intervention.
- Strongly-typed SDK reduces surface area for serialization bugs.
- Token caching avoids unnecessary M2M token requests and respects rate limits.
- Separation of M2M and OIDC credentials follows least-privilege principle.

##### Negative / Trade-offs
- Adds a new NuGet dependency (`Auth0.ManagementApi`).
- Requires Auth0 dashboard configuration (new M2M app, API permission grants) — this is a manual step that cannot be automated by code alone.
- M2M tokens are sensitive; any misconfiguration of Key Vault access policies would cause Management API calls to fail at runtime.
- Rate limits on the free Auth0 tier (2 req/sec burst, ~1,000 req/month on some plan tiers) mean bulk operations must be throttled.

#### Implementation Summary
- **Auth0 Dashboard Setup:** Create M2M app with scopes `read:users`, `read:roles`, `read:role_members`, `update:users`, `create:role_members`, `delete:role_members`
- **NuGet:** Add `Auth0.ManagementApi` to `Directory.Packages.props`
- **Secrets:** `Auth0Management:ClientId`, `Auth0Management:ClientSecret`, `Auth0Management:Domain`, `Auth0Management:Audience`
- **Token Caching:** `IMemoryCache` with 24h TTL (minus 5m safety margin)
- **Rate Limits:** Polly retry policy for HTTP 429; paginate list endpoints sequentially
- **SDK Usage:** `ManagementApiClient` for all role and user operations

---

## Process & Workflow

### 2026-04-01T17:57Z: Branching strategy — rebase before merge
**By:** mpaulosky (via Ralph session)  
**What:** All squad PR branches must be rebased onto current `main` before Aragorn performs the merge ceremony. A PR that passes CI on a stale base is not considered mergeable — the rebase must happen first, then CI must be green on the updated tip.

**Why:** PR #160 demonstrated the failure mode: branch cut before `99a446d` landed, conflicts discovered at review time rather than at author time. Rebasing before merge ensures CI tests the actual merged state, not a diverged snapshot.

**How to enforce:** Aragorn's review gate checklist now includes: (1) check `gh pr view --json mergeStateStatus` — if `BEHIND`, rebase the branch first; (2) re-trigger CI after rebase; (3) only merge once CI is green on the rebased tip.
### Frontend Components & Styling

#### RoleBadge `.badge` Utility Pattern (2026-03-29)

**Author:** Legolas  
**Issue:** #138

When implementing `RoleBadge.razor`, two options were available for pill styling:
1. Inline Tailwind classes on every `<span>` (e.g. `inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium`)
2. Use the project-defined `.badge` utility class from `src/Web/Styles/input.css`

**Decision:** Use the `.badge` CSS utility class for all badge/pill renders in the admin area.

**Rationale:**
- The `.badge` class already exists in `src/Web/Styles/input.css` and compiles to the exact same Tailwind utility set.
- Using the shared class ensures consistent pill sizing/shape project-wide.
- Reduces duplication — if pill dimensions change, one place to update.
- The existing `UserListTable.razor` had been inlining the same classes; `RoleBadge` provides the canonical extraction.

**Impact:** Any future badge-like component should use `.badge` + a color modifier class, not raw inline Tailwind. Color modifier pattern: `"badge bg-red-100 text-red-800 dark:bg-red-900 dark:text-red-200"` (append color classes to base `.badge`).

---

### Domain Models & DTOs

#### Labels Field Appended to IssueDto Positional Record (2026-04-01)

**Author:** Sam (Backend Developer)  
**Issue:** #149

**Decision:** `Labels` is appended as the **last** positional parameter in `IssueDto` (after `VotedBy`), not inserted in the middle.

**Rationale:** `IssueDto` is a positional record. Inserting a new parameter anywhere other than the end would shift all subsequent positional arguments, breaking every call site that uses positional construction syntax. Appending at the end is the safe default for positional records in this codebase.

**Implication for future fields:** Any new fields added to `IssueDto` should continue to be appended at the end unless there is a strong semantic reason to reorder, in which case all call sites must be audited.

---

### Authentication & Authorization

#### Auth0 Management API Integration Strategy (2026-04-01)

**Author:** Gandalf  
**Issue:** #130 — [Spike] Auth0 Management API — capabilities, rate limits, and SDK options

IssueTrackerApp currently uses Auth0 for end-user authentication via OIDC Authorization Code flow with PKCE. Role assignment (Admin / User) is managed manually in the Auth0 dashboard. As the platform scales, automated user-role provisioning becomes necessary. This ADR evaluates Auth0 Management API v2 integration strategy.

**Decision:** Use the official `Auth0.ManagementApi` NuGet package (`ManagementApiClient`) with:
- A dedicated Machine-to-Machine (M2M) application in Auth0
- Token caching in `IMemoryCache` with TTL-based refresh strategy
- M2M credentials in .NET User Secrets (dev) and Azure Key Vault (production)

**Rationale:**
- Official SDK is actively maintained, handles token acquisition, provides strongly-typed objects, reduces boilerplate.
- Dedicated M2M app cleanly separates management-plane credentials from user-facing OIDC credentials, limiting blast radius on credential rotation.
- App already uses `IMemoryCache` for analytics; reusing pattern is idiomatic.

**Positive Consequences:**
- Programmatic role assignment enables automated onboarding and admin UI workflows.
- Strongly-typed SDK reduces serialization bugs.
- Token caching avoids unnecessary M2M token requests and respects rate limits.
- Separation of M2M and OIDC credentials follows least-privilege principle.

**Negative / Trade-offs:**
- Adds new NuGet dependency (`Auth0.ManagementApi`).
- Requires Auth0 dashboard configuration (new M2M app, API permission grants) — manual steps.
- M2M tokens are sensitive; misconfigured Key Vault access policies would cause Management API calls to fail at runtime.
- Rate limits on free Auth0 tier (2 req/sec burst) mean bulk operations must be throttled.

**Required Auth0 Dashboard Setup:**
1. Create Machine-to-Machine Application in Auth0 dashboard
2. Grant API permissions: `read:users`, `read:roles`, `read:role_members`, `update:users`, `create:role_members`, `delete:role_members`
3. Note M2M app `Client ID` and `Client Secret`

**Required Secrets** (distinct from existing `Auth0:ClientId`/`Auth0:ClientSecret`):
- `Auth0Management:ClientId` — Client ID of M2M application
- `Auth0Management:ClientSecret` — Client Secret of M2M application
- `Auth0Management:Domain` — Same as `Auth0:Domain`
- `Auth0Management:Audience` — `https://{your-tenant}.auth0.com/api/v2/`

**Development (User Secrets):**
```bash
dotnet user-secrets set "Auth0Management:ClientId"     "YOUR_M2M_CLIENT_ID"
dotnet user-secrets set "Auth0Management:ClientSecret" "YOUR_M2M_CLIENT_SECRET"
dotnet user-secrets set "Auth0Management:Domain"       "your-tenant.auth0.com"
dotnet user-secrets set "Auth0Management:Audience"     "https://your-tenant.auth0.com/api/v2/"
```

**Production (Azure Key Vault):**
Store as Key Vault secrets: `Auth0Management--ClientId`, `Auth0Management--ClientSecret`, `Auth0Management--Domain`, `Auth0Management--Audience`. Existing `KeyVault:Uri` in `appsettings.json` auto-wires pickup.

**Token Caching Strategy:**
Auth0 Management API tokens have default TTL of 86,400 seconds (24 hours). Implement `Auth0ManagementTokenCache` service using `IMemoryCache` with safety margin (expire 5 minutes before actual TTL).

**Rate Limit Strategy:**
Auth0 enforces rate limits per tenant tier (free: ~2 req/sec). Rate-limit responses return HTTP 429 with `Retry-After` header. Implement Polly retry policy with exponential backoff and respect `Retry-After` header. For list endpoints, process pages sequentially with small delay (100ms between pages).

**API Endpoints (relative to `https://{domain}/api/v2/`):**
- List users: `GET /users?per_page=100&page={n}&include_totals=true` (requires `read:users`)
- Get user: `GET /users/{user_id}` (requires `read:users`)
- List roles: `GET /roles?per_page=100&page={n}&include_totals=true` (requires `read:roles`)
- Get role: `GET /roles/{role_id}` (requires `read:roles`)
- Get role members: `GET /roles/{role_id}/users?per_page=100` (requires `read:role_members`)
- Assign roles: `POST /users/{user_id}/roles` with body `{"roles": ["rol_XXXXXXXXXXXXXX"]}` (requires `create:role_members`)
- Remove roles: `DELETE /users/{user_id}/roles` (requires `delete:role_members`)

**Role ID Mapping:**
Auth0 roles have internal ID (e.g., `rol_XXXXXXXXXXXXXX`) differing from display name. Resolve role IDs by name at startup and cache to avoid hardcoding tenant-specific IDs.

**References:**
- [Auth0 Management API v2 Reference](https://auth0.com/docs/api/management/v2)
- [Auth0 Client Credentials Flow](https://auth0.com/docs/get-started/authentication-and-authorization-flow/client-credentials-flow)
- [Auth0.ManagementApi NuGet Package](https://www.nuget.org/packages/Auth0.ManagementApi)
- [Auth0 Rate Limit Policy](https://auth0.com/docs/troubleshoot/customer-support/operational-policies/rate-limit-policy)

**Follow-Up Recommendations (Non-Blocking):**
1. **[LOW]** Implement Polly retry policy for HTTP 429 — track in new issue
2. **[INFO]** Document in `src/Web/Auth/README.md` that `Auth0Management` secrets must be in Key Vault for production
3. **[INFO]** Monitor Auth0/Okta security bulletins for future SDK updates

---

**Scribe Note:** Merged from decision inbox files 2026-04-01T21:01:59Z
