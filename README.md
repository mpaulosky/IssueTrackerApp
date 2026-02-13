---
post_title: IssueTrackerApp
author1: Matthew Paulosky
post_slug: issuetrackerapp
microsoft_alias: mpaulosky
featured_image: ./src/Web/wwwroot/images/IssuePageProfile.png
categories:
  - dotnet
  - aspire
  - mongodb
tags:
  - blazor
  - redis
  - auth0
  - testcontainers
ai_note: Drafted with GPT-5.1-Codex and reviewed by maintainers.
summary: >-
  Cloud-ready reference implementation of a MongoDB-backed issue tracker built with .NET 10 Aspire,
  Blazor server-side components, and Redis output caching.
post_date: 2026-02-13
---

<div align="center">
  <img src="./src/Web/wwwroot/favicon.png" alt="IssueTrackerApp logo" width="96" />
  <p><strong>Cloud-ready Blazor + MongoDB issue tracking playground orchestrated with .NET Aspire.</strong></p>
  <a href="https://github.com/mpaulosky/IssueTrackerApp/actions/workflows/build-and-test.yml">
    <img src="https://img.shields.io/github/actions/workflow/status/mpaulosky/IssueTrackerApp/build-and-test.yml?style=flat-square" alt="Build status" />
  </a>
  <a href="https://dotnet.microsoft.com/en-us/download/dotnet/10.0">
    <img src="https://img.shields.io/badge/.NET-10.0-512BD4?style=flat-square" alt=".NET 10" />
  </a>
  <a href="./LICENSE.txt">
    <img src="https://img.shields.io/badge/License-MIT-yellow?style=flat-square" alt="MIT license" />
  </a>
</div>

## Project at a glance

IssueTrackerApp is a full-stack reference solution that showcases how to build a vertical-slice issue
management system with MongoDB persistence, Redis-backed caching, and Blazor Interactive Server UI that
is orchestrated through .NET Aspire. The repository doubles as a playground for experimenting with
service discovery, OpenTelemetry, Scalar-based API docs, and GitHub Actions quality gates.

- **Distributed by default** via `src/AppHost`, which wires the Web front end, the API, Redis, MongoDB,
  and Mongo Express into a single Aspire dashboard.
- **Domain-centric design** powered by the `Shared` project, which contains the aggregates, DTOs, and
  guard-clause rich abstractions for categories, issues, statuses, comments, and users.
- **MongoDB-first data access** with native `MongoDB.Driver`, context factories, and per-feature
  repositories backed by in-memory caching.
- **Observability and resiliency** built into every service through `ServiceDefaults` (OpenTelemetry,
  service discovery, health checks, antiforgery, and structured logging).
- **CI-ready template** with automated formatting, StyleCop analysis, unit/integration/architecture
  tests, Trivy vulnerability scans, and Codecov uploads.

## Architecture overview

IssueTrackerApp is organized as a collection of small projects that map 1:1 with deployment units in
Aspire. `AppHost` defines resources and lifecycles, `ServiceDefaults` provides cross-cutting concerns,
`ApiService` exposes REST endpoints and Scalar documentation, while `Web` hosts the Blazor experience.

```mermaid
flowchart LR
    Dev[(Developer)] -->|dotnet run src/AppHost| Aspire{{Aspire Host}}
    subgraph Aspire Stack
        Web[[Web (Blazor SSR)]]
        Api[(ApiService Minimal APIs)]
        Redis[(Redis Cache)]
        Mongo[(MongoDB)]
        MongoExpress[(Mongo Express UI)]
    end
    Web -->|Output cache| Redis
    Api -->|MongoDB.Driver| Mongo
    Api --> Redis
    Aspire --> MongoExpress
    Aspire --> Web
    Aspire --> Api
```

[!NOTE]
> The Aspire host pins the web front end to `http://localhost:5057` so Playwright/browser tests
> always target a predictable origin.

## Core capabilities

- **Vertical slices for every aggregate** – Each folder under `src/ApiService/Features` contains the
  repository and service pair for that entity, keeping MongoDB queries close to the handlers.
- **Caching everywhere** – `Web` relies on `Aspire.StackExchange.Redis.OutputCaching` while services use
  `IMemoryCache` to throttle hot queries (e.g., categories cached for a day, issues cached per-user).
- **ServiceDefaults boost** – Opinions baked into `.AddServiceDefaults()` provide OTLP-friendly tracing,
  resilience handlers for every `HttpClient`, health endpoints (`/health`, `/alive`), and antiforgery.
- **Operational helpers** – `AddRedisServices` registers a "Clear Cache" command, and `AddMongoDbServices`
  provisions Mongo Express alongside persistent volumes so you can inspect collections safely.
- **Security posture** – HTTPS redirection, antiforgery, Auth0 placeholders, and upcoming Scalar UI for
  OpenAPI ensure the API surface stays auditable.
- **Quality gates out of the box** – GitHub Actions enforce `dotnet format`, StyleCop, analyzer builds,
  per-project tests (with coverage), Trivy scans, and Codecov uploads before anything hits `main`.

## Tech stack

