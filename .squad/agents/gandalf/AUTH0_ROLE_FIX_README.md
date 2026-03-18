# Auth0 Role Mapping Fix — Quick Setup Guide

**Issue:** Users with Admin/User roles in Auth0 were getting "Access Denied" on protected pages.

**Root Cause:** Auth0 sends roles in a custom claim namespace (e.g., `https://issuetracker.com/roles`), but ASP.NET Core's `RequireRole()` looks for the standard `ClaimTypes.Role` claim type. Without mapping, the roles are invisible to authorization policies.

---

## What Was Fixed

1. **Created `Auth0ClaimsTransformation` service** — Maps Auth0's custom role claims to ASP.NET Core's standard role claims
2. **Extended `Auth0Options`** — Added `RoleClaimNamespace` configuration property
3. **Registered transformation** — Added as a scoped service in `Program.cs`
4. **Updated appsettings.json** — Added placeholder for the new config field

---

## Required Configuration

### Step 1: Find Your Auth0 Role Claim Namespace

Your Auth0 tenant must have an **Action** or **Rule** that adds roles to the ID token. The namespace is defined there.

**Example Auth0 Action (Post-Login):**
```javascript
exports.onExecutePostLogin = async (event, api) => {
  const namespace = 'https://issuetracker.com';
  if (event.authorization) {
    api.idToken.setCustomClaim(`${namespace}/roles`, event.authorization.roles);
  }
};
```

In this example, the namespace is `https://issuetracker.com/roles`.

### Step 2: Configure the Namespace in Your Application

**For Development (User Secrets):**
```bash
cd src/Web
dotnet user-secrets set "Auth0:RoleClaimNamespace" "https://issuetracker.com/roles"
```

**For Production (Azure Key Vault):**
Add the key `Auth0--RoleClaimNamespace` with value `https://issuetracker.com/roles` to your Key Vault.

### Step 3: Verify Auth0 Roles

Ensure users have roles assigned in the Auth0 dashboard:
1. Go to **User Management > Users**
2. Select a user
3. Go to **Roles** tab
4. Assign "Admin" or "User" role (must match `AuthorizationRoles` constants)

---

## How It Works

1. User logs in via Auth0
2. Auth0 issues a JWT ID token with custom role claim: `https://issuetracker.com/roles: ["Admin"]`
3. `Auth0ClaimsTransformation` reads the custom claim using the configured namespace
4. It creates standard `ClaimTypes.Role` claims for each role: `http://schemas.microsoft.com/ws/2008/06/identity/claims/role: "Admin"`
5. ASP.NET Core's `RequireRole()` now recognizes the roles and grants access

---

## Testing

1. **Set the namespace** in user secrets (see Step 2)
2. **Assign roles** to test users in Auth0
3. **Run the app** and log in
4. **Check logs** — You should see:
   ```
   Mapped Auth0 role 'Admin' to standard role claim.
   ```
5. **Access protected pages:**
   - `/admin` — Requires Admin role
   - `/issues` — Requires User role

---

## Troubleshooting

### "Access Denied" still occurs
- **Check namespace configuration**: Ensure `Auth0:RoleClaimNamespace` matches your Auth0 Action/Rule
- **Check Auth0 roles**: Verify user has the correct role assigned in Auth0 dashboard
- **Check logs**: Look for warnings like "Auth0:RoleClaimNamespace is not configured"
- **Verify Action/Rule**: Ensure Auth0 Action/Rule is enabled and adds roles to ID token

### Roles not in JWT token
- **Enable Action**: Ensure the Auth0 Action that adds roles is deployed and enabled
- **Test in Auth0**: Use Auth0's "Test Login" to inspect the ID token and verify the role claim is present

### Wrong namespace format
- **Example valid namespace**: `https://issuetracker.com/roles`
- **Do NOT use**: `roles` (missing https:// prefix), `https://issuetracker.com` (missing /roles suffix)

---

## Files Changed

- ✅ `src/Web/Auth/Auth0ClaimsTransformation.cs` — Claims transformation service (NEW)
- ✅ `src/Web/Auth/Auth0Options.cs` — Added RoleClaimNamespace property
- ✅ `src/Web/Program.cs` — Registered claims transformation
- ✅ `src/Web/appsettings.json` — Added RoleClaimNamespace config field

---

## Security Notes

- ✅ No secrets exposed — Namespace is not sensitive (it's in the JWT anyway)
- ✅ Idempotent — Safe to call multiple times without duplicating claims
- ✅ Audit logging — All role mappings are logged for security review
- ✅ Configuration-driven — Easy to change without code modifications

---

**Status:** ✅ Fix implemented and verified (build passes)  
**Next:** Configure `Auth0:RoleClaimNamespace` and test with Auth0 users

— Gandalf 🔒
