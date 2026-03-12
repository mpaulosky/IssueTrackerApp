# Squad Ceremonies

## Defined Ceremonies

### Pre-Sprint Planning

- **Trigger:** manual ("run sprint planning", "plan the sprint")
- **When:** before
- **Facilitator:** Aragorn
- **Participants:** Aragorn, Sam, Legolas, Gimli, Boromir
- **Purpose:** Review open issues, prioritize, assign squad labels

### Build Repair Check

- **Trigger:** automatic, when: "before push" or "before PR"
- **When:** before
- **Facilitator:** Aragorn
- **Participants:** Aragorn (runs build-repair prompt)
- **Purpose:** Ensure zero errors, zero warnings, all tests pass before pushing

### Retro

- **Trigger:** manual ("run retro", "retrospective")
- **When:** after
- **Facilitator:** Aragorn
- **Participants:** all
- **Purpose:** What went well, what didn't, action items

### Code Review

- **Trigger:** automatic, when PR is opened
- **When:** after
- **Facilitator:** Aragorn
- **Participants:** Aragorn (reviewer), original author (locked out of their own revision if rejected)
- **Purpose:** Quality gate before merge

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
4. Commit and push — all 5 pre-push gates must pass:
   - Copyright headers
   - Code formatting (`dotnet format`)
   - Api.Tests.Unit
   - Shared.Tests.Unit
   - Web.Tests.Unit + Web.Tests.Bunit
   - Architecture.Tests + AppHost.Tests.Unit
5. If pre-push fails, fix issues and retry

##### Phase 4: Review & Merge

1. Create PR:

   ```bash
   gh pr create --title "..." --body "Closes #{issue-number}" --base main
   ```

2. Wait for CI checks to pass
3. Address any review comments
4. Once approved and green, merge (prefer squash merge for clean history)

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
