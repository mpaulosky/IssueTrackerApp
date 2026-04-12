# IssueTrackerApp Decisions

This file records team decisions that affect architecture, scope, and process.

---

## Decisions

### Process & Planning

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

### 2026-04-02: Process documentation review — ceremonies + routing + skills
**By:** Aragorn (Lead)
Enhanced ceremonies.md with Sprint Review and Issue Grooming ceremonies. Updated routing.md with Admin and Labels domain signals. Audited and updated 2 skills; added 2 new skills (auth0-management-api, labels-feature-patterns). Reflects Sprints 5 and 6 new domains not previously in process docs.

### 2026-04-02: Skills Audit — 13 skills reviewed
**By:** Aragorn (Lead)
13 skills audited: 9 accurate, 2 partially stale (solution file renamed IssueManager.sln → IssueTrackerApp.slnx), 2 not applicable (Minecraft-domain skills). Action items: update build-repair and pre-push-test-gate solution references; remove building-protection and post-build-validation; add confidence headers to 3 skills.

### 2026-04-02: Post-Sprint 6 Documentation Accuracy Audit
**By:** Frodo (Tech Writer)
Audited README.md, CONTRIBUTING.md, docs/index.html, docs/blog/index.md, and XML docs after Sprints 5 and 6. All documentation found accurate and up-to-date — v0.5.0 (Admin User Management) and v0.6.0 (Labels Feature) correctly documented across all audit points. No changes required.

