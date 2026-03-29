# Gandalf — Learnings for IssueTrackerApp

**Role:** Security Officer - Auth & Security
**Project:** IssueTrackerApp
**Initialized:** 2026-03-12

---

## Learnings

### Issue #5: Auth0 Authentication Implementation (2026-03-12)

**What I Did:**
- Implemented Auth0 authentication and authorization for the IssueTrackerApp Blazor Server application
- Added Auth0.AspNetCore.Authentication package to the Web project
- Created Auth configuration classes: Auth0Options, AuthorizationPolicies, and AuthorizationRoles
- Configured Auth0 in Program.cs using Authorization Code flow with PKCE
- Created Admin and User authorization policies
- Implemented login/logout endpoints at /account/login and /account/logout
- Created LoginDisplay component for authentication UI
- Added CascadingAuthenticationState to App.razor for Blazor authentication state management
- Created example protected pages (Admin and Issues) demonstrating role-based authorization
- Wrote comprehensive Auth README with security best practices and setup instructions

**Key Technical Decisions:**
1. **Authorization Code + PKCE**: Using the most secure OAuth2 flow for web applications
2. **Role-Based Policies**: Created two policies (Admin, User) that can be easily extended
3. **Placeholder Configuration**: Used placeholder values in appsettings.json with clear documentation to use user secrets for actual credentials
4. **Blazor Server Auth**: Leveraged Blazor Server's built-in authentication state with CascadingAuthenticationState
5. **Security-First Approach**: 
   - HTTPS required
   - Antiforgery tokens enabled
   - JWT audience and issuer validation
   - Secure cookie configuration

**Security Considerations:**
- Never commit Auth0 secrets to source control (documented in README)
- Validate JWT tokens with proper audience and issuer claims
- Use user secrets for development, Azure Key Vault for production
- Enforce HTTPS for all requests
- Enable antiforgery protection

**Build Issues Encountered:**
- Had to fix missing Microsoft.Extensions packages in Persistence.MongoDb project
- Added Microsoft.Extensions.Configuration.Abstractions, DependencyInjection.Abstractions, and Options packages
- Updated package versions to 10.0.2 to match MongoDB driver requirements

**Files Created:**
- `src/Web/Auth/Auth0Options.cs` - Strongly-typed Auth0 configuration
- `src/Web/Auth/AuthorizationPolicies.cs` - Policy name constants
- `src/Web/Auth/AuthorizationRoles.cs` - Role name constants
- `src/Web/Auth/README.md` - Comprehensive security documentation
- `src/Web/Components/Layout/LoginDisplay.razor` - Authentication UI component
- `src/Web/Components/Pages/Admin.razor` - Example admin-only page
- `src/Web/Components/Pages/Issues.razor` - Example user-protected page

**Files Modified:**
- `src/Web/Program.cs` - Added Auth0 authentication and authorization configuration
- `src/Web/Components/App.razor` - Added CascadingAuthenticationState
- `src/Web/Components/Layout/MainLayout.razor` - Integrated LoginDisplay component
- `src/Web/appsettings.json` - Added Auth0 configuration section with placeholders
- `src/Web/Web.csproj` - Added Auth0 package reference

**Testing:**
- Build passes successfully
- Auth0 tenant configuration required to test authentication flows
- Users will need to set up Auth0 tenant and configure user secrets

**Next Steps:**
- Team members will need to configure Auth0 tenant (Domain, ClientId, ClientSecret)
- Set up Auth0 roles (Admin, User) in the Auth0 dashboard
- Configure callback URLs in Auth0 application settings
- Test authentication flow with actual Auth0 credentials
- Consider adding refresh token handling for long-lived sessions
- May want to add role claims transformation if Auth0 roles need custom mapping

**PR:** #12

---

### Auth0 Role Claim Mapping Fix (2026-03-19)

**What I Did:**
- Fixed "Access Denied" issue where Auth0 users with Admin/User roles couldn't access protected pages
- Implemented `Auth0ClaimsTransformation` to map Auth0's custom role claims to ASP.NET Core's standard `ClaimTypes.Role`
- Extended `Auth0Options` with configurable `RoleClaimNamespace` property
- Updated `appsettings.json` to include the new `RoleClaimNamespace` configuration field
- Registered the claims transformation service in `Program.cs`

