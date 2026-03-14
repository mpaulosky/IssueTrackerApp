# How to Add More Tests for Shared Components

## Overview
This guide shows how to add additional tests to the SharedComponentTests.cs file following the established patterns.

---

## Adding Tests to an Existing Component

### Step 1: Locate the Test Class
Find the appropriate test class in SharedComponentTests.cs, e.g., `PaginationTests`.

### Step 2: Add New Test Method
Follow the naming convention: `ComponentName_Scenario_ExpectedResult`

```csharp
[Fact]
public void Pagination_WithLargePageCount_ShowsEllipsis()
{
    // Arrange
    var cut = Render<Pagination>(parameters => parameters
        .Add(p => p.CurrentPage, 10)
        .Add(p => p.TotalPages, 50)
        .Add(p => p.TotalItems, 500)
        .Add(p => p.OnPageChange, EventCallback.Factory.Create<int>(this, _ => { })));

    // Act
    // No specific action needed for rendering test

    // Assert
    cut.Markup.Should().Contain("...");
}
```

### Step 3: Common Test Patterns

#### Testing Async Actions
```csharp
[Fact]
public async Task Component_AsyncAction_InvokesCallback()
{
    var executed = false;
    var cut = Render<Component>(p => p
        .Add(x => x.OnAsync, EventCallback.Factory.Create(this, async () =>
        {
            executed = true;
            await Task.Delay(10);
        })));

    await cut.Find("button").Click();
    executed.Should().BeTrue();
}
```

#### Testing with Multiple Clicks
```csharp
[Fact]
public async Task Component_MultipleClicks_UpdatesCounter()
{
    var count = 0;
    var cut = Render<Component>(p => p
        .Add(x => x.OnClick, EventCallback.Factory.Create(this, () =>
        {
            count++;
            return Task.CompletedTask;
        })));

    var button = cut.Find("button");
    await button.Click();
    await button.Click();
    
    count.Should().Be(2);
}
```

#### Testing Element Collections
```csharp
[Fact]
public void Component_WithMultipleItems_RendersAll()
{
    var items = new List<string> { "Item1", "Item2", "Item3" };
    
    var cut = Render<Component>(p => p
        .Add(x => x.Items, items));

    var listItems = cut.FindAll("li");
    listItems.Should().HaveCount(3);
}
```

#### Testing Component Parameter Updates
```csharp
[Fact]
public async Task Component_ParameterUpdate_ReRendersCorrectly()
{
    var cut = Render<Component>(p => p
        .Add(x => x.Value, "Initial"));

    cut.Markup.Should().Contain("Initial");

    // Update parameter
    await cut.SetParametersAsync(p => p
        .Add(x => x.Value, "Updated"));

    cut.Markup.Should().Contain("Updated");
}
```

---

## Adding Tests for a New Component

### Step 1: Create Test Class
Add a new test class at the end of SharedComponentTests.cs:

```csharp
/// <summary>
///   Tests for the NewComponent component.
/// </summary>
public class NewComponentTests : BunitTestBase
{
    [Fact]
    public void NewComponent_Renders()
    {
        // Arrange & Act
        var cut = Render<NewComponent>();

        // Assert
        cut.Should().NotBeNull();
    }
}
```

### Step 2: Add Core Tests
Start with basic rendering tests, then add parameter and callback tests:

```csharp
[Fact]
public void NewComponent_RendersTitle()
{
    var cut = Render<NewComponent>(p => p
        .Add(x => x.Title, "Test Title"));

    cut.Markup.Should().Contain("Test Title");
}

[Fact]
public void NewComponent_WithDescription_Rendered()
{
    var cut = Render<NewComponent>(p => p
        .Add(x => x.Description, "Test Description"));

    cut.Markup.Should().Contain("Test Description");
}

[Fact]
public async Task NewComponent_ClickButton_InvokesCallback()
{
    var clicked = false;
    
    var cut = Render<NewComponent>(p => p
        .Add(x => x.OnClick, EventCallback.Factory.Create(this, () =>
        {
            clicked = true;
            return Task.CompletedTask;
        })));

    await cut.Find("button").Click();
    clicked.Should().BeTrue();
}
```

### Step 3: Add Edge Case Tests
Test null values, empty collections, boundary conditions:

