# SignalR Real-Time Notifications

This document describes the SignalR infrastructure for real-time issue updates in IssueTrackerApp.

## Architecture

### Hub Endpoint
- **Path:** `/hubs/issues`
- **Hub Class:** `Web.Hubs.IssueHub`

### Client Events
The following events are broadcast to connected clients:

1. **IssueCreated** - When a new issue is created
   - Event: `IssueCreatedEvent`
   - Groups: `all`

2. **IssueUpdated** - When an issue is updated or its status changes
   - Event: `IssueUpdatedEvent`
   - Groups: `issue-{issueId}`, `all`

3. **CommentAdded** - When a comment is added to an issue
   - Event: `CommentAddedEvent`
   - Groups: `issue-{issueId}`

4. **IssueAssigned** - When an issue is assigned to a user
   - Event: `IssueAssignedEvent`
   - Groups: `issue-{issueId}`, `all`

### Client Methods
Clients can call these methods to manage group subscriptions:

- `JoinIssueGroup(issueId)` - Subscribe to updates for a specific issue
- `LeaveIssueGroup(issueId)` - Unsubscribe from a specific issue

## Development

### Local Development
In development mode, SignalR uses the built-in ASP.NET Core SignalR server with WebSockets transport.

No additional configuration is required for local development.

## Production Deployment with Azure SignalR Service

### Why Azure SignalR Service?
For production environments, it is recommended to use Azure SignalR Service for:
- **Scalability:** Handle thousands of concurrent connections
- **Reliability:** High availability and automatic failover
- **Performance:** Optimized message delivery

### Setup Steps

#### 1. Create Azure SignalR Service
```bash
# Create a resource group
az group create --name IssueTrackerRG --location eastus

# Create Azure SignalR Service
az signalr create \
  --name IssueTrackerSignalR \
  --resource-group IssueTrackerRG \
  --sku Standard_S1 \
  --unit-count 1 \
  --service-mode Default
```

#### 2. Get Connection String
```bash
# Get the connection string
az signalr key list \
  --name IssueTrackerSignalR \
  --resource-group IssueTrackerRG \
  --query primaryConnectionString -o tsv
```

#### 3. Configure Application

Add the NuGet package to `Web.csproj`:
```xml
<PackageReference Include="Microsoft.Azure.SignalR" />
```

Update `appsettings.Production.json`:
```json
{
  "Azure": {
    "SignalR": {
      "Enabled": true,
      "ConnectionString": "Endpoint=https://...;AccessKey=...;Version=1.0;"
    }
  }
}
```

#### 4. Update Program.cs
Add configuration to use Azure SignalR Service in production:

```csharp
// Add SignalR for real-time notifications
var signalRBuilder = builder.Services.AddSignalR();

// Use Azure SignalR Service in production
if (builder.Environment.IsProduction())
{
    var signalRConfig = builder.Configuration.GetSection("Azure:SignalR");
    if (signalRConfig.GetValue<bool>("Enabled"))
    {
        var connectionString = signalRConfig.GetValue<string>("ConnectionString");
        if (!string.IsNullOrEmpty(connectionString))
        {
            signalRBuilder.AddAzureSignalR(connectionString);
        }
    }
}
```

#### 5. Configure in Azure App Service
Set the connection string as an environment variable or in Azure Key Vault:

```bash
az webapp config appsettings set \
  --resource-group IssueTrackerRG \
  --name IssueTrackerWebApp \
  --settings Azure__SignalR__ConnectionString="Endpoint=https://...;AccessKey=...;Version=1.0;"
```

### Monitoring

Enable diagnostic logging in Azure SignalR Service:
```bash
az monitor diagnostic-settings create \
  --name SignalRDiagnostics \
  --resource /subscriptions/{subscription-id}/resourceGroups/IssueTrackerRG/providers/Microsoft.SignalRService/SignalR/IssueTrackerSignalR \
  --logs '[{"category": "AllLogs", "enabled": true}]' \
  --workspace /subscriptions/{subscription-id}/resourcegroups/IssueTrackerRG/providers/Microsoft.OperationalInsights/workspaces/IssueTrackerLogAnalytics
```

## Aspire Integration

For Aspire-based deployments, Azure SignalR Service can be configured via the AppHost:

```csharp
// In AppHost/Program.cs
var signalr = builder.AddAzureSignalR("signalr");

var web = builder.AddProject<Projects.Web>("web")
    .WithReference(signalr);
```

This requires the Aspire Azure SignalR component:
```bash
dotnet add package Aspire.Hosting.Azure.SignalR
```

## Testing SignalR

### Browser Console Test
```javascript
// Connect to the hub
const connection = new signalR.HubConnectionBuilder()
    .withUrl("/hubs/issues")
    .build();

// Subscribe to events
connection.on("IssueCreated", (event) => {
    console.log("Issue created:", event);
});

connection.on("IssueUpdated", (event) => {
    console.log("Issue updated:", event);
});

// Start connection
await connection.start();

// Join a specific issue group
await connection.invoke("JoinIssueGroup", "507f1f77bcf86cd799439011");
```

## Security Considerations

### Authentication
SignalR connections automatically inherit the authentication context from the HTTP request. No additional configuration is needed.

### Authorization
To restrict hub access:

```csharp
[Authorize]
public sealed class IssueHub : Hub
{
    // ...
}
```

Or add specific policies:
```csharp
[Authorize(Policy = AuthorizationPolicies.UserPolicy)]
public sealed class IssueHub : Hub
{
    // ...
}
```

## Performance Tuning

### Connection Limits
For Azure SignalR Service:
- **Free tier:** 20 concurrent connections
- **Standard tier:** 1,000 connections per unit (scalable)

### Message Size
Keep event payloads under 32KB for optimal performance.

### Reconnection
SignalR clients automatically reconnect on network interruptions. Configure retry policy in client:

```javascript
const connection = new signalR.HubConnectionBuilder()
    .withUrl("/hubs/issues")
    .withAutomaticReconnect([0, 2000, 10000, 30000])
    .build();
```

## Troubleshooting

### Connection Issues
1. Check WebSocket support is enabled in App Service
2. Verify CORS configuration allows SignalR
3. Check firewall rules allow SignalR ports (443 for HTTPS)

### Message Delivery
1. Verify group subscriptions: `JoinIssueGroup` must be called
2. Check hub context is correctly injected
3. Ensure notification methods are called after successful operations

### Logging
Enable detailed SignalR logging:

```json
{
  "Logging": {
    "LogLevel": {
      "Microsoft.AspNetCore.SignalR": "Debug",
      "Microsoft.AspNetCore.Http.Connections": "Debug"
    }
  }
}
```
