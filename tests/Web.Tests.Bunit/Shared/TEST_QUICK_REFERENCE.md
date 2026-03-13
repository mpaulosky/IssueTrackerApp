// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     TEST_QUICK_REFERENCE.md
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTrackerApp
// Project Name :  Web.Tests.Bunit
// =======================================================

# Shared Component Tests - Quick Reference Guide

## Test File Location
`tests/Web.Tests.Bunit/Shared/SharedComponentTests.cs`

## Components & Test Counts

| Component | Tests | File |
|-----------|-------|------|
| Pagination | 7 | Line 21 |
| FilterPanel | 7 | Line 70 |
| StatusBadge | 7 | Line 125 |
| CategoryBadge | 7 | Line 200 |
| SearchInput | 7 | Line 275 |
| SummaryCard | 9 | Line 350 |
| ToastContainer | 7 | Line 459 |
| SignalRConnection | 4 | Line 516 |
| FileUpload | 6 | Line 556 |
| DeleteConfirmationModal | 11 | Line 612 |
| DateRangePicker | 9 | Line 749 |
| **TOTAL** | **81** | - |

## Common Test Template

```csharp
[Fact]
public void ComponentName_Scenario_ExpectedResult()
{
    // Arrange
    var component = CreateTestComponent(); // or use helper like CreateTestStatus()
    
    // Act
    var cut = RenderComponent<ComponentName>(parameters => parameters
        .Add(p => p.Property, value)
        .Add(p => p.EventCallback, EventCallback.Factory.Create<TValue>(this, _ => { })));
    
    // Assert
    cut.Markup.Should().Contain("expected text");
    cut.Find("selector").GetAttribute("class").Should().Contain("expected-class");
}
```

## Testing Patterns

### 1. Parameter Binding
```csharp
.Add(p => p.CurrentPage, 1)
.Add(p => p.TotalPages, 5)
```

### 2. Event Callbacks
```csharp
.Add(p => p.OnPageChange, EventCallback.Factory.Create<int>(this, page => 
{
    pageChanged = true;
    newPage = page;
    return Task.CompletedTask;
}))
```

### 3. Clicking Elements
```csharp
await cut.Find("button").Click();
// or
await cut.FindAll("button").FirstOrDefault(b => b.TextContent.Contains("Next"))?.Click()!;
```

### 4. Checking Rendered Output
```csharp
cut.Markup.Should().Contain("text");
cut.Markup.Should().NotContain("hidden text");
cut.Find("nav").Should().NotBeNull();
```

### 5. Checking CSS Classes
```csharp
cut.Find("span").GetAttribute("class").Should().Contain("bg-blue-100");
```

### 6. Checking Attributes
```csharp
cut.Find("input").GetAttribute("id").Should().Be("test-search");
cut.Find("input").GetAttribute("placeholder").Should().Be("Search...");
```

## BunitTestBase Helper Methods

```csharp
// Create test data
CreateTestStatus(name: "Open")
CreateTestCategory(name: "Bug")
CreateTestUser(name: "Test User")
CreateTestIssue(title: "Test Issue")
CreateTestComment()

// Setup authentication
SetupAuthenticatedUser(userId, userName, email, isAdmin)
SetupAnonymousUser()
```

## Available Mocked Services

From BunitTestBase (inherited):

```csharp
protected IMediator Mediator { get; }
protected IIssueService IssueService { get; }
protected ICommentService CommentService { get; }
protected IAnalyticsService AnalyticsService { get; }
protected IAttachmentService AttachmentService { get; }
protected IBulkOperationService BulkOperationService { get; }
protected INotificationService NotificationService { get; }
protected IJSRuntime JsRuntime { get; }
```

## Commonly Used Assertions

### Text Content
```csharp
cut.Markup.Should().Contain("text");
cut.Find("selector").TextContent.Should().Be("expected");
```

### Visibility
```csharp
cut.Markup.Should().NotContain("<nav");
cut.FindAll("button").Should().BeEmpty();
```

### Element Existence
```csharp
cut.Find("nav").Should().NotBeNull();
cut.FindComponent<SearchInput>().Should().NotBeNull();
```

### Attributes
```csharp
cut.Find("input").GetAttribute("class").Should().Contain("primary");
cut.Find("button").GetAttribute("disabled").Should().Be("disabled");
```

## Running Tests

```bash
# All tests in file
dotnet test tests/Web.Tests.Bunit/Web.Tests.Bunit.csproj

# Specific test class
dotnet test --filter "FullyQualifiedName~PaginationTests"

# Specific test method
dotnet test --filter "FullyQualifiedName~PaginationTests.Pagination_WithSinglePage_DoesNotRender"

# With coverage
dotnet test /p:CollectCoverage=true /p:CoverletOutput=coverage

# Verbose output
dotnet test --verbosity=detailed
```

## Component-Specific Notes

### Pagination
- Tests navigation button visibility based on page position
- Verifies page change callbacks
- Checks primary color styling on current page

### FilterPanel
- Tests SearchInput component integration
- Verifies filter counts with multiple active filters
- Tests expandable filter options

### Status/CategoryBadge
- Tests color mapping for different values
- Verifies "Unknown" for null values
- Tests additional CSS classes

### SearchInput
- Tests debounce behavior implicitly through callback
- Verifies clear button conditional rendering
- Tests accessibility attributes

### SummaryCard
- Tests trend visualization (up/down arrows)
- Verifies icon rendering with custom backgrounds
- Tests subtitle conditional rendering

### ToastContainer
- Requires ToastService injection
- Tests different toast types and styles
- Verifies toast dismissal buttons

### SignalRConnection
- Tests connection state visualization
- Requires SignalRClientService

### FileUpload
- Tests file type acceptance
- Verifies accessibility labels
- Tests InputFile component usage

### DeleteConfirmationModal
- Tests modal visibility toggling
- Verifies loading spinner during deletion
- Tests button disabling during operation

### DateRangePicker
- Tests preset button selection
- Verifies date range callback
- Tests manual date input

## Tips & Tricks

1. **Use FirstOrDefault with Contains** when finding specific buttons:
   ```csharp
   var nextButton = cut.FindAll("button").FirstOrDefault(b => b.TextContent.Contains("Next"));
   ```

2. **Check TextContent for rendered text**:
   ```csharp
   cut.Find("span").TextContent.Should().Contain("Open");
   ```

3. **Use Find for single element, FindAll for multiple**:
   ```csharp
   cut.Find("nav");           // Throws if not found
   cut.FindAll("button");     // Returns list
   ```

4. **Don't forget Task.CompletedTask in callbacks**:
   ```csharp
   EventCallback.Factory.Create<int>(this, _ => Task.CompletedTask)
   ```

5. **Use @Key in Razor components for duplicate elements**:
   ```csharp
   @key="toast.Id"  // In component markup
   ```

## Debugging Tests

### Enable Console Output
```csharp
System.Diagnostics.Debug.WriteLine(cut.Markup);
```

### Take Snapshot of Component
```csharp
var snapshot = cut.GetChangesSinceFirstRender();
```

### Find Element Details
```csharp
var element = cut.Find("selector");
Console.WriteLine(element.GetAttribute("class"));
Console.WriteLine(element.TextContent);
```

## Test Organization

- Each component has its own test class
- Test methods follow naming: `ComponentName_Scenario_ExpectedResult`
- Tests are grouped logically (rendering, parameters, callbacks, styling)
- Each test class has XML documentation
- Tests are independent and can run in any order
