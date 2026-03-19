# IssueTrackerApp Decisions Archive

Archived entries from `decisions.md` (older than 30 days). Retained for historical reference.

---

## Archived Entries

### Project Structure & Setup (2026-03-12)

#### .NET Aspire Project Structure

**Author:** Sam (Backend Developer)

Implemented an Aspire-based solution structure:
- **AppHost**: Orchestration with MongoDB and Redis containers
- **ServiceDefaults**: Shared configurations for OpenTelemetry, service discovery, resilience
- **Web**: Blazor Server with Interactive Server rendering
- **Domain**: CQRS with MediatR and FluentValidation
- **Persistence.MongoDb**: MongoDB data access with Entity Framework Core provider

**Rationale:** Aspire orchestration simplifies local development; vertical slice architecture enables clean feature organization.

---

#### Aspire AppHost Configuration (2026-03-12)

**Author:** Sam (Backend Developer)

Enhanced AppHost with comprehensive orchestration:
- Containerized MongoDB with MongoExpress UI
- Containerized Redis with RedisCommander UI
- OpenTelemetry configured with OTLP exporter for distributed tracing
- Azure Monitor optional integration via Application Insights
- Health checks on `/health` (readiness) and `/alive` (liveness) endpoints

**Rationale:** Simplified local development with containerized dependencies; production-ready telemetry from day one.

---

### Data Persistence (2026-03-12)

#### MongoDB Persistence Setup (2026-03-12)

**Author:** Sam (Backend Developer)

Established MongoDB persistence patterns:
1. **Result<T> pattern** for all repository operations (no exception-based control flow)
2. **Generic IRepository<T>** with base implementation
3. **MongoDB.EntityFrameworkCore** provider for EF Core patterns and LINQ support
4. **Strongly-typed MongoDbSettings** with validation on startup
5. **DbContext and DbContextFactory** registration for flexible context usage
6. **Structured logging** in repositories for observability

**Rationale:** Result pattern enables explicit error handling; generic repository reduces duplication; structured logging integrates with OpenTelemetry.

---

#### Value Object & Mapper Infrastructure (2026-03-14)

**Author:** Sam (Backend Developer)

Foundation for DTO-Model separation:
- **Value objects** (`UserInfo`, `CategoryInfo`, `StatusInfo`) as `sealed class` in `Domain.Models`
- **Static mappers** in `Domain.Mappers` for entity ↔ DTO conversions
- BSON attributes match current DTO serialization — no MongoDB migration needed
- Value objects nest for clean DDD composition

**Consequence:** Enables DTO-Model separation sprint without data migration risk.

