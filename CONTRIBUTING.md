# Contributing to IssueTrackerApp

Welcome, and thank you for contributing! This guide covers everything you need to get up and running — from installing prerequisites to understanding the pre-push gate that protects our main branch.

If you hit a snag at any point, open an issue or drop a question in the relevant PR. We'd rather answer questions up front than debug a broken pipeline later.

---

## Table of Contents

- [Prerequisites](#prerequisites)
- [Getting Started](#getting-started)
- [Branch Naming](#branch-naming)
- [Pre-Push Gate — Overview](#pre-push-gate--overview)
  - [Gate 0 — Branch Protection](#gate-0--branch-protection)
  - [Gate 1 — Untracked Source Files](#gate-1--untracked-source-files)
  - [Gate 2 — Release Build](#gate-2--release-build)
  - [Gate 3 — Unit Tests (no Docker)](#gate-3--unit-tests-no-docker)
  - [Gate 4 — Integration Tests + Playwright E2E (Docker required)](#gate-4--integration-tests--playwright-e2e-docker-required)
- [The Docker Requirement](#the-docker-requirement)
- [Running Tests Manually](#running-tests-manually)
- [Code Coverage](#code-coverage)
- [Code Conventions](#code-conventions)
- [PR Process](#pr-process)
- [Troubleshooting](#troubleshooting)

---

## Prerequisites

You need the following tools installed before cloning the repo.

| Tool | Minimum version | Notes |
|---|---|---|
| [.NET SDK](https://dotnet.microsoft.com/download) | 10.0 | Check with `dotnet --version` |
| [Docker Desktop](https://www.docker.com/products/docker-desktop) | Latest | **Must be running** when you push |
| [Node.js](https://nodejs.org/) | 20 LTS | Required for Tailwind CSS (`npm run css:build`) |
| Git | Any modern version | Hook installation requires Bash |

> **Why Docker?** The integration tests use [Testcontainers](https://dotnet.testcontainers.org/) to spin up a `mongo:7.0` replica-set container and an Azurite Azure Storage emulator. The AppHost.Tests project boots the full Aspire application stack internally via `DistributedApplicationTestingBuilder` — all of that needs the Docker daemon running.

---

## Getting Started

### 1. Clone the repo

```bash
git clone https://github.com/mpaulosky/IssueTrackerApp.git
cd IssueTrackerApp
```

### 2. Install the Git hook

The pre-push gate is a Bash script stored in `.github/hooks/pre-push`. Run the installer once after cloning to activate it:

```bash
bash scripts/install-hooks.sh
```

The installer is idempotent — it is safe to run multiple times and will update the hook if the source has changed.

### 3. Restore dependencies

```bash
dotnet restore IssueTrackerApp.slnx
```

### 4. Build in Release mode

```bash
dotnet build IssueTrackerApp.slnx --configuration Release
```

> **Why Release?** The project sets `TreatWarningsAsErrors=true`. A Debug build may succeed while a Release build fails on a warning promoted to an error. The pre-push gate always uses `--configuration Release` to match CI exactly.

### 5. Run the app locally

The app is orchestrated by .NET Aspire. Docker must be running (Aspire starts MongoDB and Redis containers):

```bash
dotnet run --project src/AppHost/AppHost.csproj
```

Aspire opens a dashboard in your browser showing all services, logs, and traces.

### 6. Tailwind CSS (UI changes only)

If you are changing Blazor components, watch for Tailwind class changes:

```bash
cd src/Web
npm install        # first time only
npm run css:watch  # rebuilds on file save
```

---

## Branch Naming

All work **must** happen on a dedicated branch. Direct pushes to `main` are blocked at Gate 0.

Use the squad branch convention:

```
squad/{issue-number}-{kebab-case-slug}
```

Examples:

```
squad/42-fix-login-validation
squad/112-contributing-pre-push-docs
squad/87-add-comment-pagination
```

Create your branch from the latest `main`:

```bash
git checkout main && git pull origin main
git checkout -b squad/{issue-number}-{your-slug}
```

---

## Pre-Push Gate — Overview

When you run `git push`, a Bash hook fires and runs four sequential gates before the push reaches GitHub. **All four gates must pass.** If any gate fails, the push is blocked and the hook explains what went wrong.

The hook lives at `.git/hooks/pre-push` (installed from `.github/hooks/pre-push` via `scripts/install-hooks.sh`).

```
┌─────────────────────────────────────────────────────────────────────┐
│  git push → pre-push hook                                           │
│                                                                     │
│  Gate 0  Branch protection     (hard block on main)                 │
│  Gate 1  Untracked source files (warn + prompt)                     │
│  Gate 2  Release build          (hard block on failure)             │
│  Gate 3  Unit tests             (hard block on failure)             │
│  Gate 4  Integration + E2E      (hard block; Docker required)       │
│                                                                     │
│  ✅ All pass → push proceeds                                        │
└─────────────────────────────────────────────────────────────────────┘
```

Each gate that fails gives you an opportunity to fix the problem and retry (up to 3 attempts) rather than forcing you to re-run the entire hook from scratch.

---

### Gate 0 — Branch Protection

**What it does:** Checks that `HEAD` is not `main`. If you are on `main`, the push is immediately blocked.

**Why:** Direct commits to `main` bypass code review and CI. Every change must go through a PR.

**Fix:** Create a feature branch (`squad/{issue}-{slug}`) and cherry-pick or re-commit your work there.

---

### Gate 1 — Untracked Source Files

**What it does:** Runs `git ls-files --others --exclude-standard` filtered to `*.razor` and `*.cs` files. If any are found, it prints the list and prompts you to confirm.

**Why:** An untracked file is invisible to CI — the build will succeed on CI even though the file exists locally. This often surfaces when you create a new file and forget to `git add` it.

**Your options:**

- Press `N` (or Enter) to abort, then `git add` the file(s) and re-push.
- Press `Y` to push anyway if the file is intentionally excluded (e.g., a local scratch file that belongs in `.gitignore`).

---

### Gate 2 — Release Build

**What it does:** Runs:

```bash
dotnet build IssueTrackerApp.slnx --configuration Release
```

The hook makes up to **3 attempts** total. After each failed attempt, it pauses and asks you to fix the errors and press Enter to retry, or Ctrl+C to abort.

**Why:** The project uses `TreatWarningsAsErrors=true`. A Debug build may hide warnings that break CI. Building in Release mode mirrors the CI pipeline exactly.

**Fix checklist:**

- Read the MSBuild output carefully — errors include file paths and line numbers.
- Check for nullable reference warnings (`CS8600`, `CS8602`, etc.) that are treated as errors in Release.
- After fixing, press Enter to retry without restarting the entire push.

---

### Gate 3 — Unit Tests (no Docker)

**What it does:** Runs the following test projects with `--configuration Release --no-build`:

| Project | What it covers |
|---|---|
| `tests/Architecture.Tests` | Layer boundaries, naming conventions, CQRS structure |
| `tests/Domain.Tests` | Domain logic, MediatR handlers, validators |
| `tests/Web.Tests.Bunit` | Blazor component rendering (bUnit) |
| `tests/Persistence.MongoDb.Tests` | MongoDB repository unit tests (mocked) |
| `tests/Web.Tests` | Web layer unit tests |
| `tests/Persistence.AzureStorage.Tests` | Azure Storage unit tests (mocked) |

These tests do **not** require Docker. They run against in-memory/mocked dependencies.

The hook makes up to **3 attempts**. After a failure, fix the broken tests and press Enter to retry.

**Why run architecture tests on every push?** `Architecture.Tests` enforces that commands end in `Command`, queries in `Query`, handlers are `sealed`, and `Domain` has no dependency on `Web` or `Persistence.*`. These rules prevent structural drift that is very hard to reverse later.

---

### Gate 4 — Integration Tests + Playwright E2E (Docker required)

**What it does:** First checks that Docker is running (`docker info`). If Docker is not running, **the push is hard-blocked** — you cannot skip this gate.

Once Docker is confirmed, the hook runs:

| Project | What it covers |
|---|---|
| `tests/Persistence.MongoDb.Tests.Integration` | Real MongoDB via Testcontainers (`mongo:7.0`) |
| `tests/Web.Tests.Integration` | Full web layer with in-memory server |
| `tests/Persistence.AzureStorage.Tests.Integration` | Real Azure Storage via Azurite (Testcontainers) |
| `tests/AppHost.Tests` | **Aspire integration + Playwright E2E** — boots the full app stack |

> ⚠️ **AppHost.Tests is mandatory.** This project uses `DistributedApplicationTestingBuilder` to boot the entire Aspire application internally — all services, including MongoDB (via Atlas connection string from User Secrets) and Redis. It then runs Playwright E2E tests against the live app.
>
> **Matthew Paulosky directive (2026-03-30): AppHost.Tests MUST pass locally before every push. No exceptions.** If you cannot make it pass, do not push — open a draft PR and ask the team for help instead.

The hook makes up to **3 attempts** for the integration suite. AppHost.Tests can take a few minutes because it is starting a real application stack, so be patient.

---

## The Docker Requirement

Docker Desktop must be **running** before you push. This is a hard requirement, not a suggestion.

**What requires Docker:**

- `tests/Persistence.MongoDb.Tests.Integration` — spins up `mongo:7.0` with replica set `rs0`
- `tests/Persistence.AzureStorage.Tests.Integration` — spins up Azurite
- `tests/AppHost.Tests` — boots the full Aspire stack (which starts Redis; MongoDB uses Atlas via User Secrets)

**Checking Docker status:**

```bash
docker info
# Should print engine info. If it says "Cannot connect to Docker daemon", Docker is not running.
```

**Starting Docker:**

- macOS / Windows: Open Docker Desktop from your Applications or Start Menu.
- Linux: `sudo systemctl start docker`

---

## Running Tests Manually

You do not need to wait for the pre-push hook to discover test failures. Run any test project directly:

### Single project

```bash
dotnet test tests/Architecture.Tests/Architecture.Tests.csproj --configuration Release
```

### All Gate 3 unit tests

```bash
dotnet test tests/Architecture.Tests/Architecture.Tests.csproj --configuration Release
dotnet test tests/Domain.Tests/Domain.Tests.csproj --configuration Release
dotnet test tests/Web.Tests.Bunit/Web.Tests.Bunit.csproj --configuration Release
dotnet test tests/Persistence.MongoDb.Tests/Persistence.MongoDb.Tests.csproj --configuration Release
dotnet test tests/Web.Tests/Web.Tests.csproj --configuration Release
dotnet test tests/Persistence.AzureStorage.Tests/Persistence.AzureStorage.Tests.csproj --configuration Release
```

### All Gate 4 integration tests (Docker required)

```bash
dotnet test tests/Persistence.MongoDb.Tests.Integration/Persistence.MongoDb.Tests.Integration.csproj --configuration Release
dotnet test tests/Web.Tests.Integration/Web.Tests.Integration.csproj --configuration Release
dotnet test tests/Persistence.AzureStorage.Tests.Integration/Persistence.AzureStorage.Tests.Integration.csproj --configuration Release
dotnet test tests/AppHost.Tests/AppHost.Tests.csproj --configuration Release
```

### Full solution test run

```bash
dotnet test IssueTrackerApp.slnx --configuration Release
```

> Note: The full solution run includes integration tests and therefore requires Docker.

---

## Code Coverage

The CI pipeline enforces an **80% line coverage gate** via the `Coverage Analysis` job in `.github/workflows/squad-test.yml`. PRs that drop below this threshold will fail CI and cannot be merged.

Coverage is collected with [coverlet](https://github.com/coverlet-coverage/coverlet) (`coverlet.collector` v8.0.0) and merged into a single Cobertura report by [ReportGenerator](https://reportgenerator.io/). Results are also published to [Codecov](https://codecov.io/gh/mpaulosky/IssueTrackerApp) — see the badges at the top of `README.md`.

### Running coverage locally

**Step 1 — collect coverage:**

```bash
dotnet test IssueTrackerApp.slnx \
  --collect:"XPlat Code Coverage" \
  --results-directory ./coverage-results
```

**Step 2 — generate an HTML report:**

```bash
dotnet tool run reportgenerator \
  -reports:"coverage-results/**/coverage.cobertura.xml" \
  -targetdir:"coverage-report" \
  -reporttypes:Html
```

Then open `coverage-report/index.html` in your browser.

> If `dotnet tool run reportgenerator` fails, install it globally first:
> ```bash
> dotnet tool install -g dotnet-reportgenerator-globaltool
> ```

### What counts toward coverage

Only source projects under `src/` are measured. The following test projects all contribute coverage data:

| Test project | What it covers |
|---|---|
| `tests/Architecture.Tests` | Layer boundary and naming-convention checks |
| `tests/Domain.Tests` | Domain command/query handlers and validators |
| `tests/Web.Tests` | Web service unit tests (mocked dependencies) |
| `tests/Web.Tests.Bunit` | Blazor component rendering (bUnit) |
| `tests/Web.Tests.Integration` | API endpoints and SignalR (requires Docker) |
| `tests/Persistence.MongoDb.Tests` | MongoDB repository unit tests (mocked) |
| `tests/Persistence.MongoDb.Tests.Integration` | Repository integration tests (requires Docker) |

Test projects themselves, generated code, and `obj/` directories are excluded automatically by coverlet.

### The 80% threshold

The `Coverage Analysis` CI job reads `Summary.json` produced by ReportGenerator and compares `linecoverage` against `80`. If the value is below 80, the job exits with an error and the PR cannot be merged:

```
::error::Code coverage is below 80% threshold: 74.3% (required: 80%)
```

When coverage passes the gate you will see:

```
::notice::Coverage gate passed: 83.1% >= 80%
```

### The coverage badge

The `CodeCov Coverage` badge in `README.md` is served by Codecov and reflects the most recent successful merge to `main`. Click the badge to see the full Codecov dashboard with per-file breakdown and trend graphs.

### Adding new tests

Use the correct test project for each test type:

- **`tests/Domain.Tests/`** — unit tests for domain command/query handlers
- **`tests/Web.Tests/`** — unit tests for web services with mocked dependencies
- **`tests/Web.Tests.Bunit/`** — Blazor component tests with bUnit
- **`tests/Web.Tests.Integration/`** — API endpoint and SignalR integration tests (requires Docker)
- **`tests/Persistence.MongoDb.Tests.Integration/`** — repository integration tests against real MongoDB (requires Docker)

---

## Code Conventions

### Language and framework

- **C# 13** — use the latest language features: primary constructors, collection expressions, pattern matching.
- **Blazor Interactive Server** — components live under `src/Web/Components/`.
- **CQRS with MediatR** — every feature is a vertical slice under `src/Domain/Features/{Feature}/`. Commands go in `Commands/`, queries in `Queries/`.

### Naming

| Kind | Convention | Example |
|---|---|---|
| Commands | Suffix `Command` | `CreateIssueCommand` |
| Queries | Suffix `Query` | `GetIssueByIdQuery` |
| Handlers | Suffix `Handler`, mark `sealed` | `sealed class CreateIssueCommandHandler` |
| Validators | Suffix `Validator` | `CreateIssueCommandValidator` |
| Repositories | Implement `IRepository<T>` | — |

### Error handling

Use `Result<T>` / `ResultErrorCode` for expected failures — do not use exceptions for control flow. The `Result<T>` abstraction lives in `src/Domain/Abstractions/`.

### No warnings in Release builds

The project treats all warnings as errors in Release configuration. Fix every warning before pushing — there are no exceptions.

### Copyright headers

Every `.cs` file must start with:

```csharp
// Copyright (c) 2026. All rights reserved.
```

### XML documentation

All `public` types and members require a `<summary>` XML doc comment:

```csharp
/// <summary>
/// Creates a new issue in the tracker.
/// </summary>
```

---

## PR Process

1. **Push your branch** — the pre-push gate runs automatically.

   ```bash
   git push origin squad/{issue-number}-{slug}
   ```

2. **Open a PR** targeting `main`. Reference the issue in the body:

   ```
   Closes #{issue-number}
   ```

3. **If you worked as a squad member**, note it in the PR description:

   ```
   Working as Frodo (Tech Writer)
   ```

4. **CI must be green** before the PR is reviewed. If CI fails, fix it before requesting review.

5. **Review sequence:**
   - **Aragorn** (Lead) always reviews.
   - Domain specialists are added depending on which files changed (Legolas for UI, Sam for backend, Gimli/Pippin for tests, Boromir for CI/CD, Gandalf for security).

6. **Merge method:** Squash merge (`gh pr merge {N} --squash --delete-branch`) after all checks pass and approval is received.

---

## Troubleshooting

### Build fails on warnings in Release but not Debug

Release mode promotes warnings to errors (`TreatWarningsAsErrors=true`). Common culprits:

- **CS8600 / CS8602** — nullable reference: add `?` annotations or null checks.
- **CS0618** — obsolete API: update to the recommended replacement shown in the warning message.
- **IDE analysers** — check `.editorconfig` for rules that become errors in Release.

### Docker is not running (Gate 4 hard block)

Start Docker Desktop (or `sudo systemctl start docker` on Linux), then re-run your push.

### AppHost.Tests times out or hangs

AppHost.Tests boots the full Aspire stack. If the app does not become healthy within the timeout:

- Confirm Docker is running and has enough resources (at least 4 GB RAM allocated).
- Check that your MongoDB Atlas connection string is in User Secrets (`dotnet user-secrets list --project src/Web`).
- Look at Aspire DCP logs for startup errors; the test output includes them on failure.
- If a port conflict exists (default port 7043), close any other process using that port.

### AppHost.Tests Playwright tests fail

- Playwright browsers are downloaded automatically on first run. If that download is interrupted, delete `tests/AppHost.Tests/bin/` and rebuild.
- Some tests depend on Auth0 test credentials. Confirm `Auth0:Domain`, `Auth0:ClientId`, and `Auth0:ClientSecret` are present in User Secrets.

### AppHost.Tests Playwright tests are intermittently flaky

Playwright E2E tests run against a live Aspire application and can exhibit timing-related failures when the full suite runs in parallel. A test that fails on one run may pass on the next. If you see a single test failure in AppHost.Tests:

1. Re-run the test project in isolation to confirm it passes:
   ```bash
   dotnet test tests/AppHost.Tests/AppHost.Tests.csproj --configuration Release --no-build
   ```
2. If the same test fails consistently in isolation (not just in the full suite), investigate the failure — it likely points to a real regression.
3. If the test only fails in the full suite run, it is a known flakiness issue. Re-run AppHost.Tests once more. If it passes, proceed with your push.
4. If you cannot get AppHost.Tests to pass after three full-suite runs, open a draft PR, tag it `squad:pippin`, and ask for help — do not push a broken suite.

### `git push` never triggers the hook

The hook is not installed. Run:

```bash
bash scripts/install-hooks.sh
```

Then verify:

```bash
ls -la .git/hooks/pre-push
```

The file should be executable (`-rwxrwxr-x` or similar).

### Hook prompt does not appear (no interactive terminal)

The hook reads prompts from `/dev/tty` so that it works even when stdin is a pipe (as Git uses). If you are running inside a tool that suppresses TTY, you may not see prompts — in that case push from a standard terminal.

---

*Documentation maintained by Frodo (Tech Writer). If something here is out of date, open an issue with the `squad:frodo` label.*
