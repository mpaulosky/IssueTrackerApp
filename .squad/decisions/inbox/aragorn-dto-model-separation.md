# ADR: DTO–Model Separation

**Author:** Aragorn (Lead Developer)
**Date:** 2025-07-22
**Status:** Accepted
**Scope:** Domain, Persistence, CQRS Handlers, Services, Tests

---

## Context

The IssueTrackerApp currently embeds DTO types (`CategoryDto`, `UserDto`, `StatusDto`, `IssueDto`) directly inside domain Model classes that are persisted to MongoDB. For example:

```csharp
// Current — Issue.cs (domain model stored in MongoDB)
public class Issue
{
    public CategoryDto Category { get; set; } = CategoryDto.Empty;
    public UserDto Author { get; set; } = UserDto.Empty;
    public StatusDto Status { get; set; } = StatusDto.Empty;
    public UserDto ArchivedBy { get; set; } = UserDto.Empty;
}
```

This violates clean architecture: DTOs (data transfer objects) should never be persistence concerns. MongoDB serializes these DTO records directly into documents, coupling the transfer layer to the database schema. The same pattern exists in `Category`, `Status`, `Comment`, and `Attachment` models.

---

## Decision

We will enforce strict DTO–Model separation:

1. **Models** (domain entities with BSON attributes) are the ONLY types that interact with the database
2. **DTOs** (immutable records) are used ONLY for transferring data between layers
3. **Mappers** (static classes in `Domain/Mappers/`) provide explicit, testable, bidirectional conversion
4. **Value Objects** (`UserInfo`, `CategoryInfo`, `StatusInfo`) replace embedded DTO properties in Models

### Target Model Structure

```csharp
// Target — Issue.cs
public class Issue
{
    [BsonId] public ObjectId Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public CategoryInfo Category { get; set; } = CategoryInfo.Empty;
    public UserInfo Author { get; set; } = UserInfo.Empty;
    public StatusInfo Status { get; set; } = StatusInfo.Empty;
    public UserInfo ArchivedBy { get; set; } = UserInfo.Empty;
    // ... other properties
}
```

### Conversion Flow

```
UI → DTO → Mapper.ToInfo() → Model → Repository → MongoDB
MongoDB → Repository → Model → Mapper.ToDto() → DTO → UI
```

### Comment.Issue Change

`Comment.Issue` (currently `IssueDto`) will change to `Comment.IssueId` (`ObjectId`). This eliminates a circular dependency where Comment stored a full IssueDto which itself referenced other DTOs. The comment query handler will look up the issue separately when constructing the response DTO.

---

## Consequences

### Positive
- Clean architecture: no DTO types in the persistence layer
- Models can evolve independently of API contracts
- Explicit mappers are testable and auditable
- Eliminates circular DTO references (Comment → IssueDto → UserDto → ...)
- Value objects provide clear BSON serialization boundaries

### Negative
- ~140 files must change (models, handlers, services, tests)
- Temporary build breakage during model refactoring (must be done atomically on a feature branch)
- Slight verbosity increase: mapper calls replace direct DTO assignment

### Risks
- MongoDB deserialization must be validated: BSON element names on value objects must match previous DTO serialization
- Large mechanical test update required (~80 test files)

---

## Implementation

See sprint plan: `plan.md` in session state.

13 ordered work items covering: value objects, mappers, mapper tests, model refactoring, DTO constructor updates, handler updates, service updates, all test project updates, architecture test enforcement, data migration verification, and cleanup.
