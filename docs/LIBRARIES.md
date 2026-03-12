# Libraries and References

This document lists all NuGet packages used in IssueTrackerApp, sourced from `Directory.Packages.props` for centralized version management.

## .NET Aspire Integration

| Package | Version | Purpose |
|---------|---------|---------|
| Aspire.MongoDB.Driver | 13.1.0 | MongoDB integration with Aspire orchestration and health checks |
| Aspire.StackExchange.Redis | 13.1.0 | Redis integration with Aspire orchestration and service discovery |

## Data Access

| Package | Version | Purpose |
|---------|---------|---------|
| MongoDB.Driver | 3.6.0 | Low-level MongoDB client driver |
| MongoDB.EntityFrameworkCore | 10.0.0 | Entity Framework Core provider for MongoDB with LINQ support |

## Application Patterns

| Package | Version | Purpose |
|---------|---------|---------|
| MediatR | 12.4.1 | CQRS pattern implementation for command/query separation |
| FluentValidation.DependencyInjectionExtensions | 11.11.0 | Fluent API for model validation with dependency injection |

## Authentication & Security

| Package | Version | Purpose |
|---------|---------|---------|
| Auth0.AspNetCore.Authentication | 1.5.1 | Auth0 integration for ASP.NET Core applications |

## Observability & Monitoring

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

## Health Checks

| Package | Version | Purpose |
|---------|---------|---------|
| AspNetCore.HealthChecks.MongoDb | 9.0.0 | Health check endpoint for MongoDB connectivity |
| AspNetCore.HealthChecks.Redis | 9.0.0 | Health check endpoint for Redis connectivity |

## Testing Framework

| Package | Version | Purpose |
|---------|---------|---------|
| Microsoft.NET.Test.Sdk | 17.12.0 | Test SDK for running xUnit and other test frameworks |
| xunit | 2.9.2 | xUnit testing framework for unit and integration tests |
| xunit.runner.visualstudio | 2.8.2 | Visual Studio test runner for xUnit |
| FluentAssertions | 7.0.0 | Fluent assertions library for expressive test assertions |
| NSubstitute | 5.3.0 | Mocking library for creating test doubles |

## Blazor Component Testing

| Package | Version | Purpose |
|---------|---------|---------|
| bUnit | 1.31.3 | Testing library for Blazor components with xUnit |

## End-to-End Testing

| Package | Version | Purpose |
|---------|---------|---------|
| Microsoft.Playwright | 1.49.0 | Browser automation for E2E testing (Chrome, Firefox, WebKit) |

## Integration Testing Infrastructure

| Package | Version | Purpose |
|---------|---------|---------|
| Testcontainers.MongoDb | 4.2.0 | Docker-based MongoDB containers for integration tests |
| Microsoft.AspNetCore.Mvc.Testing | 10.0.0 | In-memory test server for ASP.NET Core integration tests |

---

## Notes

- **Centralized Versioning**: All package versions are managed in `Directory.Packages.props` to ensure consistency across projects
- **Aspire Integration**: The `Aspire.MongoDB.Driver` and `Aspire.StackExchange.Redis` packages handle configuration, health checks, and resilience automatically
- **OpenTelemetry**: All instrumentation packages work together to provide comprehensive observability
- **Testing**: The project uses multiple testing libraries for different scenarios (unit, integration, component, E2E)

---

## Version Strategy

- Packages are updated regularly to maintain security and feature parity with .NET 10 ecosystem
- Breaking changes are coordinated across the team before upgrade
- Security patches are applied immediately
