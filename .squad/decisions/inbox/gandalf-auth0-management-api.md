# ADR: Auth0 Management API Integration Strategy

**Status:** Proposed  
**Date:** 2025-07-15  
**Author:** Gandalf  
**Issue:** #130 — [Spike] Auth0 Management API — capabilities, rate limits, and SDK options

---

## Context

IssueTrackerApp currently uses Auth0 for end-user authentication via the OIDC Authorization Code flow with PKCE (`src/Web/Auth/`). Role assignment (Admin / User) is managed manually in the Auth0 dashboard. As the platform scales and automated user-role provisioning becomes necessary (e.g., assigning roles programmatically upon user registration, syncing roles from an admin UI), direct calls to the **Auth0 Management API v2** are required.

The existing `Auth0Options` binds `Domain`, `ClientId`, `ClientSecret`, and `RoleClaimNamespace` from configuration. The existing credential-based setup is an OIDC client app — it is **not** a Machine-to-Machine (M2M) app and does not hold Management API scopes. A separate M2M configuration is required.

This spike evaluates:
1. Which Management API v2 endpoints are needed
2. How to obtain and cache M2M access tokens (client credentials flow)
3. Auth0 rate limits and pagination strategy
4. SDK choice: `Auth0.ManagementApi` NuGet package vs raw `HttpClient`
5. Required Auth0 dashboard configuration
6. Secrets management strategy

---

## Decision

**Use the official `Auth0.ManagementApi` NuGet package (`ManagementApiClient`) with a dedicated M2M application, caching the Management API token in `IMemoryCache` with a TTL-based refresh strategy, and storing M2M credentials in .NET User Secrets (development) and Azure Key Vault (production).**

Rationale:
- The official SDK is actively maintained by Auth0/Okta, handles token acquisition internally, provides strongly-typed request/response objects, and reduces boilerplate.
- A dedicated M2M app in Auth0 cleanly separates management-plane credentials from user-facing OIDC credentials, limiting blast radius on credential rotation.
- The app already uses `IMemoryCache` for analytics TTLs; reusing the same pattern for token caching is idiomatic and avoids new infrastructure.

---

## Consequences

**Positive:**
- Programmatic role assignment enables automated onboarding and admin UI workflows without manual Auth0 dashboard intervention.
- Strongly-typed SDK reduces surface area for serialization bugs.
- Token caching avoids unnecessary M2M token requests and respects rate limits.
- Separation of M2M and OIDC credentials follows least-privilege principle.

**Negative / Trade-offs:**
- Adds a new NuGet dependency (`Auth0.ManagementApi`).
- Requires Auth0 dashboard configuration (new M2M app, API permission grants) — this is a manual step that cannot be automated by code alone.
- M2M tokens are sensitive; any misconfiguration of Key Vault access policies would cause Management API calls to fail at runtime.
- Rate limits on the free Auth0 tier (2 req/sec burst, ~1,000 req/month on some plan tiers) mean bulk operations must be throttled.

---

## Implementation Notes

### Required Auth0 Dashboard Setup

1. **Create a Machine-to-Machine Application** in the Auth0 dashboard:
   - Name: `IssueTrackerApp Management API Client` (or similar)
   - Application type: Machine to Machine
   - Authorized API: `Auth0 Management API`

2. **Grant the following API permissions** to the M2M application:
   | Permission | Purpose |
   |---|---|
   | `read:users` | `GET /api/v2/users`, `GET /api/v2/users/{id}` |
   | `read:roles` | `GET /api/v2/roles`, `GET /api/v2/roles/{id}` |
   | `read:role_members` | `GET /api/v2/roles/{id}/users` |
   | `update:users` | `PATCH /api/v2/users/{id}` |
   | `create:role_members` | `POST /api/v2/users/{id}/roles` |
   | `delete:role_members` | `DELETE /api/v2/users/{id}/roles` |

   > **Principle of least privilege:** Do not grant `create:users`, `delete:users`, or `read:user_idp_tokens` unless specifically required by a future feature.

3. **Note the M2M app's `Client ID` and `Client Secret`** for secrets configuration below.

---

### Required NuGet Packages

Add to `Directory.Packages.props` (centralized version management per repo convention):

```xml
<PackageVersion Include="Auth0.ManagementApi" Version="7.x.x" />
```

Add reference in `src/Web/Web.csproj` (or a future `src/Domain/` service project):

```xml
<PackageReference Include="Auth0.ManagementApi" />
```

> **Version note:** At time of writing, `Auth0.ManagementApi` 7.x targets .NET 6+. Pin to the latest stable in `Directory.Packages.props` per repo convention — do NOT add a version attribute in the `.csproj`.

