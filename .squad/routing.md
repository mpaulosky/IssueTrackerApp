# Squad Routing

## Signal → Agent

| Signal | Agent | Notes |
|--------|-------|-------|
| Architecture, scope, decisions, code review, PR review | Aragorn | Lead |
| Blazor, Razor, UI, frontend, components, CSS | Legolas | Frontend |
| MongoDB, repositories, API endpoints, backend services, MediatR handlers | Sam | Backend |
| Tests, quality, edge cases, test failures, test review | Gimli | Tester |
| CI/CD, GitHub Actions, NuGet, deployment, Aspire infra, protected branch | Boromir | DevOps |
| Docs, README, XML docs, comments, CONTRIBUTING | Frodo | Docs |
| Auth0, authentication, authorization, JWT, RBAC, security audit, vulnerabilities, injection, XSS, CSRF, secrets, HTTPS, CORS, security headers, security review | Gandalf | Security |
| GitHub board, issues, PRs, backlog, work queue | Ralph | Work Monitor |
| Untriaged issues (squad label, no squad:* sub-label) | Aragorn | Lead triages |
| squad:aragorn | Aragorn | — |
| squad:legolas | Legolas | — |
| squad:sam | Sam | — |
| squad:gimli | Gimli | — |
| squad:boromir | Boromir | — |
| squad:frodo | Frodo | — |
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
