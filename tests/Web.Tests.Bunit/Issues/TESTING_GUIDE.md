# bUnit Component Testing Quick Reference

## Project Structure
```
tests/
  Web.Tests.Bunit/
    BunitTestBase.cs           # Base class with mocked services and factories
    GlobalUsings.cs            # Global using statements
    Issues/
      IssueComponentTests.cs    # Issue component tests (75+ test cases)
      TEST_SUMMARY.md          # Comprehensive test documentation
```

## Common Test Patterns

### 1. Rendering a Component
```csharp
var cut = RenderComponent<AttachmentCard>(parameters => parameters
    .Add(p => p.Attachment, attachment)
    .Add(p => p.CanDelete, false)
);
```

### 2. Finding Elements
```csharp
// Find single element
var checkbox = cut.Find("input[type='checkbox']");

// Find all matching elements
var buttons = cut.FindAll("button");

// Find first matching
var deleteBtn = cut.FindAll("button").FirstOrDefault(b => b.TextContent.Contains("Delete"));

// Find component
var card = cut.FindComponent<AttachmentCard>();
```

### 3. Querying Element Properties
```csharp
cut.Find("img").GetAttribute("src").Should().Be("url");
cut.Find("img").GetAttribute("alt").Should().Be("text");
```

### 4. Assertions with FluentAssertions
```csharp
cut.Markup.Should().Contain("Expected text");
cut.Markup.Should().NotContain("Unexpected text");
buttons.Should().HaveCount(3);
deleteBtn.Should().NotBeNull();
checkedAttr.Should().BeNullOrEmpty();
```

### 5. Creating Test Data
```csharp
// Use BunitTestBase factories
var user = CreateTestUser(id: "user-1", name: "John Doe");
var issue = CreateTestIssue(title: "Test Issue");
var comment = CreateTestComment(title: "Test Comment");
var category = CreateTestCategory(name: "Bug");
var status = CreateTestStatus(name: "Open");
```

### 6. Mocking Service Returns
```csharp
CommentService.GetCommentsAsync(issueId)
    .Returns(Task.FromResult(Result.SuccessOf<IEnumerable<CommentDto>>(comments)));

CommentService.GetCommentsAsync(issueId)
    .Returns(Task.FromResult(Result.Failure<IEnumerable<CommentDto>>("Error message")));
```

### 7. Testing Loading States
```csharp
var cut = RenderComponent<CommentsSection>(parameters => parameters
    .Add(p => p.IssueId, issueId)
);

// Component loads asynchronously
cut.Markup.Should().Contain("animate-spin");
```

### 8. Testing Visibility Toggle
```csharp
// When not visible
var cut1 = RenderComponent<BulkProgressIndicator>(parameters => parameters
    .Add(p => p.IsVisible, false)
);
cut1.Markup.Should().NotContain("Processing");

// When visible
var cut2 = RenderComponent<BulkProgressIndicator>(parameters => parameters
    .Add(p => p.IsVisible, true)
);
cut2.Markup.Should().Contain("Processing");
```

### 9. Testing Conditional Rendering
```csharp
// Test with condition true
var cut = RenderComponent<AttachmentCard>(parameters => parameters
    .Add(p => p.CanDelete, true)
);
var deleteButton = cut.FindAll("button")
    .FirstOrDefault(b => b.TextContent.Contains("Delete"));
deleteButton.Should().NotBeNull();

// Test with condition false
var cut2 = RenderComponent<AttachmentCard>(parameters => parameters
    .Add(p => p.CanDelete, false)
);
var deleteButton2 = cut2.FindAll("button")
    .FirstOrDefault(b => b.TextContent.Contains("Delete"));
deleteButton2.Should().BeNull();
```

### 10. Testing Parameter Changes
```csharp
var cut = RenderComponent<IssueMultiSelect>(parameters => parameters
    .Add(p => p.ShowSelectAll, true)
    .Add(p => p.AllIssueIds, issueIds)
);

var checkbox = cut.Find("input[type='checkbox']");
checkbox.GetAttribute("id").Should().Be("select-all-checkbox");
```

