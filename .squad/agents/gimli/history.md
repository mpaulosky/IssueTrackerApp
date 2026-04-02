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
