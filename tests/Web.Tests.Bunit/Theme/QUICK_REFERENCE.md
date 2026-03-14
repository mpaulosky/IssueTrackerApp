# Theme Components bUnit Tests - Quick Reference

## Test File Structure

```
tests/Web.Tests.Bunit/Theme/
├── ThemeComponentTests.cs       # Main test file (29 tests)
└── TEST_SUMMARY.md              # This summary document
```

## Test Organization

The tests are organized into 4 main test classes:

### 1. **ThemeProviderTests** (4 tests)
Tests the core theme management component that handles state and cascading.

### 2. **ThemeToggleTests** (9 tests) 
Tests the theme mode toggle component (light/dark/system).

### 3. **ColorSchemeSelectorTests** (9 tests)
Tests the color scheme selector component (blue/red/green/yellow).

### 4. **ThemeIntegrationTests** (8 tests)
Tests how theme components work together and state persistence.

## Common Test Patterns

### Setup JavaScript Interop (All Tests)
```csharp
// String returns
JSInterop.Setup<string>("themeManager.getThemeMode").SetResult("light");

// Boolean returns  
JSInterop.Setup<bool>("themeManager.shouldUseDarkMode").SetResult(false);

// Void methods
JSInterop.SetupVoid("themeManager.watchSystemPreference");

// Exception scenarios
JSInterop.Setup<string>("method").SetException(new JSException("error"));
```

### Render Component with Theme Provider
```csharp
var themeProvider = Render<ThemeProvider>(parameters =>
    parameters.AddChildContent<ThemeToggle>());
```

### Test User Interactions
```csharp
// Click a button
var button = component.Find("button");
await button.ClickAsync(new());

// Find dropdown items
var menuItems = component.FindAll("button[role='menuitem']");
```

### Verify JavaScript Calls
```csharp
JSInterop.VerifyInvoke("themeManager.setThemeMode", calledTimes: 1);
```

## Key Assertions Used

- `Should().NotBeNull()` - Element exists
- `Should().HaveCount(n)` - Expected number of elements
- `GetAttribute("name")` - Read HTML attributes
- `TextContent` - Check text content
- `Should().Contain()` - Check string contains
- `Should().Be()` - Exact equality
- `Should().BeTrue()` / `Should().BeFalse()` - Boolean assertions

## Test Naming Convention

`[Component]_[Scenario]_[ExpectedResult]`

Examples:
- `ThemeToggle_DropdownOpens_OnButtonClick`
- `ColorSchemeSelector_HighlightsCurrentScheme`
- `Theme_StateIsShared_BetweenComponents`

## Components Tested

### ThemeProvider.razor
- **Location**: src/Web/Components/Theme/ThemeProvider.razor.cs
- **Role**: Manages theme state and cascading values
- **Key Methods**: SetThemeModeAsync, SetColorSchemeAsync, OnSystemPreferenceChanged

### ThemeToggle.razor
- **Location**: src/Web/Components/Theme/ThemeToggle.razor
- **Role**: UI for switching between light/dark/system modes
- **Key Features**: Dropdown menu, icon changes, accessibility attributes

### ColorSchemeSelector.razor
- **Location**: src/Web/Components/Theme/ColorSchemeSelector.razor
- **Role**: UI for selecting accent color scheme
- **Key Features**: Color swatches, dropdown menu, selection highlighting

## Running Tests

```bash
# Build test project
dotnet build tests/Web.Tests.Bunit/Web.Tests.Bunit.csproj

# Run all tests
dotnet test tests/Web.Tests.Bunit/Web.Tests.Bunit.csproj

# Run specific test class
dotnet test tests/Web.Tests.Bunit/Web.Tests.Bunit.csproj --filter "ThemeToggleTests"

# Run with verbose output
dotnet test tests/Web.Tests.Bunit/Web.Tests.Bunit.csproj -v detailed

# Run with code coverage
dotnet test tests/Web.Tests.Bunit/Web.Tests.Bunit.csproj /p:CollectCoverage=true
```

## Using Statements Required

```csharp
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Web.Components.Theme;
```

## Test Base Class

All tests inherit from `BunitTestBase` which provides:
- `JSInterop` - Mock JavaScript interop setup
- `Render<T>()` - Render Blazor components
- `FindAll()`, `Find()`, `FindComponent<T>()` - Query rendered components
- Service mocks (Mediator, IssueService, etc.)

## Key Testing Considerations

1. **JS Interop Must Be Mocked**: All JS calls to `themeManager` must be set up
2. **Cascading Provider Required**: Child components need ThemeProvider as cascade
3. **Async Operations**: Theme changes are async, use `await` where needed
4. **Event Cleanup**: Components dispose event handlers properly
5. **State Isolation**: Each test sets up its own component instance

## Common Issues & Solutions

**Issue**: "No matching JSInterop setup"
- **Solution**: Add `JSInterop.Setup<T>()` or `JSInterop.SetupVoid()` for the method

**Issue**: "CascadingParameter is null"
- **Solution**: Wrap child component in ThemeProvider when rendering

**Issue**: "Elements not found"
- **Solution**: Ensure components have rendered; use `Render()` if needed

**Issue**: "Async context error"
- **Solution**: Use `await component.InvokeAsync()` for state changes

## Testing Checklist for New Theme Tests

- [ ] Mock all JS interop calls
- [ ] Wrap components in ThemeProvider for cascading
- [ ] Test both happy path and error scenarios
- [ ] Verify accessibility attributes (aria-label, role)
- [ ] Check event cleanup on disposal
- [ ] Test component parameter changes
- [ ] Verify state changes trigger UI updates
- [ ] Test dropdown open/close behavior
- [ ] Check current selection highlighting
- [ ] Validate title/tooltip text

## References

- [bUnit Documentation](https://bunit.dev/)
- [Component Test Examples](tests/Web.Tests.Bunit/Theme/ThemeComponentTests.cs)
- [Theme Components Source](src/Web/Components/Theme/)
