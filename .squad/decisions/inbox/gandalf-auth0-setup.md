# Decision: Auth0 Authentication Implementation

**Date:** 2026-03-12  
**Agent:** Gandalf (Security Officer)  
**Issue:** #5  
**PR:** #12

---

## Context

IssueTrackerApp needed authentication and authorization to protect pages and features. The team had already selected Auth0 as the identity provider, and the Auth0 package was included in Directory.Packages.props.

## Decision

Implemented Auth0 authentication using the `Auth0.AspNetCore.Authentication` SDK with the following approach:

### Authentication Flow
- **OAuth2 Flow:** Authorization Code flow with PKCE (Proof Key for Code Exchange)
- **Token Type:** JWT tokens from Auth0
- **Session Management:** ASP.NET Core Cookie Authentication with Auth0 integration

### Authorization Strategy
- **Policy-Based Authorization:** Created two policies (AdminPolicy, UserPolicy)
- **Role-Based Access Control:** Policies require specific roles (Admin, User)
- **Blazor Integration:** Used CascadingAuthenticationState for component-level auth

### Configuration Management
- **appsettings.json:** Placeholder values only (never commit secrets)
- **User Secrets:** For local development
- **Azure Key Vault:** Recommended for production
- **Strongly-Typed Config:** Auth0Options class for type safety

### Security Features
1. **HTTPS Enforcement:** All requests redirected to HTTPS
2. **Antiforgery Protection:** Enabled for all forms
3. **JWT Validation:** Audience and issuer claims validated
4. **Secure Cookies:** HttpOnly, Secure, SameSite attributes set
5. **PKCE:** Prevents authorization code interception attacks

## Alternatives Considered

### 1. ASP.NET Core Identity with SQL Server
**Pros:**
- Full control over user database
- No external dependencies
- Free

**Cons:**
- More maintenance (password resets, email verification, etc.)
- Security burden on our team
- No SSO/social login out of the box
- More code to write and maintain

**Why Not:** Auth0 provides enterprise-grade security, MFA, social logins, and reduces our security maintenance burden.

### 2. Azure AD B2C
**Pros:**
- Microsoft ecosystem integration
- Pay-as-you-go pricing

**Cons:**
- More complex configuration
- Steeper learning curve
- Less flexible than Auth0

**Why Not:** Auth0 SDK is more straightforward and team already has Auth0 experience.

### 3. IdentityServer / Duende IdentityServer
**Pros:**
- Self-hosted
- Full control

**Cons:**
- Commercial licensing required for Duende
- Infrastructure to maintain
- More complexity

**Why Not:** SaaS solution (Auth0) reduces operational overhead.

## Implementation Details

### Architecture
```
Browser → Blazor Server App → Auth0 (OIDC) → JWT Token → ASP.NET Core Auth Middleware
                              ↓
                       Cookie Session
```

### Key Components
1. **Auth0Options:** Configuration model
2. **AuthorizationPolicies:** Policy name constants
3. **AuthorizationRoles:** Role name constants
4. **LoginDisplay Component:** Authentication UI
5. **Login/Logout Endpoints:** `/account/login`, `/account/logout`

### Protected Pages
- **Admin Page:** Requires Admin role
- **Issues Page:** Requires User role
- **Home Page:** Public

## Security Considerations

### What We Did Right
- ✅ Authorization Code + PKCE (most secure flow)
- ✅ Placeholder configuration (no secrets in git)
- ✅ Comprehensive security documentation
- ✅ HTTPS enforcement
- ✅ Antiforgery protection
- ✅ JWT validation

### Future Enhancements
- Consider refresh token handling for long-lived sessions
- Add custom claims transformation if needed
- Implement role caching for performance
- Add MFA enforcement for Admin role
- Consider passwordless authentication for better UX

## Testing Strategy

1. **Unit Tests:** Not implemented yet (authentication components are hard to unit test)
2. **Integration Tests:** Required - test with test Auth0 tenant
3. **Manual Testing:** Requires Auth0 tenant setup

### Testing Checklist
- [ ] Login flow works correctly
- [ ] Logout clears session
- [ ] Admin page blocks non-admin users
- [ ] Issues page blocks unauthenticated users
- [ ] JWT token validation works
- [ ] Session persistence across page refreshes

## Dependencies

### New Packages
- Auth0.AspNetCore.Authentication 1.5.1 (already in Directory.Packages.props)

### Package Updates
- Microsoft.Extensions.Configuration.Abstractions 10.0.2
- Microsoft.Extensions.DependencyInjection.Abstractions 10.0.2
- Microsoft.Extensions.Options 10.0.2

(These were added to fix Persistence.MongoDb build issues)

## Rollout Plan

### Developer Setup
1. Create Auth0 tenant (or use existing)
2. Create Auth0 application (Regular Web Application)
3. Configure callback URLs: `https://localhost:7001/callback`
4. Configure logout URLs: `https://localhost:7001/`
5. Create roles: Admin, User
6. Create test users and assign roles
7. Set user secrets:
   ```bash
   dotnet user-secrets set "Auth0:Domain" "your-tenant.auth0.com"
   dotnet user-secrets set "Auth0:ClientId" "your-client-id"
   dotnet user-secrets set "Auth0:ClientSecret" "your-client-secret"
   ```

### Production Setup
1. Create production Auth0 tenant
2. Configure production URLs
3. Store credentials in Azure Key Vault
4. Set environment variables in App Service
5. Enable Auth0 logging and monitoring

## Impact

### Positive
- ✅ Secure authentication framework in place
- ✅ Role-based authorization ready
- ✅ Blazor auth state management configured
- ✅ Clear security documentation
- ✅ Extensible policy-based approach

### Challenges
- ⚠️ Developers need to configure Auth0 tenant
- ⚠️ Additional cost (Auth0 subscription)
- ⚠️ External dependency (Auth0 uptime)

### Risks
- **Auth0 Outage:** Users cannot log in (mitigate with proper Auth0 plan SLA)
- **Misconfiguration:** Could expose protected pages (mitigate with testing)
- **Token Expiration:** Users might get logged out unexpectedly (implement refresh tokens)

## References

- [Auth0 ASP.NET Core SDK Documentation](https://auth0.com/docs/quickstart/webapp/aspnet-core)
- [Authorization Code Flow with PKCE](https://auth0.com/docs/get-started/authentication-and-authorization-flow/authorization-code-flow-with-proof-key-for-code-exchange-pkce)
- [ASP.NET Core Authorization](https://learn.microsoft.com/en-us/aspnet/core/security/authorization/introduction)
- [Blazor Authentication and Authorization](https://learn.microsoft.com/en-us/aspnet/core/blazor/security/)

## Approval

This decision document will be reviewed by the team and merged into `.squad/decisions/` once PR #12 is approved.

---

**Status:** Pending Review  
**Next Review Date:** N/A (review with PR)
