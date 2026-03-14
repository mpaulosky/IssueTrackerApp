# Theme Components bUnit Test Suite - Index

## 📚 Documentation Files

This directory contains comprehensive bUnit tests for the Theme components in IssueTrackerApp.

### Files in This Directory

1. **ThemeComponentTests.cs** (Main Test File)
   - 30 test methods organized in 4 test classes
   - Complete coverage of all theme components
   - Ready to run with `dotnet test`

2. **TEST_SUMMARY.md** (Detailed Documentation)
   - Comprehensive overview of all tests
   - Test descriptions and assertions
   - Component details and responsibilities
   - Coverage areas and patterns used

3. **QUICK_REFERENCE.md** (Quick Lookup Guide)
   - Test organization overview
   - Common test patterns with examples
   - Running tests commands
   - Troubleshooting tips
   - Testing checklist

4. **TEST_COVERAGE_MAP.md** (Coverage Analysis)
   - Test breakdown by component
   - Coverage summary table
   - Test pattern examples
   - Metrics and statistics

5. **INDEX.md** (This File)
   - Guide to documentation

---

## 🎯 Quick Navigation

### For Running Tests
→ See **QUICK_REFERENCE.md** section "Running Tests"

### For Understanding Tests
→ See **TEST_SUMMARY.md** or **TEST_COVERAGE_MAP.md**

### For Test Patterns
→ See **QUICK_REFERENCE.md** section "Common Test Patterns"

### For Troubleshooting
→ See **QUICK_REFERENCE.md** section "Common Issues & Solutions"

---

## 📊 Test Statistics

| Metric | Value |
|--------|-------|
| Test Classes | 4 |
| Test Methods | 30 |
| Lines of Code | 686 |
| File Size | 27,605 bytes |
| Components Tested | 3 |

---

## 🧪 Test Classes

### 1. ThemeProviderTests (4 tests)
- Component rendering and initialization
- Cascading parameter functionality
- Error handling

### 2. ThemeToggleTests (9 tests)
- Theme mode switching (light/dark/system)
- Dropdown menu behavior
- UI icon rendering
- State management

### 3. ColorSchemeSelectorTests (9 tests)
- Color scheme selection (4 colors)
- Dropdown functionality
- Color swatches display
- State management

### 4. ThemeIntegrationTests (8 tests)
- Component interaction
- State sharing between components
- Event handling
- State persistence

---

## 🧠 Key Concepts

### Components Tested
1. **ThemeProvider.razor** - State management and cascading
2. **ThemeToggle.razor** - Theme mode selection UI
3. **ColorSchemeSelector.razor** - Color scheme selection UI

### Testing Patterns
- Component rendering with `Render<T>()`
- JavaScript interop mocking with `JSInterop.Setup<T>()`
- DOM querying with `Find()`, `FindAll()`
- User interactions with `ClickAsync()`
- State assertions with `Should().Be()`

### Coverage Areas
- Component lifecycle
- User interactions
- State management
- JavaScript interop
- UI rendering
- Accessibility
- Error handling
- Component integration

---

## 🚀 Getting Started

### Build
```bash
dotnet build tests/Web.Tests.Bunit/Web.Tests.Bunit.csproj
```

### Run All Tests
```bash
dotnet test tests/Web.Tests.Bunit/Web.Tests.Bunit.csproj
```

### Run Specific Test
```bash
dotnet test tests/Web.Tests.Bunit/Web.Tests.Bunit.csproj --filter "ThemeToggleTests"
```

### Run with Coverage
```bash
dotnet test tests/Web.Tests.Bunit/Web.Tests.Bunit.csproj /p:CollectCoverage=true
```

---

## 📖 Reading Guide

### First Time?
1. Start with **QUICK_REFERENCE.md**
2. Then read **TEST_SUMMARY.md**
3. Examine **ThemeComponentTests.cs**

### Want to Add Tests?
1. Review **QUICK_REFERENCE.md** - "Testing Checklist"
2. Look at examples in **ThemeComponentTests.cs**
3. Follow naming conventions from **QUICK_REFERENCE.md**

### Troubleshooting?
1. Check **QUICK_REFERENCE.md** - "Common Issues & Solutions"
2. Review test examples for similar scenario
3. Check test setup patterns

### Need Details?
1. Check **TEST_COVERAGE_MAP.md** for organization
2. Check **TEST_SUMMARY.md** for descriptions
3. Review source code comments in test file

---

## 💡 Key Testing Patterns

### Setup JavaScript Interop
```csharp
JSInterop.Setup<string>("method").SetResult("value");
JSInterop.SetupVoid("method");
```

### Render Component with Provider
```csharp
var component = Render<ThemeProvider>(parameters =>
    parameters.AddChildContent<ThemeToggle>());
```

### Query DOM
```csharp
var button = component.Find("button");
var items = component.FindAll("button[role='menuitem']");
```

### Simulate User Action
```csharp
await button.ClickAsync(new());
```

### Assert State
```csharp
component.Instance.ThemeMode.Should().Be("dark");
```

---

## ✅ Quality Checklist

- ✅ All 3 components tested
- ✅ 30 comprehensive test methods
- ✅ Theme modes covered (light, dark, system)
- ✅ All color schemes tested (blue, red, green, yellow)
- ✅ Dropdown behavior verified
- ✅ Accessibility attributes validated
- ✅ Event handling tested
- ✅ State persistence verified
- ✅ Error scenarios covered
- ✅ Component integration tested

---

## 📝 Test Naming Convention

**Format**: `[Component]_[Scenario]_[Expected Result]`

**Examples**:
- `ThemeProvider_RendersSuccessfully`
- `ThemeToggle_DropdownOpens_OnButtonClick`
- `ColorSchemeSelector_HighlightsCurrentScheme`
- `Theme_StateIsShared_BetweenComponents`

---

## 🔗 Related Files

- **Source Components**: `src/Web/Components/Theme/`
- **Test Base Class**: `tests/Web.Tests.Bunit/BunitTestBase.cs`
- **Global Usings**: `tests/Web.Tests.Bunit/GlobalUsings.cs`

---

## 📞 Need Help?

1. **Understanding Tests**: See **TEST_SUMMARY.md**
2. **Running Tests**: See **QUICK_REFERENCE.md** → "Running Tests"
3. **Adding Tests**: See **QUICK_REFERENCE.md** → "Testing Checklist"
4. **Troubleshooting**: See **QUICK_REFERENCE.md** → "Common Issues"
5. **Patterns**: See **QUICK_REFERENCE.md** → "Common Test Patterns"

---

## ✨ Summary

This test suite provides **comprehensive coverage** of the Theme components with:
- ✅ Clear organization
- ✅ Well-documented tests
- ✅ Reusable patterns
- ✅ Complete accessibility testing
- ✅ Error scenario coverage
- ✅ Integration testing
- ✅ Quick reference guides

**Status**: Ready for use and integration into CI/CD pipeline

---

**Created**: 2025-03-13
**Framework**: bUnit, Xunit, FluentAssertions
**Platform**: .NET 10.0+
**Location**: `tests/Web.Tests.Bunit/Theme/`
