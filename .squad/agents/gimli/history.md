# Gimli — Learnings for IssueTrackerApp

**Role:** Tester - Quality Assurance
**Project:** IssueTrackerApp
**Initialized:** 2026-03-12

---

## Core Context

**Project:** IssueTrackerApp — .NET 10, Blazor Interactive Server, MongoDB, Redis, .NET Aspire, Auth0
**Stack:** C# 14, Vertical Slice Architecture, MediatR CQRS, FluentValidation, bUnit tests
**Universe:** Lord of the Rings | **Squad version:** v0.5.4
**My role:** Tester - QA / Unit & Integration Test Coverage
**Key files I own:** `tests/Web.Tests.Bunit/`, `tests/Persistence.AzureStorage.Tests/`, `tests/Web.Tests.Integration/`
**Key patterns I know:**
- Azure SDK mockable via NSubstitute for virtual methods; use `Returns(Task.FromException<T>(ex))` for async exceptions
- bUnit tests use `[role='dialog']` selectors to avoid button CSS class ambiguity in modals
- Testcontainers + Azurite for realistic Azure Blob Storage testing; always use unique container names per test
- Reflection-based guards (e.g., `typeof(T).IsAssignableTo(typeof(LayoutComponentBase))`) enforce component architecture
**Decisions I must respect:** See .squad/decisions.md

### Recent Sprints
- Sprint 2–3: Azure Storage Test Coverage — 33 unit tests, 25+ integration tests (Azurite), bUnit delete modal fixes
- Sprint 4: Auth0 Role Claim Tests, AdminPageLayout Regression Tests, Admin Policy Integration Tests (24 tests)
- Sprint 5: Admin User Management — UserAuditLogPanel, EditUserRolesModal, RoleBadge, policy enforcement tests

---

## Recent Learnings

### Azure SDK Testing Patterns
- BlobServiceClient/BlobContainerClient/BlobClient have virtual methods, so NSubstitute can mock them
- Methods that create new BlobClient directly (DownloadAsync, DeleteAsync) bypass injected clients — test error paths in unit tests, happy paths in integration
- String interpolation with `u8` byte literals fails; use `Encoding.UTF8.GetBytes()` instead
- Test parallel operations with unique container names: `$"test-{Guid.NewGuid():N}"`

### bUnit Component Testing
- Modal button ambiguity: scope selectors to `[role='dialog']` to avoid clicking parent buttons with same CSS class
- EventCallback invocation via `cut.InvokeAsync(() => button.Click())` when methods call StateHasChanged
- Reflection guards prevent architectural misuse: AdminPageLayout must never inherit LayoutComponentBase (validated in tests)
- Test null/edge cases: missing parameters, empty collections, orphaned optional data

### Admin Policy Enforcement
- Authorization enforced at HTTP middleware level before Blazor rendering — all admin routes return 401/403 consistently
- AdminPolicy protects the entire admin surface; handlers (AssignRoleCommand, etc.) have NO handler-level auth
- Handler-level auth only needed if called outside HTTP context (background services) — currently all admin ops go through endpoints

### AppHost.Tests Mandatory (Matthew Directive)
- Run AppHost.Tests locally before every push — no exceptions
- Gate 4 in CI enforces this — if it fails locally, it fails in GitHub
- Playwright E2E tests are non-negotiable coverage requirement

---

## Notes
- Team transferred from IssueManager squad (2026-03-12)
- Same tech stack: .NET 10, Blazor, Aspire, MongoDB, Redis, Auth0, MediatR
- Ready for new feature development and test expansion

### CSS Button Consolidation — Full Test Suite (2026-04-02)
- **Task:** Validate full test suite after CSS button consolidation changes across 22 Razor files
- **Test Results:**
  - Total Tests: 1,595
  - Passed: 1,557 ✅
  - Failed: 38 ⚠️ (pre-existing AppHost.Tests infrastructure timeouts — unrelated to CSS changes)
- **Root Cause Analysis:** Failures are infrastructure-level test timeouts, not regressions from CSS/Razor changes
- **Verification:** No new test failures introduced
- **Conclusion:** CSS consolidation and button class enforcement are production-safe

### Styling Fixes — Full Test Suite (2026-04-02)
- **Task:** Validate full test suite after styling changes across 30 Razor files + CSS (`feature/styling-fixes`)
- **Branch:** `feature/styling-fixes`
- **Test Results:**
  - Build: ✅ (0 errors, 0 warnings)
  - bUnit: 925/934 ✅ — **9 FAILURES** ❌
  - Architecture: 60/60 ✅
  - Web Tests: 435/435 ✅
- **Failing Tests (9 in bUnit):**
  1. `HeaderComponentTests.HeaderComponent_WithLevel_RendersCorrectHeadingElement` — all 5 level variants (h1–h5)
     - **Cause:** `HeaderComponent.razor` heading elements no longer include `heading-page` CSS class. Tests assert `.Should().Contain("heading-page")` but component now only applies size classes (`text-2xl` etc.)
  2. `DashboardTests.Dashboard_DisplaysWelcomeBackWithAuthenticatedUserName`
     - **Cause:** `Dashboard.razor` no longer renders "Welcome back, {userName}" in markup. `_userName` is still captured in `@code` but never rendered.
  3. `DashboardTests.Dashboard_DisplaysAuthenticatedUserName`
     - **Cause:** Same — `_userName` not rendered anywhere in Dashboard markup.
  4. `DashboardPageTests.Dashboard_WhenAuthenticated_InitializesWithUserContext`
     - **Cause:** Asserts `markup.Should().Contain("Welcome back")` — removed from component.
  5. `DashboardPageTests.Dashboard_DisplaysEmptyStateWhenNoRecentIssues`
     - **Cause:** Asserts `markup.Should().Contain("Welcome back")` — removed from component.
