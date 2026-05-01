---
name: implement-auth0-authentication
description: 'Add Auth0 authentication and authorization to a Blazor Server or Blazor Web App with interactive server components. Includes role-based authorization, claims transformation, user profile page, and admin user management via Auth0 Management API. Prompts for Auth0 tenant credentials, client secrets, callback URLs, role claim namespace, and Management API M2M application. Use for implementing OAuth 2.0 OIDC login, role management, user administration, and secure Blazor authentication with Auth0.'
argument-hint: '[optional: path to Blazor project directory]'
---

# Implement Auth0 Authentication for Blazor

Adds complete Auth0 authentication and authorization infrastructure to a Blazor Server or Blazor Web App with interactive server components, including role-based access control, a profile page, and optional admin user management.

## When to Use

- Adding Auth0 authentication to a new or existing Blazor Server or Blazor Web App
- Implementing OAuth 2.0 OIDC login/logout with Auth0
- Setting up role-based authorization mapped from Auth0 claims
- Creating a user profile page that displays claims and roles
- Building admin pages that manage Auth0 users and roles via the Management API
- Migrating from local cookie-only auth to Auth0 OIDC
- Hardening login/logout flows with antiforgery and local-return-url validation

## Prerequisites

Before starting, ensure you have:

1. **Auth0 Account**: Active Auth0 tenant
2. **Web Application Configured in Auth0**:
   - Application Type: Regular Web Application
   - Allowed Callback URLs configured (for example `https://localhost:5001/callback`)
   - Allowed Logout URLs configured (for example `https://localhost:5001/`)
   - Domain, Client ID, and Client Secret available
3. **Auth0 Management API M2M Application** (for admin user management features):
   - Application Type: Machine to Machine
   - Authorized for Auth0 Management API
   - Scopes granted: `read:users`, `update:users`, `read:roles`, `read:users_app_metadata`, `update:users_app_metadata`
   - Client ID, Client Secret, Domain, and Audience available
4. **Role Configuration in Auth0**:
   - Roles created (for example `Admin`, `User`)
   - Auth0 Action or Rule configured to add roles to the ID token with a custom namespace such as `https://yourapp.com/roles`

## What This Skill Does

1. **Gathers Auth0 configuration** from the user (see [Configuration Prompts](./references/configuration-prompts.md))
2. **Adds or updates Auth0 packages**:
   - `Auth0.AspNetCore.Authentication`
   - `Auth0.ManagementApi` (if admin user management is requested)
   - If the repo uses Central Package Management, update `Directory.Packages.props` instead of adding versions in individual project files
3. **Configures authentication** in `Program.cs` with `AddAuth0WebAppAuthentication`
4. **Creates Auth infrastructure**:
   - `Auth/Auth0Options.cs` — configuration model
   - `Auth/Auth0ClaimsTransformation.cs` — maps Auth0 roles to ASP.NET Core `ClaimTypes.Role`
   - `Auth/AuthorizationRoles.cs` — role constants (`Admin`, `User`)
   - `Auth/AuthorizationPolicies.cs` — policy constants (`AdminPolicy`, `UserPolicy`)
5. **Maps explicit login/logout endpoints**:
   - `GET /account/login` validates the `returnUrl` before calling the Auth0 challenge
   - `POST /account/logout` uses antiforgery and signs out of both Auth0 and the local cookie
   - `/callback` remains handled by the Auth0 SDK
6. **Creates login/logout UI components**:
   - `Components/Layout/LoginDisplay.razor` — login/logout buttons with user greeting
   - `Components/Layout/LoginComponent.razor` — minimal login/logout form
7. **Creates a user profile page**:
   - `Components/User/Profile.razor` — displays claims, roles, profile picture, and email
8. **Creates admin user management** (optional):
   - `Features/Admin/Users/Auth0ManagementOptions.cs`
   - `Features/Admin/Users/UserManagementExtensions.cs`
   - `Features/Admin/Users/UserManagementService.cs`
   - `Domain/Features/Admin/Abstractions/IUserManagementService.cs`
   - `Domain/Features/Admin/Models/AdminUserSummary.cs`
   - `Domain/Features/Admin/Models/RoleAssignment.cs`
   - `Components/Pages/Admin/Users.razor`
   - `Components/Admin/Users/EditUserRolesModal.razor`
   - `Components/Admin/Users/UserListTable.razor`
   - `Components/Admin/Users/RoleBadge.razor`
   - `Components/Admin/Users/UserAuditLogPanel.razor` (optional)
