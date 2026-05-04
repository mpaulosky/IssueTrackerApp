---
date: 2026-05-04
author: boromir
status: active
---

# Azure Storage SDK + Azurite Emulator API Version Mismatch Pattern

## Decision

When Azure Storage SDK (Azure.Storage.Blobs) is upgraded and introduces a newer Azure Storage API version that Azurite emulator doesn't yet support, use the `--skipApiVersionCheck` flag to bypass the version check in integration tests until Azurite adds native support.

## Context

- **Problem:** Dependabot bumped Azure.Storage.Blobs from 12.20.0 to 12.27.0 in PR #276
- **Impact:** Persistence.AzureStorage.Tests.Integration failed with: `The API version 2026-02-06 is not supported by Azurite`
- **Root Cause:** Azure.Storage.Blobs 12.27.0 uses API version 2026-02-06, but Testcontainers.Azurite 4.11.0 bundles Azurite 3.35.0 which only supports up to 2024-11-04
- **Azurite Lag:** Azurite emulator typically lags behind Azure Storage service API versions by several months (tracked in Azure/Azurite#2623)

## Implementation

In `tests/Persistence.AzureStorage.Tests.Integration/AzuriteFixture.cs`:

```csharp
public AzuriteFixture()
{
    _container = new AzuriteBuilder("mcr.microsoft.com/azure-storage/azurite:latest")
        .WithCommand("azurite-blob --blobHost 0.0.0.0 --skipApiVersionCheck")
        .Build();
}
```

## Why This Works

- The `--skipApiVersionCheck` flag tells Azurite to accept any API version without validation
- This allows integration tests to run with newer SDK versions while Azurite catches up
- The flag is officially documented in Azurite's command-line options
- This is the recommended workaround by the Azurite maintainers

## Trade-offs

**Pros:**
- Unblocks Dependabot PRs for Azure.Storage.Blobs upgrades
- Maintains test coverage while waiting for Azurite updates
- Simple one-line fix in test infrastructure

**Cons:**
- Tests may not catch API-specific behavior differences (new features in 2026-02-06 won't be validated)
- Masks the version mismatch — team should monitor Azurite releases and remove the flag once native support is added

## When to Revisit

- Check Azurite releases periodically: https://github.com/Azure/Azurite/releases
- When Azurite adds support for API version 2026-02-06, remove the `--skipApiVersionCheck` flag
- Consider adding a TODO comment in the code with a link to the tracking issue

## References

- Azure/Azurite#2623: Support service version 2026-02-06
- Azure/Azurite#2627: Discussion on API version mismatch
- PR #276: Bump Azure.Storage.Blobs to 12.27.0
- Commit 77928c7: Applied fix to AzuriteFixture

## Related Patterns

- **Testcontainers Command Customization:** Use `.WithCommand()` to pass custom startup arguments to Docker containers in integration tests
- **Azure SDK Dependency Management:** Monitor Azure SDK upgrades for API version changes that may impact emulator compatibility
