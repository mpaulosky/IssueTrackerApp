# Secrets Management

This document describes how to securely manage secrets for IssueTrackerApp across different environments.

## Overview

The application uses a layered configuration system where secrets are **never** stored in source control:

| Environment | Secret Storage | Access Method |
|-------------|----------------|---------------|
| Development | .NET User Secrets | `dotnet user-secrets` CLI |
| CI/CD | GitHub Repository Secrets | Environment variables |
| Production | Azure Key Vault | Managed Identity |

## Required Secrets

| Secret | Description | Required |
|--------|-------------|----------|
| `Auth0:Domain` | Auth0 tenant domain (e.g., `tenant.auth0.com`) | ✅ Yes |
| `Auth0:ClientId` | Auth0 application client ID | ✅ Yes |
| `Auth0:ClientSecret` | Auth0 application client secret | ✅ Yes |
| `MongoDB:ConnectionString` | MongoDB Atlas connection string | ✅ Production |
| `SendGrid:ApiKey` | SendGrid API key for email | Optional |
| `BlobStorage:ConnectionString` | Azure Blob Storage connection | Optional |

## Development Setup

### 1. Initialize User Secrets

```bash
cd src/Web
dotnet user-secrets init
```

### 2. Set Required Secrets

```bash
# Auth0 (required)
dotnet user-secrets set "Auth0:Domain" "your-tenant.auth0.com"
dotnet user-secrets set "Auth0:ClientId" "your-client-id"
dotnet user-secrets set "Auth0:ClientSecret" "your-client-secret"

# MongoDB Atlas (optional - Aspire uses local container by default)
dotnet user-secrets set "MongoDB:ConnectionString" "mongodb+srv://user:pass@cluster.mongodb.net"

# SendGrid (optional)
dotnet user-secrets set "SendGrid:ApiKey" "SG.your-api-key"
```

### 3. Verify Secrets

```bash
dotnet user-secrets list
```

Secrets are stored at:
- **Windows:** `%APPDATA%\Microsoft\UserSecrets\issuetracker-web-secrets\secrets.json`
- **macOS/Linux:** `~/.microsoft/usersecrets/issuetracker-web-secrets/secrets.json`

## CI/CD Setup (GitHub Actions)

### Required Repository Secrets

Go to **Settings → Secrets and variables → Actions** and add:

| Secret Name | Value |
|-------------|-------|
| `AUTH0_DOMAIN` | Your Auth0 tenant domain |
| `AUTH0_CLIENT_ID` | Your Auth0 client ID |
| `AUTH0_CLIENT_SECRET` | Your Auth0 client secret |
| `MONGODB_CONNECTION_STRING` | MongoDB Atlas connection string |
| `AZURE_CREDENTIALS` | Azure service principal JSON (for Key Vault access) |

### Workflow Usage

Secrets are injected as environment variables in workflows:

```yaml
env:
  Auth0__Domain: ${{ secrets.AUTH0_DOMAIN }}
  Auth0__ClientId: ${{ secrets.AUTH0_CLIENT_ID }}
  Auth0__ClientSecret: ${{ secrets.AUTH0_CLIENT_SECRET }}
  MongoDB__ConnectionString: ${{ secrets.MONGODB_CONNECTION_STRING }}
```

> **Note:** Use double underscores (`__`) for nested configuration in environment variables.

## Production Setup (Azure Key Vault)

### 1. Create Key Vault

```bash
az keyvault create \
  --name issuetracker-kv \
  --resource-group your-resource-group \
  --location eastus
```

### 2. Add Secrets

Azure Key Vault uses `--` as the section separator (translated to `:` by .NET):

```bash
az keyvault secret set --vault-name issuetracker-kv --name "Auth0--Domain" --value "your-tenant.auth0.com"
az keyvault secret set --vault-name issuetracker-kv --name "Auth0--ClientId" --value "your-client-id"
az keyvault secret set --vault-name issuetracker-kv --name "Auth0--ClientSecret" --value "your-client-secret"
az keyvault secret set --vault-name issuetracker-kv --name "MongoDB--ConnectionString" --value "mongodb+srv://..."
az keyvault secret set --vault-name issuetracker-kv --name "SendGrid--ApiKey" --value "SG.your-key"
```

### 3. Configure Managed Identity

For Azure Container Apps or App Service:

```bash
# Enable system-assigned managed identity
az containerapp identity assign --name your-app --resource-group your-rg

# Get the principal ID
PRINCIPAL_ID=$(az containerapp identity show --name your-app --resource-group your-rg --query principalId -o tsv)

# Grant Key Vault access
az keyvault set-policy \
  --name issuetracker-kv \
  --object-id $PRINCIPAL_ID \
  --secret-permissions get list
```

### 4. Configure App Settings

Set the Key Vault URI in your app configuration:

```bash
az containerapp update \
  --name your-app \
  --resource-group your-rg \
  --set-env-vars "KeyVault__Uri=https://issuetracker-kv.vault.azure.net/"
```

## Configuration Hierarchy

.NET loads configuration in this order (later sources override earlier):

1. `appsettings.json` — Base defaults (empty strings for secrets)
2. `appsettings.{Environment}.json` — Environment-specific non-secrets
3. User Secrets — Development only
4. Azure Key Vault — Non-Development environments
5. Environment Variables — Highest priority (CI/CD, containers)

## Security Best Practices

### DO ✅

- Use User Secrets for local development
- Use Azure Key Vault for production secrets
- Use GitHub Repository Secrets for CI/CD
- Enable Managed Identity for Azure services
- Rotate secrets regularly
- Use separate secrets per environment

### DON'T ❌

- Commit secrets to source control
- Use placeholder values that look like real credentials
- Share development secrets across team members
- Store secrets in appsettings.json files
- Log secrets in application logs

## Troubleshooting

### Secrets not loading in development

1. Verify User Secrets is initialized: `dotnet user-secrets list`
2. Check `UserSecretsId` exists in Web.csproj
3. Ensure running in Development environment

### Key Vault not loading in production

1. Verify `KeyVault:Uri` is set in app configuration
2. Check Managed Identity is enabled
3. Verify Key Vault access policy includes the identity
4. Check secret names use `--` separator (not `:`)

### Auth0 authentication failing

1. Verify all three Auth0 secrets are set
2. Check Auth0 application callback URLs include your app URL
3. Verify Auth0 application type is "Regular Web Application"

## Related Documentation

- [Auth0 Setup Guide](../src/Web/Auth/README.md)
- [Azure Key Vault Documentation](https://learn.microsoft.com/azure/key-vault/)
- [.NET User Secrets](https://learn.microsoft.com/aspnet/core/security/app-secrets)
