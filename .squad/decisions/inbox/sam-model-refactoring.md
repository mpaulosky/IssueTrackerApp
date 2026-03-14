# Decision: Comment.Issue → Comment.IssueId (ObjectId)

**Author:** Sam (Backend)
**Date:** 2026-03-14
**Status:** Implemented
**Work Items:** WI-3, WI-4

## Context

The `Comment` model previously embedded a full `IssueDto` object, creating a circular dependency between Comment and Issue through their DTOs. As part of WI-3 (replacing DTO types in models with value objects), we needed to decide how to handle this reference.

## Decision

Replaced `IssueDto Issue` with `ObjectId IssueId` in both the `Comment` model and `CommentDto` record.

- `Comment.IssueId` stores only the MongoDB ObjectId reference with `[BsonElement("issue_id")]`
- `CommentDto.IssueId` is a plain `ObjectId` instead of an embedded `IssueDto`
- The EF Core `CommentConfiguration` no longer has a nested `OwnsOne` for the embedded Issue document

## Rationale

1. **Breaks circular dependency:** Comment no longer depends on IssueDto/Issue types
2. **Follows MongoDB best practices:** Reference by ID rather than embedding full documents (avoids data duplication and staleness)
3. **Simpler serialization:** ObjectId is a primitive; no nested owned type configuration needed
4. **Consistent with Attachment model:** Attachment already used `ObjectId IssueId` for the same relationship

## Impact

- **Handlers (WI-5):** Comment handlers that previously accessed `comment.Issue.Id` must use `comment.IssueId` directly. Handlers needing full issue data must load it separately via the Issue repository.
- **Tests (WI-7):** Test fakes/fixtures that constructed Comments with embedded IssueDtos need updating.
- **MongoDB migration:** Existing documents with embedded Issue sub-documents will need a migration script to extract just the ObjectId reference.

## Alternatives Considered

- **Keep IssueDto in CommentDto but ObjectId in Comment model:** Would require the mapper to load the full Issue to build the DTO — adds complexity and a database call in the mapper layer.
- **Create an IssueInfo value object:** Rejected as over-engineering; the relationship only needs the ID reference.