| Area | Technology | Highlights |
| --- | --- | --- |
| Runtime | .NET 10 SDK 10.0.103, C# 14 | Locked via `global.json` to keep tooling consistent. |
| UI | Blazor Web App (Interactive Server) | Server-side rendering with Razor Components, antiforgery, output caching. |
| API | Minimal APIs + Scalar (OpenAPI 3) | `ApiService` hosts REST endpoints, with Scalar UI planned on top of `/openapi`. |
| Data | MongoDB Driver + Mongo Express | Native driver, runtime `IMongoDbContextFactory`, Aspire-provisioned cluster + UI. |
| Caching | Aspire Redis + IMemoryCache | Redis output cache for responses, in-memory caches per domain service. |
| Auth | Auth0 (via configuration placeholders) | Configure `Auth0__*` secrets via User Secrets or Azure Key Vault. |
| Observability | OpenTelemetry + OTLP exporters | Enable OTLP by setting `OTEL_EXPORTER_OTLP_ENDPOINT`; Application Insights can be added later. |
| Testing | xUnit, bUnit, Testcontainers, Aspire.Hosting.Testing | Supports unit, integration, architecture, and Playwright-style tests. |

## Local development

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [Docker Desktop](https://www.docker.com/products/docker-desktop) (needed for Testcontainers and
  Aspire-managed Mongo/Redis containers)
- [MongoDB for VS Code extension](https://marketplace.visualstudio.com/items?itemName=mongodb.mongodb-vscode)
  for secure, click-to-run database exploration directly inside VS Code
- (Optional) [MongoDB Compass](https://www.mongodb.com/products/tools/compass) or the Mongo Express UI
  that Aspire wires automatically

### Run the distributed application

1. Restore workloads and dependencies:
   ```bash
   dotnet workload restore
   dotnet restore
   ```
2. Boot the entire stack through Aspire (this spins up the dashboard, MongoDB, Redis, Mongo Express,
   ApiService, and Web):
   ```bash
   dotnet run --project src/AppHost/AppHost.csproj
   ```
3. Watch the Aspire dashboard (opens automatically, typically at `http://localhost:18888`) to inspect
   logs, environment variables, and invoke resource commands such as **Clear Cache** for Redis.

[!TIP]
> Use the MongoDB for VS Code extension or Mongo Express instead of raw shell access when inspecting
> collections. It keeps audit trails cleaner and respects TLS defaults in managed clusters.

### Useful endpoints

| Service | Default URL | Notes |
| --- | --- | --- |
| Web (Blazor) | `http://localhost:5057` | Interactive server rendering with antiforgery enabled. |
| ApiService | Discover via Aspire dashboard | Minimal APIs plus `/openapi` endpoint for Scalar to render. |
| Mongo Express | Accessible from Aspire dashboard | Securely browse collections without exposing ports. |
| Health | `/health`, `/alive` on each service | Only mapped in `Development` builds for safety. |

## Configuration & secrets

| Setting | Where to configure | Purpose |
| --- | --- | --- |
| `MongoDb:ConnectionString` / `MongoDb:Database` | User Secrets, environment variables, or
  `appsettings.*` | Connection information for the Mongo cluster seeded by Aspire. |
| `Auth0__Domain`, `Auth0__ClientId`, `Auth0__ClientSecret`, `Auth0__Audience` | User Secrets / Key
  Vault | Required once authentication is turned on; CI uses placeholder secrets. |
| `OTEL_EXPORTER_OTLP_ENDPOINT` | Environment variable | Enables OTLP exports from `ServiceDefaults`. |
| `APPLICATIONINSIGHTS_CONNECTION_STRING` | Environment variable (optional) | Uncomment the Azure
  Monitor exporter in `ServiceDefaults` to push telemetry to Application Insights. |
| `MongoDb:ConnectionString` in tests | `tests` secrets or `Testcontainers` defaults | Integration tests
  spin up throwaway MongoDB instances automatically via Testcontainers. |

[!IMPORTANT]
> Store secrets with `dotnet user-secrets` locally and Azure Key Vault in hosted environments. Never
> commit `appsettings.Production.json` or connection strings to source control.

## Testing & quality gates

- **Unit tests** live under `tests/*Unit`, leveraging xUnit, FluentAssertions, and NSubstitute.
- **Integration tests** use Testcontainers for MongoDB and `Aspire.Hosting.Testing` to spin up full
  distributed environments (see `tests/AppHost.Tests.Integration`).
- **Architecture tests** codify layering rules via `tests/IssueTracker.Tests.Architecture`.
- **Blazor UI tests** rely on bUnit (and Playwright for end-to-end scenarios) to validate Razor
  components and interactivity.
- **CI pipeline** (`.github/workflows/build-and-test.yml`) performs formatting, analyzer builds,
  service builds, per-project tests with coverage, publishes artifacts, runs Trivy for vulnerabilities,
  and uploads coverage to Codecov.

Typical local test run:

```bash
dotnet test --configuration Release
```

[!NOTE]
> Architecture tests intentionally skip Coverlet instrumentation because NetArchTest and Coverlet
> conflict; see the workflow for details.

## Troubleshooting & tips

- **Redis cache feels stale?** Use the "Clear Cache" command from the Redis resource inside the Aspire
  dashboard; it invokes the custom command defined in `RedisService.cs`.
- **Need to inspect Mongo data?** Use Mongo Express (already provisioned) or the MongoDB for VS Code
  extension instead of direct shell sessions. You can safely switch between mock sets or seeds without
  restarting Aspire.
- **OpenAPI UI missing?** Ensure you are running in `Development` so `/openapi` is exposed, and wire in
  Scalar by following the architecture guidelines in `.github/instructions/copilot-instructions.md`.
- **Ports keep changing?** Aspire intentionally randomizes most service ports for isolation; pin only
  what automated tests require (currently just the Blazor site on port 5057).

## Additional resources

- `docs/CONTRIBUTING.md` – contribution workflow, coding standards, and branch policies
- `docs/SECURITY.md` – current security posture and reporting process
- `docs/REFERENCES.md` – curated list of frameworks, testing tools, and architectural references
- `.github/` – GitHub Actions workflow, Dependabot config, and Copilot guardrails
