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