```csharp
[Fact]
public void NewComponent_WithNullDescription_RendersDefault()
{
    var cut = Render<NewComponent>(p => p
        .Add(x => x.Description, null));

    cut.Markup.Should().Contain("No description provided");
}

[Fact]
public void NewComponent_WithEmptyList_ShowsMessage()
{
    var cut = Render<NewComponent>(p => p
        .Add(x => x.Items, new List<Item>()));

    cut.Markup.Should().Contain("No items found");
}
```

---

## Using Test Data Helpers

### From BunitTestBase
```csharp
// Create test data
var issue = CreateTestIssue(title: "Custom Title");
var category = CreateTestCategory(name: "Bug");
var status = CreateTestStatus(name: "Open");
var user = CreateTestUser(name: "Test User");
var comment = CreateTestComment(description: "Test comment");
var issues = CreateTestIssues(count: 10);

// Use in tests
var cut = Render<Component>(p => p
    .Add(x => x.Issue, issue)
    .Add(x => x.Category, category));
```

### Create Custom Helpers if Needed
```csharp
private static List<StatusDto> CreateTestStatuses()
{
    return new List<StatusDto>
    {
        CreateTestStatus(name: "Open"),
        CreateTestStatus(name: "In Progress"),
        CreateTestStatus(name: "Closed")
    };
}

// Use in test
var statuses = CreateTestStatuses();
var cut = Render<FilterPanel>(p => p
    .Add(x => x.Statuses, statuses));
```

---

## Advanced Testing Scenarios

### Testing JavaScript Interop
```csharp
[Fact]
public async Task Component_WithJSInterop_CallsJS()
{
    var jsRuntime = Services.GetRequiredService<IJSRuntime>();
    jsRuntime
        .InvokeAsync<bool>("functionName", Arg.Any<string>())
        .Returns(Task.FromResult(true));

    var cut = Render<Component>();
    // Component should call JS function
    
    await jsRuntime.Received().InvokeAsync<bool>(
        "functionName", 
        Arg.Is<string>(s => s == "expected"));
}
```

### Testing with Service Calls
```csharp
[Fact]
public async Task Component_LoadsData_DisplaysResults()
{
    // Setup mock service
    var mockService = Services.GetRequiredService<IssueService>();
    var testIssues = CreateTestIssues(count: 5);
    
    mockService
        .GetIssuesAsync()
        .Returns(Task.FromResult<IEnumerable<IssueDto>>(testIssues));

    // Render and verify
    var cut = Render<Component>();
    
    cut.Markup.Should().Contain("5 issues");
}
```

### Testing Authorization
```csharp
[Fact]
public void Component_WithoutAuth_HidesContent()
{
    SetupAnonymousUser();
    
    var cut = Render<Component>();
    
    cut.Markup.Should().NotContain("protected-content");
}

[Fact]
public void Component_WithAdminAuth_ShowsAdminOptions()
{
    SetupAuthenticatedUser(isAdmin: true);
    
    var cut = Render<Component>();
    
    cut.Markup.Should().Contain("admin-button");
}
```

---

## Best Practices for New Tests

### ✅ Do
- Use descriptive test names
- Follow AAA pattern
- Test one thing per test
- Use meaningful assertions
- Add XML documentation
- Keep tests independent
- Use FluentAssertions

### ❌ Don't
- Test multiple scenarios in one test
- Create test dependencies
- Use magic numbers/strings
- Ignore edge cases
- Write overly complex assertions
- Skip documentation
- Mix concerns

---

## Test Template for Copy-Paste

```csharp
/// <summary>
///   Tests for the [ComponentName] component.
/// </summary>
public class [ComponentName]Tests : BunitTestBase
{
    [Fact]
    public void [ComponentName]_Renders()
    {
        // Arrange & Act
        var cut = Render<[ComponentName]>();

        // Assert
        cut.Should().NotBeNull();
    }

    [Fact]
    public void [ComponentName]_With[Parameter]_Rendered()
    {
        // Arrange & Act
        var cut = Render<[ComponentName]>(parameters => parameters
            .Add(p => p.[Parameter], "[value]"));

        // Assert
        cut.Markup.Should().Contain("[value]");
    }

    [Fact]
    public async Task [ComponentName]_[Action]_[Result]()
    {
        // Arrange
        var result = false;
        var cut = Render<[ComponentName]>(parameters => parameters
            .Add(p => p.OnEvent, EventCallback.Factory.Create(this, () =>
            {
                result = true;
                return Task.CompletedTask;
            })));

        // Act
        await cut.Find("[selector]").Click();

        // Assert
        result.Should().BeTrue();
    }
}
```

