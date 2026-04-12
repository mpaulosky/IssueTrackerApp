# Pippin — History

## Project Context
- **Project:** IssueTrackerApp
- **Stack:** .NET 10, C# 14, Blazor Interactive Server Rendering, MongoDB Atlas, Redis, .NET Aspire, MediatR, Auth0, Vertical Slice Architecture
- **User:** Matthew Paulosky
- **Repo:** mpaulosky/IssueTrackerApp
- **Joined:** 2026-03-27 — hired to assist Gimli with PR #76 (AppHost.Tests Aspire + Playwright E2E)

## My Domain
I own E2E tests (`tests/AppHost.Tests/`) and Aspire integration test infrastructure. Gimli owns unit, bUnit, and MongoDB integration tests.

## Key File Paths
- `tests/AppHost.Tests/` — my primary workspace
- `tests/AppHost.Tests/Infrastructure/` — BasePlaywrightTests, AspireManager, PlaywrightManager, AppHostTestCollection
- `tests/AppHost.Tests/Tests/` — all E2E test classes
- `src/Web/Program.cs` — Testing environment: cookie auth + FakeRepository, background services skipped, GET /test/login?role=user|admin
- `src/Web/Testing/FakeRepository.cs` — in-memory repo for Testing environment
- `src/Web/Testing/FakeSeedData.cs` — seed data for Testing environment

## Key Decisions & Patterns
- Cookie auth via `/test/login?role=user|admin` — no real Auth0 needed in E2E tests
- `EnvironmentCallbackAnnotation` to inject `ASPNETCORE_ENVIRONMENT=Testing` into Aspire DCP (SetEnvironmentVariable alone is insufficient)
- `WaitForWebReadyAsync` (HTTP poll with DangerousAcceptAnyServerCertificateValidator) instead of `WaitForResourceHealthyAsync` — CI self-signed cert issue
- Fixed HTTPS port 7043 with `IsProxied = false` for predictable base URL
- `DisableDashboard = true` always in test Aspire builder — no overhead in CI
- Playwright tests wait for ThemeProvider init via button title or swatch scale-110 class — not just NetworkIdle
- `List<IBrowserContext>` pattern for context tracking — never a single field that gets overwritten

## Core Context: Historical Learnings

### Aspire Test Startup Health Check Fix (2026-03-28 | PR #86)
- Fixed flaky CI failures: `web_https_/health_200_check` and `redis_check` timeouts
- Root cause: `AspireManager.StartAppAsync()` returned without waiting for Redis and Web to become healthy
- Solution: Added `WaitForWebHealthyAsync()` polling `/health` endpoint with cert-ignoring HttpClient
- 120-second timeout accommodates CI cold-start; local dev ~10s
- Key insights: Aspire DCP timing, health check strategy, dependency chains matter
- Test results: 38/40 passing; 2 UI timing failures unrelated to infrastructure

### PR #76 Review: AppHost.Tests Aspire Integration + Playwright E2E (2026-03-28)
- Verdict: APPROVED
- 37 changed files (18 new C#, test infrastructure, Program.cs, CI)
- All 18 new files carry required copyright block ✅
- xUnit collection structure correct with `BasePlaywrightTests` inheritance ✅
- AspireManager lifecycle: chains `PlaywrightManager.InitializeAsync()` + `StartAppAsync()` ✅
- Testing-environment seam (cookie auth, fake repos, skipped services) correct ✅
- `EnvironmentCallbackAnnotation` sophisticated and correct ✅
- Fixed HTTPS port 7043 with `IsProxied = false` ✅

### Gimli Blocking Issues Resolution (2026-03-28)
Resolved 6 blocking issues:
1. False "skip gracefully" docs — Removed misleading comments, rewrote docstrings
2. `InteractWithPageAsync` visibility — Changed from public to protected
3. `IBrowserContext` leak — Replaced single field with `List<IBrowserContext>` and proper disposal
4. Fragile redirect assertion — Changed `NotContain("/admin")` to `Contain("/Account/AccessDenied")`
5. Missing EOF newline — Fixed in `EnvVarTests.cs`
6. `DisableDashboard = false → true` — Disable in tests for resource efficiency

### Theme System localStorage Key Conflict (2026-03-29)
- Task: Updated theme tests to match new `tailwind-color-theme` localStorage key
- Found dual theme systems: old `window.themeManager` (lowercase) + new `window.ThemeManager` (uppercase)
- localStorage key mismatch is common theme integration bug
- Multiple theme systems can coexist, but tests must target the one components actually use
- `data-theme-ready` still set correctly by ThemeProvider
- Production issue: Old `ThemeProvider` writes to `theme-color-brightness` via `themeManager.*`; new components write to `tailwind-color-theme` via `ThemeManager.*`
- Aragorn needs to either update new components to old `themeManager.*` or remove `ThemeProvider` and migrate fully

### Team Rule: AppHost.Tests Mandatory Pre-Push (2026-03-30)
- Enforced by Matthew Paulosky
- Rule: AppHost.Tests MUST run locally before every push
- Gate 4 now includes mandatory AppHost.Tests check
- Pippin validates E2E tests locally before marking test fixes complete
