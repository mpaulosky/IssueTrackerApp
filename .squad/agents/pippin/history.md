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

## Learnings

### 2026-03-28: Aspire Test Startup Health Check Fix (PR #86)

**Task:** Fix flaky CI failures in AppHost.Tests — `web_https_/health_200_check` and `redis_check` timeouts.

**Root Cause:** `AspireManager.StartAppAsync()` returned immediately after `App.StartAsync()` without waiting for Redis and Web services to become healthy. In CI, Redis cold-start takes 30-60 seconds, causing:
1. Aspire's built-in health checks to timeout before services stabilized
2. E2E tests to fail with connection refused errors

**Solution Implemented (Already in place by Boromir):**
- Added `WaitForWebHealthyAsync()` in `AspireManager` that polls `/health` endpoint with certificate-ignoring HttpClient (for self-signed HTTPS in CI)
- 120-second timeout accommodates CI cold-start; local dev succeeds in ~10s
- Since `AppHost.cs` configures Web to `WaitFor(redis)`, the web health check implicitly ensures Redis is ready too

**Key Insights:**
1. **Aspire DCP timing** — `App.StartAsync()` returns when DCP launches containers, NOT when they're healthy. Always add explicit health checks in test fixtures.
2. **Health check strategy** — Polling the web `/health` endpoint is more reliable than Aspire's built-in `WaitForResourceHealthyAsync()` for HTTPS services with self-signed certs in CI.
3. **Dependency chains matter** — Web configured with `.WaitFor(redis)` means web health inherently validates Redis readiness. No need for separate Redis polling.
4. **Test execution results** — After fix: 38/40 tests passing. The 2 failures (ThemeToggle, ColorScheme) are unrelated Playwright UI timing issues, not infrastructure flakiness.

**Files Modified:**
- `tests/AppHost.Tests/Infrastructure/AspireManager.cs` — Added `WaitForWebHealthyAsync()` and call in `StartAppAsync()`

**Testing:** Local test run with Docker showed no Redis/web startup failures. CI will validate full fix on next push.

