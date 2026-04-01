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
- Release posts for v0.3.0 (Polish Sprint) and v0.4.0 (Voting & Prioritization) were written on 2026-04-01 to catch up on 2 missed releases. Both posts match the established tone and structure of v0.2.0 release post.
- v0.3.0 focuses on dashboard UI polish, Redis caching, issue restore command, and assignee field support.
- v0.4.0 is a major feature release: complete CQRS voting system with `VoteIssueCommand`/`UnvoteIssueCommand`, domain events, SignalR broadcasts on vote thresholds, and idempotent vote design.
- Blog catch-up process: Release posts should be triggered immediately when GitHub Release is published (not after a delay). Recommend that Ralph (DevOps) or the release process signals Bilbo to write the post synchronously.
