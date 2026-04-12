# Release Process — IssueTrackerApp Project Playbook

**Last Updated:** 2026-04-12  
**Ref:** `.squad/skills/release-process-base/SKILL.md`  
**Project:** IssueTrackerApp  
**Owner:** Boromir (DevOps) + Aragorn (Release Approval)

---

## Project Configuration

### Repository & Branches

| Parameter | Value | Notes |
|-----------|-------|-------|
| **Owner** | mpaulosky | |
| **Repo** | IssueTrackerApp | Single-owner fork (no upstream) |
| **Dev Branch** | — | TBD: Use `main` (single-branch model) or create `dev`? |
| **Release Branch** | main | Current default |
| **Default Branch** | main | All PRs merge here |

**Decision:** IssueTrackerApp currently uses **single-branch model** (all work on `main`). Consider `dev` branch if/when team scales.

### Version Management

| Parameter | Value | Notes |
|-----------|-------|-------|
| **Version System** | NBGV | Nerdbank.GitVersioning |
| **Version File** | `version.json` | At repo root |
| **Tag Prefix** | `v` | e.g., `v1.0.0` |
| **Package ID** | IssueTrackerApp | From `.csproj` |
| **Merge Strategy** | merge | Preserve commit history on main |

**version.json reference:**
```json
{
  "version": "1.0.0",
  "publicReleaseRefSpec": [
    "^refs/heads/main$",
    "^refs/tags/v\\d+(?:\\.\\d+)?$"
  ]
}
```

### Artifacts & Deployments

| Artifact | Triggered By | Produced By | Deployed To |
|----------|--------------|-------------|------------|
| **Build Verification** | release published | `.github/workflows/build.yml` | (logs only) |
| **Unit Tests** | release published | `.github/workflows/build.yml` | (logs only) |
| **Integration Tests** | release published | `.github/workflows/integration-tests.yml` | (logs only) |
| **Docker Image** | TBD | (not yet configured) | (not yet deployed) |
| **Documentation** | TBD | (not yet configured) | (not yet deployed) |
| **NuGet Package** | TBD | (not yet configured) | (not yet deployed) |

**Status:** Minimal release pipeline. Extend as needed.

---

## Step-by-Step Release Process (IssueTrackerApp)

### Prerequisites

- [ ] All feature PRs merged to `main` (single-branch model)
- [ ] `main` branch CI passing (build + tests green)
- [ ] No unmerged feature branches
- [ ] Release notes prepared (in PR body or CHANGELOG.md)

### Phase 1 — Version Bump

Since we use **NBGV**, version is auto-computed. To lock a release version:

```bash
# Edit version.json
# Current version: 1.0.0
# Release version: 1.0.0 (no bump if first release)
# Next dev version: 1.0.1-preview (NBGV auto-increments after tag)

# Commit the bump (or skip if already correct)
git add version.json
git commit -m "Bump version to 1.0.0"
git push origin main
```

**Note:** After release tag, NBGV will auto-increment to `1.0.1-preview.X` on main. No manual update needed.

### Phase 2 — Create Release PR

**Skipped for single-branch model.** Release PR would merge `dev` → `main`, but since we use only `main`, just verify main is current:

```bash
git fetch origin
git checkout main
git reset --hard origin/main
```

### Phase 3 — Tag and Release

After main is current and CI passes:

```bash
# Tag the release
git tag -a v1.0.0 -m "Release v1.0.0"
git push origin v1.0.0

# Create GitHub Release (triggers CI/CD)
gh release create v1.0.0 \
  --repo mpaulosky/IssueTrackerApp \
  --title "v1.0.0" \
  --notes "Release v1.0.0

## What's Included
- Issue CRUD with Labels, Priorities, Due Dates
- Comment Threading
- Bulk Operations (Edit, Delete)
- User Dashboard
- Admin Panel (Categories, Statuses, Users, Audit Log)
- Email Notifications (SendGrid/SMTP)
- Dark Mode + Color Themes
- Auth0 RBAC
- Redis Caching
- Real-time Updates (SignalR)

## Breaking Changes
None

## Bug Fixes
- [#123] Fixed comment edit not reflecting immediately
- [#124] Resolved empty search result display

## Contributors
- Matthew Paulosky" \
  --target main
```

