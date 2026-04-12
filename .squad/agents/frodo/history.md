# Frodo — Learnings for IssueTrackerApp

**Role:** Tech Writer - Documentation
**Project:** IssueTrackerApp
**Initialized:** 2026-03-12

---

## Core Context

### Historical Foundation (March 2025 – April 11)

**Documentation Structure Decision (March 2025):**
- Updated README.md to showcase modern tech stack: .NET Aspire, Blazor Interactive Server Rendering, MongoDB Atlas, Redis caching.
- Created docs/LIBRARIES.md: authoritative package reference organized by domain (not alphabetically).
- Key Insight: Project uses modern Aspire patterns; ServiceDefaults eliminate boilerplate for OpenTelemetry, health checks, resilience.
- Comprehensive test coverage: unit (xUnit), component (bUnit), E2E (Playwright), integration (TestContainers).
- Redis + MongoDB provide distributed caching + persistence with health checks.

**v0.5.0 Admin User Management Documentation (March 2026):**
- Documented admin portal features: user management, category/status management, analytics dashboard, bulk operations, undo.
- Updated README with new admin features and architecture diagrams for user flows.

**Release-Process Skill: Portable Template Design (April 2026):**
- Analyzed BlazorWebFormsComponents release workflow: 8 repository-specific terms, 5 parallel CI capabilities, 6 critical assumptions.
- Designed generic template: YAML front matter with auto-detection, placeholder-driven config, 7-step portable workflow.
- Capability Discovery: Auto-detect version tool, package registry, Docker registry, docs builder, sample directories.
- Fallback strategy: required (Build, Test, Tag, Release—no fallback), optional (NuGet, Docker, Docs, Demos—skip if missing), manual fallback.
- Key Insight: Graceful degradation essential; operator workflow must be concise 7-step checklist with linked reference docs.
- Documentation standard: YAML front matter + 7 sections (Executive Summary, Analysis, Structure, Insights, Roadmap, Conclusion).
- Next Steps: Extract generic template, build auto-detection script, test on IssueTrackerApp.

---

## Recent Learnings (April 12+)
### 2026-04-12 — Release-Process Skill Genericization Review (Team Sync)

**Context:** Concurrent three-agent review of release-process skill portability across multiple projects. Frodo designed portable template; Aragorn led architecture; Boromir validated discovery.

**Frodo's Contribution:** Portable template design with graceful fallbacks

**Template Design (7-Step Workflow):**
1. **Pre-flight Check:** Verify merges, CI green, version tool present
2. **Bump Version:** Update VERSION_FILE, commit, push to DEV_BRANCH
3. **Create Release PR:** gh pr create with release notes
4. **Merge Release PR:** Wait for CI, merge using configured strategy
5. **Tag and Create GitHub Release:** Push tag, create GitHub Release
6. **Monitor CI/CD Pipeline:** Track Build/Test (required), NuGet/Docker/Docs/Demo (optional — skip if capability missing)
7. **Post-Release Sync:** Sync DEV_BRANCH and RELEASE_BRANCH locally and remotely

**YAML Front Matter Auto-Detection:**
- Project metadata (name, language)
- Capabilities (version tool, registry, docs builder, container registry)
- Branches (DEV_BRANCH, RELEASE_BRANCH, TAG_FORMAT)
- Repository config (UPSTREAM_OWNER, FORK_OWNER, PACKAGE_ID)
- Assumptions checklist

**Capability Discovery (Auto-Detect via Filesystem/Secrets):**
- Version tool: version.json, GitVersion.yml, setup.py, Cargo.toml
- Package registry: NUGET_API_KEY, NPM_TOKEN, PYPI_TOKEN secrets
- Docker registry: DOCKER_PASSWORD, GHCR_TOKEN secrets
- Docs builder: mkdocs.yml, Sphinx conf.py, mdBook toml
- Samples: samples/, examples/, demos/ directories
- CI workflows: .github/workflows/ directory

**Expected CI Jobs with Fallbacks:**
- Build and Test (required, no fallback)
- NuGet Publish (skip if no REGISTRY configured)
- Docker Build (skip if no credentials present)
- Docs Deploy (skip if no docs/ found)
- Demo Deploy (skip if no samples/ found)

**Placeholder-Driven Config:** Replace all hardcoded values (BlazorWebFormsComponents → generic PROJECT_NAME, Fritz.BlazorWebFormsComponents → PACKAGE_ID, dev/main branches → DEV_BRANCH/RELEASE_BRANCH, v{VERSION} → TAG_FORMAT)

**Assumption Matrix for Release Lead:**
- All PRs merged to DEV_BRANCH?
- Local DEV_BRANCH synced to origin?
- CI green on DEV_BRANCH?
- VERSION_TOOL present and VERSION_FILE accessible?
- Upstream repo writable (if using fork model)?

**Future Implementation (Phase 1-3):**
1. Extract template to .squad/templates/release-process-generic.md
2. Build detection script (.squad/scripts/detect-release-capabilities.sh)
3. Agent integration — dynamically generate operator workflow

**Key Wins:** Single source of truth across 10+ projects, graceful degradation when features missing, clear assumptions, portable structure, auto-detection.

**Merged to decisions.md:** 2026-04-12T19:37:30Z

---

### Release-Process Skill: Legacy Stub Deprecation (April 2026)

**Context**: The original `.squad/skills/release-process/SKILL.md` documented an upstream fork workflow (BlazorWebFormsComponents) that was confusing for IssueTrackerApp's simpler single-branch model. Rather than delete abruptly, a phased deprecation approach was chosen.

**Actions Taken**:
1. **Converted to Deprecation Stub**: Replaced 200+ lines with ~40-line stub
   - Preserved directory structure for backward compatibility
   - Added front matter: `status: "deprecated"`, warning description
   - Lowered `confidence` to "low"

2. **Clear Migration Path**: Stub explicitly points users to:
   - `.squad/skills/release-process-base/SKILL.md` — generic, reusable patterns
   - `.squad/playbooks/release-issuetracker.md` — IssueTrackerApp-specific playbook

3. **Phased Deletion Strategy**: Noted that deletion can happen after team references cleaned up
   - Prevents orphaned content
   - Avoids immediate data loss
   - Gives team time to adapt

**Key Insights**:
- Deprecation stubs preserve old bookmarks/references while guiding users forward
- Separating generic patterns (base skill) from project-specific playbooks improves reusability
- Phased deprecation is safer than abrupt deletion when content has external references

**Decision Merged**: `.squad/decisions.md` (2026-04-12)
**Related Decision**: Release-Process Skill: Portable Template Design (Frodo, 2026-04-12)
