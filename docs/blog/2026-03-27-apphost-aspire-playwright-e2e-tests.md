---
title: "Adding AppHost.Tests — Aspire Integration + Playwright E2E Tests"
date: 2026-03-27
author: Matthew Paulosky
tags: [tests, aspire, playwright, e2e, architecture, ci-cd]
summary: "Introducing comprehensive E2E and integration tests with Aspire and Playwright, boosting end-to-end confidence without Auth0 dependencies."
---

## Summary

PR #76 brought a complete test automation story to IssueTrackerApp: the new `AppHost.Tests` project includes 3 Aspire integration tests for health checks and environment validation, plus 29 Playwright E2E tests across 8 files. Together, they validate the full stack—from AppHost orchestration through Blazor UI—without depending on external Auth0 infrastructure during test runs.

## Context

Before this work, we had solid unit and integration tests but no true end-to-end automation in the browser. That's a gap—integration tests run against APIs, but they don't catch UI bugs, JavaScript interop issues, or the full request/response cycle through a rendered Blazor page. We needed confidence that a user could log in, navigate, and interact with the app without hitting unexpected errors.

The challenge: Auth0 is great for production, but it's not great for tests. Mocking it or replaying tokens isn't reliable. So we built a **testing auth seam**: a `/test/login` endpoint that bypasses Auth0 during test runs, allowing Playwright to authenticate without external dependencies or rate limits.

## Key Architecture Decisions

### 1. Cookie-Based Auth for Tests

The test environment uses **local cookie authentication** instead of Auth0:

```csharp
// From AuthStateManager.cs – one-time Auth0 login (production)
// But tests use a simpler path via /test/login
```

Tests authenticate by POST-ing to `/test/login` with test credentials, receive a session cookie, and proceed. No Auth0 tenant, no PKCE flow, no redirects—just a direct auth response.

### 2. `EnvironmentCallbackAnnotation` for ASPNETCORE_ENVIRONMENT

Aspire integration tests use a custom annotation to inject `ASPNETCORE_ENVIRONMENT=Testing` into the AppHost:

```csharp
[Aspire]
[EnvironmentCallbackAnnotation("ASPNETCORE_ENVIRONMENT", "Testing")]
public async Task WebServerIsHealthy()
{
    // AppHost runs with Environment == "Testing"
    // Seeding, Auth0, and other startup checks skip gracefully
}
```

This avoids needing to mock or configure Auth0 when the tests run.

### 3. Fixed Port + HTTPS Self-Signed Certs

The web service runs on a **fixed port (7043)** in tests. Playwright connects to `https://localhost:7043`. AppHost generates a self-signed certificate, and `WaitForWebReadyAsync` validates that the HTTPS endpoint is ready before tests run:

```csharp
var web = builder.AddProject<Web>("web")
    .WithHttpsPort(7043)
    .WaitForWebReadyAsync();
```

This ensures Playwright doesn't race the server startup.

### 4. Storage State Persistence

`AuthStateManager` runs Auth0 login once per test session, saves the authenticated storage state (cookies, localStorage) to a temp file, and injects it into all subsequent Playwright browser contexts. This avoids re-authenticating for every test:

```csharp
// First test: real Auth0 login
// Subsequent tests: reuse storage state from cache file
```

It gracefully skips when credentials (`PLAYWRIGHT_TEST_EMAIL`, `PLAYWRIGHT_TEST_PASSWORD`) are not set, making the tests CI-safe.

## Test Coverage

### Aspire Integration Tests (3 tests)
- **AppHostIntegrationTests.cs**: Health check (`/health`) endpoint responds
- **EnvVarTests.cs**: MongoDB and Redis connection strings are properly injected
- **IntegrationTests.cs**: HTTP health endpoint + home page load

### Playwright E2E Tests (29 tests across 8 files)

#### Layout & Navigation (10 tests)
- **LayoutAnonymousTests.cs** — 6 tests: brand visible, login link present, nav hidden, footer, theme selector, color scheme selector
- **LayoutAuthenticatedTests.cs** — 4 tests: nav links visible, footer, login link hidden from authenticated users

#### Pages (9 tests)
- **HomePageTests.cs** — 4 tests: guest heading, authenticated welcome-back message
- **DashboardPageTests.cs** — 3 tests: no redirect loop, heading, stat cards render
- **NotFoundPageTests.cs** — 2 tests: heading and helpful message

#### Issues & Features (6 tests)
- **IssueIndexPageTests.cs** — 2 tests: authenticated users see no redirect, page title
- **ThemeToggleTests.cs** — 4 tests: toggle visible, dropdown renders, dark class applied to body
- **ColorSchemeTests.cs** — 4 tests: color swatches visible, selection works, CSS class applied

## What's Next

Aragorn's review noted 3 non-blocking nits (see PR #76 comments):

1. **Test output clarity**: Add Playwright trace artifacts to CI logs for easier debugging
2. **Flaky test handling**: Add retry logic for timing-sensitive theme tests
3. **Auth coverage**: Consider end-to-end flow tests (login → create issue → comment → logout)

These are all good future improvements but don't block the current merge. The test foundation is solid and catches real issues.

---

**PR #76:** [feat(tests): AppHost.Tests — Aspire integration + Playwright E2E tests](https://github.com/mpaulosky/IssueTrackerApp/pull/76)