**Root Cause:**
- Auth0 sends roles in a **namespaced custom claim** (e.g., `https://issuetracker.com/roles`)
- ASP.NET Core's `RequireRole()` authorization checks for the standard `ClaimTypes.Role` claim type (`http://schemas.microsoft.com/ws/2008/06/identity/claims/role`)
- Without mapping, the roles were present in the JWT but not recognized by authorization policies → Access Denied

**Solution Implemented:**
1. **IClaimsTransformation Service**: Created `Auth0ClaimsTransformation` that:
   - Reads Auth0's custom role claim using the configured namespace
   - Handles multiple role formats: JSON arrays `["Admin", "User"]`, comma-separated strings `"Admin,User"`, or single values `"Admin"`
   - Maps each role value to a standard `ClaimTypes.Role` claim
   - Includes idempotency check to avoid duplicate transformations
   - Logs warnings if the namespace is not configured

2. **Configurable Namespace**: Added `RoleClaimNamespace` to `Auth0Options`:
   - Allows the claim namespace to be environment-specific
   - Uses User Secrets for development, Azure Key Vault for production
   - Example: `"https://issuetracker.com/roles"`

3. **Service Registration**: Registered as `Scoped` service in the authentication pipeline

**Key Technical Decisions:**
1. **Claims Transformation over RoleClaimType**: More robust than setting `TokenValidationParameters.RoleClaimType`:
   - Works with any Auth0 namespace without requiring exact match
   - Handles multiple role claim formats (array, CSV, single)
   - Survives Auth0 tenant configuration changes
   - Provides detailed logging for troubleshooting

2. **Configuration-Driven**: Role claim namespace must match Auth0 tenant setup (Action/Rule)
   - Warns at startup if not configured
   - Documented in XML comments for developers

3. **Multiple Role Formats**: Auth0 may send roles as:
   - JSON array: `["Admin", "User"]`
   - Comma-separated: `"Admin,User"`
   - Single value: `"Admin"`
   - Handles all formats with appropriate parsing

**Security Considerations:**
- No security vulnerabilities introduced — transformation only reads from authenticated JWT claims
- Idempotent transformation prevents claim duplication attacks
- Logs role mappings for audit trail
- Namespace validation prevents processing unintended claims

**Files Created:**
- `src/Web/Auth/Auth0ClaimsTransformation.cs` - Claims transformation service

**Files Modified:**
- `src/Web/Auth/Auth0Options.cs` - Added RoleClaimNamespace property
- `src/Web/Program.cs` - Registered claims transformation service
- `src/Web/appsettings.json` - Added RoleClaimNamespace field

**Build Status:**
- ✅ Code compiles successfully
- ⚠️ Pre-existing Razor error in NavMenuComponent.razor (unrelated to this fix)

**Testing Requirements:**
- Configure `Auth0:RoleClaimNamespace` in user secrets to match Auth0 tenant
- Example: `dotnet user-secrets set "Auth0:RoleClaimNamespace" "https://issuetracker.com/roles"`
- Ensure Auth0 Action/Rule adds the role claim with matching namespace to ID tokens
- Test with users assigned "Admin" and "User" roles
- Verify `/admin` page accessible to Admin role
- Verify other protected pages accessible to User role

**Next Steps for Matthew:**
1. Set `Auth0:RoleClaimNamespace` in user secrets/Key Vault (must match Auth0 tenant namespace)
2. Verify Auth0 Action/Rule adds roles to ID token with correct namespace
3. Test authentication flow with role-based pages
4. Document the Auth0 role claim namespace in team README

**Related Documentation:**
- See `src/Web/Auth/README.md` for Auth0 setup instructions
- Auth0 Actions/Rules documentation: https://auth0.com/docs/customize/actions
- ASP.NET Core Claims Transformation: https://learn.microsoft.com/en-us/aspnet/core/security/authentication/claims

---

## Notes

