# Legolas — Learnings for IssueTrackerApp

**Role:** Frontend - Blazor UI Components
**Project:** IssueTrackerApp
**Initialized:** 2026-03-12

---

## Core Context

**Project:** IssueTrackerApp — .NET 10, Blazor Interactive Server, MongoDB, Redis, .NET Aspire, Auth0
**Stack:** C# 14, Vertical Slice Architecture, MediatR CQRS, FluentValidation, bUnit tests
**Universe:** Lord of the Rings | **Squad version:** v0.5.4
**My role:** Frontend Developer - Blazor UI & Components
**Key files I own:** `src/Web/Components/`, `src/Web/Services/*Service.cs`, `src/Web/Styles/`
**Key patterns I know:**
- Tailwind CSS for utility-first styling with dark mode (data-theme attribute)
- SignalR real-time theme/nav sync via `SignalRClientService` with exponential backoff reconnection
- Two-level layout pattern: full-width outer element (w-full, themed background) + inner max-w-7xl constrained div
- Event callbacks for component communication; cascading parameters for state sharing
- Component wrapper vs layout component distinction: AdminPageLayout is ChildContent-based, not @layout-compatible
**Decisions I must respect:** See .squad/decisions.md

### Recent Sprints
- Sprint 1: SignalR frontend integration, Toast notifications, real-time issue updates
- Sprint 2: Issue Attachments UI (FileUpload, AttachmentCard/List components), Analytics Dashboard with Chart.js
- Sprint 3–4: NavMenu with role-based visibility, Landing page redesign, Profile role claims hardening
- Sprint 5: Admin users page scaffold, RoleBadge component, UserAuditLogPanel audit log inline viewer

---

## Recent Learnings

### Theme System Architecture
- Single localStorage key: `'tailwind-color-theme'` (unified across theme.js and components)
- themeManager global API (lowercase): getColor(), setColor(), getBrightness(), setBrightness()
- `data-theme-ready='true'` attribute for E2E test synchronization before clicking theme buttons
- Global CSS rule `nav {}` must be empty or removed — conflicted with multiple nav use cases (breadcrumbs, pagination, admin)

### Component Design Patterns
- **Two-level full-width layout:** Outer `<header class="w-full">` + inner `<div class="max-w-7xl mx-auto px-4">`
- **Component vs Layout:** AdminPageLayout is a wrapper component (ChildContent parameter), NOT a layout component (no @layout directive)
- **Modal button ambiguity:** Scope selectors to `[role='dialog']` in tests to avoid clicking header button instead of confirm
- **Profile role display:** Use GetAllRoleClaims() with optional roleClaimNamespace to handle Auth0 custom role claims as fallback

### SignalR Integration
- Services as scoped (not singleton) — each user circuit gets own state
- EventCallbacks for parent-child communication; use `InvokeAsync(StateHasChanged)` for thread-safe updates from SignalR
- IDisposable/IAsyncDisposable for proper cleanup; unsubscribe from hub groups on component disposal
- Exponential backoff reconnection: 0s, 2s, 5s, 10s (reduces server load)

### Analytics Dashboard & Charts
- Chart.js via CDN (simplifies setup vs npm dependency)
- Dark mode: read `<html>` classList for `.dark` class, apply appropriate chart colors
- Date range filtering applied at backend query level (not UI-side filtering)
- CSV export: backend generates fresh data each time (no caching)

### Authorization Integration
- Admin links visible only with `<AuthorizeView Policy="@AuthorizationPolicies.AdminPolicy">`
- Nested AuthorizeView requires `Context="adminContext"` to avoid context name collision in Razor
- Profile.razor requires `@inject IConfiguration Configuration` to read Auth0:RoleClaimNamespace config

---

## Notes
- Team transferred from IssueManager squad (2026-03-12)
- Same tech stack: .NET 10, Blazor, Aspire, MongoDB, Redis, Auth0, MediatR
- Ready for feature expansion and component refinement

### CSS Button Consolidation (2026-06-20)
- **Task:** Consolidated button styling in `src/Web/Styles/input.css` and added `btn` prefix to all variant usages across 22 Razor files.
- **Key changes to input.css:**
  - `.btn` base: changed `border border-transparent` → `border-2 border-transparent`, added `text-white`
  - `.btn-primary`, `.btn-secondary`: removed duplicate `text-white` and `border-2 border-transparent`
  - `.btn-warning`: changed from red to amber (`bg-amber-500`, `hover:bg-amber-700`, `focus:ring-amber-400`), removed duplicates
  - Added `.btn-danger` (red) — was missing but used in 7 places
  - Added `.container-card` utility after `.card-footer`
- **Pattern applied to Razor files:** Every `class="btn-primary"` etc. → `class="btn btn-primary"` (22 files)
- **Special cases handled:**
  - `BulkConfirmationModal.razor`: C# string interpolation `$"btn-danger {extraClasses}"` → `$"btn btn-danger {extraClasses}"`
  - `DateRangePicker.razor`: C# ternary `"btn-primary rounded-lg"` → `"btn btn-primary rounded-lg"`
  - `Index.razor`: Inline Razor ternary `"btn-primary text-xs px-3 py-1.5"` → `"btn btn-primary text-xs px-3 py-1.5"`
