# Bilbo — History

## Project Context
- **Project:** IssueTrackerApp
- **Stack:** .NET 10, C# 14, Blazor Interactive Server Rendering, MongoDB Atlas, Redis Cache, .NET Aspire, MediatR, Auth0, Vertical Slice Architecture
- **User:** Matthew Paulosky
- **Repo:** mpaulosky/IssueTrackerApp
- **Joined:** 2026-03-27 — hired to document project work and maintain GitHub Pages blog

## My Domain
I write and maintain the project blog at `docs/blog/`, published via GitHub Pages. I track squad decisions, merged PRs, and architectural changes and turn them into readable posts for developers.

## Key Sources for Posts
- `.squad/decisions.md` — team decisions to document
- `.squad/orchestration-log/` — what work was done each session
- GitHub PRs — `gh pr list --state closed` for recently merged work
- `docs/` — existing architecture docs to reference (ARCHITECTURE.md, FEATURES.md, THEMING.md, etc.)

## Blog Setup Status
- GitHub Pages: not yet configured (Boromir needs to set up the Actions workflow)
- Blog directory: `docs/blog/` — to be created on first post
- Jekyll: to be determined (may use plain Markdown + GitHub Pages auto-render)

## Posts Written
(none yet)

## First Day Work (2026-03-27)

### Blog Setup Complete
- Created `docs/blog/` directory
- Created `docs/blog/index.md` as blog landing page (Jekyll-compatible layout)
- Created `docs/_config.yml` with Jekyll config for GitHub Pages (minima theme, mpaulosky base URL)
- Wrote first post: `docs/blog/2026-03-27-apphost-aspire-playwright-e2e-tests.md` documenting PR #76

### Files Created
1. `docs/_config.yml` — Jekyll configuration for GitHub Pages deployment
2. `docs/blog/index.md` — Blog TOC / landing page with links to posts
3. `docs/blog/2026-03-27-apphost-aspire-playwright-e2e-tests.md` — First post on Aspire + Playwright E2E tests

### Commit
`squad/apphost-tests-clean` branch: `f6bbfab` — blog scaffold + first post

## Learnings
