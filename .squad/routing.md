# Squad Routing

## Signal → Agent

| Signal | Agent | Notes |
|--------|-------|-------|
| /plan, plan mode, [[PLAN]] | Aragorn | Lead runs Plan Ceremony after plan.md is approved |
| Architecture, scope, decisions, code review, PR review | Aragorn | Lead |
| Blazor, Razor, UI, frontend, components, CSS | Legolas | Frontend |
| RoleBadge, EditUserRolesModal, UserAuditLogPanel, UserListTable components | Legolas | Frontend — admin UI components |
| LabelInput component, label filter chips, label autocomplete, multi-value input | Legolas | Frontend — LabelInput component |
| MongoDB, repositories, API endpoints, backend services, MediatR handlers | Sam | Backend |
| Admin user management, UserManagementService, Auth0 Management API, admin roles, /admin/users | Sam | Backend — admin user CQRS handlers |
| Labels, label filtering, AddLabelCommand, RemoveLabelCommand, ILabelService | Sam | Backend — Labels CQRS + service |
| Unit tests, bUnit, MongoDB integration tests, test quality review | Gimli | Tester |
| Playwright E2E tests, Aspire integration tests, test infrastructure | Pippin | Tester (E2E) |
| CI/CD, GitHub Actions, NuGet, deployment, Aspire infra, protected branch | Boromir | DevOps |
| Docs, README, XML docs, comments, CONTRIBUTING | Frodo | Docs |
| Blog posts, GitHub Pages, project announcements, changelog posts, feature write-ups | Bilbo | Tech Blogger |
| Auth0, authentication, authorization, JWT, RBAC, security audit, vulnerabilities, injection, XSS, CSRF, secrets, HTTPS, CORS, security headers, security review | Gandalf | Security |
| GitHub board, issues, PRs, backlog, work queue | Ralph | Work Monitor |
| Session log, orchestration log, history summarization, decisions archival, memory sweep | Scribe | Memory management |
| PR with reviewDecision: CHANGES_REQUESTED | Aragorn | Lead routes fix to non-author agent |
| PR with mergeable: CONFLICTED | Aragorn | Lead determines resolver by file domain |
| PR with statusCheckRollup: FAILURE | Boromir + author agent | CI failure: Boromir diagnoses, author fixes |
| PR ready for review (CI green, no conflicts) | Aragorn + domain reviewers | Spawn per files-changed table in ceremonies.md |
| Untriaged issues (squad label, no squad:* sub-label) | Aragorn | Lead triages |
| squad:aragorn | Aragorn | — |
| squad:legolas | Legolas | — |
| squad:sam | Sam | — |
| squad:gimli | Gimli | — |
| squad:pippin | Pippin | — |
| squad:boromir | Boromir | — |
| squad:frodo | Frodo | — |
| squad:bilbo | Bilbo | — |
| squad:gandalf | Gandalf | — |
| squad:copilot | @copilot | Auto-assign: false |

## Branching Policy

- Squad work branches: `squad/{issue-number}-{slug}` — exempt from Protected Branch Guard
- NEVER commit `.squad/` files on `feature/*` branches — guard will block the PR
- Scribe commits `.squad/` changes on `squad/*` branches only

## Skill-Aware Routing

Before spawning any agent, check `.squad/skills/` for relevant skills:

- Any push/commit work → `.squad/skills/pre-push-test-gate/SKILL.md`
- Any build/test failure → `.github/prompts/build-repair.prompt.md`
- Any integration test work → `.squad/skills/pre-push-test-gate/SKILL.md` (Integration section)
