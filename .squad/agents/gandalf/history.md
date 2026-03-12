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

## Notes

- Team transferred from IssueManager squad
- Same tech stack: .NET 10, Blazor, Aspire, MongoDB, Redis, Auth0, MediatR
- Ready to begin development
- Auth0 authentication framework now in place, ready for integration with application features