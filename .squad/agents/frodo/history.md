# Frodo — Learnings for IssueTrackerApp

**Role:** Tech Writer - Documentation
**Project:** IssueTrackerApp
**Initialized:** 2026-03-12

---

## Learnings

### Documentation Structure Decision (March 2025)

**Context**: Project needed comprehensive documentation to reflect current architecture with .NET Aspire, Blazor Interactive Server Rendering, MongoDB Atlas, and Redis caching.

**Actions Taken**:
1. **README.md Update**: Completely refreshed to showcase modern tech stack
   - Added clear project overview and key features
   - Documented project structure with AppHost, ServiceDefaults, and Blazor web app
   - Included development prerequisites and getting started guide
   - Emphasized Aspire orchestration as central to architecture
   - Added architecture section explaining ServiceDefaults pattern

2. **docs/LIBRARIES.md Creation**: New authoritative package reference
   - Categorized all 22 NuGet packages by domain (Aspire, Data Access, Authentication, etc.)
   - Sourced from centralized `Directory.Packages.props` for single source of truth
   - Included version and purpose for each package
   - Added notes on Aspire integration, OpenTelemetry strategy, and testing approach

**Key Insights**:
- Project uses modern Aspire patterns: ServiceDefaults eliminate boilerplate for OpenTelemetry, health checks, and resilience
- Comprehensive test coverage spans unit (xUnit), component (bUnit), E2E (Playwright), and integration (TestContainers)
- Redis + MongoDB provide distributed caching + persistence; both have health checks integrated
- Auth0 is authentication standard; MediatR provides CQRS pattern for scalability

**Documentation Decisions Made**:
- LIBRARIES.md organizes packages by architectural concern, not alphabetically (easier to find related packages)
- README focuses on "getting started" rather than exhaustive API details (API docs via Scalar at `/api/docs`)
- Emphasized Aspire + ServiceDefaults as core to understanding the architecture

---

## Notes

- Team transferred from IssueManager squad
- Same tech stack: .NET 10, Blazor, Aspire, MongoDB, Redis, Auth0, MediatR
- Ready to begin development

---

### v0.5.0 Admin User Management Documentation (March 2026)

**Context**: Issue #144 required comprehensive documentation for the new Admin User Management feature being released in v0.5.0.

**Actions Taken**:
1. **Created docs/features/admin-user-management.md**
   - Organized into clear sections: Overview, Prerequisites, Setup, Features, Architecture, Security, Troubleshooting
   - Included step-by-step Auth0 M2M application setup instructions (create app, authorize scopes, obtain credentials)
   - Provided dotnet user-secrets configuration instructions for local development
   - Documented all three core features: List Users, Assign Role, Remove Role
   - Added Architecture section covering: IUserManagementService, UserManagementService, Auth0ManagementOptions, AuditLogRepository, CQRS pattern
   - Included detailed Security section with AdminPolicy authorization, secrets management, audit trail, and best practices
   - Added Troubleshooting section with 5 common issues and resolutions

2. **Updated README.md**
   - Added "User Management" feature line to Administration section
   - Placed alphabetically after Status Management, before Admin Dashboard
   - Description highlights the three key features: view users, assign/remove roles, audit log

3. **Verified XML Documentation**
   - Confirmed IUserManagementService has complete interface-level summary and method documentation
   - Confirmed IAuditLogRepository has complete interface-level summary and method documentation
   - Verified Auth0ManagementOptions record has comprehensive XML comments with security notes
   - All public types (AdminUserSummary, RoleChangeAuditEntry, RoleAssignment, DTOs) already have complete XML documentation
   - No XML doc additions needed; all public APIs are properly documented

**PR**: #161 - docs: v0.5.0 Admin User Management feature guide and README update

**Key Insights**:
- Admin User Management feature uses Auth0 Management API v2 with M2M OAuth 2.0 client credentials flow
- Token caching (24-hour TTL minus 5-minute safety margin) and role caching (30-minute TTL) reduce API calls
- Audit log architecture uses MongoDB collection with immutable append-only pattern for compliance auditing
- Feature properly integrates with existing AdminPolicy authorization and CQRS pattern using MediatR
- Security notes cover secrets management (User Secrets for dev, Key Vault for production), rate limiting considerations, and best practices for least privilege

**Documentation Standards Applied**:
- Feature documentation placed in new docs/features/ subdirectory (separate from root-level docs like SECURITY.md)
- Used consistent markdown structure matching existing docs/FEATURES.md style
- Included code examples for configuration and architecture patterns
- Provided troubleshooting section for operational guidance
- Related Documentation section links to connected docs (SECURITY.md, ARCHITECTURE.md, CONTRIBUTING.md)

---

### Release Notes Section Added to docs/index.html (April 2026)

**Context**: docs/index.html was missing a Release Notes section to showcase project version history and highlights. The page had a Dev Blog section but no structured release history.

**Actions Taken**:
1. **Added Release Notes section to docs/index.html**
   - Inserted new `<h2>Release Notes</h2>` section immediately before the `<h2>Dev Blog</h2>` section
   - Created a three-column table with Version, Date, and Highlights columns
   - Listed v0.4.0 (Latest), v0.3.0, and v0.2.0 with links to GitHub release tags
   - v0.4.0 marked with a green "Latest" badge
   - Each release includes brief feature highlights and implementation date
   - Added "View all releases" link pointing to GitHub releases page

2. **Updated footer status line**
   - Changed "Latest Release: .NET 10" to "Latest Release: v0.4.0" 
   - Made version text a hyperlink to the v0.4.0 GitHub release tag
   - Footer now correctly reflects actual project release version

**PR**: squad/docs-blog-catchup - commit 5a6f38b

**Key Insights**:
- docs/index.html uses RELEASES_START/RELEASES_END markers to delimit the release table, enabling future automated release updates
- Release Notes section positioned before Dev Blog creates a natural flow: release history → development blog
- Using HTML spans with inline green styling for the "Latest" badge provides visual distinction
- GitHub release links enable direct navigation from documentation to release artifacts

**Documentation Standards Applied**:
- Release table structure follows standard semantic HTML (thead, tbody, th for headers)
- Version numbers presented as links to their GitHub release pages
- Included both release date and human-readable highlights for each version
- Latest release clearly marked with a badge badge for visitor prominence