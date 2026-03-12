# Decision: Aspire AppHost Configuration

**Date:** 2026-03-12  
**Author:** Sam (Backend Developer)  
**Context:** Issue #3 - Configure .NET Aspire AppHost orchestration

## Decision

Enhanced the AppHost project with comprehensive Aspire orchestration:

### Service Resources
- **MongoDB**: Containerized with MongoExpress UI for database management
- **Redis**: Containerized with RedisCommander UI for cache management
- **Web**: Blazor Server application with service discovery to MongoDB and Redis

### Observability Stack
- **OpenTelemetry**: Configured with OTLP exporter for distributed tracing, metrics, and logs
- **Azure Monitor**: Optional integration via Application Insights connection string
- **Health Checks**: `/health` (readiness) and `/alive` (liveness) endpoints
- **Aspire Dashboard**: Built-in monitoring and telemetry visualization

### ServiceDefaults Enhancements
- Added Azure.Monitor.OpenTelemetry.AspNetCore package (v1.3.0)
- Enabled conditional Azure Monitor exporter based on configuration
- Configured standard resilience patterns for HTTP clients
- Integrated service discovery for inter-service communication

### Environment Configuration
- **Development**: OTLP endpoint at `http://localhost:4317`, detailed logging
- **Staging**: Configurable OTLP and App Insights, standard logging
- **Production**: Configurable OTLP and App Insights, minimal logging

## Rationale

**Aspire Orchestration Benefits:**
- Simplified local development with containerized dependencies
- Built-in service discovery eliminates manual connection string management
- Health checks provide insight into service readiness
- Aspire Dashboard offers real-time monitoring without additional tools

**OpenTelemetry Integration:**
- Industry-standard observability with OTLP protocol
- Flexible exporter configuration (local, Azure Monitor, or both)
- Automatic instrumentation of ASP.NET Core, HTTP clients, and runtime
- Distributed tracing across services

**ServiceDefaults Approach:**
- Single source of truth for cross-cutting concerns
- All projects get consistent telemetry, resilience, and service discovery
- Easy to extend with additional observability or infrastructure patterns

**Environment-Specific Configuration:**
- Development uses local OTLP collector for testing telemetry
- Production-ready with Azure Monitor integration via connection string
- Flexible configuration via appsettings or environment variables

## Technical Notes

- AppHost uses `.WaitFor()` to ensure MongoDB and Redis start before Web
- Health check endpoints excluded from tracing to reduce telemetry noise
- Docker must be running for MongoDB and Redis containers to start
- ServiceDefaults uses conditional configuration to enable exporters only when configured
- Web project automatically gets all ServiceDefaults benefits via `builder.AddServiceDefaults()`

## Impact

- **Developer Experience**: Simplified local development with `dotnet run --project src/AppHost`
- **Observability**: Production-ready telemetry from day one
- **Reliability**: Built-in health checks and resilience patterns
- **Scalability**: Service discovery enables easy addition of new services
- **Operations**: Aspire Dashboard provides operational visibility
