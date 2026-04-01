# Admin User Management

## Overview

The Admin User Management feature provides administrators with a centralized interface to manage application users and their assigned Auth0 roles. This feature is designed for administrators only and enables:

- **List Users**: View all application users with pagination
- **Assign Roles**: Add Auth0 roles (such as Admin or User) to specific users
- **Remove Roles**: Revoke Auth0 roles from users
- **Audit Log**: Review a complete log of all role changes with timestamps and the acting administrator

The feature is accessible only to users with the Admin role and is protected by the `AdminPolicy` authorization policy.

## Prerequisites

### Auth0 Machine-to-Machine (M2M) Application Setup

The Admin User Management feature requires a dedicated M2M (Machine-to-Machine) application in Auth0 to call the Management API v2 on behalf of the application.

#### Create an M2M Application in Auth0

1. Log in to the **Auth0 Dashboard** at https://manage.auth0.com
2. Navigate to **Applications > Applications** in the left sidebar
3. Click **Create Application** and select **Machine-to-Machine Apps**
4. Enter an application name (e.g., "IssueTrackerApp - User Management")
5. Click **Create**

#### Authorize the M2M Application

1. In the application details page, navigate to the **APIs** tab
2. Find and click **Auth0 Management API** in the list
3. Click the toggle to enable it
4. Expand the permissions list and select the following scopes:
   - `read:users` — Read user information
   - `update:users` — Update user email and other profile fields
   - `read:roles` — List available roles
   - `create:role_members` — Assign roles to users
   - `delete:role_members` — Remove roles from users
5. Click **Save**

#### Obtain M2M Credentials

1. Navigate to the **Settings** tab of the M2M application
2. Copy the following values:
   - **Client ID**
   - **Client Secret**
3. Also note your **Auth0 tenant domain** (shown in the top-right of the Auth0 Dashboard, e.g., `your-tenant.auth0.com`)

The Management API **Audience** is always: `https://your-tenant.auth0.com/api/v2/`

**⚠️ Security Note:** Never commit the Client Secret to source control. Store it securely in User Secrets (development) or Azure Key Vault (production).

## Local Development Setup

### Configure Secrets Using dotnet user-secrets

Use the .NET user-secrets tool to store Auth0 M2M credentials securely on your development machine:

```bash
cd src/Web

dotnet user-secrets set "Auth0Management:ClientId" "your-m2m-client-id"
dotnet user-secrets set "Auth0Management:ClientSecret" "your-m2m-client-secret"
dotnet user-secrets set "Auth0Management:Domain" "your-tenant.auth0.com"
dotnet user-secrets set "Auth0Management:Audience" "https://your-tenant.auth0.com/api/v2/"
```

Replace the placeholder values with the actual credentials from your Auth0 M2M application.

### Verify Configuration

When the application starts, it will bind these secrets to the `Auth0Management` configuration section. If any required secret is missing, the application may fail during startup or when attempting to access the User Management page.

To verify the configuration is correct:
1. Run the application: `dotnet run --project src/AppHost`
2. Log in with an Admin user
3. Navigate to `/admin/users` — you should see the list of users from your Auth0 tenant

## Features

### List Users

The main Admin User Management page (`/admin/users`) displays all users from your Auth0 tenant in a paginated table.

**Fields displayed:**
- User ID (Auth0 identifier, e.g., `auth0|abc123`)
- Email address
- Display name
- Created date
- Assigned roles (comma-separated list)

**Pagination:**
- Navigate between pages of users (default: 10 users per page)
- Maximum page size: 100 users per page

### Assign Role

From the user detail page or inline actions, administrators can assign one or more Auth0 roles to a user.

**Steps:**
1. Click on a user in the list or select "Assign Role" from the user row
2. Select one or more roles from the "Available Roles" dropdown
3. Click **Assign**
4. The assignment is logged to the audit log with the timestamp and acting administrator

**Result:**
- The role is immediately applied in Auth0
- The user's session is refreshed on their next login to reflect the new role
- A `RoleChangeAuditEntry` is created in the audit log

### Remove Role

Administrators can revoke assigned roles from users.

**Steps:**
1. Click on a user in the list
2. Click **Remove** next to the role to be revoked
3. Confirm the action
4. The revocation is logged to the audit log

**Result:**
- The role is immediately removed in Auth0
- The user's session is updated on their next login
- A `RoleChangeAuditEntry` is created in the audit log

