# Decision: .NET Aspire Project Structure

**Date:** 2026-03-12  
**Author:** Sam (Backend Developer)  
**Context:** Issue #2 - Set up project structure for IssueTrackerApp

## Decision

Implemented a .NET Aspire-based solution structure with the following projects:

1. **AppHost** - Aspire orchestration
   - Hosts MongoDB and Redis containers
   - References Web project
   - Uses Aspire.AppHost.Sdk (13.1.0)
   - Central package management disabled for SDK compatibility

2. **ServiceDefaults** - Shared Aspire configurations
   - OpenTelemetry, service discovery, resilience
   - Referenced by AppHost with `IsAspireProjectResource="false"`

3. **Web** - Blazor Server with Interactive Server rendering
   - References Domain, Persistence.MongoDb, ServiceDefaults
   - Includes `public partial class Program {}` for testing

4. **Domain** - Domain entities and business logic
   - CQRS with MediatR (12.4.1)
   - FluentValidation (11.11.0)
   - Vertical Slice Architecture structure

5. **Persistence.MongoDb** - MongoDB data access
   - MongoDB.Driver (3.6.0)
   - MongoDB.EntityFrameworkCore (10.0.0)
   - Repository pattern

## Rationale

- **Aspire orchestration** simplifies local development with containerized dependencies
- **Vertical Slice Architecture** keeps related features together
- **Centralized package management** ensures version consistency across projects
- **CQRS with MediatR** provides clear separation of commands and queries
- **MongoDB.EntityFrameworkCore** offers familiar EF Core patterns for MongoDB

## Technical Notes

- Aspire.AppHost.Sdk implicitly references Aspire.Hosting.AppHost, requiring `ManagePackageVersionsCentrally=false`
- MongoDB.EntityFrameworkCore 10.0.0 requires MongoDB.Driver >= 3.6.0
- ServiceDefaults requires OpenTelemetry packages (1.14.0)

## Impact

- Clean separation of concerns
- Scalable architecture ready for vertical slice features
- Easy local development with Aspire dashboard
- Testable design with WebApplicationFactory support
