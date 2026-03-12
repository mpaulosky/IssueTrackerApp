# Libraries and References

This document lists all NuGet and npm packages used in IssueTrackerApp, sourced from `Directory.Packages.props` and `src/Web/package.json` for centralized version management.

## NuGet Packages

### .NET Aspire Integration

| Package | Version | Purpose |
|---------|---------|---------|
| Aspire.MongoDB.Driver | 13.1.0 | MongoDB integration with Aspire orchestration and health checks |
| Aspire.StackExchange.Redis | 13.1.0 | Redis integration with Aspire orchestration and service discovery |

### Data Access

| Package | Version | Purpose |
|---------|---------|---------|
| MongoDB.Bson | 3.6.0 | BSON serialization for MongoDB documents |
| MongoDB.Driver | 3.6.0 | Low-level MongoDB client driver |
| MongoDB.EntityFrameworkCore | 10.0.0 | Entity Framework Core provider for MongoDB with LINQ support |

### Application Patterns

| Package | Version | Purpose |
|---------|---------|---------|
| MediatR | 12.4.1 | CQRS pattern implementation for command/query separation |
| FluentValidation.DependencyInjectionExtensions | 11.11.0 | Fluent API for model validation with dependency injection |

### Authentication & Security

| Package | Version | Purpose |
|---------|---------|---------|
| Auth0.AspNetCore.Authentication | 1.5.1 | Auth0 integration with Authorization Code + PKCE flow |

### Microsoft Extensions

| Package | Version | Purpose |
|---------|---------|---------|
| Microsoft.Extensions.Configuration.Abstractions | 10.0.2 | Configuration abstractions for options pattern |
| Microsoft.Extensions.Configuration.Binder | 10.0.2 | Strongly-typed configuration binding |
| Microsoft.Extensions.DependencyInjection.Abstractions | 10.0.2 | DI container abstractions |
| Microsoft.Extensions.Options | 10.0.2 | Options pattern implementation |
| Microsoft.Extensions.Options.ConfigurationExtensions | 10.0.2 | Configuration extensions for options |

### Observability & Monitoring

| Package | Version | Purpose |
|---------|---------|---------|
| Microsoft.Extensions.Http.Resilience | 10.1.0 | HTTP resilience policies and circuit breakers |
| Microsoft.Extensions.ServiceDiscovery | 10.1.0 | Service discovery for Aspire-managed services |
| OpenTelemetry.Exporter.OpenTelemetryProtocol | 1.14.0 | OTLP exporter for OpenTelemetry traces and metrics |
| OpenTelemetry.Extensions.Hosting | 1.14.0 | Hosting extensions for OpenTelemetry setup |
| OpenTelemetry.Instrumentation.AspNetCore | 1.14.0 | Automatic instrumentation for ASP.NET Core |
| OpenTelemetry.Instrumentation.Http | 1.14.0 | Automatic instrumentation for HTTP calls |
| OpenTelemetry.Instrumentation.Runtime | 1.14.0 | Runtime metrics collection (GC, memory, threads) |
| Azure.Monitor.OpenTelemetry.AspNetCore | 1.3.0 | Azure Application Insights integration with OpenTelemetry |

### Health Checks

| Package | Version | Purpose |
|---------|---------|---------|
| AspNetCore.HealthChecks.MongoDb | 9.0.0 | Health check endpoint for MongoDB connectivity |
| AspNetCore.HealthChecks.Redis | 9.0.0 | Health check endpoint for Redis connectivity |

### Testing Framework

| Package | Version | Purpose |
|---------|---------|---------|
| Microsoft.NET.Test.Sdk | 17.12.0 | Test SDK for running xUnit and other test frameworks |
| xunit | 2.9.2 | xUnit testing framework for unit and integration tests |
| xunit.runner.visualstudio | 2.8.2 | Visual Studio test runner for xUnit |
| FluentAssertions | 7.0.0 | Fluent assertions library for expressive test assertions |
| NSubstitute | 5.3.0 | Mocking library for creating test doubles |

### Blazor Component Testing

| Package | Version | Purpose |
|---------|---------|---------|
| bUnit | 1.31.3 | Testing library for Blazor components with xUnit |

### End-to-End Testing

| Package | Version | Purpose |
|---------|---------|---------|
| Microsoft.Playwright | 1.49.0 | Browser automation for E2E testing (Chrome, Firefox, WebKit) |

### Integration Testing Infrastructure

| Package | Version | Purpose |
|---------|---------|---------|
| Testcontainers.MongoDb | 4.2.0 | Docker-based MongoDB containers for integration tests |
| Microsoft.AspNetCore.Mvc.Testing | 10.0.0 | In-memory test server for ASP.NET Core integration tests |

---

## npm Packages

Frontend packages are managed in `src/Web/package.json`.

### TailwindCSS v4

| Package | Version | Purpose |
|---------|---------|---------|
| tailwindcss | ^4.2.1 | Utility-first CSS framework (v4 with new architecture) |
| @tailwindcss/cli | ^4.2.1 | TailwindCSS CLI for build integration |
| postcss | ^8.5.8 | CSS transformation tool |
| autoprefixer | ^10.4.27 | PostCSS plugin for vendor prefixes |

### npm Scripts

```json
{
  "css:build": "tailwindcss -i ./Styles/app.css -o ./wwwroot/css/app.css --minify",
  "css:watch": "tailwindcss -i ./Styles/app.css -o ./wwwroot/css/app.css --watch"
}
```

---

## Notes

- **Centralized NuGet Versioning**: All package versions are managed in `Directory.Packages.props` to ensure consistency across projects
- **Aspire Integration**: The `Aspire.MongoDB.Driver` and `Aspire.StackExchange.Redis` packages handle configuration, health checks, and resilience automatically
- **OpenTelemetry**: All instrumentation packages work together to provide comprehensive observability
- **Testing**: The project uses multiple testing libraries for different scenarios (unit, integration, component, E2E)
- **TailwindCSS v4**: Uses the new v4 architecture with `@import "tailwindcss"` and `@source` directives
- **MSBuild Integration**: CSS compilation is integrated into the .NET build process

---

## Version Strategy

- NuGet packages are updated regularly to maintain security and feature parity with .NET 10 ecosystem
- npm packages follow semantic versioning with `^` prefix for minor version updates
- Breaking changes are coordinated across the team before upgrade
- Security patches are applied immediately
