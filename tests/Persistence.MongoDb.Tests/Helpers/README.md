# Repository Testing Infrastructure

This directory contains helper classes and base test classes for unit testing MongoDB repositories with NSubstitute mocking.

## Files Created

### 1. `Helpers/MockDbSetHelper.cs`
Static helper class for creating mockable `DbSet<T>` instances with support for:
- `ToListAsync()` - Async enumeration
- `FindAsync()` - MongoDB ObjectId lookup
- `AddAsync()`, `AddRangeAsync()` - Adding entities
- `Remove()`, `Update()` - Modifying entities
- Basic LINQ operations

### 2. `RepositoryTestBase.cs`
Abstract base class providing common test infrastructure:
- Pre-configured mocks for `IIssueTrackerDbContext`, `DbSet<T>`, and `ILogger<T>`
- Helper methods for setting up test scenarios
- Verification methods for common assertions

### 3. `RepositoryTestBaseExampleTests.cs`
Example tests demonstrating usage patterns for:
- Testing with empty collections
- Testing with in-memory data
- Testing add operations
- Testing error handling

## Usage Examples

### Basic Test with Empty DbSet
```csharp
public class MyRepositoryTests : RepositoryTestBase<Category>
{
    [Fact]
    public async Task GetAllAsync_WithEmptyDbSet_Should_ReturnEmptyCollection()
    {
        // Arrange
        SetupEmptyDbSet();
        SetupSaveChangesAsync();

        // Act
        var result = await Sut.GetAllAsync();

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }
}
```

### Test with Sample Data
```csharp
[Fact]
public async Task GetAllAsync_WithData_Should_ReturnAllEntities()
{
    // Arrange
    var categories = new List<Category>
    {
        new() { Id = ObjectId.GenerateNewId(), CategoryName = "Bug" },
        new() { Id = ObjectId.GenerateNewId(), CategoryName = "Feature" }
    };

    SetupDbSetWithData(categories);
    SetupSaveChangesAsync();

    // Act
    var result = await Sut.GetAllAsync();

    // Assert
    result.Success.Should().BeTrue();
    result.Value.Should().HaveCount(2);
}
```

### Test FindAsync with ObjectId
```csharp
[Fact]
public async Task GetByIdAsync_WithValidId_Should_ReturnEntity()
{
    // Arrange
    var id = ObjectId.GenerateNewId();
    var category = new Category { Id = id, CategoryName = "Bug" };

    SetupDbSetWithFind(new[] { category }, c => c.Id);
    SetupSaveChangesAsync();

    // Act
    var result = await Sut.GetByIdAsync(id.ToString());

    // Assert
    result.Success.Should().BeTrue();
    result.Value.Should().BeEquivalentTo(category);
}
```

### Test Error Handling
```csharp
[Fact]
public async Task AddAsync_WhenSaveChangesFails_Should_ReturnFail()
{
    // Arrange
    var category = new Category { Id = ObjectId.GenerateNewId() };
    var exception = new InvalidOperationException("Database error");
    
    SetupEmptyDbSet();
    SetupSaveChangesToThrow(exception);

    // Act
    var result = await Sut.AddAsync(category);

    // Assert
    result.Success.Should().BeFalse();
    result.Error.Should().Contain("Failed to add");
    VerifyErrorLogged();
}
```

## RepositoryTestBase Helper Methods

### Setup Methods
- `SetupEmptyDbSet()` - Creates an empty DbSet
- `SetupDbSetWithData(IEnumerable<T> data)` - Creates DbSet with test data
- `SetupDbSetWithFind(data, keySelector)` - Creates DbSet with FindAsync support
- `SetupDbSetToThrow(Exception ex)` - Makes DbSet throw exception
- `SetupSaveChangesAsync(int affectedRows = 1)` - Configures successful save
- `SetupSaveChangesToThrow(Exception ex)` - Makes save throw exception

### Verification Methods
- `VerifySaveChangesCalledOnce()` - Verifies SaveChangesAsync was called once
- `VerifySaveChangesNotCalled()` - Verifies SaveChangesAsync was not called
- `VerifyErrorLogged()` - Verifies error was logged
- `VerifyInformationLogged()` - Verifies information was logged

### Properties
- `MockContext` - Mock database context
- `MockDbSet` - Mock DbSet instance
- `MockLogger` - Mock logger
- `Sut` - System Under Test (Repository instance)

## Important Notes

### Limitations
The mock infrastructure is designed for **unit testing basic CRUD operations**. It does NOT fully support:
- Complex async LINQ queries with predicates (CountAsync, AnyAsync, FirstOrDefaultAsync with Where)
- EF Core's async extension methods that require a real database provider

### When to Use Integration Tests
For testing complex queries with predicates, use **integration tests** with:
- **TestContainers** - Provides a real MongoDB instance in Docker
- **Real MongoDB instance** - See integration tests that use TestContainers in this project

### Example: When Unit Tests Are Sufficient
✅ `GetByIdAsync(id)` - Direct lookup by ID  
✅ `AddAsync(entity)` - Adding entities  
✅ `UpdateAsync(entity)` - Updating entities  
✅ `DeleteAsync(id)` - Deleting by ID  
✅ `GetAllAsync()` - Retrieving all entities  

### Example: When Integration Tests Are Needed
❌ `CountAsync(x => x.Name == "Test")` - Predicate-based count  
❌ `AnyAsync(x => x.IsActive)` - Predicate-based existence check  
❌ `FirstOrDefaultAsync(x => x.Category == "Bug")` - Predicate-based query  
❌ Complex LINQ expressions with joins, grouping, etc.

## Next Steps

1. Update existing repository tests to inherit from `RepositoryTestBase<T>`
2. Remove duplicate mock setup code
3. Use the helper methods for consistent test structure
4. Keep integration tests for complex query scenarios

## References

- NSubstitute Documentation: https://nsubstitute.github.io/
- EF Core Testing: https://learn.microsoft.com/en-us/ef/core/testing/
- TestContainers: https://dotnet.testcontainers.org/
