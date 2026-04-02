---
last_updated: 2026-04-02T00:00:00.000Z
---

# Current Focus

**Project:** IssueTrackerApp  
**Status:** v0.6.0 released — Labels Feature shipped  
**Date:** 2026-04-02

## What We've Built

A full-featured issue tracking application:

- Users can raise issues with categories, labels, attachments, and comments
- Users can vote on issues to determine priority order
- Admin users can manage issue status, categories, labels, and user roles
- Real-time notifications via SignalR and email (SendGrid/SMTP)
- Analytics dashboard with charts (bar, line, pie)
- Bulk operations with undo support

## Tech Stack

- .NET 10 / C# 14
- Blazor (Interactive Server Rendering)
- .NET Aspire
- MongoDB Atlas + EF Core MongoDB Provider
- Redis Cache
- MediatR + Vertical Slice Architecture
- Result<T> Response Pattern
- Auth0 (Authentication + Authorization with Roles + Management API)

## Sprints Completed

1. Sprint 1 — Core issue CRUD + categories + statuses
2. Sprint 2 — Comments + attachments (Azure Blob / local fallback)
3. Sprint 3 — Voting + dashboard + analytics
4. Sprint 4 — Notifications (SignalR + email pipeline)
5. Sprint 5 — Admin User Management (Auth0 Management API)
6. Sprint 6 — Labels Feature (full CRUD, label assignment, filtering)

## Current State

- **v0.6.0 tag** released on main
- Process optimisation session complete (2026-04-02): ceremonies enhanced, routing updated, 3 new skills, histories summarized, decisions archived
- Sprint 7 not yet planned

## Next Steps

To be determined in Sprint 7 planning.