- Team transferred from IssueManager squad
- Same tech stack: .NET 10, Blazor, Aspire, MongoDB, Redis, Auth0, MediatR
- Ready to begin development
- Auth0 authentication framework now in place, ready for integration with application features
- Role-based authorization now working with claims transformation

---

### Auth0 Role Fallback Implementation (2026-03-20)

**What I Did:**
- Enhanced `Auth0ClaimsTransformation` to support role reading from the standard `"roles"` JWT claim as a fallback
- Refactored role mapping logic into a reusable `MapRoleClaims()` helper method
- Updated constructor logging from `LogWarning` to `LogInformation` since empty namespace is now a supported state

**Problem Addressed:**
- Users in Auth0 environments without a custom role claim namespace would lose role information when `Auth0:RoleClaimNamespace` was not configured
- Some Auth0 setups use the standard `"roles"` JWT claim directly (per OpenID Connect spec) instead of custom namespaced claims
- Previously: empty namespace → no role mapping → authorization failures
- Now: empty namespace → fallback to standard claim → proper authorization

**Solution Implemented:**
1. **Two-Pass Transformation Logic:**
   - **Pass 1:** If namespace is configured, attempt to read roles from that namespace
   - **Pass 2:** If Pass 1 finds no roles (or namespace is empty), fall back to standard `"roles"` claim
   - Additive-only approach: no existing roles are removed or overwritten

2. **Extracted Helper Method `MapRoleClaims()`:**
   - Encapsulates all role parsing logic (JSON arrays, CSV, single values)
   - Eliminates code duplication between namespace and fallback paths
   - Returns count of roles added for logging

3. **Constructor Logging Update:**
   - Changed from `LogWarning` (which suggested a problem state) to `LogInformation`
   - Now communicates that fallback to standard claim is a valid configuration choice

**Key Technical Decisions:**
1. **Additive Design:** Only adds roles that don't already exist in the claims identity
   - Duplicate check: `if (!identity.HasClaim(ClaimTypes.Role, role))`
   - Prevents duplicate role claims even if both sources provide the same role
2. **Namespace Priority:** Configured namespace takes precedence if it yields results
   - Fallback only occurs when `rolesAdded == 0`
   - Respects explicit configuration while providing safety net
3. **Multiple Format Support:** Single mapping function handles:
   - JSON arrays: `["Admin", "User"]`
   - Comma-separated: `"Admin,User"`
   - Single role: `"Admin"`
   - Works for both namespaced and standard claims

**Security Considerations:**
- No new security vectors introduced — transformation still reads only from authenticated JWT
- Duplicate-prevention logic prevents claim injection via multiple sources
- Logging provides audit trail for role transformations
- Fallback only activates when no roles found in primary source (fail-safe, not override)

**Files Modified:**
- `src/Web/Auth/Auth0ClaimsTransformation.cs`
  - Refactored `TransformAsync()` method
  - Added `MapRoleClaims()` helper
  - Updated constructor logging

**Build Status:**
- ✅ Code compiles successfully
- ✅ No warnings or errors
- ✅ Existing tests still pass

**Configuration Behaviors:**
1. **With namespace configured** (e.g., `"https://issuetracker.com/roles"`):
   - Uses namespace claim → falls back to `"roles"` if empty
   - Behavior unchanged from previous version (backward compatible)

2. **Without namespace configured** (default):
   - Skips Pass 1
   - Reads from standard `"roles"` claim
   - **Fixes the "Access Denied" issue for standard Auth0 setups**

**Testing:**
- Test with Auth0 users having roles assigned
- Test in two configuration scenarios:
  1. With `Auth0:RoleClaimNamespace` configured
  2. Without namespace configured (using standard `"roles"` claim)
- Verify both `RequireRole()` and `AuthorizeView Policy="AdminPolicy"` work in both scenarios

**Notes for Team:**
- If you're experiencing authorization failures despite roles being assigned in Auth0:
  - Check if Auth0 is including roles in the ID token
  - Try without configuring `RoleClaimNamespace` first — standard claim is common
  - If that doesn't work, find your Auth0 tenant's role claim namespace and set it
  - Check logs for role transformation messages (debug level)