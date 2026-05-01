# Admin User Management Implementation

This reference documents the current Auth0 Management API pattern used by the app. It targets **Auth0.ManagementApi v8** and relies on the SDK's `ClientCredentialsTokenProvider` instead of hand-rolled token fetching.

Replace `YourApp` with your web project's root namespace in the web-layer snippets below.

## Overview

The admin user management feature enables administrators to:

- List Auth0 users
- Read a single user's summary information and assigned roles
- List available Auth0 roles
- Assign and remove roles by role name
- Drive admin UI components such as `Users.razor`, `EditUserRolesModal.razor`, and `UserListTable.razor`

## Prerequisites

1. **Auth0 Management API M2M Application** created in the Auth0 Dashboard
2. **Scopes granted**: `read:users`, `update:users`, `read:roles`, `read:users_app_metadata`, `update:users_app_metadata`
3. **Client ID, Client Secret, Domain, and Audience** from the M2M application
4. **Auth0.ManagementApi v8.x** referenced by the project

## Domain Contracts

### Domain/Features/Admin/Abstractions/IUserManagementService.cs

```csharp
using Domain.Abstractions;
using Domain.Features.Admin.Models;

namespace Domain.Features.Admin.Abstractions;

public interface IUserManagementService
{
Task<Result<IReadOnlyList<AdminUserSummary>>> ListUsersAsync(
int page,
int perPage,
CancellationToken ct);

Task<Result<AdminUserSummary>> GetUserByIdAsync(
string userId,
CancellationToken ct);

Task<Result<bool>> AssignRolesAsync(
string userId,
IEnumerable<string> roleNames,
CancellationToken ct);

Task<Result<bool>> RemoveRolesAsync(
string userId,
IEnumerable<string> roleNames,
CancellationToken ct);

Task<Result<IReadOnlyList<RoleAssignment>>> ListRolesAsync(CancellationToken ct);
}
```

### Domain/Features/Admin/Models/AdminUserSummary.cs

```csharp
namespace Domain.Features.Admin.Models;

public record AdminUserSummary
{
public string UserId { get; init; } = string.Empty;
public string Email { get; init; } = string.Empty;
public string Name { get; init; } = string.Empty;
public string Picture { get; init; } = string.Empty;
public IReadOnlyList<string> Roles { get; init; } = [];
public DateTimeOffset? LastLogin { get; init; }
public bool IsBlocked { get; init; }

public static AdminUserSummary Empty => new();
}
```

### Domain/Features/Admin/Models/RoleAssignment.cs

```csharp
namespace Domain.Features.Admin.Models;

public record RoleAssignment
{
public string RoleId { get; init; } = string.Empty;
public string RoleName { get; init; } = string.Empty;
public string Description { get; init; } = string.Empty;
}
```

## Web Layer Implementation

### Features/Admin/Users/Auth0ManagementOptions.cs

```csharp
namespace YourApp.Features.Admin.Users;

public sealed record Auth0ManagementOptions
{
public const string SectionName = "Auth0Management";

public string ClientId { get; init; } = string.Empty;
public string ClientSecret { get; init; } = string.Empty;
public string Domain { get; init; } = string.Empty;
public string Audience { get; init; } = string.Empty;
}
```

### Features/Admin/Users/UserManagementExtensions.cs

Register the SDK client once, then consume `IManagementApiClient` from the scoped service.

```csharp
using Auth0.ManagementApi;
using Domain.Features.Admin.Abstractions;
using Microsoft.Extensions.Options;

namespace YourApp.Features.Admin.Users;

public static class UserManagementExtensions
{
public static IServiceCollection AddUserManagement(
this IServiceCollection services,
IConfiguration configuration)
{
services.AddMemoryCache();
services.Configure<Auth0ManagementOptions>(
configuration.GetSection(Auth0ManagementOptions.SectionName));

services.AddSingleton<IManagementApiClient>(sp =>
{
var opts = sp.GetRequiredService<IOptions<Auth0ManagementOptions>>().Value;
var audience = string.IsNullOrWhiteSpace(opts.Audience) ? null : opts.Audience;

return new ManagementClient(new ManagementClientOptions
{
Domain = opts.Domain,
TokenProvider = new ClientCredentialsTokenProvider(
opts.Domain,
opts.ClientId,
opts.ClientSecret,
audience: audience)
});
});

services.AddScoped<IUserManagementService, UserManagementService>();
return services;
}
}
```

### Features/Admin/Users/UserManagementService.cs

Key differences from the old v7 pattern:

- Use `IManagementApiClient`, not `ManagementApiClient`
- Use `Auth0.ManagementApi.Users` request/response types
- Let the SDK manage M2M tokens internally
- Keep app-level caching for user summaries, role lists, and role-name lookups

