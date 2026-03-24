# Decision: Skipped Test Audit Results

**Author:** Gimli (Tester)
**Date:** 2025-07-17
**Status:** Informational

## Context

Audited all 8 skipped tests across the test suite. All skip reasons remain valid.

## Findings

### Two blocking gaps prevent unskipping:

1. **MediatR ValidationBehavior pipeline not wired** (3 tests in `IssueEndpointTests.cs`)
   - FluentValidation validators exist but no `IPipelineBehavior<,>` implementation enforces them.
   - **Action needed:** Aragorn or Legolas should implement `ValidationBehavior<TRequest, TResponse>` and register it in DI. Once done, unskip the 3 validation tests.

2. **Auth0 test infrastructure incomplete** (5 tests in `AuthEndpointSecurityTests.cs`)
   - `TestWebApplicationFactory` registers a "Test" auth scheme but endpoints use `Auth0Constants.AuthenticationScheme`.
   - **Action needed:** Either map the test scheme to Auth0's expected scheme name, or refactor endpoints to use a configurable scheme. Then unskip the 5 auth tests.

## Recommendation

Track these as backlog items so they don't stay skipped indefinitely.
