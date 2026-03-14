# Theme Components bUnit Tests

## Overview
This test suite provides comprehensive testing for the Theme-related Blazor components in the IssueTrackerApp. The tests cover component rendering, user interactions, state management, and theme persistence.

## Test File Location
`tests/Web.Tests.Bunit/Theme/ThemeComponentTests.cs`

## Components Tested

### 1. ThemeProvider Component
**Purpose:** Manages theme state (dark/light mode, color scheme) and provides cascading values to child components.

**Tests (4 tests):**
- `ThemeProvider_RendersSuccessfully` - Verifies component renders with child content
- `ThemeProvider_InitializesThemeOnFirstRender` - Tests theme initialization on first render
- `ThemeProvider_CascadesValueToChildren` - Validates cascading of provider to child components
- `ThemeProvider_HandlesJSException_DuringInitialization` - Tests error handling for prerendering scenario

### 2. ThemeToggle Component
**Purpose:** Provides a dropdown menu for switching between light, dark, and system theme modes.

**Tests (8 tests):**
- `ThemeToggle_RendersWithButton` - Confirms button renders with correct attributes
- `ThemeToggle_ShowsSunIcon_InLightMode` - Verifies sun icon displays in light mode
- `ThemeToggle_ShowsMoonIcon_InDarkMode` - Verifies moon icon displays in dark mode
- `ThemeToggle_DropdownOpens_OnButtonClick` - Tests dropdown open/close functionality
- `ThemeToggle_DropdownContains_AllThemeModes` - Validates all three theme options are present
- `ThemeToggle_HighlightsCurrentTheme` - Tests current theme mode highlighting
- `ThemeToggle_CallsSetThemeModeAsync_OnSelection` - Verifies theme change via dropdown
- `ThemeToggle_ClosesDropdown_AfterSelection` - Tests dropdown closes after selection
- `ThemeToggle_TitleReflectsCurrentMode` - Validates button title reflects current mode
- `ThemeToggle_DisposesEventHandler` - Tests proper cleanup on disposal

### 3. ColorSchemeSelector Component
**Purpose:** Allows users to select accent color scheme (Blue, Red, Green, Yellow).

**Tests (9 tests):**
- `ColorSchemeSelector_RendersWithButton` - Confirms button renders with correct attributes
- `ColorSchemeSelector_DropdownOpens_OnButtonClick` - Tests dropdown open/close functionality
- `ColorSchemeSelector_DropdownContains_AllColorSchemes` - Validates all four color options
- `ColorSchemeSelector_HighlightsCurrentScheme` - Tests current scheme highlighting
- `ColorSchemeSelector_DisplaysColorSwatches` - Verifies color swatches are displayed
- `ColorSchemeSelector_CallsSetColorSchemeAsync_OnSelection` - Tests color scheme change
- `ColorSchemeSelector_ClosesDropdown_AfterSelection` - Tests dropdown closes after selection
- `ColorSchemeSelector_DisplaysColorSchemeLabel` - Validates label is displayed
- `ColorSchemeSelector_DisposesEventHandler` - Tests proper cleanup on disposal

### 4. Integration Tests
**Purpose:** Tests theme components working together and theme state persistence.

**Tests (8 tests):**
- `Theme_BothComponents_RenderTogether` - Verifies both toggle and selector render together
- `Theme_StateIsShared_BetweenComponents` - Validates theme state is shared across components
- `Theme_OnThemeChanged_TriggeredOnThemeChange` - Tests event triggering on theme change
- `Theme_ColorScheme_PersistsAcrossThemeModeChanges` - Validates color scheme persists
- `Theme_ThemeMode_PersistsAcrossColorSchemeChanges` - Validates theme mode persists
- `Theme_SystemPreferenceChanged_UpdatesIsDarkMode` - Tests system preference change callback
- `Theme_SystemPreferenceChanged_IgnoredInNonSystemMode` - Validates proper system preference handling

## Test Statistics
- **Total Test Classes:** 4
- **Total Test Methods:** 29
- **File Size:** 27,605 bytes

## Key Testing Patterns

### JavaScript Interop Mocking
```csharp
// For methods returning values
JSInterop.Setup<string>("themeManager.getThemeMode").SetResult("light");
JSInterop.Setup<bool>("themeManager.shouldUseDarkMode").SetResult(false);

// For void methods
JSInterop.SetupVoid("themeManager.watchSystemPreference");

// For exception scenarios
JSInterop.Setup<string>("method").SetException(new JSException("error"));
```

### Component Rendering with Cascading Values
```csharp
var themeProvider = Render<ThemeProvider>(parameters =>
    parameters.AddChildContent<ThemeToggle>());
```

### Dropdown and Event Testing
```csharp
var button = component.Find("button");
await button.ClickAsync(new());
var menuItems = component.FindAll("button[role='menuitem']");
```

## Coverage Areas

### Component Lifecycle
- ✅ Initial render and initialization
- ✅ Parameter changes
- ✅ Disposal and cleanup
- ✅ Event subscription/unsubscription

### User Interactions
- ✅ Button clicks
- ✅ Dropdown toggle
- ✅ Menu item selection
- ✅ State updates via user actions

### State Management
- ✅ Theme mode changes (light/dark/system)
- ✅ Color scheme changes
- ✅ State persistence across component boundaries
- ✅ Event triggering and handling

### JavaScript Interop
- ✅ localStorage integration
- ✅ System preference detection
- ✅ Error handling during JS interop
- ✅ Exception scenarios (prerendering)

### UI Rendering
- ✅ Icon display based on mode
- ✅ Menu items rendering
- ✅ Color swatches display
- ✅ Selection highlighting
- ✅ Accessibility attributes (aria-label, role)

## Running the Tests

### Run all theme tests:
```bash
dotnet test tests/Web.Tests.Bunit/Web.Tests.Bunit.csproj --logger "console" -v normal
```

### Run specific test class:
```bash
dotnet test tests/Web.Tests.Bunit/Web.Tests.Bunit.csproj --filter "ClassName"
```

### Run with code coverage:
```bash
dotnet test tests/Web.Tests.Bunit/Web.Tests.Bunit.csproj /p:CollectCoverage=true
```

## Dependencies

### Using Global
- Xunit
- FluentAssertions
- NSubstitute
- Bunit
- Bunit.TestDoubles
- Microsoft.Extensions.DependencyInjection
- Microsoft.AspNetCore.Components
- Microsoft.AspNetCore.Components.Rendering

### Base Class
- `BunitTestBase` - Provides JSInterop mocking and service setup

## Notes

1. **JSInterop Setup:** All JS interop calls must be mocked before rendering the component
2. **Cascading Parameters:** ThemeProvider must wrap child components to provide cascading values
3. **Async Operations:** Theme mode/color scheme changes are async and properly awaited in tests
4. **Event Cleanup:** Components properly clean up event subscriptions on disposal
5. **Accessibility:** Tests verify proper ARIA attributes and semantic HTML

## Future Enhancements

- Add tests for mobile/responsive behavior
- Add tests for keyboard navigation in dropdowns
- Add performance tests for rapid theme switching
- Add tests for localStorage persistence across sessions
- Add tests for theme transitions/animations
