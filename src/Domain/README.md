# Domain Layer

This project contains the domain entities, business logic, and domain events for the IssueTrackerApp.

## Folder Structure

- **Abstractions/** - Shared interfaces and base classes (e.g., IRepository<T>, Result<T>)
- **Entities/** - Domain entities and value objects
- **Features/** - Vertical slice feature folders with commands/queries using CQRS

## Key Patterns

- CQRS with MediatR
- Repository pattern
- Result pattern for error handling
- FluentValidation for input validation
