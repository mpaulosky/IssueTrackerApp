# Aragorn — Learnings for IssueTrackerApp

**Role:** Lead - Architecture & Coordination
**Project:** IssueTrackerApp
**Initialized:** 2026-03-12

---

## Core Context

### Historical Foundation (July 2025 – March 27)

**DTO–Model Separation Analysis (2025-07-22):**
- Architecture Decision: Models must NOT embed DTO types. DTOs are transfer-only; Models are persistence-only.
- Key Findings: 5 domain Models embed DTOs as persisted properties. Comment.Issue stores full IssueDto creating circular dependency — must change to ObjectId IssueId.
- No mapper classes exist — conversion via DTO constructors.
- Key file paths: Models in `src/Domain/Models/`, DTOs in `src/Domain/DTOs/`, CQRS in `src/Domain/Features/`, Persistence in `src/Persistence.MongoDb/`, Services in `src/Web/Services/`.
- Generic Repository<TEntity> wraps DbContext with Result<T> error handling; Services are MediatR facades.
- 31 CQRS handlers total; PaginatedResponse<T> and PagedResult<T> duplication noted for future cleanup.
- User Preference: Matthew Paulosky wants strict clean architecture enforcement.

**PR #76 Review & Fixes (2026-07-23):**
- AppHost.Tests added: Aspire integration + Playwright E2E tests.
- AspireManager lifecycle: chains PlaywrightManager.InitializeAsync() + StartAppAsync().
- Testing seam: cookie auth, fake repos, skipped background services (correct Aspire E2E pattern).
- Fixed HTTPS port 7043 with IsProxied = false for predictable base URL.
- Six Gimli blocking issues resolved: false skip docs, visibility, context leak, fragile assertions, EOF newline, dashboard disabled.