- **Build:** Tailwind CSS rebuild ran successfully with `npm run css:build`

### CSS Button Consolidation — Phase 2 (2026-04-02)
- **Task:** Enforced `.btn` base class pairing across all 22 Razor components
- **Key Work:**
  - Added "btn " prefix to all button variant class references (e.g., `class="btn btn-primary"`)
  - Updated C# string interpolations: `$"btn-danger ..."` → `$"btn btn-danger ..."`
  - Updated Razor ternary expressions: `_active ? "btn-primary" : ...` → `_active ? "btn btn-primary" : ...`
  - All button usage now follows the rule: `.btn` base + `.btn-{variant}`
- **Build Status:** Tailwind CSS rebuild succeeded
- **Verification:** Full test suite passed (1,557/1,595 — 38 pre-existing infrastructure failures unrelated to changes)
- **Note:** This enforcement ensures consistent button appearance and semantic color usage (warning now amber, not red)

## Learnings

### Styling-Fixes Branch Review (2026-06-22)
- **Task:** Full frontend review of `feature/styling-fixes` branch (28 Razor files + 2 CSS files)
- **Theme of the PR:** Readability uplift — `text-sm text-primary-500 dark:text-primary-400` → `text-base text-primary-800 dark:text-primary-50` across all components, CSS palette migration from `gray-*` to `primary-*`, Tailwind modernization.
- **Critical bugs found (❌):**
  - `FileUpload.razor`: `text-primary-6800` typo (line 59) — invalid class, upload link will be unstyled
  - `input.css` `.form-input`: `dark:bg-primary-50` — very light bg in dark mode, should be `dark:bg-primary-900` or similar dark tone
  - `input.css` Blazor error boundary: `color: #929292` (gray) on `#b32121` red bg — fails WCAG contrast (was `color: white`)
  - `SearchInput.razor`: outer wrapper gets `bg-primary-800` while inner input has `bg-primary-50` from `.form-input` — visual mismatch in light mode
  - `Details.razor`: error-state back-link div gets `bg-primary-700` hardcoded in light mode — dark box around link in error state
  - `UserListTable.razor`: "Edit Roles" button stripped of `btn btn-primary` → bare `text-green-600` text link — loses button affordance, inconsistent with "Audit Log" button beside it
- **Minor issues found (⚠️):**
  - `CommentsSection.razor`: tab character artifact in `InputTextArea` class string
  - `UserAuditLogPanel.razor`: table header still uses `text-primary-300` (not updated to `text-primary-100` like UserListTable)
  - `FilterPanel.razor`: active filter count badge changed from `text-xs` to `text-base` — too large for compact badge
  - `Details.razor`: bottom "Back to Issues" div `hover:bg-primary-700` on already `bg-primary-700` = invisible hover
  - `SummaryCard.razor`: `@Value` text still uses `dark:text-white` while rest of card uses `dark:text-primary-50`
  - `LabelInput.razor`: `placeholder-primary-800 dark:placeholder-primary-800` — no dark mode adjustment
  - `Analytics.razor`: removed `heading-section` class from all 4 chart headings — relies on global h3 styles now
- **Patterns confirmed working:**
  - All `@bind`, `@onclick`, `@onkeydown`, `@ref` event handlers fully preserved
  - All ARIA attributes (`aria-label`, `aria-expanded`, `aria-modal`, `role="dialog"`) preserved
  - `flex-shrink-0` → `shrink-0` throughout — valid Tailwind modernization
  - `gray-*` → `primary-*` in CSS utilities (btn-icon, modals, links, headings) — excellent systematic palette work
  - `text-md` → `text-base` in FooterComponent — legitimate bug fix (`text-md` is invalid Tailwind)
- **Key learning:** When applying a bulk text color migration, always check that dark-mode variants are actually darker, not accidentally the same light shade as light mode (the `dark:bg-primary-50` bug in `.form-input` is the canonical example).

### Button Padding & Admin Color Palette Update (2026-06-21)
- **Task:** Removed inline `px-*`/`py-*` overrides from buttons already using `.btn` class; updated Admin/Users components from gray to primary palette
- **Button Padding Changes:**
  - `.btn` base class already defines `px-5 py-2` in `input.css` — inline overrides removed from 11 locations
  - Files cleaned: CommentsSection, AttachmentCard, BulkActionToolbar, Issues/Index, Issues/Details, Dashboard, Home
  - Rule: Keep `.btn` padding consistent; only override for specific design intent (e.g., text-xs sizing)
  - Removed `rounded-lg` from Home.razor CTA button — `.btn` base already defines `rounded-full`
