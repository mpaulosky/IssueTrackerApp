// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     SHARED_COMPONENT_TESTS_SUMMARY.md
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTrackerApp
// Project Name :  Web.Tests.Bunit
// =======================================================

# Shared Component Tests Summary

## Overview
Comprehensive bUnit test suite created for 11 Shared components in IssueTrackerApp.
All tests inherit from `BunitTestBase` and use mocked services provided by the base class.

## Test File Location
`tests/Web.Tests.Bunit/Shared/SharedComponentTests.cs`

---

## Components Tested (11 Total)

### 1. **Pagination Component** - `PaginationTests`
- ✅ Single page: Does not render navigation
- ✅ Multiple pages: Renders navigation correctly
- ✅ First page: Previous button not rendered
- ✅ Last page: Next button not rendered
- ✅ Middle page: Both navigation buttons rendered
- ✅ Click next: Invokes page change callback
- ✅ Current page: Highlighted with primary color class

**Key Tests:** 7 tests
**Focus:** Navigation behavior, conditional rendering, button states, event callbacks

---

### 2. **FilterPanel Component** - `FilterPanelTests`
- ✅ Renders SearchInput component
- ✅ Without filters: Clear button not visible
- ✅ With filters: Displays active filter count
- ✅ Toggle: Shows/hides filter options
- ✅ With statuses: Renders all status options
- ✅ With categories: Renders all category options
- ✅ Displays total results count

**Key Tests:** 7 tests
**Focus:** Component nesting, filter state management, conditional rendering, data binding

---

### 3. **StatusBadge Component** - `StatusBadgeTests`
- ✅ With status: Renders status name
- ✅ Null status: Renders "Unknown"
- ✅ Open status: Blue color class applied
- ✅ In Progress status: Yellow color class applied
- ✅ Resolved status: Green color class applied
- ✅ Closed status: Gray color class applied
- ✅ Additional classes: Included in output

**Key Tests:** 7 tests
**Focus:** Color mapping, CSS class binding, parameter binding, conditional styling

---

### 4. **CategoryBadge Component** - `CategoryBadgeTests`
- ✅ With category: Renders category name
- ✅ Null category: Renders "Unknown"
- ✅ Bug category: Red color class applied
- ✅ Feature category: Green color class applied
- ✅ Enhancement category: Blue color class applied
- ✅ Question category: Purple color class applied
- ✅ Additional classes: Included in output

**Key Tests:** 7 tests
**Focus:** Color mapping, CSS class binding, parameter binding, conditional styling

---

### 5. **SearchInput Component** - `SearchInputTests`
- ✅ Correct ID attribute rendered
- ✅ Placeholder text displayed
- ✅ Value displayed in input
- ✅ With value: Clear button shown
- ✅ Without value: Clear button hidden
- ✅ Clear button: Clears value and invokes callback
- ✅ Aria-label rendered for accessibility

**Key Tests:** 7 tests
**Focus:** Input binding, conditional rendering, event callbacks, accessibility

---

### 6. **SummaryCard Component** - `SummaryCardTests`
- ✅ Renders title
- ✅ Renders value
- ✅ With subtitle: Rendered
- ✅ Without subtitle: Not rendered
- ✅ With icon: SVG rendered
- ✅ Positive trend: Shows up arrow and green color
- ✅ Negative trend: Shows down arrow and red color
- ✅ Zero trend: Shows "No change"
- ✅ Custom icon background: Class applied

**Key Tests:** 9 tests
**Focus:** Conditional rendering, trend visualization, CSS classes, parameter binding

---

### 7. **ToastContainer Component** - `ToastContainerTests`
- ✅ Component renders
- ✅ No toasts: No alert messages
- ✅ Info toast: Displayed with blue styles
- ✅ Success toast: Displayed with green styles
- ✅ Warning toast: Displayed with yellow styles
- ✅ Error toast: Displayed with red styles
- ✅ All toasts: Have dismiss button

**Key Tests:** 7 tests
**Focus:** Service integration, conditional styling, state management, user interactions

---

### 8. **SignalRConnection Component** - `SignalRConnectionTests`
- ✅ Component renders
- ✅ Disconnected state: Shows appropriate status
- ✅ Status indicator: Has title attribute
- ✅ Fixed positioning: Applied correctly

**Key Tests:** 4 tests
**Focus:** Service integration, state rendering, CSS positioning classes

---

### 9. **FileUpload Component** - `FileUploadTests`
- ✅ Upload zone rendered
- ✅ Correct accept types displayed
- ✅ Without error: Error message not shown
- ✅ Label clickable for accessibility
- ✅ InputFile component present
- ✅ Attachments label displayed

**Key Tests:** 6 tests
**Focus:** Rendering, accessibility, file handling, UI structure

---