**PR Review Sessions (2026-03-27):**
- Lead reviewer for Pippin (#84) & Legolas (#83).

---

## Recent Learnings (March 28+)

---

### 2026-03-29 — Sprint 1: Auth0 Role Claim Namespace — Diagnosis & Config Fix (Issues #88, #89)

**Trigger:** Issues #88 (diagnose) and #89 (config) — Auth0 role claims not mapping to ClaimTypes.Role due to empty RoleClaimNamespace setting.

**Diagnosis (Issue #88):**
- Confirmed Auth0 sends roles under claim type: `https://issuetracker.com/roles`
- Verified by test constant in `tests/Web.Tests.Bunit/Auth/Auth0ClaimsTransformationTests.cs` line 26
- Root cause: `appsettings.json` has `Auth0.RoleClaimNamespace = ""` (empty)
- When namespace is empty, `Auth0ClaimsTransformation.TransformAsync()` Pass 1 skips execution
- Pass 2 fallback looks for bare `"roles"` claim — Auth0 never sends this (only the namespaced form)
- Result: `ClaimTypes.Role` is never added to the principal

**Impact:**
- Profile > Roles & Permissions displays "No roles assigned"
- AdminPolicy checks fail (requires `ClaimTypes.Role == "Admin"`)
- NavMenu admin links hidden
- Admin dashboard access denied

**Config Fix (Issue #89):**
- Updated: `src/Web/appsettings.Development.json`
- Added: `"Auth0": { "RoleClaimNamespace": "https://issuetracker.com/roles" }`
- NOT added to `appsettings.json` — left as empty template per convention
- appsettings.Development.json is not in .gitignore (safe to commit)
- For production: use environment variable `Auth0__RoleClaimNamespace`
- For local development: User Secrets alternative available

**Verification:**
- `Auth0ClaimsTransformation` Pass 1 now executes (namespace configured)
- Namespaced claims are mapped to `ClaimTypes.Role`
- Profile and Admin UI now work correctly

**Files Changed:**
- `src/Web/appsettings.Development.json` (added Auth0 section)

**Decision Record:** `.squad/decisions/inbox/aragorn-role-claim-namespace.md`

**GitHub Comments Posted:**
- Issue #88: Diagnosis confirmed + documented
- Issue #89: Config applied + environment setup documented


### 2026-03-29 — Auth0 Role Claim Namespace Configuration (Sprint 1 Complete)

**Role:** Lead - Architecture & Coordination

**Work:**
- Diagnosed Auth0 role claim type requirement (Issue #88)
- Configured Auth0:RoleClaimNamespace in appsettings.Development.json (Issue #89)
- Confirmed namespace: `"https://issuetracker.com/roles"` (per test constant)
- Documented configuration requirement in decisions.md

**Key Finding:** Empty namespace cascades Auth0ClaimsTransformation to silent failure—Pass 1 skipped, Pass 2 looks for bare "roles" claim but Auth0 uses namespaced form, result: no ClaimTypes.Role added.

**Integration:** Coordinated with Sam (Pass 3 auto-detect) and Legolas (Profile.razor hardening) to create multi-layer defense against role claim misconfiguration.

**Outcome:** ✓ Build clean, issues resolved, team ready for next sprint.

---

### 2026-03-29 — Plan Ceremony Standard Process Implemented

**Role:** Lead - Architecture & Coordination

**Work:** 
- Designed and documented Plan Ceremony workflow for `/plan` command
- Updated `.squad/ceremonies.md` with comprehensive 4-phase Plan Ceremony protocol before Pre-Sprint Planning
- Added routing entry: `/plan` → Aragorn (Lead runs Plan Ceremony)
- Documented decision: all plan sessions MUST produce GitHub milestones + sprints before work begins

**Plan Ceremony Process:**
1. **Phase 1:** Create GitHub milestone via API (name from plan title/epic, optional due date)
2. **Phase 2:** Group todos into sprints (5–8 issues per sprint or logical grouping)
3. **Phase 3:** Create GitHub issues with `squad` label, milestone assignment, `sprint-{N}` labels, and `squad:{member}` routing
4. **Phase 4:** Present board summary table showing milestone + sprint structure

**Key Rules:**
- Sprint labels: `sprint-1`, `sprint-2`, etc. (auto-created)
- Sprint naming: `Sprint {N} — {theme}` (e.g., "Sprint 1 — Foundation")
- No issue worked without milestone + sprint assignment — this is the team's planning contract

**Decision recorded:** `.squad/decisions/inbox/aragorn-plan-ceremony.md`

### Formal PR Review Process Implementation (2026-03-29)
- **Task:** Lead orchestration of formal PR review process (approved by Matthew Paulosky)
- **Deliverables:**
  - Created `.github/pull_request_template.md` with domain checkboxes and self-review checklist
  - Updated `.squad/ceremonies.md`: 3 new ceremonies (PR Review Gate, CHANGES_REQUESTED Ceremony, Merge Conflict Resolution)
  - Updated `.squad/routing.md`: 4 new PR state signals (CHANGES_REQUESTED, CONFLICTED, CI FAILURE, ready-for-review)
  - Updated `.squad/agents/ralph/charter.md`: Pre-review gates (CI green, MERGEABLE, template filled) + pre-merge gates (APPROVED, CI green, no CHANGES_REQUESTED)
  - Documented review role matrix: Aragorn (all PRs) + domain specialists (Sam/Legolas/Gimli/Pippin/Boromir/Gandalf/Frodo per files changed)
  - Defined CHANGES_REQUESTED rejection protocol with author lockout and fix routing to non-author agent
  - Defined merge conflict resolution routing by domain
- **Status:** Complete, documented in `.squad/decisions.md`

---

### 2026-03-30 — Plan Ceremony: NavMenu Cleanup

Ran Plan Ceremony retroactively. Milestone: "NavMenu Cleanup — Sprint 1" (#3). Created 2 issues for NavMenu simplification work (#104, #105) and immediately closed them (work already done in branch `squad/nav-cleanup-and-admin-portal`).

**Process violation noted:** @copilot skipped ceremony step after plan approval. Reminded team: [[PLAN]] → Aragorn Plan Ceremony → issues → work begins.

### 2026-03-30 — Team Rule: AppHost.Tests Mandatory

**Enforced by:** Matthew Paulosky (User directive via Copilot)

**Rule:** AppHost.Tests (Playwright E2E) MUST be run locally before every push. No exceptions. If AppHost.Tests fail locally, they WILL fail in PR CI on GitHub. Claiming "all tests pass" without running AppHost.Tests is a false statement.

**Impact:** Affects all agents. Gate 4 in CI now includes mandatory AppHost.Tests check. Aragorn to enforce during code review routing.

---

### 2026-03-30 — Plan Ceremony: Test Gate Enforcement & Dev Workflow Hardening

**Session:** Squad Plan Ceremony post-sprint completion
**Outcome:** Milestone created, Sprint 1 completed & closed, Sprint 2 planned

**What Happened:**
- Reviewed PR #106 deliverables: Playwright E2E test fix, README sync action, Gate 4 hardening, AppHost.Tests mandatory
- Created GitHub milestone: "Test Gate Enforcement & Dev Workflow Hardening" (https://github.com/mpaulosky/IssueTrackerApp/milestone/4)
- Created 6 GitHub issues (4 Sprint 1, 2 Sprint 2) with proper routing and sprint labels
- Closed Sprint 1 issues #107–#110 (work already complete in PR #106)
- Added Plan Ceremony summary comment to PR #106

**Team Directive Captured:**
Matthew Paulosky: "AppHost.Tests MUST be run locally before every push — no exceptions — even if they take a long time."
- This is now documented in milestone, issue #110, and PR #106 comment
- Reflects strong commitment to test coverage enforcement

**Sprint 1 Issues (Closed):**
- #107: Playwright test fix (Pippin + Gimli)
- #108: README sync action (Frodo + Boromir)
- #109: Gate 4 hardening (Boromir)
- #110: AppHost.Tests mandatory (Boromir + Pippin)

**Sprint 2 Issues (Open):**
- #111: Hook install script (Boromir) — auto-install pre-push gate on fresh clone
- #112: CONTRIBUTING.md update (Frodo) — document gate requirements

**Key Learning:**
- GitHub CLI `gh milestone` command doesn't exist; use `gh api repos/{owner}/{repo}/milestones --input -` instead
- Multiple labels require separate `--label` flags (not comma-separated)
- Matthew's emphasis on "no exceptions" for AppHost.Tests reflects production-grade test gate philosophy

**Decision Document:** `.squad/decisions/inbox/aragorn-plan-ceremony-2026-03-30.md`


---

### 2026-04-01 — PR Review Session: Sprint 5 Admin User Management PRs (#146, #157, #158)

**Role:** Lead Reviewer

**PRs Reviewed:**

1. **PR #146 (Gandalf):** Auth0 Management API research spike — ADR only, no production code
   - **Verdict:** ✅ APPROVED
   - Research quality: Comprehensive ADR covering SDK choice, token caching, rate limits, secrets strategy
   - .squad/ file: `gandalf-auth0-management-api.md` is properly placed in `.squad/decisions/inbox/` and permissible on `squad/*` branch
   - All CI checks passed

2. **PR #157 (Gandalf):** Admin-only authorization policy for /admin/users routes (#135)
   - **Verdict:** ✅ APPROVED
   - Key changes: AccessDenied route alias, AuthorizeRouteView upgrade in Routes.razor, new Users.razor scaffold, Analytics.razor policy constant fix
   - File headers: ✅ All new files (Users.razor, AdminPolicyAuthorizationTests.cs) carry required copyright block
   - Tests: 7 new bUnit tests for AdminPolicy authorization — all passed
   - CI: ✅ All checks passed (23 jobs, 0 failures)

3. **PR #158 (Sam + Gandalf):** UserManagementService wrapping Auth0 Management API (#131)
   - **Verdict:** ❌ REJECTED — Architecture test failure must be fixed before merge
   - CI Status: ❌ Architecture.Tests failed — `AuditLogRepository` does not implement `IRepository<T>` (2 failures: `CodeStructureTests.Repositories_ShouldImplementIRepository` + `AdvancedArchitectureTests.AllRepositories_ShouldImplementIRepository`)
   - File headers: ✅ All new files carry required copyright block
   - .squad/ file violation: ❌ `.squad/decisions/inbox/gandalf-auth0-management-api.md` is included in PR diff on branch `squad/131-user-management-service` — this is the SAME ADR from PR #146. PR #158 should NOT modify `.squad/` files since it's implementing production code, not research.
   - VSA compliance: ✅ New code properly structured under `src/Web/Features/Admin/Users/` and `src/Domain/Features/Admin/`
   - Key architecture concern: `AuditLogRepository` in `src/Persistence.MongoDb/Repositories/` is named like a repository but does NOT implement `IRepository<T>` interface — breaking the repository pattern convention enforced by Architecture.Tests

**Key Findings:**

1. **PR #158 blocking issue:** `AuditLogRepository` must either:
   - (A) Implement `IRepository<RoleChangeAuditEntry>` and inherit from `Repository<RoleChangeAuditEntry>`, OR
   - (B) Be renamed to `AuditLogService` or `AuditLogWriter` if it's not a true repository pattern implementation

2. **PR #158 .squad/ file concern:** The ADR file should not be in PR #158's diff — it was already added in PR #146. If PR #158 was branched before PR #146 merged, this is a merge artifact — the fix is to rebase on latest main after PR #146 merges.

3. **Rate limit retry TODO:** PR #158 includes comments noting `// TODO: Rate limit retry on HTTP 429` per ADR — this is acceptable as a known-future enhancement, not a blocking issue.

**Merge Sequence Recommendation:**
1. Merge PR #146 first (research spike, no blockers)
2. Merge PR #157 next (authorization scaffold, all green)
3. PR #158 must be fixed:
   - Fix `AuditLogRepository` architecture violation
   - Rebase on main to eliminate duplicate `.squad/` file in diff
   - Re-run full CI to confirm Architecture.Tests pass
   - Then approve & merge

**Team Coordination:** Notified Sam (PR #158 author) of Architecture.Tests failure and `.squad/` diff issue. Gandalf's ADR work in PR #146 is excellent foundation for PR #158 implementation.


---

## Learnings (2026-04-02 — Process Review)
- Added Sprint Review + Issue Grooming ceremonies to ceremonies.md
- Added Admin User Mgmt + Labels domain routing signals to routing.md
- Added 2 new skills: auth0-management-api, labels-feature-patterns
- Audited 13 existing skills (see aragorn-skills-audit.md decision inbox)

---

## Learnings (2026-04-02 — Feature Investigation)

**Scope:** Full codebase feature gap analysis to surface 20 prioritised ideas for Matthew Paulosky.

**Key findings from investigation:**

### What is already well-built
- Issue CRUD, Comments, Labels, Voting, Attachments, Bulk Operations, debounced Search+Filters, Analytics (5-min IMemoryCache), User Dashboard, Admin panel (Categories/Statuses/Users/Audit), Email Notifications pipeline (SendGrid/SMTP), SignalR real-time, dark mode + 4 colour themes, Auth0 RBAC.
- `NotificationPreferences` model already models per-user email opt-ins (assigned, comment, status change, mention) — but **has zero UI**.
- `ExportAnalyticsQuery` already generates CSV export bytes — but **it is not wired to a download button on the Analytics page**.
- `IAuditLogRepository` + `IAuditLogWriterService` exist for role-change audits — **generalising to system-wide audit is low-effort**.
- Redis is provisioned by AppHost and health-checked by ServiceDefaults — but **the app uses `IMemoryCache` everywhere, never `IDistributedCache`**.
- `IBulkOperationQueue` interface is clean and injectable — but the implementation is **`InMemoryBulkOperationQueue` (not durable)**.

### Critical performance gap
`SearchIssuesQueryHandler` and `GetIssuesQueryHandler` both call `GetAllAsync()` (full collection load) then apply LINQ in-memory. This is **O(N)** and will collapse at scale. MongoDB Atlas Search is the correct fix at the database layer.

### Top 5 quick wins (high-value, low-complexity)
1. **User Notification Preferences UI** — model exists, page is missing (S)
2. **Due Dates + Priority Fields** — additive model change, no migration (S)
3. **Issue Watchers / Subscriptions** — `WatcherIds` on Issue + handler tweak (S)
4. **Redis Distributed Cache for Analytics** — Redis is already running, swap `IMemoryCache` → `IDistributedCache` (S)
5. **Background Job Visibility Admin Page** — `IBulkOperationQueue.GetStatusAsync` exists, new page only (S)

### Investigation output
Full structured investigation (20 ideas, prioritised) written to:
`.squad/decisions/inbox/aragorn-feature-ideas-2026-04-02.md`

---

## Learnings (2026-04-12 — Release-Process Abstraction)

### Release-Process Skill Refactoring Complete
**Decision:** Extracted monolithic, hardcoded release-process skill into generic two-layer architecture.

**Layer 1 — Generic Skill (`release-process-base/SKILL.md`):**
- Framework-agnostic patterns: versioning systems, merge strategies, branch models, CI/CD architecture
- Decision trees: "When to squash vs. merge?", "Which version system?", "How do I handle conflicts?"
- Anti-patterns: version bumps on release branch, manual publishing, mixed version systems
- 13,674 lines; reusable across .NET, Node.js, Python, Java ecosystems
- Replaces all hardcoded values with `{PLACEHOLDER}` parameters

**Layer 2 — Project Playbooks (e.g., `.release-config.json`):**
- Bind generic patterns to concrete project config
- Parameters: devBranch, releaseBranch, versionSystem, workflows, packageId, etc.
- Optional; can be inferred from repo state via `gh CLI`

### Hardcoding Analysis
**15+ hardcoded assumptions removed:**
- Repository: `FritzAndFriends/BlazorWebFormsComponents` → `{OWNER}/{REPO}` (inferred)
- Package ID: `Fritz.BlazorWebFormsComponents` → `{PACKAGE_ID}` (inferred from .csproj)
- Registry: `ghcr.io/fritzandfriends/...` → `{CONTAINER_REGISTRY}` (from secrets)
- Workflows: `.github/workflows/release.yml` → array of workflow names
- Versioning: NBGV only → supports 3 patterns (static file, tool-computed, tag-only)
- Merge strategy: merge commit → parameterized with decision criteria
- Branches: dev + main → `{DEV_BRANCH}` and `{RELEASE_BRANCH}`

### GitHub Metadata Inference (Safe)
**Read-only detection via gh CLI:**
- ✅ `gh repo view --field {name,owner,parent,defaultBranchRef}` — repo metadata
- ✅ `gh workflow list --all` — workflow file names
- ✅ `gh secret list --json name` — secret names only (never values)
- ✅ File inspection: `version.json`, `package.json`, `.csproj` for version scheme + package name
- ❌ Never use `gh secret get` (exposes values)
- ❌ Never parse `.github/workflows/*.yml` content (brittle)

### Architecture Patterns Documented
- **Two-branch model (recommended):** dev (features) + main (releases); preserves history, auditable tags
- **Single-branch model:** simpler for small projects; all history on main
- **Merge strategies:** merge commits (preferred for release history), squash (clean but loses context), rebase (linear but rewrites)

### Version System Abstraction
**Three patterns, each with trade-offs:**
1. Static file (`version.json`, `package.json`) — simple but requires manual bump
2. Tool-computed (`NBGV`, Maven, Cargo) — auto-increment but tool dependency
3. Tag-only — minimal deps but CI must parse tag

**Recommendation:** Choose one; mixing causes conflicts.

### Common Issues Resolved
- Version mismatch (tag vs. file) — root cause + diagnostic steps provided
- Merge conflicts during release PR — conflict resolution strategies
- CI/CD doesn't trigger — workflow trigger configuration debugging
- Package publishing fails — secret rotation + package ID verification

### Reusability Impact
**Before:** Skill locked to BlazorWebFormsComponents; manual editing for other projects
**After:** Generic skill + `.release-config.json` binding → reusable on any project
**Next Phase:** IssueTrackerApp playbook binding + validation

### Key Files Created
- `.squad/skills/release-process-base/SKILL.md` — generic skill, 13.6 KB
- `.squad/decisions/inbox/aragorn-release-process-generic.md` — decision + refactor roadmap
- *Pending:* IssueTrackerApp `.release-config.json` + project playbook

### Session Notes
- NBGV version conflicts in release CI well-understood (tool removal in release.yml mitigates)
- Fork + upstream pattern is BlazorWebFormsComponents-specific; single-repo common (removed assumption)
- Merge commits preserve release branch history for auditing; critical for long-lived branches
- Version bumps must be separate, reviewable commits on dev (prevents tag-version skew)


---

### 2026-04-12 — Release-Process Skill Genericization Review (Team Sync)

**Context:** Concurrent three-agent review of release-process skill portability across multiple projects. Aragorn led architecture design; Boromir validated GitHub discovery; Frodo designed portable template.

**Aragorn's Contribution:** Architected two-layer skill refactoring
- **Layer 1 (Generic):** release-process-base SKILL — framework-agnostic patterns (version bump mechanics, merge strategies, tagging semantics, CI/CD flow, troubleshooting)
- **Layer 2 (Project-Specific):** Project playbook binding — concrete parameters (REPO_OWNER, RELEASE_BRANCH, VERSION_FILE, PACKAGE_ID, WORKFLOWS, ARTIFACTS, DOCS_TOOL, CONTAINER_REGISTRY)
- **Inference Strategy:** Safe gh CLI discovery (repo owner, branches, workflows, secret names) plus filesystem detection (version.json, Dockerfile, mkdocs.yml) plus user prompts for release type and targets
- **Guardrails:** No hardcoded repo/workflow names, URLs, registries; never expose secret values; read-only gh access only

**Refactor Roadmap (P1-P4):**
1. P1 (Unblock) — Create generic skill base
2. P2 (Validate) — IssueTrackerApp playbook and .release-config.json
3. P3 (Deprecate, with Boromir) — Legacy skill markup
4. P4 (Automate, optional) — Inference scripting

**Key Decisions:** Approved — aligns with VSA abstraction principles. Boromir to review Phase 3. Frodo to document public generic skill.

**Merged to decisions.md:** 2026-04-12T19:37:30Z

---

### 2026-04-13 — Feasibility Assessment: dev/main Two-Branch Strategy

**Context:** Matthew Paulosky requested a read-only feasibility assessment of switching from single-branch (`main`) to two-branch (`dev` + `main`) model. Aragorn led comprehensive analysis across all documentation, workflows, squad conventions, release processes, and CI/CD pipelines.

**Scope:** 8 documentation files, 7 GitHub Actions workflows, GitVersion.yml, pre-push hook, 4 squad skills, release playbook, squad-promote pipeline, branch protection configuration.

**Prior Team Audits Reviewed:**
- Boromir (DevOps): Workflow/infrastructure audit — verdict: FEASIBLE, ~30 min effort, LOW risk
- Frodo (Tech Writer): Documentation audit — verdict: MODERATE impact, FEASIBLE, 3-4 hours + 15 min workflow

**Aragorn's Lead Assessment — Key Findings:**

1. **Infrastructure is pre-built.** `squad-promote.yml` already implements `dev → preview → main` flow. `squad-ci.yml` already triggers on `dev`. Tag-based release flow (`squad-release.yml`) is branch-agnostic. The `.copilot/skills/git-workflow/SKILL.md` already documents the three-branch model with dev-first workflow.

2. **Three discovery areas of concern:**
   - **GitVersion.yml gap:** No `dev` branch definition exists. Needs new branch config block with `is-release-branch: false`, appropriate pre-release label (e.g., `alpha`), and `source-branches: [main]`. Feature branches need `dev` added to their `source-branches`.
   - **squad-promote.yml Node.js artifact:** Lines 57, 90, 95, 114 reference `package.json` for version extraction. This is a .NET project using NBGV — these lines will fail. Must be replaced with `nbgv get-version -v NuGetPackageVersion` or `dotnet nbgv get-version -v Version`.
   - **squad-preview.yml is a stub:** Contains TODO placeholders. If going two-branch (skip preview), this is irrelevant. If going three-branch, it needs implementation.

3. **Recommendation: ADOPT WITH ADJUSTMENTS — Two-branch model (`dev` + `main`), defer `preview` tier.**

**Changes Required (by category):**

| Category | Item | Effort | Priority |
|----------|------|--------|----------|
| Branch creation | Create `dev` branch from `main` HEAD | 1 min | P0 |
| GitVersion.yml | Add `dev` branch config, update feature source-branches | 10 min | P0 |
| Pre-push hook | Gate 0: block `dev` AND `main` | 2 min | P0 |
| squad-test.yml | Add `dev` to push trigger | 2 min | P0 |
| CONTRIBUTING.md | 3 line changes + new release section | 30 min | P1 |
| New Work process.md | 3 line changes + release flow section | 30 min | P1 |
| squad-promote.yml | Fix `package.json` → NBGV version extraction | 15 min | P1 |
| GitHub branch protection | Protect `dev` (squash-only, required checks) | 5 min | P0 |
| Dependabot config | Verify targeting `dev` not `main` | 5 min | P1 |
| merged-pr-guard skill | Update "sync to main" → "sync to dev" | 5 min | P2 |
| release playbook | Update single-branch references → two-branch | 20 min | P2 |

**Risk Assessment:** 🟢 LOW — All three auditors (Aragorn, Boromir, Frodo) independently reached FEASIBLE verdict. No architectural blockers. Framework is 80% pre-built.

**Decision:** Filed to `.squad/decisions/inbox/aragorn-dev-main-branching.md`

**Learnings:**
- Squad infrastructure was designed for multi-branch from the start (promote, ci, preview workflows all pre-positioned)
- The `.copilot/skills/git-workflow/SKILL.md` already documents the target model — it was aspirational, not descriptive
- GitVersion.yml is the most technically nuanced change — pre-release labeling strategy affects SemVer output for all builds on `dev`
- squad-promote.yml contains Node.js artifacts (`package.json` version reads) that will fail in this .NET project — template debt from original squad framework
- Three prior assessments (Aragorn, Boromir, Frodo) converged on same verdict independently — strong signal

---

### 2026-04-12 — dev/main Branching Model Review (Architectural Lead)

**Context:** Matthew Paulosky requested team review of adopting `dev` as active development branch and `main` as release-only. Three-agent concurrent review: Aragorn (full), Aragorn (fast), Boromir (CI/CD), Frodo (docs).

**Aragorn's Role:** Full architectural and governance review (claude-opus-4.6, background).

**Analysis Scope:**
- Repository structure impact (branch naming, protection rules, role contract)
- CI/CD workflow implications (multi-branch triggers, promote flow, release gating)
- Release process alignment (tag-based triggers, version numbering, production deployment)
- Team collaboration patterns (PR routing, review expectations, developer workflows)
- Risk assessment and contingency planning

**Key Recommendations:**
1. **Adopt** — branch model is architecturally sound and alignment with squad framework
2. Treat `dev` as default PR merge target (change from `main`)
3. Update pre-push protection rules to gate on BOTH `dev` and `main`
4. Simplify preview/promotion assumptions in workflows — use explicit branch gating, not heuristics
5. Clear team communication on branch contracts: dev = "unstable", main = "production-ready"

**Architectural Findings:**
- Squad infrastructure was designed for multi-branch from the start; promote/ci/preview workflows pre-positioned
- `.copilot/skills/git-workflow/SKILL.md` already documents the target model — was aspirational, now descriptive
- GitVersion.yml pre-release labeling strategy is key nuance for dev builds (affects SemVer output)
- squad-promote.yml contains Node.js artifacts that will fail in this .NET project — template debt

**Coordination:**
- Fast verdict (Haiku) confirmed adoption path
- Boromir validated CI/CD feasibility with minimal friction
- Frodo assessed moderate documentation impact
- Coordinator synthesis: trending toward adoption with workflow/docs adjustments

**Output:** Detailed technical analysis, risk matrix, implementation roadmap filed to `.squad/orchestration-log/2026-04-12T20-17-00Z-aragorn-full-review.md` and `.squad/decisions.md`.

**Status:** ✅ Complete — Recommendation merged to team decisions.
