# Configuration Prompts

When gathering Auth0 configuration, prompt the user with clear questions and examples. **Do not proceed until all required values are collected.**

## Auth0 Web Application (OIDC) — Required

### Auth0 Domain
**Prompt**: "What is your Auth0 tenant domain? (e.g., `your-tenant.auth0.com` or `your-tenant.us.auth0.com`)"

**Where to find**: Auth0 Dashboard → Applications → [Your Application] → Settings → Domain

**Example**: `dev-abc123.us.auth0.com`

### Client ID
**Prompt**: "What is the Client ID for your Auth0 web application?"

**Where to find**: Auth0 Dashboard → Applications → [Your Application] → Settings → Client ID

**Example**: `abc123XYZ456def789`

**Note**: This is the OIDC web application Client ID, not the Management API M2M Client ID.

### Client Secret
**Prompt**: "What is the Client Secret for your Auth0 web application? (This will be stored in user secrets.)"

**Where to find**: Auth0 Dashboard → Applications → [Your Application] → Settings → Client Secret

**Example**: `AbC123-XyZ456_DeF789_GhI012`

**Security Note**: This value will be stored in user secrets for development and should be stored in a secure vault (Azure Key Vault, AWS Secrets Manager) for production. Never commit to source control.

### Role Claim Namespace
**Prompt**: "What is the custom namespace for Auth0 role claims? (e.g., `https://yourapp.com/roles`)"

**Where to find**: This is configured in your Auth0 Action or Rule. It's the custom claim namespace you use when adding roles to the ID token.

**Example**: `https://issuetracker.com/roles`

**Default behavior**: If this is not configured or left empty, the skill will use a fallback detection strategy that checks standard `roles` claims and auto-detects namespaced role claims ending in `/roles`.

**Auth0 Action Example**:
```javascript
exports.onExecutePostLogin = async (event, api) => {
  const namespace = 'https://yourapp.com';
  if (event.authorization) {
    api.idToken.setCustomClaim(`${namespace}/roles`, event.authorization.roles);
  }
};
```

## Callback and Logout URLs — Required

### Callback URL
**Prompt**: "What is the callback URL for your application? (Development example: `https://localhost:5001/callback`, Production: `https://yourdomain.com/callback`)"

**Where to configure**: Auth0 Dashboard → Applications → [Your Application] → Settings → Allowed Callback URLs

**Development Example**: `https://localhost:5001/callback`

**Production Example**: `https://yourdomain.com/callback`

**Note**: The callback URL must match exactly (protocol, domain, port, path). The Auth0 SDK automatically handles the `/callback` endpoint.

### Logout URL
**Prompt**: "What is the logout redirect URL for your application? (Development example: `https://localhost:5001/`, Production: `https://yourdomain.com/`)"

**Where to configure**: Auth0 Dashboard → Applications → [Your Application] → Settings → Allowed Logout URLs

**Development Example**: `https://localhost:5001/`

**Production Example**: `https://yourdomain.com/`

## Auth0 Management API (M2M) — Optional (Required for Admin User Management)

Ask the user: "Do you want to include admin user management features? This requires an Auth0 Management API Machine-to-Machine (M2M) application. (yes/no)"

If yes, prompt for:

### Management API Client ID
**Prompt**: "What is the Client ID for your Auth0 Management API M2M application?"

**Where to find**: Auth0 Dashboard → Applications → [M2M Application] → Settings → Client ID

**Example**: `xyz789ABC123ghi456`

**Note**: This is different from the OIDC web app Client ID.

### Management API Client Secret
**Prompt**: "What is the Client Secret for your Auth0 Management API M2M application? (This will be stored in user secrets.)"

**Where to find**: Auth0 Dashboard → Applications → [M2M Application] → Settings → Client Secret

**Example**: `Xyz789-Abc123_Ghi456_Jkl789`

**Security Note**: Store in user secrets (development) or secure vault (production).

### Management API Domain
**Prompt**: "What is the Auth0 domain for the Management API? (Usually the same as your Auth0:Domain, e.g., `your-tenant.auth0.com`)"

**Where to find**: Same as Auth0 Domain, unless using a custom domain.

**Example**: `dev-abc123.us.auth0.com`

### Management API Audience
**Prompt**: "What is the Auth0 Management API audience? (Format: `https://your-tenant.auth0.com/api/v2/`)"

**Where to find**: Auth0 Dashboard → Applications → APIs → Auth0 Management API → Identifier

**Example**: `https://dev-abc123.us.auth0.com/api/v2/`

**Note**: The trailing slash is required.