---

### Required Secrets

Two new secrets are needed, distinct from the existing `Auth0:ClientId` / `Auth0:ClientSecret` (which are the OIDC web app credentials):

| Key | Description |
|---|---|
| `Auth0Management:ClientId` | Client ID of the M2M application |
| `Auth0Management:ClientSecret` | Client Secret of the M2M application |
| `Auth0Management:Domain` | Same as `Auth0:Domain` (can share or reference) |
| `Auth0Management:Audience` | `https://{your-tenant}.auth0.com/api/v2/` |

**Development (User Secrets):**
```bash
dotnet user-secrets set "Auth0Management:ClientId"     "YOUR_M2M_CLIENT_ID"       --project src/Web
dotnet user-secrets set "Auth0Management:ClientSecret" "YOUR_M2M_CLIENT_SECRET"   --project src/Web
dotnet user-secrets set "Auth0Management:Domain"       "your-tenant.auth0.com"    --project src/Web
dotnet user-secrets set "Auth0Management:Audience"     "https://your-tenant.auth0.com/api/v2/" --project src/Web
```

**Production (Azure Key Vault):**
Store as Key Vault secrets with names matching the double-underscore convention for .NET config binding:
- `Auth0Management--ClientId`
- `Auth0Management--ClientSecret`
- `Auth0Management--Domain`
- `Auth0Management--Audience`

The existing `KeyVault:Uri` in `appsettings.json` already wires up Key Vault via `src/Web/Program.cs`; new secrets are picked up automatically once added to the vault.

> ⚠️ **Never** commit actual M2M credentials to `appsettings.json`, `appsettings.Development.json`, or any source-controlled file. Follow the same pattern as the existing `Auth0:ClientSecret` — placeholder only in checked-in config.

---

### Token Caching Strategy

Auth0 Management API access tokens (obtained via the client credentials flow) have a configurable TTL, defaulting to **86,400 seconds (24 hours)**. The `ManagementApiClient` does **not** cache tokens internally — the application must manage caching.

**Recommended approach using `IMemoryCache`** (already available in the app):

```csharp
// Auth0ManagementTokenCache.cs  (src/Web/Auth/ or a future service project)
public sealed class Auth0ManagementTokenCache
{
    private const string CacheKey = "Auth0ManagementApiToken";
    private readonly IMemoryCache _cache;
    private readonly Auth0ManagementOptions _options;
    private readonly HttpClient _httpClient;

    public async Task<string> GetTokenAsync(CancellationToken ct = default)
    {
        if (_cache.TryGetValue(CacheKey, out string? cachedToken))
            return cachedToken!;

        var token = await FetchTokenFromAuth0Async(ct);

        // Cache with a safety margin: expire 5 minutes before actual TTL
        _cache.Set(CacheKey, token.AccessToken,
            TimeSpan.FromSeconds(token.ExpiresIn - 300));

        return token.AccessToken;
    }
}
```

**Token acquisition (client credentials flow):**
```http
POST https://{domain}/oauth/token
Content-Type: application/x-www-form-urlencoded

grant_type=client_credentials
&client_id={AUTH0_MANAGEMENT_CLIENT_ID}
&client_secret={AUTH0_MANAGEMENT_CLIENT_SECRET}
&audience=https://{domain}/api/v2/
```

Response:
```json
{
  "access_token": "eyJ...",
  "token_type": "Bearer",
  "expires_in": 86400
}
```

**`ManagementApiClient` wiring:**
```csharp
// In DI setup (Program.cs or a dedicated extension method):
services.AddSingleton<Auth0ManagementTokenCache>();
services.AddTransient<ManagementApiClient>(sp =>
{
    var cache = sp.GetRequiredService<Auth0ManagementTokenCache>();
    var options = sp.GetRequiredService<IOptions<Auth0ManagementOptions>>().Value;
    var token = cache.GetTokenAsync().GetAwaiter().GetResult(); // or use factory pattern
    return new ManagementApiClient(token, new Uri($"https://{options.Domain}/api/v2"));
});
```

> **Better pattern:** Use a typed `IManagementApiClientFactory` that resolves the token asynchronously and creates a fresh `ManagementApiClient` per logical operation scope, avoiding async-over-sync and scoped lifetime pitfalls.

---

### Rate Limit Strategy

Auth0 Management API enforces rate limits per tenant tier:

| Tier | Rate Limit | Notes |
|---|---|---|
| Free | ~2 requests/second (endpoint-dependent) | Bursts up to ~10 may be allowed briefly |
| Developer Pro+ | Higher; see Auth0 docs for per-endpoint limits | |
| Enterprise | Custom SLAs | |

