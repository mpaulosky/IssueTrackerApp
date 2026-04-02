# Gandalf — Learnings for IssueTrackerApp

**Role:** Security Officer - Auth & Security
**Project:** IssueTrackerApp
**Initialized:** 2026-03-12

---

## Core Context

**Project:** IssueTrackerApp — .NET 10, Blazor Interactive Server, MongoDB, Redis, .NET Aspire, Auth0
**Stack:** C# 14, Vertical Slice Architecture, MediatR CQRS, FluentValidation, bUnit tests
**Universe:** Lord of the Rings | **Squad version:** v0.5.4
**My role:** Security Officer - Authentication & Authorization
**Key files I own:** `src/Web/Auth/`, `src/Web/Features/Admin/Users/`, Auth0 configuration
**Key patterns I know:**
- Auth0 OIDC flow with PKCE (most secure for web apps); Authorization Code flow with refresh tokens
- Role claims transformation: 3-pass mapping (namespace → bare "roles" → auto-detect "/roles" suffix)
- M2M credentials separate from OIDC; token caching 24h TTL - 5min margin; rate limit TODO acceptable technical debt
- Input validation on all Auth0 API calls; error surfacing without stack trace leakage
- Access-denied redirect path: `/Account/AccessDenied` (ASP.NET Core default when not explicitly overridden)
**Decisions I must respect:** See .squad/decisions.md

### Recent Sprints
- Sprint 1: Auth0 authentication & authorization setup, claims transformation, role claim mapping
- Sprint 2–3: Pass 3 auto-detect for misconfigured namespace, role fallback to bare "roles" claim
- Sprint 4: Auth0 Management API research spike (ADR #130), M2M token caching strategy
- Sprint 5: UserManagementService security review (approved), token caching validation, input sanitization

---

## Recent Learnings

### Auth0 Integration Patterns
- Authorization Code + PKCE flow is most secure for server-side web apps
- HTTPS required; JWT audience/issuer validation; secure cookie configuration enforced
- Role claim namespace configurable via Auth0:RoleClaimNamespace (production: environment variable)
- Never commit Auth0 secrets to source control; use user secrets (dev) or Azure Key Vault (prod)

### Claims Transformation Strategy
- Pass 1: If namespace configured, map from that claim type
- Pass 2: If Pass 1 finds no roles, fall back to standard "roles" claim
- Pass 3: If Passes 1–2 find no roles, auto-detect any claim type ending in "/roles" (defensive catch-all)
- All passes are additive-only; deduplication via identity.HasClaim() prevents claim injection
- Idempotent transformation prevents duplicate role claims from multiple sources

### Auth0 Management API (M2M) Security
- Client credentials flow scoped to Management API only (`https://{domain}/api/v2/`)
- M2M credentials isolated from OIDC credentials (least-privilege principle)
- Token caching in IMemoryCache with TTL = ExpiresIn - 300s (5-minute safety margin)
- Rate limit HTTP 429 handling deferred to follow-up (acceptable non-blocking TODO)
- Input validation: userId null-check, roleNames safe via (roleNames ?? []).ToList(), unknown roles rejected with ResultErrorCode.Validation

### Security Review Checklist
- ✅ Secrets hygiene: no credentials in appsettings.json (empty placeholders only)
- ✅ Token security: application-wide M2M cache (not user-specific), proper TTL, fresh ManagementApiClient per operation
- ✅ Client credentials scope: separate M2M from OIDC, audience-scoped to Management API
- ✅ Input validation: no raw string concatenation, all via strongly-typed models
- ✅ Error surfacing: full exception logged server-side, only ex.Message to client (no stack trace leakage)
- ✅ Dependency security: Auth0.ManagementApi 7.46.0 no known CVEs

---

## Notes
- Team transferred from IssueManager squad (2026-03-12)
- Same tech stack: .NET 10, Blazor, Aspire, MongoDB, Redis, Auth0, MediatR
- Ready for security-critical feature review and vulnerability assessments
