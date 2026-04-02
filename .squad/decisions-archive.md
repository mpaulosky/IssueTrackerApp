# IssueTrackerApp Decisions Archive

Historical decisions archived from before 2026-02-01. See decisions.md for current decisions.

---

## Archived Decisions

### MongoDB Connection String Fallback (2025-03-21)

**Author:** Sam (Backend Developer)  
**Status:** Implemented

The Web project crashed at startup with `System.TimeoutException` because the EF Core MongoDB provider reads `MongoDB:ConnectionString` (hardcoded to `mongodb://localhost:27017` in appsettings.Development.json), while the actual Atlas connection string lives in `ConnectionStrings:mongodb` (user secrets / Aspire injection). These two config paths never intersect.

**Decision:** Added fallback logic in `AddMongoDbPersistence` that bridges the gap:

1. Before binding `MongoDbSettings`, check if `MongoDB:ConnectionString` is empty or equals `mongodb://localhost:27017`
2. If so, read `ConnectionStrings:mongodb` and overlay it into the MongoDB config section  
3. Changed `appsettings.Development.json` to use empty string instead of the localhost default

**Priority order:**
- Explicit `MongoDB:ConnectionString` → used as-is
- Empty/localhost → falls back to `ConnectionStrings:mongodb` (Aspire-injected or user secrets)

**Impact:**
- **Aspire AppHost:** Works — Aspire injects `ConnectionStrings:mongodb` as env var, fallback picks it up
- **Standalone + user secrets:** Works — user secret `ConnectionStrings:mongodb` is read as fallback
- **Explicit config:** Works — non-empty, non-localhost `MongoDB:ConnectionString` takes priority
- **Tests:** Unaffected — `Testing` environment skips `AddMongoDBClient` and tests use TestContainers

**Files Changed:** `src/Persistence.MongoDb/ServiceCollectionExtensions.cs`, `src/Web/appsettings.Development.json`

**Rationale:** When two config systems disagree (Aspire vs raw appsettings), bridge them at the DI registration layer using configuration overlay before binding Options.

---

### 2025-07-14: v0.5.0 — Admin User Management — Architectural Decisions

**By:** Aragorn (Lead Developer) — Plan Ceremony  
**Feature:** v0.5.0 Admin User Management  
**Milestone:** #7 — v0.5.0 - Admin User Management

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

### 2025-07-15: ADR: Auth0 Management API Integration Strategy

**Status:** Proposed  
**Author:** Gandalf  
**Issue:** #130 — [Spike] Auth0 Management API — capabilities, rate limits, and SDK options

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

**Scribe Note:** All entries in this archive are pre-2026-02-01. Current decisions are in decisions.md.
