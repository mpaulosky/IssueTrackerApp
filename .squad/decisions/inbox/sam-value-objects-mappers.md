# Decision: Value Object and Mapper Infrastructure (WI-1)

**Author:** Sam (Backend Developer)
**Date:** 2026-03-14
**Status:** Implemented

## Context

Foundation work for the DTO-Model separation sprint. Models currently embed DTOs (UserDto, CategoryDto, StatusDto) directly. This creates tight coupling between the API contract (DTOs) and the persistence layer (Models).

## Decision

1. **Value objects** (`UserInfo`, `CategoryInfo`, `StatusInfo`) created as `sealed class` in `Domain.Models` namespace
2. **Static mapper classes** created in new `Domain.Mappers` namespace for all entity ↔ DTO conversions
3. BSON attributes explicitly set to match current DTO serialization — no MongoDB data migration needed
4. Value objects nest other value objects (e.g., `CategoryInfo.ArchivedBy` is `UserInfo`) for clean DDD composition

## Consequences

- WI-3 (Model refactoring) can now swap embedded DTOs for value objects
- WI-4 (Mapper updates) can update mappers when model types change
- Zero risk to existing data — BSON field names are identical
- Additive-only change — no existing files modified