9. **Configures `appsettings.json` placeholders and user secrets** for sensitive values
10. **Adds cascading authentication state, middleware, and verification guidance**

## Procedure

### Step 1: Gather Configuration

Prompt the user for the required values before making changes. Use [Configuration Prompts](./references/configuration-prompts.md).

**Auth0 Web Application (OIDC):**
- `Auth0:Domain`
- `Auth0:ClientId`
- `Auth0:ClientSecret`
- `Auth0:RoleClaimNamespace`

**Auth0 Management API M2M Application (optional):**
- `Auth0Management:ClientId`
- `Auth0Management:ClientSecret`
- `Auth0Management:Domain`
- `Auth0Management:Audience`

**Callback and Logout URLs:**
- Callback URL (for example `https://localhost:5001/callback`)
- Logout URL (for example `https://localhost:5001/`)

**Feature Selection:**
- Whether to include admin user management pages
- Whether to create a user profile page

### Step 2: Add the Required Packages

If the repo does **not** use Central Package Management:

```bash
dotnet add package Auth0.AspNetCore.Authentication
```

If admin user management is requested:

```bash
dotnet add package Auth0.ManagementApi
```

If the repo **does** use Central Package Management, update `Directory.Packages.props` instead of setting package versions in project files.

### Step 3: Create Auth Infrastructure

Create the following files in the `Auth/` folder if they do not already exist:

- `Auth/Auth0Options.cs`
- `Auth/Auth0ClaimsTransformation.cs`
- `Auth/AuthorizationRoles.cs`
- `Auth/AuthorizationPolicies.cs`

See [Auth0 Implementation](./references/auth-implementation.md) for the current code patterns.

### Step 4: Configure Authentication in Program.cs

Add the Auth0 authentication configuration shown in [Program.cs Configuration](./references/program-configuration.md), including:

- Cookie auth in `Testing`
- `AddAuth0WebAppAuthentication` for non-test environments
- `IClaimsTransformation` registration
- Authorization policy registration
- `AddCascadingAuthenticationState()`
- Explicit `UseAuthentication()`, `UseAuthorization()`, and `UseAntiforgery()` middleware

### Step 5: Map Secure Login/Logout Endpoints

Create the endpoint pattern documented in [Program.cs Configuration](./references/program-configuration.md):

- `GET /account/login` accepts `returnUrl`, rejects non-local redirect targets, and challenges the Auth0 scheme
- `POST /account/logout` requires authorization, includes antiforgery, and signs out of both Auth0 and the cookie scheme
- In `Testing`, add a lightweight `/test/login` endpoint so E2E tests do not depend on Auth0

### Step 6: Create Login/Logout UI Components

Use the secure UI patterns in [Auth0 Implementation](./references/auth-implementation.md):

- `Components/Layout/LoginDisplay.razor`
- `Components/Layout/LoginComponent.razor`

Prefer base-relative `returnUrl` values so they pass local-url validation.

### Step 7: Create the User Profile Page

If requested, add `Components/User/Profile.razor` using the example in [Auth0 Implementation](./references/auth-implementation.md).

### Step 8: Create Admin User Management

If admin user management was requested, create:

- `Domain/Features/Admin/Abstractions/IUserManagementService.cs`
- `Domain/Features/Admin/Models/AdminUserSummary.cs`
- `Domain/Features/Admin/Models/RoleAssignment.cs`
- `Features/Admin/Users/Auth0ManagementOptions.cs`
- `Features/Admin/Users/UserManagementExtensions.cs`
- `Features/Admin/Users/UserManagementService.cs`
- Admin UI components under `Components/Pages/Admin/Users.razor` and `Components/Admin/Users/`

The current reference implementation targets **Auth0.ManagementApi v8** and uses:

- `IManagementApiClient`
- `ManagementClient`
- `ManagementClientOptions`
- `ClientCredentialsTokenProvider`
- `Auth0.ManagementApi.Users` request/response types

See [Admin User Management](./references/admin-user-management.md).

### Step 9: Configure appsettings and User Secrets

Add placeholder configuration to `appsettings.json`:

