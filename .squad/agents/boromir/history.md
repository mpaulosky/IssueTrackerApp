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

---

## Notes

- Team transferred from IssueManager squad
- Same tech stack: .NET 10, Blazor, Aspire, MongoDB, Redis, Auth0, MediatR
- Ready to begin development