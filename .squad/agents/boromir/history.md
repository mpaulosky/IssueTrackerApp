# Boromir — Learnings for IssueTrackerApp

**Role:** DevOps - CI/CD & Infrastructure
**Project:** IssueTrackerApp
**Initialized:** 2026-03-12

---

## Learnings

### MongoDB Atlas Connection String Migration (2026-03-18)
- **AppHost MongoDB pattern changed**: From `AddMongoDB("mongodb").AddDatabase("issuetracker-db")` (container) to `AddConnectionString("mongodb")` (Atlas connection string from User Secrets)
- **AppHost.csproj**: Removed `Aspire.Hosting.MongoDB` package — `AddConnectionString` comes from base `Aspire.Hosting.AppHost`
- **Two MongoDB config paths in Web project**: `MongoDB:ConnectionString` (for `MongoDbSettings`/EF Core) and `ConnectionStrings:mongodb` (for Aspire's `AddMongoDBClient`). Both must be set.
- **MongoDbSettings config section**: `MongoDB` (not `MongoDb`). Properties: `ConnectionString`, `DatabaseName` (default: `issuetracker-db`)
- **AppHost `ManagePackageVersionsCentrally` is `false`** — Aspire AppHost SDK manages its own package versions outside `Directory.Packages.props`
- **Key file paths**: `src/AppHost/AppHost.cs`, `src/AppHost/AppHost.csproj`, `src/Persistence.MongoDb/Configurations/MongoDbSettings.cs`, `src/Persistence.MongoDb/ServiceCollectionExtensions.cs`
- **AppHost UserSecretsId**: `27ff814c-e630-4d84-a864-c3a534dd5c93`

### Git Describe Stderr Leak Fix (2026-03-18)
- **Fixed `GetGitBuildInfo` MSBuild target** in `src/Web/Web.csproj` — git commands now redirect stderr to `/dev/null`
- **Root cause**: When no git tags exist, `git describe --tags --abbrev=0` outputs `fatal: No names found, cannot describe anything.` to stderr. With `ConsoleToMSBuild="true"`, this error text was captured into `_RawGitTag`, preventing the `v0.0.0` fallback at line 66 from triggering.
- **Fix applied**: Changed git commands from `git describe --tags --abbrev=0` → `git describe --tags --abbrev=0 2>/dev/null` and `git rev-parse --short HEAD` → `git rev-parse --short HEAD 2>/dev/null`
- **Created initial tag**: `v0.1.0` so future builds return real version
- **Verified**: BuildInfo.g.cs now correctly generates `Version = "v0.1.0"` and `Commit = "e4874a8"`

---

## Notes

- Team transferred from IssueManager squad
- Same tech stack: .NET 10, Blazor, Aspire, MongoDB, Redis, Auth0, MediatR
- Ready to begin development
### BuildInfo Generation Pipeline — Stderr Redirection in MSBuild (2026-03-19)
- **Issue:** MSBuild's `GetGitBuildInfo` target leaked git stderr into build constants
- **Solution:** Redirected stderr in both `git describe` and `git rev-parse` commands using `2>/dev/null`
- **Tag:** Created `v0.1.0` to seed version for future builds
- **Verification:** Gimli confirmed BuildInfo.g.cs generates clean constants; footer displays correct version
- **Related:** `.squad/decisions.md` entry on MSBuild Git Stderr Redirection Pattern
