# IssueTrackerApp Decisions

This file records team decisions that affect architecture, scope, and process.

---

## Decisions

### Project Structure & Setup

#### .NET Aspire Project Structure (2026-03-12)
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

### Data Persistence

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

---

#### DTO–Model Separation (2026-03-14)
**Author:** Aragorn (Lead Developer)

Enforced strict DTO–Model separation across all layers:
- **Models** interact with database (only persistence concern)
- **DTOs** for inter-layer data transfer (immutable records)
- **Mappers** provide explicit, testable bidirectional conversion
- **Value Objects** replace embedded DTO properties in Models

**Conversion Flow:** UI → DTO → Mapper.ToInfo() → Model → Repository → MongoDB

**Notable Change:** `Comment.Issue` → `Comment.IssueId` (ObjectId reference) breaks circular dependency.

**Scope:** ~140 files affected; implementation tracked in sprint plan.

---

#### Comment.Issue → Comment.IssueId Refactoring (2026-03-14)
**Author:** Sam (Backend Developer)

Replaced `IssueDto Issue` with `ObjectId IssueId` in Comment model:
- Breaks circular dependency between Comment and Issue DTOs
- Follows MongoDB best practice (reference by ID, not embedding full documents)
- Simplifies serialization (ObjectId is primitive, no nested owned type config)
- Consistent with Attachment model pattern

**Impact:** Comment handlers must use `comment.IssueId` directly; handlers needing full issue data must load separately.

---

### Security & Authentication

#### Auth0 Authentication Implementation (2026-03-12)
**Author:** Gandalf (Security Officer)

Implemented Auth0 authentication with:
- **OAuth2 Authorization Code flow** with PKCE
- **JWT tokens** from Auth0
- **Policy-based authorization** with roles (AdminPolicy, UserPolicy)
- **HTTPS enforcement**, antiforgery protection, secure cookies
- **Strongly-typed Auth0Options** configuration
- **Blazor CascadingAuthenticationState** for component-level auth

**Security Features:**
✅ JWT validation (audience/issuer)  
✅ PKCE prevents authorization code interception  
✅ HttpOnly, Secure, SameSite cookie attributes  
✅ Placeholder configuration (no secrets in git)  

**Alternatives Rejected:**
- ASP.NET Core Identity (more maintenance burden)
- Azure AD B2C (more complex configuration)
- Self-hosted IdentityServer (operational overhead)

---

### Testing

#### Azure Storage Test Projects (2026-03-14)
**Author:** Sam (Backend Developer)

Chose **Testcontainers.Azurite** for integration testing:

**Why Testcontainers.Azurite:**
- ✅ Cross-platform (Linux, macOS, Windows)
- ✅ Docker-based containers, clean isolation
- ✅ Works in CI/CD pipelines
- ✅ Actual Azure SDK against real emulator
- ✅ Consistent with existing Testcontainers.MongoDb pattern

**Alternatives Rejected:**
- Azure Storage Emulator (Windows-only, deprecated)
- In-memory mocks (doesn't test real SDK behavior)
- Real Azure Storage (requires credentials, costs, slower)

---

#### Azure Storage Unit Test Strategy (2026-03-14)
**Author:** Gimli (Tester)

**Focus unit tests on mockable code paths; defer unmockable happy paths to integration tests.**

Unit test coverage:
1. Constructor validation (ArgumentNullException paths)
2. Settings class defaults and property setters
3. Upload operations with full mocking
4. Download/Delete/Thumbnail exception handling and logging
5. DI registration with various configuration scenarios

**Key Pattern:** `DownloadAsync` and `DeleteAsync` create `new BlobClient(Uri)` directly — bypass injected mocks. Focus on exception paths; integration tests cover happy paths.

**Result:** 33 unit tests across 7 files, all passing.

---

#### Azure Blob Storage Integration Test Strategy (2026-03-14)
**Author:** Gimli (Tester)

**Chosen Approach:** Azurite TestContainers with xUnit shared fixture pattern

Test isolation via unique container names per test. Coverage:
- **Upload Tests:** 5 tests (blob creation, auto-creation, content-type, unique naming)
- **Download Tests:** 4 tests (roundtrip, content verification, error handling)
- **Delete Tests:** 4 tests (idempotent deletes, selective deletion)
- **Thumbnail Tests:** 7 tests (ImageSharp integration, resize, aspect ratio, format conversion)
- **Concurrency Tests:** 6 tests (parallel operations, 10+ concurrent)

**Result:** 25+ tests, build successful. Requires Docker/Azurite to run.

---

### Process & Team Dynamics

#### PR Review Process (2026-03-12)
**Directive:** When reviewing PRs for merge, valid suggestions from reviewers (human or automated) must be implemented before merging. Invalid suggestions require a response explaining why they weren't applied. Never ignore suggestions.

---

#### Documentation Structure (2026-03-14)
**Author:** Frodo (Tech Writer)

Implemented **category-based organization** for `docs/LIBRARIES.md` package reference:

**Categories:**
- .NET Aspire Integration
- Data Access
- Application Patterns
- Authentication & Security
- Observability & Monitoring
- Health Checks
- Testing
- Blazor Component Testing
- End-to-End Testing
- Integration Testing Infrastructure

**Rationale:** Developers think in architectural domains, not alphabetically. Single source of truth from centralized `Directory.Packages.props`.

---

#### Frodo's Documentation Responsibilities (2026-03-12)
**Directive:** Frodo (Tech Writer) monitors and documents project changes:
1. Monitor changes and document them
2. Update README.md with significant changes
3. Maintain document listing all libraries and references used

---

### Architectural Directives

#### DTO-Model Separation Architectural Pattern (2026-03-14)
**Directive:** DTOs should only transfer records between application layers. Mappers must convert DTO ↔ Model. Only models interact with the database. This is a **mandatory architectural pattern** going forward.

---

#### bUnit Test Suite Optimization (2026-03-17)
**Author:** Gimli (Tester)

Diagnosed performance issues in bUnit test suite (595 tests):

**Problem:** Full suite execution hangs (~2+ minutes), while individual projects run in 1-7 seconds.

**Solution Implemented:**
- Created `tests/Web.Tests.Bunit/xunit.runner.json` with parallelization controls
- Disabled cross-collection parallelization to reduce BunitContext state conflicts
- Set `maxParallelThreads: 4` to balance throughput with resource usage

**Outstanding Issue:** Two delete tests in DetailsPageTests fail due to EventCallback chain not completing when modal is embedded in Details page. Investigation ongoing.

**Rationale:** Explicit parallelism control reduces test contention. EventCallback bug may reveal underlying resource leak affecting suite performance.

**Consequence:** bUnit tests are more stable; full suite optimization deferred pending bug fix.

---

## Summary of Key Principles

1. **DTO-Model Separation:** Clear boundaries between persistence and API contracts
2. **Result<T> Pattern:** Explicit error handling without exceptions
3. **Testcontainers for Integration:** Realistic testing without cloud dependencies
4. **Aspire Orchestration:** Simplified local development with containerized dependencies
5. **OpenTelemetry Observability:** Production-ready monitoring from day one
6. **Auth0 Identity:** Enterprise-grade security without maintenance burden
7. **Category-Based Documentation:** Developer-centric organization of resources
8. **bUnit Test Optimization:** Explicit parallelism control; defer full suite optimization until failing tests fixed
