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
