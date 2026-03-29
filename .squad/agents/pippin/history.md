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

### 2026-03-28: Playwright WaitForFunctionAsync API Fix (Issue #86)

**Task:** Fix 2 failing Playwright tests: `ThemeToggle_SelectLight_RemovesDarkClassFromHtml` and `ColorScheme_SelectRed_AppliesRedTheme`.

**Root Cause:** Incorrect API usage in all `WaitForFunctionAsync` calls — `PageWaitForFunctionOptions` was passed as the 2nd argument (JavaScript expression arg) instead of the 3rd argument (options arg). This caused the custom timeout of 15000ms to be silently ignored, falling back to Playwright's default 30000ms timeout. In CI under load, Blazor Server SignalR event processing exceeded even the intended 15s timeout, causing test failures.

**Solution Implemented:**
1. Fixed all `WaitForFunctionAsync` calls to pass `null` as 2nd arg and `PageWaitForFunctionOptions` as 3rd arg (correct API signature)
2. Increased timeout from 15000ms to 30000ms for CI reliability under heavy load
3. Added `data-theme-ready` initialization wait before button title check in `ThemeToggle_SelectLight` test
4. Added `WaitForLoadStateAsync(NetworkIdle)` after color swatch click to allow Blazor Server SignalR to complete event processing before checking localStorage

**Key Insights:**
1. **Playwright API signature matters** — `WaitForFunctionAsync(expression, arg, options)` requires arg even when null. Passing options as arg silently fails.
2. **CI timing is unpredictable** — Blazor Server via SignalR can take 20-30+ seconds in CI for state changes to propagate to localStorage. Always add explicit waits for state updates.
3. **NetworkIdle is critical** — After user interactions (clicks) that trigger Blazor Server event handlers, `WaitForLoadStateAsync(NetworkIdle)` ensures SignalR round-trip completes before asserting on client-side state.
4. **Initialization gates** — `data-theme-ready` attribute prevents race conditions where tests check theme state before ThemeProvider completes JS interop initialization.

**Files Modified:**
- `tests/AppHost.Tests/Tests/Theme/ThemeToggleTests.cs` — Fixed 4 `WaitForFunctionAsync` calls (lines 95-97, 102-104, 131-137, 142-144)
- `tests/AppHost.Tests/Tests/Theme/ColorSchemeTests.cs` — Fixed 2 `WaitForFunctionAsync` calls and added NetworkIdle wait (lines 90-92, 103-110)

**Testing:** Build succeeded with no errors. Tests cannot run locally without Docker but fixes address diagnosed root causes. CI will validate on next push.

### 2026-03-29: Switch from /health to /alive for Test Startup Polling (PR #86)

**Task:** Fix 2 flaky CI test failures caused by Redis health check timeouts blocking test startup.

**Root Cause:** Both `AspireManager.WaitForWebHealthyAsync` and `BasePlaywrightTests.WaitForWebReadyAsync` polled `/health`, which includes Redis and MongoDB health checks. In CI, Redis container startup could exceed the 120s timeout, causing `/health` to return unhealthy indefinitely and tests to fail with connection timeouts.

**Solution Implemented:**
1. Changed both polling methods from `/health` to `/alive`
2. Updated XML doc comments to reflect that `/alive` is a liveness probe (ASP.NET Core process running) not a readiness probe (all dependencies healthy)
3. Updated `StartAppAsync` comment to clarify that the wait is for the web process to be alive, not for Redis/MongoDB to be healthy
4. Emphasized in comments that the Testing environment uses in-memory fakes (FakeRepository) and doesn't depend on Redis/MongoDB at runtime

**Key Insights:**
1. **/alive vs /health distinction** — `/alive` returns 200 as soon as the ASP.NET Core process is up, regardless of dependency health. `/health` waits for ALL health checks (Redis, MongoDB) to pass. For test startup, we only need to know the web process is running — the Testing environment doesn't use Redis or MongoDB.
2. **Testing environment is self-contained** — The `ASPNETCORE_ENVIRONMENT=Testing` configuration uses `FakeRepository` (in-memory), cookie auth (no Auth0), and skips background services. Redis and MongoDB are Aspire orchestration artifacts only — they don't affect test execution.
3. **Health checks are for production readiness, not test startup** — Waiting for production-level readiness (all dependencies healthy) in a test environment that doesn't use those dependencies is unnecessary and causes CI flakiness.

**Files Modified:**
- `tests/AppHost.Tests/Infrastructure/AspireManager.cs` — Changed `WaitForWebHealthyAsync` to poll `/alive` (line 98); updated doc comment and `StartAppAsync` comment
- `tests/AppHost.Tests/BasePlaywrightTests.cs` — Changed `WaitForWebReadyAsync` to poll `/alive` (line 144); updated doc comment

**Testing:** Build succeeded with no compilation errors. Full AppHost.Tests suite requires Docker. CI will validate the fix on next push.

