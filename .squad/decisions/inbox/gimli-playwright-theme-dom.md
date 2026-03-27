# Decision: Playwright Theme DOM Assertions & Auth0 State Pattern

**Author:** Gimli (Tester)
**Date:** 2026

---

## Confirmed: Theme DOM Selectors

### Dark Mode Detection
- **Attribute:** `document.documentElement.classList.contains('dark')`
- **Selector usage:** `await page.EvaluateAsync<bool>("document.documentElement.classList.contains('dark')")`
- **When true:** dark mode is active; when false light/system mode is active
- **Toggled by:** clicking `button[aria-label="Toggle theme"]` then choosing "Light", "Dark", or "System"

### Color Scheme Detection
- **Attribute:** `document.documentElement.getAttribute('data-theme')`
- **Selector usage:** `await page.EvaluateAsync<string>("document.documentElement.getAttribute('data-theme')")`
- **Values:** `'blue'` | `'red'` | `'green'` | `'yellow'`
- **Default:** `'blue'` (applied on page load when no localStorage key is set)
- **Changed by:** clicking `button[aria-label="Change color scheme"]` then `button[title="Blue|Red|Green|Yellow"]`

---

## Auth State Pattern for Auth0 Tests

### Strategy: One-Time Login + Cached Storage State

The `AuthStateManager` static class performs a single Auth0 login and caches the Playwright
[storage state](https://playwright.dev/dotnet/docs/auth) (cookies + localStorage) to a JSON file.
All subsequent authenticated tests reuse the stored state by loading it into a fresh browser context.

**Key design decisions:**
1. `SemaphoreSlim(1,1)` guards the one-time login to prevent race conditions in parallel xUnit test runs.
2. The login page uses a temporary browser context with `IgnoreHTTPSErrors = true` to handle dev HTTPS certs.
3. Storage state is persisted to `Path.GetTempPath() + "issuetracker-playwright-auth.json"` (Playwright convention).
4. If `PLAYWRIGHT_TEST_EMAIL` / `PLAYWRIGHT_TEST_PASSWORD` env vars are absent, `GetStorageStatePathAsync` returns `null` and `InteractWithAuthenticatedPageAsync` skips the test gracefully (no exception).

### Login Flow
```
navigate → /account/login?returnUrl=/
wait for Auth0 Universal Login (NetworkIdle)
fill input[name="username"]
fill input[name="password"]
click button[type="submit"]
WaitForURLAsync(url => url.StartsWith(baseUrl), timeout: 30s)
save page.Context.StorageStateAsync(path: ...)
```

### Authenticated Context Options
```csharp
new BrowserNewContextOptions
{
    IgnoreHTTPSErrors = true,
    ColorScheme = ColorScheme.Dark,
    StorageStatePath = statePath,
    BaseURL = uri.ToString()
}
```
