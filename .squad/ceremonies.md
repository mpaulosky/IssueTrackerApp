# Squad Ceremonies

## Defined Ceremonies

### Pre-Sprint Planning

- **Trigger:** manual ("run sprint planning", "plan the sprint")
- **When:** before
- **Facilitator:** Aragorn
- **Participants:** Aragorn, Sam, Legolas, Gimli, Boromir
- **Purpose:** Review open issues, prioritize, assign squad labels

### Build Repair Check

- **Trigger:** automatic — enforced by `.git/hooks/pre-push` on every `git push`
- **When:** before push (gate 2) and before PR (gate 3)
- **Facilitator:** Aragorn
- **Participants:** Aragorn (runs build-repair prompt)
- **Purpose:** Ensure zero errors, zero warnings, all tests pass before pushing
- **Critical rules (learned from PR #86 incident):**
  1. **Always use `--configuration Release`** — CI uses Release; Debug builds hide missing files. Never accept a Debug-only passing build.
  2. **Stage ALL untracked `.razor`/`.cs` files before committing** — run `git status --short` and treat any `??` source file as a blocker. Files present on disk but untracked are invisible to CI.
  3. **Loop until green** — the pre-push hook retries build+tests up to 3 times. Fix errors between retries. Do not bypass the hook.
  4. **Hook enforcement:** `.git/hooks/pre-push` runs automatically. It checks for untracked source files (Gate 1), runs `dotnet build --configuration Release` (Gate 2), then runs Architecture/Domain/bUnit tests (Gate 3). Push is blocked if any gate fails.

### Retro

- **Trigger:** manual ("run retro", "retrospective")
- **When:** after
- **Facilitator:** Aragorn
- **Participants:** all
- **Purpose:** What went well, what didn't, action items

### PR Review Gate

- **Trigger:** automatic, when any PR is ready for review (CI checks passing)
- **When:** after (CI passes)
- **Facilitator:** Aragorn
- **Participants:** Aragorn (always) + domain specialists based on PR content:
  - DevOps/CI workflow changes → Boromir
  - Security-relevant changes (auth, permissions, secrets, endpoints) → Gandalf
  - Test file changes → Gimli and/or Pippin
  - Backend/API changes → Sam
  - Frontend/Blazor changes → Legolas
  - Blog/docs changes → Bilbo (author) or Frodo
- **Purpose:** Quality and security gate before merge
- **Protocol:**
  1. Wait for ALL CI checks to pass
  2. Spawn Aragorn + relevant domain reviewers in parallel
  3. Collect all verdicts — ALL must approve (unanimous)
  4. If ANY reviewer rejects: route fixes to a DIFFERENT agent (strict lockout — PR author cannot self-revise)
  5. Push fixes to PR branch → wait for CI to re-pass → repeat review cycle
  6. All approved + CI green → squash merge
- **Merge command:** `gh pr merge {N} --squash --delete-branch`
- **Post-merge:** `git checkout main && git pull origin main && git fetch --prune`
- **Lockout rule:** Rejected artifact's original author is locked out of that revision cycle. Fixes must come from a different team member.

### Standard Task Workflow

- **Trigger:** When starting any new task or issue
- **When:** throughout (setup → planning → implementation → review → cleanup)
- **Facilitator:** Agent or human working the task
- **Participants:** Task owner, reviewers (for PR phase)
- **Purpose:** Ensure consistent task execution with proper branch isolation and verification
- **Enforcement:** The pre-push hook (Gate 0) blocks direct pushes to `main` — you must use a `squad/{issue}-{slug}` feature branch

#### Phases

##### Phase 1: Setup

1. Sync with main:

   ```bash
   git checkout main
   git pull origin main
   ```

2. Create branch:

   ```bash
   git checkout -b squad/{issue-number}-{kebab-slug}
   ```

3. Push branch to GitHub:

   ```bash
   git push -u origin squad/{issue-number}-{kebab-slug}
   ```

4. If branch falls behind main during work:

   ```bash
   git merge origin/main
   ```

##### Phase 2: Planning

1. Analyze the problem
2. Document approach (in session plan, issue, or PR description)
3. Get user/stakeholder approval before implementing

##### Phase 3: Implementation

1. Make changes in the branch
2. Test locally
3. Iterate until complete
4. Commit and push — the pre-push hook (`.git/hooks/pre-push`) enforces all gates automatically:
   - **Gate 0:** Block direct push to `main`
   - **Gate 1:** Warn/block on untracked `.razor`/`.cs` files (invisible to CI)
   - **Gate 2:** `dotnet build IssueTrackerApp.slnx --configuration Release` (must match CI exactly — **never** Debug-only)
   - **Gate 3:** `Architecture.Tests` + `Domain.Tests` + `Web.Tests.Bunit` (Release, no-build)
   - Hook loops up to 3 attempts with a "fix and retry" prompt between attempts
5. If a gate fails, fix the issue and re-run `git push` — the hook will re-execute all gates from scratch

##### Phase 4: Review & Merge

1. **CI must pass first.** Do not request review while checks are pending or failing.
   - Poll: `gh pr checks {N}` — wait for all green

2. **Spawn reviewers in parallel** (Aragorn always + domain specialists):
   - Aragorn — lead review (scope, architecture, correctness, naming conventions)
   - Boromir — if any `.github/workflows/` or CI config changed
   - Gandalf — if any auth, permissions, secrets, or security-relevant code changed
   - Gimli/Pippin — if test files changed
   - Sam — if backend/domain/persistence code changed
   - Legolas — if Blazor components or frontend changed

3. **Unanimous approval required.** All spawned reviewers must approve.
   - If rejected: identify fixes → route to a DIFFERENT agent (not the PR author, lockout enforced) → push fixes → wait for CI → repeat from step 1

4. **Merge (squash):**
   ```bash
   gh pr merge {N} --squash --delete-branch
   ```

5. **Update local main:**
   ```bash
   git checkout main
   git pull origin main
   git fetch --prune
   ```

6. **Clean up orphan local branches:**
   ```bash
   git branch --merged main | grep -v "^\* main" | xargs git branch -d
   ```

##### Phase 5: Cleanup

1. Return to main and sync:

   ```bash
   git checkout main
   git pull origin main
   ```

2. Optionally delete local branch:

   ```bash
   git branch -d squad/{issue-number}-{kebab-slug}
   ```

### Integration Points

- **Build Repair Check:** Enforced via pre-push hook (Phase 3, step 4)
- **Code Review:** Triggered when PR is opened (Phase 4, step 2-3)
- **Merged-PR Branch Guard:** Check before committing to avoid stranded commits
