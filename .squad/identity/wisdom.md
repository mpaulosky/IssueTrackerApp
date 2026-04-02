---
last_updated: 2026-03-26T22:54:45.211Z
---

# Team Wisdom

Reusable patterns and heuristics learned through work. NOT transcripts — each entry is a distilled, actionable insight.

## Patterns

<!-- Append entries below. Format: **Pattern:** description. **Context:** when it applies. -->

**Pattern:** Use `Result<T>` / `ResultErrorCode` for expected failures — never throw for business logic errors (not-found, validation, external service). **Context:** Any Domain command/query handler; enforced by Architecture Tests.

**Pattern:** CQRS strict naming — Commands end in `Command`, Queries in `Query`, Handlers in `Handler` (sealed), Validators in `Validator`. Architecture Tests enforce this at build time. **Context:** Every Domain feature slice.

**Pattern:** Domain events + MediatR notifications for cross-cutting side-effects (email, SignalR). Publish via `IPublisher`; handle in `Domain/Features/Notifications/` handlers. **Context:** Whenever an action needs to notify external parties without coupling feature slice to notification code.

**Pattern:** `IMemoryCache` for analytics + token caching (5–30 min TTL). Use `GetOrCreateAsync` to prevent concurrent cold-start races. **Context:** Auth0 Management API token, analytics aggregation results.

**Pattern:** Auth0 Management API secrets bound from `Auth0Management` config section; never hardcoded. Store in User Secrets (dev) or Azure Key Vault (prod). **Context:** Any feature touching `UserManagementService`.

**Pattern:** Minimal API endpoints live in `src/Web/Endpoints/` (categories, statuses) AND `src/Web/Features/` (comments, attachments). Both follow route-group + `AdminPolicy`/`RequireAuthorization`. **Context:** Adding new REST endpoints.

**Pattern:** Startup seeds categories and statuses via `DataSeeder.cs`; seeding skipped in `Testing` environment. **Context:** Any test that runs against MongoDB (check `Environment == "Testing"`).

**Pattern:** `.squad/` changes go on `squad/*` branches with `git push --no-verify` (pre-push hook runs dotnet build/test; not needed for squad-only changes). **Context:** All squad process/doc commits.

**Pattern:** `ObjectIdJsonConverter` registered globally in `Program.cs` for MongoDB `ObjectId` ↔ JSON string. Any new model with ObjectId field works automatically. **Context:** Adding new domain models with ObjectId.

**Pattern:** Architecture Tests in `tests/Architecture.Tests/` enforce layer boundaries, naming, and structure at build time. Run them after any new domain type to catch violations early. **Context:** Every sprint; run after adding features.
