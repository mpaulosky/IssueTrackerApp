# IssueTrackerApp

A modern issue tracking application built with .NET Aspire, Blazor, and MongoDB.

[![License](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)

## Overview

IssueTrackerApp is a full-stack web application for managing issues and tracking project progress. It demonstrates modern .NET development practices using the latest technologies and architectural patterns.

## Technology Stack

- **.NET 10** with **C# 14**
- **Blazor Interactive Server Rendering** for responsive UI
- **.NET Aspire** for orchestration and service management
- **MongoDB Atlas** for data persistence
- **Redis** for distributed caching
- **Auth0** for authentication and authorization
- **MediatR** for CQRS pattern implementation
- **FluentValidation** for robust data validation
- **OpenTelemetry** for observability and monitoring
- **xUnit** and **bUnit** for comprehensive testing

## Project Structure

```
IssueTrackerApp/
├── AppHost/                      # .NET Aspire orchestration
├── ServiceDefaults/              # Cross-cutting concerns (OpenTelemetry, health checks)
├── src/
│   ├── Web/                      # Blazor Interactive Server application
│   ├── Domain/                   # Business logic and entities
│   ├── Persistence.MongoDb/      # Data access layer with MongoDB
│   └── ...
├── tests/                        # Unit, integration, and E2E tests
├── docs/                         # Documentation
└── Directory.Packages.props      # Centralized package versioning
```

## Key Features

- **Distributed Caching**: Redis integration for high-performance data caching
- **Health Checks**: Built-in health endpoints for MongoDB and Redis
- **Observability**: OpenTelemetry integration with Azure Application Insights
- **Authentication**: Secure Auth0 integration with role-based access control
- **CQRS Pattern**: MediatR-based command/query separation
- **Comprehensive Testing**: Unit tests, integration tests, and Blazor component tests

## Getting Started

### Prerequisites

- .NET 10 SDK or later
- MongoDB Atlas cluster
- Redis instance (for caching)
- Auth0 tenant configuration

### Development

1. **Clone the repository**
   ```bash
   git clone https://github.com/mpaulosky/IssueTrackerApp.git
   cd IssueTrackerApp
   ```

2. **Restore dependencies**
   ```bash
   dotnet restore
   ```

3. **Build the solution**
   ```bash
   dotnet build
   ```

4. **Run tests**
   ```bash
   dotnet test
   ```

5. **Run the application** (via Aspire AppHost)
   ```bash
   cd src/AppHost
   dotnet run
   ```

### Configuration

Configuration is managed through user secrets and environment variables:

- **MongoDB connection**: Configure via `services.AddAspireMongoDBDriver()`
- **Redis connection**: Configure via `services.AddAspireStackExchangeRedisClient()`
- **Auth0**: Configure client credentials in user secrets or environment
- **OpenTelemetry**: Configured in ServiceDefaults for automatic instrumentation

## Documentation

- [CONTRIBUTING.md](docs/CONTRIBUTING.md) - Contribution guidelines
- [LIBRARIES.md](docs/LIBRARIES.md) - NuGet package references
- [SECURITY.md](docs/SECURITY.md) - Security guidelines
- [CODE_OF_CONDUCT.md](docs/CODE_OF_CONDUCT.md) - Community standards

## Architecture

### ServiceDefaults

The `ServiceDefaults` project provides shared infrastructure concerns:

- **OpenTelemetry Integration**: Automatic instrumentation for ASP.NET Core, HTTP, and runtime metrics
- **Health Checks**: Endpoints for MongoDB and Redis connectivity verification
- **Resilience**: HTTP resilience policies for reliable communication
- **Service Discovery**: Built-in service discovery for Aspire-managed services

### Data Persistence

MongoDB integration via Entity Framework Core:

- Type-safe queries with LINQ
- Change tracking and automatic persistence
- Async/await throughout for non-blocking operations
- Health checks for connection verification

### Caching Strategy

Redis provides distributed caching for:

- Frequently accessed data
- Session state management
- Cache invalidation across instances

## Testing

The project includes multiple testing layers:

- **Unit Tests**: Business logic validation with NSubstitute mocks
- **Integration Tests**: Full application testing with TestContainers
- **Blazor Component Tests**: bUnit for UI component verification
- **E2E Tests**: Playwright for browser-based testing

## API Documentation

API endpoints are documented with OpenAPI 3.0+ specifications via Scalar:

- Navigate to `/api/docs` to view interactive API documentation
- All REST endpoints include XML documentation comments
- Request/response schemas are auto-generated from models

## License

Licensed under the MIT License. See [LICENSE](LICENSE) file for details.

## Contributing

We welcome contributions! Please see [CONTRIBUTING.md](docs/CONTRIBUTING.md) for guidelines.

---

**Status**: Active Development | **Latest Release**: .NET 10 | **Maintained By**: @mpaulosky
