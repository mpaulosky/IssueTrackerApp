# Sam — Learnings for IssueTrackerApp

**Role:** Backend - API & Data Layer
**Project:** IssueTrackerApp
**Initialized:** 2026-03-12

---

## Core Context

**Project:** IssueTrackerApp — .NET 10, Blazor Interactive Server, MongoDB, Redis, .NET Aspire, Auth0
**Stack:** C# 14, Vertical Slice Architecture, MediatR CQRS, FluentValidation, bUnit tests
**Universe:** Lord of the Rings | **Squad version:** v0.5.4
**My role:** Backend Developer - API & Data Layer
**Key files I own:** `src/Domain/Features/`, `src/Domain/Models/`, `src/Persistence.MongoDb/`, `src/Web/Features/Admin/`
**Key patterns I know:**
- Generic IRepository<T> with async-first operations and Result<T> error handling; MongoDB EntityFramework provider
- CQRS with MediatR: commands under Features/*/Commands, queries under Features/*/Queries, handlers sealed and one per command/query
- Value objects (UserInfo, CategoryInfo, StatusInfo) embedded in models; DTOs for transfer; Mappers for conversion
- Comment.IssueId (ObjectId reference) breaks circular dependencies; handlers load full issue separately if needed
- Auth0 Pass 3 auto-detect: if Passes 1–2 find no roles, scan all claims for type ending in "/roles"
**Decisions I must respect:** See .squad/decisions.md

### Recent Sprints
- Sprint 1–2: Domain model refactoring — Value Objects + Mappers, DTO–Model separation enforcement
- Sprint 3: Email notifications infrastructure — background queue processing, domain event handlers
- Sprint 4: Analytics backend — 6 queries, MediatR handlers, IMemoryCache caching, CSV export
- Sprint 5: Admin features — UserManagementService (Auth0 M2M API), AuditLogRepository, role assignment/removal

---

## Recent Learnings

### DTO–Model Separation
- Models persist to MongoDB using value objects (UserInfo, CategoryInfo, StatusInfo), NOT DTOs
- DTOs are for HTTP transfer only; never persist DTO types to database
- Mappers bridge the gap: static sealed classes with ToDto/ToModel/ToInfo conversions
- Comment.Issue DTO → Comment.IssueId ObjectId reference eliminates circular MongoDB dependency
- All 6 DTO constructors now convert value objects via Mappers; zero model-to-DTO direct casts

### Authentication & Authorization Architecture
- Auth0ClaimsTransformation: 3-pass role mapping (Pass 1: configured namespace, Pass 2: bare "roles", Pass 3: auto-detect "/roles" suffix)
- Pass 3 auto-detect fires only when Passes 1–2 find no roles; prevents silent admin failure if namespace misconfigured
- Claims transformation is idempotent; MapRoleClaims deduplicates via identity.HasClaim() check
- UserManagementService uses M2M credentials (separate from OIDC), token caching (24h TTL - 5min margin), role ID mapping cache (30min)

### MongoDB Integration
- Connection string fallback: Check MongoDB:ConnectionString, fall back to ConnectionStrings:mongodb (Aspire injection path)
- Use `IRepository<T>.FindAsync(Expression<Func<T, bool>>) → Result<T>` for single-entity queries
- GroupBy, aggregations, and date-range filtering all work via standard LINQ (MongoDB EF provider handles translation)
- EntityFramework configurations use builder.Property() for mapped columns; BSON serialization automatic

### Background Services & Email Queue
- IHostedService implementations must create IServiceScope for scoped dependencies; never inject scoped services into constructor
- EmailQueueBackgroundService polls every 10s, processes batch of 10, implements exponential backoff retry (1, 2, 4 minutes)
- Domain events published via IMediator.Publish after successful repository operations
- Notification handlers (INotificationHandler) react to events and enqueue emails via QueueEmailCommand

### Analytics Backend
- Summary query executes 6 sub-queries in parallel via Task.WhenAll for performance
- IMemoryCache with 5-minute TTL per query; export query never cached (fresh data)
- CSV export via reflection-based CsvExportHelper; handles escaping (commas, quotes, newlines)
- Date range filtering applied at database level (LINQ predicate); export includes resolution time calculation

---

## Notes
- Team transferred from IssueManager squad (2026-03-12)
- Same tech stack: .NET 10, Blazor, Aspire, MongoDB, Redis, Auth0, MediatR
- Ready for scaling backend services and feature expansion