## Test Naming Convention
```
{ComponentName}_{Scenario}_{ExpectedBehavior}()

Examples:
- AttachmentCard_WithImageAttachment_DisplaysImage()
- AttachmentList_WithEmptyList_ShowsEmptyState()
- CommentsSection_WithNoComments_ShowsEmptyState()
- BulkActionToolbar_WithoutSelection_IsHidden()
- BulkConfirmationModal_DeleteAction_ShowsDeleteIcon()
```

## Arrange-Act-Assert Pattern
```csharp
[Fact]
public void ComponentName_Scenario_ExpectedBehavior()
{
    // Arrange - Set up test data and component state
    var testData = CreateTestData();
    
    // Act - Perform the action being tested
    var cut = RenderComponent<MyComponent>(parameters => parameters
        .Add(p => p.Data, testData)
    );
    
    // Assert - Verify the expected result
    cut.Markup.Should().Contain("expected text");
}
```

## Running Tests

### Run all Issue component tests
```bash
dotnet test tests/Web.Tests.Bunit/Web.Tests.Bunit.csproj \
    --filter "FullyQualifiedName~Issues"
```

### Run specific test class
```bash
dotnet test tests/Web.Tests.Bunit/Web.Tests.Bunit.csproj \
    --filter "AttachmentCardTests"
```

### Run single test
```bash
dotnet test tests/Web.Tests.Bunit/Web.Tests.Bunit.csproj \
    --filter "AttachmentCard_WithImageAttachment_DisplaysImage"
```

### Run with verbose output
```bash
dotnet test tests/Web.Tests.Bunit/Web.Tests.Bunit.csproj \
    --verbosity normal
```

## CSS Selectors for Element Querying

```csharp
// ID selector
cut.Find("#select-all-checkbox");

// Class selector
cut.Find(".animate-spin");

// Attribute selector
cut.Find("input[type='checkbox']");
cut.Find("button[disabled='disabled']");

// Multiple classes
cut.FindAll("div[class*='ring']");

// Text content (use TextContent property)
var element = cut.FindAll("button")
    .FirstOrDefault(b => b.TextContent.Contains("Delete"));

// Pseudo-selectors are not supported, use FindAll instead
```

## Common Assertions

```csharp
// String assertions
cut.Markup.Should().Contain("text");
cut.Markup.Should().NotContain("text");
cut.Markup.Should().BeEmpty();

// Element assertions
element.Should().NotBeNull();
element.Should().BeNull();

// Collection assertions
buttons.Should().HaveCount(3);
buttons.Should().NotBeEmpty();
buttons.Should().AllSatisfy(b => b.GetAttribute("disabled") == "disabled");

// Attribute assertions
cut.Find("img").GetAttribute("src").Should().Be("url");
cut.Find("img").GetAttribute("alt").Should().Be("text");
cut.Find("input").GetAttribute("checked").Should().BeNullOrEmpty();

// Pluralization
itemCount.Should().Be(5);
```

## Important Considerations

1. **No Real Service Calls** - All services are mocked with NSubstitute
2. **No Database Access** - Use test data created by factories
3. **No JavaScript** - JS interop is mocked
4. **Synchronous Tests** - Most tests are synchronous, use [Fact] not [Theory]
5. **Component Isolation** - Test one component at a time
6. **Clear Naming** - Test names should describe exactly what is being tested

## Troubleshooting

### Element Not Found
- Use `cut.Markup` to inspect actual HTML
- Check CSS selectors are correct
- Verify element is conditionally rendered (check conditions)
- Use `FindAll()` instead of `Find()` if element might not exist

### Test Fails Unexpectedly
- Check mock setup is correct
- Verify parameters passed to RenderComponent
- Check for asynchronous operations (some need await)
- Ensure test data is valid

### Build Errors
- Check GlobalUsings.cs has necessary namespaces
- Ensure all DTOs and components are imported
- Verify namespaces match actual locations