```csharp
using System.Buffers.Binary;
using System.Text.Json;
using Auth0.ManagementApi;
using Auth0.ManagementApi.Users;
using Domain.Abstractions;
using Domain.Features.Admin.Abstractions;
using Domain.Features.Admin.Models;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;

namespace YourApp.Features.Admin.Users;

public sealed class UserManagementService : IUserManagementService
{
private const string RolesCacheKey = "Auth0Management:Roles";
private const string UserListCacheKeyPrefix = "auth0_users_page_";
private const string UserByIdCacheKeyPrefix = "auth0_user_";
private const string RolesListCacheKey = "auth0_roles_list";
private const string UserListVersionKey = "auth0_users_version";

private static readonly TimeSpan UserListTtl = TimeSpan.FromMinutes(5);
private static readonly TimeSpan UserByIdTtl = TimeSpan.FromMinutes(10);
private static readonly TimeSpan RolesListTtl = TimeSpan.FromMinutes(30);

private readonly IMemoryCache _cache;
private readonly IDistributedCache _distributedCache;
private readonly IManagementApiClient _managementClient;
private readonly ILogger<UserManagementService> _logger;

public UserManagementService(
IMemoryCache cache,
IDistributedCache distributedCache,
IManagementApiClient managementClient,
ILogger<UserManagementService> logger)
{
_cache = cache;
_distributedCache = distributedCache;
_managementClient = managementClient;
_logger = logger;
}

public async Task<Result<IReadOnlyList<AdminUserSummary>>> ListUsersAsync(
int page,
int perPage,
CancellationToken ct)
{
var version = await GetUserListVersionAsync(ct).ConfigureAwait(false);
var cacheKey = $"{UserListCacheKeyPrefix}{version}_{page}_{perPage}";
var cached = await GetFromDistributedCacheAsync<List<AdminUserSummary>>(cacheKey, ct)
.ConfigureAwait(false);
if (cached is not null) return Result.Ok<IReadOnlyList<AdminUserSummary>>(cached);

var auth0Page = Math.Max(0, page - 1);
var pager = await _managementClient.Users
.ListAsync(new ListUsersRequestParameters { Page = auth0Page, PerPage = perPage }, null, ct)
.ConfigureAwait(false);

var summaries = await Task.WhenAll(pager.CurrentPage.Items.Select(async user =>
{
var rolesPager = await _managementClient.Users.Roles
.ListAsync(user.UserId!, new ListUserRolesRequestParameters { PerPage = 100 }, null, ct)
.ConfigureAwait(false);

return new AdminUserSummary
{
UserId = user.UserId ?? string.Empty,
Email = user.Email ?? string.Empty,
Name = user.Name ?? user.Email ?? string.Empty,
Picture = user.Picture ?? string.Empty,
Roles = rolesPager.CurrentPage.Items.Select(r => r.Name ?? string.Empty).ToList(),
LastLogin = ParseLastLogin(user.LastLogin),
IsBlocked = user.Blocked ?? false
};
})).ConfigureAwait(false);

var result = summaries.ToList();
await SetInDistributedCacheAsync(cacheKey, result, UserListTtl, ct).ConfigureAwait(false);
return Result.Ok<IReadOnlyList<AdminUserSummary>>(result);
}

public async Task<Result<AdminUserSummary>> GetUserByIdAsync(string userId, CancellationToken ct)
{
var user = await _managementClient.Users
.GetAsync(userId, new GetUserRequestParameters(), null, ct)
.ConfigureAwait(false);

var rolesPager = await _managementClient.Users.Roles
.ListAsync(userId, new ListUserRolesRequestParameters { PerPage = 100 }, null, ct)
.ConfigureAwait(false);

return Result.Ok(new AdminUserSummary
{
UserId = user.UserId ?? string.Empty,
Email = user.Email ?? string.Empty,
Name = user.Name ?? user.Email ?? string.Empty,
Picture = user.Picture ?? string.Empty,
Roles = rolesPager.CurrentPage.Items.Select(r => r.Name ?? string.Empty).ToList(),
LastLogin = ParseLastLogin(user.LastLogin),
IsBlocked = user.Blocked ?? false
});
}

public async Task<Result<bool>> AssignRolesAsync(string userId, IEnumerable<string> roleNames, CancellationToken ct)
{
var roleMap = await GetRoleMapAsync(ct).ConfigureAwait(false);
var roleIds = roleNames.Select(name => roleMap[name]).ToArray();

await _managementClient.Users.Roles
.AssignAsync(userId, new AssignUserRolesRequestContent { Roles = roleIds }, null, ct)
.ConfigureAwait(false);

return Result.Ok(true);
}

public async Task<Result<bool>> RemoveRolesAsync(string userId, IEnumerable<string> roleNames, CancellationToken ct)
{
var roleMap = await GetRoleMapAsync(ct).ConfigureAwait(false);
var roleIds = roleNames.Select(name => roleMap[name]).ToArray();

await _managementClient.Users.Roles
.DeleteAsync(userId, new DeleteUserRolesRequestContent { Roles = roleIds }, null, ct)
.ConfigureAwait(false);

return Result.Ok(true);
}

public async Task<Result<IReadOnlyList<RoleAssignment>>> ListRolesAsync(CancellationToken ct)
{
var pager = await _managementClient.Roles
.ListAsync(new ListRolesRequestParameters { PerPage = 100 }, null, ct)
.ConfigureAwait(false);

return Result.Ok<IReadOnlyList<RoleAssignment>>(pager.CurrentPage.Items
.Select(r => new RoleAssignment
{
RoleId = r.Id ?? string.Empty,
RoleName = r.Name ?? string.Empty,
Description = r.Description ?? string.Empty
})
.ToList());
}

private async Task<T?> GetFromDistributedCacheAsync<T>(string key, CancellationToken ct) =>
(await _distributedCache.GetAsync(key, ct).ConfigureAwait(false)) is { } bytes
? JsonSerializer.Deserialize<T>(bytes)
: default;

private async Task SetInDistributedCacheAsync<T>(string key, T value, TimeSpan ttl, CancellationToken ct) =>
await _distributedCache.SetAsync(
key,
JsonSerializer.SerializeToUtf8Bytes(value),
new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = ttl },
ct).ConfigureAwait(false);

private async Task<long> GetUserListVersionAsync(CancellationToken ct)
{
var bytes = await _distributedCache.GetAsync(UserListVersionKey, ct).ConfigureAwait(false);
return bytes is null ? 0L : BinaryPrimitives.ReadInt64LittleEndian(bytes);
}

private async Task<Dictionary<string, string>> GetRoleMapAsync(CancellationToken ct)
{
var map = await _cache.GetOrCreateAsync(RolesCacheKey, async entry =>
{
var pager = await _managementClient.Roles
.ListAsync(new ListRolesRequestParameters { PerPage = 100 }, null, ct)
.ConfigureAwait(false);

entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30);
return pager.CurrentPage.Items
.Where(r => r.Name is not null && r.Id is not null)
.ToDictionary(r => r.Name!, r => r.Id!, StringComparer.OrdinalIgnoreCase);
}).ConfigureAwait(false);

return map ?? [];
}

private static DateTimeOffset? ParseLastLogin(UserDateSchema? lastLogin)
{
if (lastLogin is null) return null;
return lastLogin.TryGetString(out var s) && DateTimeOffset.TryParse(s, out var dto) ? dto : null;
}
}
```

