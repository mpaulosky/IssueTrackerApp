# Gimli — Learnings for IssueTrackerApp

**Role:** Tester - Quality Assurance
**Project:** IssueTrackerApp
**Initialized:** 2026-03-12

---

## Learnings

### 2026-03-14: bUnit Test Hang — Root Cause & Fix

**Problem:** `dotnet test tests/Web.Tests.Bunit/` hung indefinitely (5+ min timeout). 328 tests, ~1/3 never completed.

**Root Causes Found (3 issues in `BunitTestBase.cs`):**

1. **Circular DI factory (PRIMARY HANG CAUSE):** `SetupAuthenticatedUser()` registered a self-referencing factory:
   ```csharp
   Services.AddSingleton(sp => sp.GetRequiredService<AuthenticationStateProvider>());
   ```
   When called twice (once in constructor, once in test body), the factory became the LAST registration. Any component injecting `AuthenticationStateProvider` directly triggered infinite DI resolution → hang. Components affected: Dashboard, Categories, Statuses, Issue Index/Create/Edit/Details pages.

2. **NSubstitute IJSRuntime overriding bUnit's JSInterop:** `Substitute.For<IJSRuntime>()` was registered via `Services.AddSingleton(JsRuntime)`, overriding bUnit's built-in JSInterop. Tests using `JSInterop.SetupVoid(...)` and `JSInterop.VerifyInvoke(...)` were talking to bUnit's system while components used the NSubstitute mock. Result: assertion failures on JSInterop verification tests.

3. **Missing service registrations:** `ILookupService`, `SignalRClientService`, `BulkSelectionState`, `ToastService`, `ILogger<T>` were not registered. Page components that inject these failed during render.

**Fixes Applied:**
- Removed the circular factory line from `SetupAuthenticatedUser()`
- Removed NSubstitute `IJSRuntime` mock; set `JSInterop.Mode = JSRuntimeMode.Loose`
- Added `ILookupService` (NSubstitute mock), `ToastService` (real instance), `BulkSelectionState` (real instance), `SignalRClientService` (real instance with NullLogger), `ILoggerFactory`/`ILogger<>` (NullLogger)
- Note: `SignalRClientService` is `sealed` — cannot use `Substitute.For<T>()`, must instantiate directly

**Result:** Full suite (328 tests) completes in ~1 second. 242 pass, 86 fail (pre-existing assertion mismatches, not hangs).

**Key Takeaway:** Never register `sp => sp.GetRequiredService<T>()` as a factory for service type `T` — it creates circular resolution. Also, never override bUnit's `IJSRuntime` with NSubstitute; use `JSInterop.Mode = JSRuntimeMode.Loose` instead.

---

## Notes

- Team transferred from IssueManager squad
- Same tech stack: .NET 10, Blazor, Aspire, MongoDB, Redis, Auth0, MediatR
- Ready to begin development