---

## Debugging Tests

### Print Markup
```csharp
System.Diagnostics.Debug.WriteLine(cut.Markup);
```

### Print Element Info
```csharp
var element = cut.Find("selector");
System.Diagnostics.Debug.WriteLine($"Class: {element.GetAttribute("class")}");
System.Diagnostics.Debug.WriteLine($"Content: {element.TextContent}");
```

### Breakpoints
- Set breakpoints in test method
- Use Debug mode to inspect state
- Check variable values

### Inspect Rendered HTML
```csharp
// Save markup to file for inspection
File.WriteAllText("output.html", cut.Markup);
```

---

## Common Assertions Cheat Sheet

```csharp
// Text content
cut.Markup.Should().Contain("text");
cut.Find("span").TextContent.Should().Be("exact");
cut.Markup.Should().NotContain("text");

// Element existence
cut.Find("nav").Should().NotBeNull();
cut.FindAll("button").Should().NotBeEmpty();
cut.FindAll("li").Should().HaveCount(3);

// Attributes
cut.Find("input").GetAttribute("id").Should().Be("search");
cut.Find("button").GetAttribute("disabled").Should().Be("disabled");
cut.Find("div").GetAttribute("class").Should().Contain("hidden");

// Collections
cut.FindAll("button").Should().HaveCount(2);
cut.FindAll("li").Should().NotBeEmpty();
cut.FindAll("div").Should().BeEmpty();
```

---

## File Organization Tips

1. **Group related tests** - Keep tests for same component together
2. **Use consistent naming** - Makes tests easy to find
3. **Add documentation** - XML docs for each test class
4. **Keep file manageable** - Consider splitting if > 2000 lines
5. **Order tests logically** - Rendering first, then parameters, then callbacks

---

## Example: Adding Tests for a New Button Component

```csharp
/// <summary>
///   Tests for the CustomButton component.
/// </summary>
public class CustomButtonTests : BunitTestBase
{
    [Fact]
    public void CustomButton_RendersWithText()
    {
        var cut = Render<CustomButton>(p => p
            .Add(x => x.Text, "Click Me"));

        cut.Markup.Should().Contain("Click Me");
    }

    [Fact]
    public void CustomButton_DisabledState_ButtonDisabled()
    {
        var cut = Render<CustomButton>(p => p
            .Add(x => x.Disabled, true));

        cut.Find("button").GetAttribute("disabled").Should().Be("disabled");
    }

    [Fact]
    public async Task CustomButton_Click_InvokesCallback()
    {
        var clicked = false;
        var cut = Render<CustomButton>(p => p
            .Add(x => x.OnClick, EventCallback.Factory.Create(this, () =>
            {
                clicked = true;
                return Task.CompletedTask;
            })));

        await cut.Find("button").Click();
        clicked.Should().BeTrue();
    }

    [Fact]
    public void CustomButton_WithVariant_AppliesToClass()
    {
        var cut = Render<CustomButton>(p => p
            .Add(x => x.Variant, "primary"));

        cut.Find("button").GetAttribute("class").Should().Contain("primary");
    }

    [Fact]
    public void CustomButton_WithLoading_ShowsSpinner()
    {
        var cut = Render<CustomButton>(p => p
            .Add(x => x.Loading, true));

        cut.Markup.Should().Contain("spinner");
    }
}
```

---

## Running Your New Tests

```bash
# Run all tests
dotnet test tests\Web.Tests.Bunit\Web.Tests.Bunit.csproj

# Run specific test class
dotnet test --filter "FullyQualifiedName~CustomButtonTests"

# Run specific test method
dotnet test --filter "FullyQualifiedName~CustomButtonTests.CustomButton_RendersWithText"

# Verbose output
dotnet test --verbosity detailed
```

---

## Summary

To add tests for Shared components:

1. ✅ Follow the AAA pattern
2. ✅ Use descriptive names
3. ✅ Use FluentAssertions
4. ✅ Test rendering, parameters, callbacks, styling
5. ✅ Use BunitTestBase helpers
6. ✅ Keep tests independent
7. ✅ Add documentation

Happy testing! 🎉