### Audit Log

The audit log tracks all role changes (assignments and removals) with the following information:

- **Target User ID**: Auth0 identifier of the user whose roles were modified
- **Action**: "RoleAssigned" or "RoleRemoved"
- **Role Name**: The name of the role that was assigned or removed
- **Performed By**: Email address of the administrator who performed the action
- **Timestamp**: Date and time (UTC) of the action
- **Audit Trail**: All historical changes are retained for compliance and security auditing

Access the audit log from the User Management admin page or query the MongoDB collection `auditlogs` directly for reporting.

## Architecture

### Components

#### IUserManagementService

Interface in `Domain.Features.Admin.Abstractions.IUserManagementService`:

- `ListUsersAsync(page, perPage)` — Returns a paginated list of `AdminUserSummary` from Auth0
- `GetUserByIdAsync(userId)` — Returns a single user with assigned roles
- `AssignRolesAsync(userId, roleNames)` — Assigns one or more roles by name
- `RemoveRolesAsync(userId, roleNames)` — Removes one or more roles
- `ListRolesAsync()` — Returns all available roles in the Auth0 tenant

#### UserManagementService

Implementation in `Web.Features.Admin.Users.UserManagementService`:

- Uses the **Auth0.ManagementApi** NuGet package (SDK v7.x)
- Obtains M2M access tokens via the OAuth 2.0 client credentials flow
- **Token Caching**: M2M tokens are cached in `IMemoryCache` with a 24-hour TTL minus a 5-minute safety margin
- **Role Caching**: Role lists are cached for 30 minutes to avoid repeated lookups
- **Error Handling**: All operations return `Result<T>` to provide explicit error handling without exceptions

#### Auth0ManagementOptions

Configuration record in `Web.Features.Admin.Users.Auth0ManagementOptions`:

```csharp
public sealed record Auth0ManagementOptions
{
    public const string SectionName = "Auth0Management";
    public string ClientId { get; init; }
    public string ClientSecret { get; init; }
    public string Domain { get; init; }
    public string Audience { get; init; }
}
```

Bound from the `Auth0Management` configuration section (User Secrets, appsettings.json, or environment variables).

#### Audit Log Models

`Domain.Features.Admin.Models.RoleChangeAuditEntry`:

```csharp
public class RoleChangeAuditEntry
{
    public ObjectId Id { get; set; }
    public string TargetUserId { get; set; }     // Auth0 user ID
    public string ActionType { get; set; }        // "RoleAssigned" or "RoleRemoved"
    public string RoleName { get; set; }
    public string PerformedBy { get; set; }       // Admin email
    public DateTime Timestamp { get; set; }       // UTC
}
```

#### AuditLogRepository

Interface in `Domain.Features.Admin.Abstractions.IAuditLogRepository`:

- `AddAsync(entry)` — Persists a new audit log entry to MongoDB
- `GetByTargetUserAsync(targetUserId)` — Returns all audit log entries for a user

Implementation in `Persistence.MongoDb.Repositories.AuditLogRepository`:

- Stores entries in the MongoDB collection `auditlogs`
- Supports querying by target user for compliance audits

### Data Flow

1. **User Navigates to `/admin/users`**
   - The page component (`Admin/Users.razor`) is rendered with `AdminPolicy` authorization

2. **List Users**
   - Page calls `UserManagementService.ListUsersAsync()`
   - Service obtains an M2M access token (cached if available)
   - Calls Auth0 Management API `/users` endpoint
   - Returns paginated `AdminUserSummary` list

3. **Assign Role**
   - Admin selects a user and role, clicks "Assign"
   - Page calls `UserManagementService.AssignRolesAsync(userId, roleNames)`
   - Service resolves role names to role IDs via `ListRolesAsync()` (cached)
   - Calls Auth0 Management API `POST /users/{id}/roles` endpoint
   - Service publishes a `RoleAssignedEvent` domain event
   - `AuditLogWriterService` handler receives the event and creates a `RoleChangeAuditEntry`
   - Entry is persisted via `AuditLogRepository.AddAsync()`

4. **Remove Role**
   - Admin clicks "Remove" next to a role
   - Similar flow to Assign Role, but calls `DELETE /users/{id}/roles`
   - Publishes `RoleRemovedEvent` and creates an audit log entry

