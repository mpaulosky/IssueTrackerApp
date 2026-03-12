# Authentication & Authorization

This folder contains Auth0 authentication and authorization configuration for IssueTrackerApp.

## Overview

The application uses Auth0 for authentication with OAuth2/OIDC Authorization Code flow with PKCE for enhanced security.

## Configuration

### appsettings.json

The `appsettings.json` file contains **placeholder values only**. Never commit actual Auth0 secrets to source control.

```json
{
  "Auth0": {
    "Domain": "YOUR_AUTH0_DOMAIN.auth0.com",
    "ClientId": "YOUR_AUTH0_CLIENT_ID",
    "ClientSecret": "YOUR_AUTH0_CLIENT_SECRET"
  }
}
```

### User Secrets (Development)

For local development, use user secrets to store actual Auth0 credentials:

```bash
dotnet user-secrets set "Auth0:Domain" "your-tenant.auth0.com"
dotnet user-secrets set "Auth0:ClientId" "your-client-id"
dotnet user-secrets set "Auth0:ClientSecret" "your-client-secret"
```

### Production Configuration

In production environments, use:
- Azure Key Vault
- Environment variables
- Secure configuration providers

**NEVER** store secrets in appsettings.json or appsettings.Production.json.

## Authorization

### Roles

The application defines two roles:
- **Admin**: Full access to all features including admin dashboard
- **User**: Standard access to user features

These roles must be configured in your Auth0 tenant and assigned to users.

### Policies

Two authorization policies are defined:
- **AdminPolicy**: Requires the Admin role
- **UserPolicy**: Requires the User role

### Usage

Apply the `[Authorize]` attribute with a policy to protect pages:

```csharp
@page "/admin"
@using Microsoft.AspNetCore.Authorization
@using Web.Auth
@attribute [Authorize(Policy = AuthorizationPolicies.AdminPolicy)]
```

## Endpoints

- `/account/login` - Initiates Auth0 login flow
- `/account/logout` - Signs out the user and redirects to Auth0 logout

## Security Features

- **HTTPS Required**: All traffic is redirected to HTTPS
- **Antiforgery Tokens**: Enabled for all forms
- **Secure Cookies**: Authentication cookies are configured as secure and HTTP-only
- **PKCE**: Authorization Code flow with Proof Key for Code Exchange
- **JWT Validation**: Audience and issuer claims are validated

## Auth0 Setup

To configure Auth0 for this application:

1. Create an Auth0 application (type: Regular Web Application)
2. Configure Allowed Callback URLs: `https://localhost:7001/callback`
3. Configure Allowed Logout URLs: `https://localhost:7001/`
4. Enable Authorization Code flow
5. Create roles: Admin, User
6. Assign roles to users in Auth0 dashboard

## References

- [Auth0 ASP.NET Core SDK](https://auth0.com/docs/quickstart/webapp/aspnet-core)
- [Authorization Code Flow with PKCE](https://auth0.com/docs/get-started/authentication-and-authorization-flow/authorization-code-flow-with-proof-key-for-code-exchange-pkce)
- [ASP.NET Core Authorization](https://learn.microsoft.com/en-us/aspnet/core/security/authorization/introduction)