Rate limit responses return **HTTP 429 Too Many Requests** with a `Retry-After` header (seconds to wait).

**Recommended Polly retry policy** (to be added when implementing the service):

```csharp
services.AddHttpClient<IAuth0ManagementService, Auth0ManagementService>()
    .AddPolicyHandler(Policy
        .HandleResult<HttpResponseMessage>(r => r.StatusCode == HttpStatusCode.TooManyRequests)
        .WaitAndRetryAsync(
            retryCount: 3,
            sleepDurationProvider: (attempt, result, ctx) =>
            {
                // Respect Retry-After header when present
                if (result?.Result?.Headers.RetryAfter?.Delta is { } retryAfter)
                    return retryAfter + TimeSpan.FromMilliseconds(100);
                return TimeSpan.FromSeconds(Math.Pow(2, attempt)); // exponential back-off
            },
            onRetryAsync: (result, delay, attempt, ctx) =>
            {
                logger.LogWarning("Auth0 Management API rate limited. Retry {Attempt} in {Delay}s.", attempt, delay.TotalSeconds);
                return Task.CompletedTask;
            }));
```

**Pagination strategy for list endpoints:**

All list endpoints (`GET /api/v2/users`, `GET /api/v2/roles`, etc.) support:

| Parameter | Default | Max |
|---|---|---|
| `per_page` | 50 | 100 |
| `page` | 0 | — |
| `include_totals` | false | — |

Example paginated request via SDK:
```csharp
var users = await managementClient.Users.GetAllAsync(new GetUsersRequest(),
    new PaginationInfo(pageNo: 0, perPage: 100, includeTotals: true));
// users.Total gives total count; iterate pages until users.Users.Count < perPage
```

For bulk operations, **process pages sequentially with a small delay** (e.g., 100ms between pages) to stay within rate limits, rather than launching parallel requests.

---

### API Endpoints to Use

All endpoints are relative to `https://{domain}/api/v2/`.

| Operation | Method | Endpoint | Required Scope |
|---|---|---|---|
| List all users | GET | `/users?per_page=100&page={n}&include_totals=true` | `read:users` |
| Get user by ID | GET | `/users/{user_id}` | `read:users` |
| List all roles | GET | `/roles?per_page=100&page={n}&include_totals=true` | `read:roles` |
| Get role by ID | GET | `/roles/{role_id}` | `read:roles` |
| Get users for a role | GET | `/roles/{role_id}/users?per_page=100` | `read:role_members` |
| Assign roles to user | POST | `/users/{user_id}/roles` | `create:role_members` |
| Remove roles from user | DELETE | `/users/{user_id}/roles` | `delete:role_members` |

**Request body for role assignment:**
```json
{
  "roles": ["rol_XXXXXXXXXXXXXX"]
}
```

**`ManagementApiClient` equivalents:**
```csharp
// Assign roles
await client.Users.AssignRolesAsync(userId, new AssignRolesRequest
{
    Roles = new[] { roleId }
});

// Remove roles
await client.Users.RemoveRolesAsync(userId, new AssignRolesRequest
{
    Roles = new[] { roleId }
});

// Get roles for a user
var roles = await client.Users.GetRolesAsync(userId, new PaginationInfo());

// List all roles in tenant
var allRoles = await client.Roles.GetAllAsync(new GetRolesRequest());
```

---

### Mapping Auth0 Role IDs to Application Roles

Auth0 roles have an internal ID (e.g., `rol_XXXXXXXXXXXXXX`) that differs from the display name (`Admin`, `User`). The implementation will need to resolve role IDs by name at startup or cache the mapping:

```csharp
// Startup: resolve role IDs once and cache
var roles = await client.Roles.GetAllAsync(new GetRolesRequest { NameFilter = "Admin" });
var adminRoleId = roles.First().Id; // store in IMemoryCache or IOptions
```

This avoids hardcoding role IDs (which change per tenant) in application code.

---

## References

- [Auth0 Management API v2 Reference](https://auth0.com/docs/api/management/v2)
- [Auth0 Client Credentials Flow](https://auth0.com/docs/get-started/authentication-and-authorization-flow/client-credentials-flow)
- [Auth0.ManagementApi NuGet Package](https://www.nuget.org/packages/Auth0.ManagementApi)
- [Auth0 Rate Limit Policy](https://auth0.com/docs/troubleshoot/customer-support/operational-policies/rate-limit-policy)
- [Existing auth setup — `src/Web/Auth/`](../../../src/Web/Auth/)
- [Existing secrets strategy — `src/Web/Auth/README.md`](../../../src/Web/Auth/README.md)