5. **View Audit Log**
   - Page calls `AuditLogRepository.GetByTargetUserAsync(userId)`
   - Returns all historical role changes for the user, ordered by timestamp

### CQRS Pattern

The feature follows the CQRS (Command Query Responsibility Segregation) pattern using MediatR:

- **Queries**: `Domain.Features.Admin.Users.Queries.*` — e.g., `ListUsersQuery`, `GetUserQuery`
- **Commands**: `Domain.Features.Admin.Users.Commands.*` — e.g., `AssignRoleCommand`, `RemoveRoleCommand`
- **Handlers**: `*Handler` classes implement `IRequestHandler<>` or `INotificationHandler<>`
- **Validators**: FluentValidation validators ensure command/query data is valid before dispatch

Each handler is `sealed` and injects dependencies via constructor.

## Security

### Authorization

The Admin User Management feature is protected by the **AdminPolicy** authorization policy:

```csharp
app.MapAdminGroup()
   .RequireAuthorization("AdminPolicy");
```

- Only users with the **Admin** role (as defined in Auth0 or the application's role mappings) can access `/admin/users`
- Attempting to access the page without Admin role results in a **403 Forbidden** response
- The `Auth0ClaimsTransformation` middleware ensures the Admin role claim is properly set on the user's principal

### Secrets Management

M2M credentials are sensitive and must never be committed to source control:

- **Development**: Use `dotnet user-secrets` to store credentials locally (stored in `~/.microsoft/usersecrets/` on the developer's machine)
- **Production**: Store credentials in **Azure Key Vault** and configure the application to load from Key Vault
- **CI/CD**: Never hardcode secrets in GitHub Actions workflows; use GitHub Secrets to inject them at runtime

### Audit Trail

All role changes are logged to the audit log for compliance and security:

- Every assignment and removal of a role is recorded with:
  - The user ID of the target user
  - The role name
  - The email of the administrator who performed the action
  - The exact timestamp (UTC)
- Audit logs are immutable and retained indefinitely in MongoDB
- Administrators can query the audit log to answer questions like:
  - "Who assigned the Admin role to this user, and when?"
  - "What roles did this user have on a specific date?"
  - "Which users did this administrator modify roles for?"

### Rate Limiting

Auth0 Management API enforces rate limits (HTTP 429). Implement a Polly retry policy (see ADR #130) in a follow-up task to handle transient failures gracefully.

### Best Practices

1. **Audit logs regularly** — Review role change logs quarterly to detect unauthorized or suspicious changes
2. **Principle of least privilege** — Only assign roles that users actually need
3. **Segregate M2M credentials** — The M2M application should have only the minimum necessary scopes (the 5 listed above)
4. **Monitor M2M token usage** — Check Auth0 logs for unusual M2M token requests
5. **Rotate M2M secrets periodically** — Follow your organization's secret rotation policy

## Troubleshooting

### Issue: "Unauthorized" error when accessing `/admin/users`

**Cause:** User does not have the Admin role.

**Resolution:** Use Auth0 Dashboard to assign the Admin role to the user, or ensure the role claim mapper in `Auth0ClaimsTransformation.cs` is correctly configured.

### Issue: "Invalid configuration" or null reference on startup

**Cause:** Missing M2M configuration secrets.

**Resolution:** Verify all four secrets are set:
```bash
dotnet user-secrets list --project src/Web
```

### Issue: "Auth0 Management API call failed" errors

**Cause:** Incorrect M2M credentials or M2M application lacks required scopes.

**Resolution:**
1. Verify M2M credentials in User Secrets match Auth0 Dashboard
2. Verify the M2M application has the required scopes (read:users, update:users, read:roles, create:role_members, delete:role_members)
3. Check Auth0 logs for API errors
4. Consider adding retry logic for transient failures (ADR #130)

### Issue: Role changes don't appear immediately for the user

**Cause:** Auth0 sessions are cached; the user must log out and log back in for new roles to take effect.

**Resolution:** Inform the user to log out and log back in to refresh their session. Alternatively, consider implementing a "Force Logout" feature to refresh user sessions immediately.

## Related Documentation

- [SECURITY.md](../SECURITY.md) — General security practices
- [ARCHITECTURE.md](../ARCHITECTURE.md) — System architecture overview
- [CONTRIBUTING.md](../CONTRIBUTING.md) — Development guidelines
