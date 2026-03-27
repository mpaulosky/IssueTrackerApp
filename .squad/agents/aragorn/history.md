# Aragorn — Learnings for IssueTrackerApp

**Role:** Lead - Architecture & Coordination
**Project:** IssueTrackerApp
**Initialized:** 2026-03-12

---

## Learnings

### 2025-07-22 — DTO–Model Separation Analysis

**Architecture Decision:** Models must NOT embed DTO types. DTOs are transfer-only; Models are persistence-only. Mappers bridge the two. See `.squad/decisions/inbox/aragorn-dto-model-separation.md`.

**Key Findings:**
- 5 domain Models (Issue, Category, Status, Comment, Attachment) embed DTOs (`CategoryDto`, `UserDto`, `StatusDto`, `IssueDto`) as properties persisted to MongoDB
- `Comment.Issue` stores a full `IssueDto` creating a circular dependency — must change to `ObjectId IssueId`
- No mapper classes exist — conversion happens via DTO constructors (`new IssueDto(issue)`)
- `IssueConfiguration` uses `builder.Ignore()` to skip DTO properties for EF Core, letting MongoDB BSON serializer handle them directly
- `EmailQueueItem`, `NotificationPreferences`, `User` models are already clean (no DTO references)

**Key File Paths:**
- Models: `src/Domain/Models/` (Issue.cs, Category.cs, Status.cs, Comment.cs, Attachment.cs)
- DTOs: `src/Domain/DTOs/` (IssueDto.cs, CategoryDto.cs, StatusDto.cs, CommentDto.cs, UserDto.cs, AttachmentDto.cs, Analytics/)
- CQRS Handlers: `src/Domain/Features/` (Issues, Categories, Statuses, Comments, Attachments, Analytics, Dashboard, Notifications)
- Persistence: `src/Persistence.MongoDb/` (Repository.cs, IssueTrackerDbContext.cs, Configurations/)
- Services: `src/Web/Services/` (IssueService.cs, LookupService.cs uses direct repo access)
- Tests: 81 test files across 5 projects (Domain.Tests ~50, Web.Tests ~9, Bunit ~9, Integration ~9, Architecture ~4)

**Patterns Confirmed:**
- Generic `Repository<TEntity>` wraps `DbContext` with `Result<T>` error handling
- Services are MediatR facades — delegate to handlers, no business logic
- `LookupService` is the only service with direct repository access and inline Model→DTO conversion
- 31 CQRS handlers total across all features
- Blazor components consume DTOs for display — minimal UI impact from this refactoring
- `PaginatedResponse<T>` and `PagedResult<T>` both exist (pagination duplication — future cleanup candidate)

**User Preference:** Matthew Paulosky wants strict clean architecture enforcement

---

## Notes

- Team transferred from IssueManager squad
- Same tech stack: .NET 10, Blazor, Aspire, MongoDB, Redis, Auth0, MediatR
- Ready to begin development
### 2026-03-27: AppHost Template Rewrite & Integration Test Refactoring

**Session:** AppHost.Tests Playwright E2E tests (Team Gimli, Boromir, Aragorn)

**Accomplishments:**
- Rewrote AppHost project template code to remove weather-specific logic
- Created IssueTrackerApp-specific integration test scenarios
- Fixed and validated all ProjectReference paths in AppHost.Tests

**Changes:**
1. **AppHost Program Template:** Removed weather-related services; replaced with infrastructure setup
2. **Integration Tests:** Updated scenarios from weather domain to issue tracker domain
3. **ProjectReferences:** Verified relative paths to Web, Domain, Persistence projects

**Files Modified:**
- `src/AppHost/AppHost.cs` — template rewrite
- `tests/AppHost.Tests/AppHost.Tests.csproj` — ProjectReference path corrections

**Build Status:** Clean; all references resolve; no path warnings
**Architecture:** Integration tests now aligned with application domain (issues, dashboard, auth)
