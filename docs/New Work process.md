# New Work Process

> **Authoritative detail:** See `.squad/ceremonies.md` for the full ceremony
> definitions, command examples, and gate checklists.

---

## Phase 1 — Planning

1. **User creates or refines `plan.md`** (via `/plan` mode or `[[PLAN]]` prefix)
2. **User approves `plan.md`** — no GitHub objects are created before user sign-off
3. **Aragorn runs the Plan Ceremony:**
   - Creates the GitHub Milestone via `gh api`
   - Derives sprint groupings (5–8 issues per sprint, by logical dependency)
   - Creates a GitHub issue for each todo; assigns `sprint-{N}` and `squad:{member}`
     routing labels
   - Posts a board summary: milestone number, sprint themes, issue assignments

> Work is routed to domain specialists — not done by "Team" as a unit:
> UI/Blazor → Legolas · Tests → Gimli · CI/DevOps → Boromir ·
> MongoDB/persistence → Sam · Docs/prose → Frodo · Architecture/CQRS → Aragorn

---

## Phase 2 — Sprint Execution *(repeat for each sprint)*

### Setup

1. Assigned squad member picks up their issue
2. Creates a **worktree** on a new branch based on `origin/main`:

   ```text
   Branch name: squad/{issue-number}-{kebab-case-slug}
   Worktree path: ../IssueTrackerApp-sprint
   ```

### Do the work

1. Implements the feature, including tests and documentation, following all
   `.github/instructions/` coding standards
2. All new C# (`.cs`) files must include the copyright block header — `.razor`
   files do **not** get copyright headers

### Pre-push gate *(mandatory — enforced by `.git/hooks/pre-push`)*

1. Run the full local test suite in **Release** configuration:

   ```bash
   dotnet test tests/Unit.Tests tests/Blazor.Tests tests/Architecture.Tests \
     --configuration Release
   ```

2. Zero test failures, zero build errors, zero warnings required
3. Resolve any failures before proceeding — CI must never be the first place
   failures are discovered

### Open the PR

1. Commit changes, push branch, open PR:
   - Branch must be `squad/*`
   - PR body must reference the issue (`Closes #{N}`) and have at least one
     filled `[x]` checkbox from the PR template
   - `.squad/` files must NOT appear in the diff

---

## Phase 3 — PR Review Gate *(Ralph monitors)*

Ralph checks all gates before spawning reviewers:

| Gate               | Pass condition              |
| ------------------ | --------------------------- |
| CI green           | All checks `pass`           |
| No merge conflicts | `MERGEABLE`                 |
| Branch naming      | Starts with `squad/`        |
| PR template filled | At least one `[x]` checkbox |

1. **Ralph spawns parallel domain reviewers** — Aragorn always reviews; relevant
   specialists (Legolas, Gimli, etc.) review their domain
2. Reviewers post verdicts via GitHub PR review (`--approve` or
   `--request-changes`)
3. If **CHANGES_REQUESTED**: PR author is locked out; a *different* squad member
   fixes and pushes to the same branch — then re-review begins
4. **Unanimous approval + CI green** → Ralph squash-merges and deletes the branch:

   ```bash
   gh pr merge {N} --squash --delete-branch
   ```

   **Why squash?** One commit per PR keeps `git log --oneline main` readable as a
   changelog. GitHub auto-links the squash commit to the PR and issue, so full
   traceability is preserved without merge-commit topology noise.

   The squash commit message **must** follow Conventional Commits format and include
   the issue reference — this makes `git log` double as release notes:

   ```text
   feat: add label suggestions to issue form (Closes #215)
   fix: resolve race condition in bulk operation queue (Closes #198)
   docs: rewrite New Work process to reflect squad ceremonies (Closes #215)
   ```

5. Ralph runs **Post-Merge Orphan Branch Cleanup** ceremony automatically —
   removes stale local and remote `squad/*` refs and the sprint worktree

---

## Phase 4 — Sprint Close

- Ralph monitors: when all issues in a sprint are merged and closed, the sprint
  is complete
- **The previous sprint's PR must be merged to `main` before the next sprint
  begins** — this eliminates merge conflicts and ensures each sprint builds on a
  stable, fully-integrated baseline
- Begin next sprint (return to Phase 2)

---

## Phase 5 — Milestone Completion

1. All sprints merged, all issues closed → **Close the Milestone**
2. `milestone-blog.yml` fires automatically — creates a Ralph review issue
   (`squad:ralph` + `pending-review` labels) with a release checklist
3. **Ralph reviews** and applies one of two labels to the issue:
   - `release-candidate` — work is shippable and warrants a GitHub Release
   - `blog-only` — improvements noted but no release needed
4. `milestone-release-decision.yml` detects the label and routes automatically:

```text
release-candidate
  └─ squad-milestone-release.yml → GitHub Release created (tag + release notes)
       └─ release-blog.yml fires on publish → squad:bilbo brief issue created

blog-only
  └─ squad:bilbo brief issue created directly
```

1. **Bilbo** picks up the brief, writes the blog post, and adds a row to
   `docs/blog/index.md`
2. `blog-readme-sync.yml` detects the index change and auto-updates the
   **README Dev Blog section** — no manual README edit needed
