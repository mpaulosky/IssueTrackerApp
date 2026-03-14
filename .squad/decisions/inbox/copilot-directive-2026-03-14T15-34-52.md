### 2026-03-14T15:34:52Z: User directive
**By:** Matthew Paulosky (via Copilot)
**What:** DTOs should only be used to transfer records between the layers of the application. There should be mappers that convert DTO to model and model to DTO. The only entities to interact with the database should be the models. This is a mandatory architectural pattern going forward.
**Why:** User request — captured for team memory