### 10. **DeleteConfirmationModal Component** - `DeleteConfirmationModalTests`
- ✅ Hidden: Dialog not rendered
- ✅ Visible: Dialog rendered
- ✅ Renders title
- ✅ Renders message
- ✅ With item title: Item title rendered
- ✅ Custom confirm button text displayed
- ✅ Custom cancel button text displayed
- ✅ Confirm button: Invokes callback
- ✅ Cancel button: Invokes callback
- ✅ While deleting: Confirm button disabled
- ✅ While deleting: Loading spinner shown

**Key Tests:** 11 tests
**Focus:** Modal visibility, conditional rendering, button callbacks, loading states

---

### 11. **DateRangePicker Component** - `DateRangePickerTests`
- ✅ Renders preset buttons (7, 30, 90 days, All time)
- ✅ Renders date inputs
- ✅ With start date: Start date rendered
- ✅ With end date: End date rendered
- ✅ Preset button: Updates dates
- ✅ From label displayed
- ✅ To label displayed
- ✅ Manual date change: Invokes callback
- ✅ All time button: Clears preset

**Key Tests:** 9 tests
**Focus:** Date handling, preset buttons, callbacks, parameter binding

---

## Test Statistics

| Component | Test Count | Key Focus |
|-----------|-----------|-----------|
| Pagination | 7 | Navigation, callbacks |
| FilterPanel | 7 | Filtering, state |
| StatusBadge | 7 | Styling, color mapping |
| CategoryBadge | 7 | Styling, color mapping |
| SearchInput | 7 | Input binding, callbacks |
| SummaryCard | 9 | Conditional rendering, trends |
| ToastContainer | 7 | Service integration |
| SignalRConnection | 4 | Service integration |
| FileUpload | 6 | File handling |
| DeleteConfirmationModal | 11 | Modal behavior |
| DateRangePicker | 9 | Date handling |
| **TOTAL** | **81** | **Comprehensive coverage** |

---

## Test Patterns Used

### 1. **Arrange-Act-Assert (AAA)**
All tests follow the AAA pattern:
```csharp
// Arrange - Set up component parameters
var cut = Render<Component>(parameters => ...);

// Act - Perform action (click, input, etc.)
await button.Click();

// Assert - Verify expected behavior
cut.Markup.Should().Contain("expected");
```

### 2. **Parameter Binding**
Tests verify parameter binding by passing parameters and checking rendered output:
```csharp
.Add(p => p.CurrentPage, 1)
.Add(p => p.TotalPages, 5)
```

### 3. **Event Callbacks**
Tests verify callbacks are invoked with correct values:
```csharp
.Add(p => p.OnPageChange, EventCallback.Factory.Create<int>(this, page => { }))
```

### 4. **Conditional Rendering**
Tests verify elements are shown/hidden based on state:
```csharp
cut.Markup.Should().NotContain("Clear All");
```

### 5. **CSS Class Verification**
Tests verify correct styling applied based on state:
```csharp
cut.Markup.Should().Contain("bg-blue-100");
```

### 6. **Helper Methods**
Tests use BunitTestBase helper methods:
- `CreateTestStatus(name)` - Creates test StatusDto
- `CreateTestCategory(name)` - Creates test CategoryDto
- `CreateTestUser()` - Creates test UserDto
- `CreateTestIssue()` - Creates test IssueDto

---

## Key Features

✅ **Complete Coverage** - Tests for all 11 Shared components
✅ **Consistent Patterns** - All tests follow AAA pattern
✅ **FluentAssertions** - Readable assertion syntax
✅ **Event Verification** - Tests callback invocation
✅ **Visual Tests** - Tests CSS classes and styling
✅ **Accessibility** - Tests aria-labels and semantic HTML
✅ **Service Integration** - Tests components that use injected services
✅ **State Management** - Tests parameter binding and state changes
✅ **Error Handling** - Tests error states and edge cases

---

## Running the Tests

### Run all tests
```bash
dotnet test tests/Web.Tests.Bunit/Web.Tests.Bunit.csproj
```

### Run specific test class
```bash
dotnet test tests/Web.Tests.Bunit/Web.Tests.Bunit.csproj --filter "FullyQualifiedName~PaginationTests"
```

### Run with verbose output
```bash
dotnet test tests/Web.Tests.Bunit/Web.Tests.Bunit.csproj --verbosity normal
```

### Run with code coverage
```bash
dotnet test tests/Web.Tests.Bunit/Web.Tests.Bunit.csproj /p:CollectCoverage=true
```

---

## Dependencies

- **bUnit** - Blazor component testing library
- **FluentAssertions** - Assertion library
- **xUnit** - Test framework
- **NSubstitute** - Mocking library
- **BunitTestBase** - Custom base class with mocked services

---

## Notes

- All tests are independent and can run in any order
- Mocked services from BunitTestBase are available to all components
- Tests do not require actual API/database calls
- Tests verify both rendering and behavior
- Tests are maintainable and clearly document expected component behavior