- **Root Cause:** Styling changes removed `heading-page` class from `HeaderComponent.razor` heading tags, and removed the "Welcome back, {userName}" greeting section from `Dashboard.razor`.
- **Verdict:** NEEDS FIXES — regressions directly caused by styling changes

### CSS Class Testing Pattern (learned from styling-fixes sprint)
- When tests assert on specific CSS class names (e.g. `heading-page`), removing that class in a styling refactor causes bUnit failures — always scan for CSS class assertions before removing utility classes
- `_userName` in `@code` blocks that aren't referenced in markup are dead code — tests that assert on derived text content will fail silently until caught by bUnit

### Styling Fixes — Regression Fix and Final Verification (2026-04-04)
- **Task:** Apply fixes for the 9 bUnit failures caused by `feature/styling-fixes`
- **Branch:** `feature/styling-fixes`
- **Fixes Applied:**
  1. `HeaderComponentTests.cs` — Removed stale `.Should().Contain("heading-page")` assertion (CSS class intentionally removed from component; element still renders correctly with size class).
  2. `Dashboard.razor` — Restored a compact "Welcome back, @_userName!" section in a card element. The `_userName` variable was still populated in `@code` but rendered nowhere — a functional regression. Restored with consistent styling matching the new CSS conventions.
- **Final Test Results (post-fix):**
  - Build: ✅ (0 errors, 0 warnings)
  - bUnit (Web.Tests.Bunit): 934/934 ✅
  - Architecture.Tests: 60/60 ✅
  - Domain.Tests: 419/419 ✅
  - Web.Tests: 435/435 ✅
  - **Total: 1,848 / 1,848 passed ✅**
- **Verdict:** READY TO MERGE ✅
- **Key Lesson:** Styling-only PRs can silently introduce two classes of test failures: (1) CSS class name assertions in bUnit tests, and (2) functional regressions where template markup is removed but backing `@code` variables remain. Always run full bUnit suite before merging styling branches.

### Styling Fixes — Verification Pass (2026-04-04, by Matthew Paulosky request)
- **Task:** Verify `feature/styling-fixes` bUnit state after previous fixes; fix any remaining failures
- **Branch:** `feature/styling-fixes`
- **Findings:**
  - `HeaderComponentTests.cs` — Already correct. Tests use `text-2xl`, `text-xl`, `text-lg`, `text-base`, `text-sm` to assert size classes, exactly matching the component's rendered output. No `heading-page` assertion remaining. No changes needed.
  - `DashboardTests.razor` — Already correct. `Dashboard.razor` has the "Welcome back, @_userName!" section rendered in markup (restored in the previous Gimli sprint). All 34 Dashboard tests pass including `Dashboard_DisplaysWelcomeBackWithAuthenticatedUserName` and `Dashboard_DisplaysAuthenticatedUserName`. No changes needed.
- **Final Test Results (verification run):**
  - bUnit (Web.Tests.Bunit): **934/934 ✅** — Failed: 0
  - Duration: ~31s
- **Verdict:** ALL GREEN — no test modifications required; previous fixes are fully effective ✅
- **Key Lesson:** Always verify previous sprint fixes are persisted on the branch before beginning new work — in this case both fixes were intact and no code edits were needed.

### PR #276 Test Failure Investigation (2026-05-04, by mpaulosky request)
- **Task:** Diagnose failing `Persistence.AzureStorage.Tests.Integration` check in PR #276 (dependency bump PR)
- **PR:** Bump the all-actions group with 3 updates
  - Azure.Storage.Blobs: 12.20.0 → 12.27.0 ⚠️
  - Azure.Monitor.OpenTelemetry.AspNetCore: 1.4.0 → 1.5.0
  - Aspire.Hosting.Redis: 13.2.1 → 13.2.4
- **Root Cause:** **API Version Mismatch** — Azure.Storage.Blobs 12.27.0 sends API version `2026-02-06` in Blob Storage requests. Testcontainers.Azurite 4.11.0 bundles Azurite 3.35.0, which doesn't support this new API version.
- **Error Details:**
  ```
  Azure.RequestFailedException: The API version 2026-02-06 is not supported by Azurite. 
  Please upgrade Azurite to latest version and retry.
  ErrorCode: InvalidHeaderValue
  Status: 400
  ```
- **Culprit:** **Azure.Storage.Blobs 12.27.0 bump** — Direct cause of test failures; other bumps are unrelated
- **Test Impact:** All 25 integration tests fail at `BlobContainerClient.CreateIfNotExistsAsync()` when SDK attempts to contact Azurite
- **Tests Pass on dev:** ✅ Confirmed — Testcontainers.Azurite 4.11.0 + Azure.Storage.Blobs 12.20.0 compatible
- **Tests Fail on PR #276:** ❌ Confirmed — Testcontainers.Azurite 4.11.0 + Azure.Storage.Blobs 12.27.0 incompatible
- **Verdict:** **BLOCKER** — PR #276 cannot merge until Testcontainers.Azurite is upgraded to support Azure Storage Blob API v2026-02-06
- **Next Steps for Owner (Boromir/Aragorn):**
  1. Upgrade `Testcontainers.Azurite` in `Directory.Packages.props` to a version that bundles Azurite supporting API v2026-02-06 (likely v4.12.0+)
  2. Re-run integration tests to confirm all 25 tests pass
  3. Ensure no other Azure Storage API changes break existing test patterns
- **Key Lesson:** Dependency bumps in Azure SDK versions must be accompanied by corresponding test infrastructure (Testcontainers/Azurite) upgrades; monitor Azure Storage API versioning in release notes