### Management API Scopes
**Important**: Ensure the M2M application is authorized for the Auth0 Management API with the following scopes:

- `read:users` — List and read user details
- `update:users` — Update user metadata
- `read:roles` — List available roles
- `update:roles` — Assign/remove roles from users
- `read:users_app_metadata` — Read user app metadata
- `update:users_app_metadata` — Update user app metadata

**Where to configure**: Auth0 Dashboard → Applications → [M2M Application] → APIs → Auth0 Management API → Authorize → Select scopes

## Feature Selection — Optional

### User Profile Page
**Prompt**: "Do you want to create a user profile page that displays claims and roles? (yes/no)"

**Default**: yes

**What it creates**: `Components/User/Profile.razor` — A Blazor page that displays the authenticated user's claims, roles, email, profile picture, and debug information.

### Admin User Management
**Prompt**: "Do you want to include admin user management pages? This requires an Auth0 Management API M2M application. (yes/no)"

**Default**: no (due to additional Auth0 setup required)

**What it creates**:
- `Features/Admin/Users/UserManagementService.cs`
- `Features/Admin/Users/UserManagementExtensions.cs`
- `Features/Admin/Users/Auth0ManagementOptions.cs`
- `Components/Pages/Admin/Users.razor`
- `Components/Admin/Users/EditUserRolesModal.razor`
- `Components/Admin/Users/UserListTable.razor`
- `Components/Admin/Users/RoleBadge.razor`
- Domain contracts: `IUserManagementService`, `AdminUserSummary`, `RoleAssignment`

## Validation

After gathering all values:

1. **Validate URLs**:
   - Callback URL must be a valid URI with protocol, domain, and `/callback` path
   - Logout URL must be a valid URI with protocol and domain
   - Management API Audience must match `https://{domain}/api/v2/`

2. **Validate Domain**:
   - Domain should match pattern `*.auth0.com` or `*.us.auth0.com` or custom domain
   - No protocol (`https://`) prefix

3. **Validate Client IDs and Secrets**:
   - Client ID and Client Secret should not be empty
   - If Management API is requested, M2M Client ID and Secret should not be empty

4. **Confirm with user**:
   - Display all collected values (mask secrets)
   - Ask "Are these values correct? (yes/no)"
   - If no, re-prompt for corrections

## Example Prompt Flow

```
I'll help you add Auth0 authentication to your Blazor application. I need to gather some configuration values first.

Auth0 Web Application (OIDC):
1. What is your Auth0 tenant domain? (e.g., your-tenant.auth0.com)
   > dev-abc123.us.auth0.com

2. What is the Client ID for your Auth0 web application?
   > abc123XYZ456def789

3. What is the Client Secret? (This will be stored securely in user secrets.)
   > AbC123-XyZ456_DeF789_GhI012

4. What is the custom namespace for Auth0 role claims? (e.g., https://yourapp.com/roles)
   > https://issuetracker.com/roles

Callback and Logout URLs:
5. What is the callback URL for development? (e.g., https://localhost:5001/callback)
   > https://localhost:5001/callback

6. What is the logout redirect URL? (e.g., https://localhost:5001/)
   > https://localhost:5001/

Feature Selection:
7. Do you want to create a user profile page? (yes/no)
   > yes

8. Do you want to include admin user management features? This requires an Auth0 Management API M2M application. (yes/no)
   > yes

Auth0 Management API (M2M):
9. What is the Client ID for your Auth0 Management API M2M application?
   > xyz789ABC123ghi456

10. What is the Client Secret for your M2M application? (This will be stored securely.)
    > Xyz789-Abc123_Ghi456_Jkl789

11. What is the Management API domain? (Usually the same as your Auth0 domain)
    > dev-abc123.us.auth0.com

12. What is the Management API audience? (Format: https://your-tenant.auth0.com/api/v2/)
    > https://dev-abc123.us.auth0.com/api/v2/

Summary:
- Auth0 Domain: dev-abc123.us.auth0.com
- Client ID: abc123XYZ456def789
- Client Secret: AbC***I012 (will be stored in user secrets)
- Role Claim Namespace: https://issuetracker.com/roles
- Callback URL: https://localhost:5001/callback
- Logout URL: https://localhost:5001/
- Profile Page: Yes
- Admin User Management: Yes
- M2M Client ID: xyz789ABC123ghi456
- M2M Client Secret: Xyz***789 (will be stored in user secrets)
- M2M Audience: https://dev-abc123.us.auth0.com/api/v2/

Are these values correct? (yes/no)
> yes

Great! I'll now add Auth0 authentication to your Blazor application...
```
