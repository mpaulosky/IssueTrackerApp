# Aragorn — Learnings for IssueTrackerApp

**Role:** Lead - Architecture & Coordination
**Project:** IssueTrackerApp
**Initialized:** 2026-03-12

---

## Learnings

### 2025-07-22 — DTO–Model Separation Analysis

**Architecture Decision:** Models must NOT embed DTO types. DTOs are transfer-only; Models are persistence-only. Mappers bridge the two. See `.squad/decisions/inbox/aragorn-dto-model-separation.md`.

**Key Findings:**
- 5 domain Models (Issue, Category, Status, Comment, Attachment) embed DTOs (`CategoryDto`, `UserDto`, `StatusDto`, `IssueDto`) as properties persisted to MongoDB
- `Comment.Issue` stores a full `IssueDto` creating a circular dependency — must change to `ObjectId IssueId`
- No mapper classes exist — conversion happens via DTO constructors (`new IssueDto(issue)`)
- `IssueConfiguration` uses `builder.Ignore()` to skip DTO properties for EF Core, letting MongoDB BSON serializer handle them directly
- `EmailQueueItem`, `NotificationPreferences`, `User` models are already clean (no DTO references)

**Key File Paths:**
- Models: `src/Domain/Models/` (Issue.cs, Category.cs, Status.cs, Comment.cs, Attachment.cs)
- DTOs: `src/Domain/DTOs/` (IssueDto.cs, CategoryDto.cs, StatusDto.cs, CommentDto.cs, UserDto.cs, AttachmentDto.cs, Analytics/)
- CQRS Handlers: `src/Domain/Features/` (Issues, Categories, Statuses, Comments, Attachments, Analytics, Dashboard, Notifications)
- Persistence: `src/Persistence.MongoDb/` (Repository.cs, IssueTrackerDbContext.cs, Configurations/)
- Services: `src/Web/Services/` (IssueService.cs, LookupService.cs uses direct repo access)
- Tests: 81 test files across 5 projects (Domain.Tests ~50, Web.Tests ~9, Bunit ~9, Integration ~9, Architecture ~4)

**Patterns Confirmed:**
- Generic `Repository<TEntity>` wraps `DbContext` with `Result<T>` error handling
- Services are MediatR facades — delegate to handlers, no business logic
- `LookupService` is the only service with direct repository access and inline Model→DTO conversion
- 31 CQRS handlers total across all features
- Blazor components consume DTOs for display — minimal UI impact from this refactoring
- `PaginatedResponse<T>` and `PagedResult<T>` both exist (pagination duplication — future cleanup candidate)

**User Preference:** Matthew Paulosky wants strict clean architecture enforcement

---

## Notes

- Team transferred from IssueManager squad
- Same tech stack: .NET 10, Blazor, Aspire, MongoDB, Redis, Auth0, MediatR
- Ready to begin development
---

### 2026-07-23 — PR #76 Review: AppHost.Tests — Aspire integration + Playwright E2E tests

**Verdict:** APPROVED (posted as comment — GitHub prevented self-approval by PR author)