**Note**: The real app keeps additional logging, validation, and cache-invalidation behavior around these methods. The key point is the v8 API shape and token-provider registration.

## UI Components

The current app composes the admin user experience from these pieces:

- `Components/Pages/Admin/Users.razor` — page entry point, protected by `AdminPolicy`
- `Components/Admin/Users/UserListTable.razor` — tabular user display
- `Components/Admin/Users/EditUserRolesModal.razor` — add/remove role workflow
- `Components/Admin/Users/RoleBadge.razor` — role chip rendering
- `Components/Admin/Users/UserAuditLogPanel.razor` — optional audit detail display

## Configuration

### appsettings.json

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

### User Secrets (Development)

```bash
dotnet user-secrets set "Auth0Management:ClientId" "YOUR_M2M_CLIENT_ID"
dotnet user-secrets set "Auth0Management:ClientSecret" "YOUR_M2M_CLIENT_SECRET"
dotnet user-secrets set "Auth0Management:Domain" "your-tenant.auth0.com"
dotnet user-secrets set "Auth0Management:Audience" "https://your-tenant.auth0.com/api/v2/"
```

## Registration in Program.cs

```csharp
using YourApp.Features.Admin.Users;

builder.Services.AddUserManagement(builder.Configuration);
```

## Testing

1. Log in as an admin user
2. Navigate to `/admin/users`
3. Verify the user list loads
4. Open the role editor and assign/remove roles
5. Verify the new roles appear after cache invalidation

## Troubleshooting

### 401 Unauthorized from the Management API

- Confirm the M2M app is authorized for the Auth0 Management API
- Verify `Domain`, `ClientId`, `ClientSecret`, and `Audience`
- Check the `ClientCredentialsTokenProvider` configuration in `AddUserManagement()`

### 403 Forbidden

- Ensure the M2M app has the required scopes
- Confirm the signed-in app user has the `Admin` role before exposing admin UI

### Empty Role List

- Verify roles exist in the tenant
- Confirm `ListRolesAsync` is reaching the Auth0 tenant you expect
