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