- **Admin Components Color Update (Components/Admin/Users/):**
  - Converted from gray palette to primary palette for consistency with Home.razor visual style
  - `bg-white dark:bg-gray-800` → `card-bordered` (existing CSS class with primary background)
  - `bg-gray-50 dark:bg-gray-700` (table headers) → `bg-primary-200 dark:bg-primary-700`
  - `border-gray-200 dark:border-gray-700` → `border-primary-200 dark:border-primary-700`
  - `divide-gray-200 dark:divide-gray-700` → `divide-primary-200 dark:divide-primary-700`
  - Pagination buttons in UserAuditLogPanel: converted from long inline classes → `btn btn-secondary`
  - Files updated: UserListTable, UserAuditLogPanel, EditUserRolesModal
  - Text color classes (`text-gray-*`, `text-neutral-*`) intentionally preserved for readability
- **Build Status:** Tailwind CSS rebuild succeeded (80ms)
- **Key Learning:** When base CSS class defines padding/spacing, avoid inline overrides unless required for visual hierarchy

## Styling Review — `feature/styling-fixes` (2026-06-22)

**Task:** Full review of 30 changed files on `feature/styling-fixes` branch.
**Verdict:** Needs fixes (5 critical, ~14 minor) — do NOT merge as-is.

### Critical bugs found

1. **`CommentsSection.razor:194`** — `primary-50space-pre-wrap` is a corrupted class (merge artefact). Should be `whitespace-pre-wrap`. Comment content loses whitespace preservation.
2. **`FileUpload.razor:59`** — `text-primary-6800` is an invalid TW class. Should be `text-primary-800`.
3. **`input.css .form-input`** — `dark:bg-primary-50` is same as light value — all form inputs render with light background in dark mode. Fix: `dark:bg-primary-800`.
4. **`Issues/Index.razor:193`** — Removed null guard: `@issue.Author.Name` (was `?.Name ?? "Unknown"`). Potential NullReferenceException.
5. **`input.css .blazor-error-boundary`** — `color: #929292` (hardcoded hex) on `#b32121` red background. ~2.5:1 contrast, fails WCAG AA. Fix: `color: white`.

### Important patterns learned

- **Always pair `dark:` variants** when applying any `bg-*` or `text-*` that differs in dark mode. Several containers in this branch gained a hardcoded dark `bg-primary-700` with no `dark:` pair (wrong in light mode).
- **`.form-input` now includes `p-2`** in input.css — do NOT add inline `p-2` on top of `form-input`; it doubles padding.
- **`text-md` is not a Tailwind class** — the correct utility is `text-base`. This was caught and fixed throughout this PR.
- **`flex-shrink-0` → `shrink-0`** — `shrink-0` is the correct Tailwind v4 utility (though both work in v3/v4, `shrink-0` is canonical).
- **`heading-page` / `heading-section`** CSS classes can be dropped where the global h1–h6 rule (added in input.css) already supplies `font-bold tracking-tight text-primary-800 dark:text-primary-50`. But dropping them changes `font-medium` sections to `font-bold` — subtle weight regression.
- **Bracket syntax safer for arbitrary max-w values** — `max-w-[150px]` is more portable than `max-w-37.5` even if TW4 JIT handles decimals.
- **Non-styling commits (version bumps)** should not be mixed into styling PRs — Aspire 13.2.0→13.2.1 bumps landed in this PR.
- **Design token hygiene**: `dark:text-primary-800` (same as light value) and `dark:bg-primary-800` (identical to non-dark) are no-ops and indicate the dark: variant was copy-pasted without review.

### PR Review Clarifications — Items 6 & 7 (2026-06-23)

#### Item 6 — Details.razor `bg-primary-700` dark-mode scoping

Two `bg-primary-700` occurrences land in the diff without a `dark:` prefix:

1. **Error-state back-link div** (`<div class="mt-4 bg-primary-700 text-primary-50">`):
   - In light mode: renders a dark-navy box around the "← Back to Issues" link in the error banner — jarring against the page's light background.
   - Fix: add `dark:` prefix → `<div class="mt-4 dark:bg-primary-700 dark:text-primary-50">` (or revert to `<div class="mt-4">` with `class="link-primary"` on the `<a>`).

2. **Bottom card back-link strip** (`<div class="bg-primary-700 px-4 py-3 sm:px-6">`):
   - Original was `bg-primary-50 dark:bg-primary-700` (light in light mode, dark in dark mode). Matthew dropped the `bg-primary-50` and the `dark:` scope, making it always dark navy.
   - Fix: revert to `bg-primary-50 dark:bg-primary-700`.

**Rule reinforced:** Any `bg-primary-700` applied without a `dark:` scope will render a dark navy block in light mode — always add `dark:bg-primary-700`, never bare.

#### Item 7 — UserListTable "Edit Roles" button text-link pattern

- Matthew changed `btn btn-primary` → `text-green-600 dark:text-green-400 hover:text-green-900 dark:hover:text-green-300`.
- Categories.razor and Statuses.razor both use this exact text-link pattern for in-table action buttons (Edit, Restore, Archive).
- The "Audit Log" button on the same row is also a text link (`text-indigo-600 dark:text-indigo-400 hover:text-indigo-900 dark:hover:text-indigo-300`).
- Matthew is correct — text-link style IS the established pattern for admin table actions. `btn btn-primary` was the inconsistency.
- Existing bUnit tests (`UserListTableTests.cs`) do NOT assert on CSS classes — they only check text content and callback invocation. No test update required.