**PR:** `feat(tests): AppHost.Tests — Aspire integration + Playwright E2E tests`  
**Branch:** `squad/apphost-tests-clean`  
**Files reviewed:** 37 changed files (18 new C# files, test infrastructure, Program.cs, CI)

**Key findings:**
- All 18 new C# files carry the required copyright block ✅
- `.squad/` files on a `squad/*` branch — permissible per charter (prohibition is `feature/*` only) ✅
- xUnit collection structure correct: `[Collection]` on abstract `BasePlaywrightTests` inherits to all derived test classes ✅
- `AspireManager` lifecycle correct: chains `PlaywrightManager.InitializeAsync()` + `StartAppAsync()` ✅
- Testing-environment seam in `Program.cs` (cookie auth, fake repos, skipped background services) is the right Aspire E2E pattern ✅
- `EnvironmentCallbackAnnotation` to inject `ASPNETCORE_ENVIRONMENT=Testing` past DCP override — sophisticated and correct ✅
- Fixed HTTPS port 7043 with `IsProxied = false` — predictable base URL ✅

**Nits flagged (non-blocking):**
1. `EnvVarTests.cs`: Add a TODO alongside `#pragma warning disable CS0618` for the obsolete `GetEnvironmentVariableValuesAsync` API
2. `FakeRepository.cs` / `FakeSeedData.cs` in `src/Web/Testing/`: decorate with `[ExcludeFromCodeCoverage]` to avoid coverage inflation
3. `WebPlaywrightTests.cs` home-page tests overlap with `HomePageTests.cs` — remove in follow-up

**Decision recorded:** `.squad/decisions/inbox/aragorn-pr76-review.md`

---

### 2026-07-23 — PR #76 Fixes: Gimli Blocking Issues Resolved

**Trigger:** Gimli (Tester) rejected PR #76 with 6 blocking issues.

**Fixes applied on `squad/apphost-tests-clean`:**

1. **False "skip gracefully" docs (3 files)** — `AdminPageTests.cs`, `LayoutAdminTests.cs`, `LayoutAuthenticatedTests.cs` had file-top comments and class summary docstrings claiming tests skip when `PLAYWRIGHT_TEST_*` env vars are absent. This is factually wrong — the tests use `/test/login?role=...` cookie auth and always run. Removed all misleading comments; rewrote docstrings to describe the actual cookie-based auth mechanism.

2. **`InteractWithPageAsync` visibility** — Changed from `public` to `protected` in `BasePlaywrightTests.cs` to match all sibling helper methods.

3. **`IBrowserContext` leak** — `CreatePageAsync` was overwriting a single `_context` field on every call, leaking all but the last context. Replaced with `private readonly List<IBrowserContext> _contexts = new()` and `foreach` disposal in `DisposeAsync`.

4. **Fragile redirect assertion** — `AdminPage_RedirectsNonAdminUser` used `NotContain("/admin")` which is brittle. Replaced with `Contain("/Account/AccessDenied")` — the redirect destination set by ASP.NET Core cookie auth when `AccessDeniedPath` is not explicitly overridden (default: `/Account/AccessDenied`).

5. **Missing EOF newline** — `EnvVarTests.cs` was missing the trailing newline. Fixed.

6. **`DisableDashboard = false → true`** — The Aspire dashboard should be disabled in tests to avoid unnecessary resource usage and port conflicts.

**Build:** `dotnet build tests/AppHost.Tests/AppHost.Tests.csproj --no-restore` — 0 errors, 0 warnings ✅


---

### 2026-03-27 — PR Review Session: Pippin (#84) & Legolas (#83)

**Role:** Lead Reviewer

**PRs Reviewed:**

1. **PR #84 (Pippin):** Test fixes for #78, #79, #80
   - TimeoutException semantics in `WaitForWebReadyAsync`
   - `DisableDashboard = true` in `EnvVarTests.cs`
   - Specific assertion on Admin dashboard heading
   - **Verdict:** ✅ Approved — all fixes semantically correct and well-scoped

2. **PR #83 (Legolas):** `/Account/AccessDenied` Blazor page (#77)
   - Public, unauthorized page for Auth0 redirect flow
   - Consistent layout, friendly copy, Tailwind styling
   - **Verdict:** ✅ Approved — proper auth flow design, UX improvement

**Team Coordination:** Both PRs merged same session; squad decisions recorded and deduplicated.

---

### 2026-03-28 — Theme System Unification: Resolved Dual localStorage Conflict

**Trigger:** Pippin discovered during E2E test analysis (PR #86) that two conflicting theme systems were active, causing user theme preferences to not persist across page reloads.

**Problem:**
- **Old System:** `theme.js` with `window.themeManager` (lowercase), used `theme-color-brightness` localStorage key, consumed by `ThemeProvider.razor.cs`
- **New System:** `theme-manager.js` with `window.ThemeManager` (uppercase), used `tailwind-color-theme` localStorage key, consumed by `ThemeColorDropdownComponent` and `ThemeBrightnessToggleComponent` (added in PR #86)
- User selects red theme → New components write to `tailwind-color-theme` → Page reload → ThemeProvider reads `theme-color-brightness` → Theme reverts to blue

**Solution Chosen:** Option A — Adapt new components to old system, keep ThemeProvider as single source of truth

**Rationale:**
- `theme.js` / `themeManager` is well-established, sets `data-theme-ready` for E2E tests, has complete API
- `ThemeProvider.razor.cs` is the architectural authority for theme state
- Pippin already updated E2E tests to expect `tailwind-color-theme` key (PR #86), so aligned `theme.js` STORAGE_KEY to match
- Single localStorage key + single JS API eliminates persistence bugs

**Changes Applied:**
1. **theme.js:** Changed `STORAGE_KEY` from `'theme-color-brightness'` to `'tailwind-color-theme'` (line 20)
2. **ThemeColorDropdownComponent.razor:**
   - `OnAfterRenderAsync`: Changed `ThemeManager.getCurrentColor()` → `themeManager.getColor()`, uppercase color response
   - `SelectColorAsync`: Changed `ThemeManager.selectColorAndUpdateUI(color)` → `themeManager.setColor(color.ToLowerInvariant())`
3. **ThemeBrightnessToggleComponent.razor:**
   - `OnAfterRenderAsync`: Changed `ThemeManager.syncUI()` → `themeManager.getBrightness()`, read current brightness
   - `ToggleBrightnessAsync`: Changed `ThemeManager.selectBrightnessAndUpdateUI(next)` → `themeManager.setBrightness(next)`
4. **App.razor:**
   - Removed `<script src="js/theme-manager.js"></script>` reference (line 53 deleted)
   - Updated inline script comment: `theme-manager.js` → `theme.js`

**Files Changed:**
- `src/Web/wwwroot/js/theme.js` (1 line)
- `src/Web/Components/Theme/ThemeColorDropdownComponent.razor` (3 lines)
- `src/Web/Components/Theme/ThemeBrightnessToggleComponent.razor` (3 lines)
- `src/Web/Components/App.razor` (2 lines removed, 1 comment updated)

**Build:** ✅ `dotnet build IssueTrackerApp.slnx --configuration Release` — 0 errors, 0 warnings

**Test Compatibility:** E2E tests in `AppHost.Tests/Tests/Theme/` (ThemeToggleTests.cs, ColorSchemeTests.cs) now align with production code — both use `tailwind-color-theme` key.

**Architectural Note:** `theme-manager.js` still exists in `wwwroot/js/` but is no longer referenced or loaded. Should be deleted in a follow-up cleanup commit to avoid confusion.

**Decision recorded:** `.squad/decisions/inbox/aragorn-unified-theme-system.md`
