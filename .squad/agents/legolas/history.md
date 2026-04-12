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

### Historical Foundation (March–June 2026)

**Sprints 1–5:**
- Sprint 1: SignalR frontend integration, Toast notifications, real-time issue updates
- Sprint 2: Issue Attachments UI (FileUpload, AttachmentCard/List components), Analytics Dashboard with Chart.js
- Sprint 3–4: NavMenu with role-based visibility, Landing page redesign, Profile role claims hardening
- Sprint 5: Admin users page scaffold, RoleBadge component, UserAuditLogPanel audit log inline viewer

**Theme System Architecture:**
- Single localStorage key: `'tailwind-color-theme'` (unified across theme.js and components)
- themeManager global API (lowercase): getColor(), setColor(), getBrightness(), setBrightness()
- `data-theme-ready='true'` attribute for E2E test synchronization

**Component Design Patterns:**
- Two-level full-width layout: Outer `<header class="w-full">` + inner `<div class="max-w-7xl mx-auto px-4">`
- AdminPageLayout is wrapper component (ChildContent parameter), NOT layout component
- Modal button ambiguity: Scope selectors to `[role='dialog']` to avoid header button clicks
- Profile role display: Use GetAllRoleClaims() with optional roleClaimNamespace for Auth0 custom role claims

**SignalR Integration:**
- Services as scoped (not singleton) — each user circuit gets own state
- EventCallbacks for parent-child communication; use `InvokeAsync(StateHasChanged)` for thread-safe updates
- IDisposable/IAsyncDisposable for proper cleanup
- Exponential backoff reconnection: 0s, 2s, 5s, 10s

**Analytics Dashboard & Charts:**
- Chart.js via CDN
- Dark mode: read `<html>` classList for `.dark` class, apply appropriate chart colors
- Date range filtering at backend query level
- CSV export: backend generates fresh data each time (no caching)

**CSS Button Consolidation (2026-06-20 & 2026-04-02):**
- Consolidated button styling: `.btn` base + `.btn-{variant}` across 22 Razor files
- Pattern: `class="btn btn-primary"` everywhere
- Key changes: Added `.btn-danger`, changed `.btn-warning` from red to amber, unified border styling
- Special cases: C# string interpolation `$"btn-danger {extraClasses}"`, Razor ternary expressions
- Tailwind CSS rebuild successful

**Styling-Fixes Branch Review (2026-06-22 & 2026-06-23):**
- Readability uplift: `text-sm text-primary-500 dark:text-primary-400` → `text-base text-primary-800 dark:text-primary-50`
- CSS palette migration: `gray-*` to `primary-*`
- Tailwind modernization: `text-md` → `text-base`, `flex-shrink-0` → `shrink-0`
- Global h1–h6 rule added (font-bold, tracking-tight, text-primary-800 dark:text-primary-50)
- Design token hygiene: Avoid `dark:bg-primary-800` when light value is identical (no-ops indicate review gaps)
- Dark-mode scoping rule: Any `bg-primary-700` without `dark:` scope renders dark navy in light mode — always scoped
- Text-link pattern for admin table actions: `text-green-600 dark:text-green-400` (established; `btn btn-primary` was inconsistency)
- bUnit tests do NOT assert on CSS classes — only text content and callback invocation

---

## Recent Learnings

### Authorization Integration
- Admin links visible only with `<AuthorizeView Policy="@AuthorizationPolicies.AdminPolicy">`
- Nested AuthorizeView requires `Context="adminContext"` to avoid context name collision
- Profile.razor requires `@inject IConfiguration Configuration` to read Auth0:RoleClaimNamespace config

### Notes
- Team transferred from IssueManager squad (2026-03-12)
- Same tech stack: .NET 10, Blazor, Aspire, MongoDB, Redis, Auth0, MediatR
- Ready for feature expansion and component refinement