```json
{
  "Auth0": {
    "Domain": "",
    "ClientId": "",
    "ClientSecret": "",
    "RoleClaimNamespace": ""
  },
  "Auth0Management": {
    "ClientId": "",
    "ClientSecret": "",
    "Domain": "",
    "Audience": ""
  }
}
```

Store secrets with `dotnet user-secrets` during development. Use Azure Key Vault, environment variables, or another secure secret store in production.

### Step 10: Verify the Integration

1. Ensure `UseAuthentication()`, `UseAuthorization()`, and `UseAntiforgery()` are present in the middleware pipeline
2. Ensure `AddCascadingAuthenticationState()` is registered
3. Verify Auth0 callback and logout URLs match the deployed app URLs
4. Test login/logout flows
5. Verify role claims are mapped correctly on the profile page
6. If admin features are enabled, verify user list and role assignment flows

## Post-Implementation Testing

### Test Login Flow

1. Navigate to the application
2. Click **Log in** and verify the app redirects to Auth0 Universal Login
3. Authenticate with Auth0 credentials
4. Verify the app returns to the requested local page
5. Verify the user name appears in navigation

### Test Role Mapping

1. Log in with a user that has roles assigned in Auth0
2. Navigate to `/profile`
3. Verify roles appear under **Roles & Permissions**
4. Verify the standard role claim type contains the mapped roles

### Test Authorization

1. Log in with a non-admin user
2. Attempt to access `/admin/users` and verify access is denied
3. Log in with an admin user
4. Verify `/admin/users` loads successfully

### Test Admin User Management

1. Log in as an admin
2. Navigate to `/admin/users`
3. Verify the user list loads
4. Open the role editor for a user
5. Assign and remove roles
6. Verify changes persist in Auth0

## Troubleshooting

### Roles Not Appearing

- **Cause**: `Auth0:RoleClaimNamespace` does not match the namespace in the Auth0 Action or Rule
- **Fix**: Update `Auth0:RoleClaimNamespace` to match exactly
- **Fallback**: `Auth0ClaimsTransformation` also checks the standard `roles` claim and auto-detects namespaced claims ending in `/roles`

### Login Redirects to the Wrong Page

- **Cause**: The UI passed an absolute URL or another non-local redirect target
- **Fix**: Generate a base-relative `returnUrl` and keep the local-URL validation in the login endpoint

### Management API Calls Fail

- **Cause**: The M2M app is missing scopes or has the wrong audience/domain
- **Fix**: Re-check the Auth0 Management API authorization settings and the `Auth0Management` configuration section

### 401 or Token Errors from the Management API

- **Cause**: `ClientCredentialsTokenProvider` is misconfigured, the secret is wrong, or the audience/domain does not match the tenant
- **Fix**: Verify `AddUserManagement()` registers `ManagementClient` with the correct domain, client ID, client secret, and audience

## Architecture Decisions

1. **Role Claim Mapping**: Use `IClaimsTransformation` to map Auth0 role claims to `ClaimTypes.Role` so ASP.NET Core policies and `RequireRole()` work normally.
2. **Multi-Pass Role Detection**: Check the configured namespace first, then `roles`, then any namespaced claim ending in `/roles`.
3. **Auth0.ManagementApi v8**: Prefer `IManagementApiClient` plus `ClientCredentialsTokenProvider` over manual token-fetch code.
4. **Result-Based Error Handling**: Return `Result<T>` or `Result<bool>` for expected failures instead of relying on exception-driven flow.
5. **Cache Strategy**: Cache role lookup data in `IMemoryCache` and user/list responses in `IDistributedCache` when those dependencies exist.
6. **Testing Mode**: Use cookie auth plus a testing-only login endpoint so E2E runs do not depend on Auth0.

## References

- [Auth0 ASP.NET Core SDK Documentation](https://auth0.com/docs/quickstart/webapp/aspnet-core)
- [Auth0 Management API Documentation](https://auth0.com/docs/api/management/v2)
- [Auth0 Actions (Custom Claims)](https://auth0.com/docs/customize/actions)
- [ASP.NET Core Authorization](https://learn.microsoft.com/aspnet/core/security/authorization/introduction)

## Additional Files

- [Configuration Prompts](./references/configuration-prompts.md)
- [Program.cs Configuration](./references/program-configuration.md)
- [Auth0 Implementation](./references/auth-implementation.md)
- [Admin User Management](./references/admin-user-management.md)
