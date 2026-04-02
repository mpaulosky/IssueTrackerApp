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

## Learnings

### User Authorization Failure Root Cause Analysis (2026-03-29)

**Investigation:** Matthew Paulosky reported being authenticated but receiving Access Denied when accessing Dashboard, Issues, and Create pages.

**Root Cause Identified:** `UserPolicy` requires the `User` role claim (`AuthorizationRoles.User = "User"`), but Auth0 is not sending role claims that the `Auth0ClaimsTransformation` can map to ASP.NET Core `ClaimTypes.Role`.

**Diagnosis Chain:**
1. **Pages affected:** Dashboard, Issues/Index, Issues/Create all have `[Authorize(Policy = AuthorizationPolicies.UserPolicy)]`
2. **Policy definition:** `UserPolicy` requires `policy.RequireRole(AuthorizationRoles.User)` where `User = "User"` (src/Web/Program.cs:221-222)
3. **Claims transformation:** `Auth0ClaimsTransformation` has 3-pass role mapping:
   - Pass 1: Reads `Auth0:RoleClaimNamespace` config (empty in user secrets → skipped)
   - Pass 2: Falls back to standard `"roles"` JWT claim
   - Pass 3: Auto-detects any claim type ending in `/roles`
4. **Configuration gap:** `Auth0:RoleClaimNamespace` is NOT configured in user secrets (only Domain, ClientId, ClientSecret present)
5. **Auth0 tenant issue:** Auth0 tenant is not sending roles in the JWT token, either:
   - No custom Action/Rule configured to add roles to the token, OR
   - Roles are present but under a namespace that doesn't match Pass 2 or Pass 3 detection patterns

**Possible Solutions:**
1. **Auth0 tenant fix (recommended):** Configure Auth0 Action to add `roles` claim to ID token with values `["User"]` or `["Admin", "User"]`
2. **Auth0 namespace fix:** If roles are already in token under a custom namespace (e.g., `https://issuetracker.com/roles`), set `Auth0:RoleClaimNamespace` in user secrets
3. **Code workaround (not recommended):** Change `UserPolicy` to `RequireAuthenticatedUser()` instead of `RequireRole("User")` — but this breaks admin/user separation

**Access Denied Flow:** Routes.razor → `<AuthorizeRouteView>` → `<NotAuthorized>` → authenticated user → `Navigation.NavigateTo("/access-denied")` (line 11)

**Verification Needed:** Check Auth0 tenant JWT token (decoded at jwt.io) to see if `roles` claim exists and what namespace it uses.

---

## Notes
- Team transferred from IssueManager squad (2026-03-12)
- Same tech stack: .NET 10, Blazor, Aspire, MongoDB, Redis, Auth0, MediatR
- Ready for security-critical feature review and vulnerability assessments