### Phase 4 — Verify CI/CD Pipeline

Visit https://github.com/mpaulosky/IssueTrackerApp/releases/tag/v1.0.0 and confirm:

- ✅ **build.yml** job passed (Build + Unit Tests)
- ✅ **integration-tests.yml** job passed (Playwright E2E)
- ✅ No workflow failures

**If any job fails:**
```bash
# Delete tag and release
git tag -d v1.0.0
git push origin :v1.0.0
gh release delete v1.0.0 --confirm

# Fix the issue on main
git commit -m "Fix: [issue]"
git push origin main

# Retry release
# Repeat Phase 3
```

### Phase 5 — Post-Release

```bash
# Sync local main
git fetch origin
git checkout main
git reset --hard origin/main

# Verify version.json auto-incremented (or manually bump to next dev version)
git log -1 --format="%h %s"

# Document in CHANGELOG.md (optional)
echo "## v1.0.0 ($(date +%Y-%m-%d))" >> CHANGELOG.md
echo "" >> CHANGELOG.md
echo "- Issue CRUD with Labels, Priorities, Due Dates" >> CHANGELOG.md
git add CHANGELOG.md
git commit -m "docs: Update CHANGELOG for v1.0.0"
git push origin main
```

---

## Common Issues (IssueTrackerApp-Specific)

### Issue: Build Fails on Release Tag

**Symptom:** `v1.0.0` tag created, but build.yml workflow fails

**Root Cause:** .csproj or build script expects `version.json` in a specific location

**Fix:**
```bash
# Verify version.json is at repo root
ls -la version.json

# Check .csproj includes NBGV reference
grep -i "nbgv" Directory.Build.props

# If NBGV removed for release (per release.yml logic), manually verify version
dotnet build -p:Version=1.0.0
```

### Issue: Integration Tests Timeout on Release

**Symptom:** `.github/workflows/integration-tests.yml` times out after 15 minutes

**Root Cause:** Playwright E2E test is slow; needs optimization or longer timeout

**Fix:** Contact Pippin (Tester E2E). May need to:
- Increase GitHub Actions timeout
- Skip E2E on release tags (if desired)
- Parallelize E2E tests

### Issue: Docker Image Not Built

**Symptom:** Release created but no Docker image attached

**Root Cause:** Docker workflow not configured for IssueTrackerApp; Dockerfile may not exist

**Fix:** Boromir to configure `.github/workflows/publish-container.yml` when Docker deployment is ready.

---

## Secrets & Permissions

| Secret | Used By | Type | Status |
|--------|---------|------|--------|
| `GITHUB_TOKEN` | CI/CD (auto-provided) | Built-in | ✅ Active |
| `NUGET_API_KEY` | (not used yet) | Manual | ⏸️ Not configured |
| `AZURE_WEBAPP_WEBHOOK_URL` | (not used yet) | Manual | ⏸️ Not configured |

**To Deploy Docker or NuGet Packages:**
1. Contact Boromir (DevOps)
2. Configure secrets in GitHub
3. Update release workflow to include new jobs

---

## Future Extensions

- [ ] **Docker Image Publishing:** Add `publish-container.yml` when container deployment is needed
- [ ] **NuGet Package Publishing:** Add `publish-nuget.yml` + configure `NUGET_API_KEY` secret
- [ ] **Documentation Deployment:** Add `docs.yml` when GitHub Pages docs site is ready
- [ ] **Multi-Branch Model:** Consider `dev` branch when team grows beyond single owner
- [ ] **Automated Release Notes:** Script CHANGELOG.md generation from PR titles

---

## Reference

- **Generic Skill:** `.squad/skills/release-process-base/SKILL.md`
- **Decision:** `.squad/decisions/inbox/aragorn-release-process-generic.md`
- **Current Workflows:** `.github/workflows/build.yml`, `integration-tests.yml`, `push` triggers
- **GitHub Docs:** https://docs.github.com/en/repositories/releasing-projects-on-github/managing-releases-in-a-repository

**Owner for Updates:** Aragorn (Lead) + Boromir (DevOps)  
**Last Reviewed:** 2026-04-12