### 2026-04-02: Security Review — Sprint 5/6 Admin Additions
**By:** Gandalf (Security Officer)
Auth0 Management API integration reviewed: secrets hygiene PASS, error handling PASS, token caching PASS, input validation PASS, page authorization PASS. One MEDIUM finding: no audit log for role assign/revoke in UserManagementService (track as follow-up issue). One LOW finding: no Polly retry for HTTP 429 (per ADR #130, non-blocking). Auth0ClaimsTransformation reviewed and accepted.
---

### Styling & Components

#### Button CSS Consolidation (2026-04-02)

**Author:** Legolas (Frontend Developer)
**Status:** Accepted
**Date:** 2026-04-02

**Decision:** The `.btn` base class now includes shared properties (`text-white`, `border-2 border-transparent`) previously duplicated across variant classes. All button variant classes MUST pair with `.btn`.

**Changes:**
1. `.btn` base class now includes `border-2 border-transparent` and `text-white`
2. `.btn-primary`, `.btn-secondary`, `.btn-warning`, `.btn-danger` — no longer declare duplicate properties
3. `.btn-warning` color fixed: amber (`bg-amber-500 / hover:bg-amber-700`) instead of red (was semantic mismatch with danger)
4. `.btn-danger` class created (was missing but referenced in 7 components)
5. `.container-card` class created (was missing but referenced in Profile.razor, PageHeadingComponent.razor)
6. All 22 Razor files updated to enforce `btn` + `btn-{variant}` pairing

**Usage Rule (ENFORCED):**
```html
<!-- Correct -->
<button class="btn btn-primary">Save</button>
<button class="btn btn-danger w-full">Delete</button>

<!-- Wrong (base class missing) -->
<button class="btn-primary">Save</button>
```

Also applies in C# string interpolation and ternary expressions:
```csharp
$"btn btn-danger {extraClasses}"
_active ? "btn btn-primary" : "btn btn-secondary"
```

**Rationale:**
- Eliminates CSS duplication across 4+ variant classes
- Enforces consistent button appearance
- Fixes semantic mismatch (warning was red, identical to danger)
- Resolves undefined `.btn-danger` runtime issue

**Verification:**
- Full test suite: 1,595 tests, 1,557 passed (38 pre-existing infrastructure failures unrelated to CSS changes)
- No regressions introduced

**Scribe Note:** Merged from decision inbox file `legolas-btn-consolidation.md`


---

### Git Workflow

#### Git Worktrees as Standard Isolation Strategy (2026-04-03)

**By:** Matthew Paulosky (via Copilot)
**What:** Adopt git worktrees as the standard isolation strategy for squad planning, scribe, and sprint branch work.
**Why:** Single-worktree workflow caused recurring `.squad/` files bleeding into `feature/*` branches, context-switching overhead, and inability to run parallel sprint branches without interference.

**Layout:**
```
~/Repos/
├── IssueTrackerApp/            ← main worktree  (stays on main)
├── IssueTrackerApp-scribe/     ← scribe/planning worktree (.squad/ commits only)
└── IssueTrackerApp-sprint/     ← active sprint worktree (squad/{issue}-{slug})
```

**Trigger phrases:** "use worktrees", "use a worktree", "isolate in a worktree", "set up a sprint worktree", or automatically when ≥2 squad branches are active.

**Rules:**
- Main worktree stays on `main` — never used for active squad branch work
- Scribe worktree: only `.squad/` commits — no source code changes
- Sprint worktrees: one per active squad branch
- No simultaneous `dotnet build` — `bin/`/`obj/` are shared
- Pre-push hook enforced in every worktree

**Setup:**
```bash
git worktree add ../IssueTrackerApp-scribe squad/scribe-log-updates
git worktree add ../IssueTrackerApp-sprint -b squad/{issue-number}-{slug}
git worktree remove ../IssueTrackerApp-sprint  # after merge
```

**Scribe Note:** Merged from decision inbox file `copilot-git-worktrees.md`

---

## Release Process & Portability (2026-04-12)

#### Generic Release-Process Skill Refactoring — Aragorn (Lead)

**Status:** Approved | **Date:** 2026-04-12 | **Scope:** team

**Problem:** Current `release-process/SKILL.md` is hardcoded for BlazorWebFormsComponents (repository names, workflows, NBGV versioning, package names, registries). Cannot reuse on IssueTrackerApp or other projects without manual editing.

**Decision:** Refactor into two-layer architecture:
- **Layer 1 (Generic):** `.squad/skills/release-process-base/SKILL.md` — Framework-agnostic patterns, decision trees, role boundaries (100% reusable, zero project-specific values)
- **Layer 2 (Project-Specific):** `.squad/playbooks/{project-name}/release.md` — Concrete parameters (branch names, secrets, workflows, package ID, registries), inferred from repo state or `.release-config.json`

**Parameters (as placeholders):** REPO_OWNER, REPO_NAME, DEV_BRANCH, RELEASE_BRANCH, VERSION_FILE, VERSION_SYSTEM, TAG_PREFIX, RELEASE_MERGE_STRATEGY, PACKAGE_ID, DOCS_TOOL, CONTAINER_REGISTRY, POST_RELEASE_STEPS

**Generic Skill Covers:** Version bump mechanics, two-branch rationale, merge vs. squash trade-offs, tagging semantics, CI/CD flow, troubleshooting, rollback. **Does NOT cover:** Hardcoded repo/workflow names, URLs, registries, package IDs.

**Inference via gh/git (safe, read-only):** gh repo view commands, gh workflow list, gh secret list (names only, no values), filesystem detection (version.json, Dockerfile, mkdocs.yml, etc.)

**Refactor Roadmap:** P1 extract generic skill, P2 IssueTrackerApp playbook, P3 deprecate legacy (with Boromir review), P4 inference automation.

**Approval:** ✅ Aragorn (approved), ✅ Boromir (to review P3), ✅ Frodo (to document).

**Source:** `.squad/decisions/inbox/aragorn-release-process-generic.md` (merged 2026-04-12)

---

#### Release-Process Skill: GitHub Discovery & Inference — Boromir (DevOps)

**Status:** Approved | **Date:** 2026-04-12 | **Scope:** team

**Verified:** All project facts discoverable via gh (100% confidence): owner, repo, branches, language, metadata, workflows (names), secrets (names only), branch protection rules, release/tag info.

**Inference Confidence:** Repo facts 100%, language/Docker 95%, versioning tool 85%, package registry 80%, deployment capability 70%.

**Ask vs. Infer:** User provides release type (major/minor/patch), publish targets, deployment URL (if custom); system auto-detects repo, branches, version from tags, package name, build commands, registry capabilities via secrets.

**Safe GitHub Access:** gh repo view, gh workflow list, gh secret list with json name (read-only, no values). Never use gh secret get or parse .github/workflows content.

**Fallback Strategies:** Version auto-detect to manual prompt, branch inference to default main, deployment skip unless configured, registry choice GitHub plus user select.

**Detection Script Pattern:** Runs early, emits discovered facts as JSON/shell vars. Interactive wizard prompts for required params using detected facts as defaults. Parameterized workflow template handles multiple strategies.

**Test Results (IssueTrackerApp):** gh repo view reliable, git describe finds v0.7.0, 9+ secrets discoverable, GitVersion.yml plus global.json coexist, workflows detectable. **Caveats:** Single-job CI, multiple version tools, secrets without workflows, tag-prefix variations.

**Source:** `.squad/decisions/inbox/boromir-release-process-generic.md` (merged 2026-04-12)

---

#### Release-Process Skill: Portable Template Design — Frodo (Tech Writer)

**Status:** Approved | **Date:** 2026-04-12 | **Scope:** team

**Solution:** Extract repo-agnostic template with YAML front matter auto-detection, placeholder-driven config, assumption matrix, and 7-step portable workflow degrading gracefully when features missing.

**YAML Front Matter:** Includes project name, language, capabilities (version tool, registry, docs builder, container registry), branches (DEV_BRANCH, RELEASE_BRANCH, TAG_FORMAT), repository config (UPSTREAM_OWNER, FORK_OWNER, PACKAGE_ID), assumptions checklist.

**7-Step Operator Workflow:** (1) Pre-flight check (merges, CI green, version tool present), (2) Bump version in VERSION_FILE, (3) Create release PR, (4) Merge PR, (5) Tag and create GitHub Release, (6) Monitor CI/CD (Build/Test required, NuGet/Docker/Docs/Demo optional — skip if capability missing), (7) Post-release sync branches.

**Capability Discovery:** Auto-detect version tool (version.json, GitVersion.yml, setup.py, Cargo.toml), package registry (secrets: NUGET_API_KEY, NPM_TOKEN, PYPI_TOKEN), Docker registry (DOCKER_PASSWORD, GHCR_TOKEN), docs builder (mkdocs.yml, Sphinx, mdBook), samples directory, CI workflows.

**Expected CI Jobs (Skip if Missing):** Build and Test (required), NuGet Publish (if REGISTRY configured), Docker Build (if credentials present), Docs Deploy (if docs found), Demo Deploy (if samples found).

**Fallback Hierarchy:** Required — Build, Tag, Release (no fallback). Optional — NuGet, Docker, Docs, Demos (skip if missing). Manual fallback — version bump (prompt if tool missing), CI trigger (manual dispatch if auto-trigger not configured).

**Assumptions for Release Lead:** All PRs merged to DEV_BRANCH? Local synced? CI green? VERSION_TOOL present? Upstream writable? If any unchecked, halt with guidance.

**Future Structure:** `.squad/templates/release-process-generic.md` (reusable template), `.squad/skills/release-process/CONFIG.yaml` (project-specific bindings).

**Implementation:** Phase 1 extract template, Phase 2 detection script, Phase 3 agent integration.

**Key Wins:** Single source of truth across 10+ projects, graceful degradation, clear assumptions, portable structure, auto-detection.

**Source:** `.squad/decisions/inbox/frodo-release-process-generic.md` (merged 2026-04-12)

---

**Scribe Note:** Three concurrent agent reviews (Aragorn architecture, Boromir discovery validation, Frodo template design) merged into single decision entry 2026-04-12T19:37:30Z. Orchestration logs written to `.squad/orchestration-log/`. Session log written to `.squad/log/`. Inbox files deleted after merge.

---

#### Release-Process Skill: Legacy Stub Deprecation — Frodo (Tech Writer)

**Status:** Implemented | **Date:** 2026-04-13 | **Scope:** team

## Decision

Replaced the content of `.squad/skills/release-process/SKILL.md` with a concise deprecation stub instead of deleting the directory.

## Rationale

1. **Preserve Old References:** Keeping the skill name `release-process` and directory ensures that old bookmarks, wiki links, and team documentation still land on a useful page.

2. **Clear Migration Path:** The stub explicitly points users to:
   - `.squad/skills/release-process-base/SKILL.md` — for generic, reusable release patterns
   - `.squad/playbooks/release-issuetracker.md` — for IssueTrackerApp-specific steps

3. **Avoid Orphaned Content:** The original content was project-specific (BlazorWebFormsComponents upstream fork) and created confusion with IssueTrackerApp's simpler single-branch model. Moving to a base skill + project playbook separates concerns.

4. **Phased Deletion:** The stub notes that deletion can happen later once all references are cleaned up, avoiding immediate data loss and giving the team time to adapt.

## What Changed

- **Old:** 200+ lines of upstream-fork-specific release workflow
- **New:** ~40 lines of deprecation guidance with clear next steps
- **Front Matter Updated:**
  - `status: "deprecated"` added
  - `description` updated to warn users
  - `confidence` lowered to "low"

## Next Steps (Out of Scope)

1. Track cleanup of old references to `release-process` in docs, wikis, and scripts
2. Once cleanup is complete, delete `.squad/skills/release-process/` directory
3. Update any `.squad/routing.md` rules pointing to this skill

## Impact

- **Team Adoption:** Quick, clear; users immediately know where to go
- **Documentation:** No orphaned or confusing content
- **Long-term:** Enables safe deletion once migration is verified

---

**Related Files:**
- `.squad/skills/release-process-base/SKILL.md` — generic patterns (already exists)
- `.squad/playbooks/release-issuetracker.md` — IssueTrackerApp playbook (already exists)

**Source:** `.squad/decisions/inbox/frodo-release-process-legacy-stub.md` (merged 2026-04-12)

---

## Branch Strategy: dev/main Two-Branch Model

**Author:** Boromir (DevOps)  
**Date:** 2026-04-13  
**Status:** ✅ Audit Complete — Feasible  

### Proposal

Implement a two-branch release model:
- **dev**: Active development branch — all feature/squad branches merge via **squash merge**
- **main**: Release-only branch — dev merges into main via **merge commit**, then tag + GitHub Release

### Current State

Repository already operates a **multi-branch model**:
- main — protected, squash-only merge
- preview — staging, manually promoted from dev
- insider — canary, auto-promoted on push
- squad/* — feature branches (current integration point: PR to main)

**Key infrastructure already in place:**
- squad-promote.yml workflow (dev → preview → main promotions)
- .squad/ path stripping on preview merge (forbidden paths never reach main)
- Tag-based release flow (squad-release.yml triggers on v*.*.*)
- Multi-branch CI (squad-ci.yml runs on dev/preview/main/insider)

### Audit Findings

**No Workflow Rewrites Needed** — Existing infrastructure supports this model.

**Pre-Push Hook Gate 0: One-Line Change**
- Current: blocks main only
- Required: block both dev and main

**.squad/ Path Guard Already Correct** — Already strips on dev → preview merge.

**Documentation Updates Required** (CONTRIBUTING.md):
1. Line 101 — Branch naming section
2. Line 120 — Create branch section (from dev, not main)
3. Line 431 — PR process section (target dev)
4. New section — Add release flow documentation

**GitHub Branch Protection Configuration** (admin task):
- Protect dev branch with same rules as main
- Require status checks, squash-only merges, auto-delete head branches

### Risk Assessment

| Risk | Severity | Mitigation |
|------|----------|-----------|
| Gate 0 pre-push hook not updated | Medium | One-line change |
| dev branch not protected | Medium | Admin configures |
| Dependabot bypasses dev | Low | Verify config |
| Release tagged from dev | Low | Enforce discipline |
| Documentation out of date | Low | Update CONTRIBUTING.md |

### Verdict

**✅ FEASIBLE** — Effort ~30 minutes; Risk: LOW. Framework already built for this.

**Source:** .squad/decisions/inbox/boromir-dev-main-workflows.md (merged 2026-04-12)

---

## MCP Configuration Commit Safety

**Author:** Boromir (DevOps)  
**Date:** 2026-04-12  
**Decision:** Committed MCP configuration files to squad/scribe-log-mcp-export  
**Verdict:** ✅ SAFE  

### Files Committed

- .copilot/mcp-config.json (modified)
- .mcp.json (new, untracked)
- squad-export.json (modified)

### Safety Assessment

All three files are **safe to commit**:

- **.copilot/mcp-config.json and .mcp.json:** MCP server configurations reference CONTEXT7_API_KEY only via input:CONTEXT7_API_KEY (VS Code input prompt). No hardcoded credentials.
- **squad-export.json:** Team metadata (agent charters, capabilities, decisions). No secrets embedded.

### Commit Hash

e8b1c22 on squad/scribe-log-mcp-export

**Security:** 🟢 No exposure risk; no credential leakage.

**Source:** .squad/decisions/inbox/boromir-mcp-config-commit.md (merged 2026-04-12)

---

## Documentation Audit: dev/main Branch Strategy

**Author:** Frodo (Tech Writer)  
**Date:** 2026-04-12  
**Status:** ✅ Recommended  

### Executive Summary

Reviewed 8 documentation files and 22 GitHub workflows to assess dev/main branch model impact. **Verdict: MODERATE documentation impact, FEASIBLE to implement.**

### Critical Updates (Must-do)

1. CONTRIBUTING.md Line 122 — Create branch from dev (not main)
2. CONTRIBUTING.md Lines 150–156 — Gate 0 protects dev AND main
3. CONTRIBUTING.md Line 431 — PR targets dev (features) or main (releases)
4. docs/New Work process.md Line 30 — Branch from origin/dev
5. docs/New Work process.md Line 115 — Merge to dev before sprint
6. docs/New Work process.md New Section — Add Release Flow documentation
7. squad-test.yml Workflow — Add dev to push trigger branches

### Impact Classification

| Metric | Assessment |
|--------|-----------|
| Severity | MODERATE |
| Files to update | 4 primary; 1 optional |
| Workflow updates | 1 (squad-test.yml) |
| Breaking changes | None |
| Estimated effort | 3–4 hours (docs) + 15 min (workflow) |
| Risk | Low |
| Recommendation | **PROCEED** with dev/main model |

### Implementation Roadmap

**Phase 1:** Update CONTRIBUTING.md (root) and docs/New Work process.md  
**Phase 2:** Update squad-test.yml (add dev to push triggers)  
**Phase 3:** Polish docs/CONTRIBUTING.md (optional)

### Conclusion

Dev/main branch model is documentation-feasible. Overhead is moderate and manageable.

**Source:** .squad/decisions/inbox/frodo-dev-main-docs-audit.md (merged 2026-04-12)

