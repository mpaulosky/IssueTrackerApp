// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     CategoriesPageTests.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTrackerApp
// Project Name :  Web.Tests.Bunit
// =======================================================

namespace Web.Tests.Bunit.Pages.Admin;

/// <summary>
///   Comprehensive bUnit tests for the Categories admin page component.
///   Tests category listing, creation, editing, archiving, validation, and error handling.
/// </summary>
public class CategoriesPageTests : BunitTestBase
{
	#region Loading State Tests

	[Fact]
	public void Categories_InitialRender_DisplaysLoadingSpinner()
	{
		// Arrange
		SetupAuthenticatedUser(isAdmin: true);
		var tcs = new TaskCompletionSource<Result<IEnumerable<CategoryDto>>>();
		CategoryService.GetCategoriesAsync(Arg.Any<bool>(), Arg.Any<CancellationToken>())
			.Returns(tcs.Task);

		// Act
		var cut = Render<Categories>();

		// Assert
		cut.Markup.Should().Contain("animate-spin", "Loading spinner should be visible during initial load");
	}

	[Fact]
	public async Task Categories_AfterLoading_HidesLoadingSpinner()
	{
		// Arrange
		SetupAuthenticatedUser(isAdmin: true);
		var categories = new[] { CreateTestCategory(name: "Bug") };
		CategoryService.GetCategoriesAsync(Arg.Any<bool>(), Arg.Any<CancellationToken>())
			.Returns(Task.FromResult(Result.Ok<IEnumerable<CategoryDto>>(categories)));

		// Act
		var cut = Render<Categories>();
		await cut.InvokeAsync(() => Task.Delay(50));

		// Assert
		cut.Markup.Should().NotContain("animate-spin", "Loading spinner should be hidden after loading completes");
	}

	#endregion

	#region Category List Display Tests

	[Fact]
	public async Task Categories_WithCategories_DisplaysCategoryTable()
	{
		// Arrange
		SetupAuthenticatedUser(isAdmin: true);
		var categories = new[]
		{
			CreateTestCategory(name: "Bug"),
			CreateTestCategory(name: "Feature"),
			CreateTestCategory(name: "Enhancement")
		};
		CategoryService.GetCategoriesAsync(Arg.Any<bool>(), Arg.Any<CancellationToken>())
			.Returns(Task.FromResult(Result.Ok<IEnumerable<CategoryDto>>(categories)));

		// Act
		var cut = Render<Categories>();
		await cut.InvokeAsync(() => Task.Delay(50));

		// Assert
		var table = cut.Find("table");
		table.Should().NotBeNull("Categories page should display a table");
	}

	[Fact]
	public async Task Categories_WithCategories_DisplaysAllCategoryNames()
	{
		// Arrange
		SetupAuthenticatedUser(isAdmin: true);
		var categories = new[]
		{
			CreateTestCategory(name: "Bug"),
			CreateTestCategory(name: "Feature Request"),
			CreateTestCategory(name: "Enhancement")
		};
		CategoryService.GetCategoriesAsync(Arg.Any<bool>(), Arg.Any<CancellationToken>())
			.Returns(Task.FromResult(Result.Ok<IEnumerable<CategoryDto>>(categories)));

		// Act
		var cut = Render<Categories>();
		await cut.InvokeAsync(() => Task.Delay(50));

		// Assert
		cut.Markup.Should().Contain("Bug");
		cut.Markup.Should().Contain("Feature Request");
		cut.Markup.Should().Contain("Enhancement");
	}

	[Fact]
	public async Task Categories_WithCategories_DisplaysCorrectRowCount()
	{
		// Arrange
		SetupAuthenticatedUser(isAdmin: true);
		var categories = new[]
		{
			CreateTestCategory(name: "Bug"),
			CreateTestCategory(name: "Feature"),
			CreateTestCategory(name: "Enhancement")
		};
		CategoryService.GetCategoriesAsync(Arg.Any<bool>(), Arg.Any<CancellationToken>())
			.Returns(Task.FromResult(Result.Ok<IEnumerable<CategoryDto>>(categories)));

		// Act
		var cut = Render<Categories>();
		await cut.InvokeAsync(() => Task.Delay(50));

		// Assert
		var rows = cut.FindAll("tbody tr");
		rows.Should().HaveCount(3, "Table should display all 3 categories");
	}

	[Fact]
	public async Task Categories_WithActiveCategory_DisplaysActiveStatus()
	{
		// Arrange
		SetupAuthenticatedUser(isAdmin: true);
		var category = CreateTestCategory(name: "Bug");
		CategoryService.GetCategoriesAsync(Arg.Any<bool>(), Arg.Any<CancellationToken>())
			.Returns(Task.FromResult(Result.Ok<IEnumerable<CategoryDto>>(new[] { category })));

		// Act
		var cut = Render<Categories>();
		await cut.InvokeAsync(() => Task.Delay(50));

		// Assert
		cut.Markup.Should().Contain("Active", "Active category should display 'Active' status badge");
	}

	[Fact]
	public async Task Categories_WithArchivedCategory_DisplaysArchivedStatus()
	{
		// Arrange
		SetupAuthenticatedUser(isAdmin: true);
		var archivedCategory = CreateTestCategory(name: "Old Category") with { Archived = true };
		CategoryService.GetCategoriesAsync(Arg.Any<bool>(), Arg.Any<CancellationToken>())
			.Returns(Task.FromResult(Result.Ok<IEnumerable<CategoryDto>>(new[] { archivedCategory })));

		// Act
		var cut = Render<Categories>();
		await cut.InvokeAsync(() => Task.Delay(50));

		// Assert
		cut.Markup.Should().Contain("Archived", "Archived category should display 'Archived' status badge");
	}

	[Fact]
	public async Task Categories_WithCategories_DisplaysEditButtons()
	{
		// Arrange
		SetupAuthenticatedUser(isAdmin: true);
		var category = CreateTestCategory(name: "Bug");
		CategoryService.GetCategoriesAsync(Arg.Any<bool>(), Arg.Any<CancellationToken>())
			.Returns(Task.FromResult(Result.Ok<IEnumerable<CategoryDto>>(new[] { category })));

		// Act
		var cut = Render<Categories>();
		await cut.InvokeAsync(() => Task.Delay(50));

		// Assert
		var editButtons = cut.FindAll("button").Where(b => b.TextContent.Contains("Edit"));
		editButtons.Should().NotBeEmpty("Each category row should have an Edit button");
	}

	[Fact]
	public async Task Categories_WithActiveCategory_DisplaysArchiveButton()
	{
		// Arrange
		SetupAuthenticatedUser(isAdmin: true);
		var category = CreateTestCategory(name: "Bug");
		CategoryService.GetCategoriesAsync(Arg.Any<bool>(), Arg.Any<CancellationToken>())
			.Returns(Task.FromResult(Result.Ok<IEnumerable<CategoryDto>>(new[] { category })));

		// Act
		var cut = Render<Categories>();
		await cut.InvokeAsync(() => Task.Delay(50));

		// Assert
		var archiveButtons = cut.FindAll("button").Where(b => b.TextContent.Contains("Archive"));
		archiveButtons.Should().NotBeEmpty("Active category should have an Archive button");
	}

	[Fact]
	public async Task Categories_WithArchivedCategory_DisplaysRestoreButton()
	{
		// Arrange
		SetupAuthenticatedUser(isAdmin: true);
		var archivedCategory = CreateTestCategory(name: "Old Category") with { Archived = true };
		CategoryService.GetCategoriesAsync(Arg.Any<bool>(), Arg.Any<CancellationToken>())
			.Returns(Task.FromResult(Result.Ok<IEnumerable<CategoryDto>>(new[] { archivedCategory })));

		// Act
		var cut = Render<Categories>();
		await cut.InvokeAsync(() => Task.Delay(50));

		// Assert
		var restoreButtons = cut.FindAll("button").Where(b => b.TextContent.Contains("Restore"));
		restoreButtons.Should().NotBeEmpty("Archived category should have a Restore button");
	}

	#endregion

	#region Empty State Tests

	[Fact]
	public async Task Categories_WithNoCategories_DisplaysEmptyState()
	{
		// Arrange
		SetupAuthenticatedUser(isAdmin: true);
		CategoryService.GetCategoriesAsync(Arg.Any<bool>(), Arg.Any<CancellationToken>())
			.Returns(Task.FromResult(Result.Ok<IEnumerable<CategoryDto>>(Enumerable.Empty<CategoryDto>())));

		// Act
		var cut = Render<Categories>();
		await cut.InvokeAsync(() => Task.Delay(50));

		// Assert
		cut.Markup.Should().Contain("No categories", "Empty state should display 'No categories' message");
	}

	[Fact]
	public async Task Categories_WithNoCategories_DisplaysCreateButtonInEmptyState()
	{
		// Arrange
		SetupAuthenticatedUser(isAdmin: true);
		CategoryService.GetCategoriesAsync(Arg.Any<bool>(), Arg.Any<CancellationToken>())
			.Returns(Task.FromResult(Result.Ok<IEnumerable<CategoryDto>>(Enumerable.Empty<CategoryDto>())));

		// Act
		var cut = Render<Categories>();
		await cut.InvokeAsync(() => Task.Delay(50));

		// Assert
		var createButton = cut.FindAll("button").FirstOrDefault(b => b.TextContent.Contains("Create Category"));
		createButton.Should().NotBeNull("Empty state should have a 'Create Category' button");
	}

	[Fact]
	public async Task Categories_WithNoCategories_DisplaysHelpfulMessage()
	{
		// Arrange
		SetupAuthenticatedUser(isAdmin: true);
		CategoryService.GetCategoriesAsync(Arg.Any<bool>(), Arg.Any<CancellationToken>())
			.Returns(Task.FromResult(Result.Ok<IEnumerable<CategoryDto>>(Enumerable.Empty<CategoryDto>())));

		// Act
		var cut = Render<Categories>();
		await cut.InvokeAsync(() => Task.Delay(50));

		// Assert
		cut.Markup.Should().Contain("Get started", "Empty state should display helpful guidance");
	}

	#endregion

	#region Error Handling Tests

	[Fact]
	public async Task Categories_OnServiceError_DisplaysErrorMessage()
	{
		// Arrange
		SetupAuthenticatedUser(isAdmin: true);
		CategoryService.GetCategoriesAsync(Arg.Any<bool>(), Arg.Any<CancellationToken>())
			.Returns(Task.FromResult(Result.Fail<IEnumerable<CategoryDto>>("Failed to load categories")));

		// Act
		var cut = Render<Categories>();
		await cut.InvokeAsync(() => Task.Delay(50));

		// Assert
		cut.Markup.Should().Contain("Failed to load categories", "Error message should be displayed");
	}

	[Fact]
	public async Task Categories_OnServiceException_DisplaysExceptionMessage()
	{
		// Arrange
		SetupAuthenticatedUser(isAdmin: true);
		CategoryService.GetCategoriesAsync(Arg.Any<bool>(), Arg.Any<CancellationToken>())
			.Returns(Task.FromException<Result<IEnumerable<CategoryDto>>>(new Exception("Database connection failed")));

		// Act
		var cut = Render<Categories>();
		await cut.InvokeAsync(() => Task.Delay(50));

		// Assert
		cut.Markup.Should().Contain("Database connection failed", "Exception message should be displayed");
	}

	[Fact]
	public async Task Categories_ErrorMessage_CanBeDismissed()
	{
		// Arrange
		SetupAuthenticatedUser(isAdmin: true);
		CategoryService.GetCategoriesAsync(Arg.Any<bool>(), Arg.Any<CancellationToken>())
			.Returns(Task.FromResult(Result.Fail<IEnumerable<CategoryDto>>("Test error")));

		var cut = Render<Categories>();
		await cut.InvokeAsync(() => Task.Delay(50));

		// Act - Find and click dismiss button in error message
		var errorDiv = cut.Find(".bg-red-50, [class*='bg-red']");
		var dismissButton = errorDiv.QuerySelector("button");

		if (dismissButton != null)
		{
			dismissButton.Click();
			await cut.InvokeAsync(() => Task.Delay(50));

			// Assert
			cut.Markup.Should().NotContain("Test error", "Error message should be dismissed after clicking close button");
		}
	}

	#endregion

	#region Create Category Modal Tests

	[Fact]
	public async Task Categories_CreateButton_OpensModal()
	{
		// Arrange
		SetupAuthenticatedUser(isAdmin: true);
		CategoryService.GetCategoriesAsync(Arg.Any<bool>(), Arg.Any<CancellationToken>())
			.Returns(Task.FromResult(Result.Ok<IEnumerable<CategoryDto>>(new[] { CreateTestCategory() })));

		var cut = Render<Categories>();
		await cut.InvokeAsync(() => Task.Delay(50));

		// Act
		var createButton = cut.FindAll("button").First(b => b.TextContent.Contains("Create Category"));
		createButton.Click();

		// Assert
		cut.Markup.Should().Contain("Create Category", "Modal title should indicate creating a new category");
		cut.Markup.Should().Contain("Name", "Modal should have Name field");
		cut.Markup.Should().Contain("Description", "Modal should have Description field");
	}

	[Fact]
	public async Task Categories_CreateModal_DisplaysRequiredFieldIndicators()
	{
		// Arrange
		SetupAuthenticatedUser(isAdmin: true);
		CategoryService.GetCategoriesAsync(Arg.Any<bool>(), Arg.Any<CancellationToken>())
			.Returns(Task.FromResult(Result.Ok<IEnumerable<CategoryDto>>(Enumerable.Empty<CategoryDto>())));

		var cut = Render<Categories>();
		await cut.InvokeAsync(() => Task.Delay(50));

		// Act
		var createButton = cut.FindAll("button").First(b => b.TextContent.Contains("Create Category"));
		createButton.Click();

		// Assert
		var requiredIndicators = cut.FindAll(".text-red-500");
		requiredIndicators.Should().NotBeEmpty("Required fields should be marked with asterisks");
	}

	[Fact]
	public async Task Categories_CreateModal_CancelClosesModal()
	{
		// Arrange
		SetupAuthenticatedUser(isAdmin: true);
		CategoryService.GetCategoriesAsync(Arg.Any<bool>(), Arg.Any<CancellationToken>())
			.Returns(Task.FromResult(Result.Ok<IEnumerable<CategoryDto>>(new[] { CreateTestCategory() })));

		var cut = Render<Categories>();
		await cut.InvokeAsync(() => Task.Delay(50));

		var createButton = cut.FindAll("button").First(b => b.TextContent.Contains("Create Category"));
		createButton.Click();

		// Act
		var cancelButton = cut.FindAll("button").First(b => b.TextContent.Contains("Cancel"));
		cancelButton.Click();

		// Assert
		var modal = cut.FindAll("[role='dialog']");
		modal.Should().BeEmpty("Modal should be closed after clicking Cancel");
	}

	[Fact]
	public async Task Categories_CreateCategory_CallsServiceWithCorrectData()
	{
		// Arrange
		SetupAuthenticatedUser(isAdmin: true);
		var newCategory = CreateTestCategory(name: "New Bug Category");
		CategoryService.GetCategoriesAsync(Arg.Any<bool>(), Arg.Any<CancellationToken>())
			.Returns(Task.FromResult(Result.Ok<IEnumerable<CategoryDto>>(Enumerable.Empty<CategoryDto>())));
		CategoryService.CreateCategoryAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
			.Returns(Task.FromResult(Result.Ok(newCategory)));

		var cut = Render<Categories>();
		await cut.InvokeAsync(() => Task.Delay(50));

		// Open modal
		var createButton = cut.FindAll("button").First(b => b.TextContent.Contains("Create Category"));
		createButton.Click();

		// Fill in form
		var nameInput = cut.Find("#category-name");
		var descInput = cut.Find("#category-description");
		nameInput.Change("New Bug Category");
		descInput.Change("Description for new bug category");

		// Act - Submit form
		var submitButton = cut.FindAll("button[type='submit']").FirstOrDefault();
		submitButton?.Click();
		await cut.InvokeAsync(() => Task.Delay(50));

		// Assert
		await CategoryService.Received(1).CreateCategoryAsync(
			"New Bug Category",
			"Description for new bug category",
			Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task Categories_CreateSuccess_DisplaysSuccessMessage()
	{
		// Arrange
		SetupAuthenticatedUser(isAdmin: true);
		var newCategory = CreateTestCategory(name: "New Category");
		CategoryService.GetCategoriesAsync(Arg.Any<bool>(), Arg.Any<CancellationToken>())
			.Returns(Task.FromResult(Result.Ok<IEnumerable<CategoryDto>>(Enumerable.Empty<CategoryDto>())));
		CategoryService.CreateCategoryAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
			.Returns(Task.FromResult(Result.Ok(newCategory)));

		var cut = Render<Categories>();
		await cut.InvokeAsync(() => Task.Delay(50));

		// Open modal and fill form
		var createButton = cut.FindAll("button").First(b => b.TextContent.Contains("Create Category"));
		createButton.Click();

		var nameInput = cut.Find("#category-name");
		var descInput = cut.Find("#category-description");
		nameInput.Change("New Category");
		descInput.Change("New description");

		// Act
		var submitButton = cut.FindAll("button[type='submit']").First();
		submitButton.Click();
		await cut.InvokeAsync(() => Task.Delay(100));

		// Assert
		cut.Markup.Should().Contain("created successfully", "Success message should be displayed");
	}

	#endregion

	#region Edit Category Modal Tests

	[Fact]
	public async Task Categories_EditButton_OpensEditModal()
	{
		// Arrange
		SetupAuthenticatedUser(isAdmin: true);
		var category = CreateTestCategory(name: "Bug");
		CategoryService.GetCategoriesAsync(Arg.Any<bool>(), Arg.Any<CancellationToken>())
			.Returns(Task.FromResult(Result.Ok<IEnumerable<CategoryDto>>(new[] { category })));

		var cut = Render<Categories>();
		await cut.InvokeAsync(() => Task.Delay(50));

		// Act
		var editButton = cut.FindAll("button").First(b => b.TextContent.Contains("Edit"));
		editButton.Click();

		// Assert
		cut.Markup.Should().Contain("Edit Category", "Modal title should indicate editing");
	}

	[Fact]
	public async Task Categories_EditModal_PopulatesExistingValues()
	{
		// Arrange
		SetupAuthenticatedUser(isAdmin: true);
		var category = new CategoryDto(
			Id: MongoDB.Bson.ObjectId.GenerateNewId(),
			CategoryName: "Existing Bug",
			CategoryDescription: "Existing bug description",
			DateCreated: DateTime.UtcNow,
			DateModified: null,
			Archived: false,
			ArchivedBy: UserDto.Empty
		);
		CategoryService.GetCategoriesAsync(Arg.Any<bool>(), Arg.Any<CancellationToken>())
			.Returns(Task.FromResult(Result.Ok<IEnumerable<CategoryDto>>(new[] { category })));

		var cut = Render<Categories>();
		await cut.InvokeAsync(() => Task.Delay(50));

		// Act
		var editButton = cut.FindAll("button").First(b => b.TextContent.Contains("Edit"));
		editButton.Click();

		// Assert
		var nameInput = cut.Find("#category-name") as AngleSharp.Html.Dom.IHtmlInputElement;
		nameInput?.Value.Should().Be("Existing Bug", "Name input should be populated with existing value");
	}

	[Fact]
	public async Task Categories_EditModal_DisplaysUpdateButton()
	{
		// Arrange
		SetupAuthenticatedUser(isAdmin: true);
		var category = CreateTestCategory(name: "Bug");
		CategoryService.GetCategoriesAsync(Arg.Any<bool>(), Arg.Any<CancellationToken>())
			.Returns(Task.FromResult(Result.Ok<IEnumerable<CategoryDto>>(new[] { category })));

		var cut = Render<Categories>();
		await cut.InvokeAsync(() => Task.Delay(50));

		// Act
		var editButton = cut.FindAll("button").First(b => b.TextContent.Contains("Edit"));
		editButton.Click();

		// Assert
		var updateButton = cut.FindAll("button[type='submit']").FirstOrDefault();
		updateButton?.TextContent.Should().Contain("Update", "Submit button should say 'Update' when editing");
	}

	[Fact]
	public async Task Categories_UpdateCategory_CallsServiceWithCorrectData()
	{
		// Arrange
		SetupAuthenticatedUser(isAdmin: true);
		var category = CreateTestCategory(name: "Bug");
		var updatedCategory = category with { CategoryName = "Updated Bug" };
		CategoryService.GetCategoriesAsync(Arg.Any<bool>(), Arg.Any<CancellationToken>())
			.Returns(Task.FromResult(Result.Ok<IEnumerable<CategoryDto>>(new[] { category })));
		CategoryService.UpdateCategoryAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
			.Returns(Task.FromResult(Result.Ok(updatedCategory)));

		var cut = Render<Categories>();
		await cut.InvokeAsync(() => Task.Delay(50));

		// Open edit modal
		var editButton = cut.FindAll("button").First(b => b.TextContent.Contains("Edit"));
		editButton.Click();

		// Modify name
		var nameInput = cut.Find("#category-name");
		nameInput.Change("Updated Bug");

		// Act
		var submitButton = cut.FindAll("button[type='submit']").First();
		submitButton.Click();
		await cut.InvokeAsync(() => Task.Delay(50));

		// Assert
		await CategoryService.Received(1).UpdateCategoryAsync(
			category.Id.ToString(),
			"Updated Bug",
			Arg.Any<string>(),
			Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task Categories_UpdateSuccess_DisplaysSuccessMessage()
	{
		// Arrange
		SetupAuthenticatedUser(isAdmin: true);
		var category = CreateTestCategory(name: "Bug");
		var updatedCategory = category with { CategoryName = "Updated Bug" };
		CategoryService.GetCategoriesAsync(Arg.Any<bool>(), Arg.Any<CancellationToken>())
			.Returns(Task.FromResult(Result.Ok<IEnumerable<CategoryDto>>(new[] { category })));
		CategoryService.UpdateCategoryAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
			.Returns(Task.FromResult(Result.Ok(updatedCategory)));

		var cut = Render<Categories>();
		await cut.InvokeAsync(() => Task.Delay(50));

		var editButton = cut.FindAll("button").First(b => b.TextContent.Contains("Edit"));
		editButton.Click();

		var nameInput = cut.Find("#category-name");
		nameInput.Change("Updated Bug");

		// Act
		var submitButton = cut.FindAll("button[type='submit']").First();
		submitButton.Click();
		await cut.InvokeAsync(() => Task.Delay(100));

		// Assert
		cut.Markup.Should().Contain("updated successfully", "Success message should be displayed");
	}

	#endregion

	#region Archive/Restore Tests

	[Fact]
	public async Task Categories_ArchiveButton_CallsArchiveService()
	{
		// Arrange
		SetupAuthenticatedUser(isAdmin: true);
		var category = CreateTestCategory(name: "Bug");
		var archivedCategory = category with { Archived = true };
		CategoryService.GetCategoriesAsync(Arg.Any<bool>(), Arg.Any<CancellationToken>())
			.Returns(Task.FromResult(Result.Ok<IEnumerable<CategoryDto>>(new[] { category })));
		CategoryService.ArchiveCategoryAsync(Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<UserDto>(), Arg.Any<CancellationToken>())
			.Returns(Task.FromResult(Result.Ok(archivedCategory)));

		var cut = Render<Categories>();
		await cut.InvokeAsync(() => Task.Delay(50));

		// Act
		var archiveButton = cut.FindAll("button").First(b => b.TextContent.Contains("Archive"));
		archiveButton.Click();
		await cut.InvokeAsync(() => Task.Delay(50));

		// Assert
		await CategoryService.Received(1).ArchiveCategoryAsync(
			category.Id.ToString(),
			true,
			Arg.Any<UserDto>(),
			Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task Categories_RestoreButton_CallsUnarchiveService()
	{
		// Arrange
		SetupAuthenticatedUser(isAdmin: true);
		var archivedCategory = CreateTestCategory(name: "Archived Bug") with { Archived = true };
		var restoredCategory = archivedCategory with { Archived = false };
		CategoryService.GetCategoriesAsync(Arg.Any<bool>(), Arg.Any<CancellationToken>())
			.Returns(Task.FromResult(Result.Ok<IEnumerable<CategoryDto>>(new[] { archivedCategory })));
		CategoryService.ArchiveCategoryAsync(Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<UserDto>(), Arg.Any<CancellationToken>())
			.Returns(Task.FromResult(Result.Ok(restoredCategory)));

		var cut = Render<Categories>();
		await cut.InvokeAsync(() => Task.Delay(50));

		// Act
		var restoreButton = cut.FindAll("button").First(b => b.TextContent.Contains("Restore"));
		restoreButton.Click();
		await cut.InvokeAsync(() => Task.Delay(50));

		// Assert
		await CategoryService.Received(1).ArchiveCategoryAsync(
			archivedCategory.Id.ToString(),
			false,
			Arg.Any<UserDto>(),
			Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task Categories_ArchiveSuccess_DisplaysSuccessMessage()
	{
		// Arrange
		SetupAuthenticatedUser(isAdmin: true);
		var category = CreateTestCategory(name: "Bug");
		var archivedCategory = category with { Archived = true };
		CategoryService.GetCategoriesAsync(Arg.Any<bool>(), Arg.Any<CancellationToken>())
			.Returns(Task.FromResult(Result.Ok<IEnumerable<CategoryDto>>(new[] { category })));
		CategoryService.ArchiveCategoryAsync(Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<UserDto>(), Arg.Any<CancellationToken>())
			.Returns(Task.FromResult(Result.Ok(archivedCategory)));

		var cut = Render<Categories>();
		await cut.InvokeAsync(() => Task.Delay(50));

		// Act
		var archiveButton = cut.FindAll("button").First(b => b.TextContent.Contains("Archive"));
		archiveButton.Click();
		await cut.InvokeAsync(() => Task.Delay(100));

		// Assert
		cut.Markup.Should().Contain("archived successfully", "Success message should indicate category was archived");
	}

	[Fact]
	public async Task Categories_RestoreSuccess_DisplaysSuccessMessage()
	{
		// Arrange
		SetupAuthenticatedUser(isAdmin: true);
		var archivedCategory = CreateTestCategory(name: "Bug") with { Archived = true };
		var restoredCategory = archivedCategory with { Archived = false };
		CategoryService.GetCategoriesAsync(Arg.Any<bool>(), Arg.Any<CancellationToken>())
			.Returns(Task.FromResult(Result.Ok<IEnumerable<CategoryDto>>(new[] { archivedCategory })));
		CategoryService.ArchiveCategoryAsync(Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<UserDto>(), Arg.Any<CancellationToken>())
			.Returns(Task.FromResult(Result.Ok(restoredCategory)));

		var cut = Render<Categories>();
		await cut.InvokeAsync(() => Task.Delay(50));

		// Act
		var restoreButton = cut.FindAll("button").First(b => b.TextContent.Contains("Restore"));
		restoreButton.Click();
		await cut.InvokeAsync(() => Task.Delay(100));

		// Assert
		cut.Markup.Should().Contain("restored successfully", "Success message should indicate category was restored");
	}

	#endregion

	#region Archive Filter Tests

	[Fact]
	public async Task Categories_IncludeArchivedCheckbox_TogglesFilter()
	{
		// Arrange
		SetupAuthenticatedUser(isAdmin: true);
		var activeCategory = CreateTestCategory(name: "Active");
		var archivedCategory = CreateTestCategory(name: "Archived") with { Archived = true };

		CategoryService.GetCategoriesAsync(false, Arg.Any<CancellationToken>())
			.Returns(Task.FromResult(Result.Ok<IEnumerable<CategoryDto>>(new[] { activeCategory })));
		CategoryService.GetCategoriesAsync(true, Arg.Any<CancellationToken>())
			.Returns(Task.FromResult(Result.Ok<IEnumerable<CategoryDto>>(new[] { activeCategory, archivedCategory })));

		var cut = Render<Categories>();
		await cut.InvokeAsync(() => Task.Delay(50));

		// Act
		var checkbox = cut.Find("input[type='checkbox']");
		checkbox.Change(true);
		await cut.InvokeAsync(() => Task.Delay(50));

		// Assert
		await CategoryService.Received().GetCategoriesAsync(true, Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task Categories_IncludeArchivedCheckbox_DisplaysLabelText()
	{
		// Arrange
		SetupAuthenticatedUser(isAdmin: true);
		CategoryService.GetCategoriesAsync(Arg.Any<bool>(), Arg.Any<CancellationToken>())
			.Returns(Task.FromResult(Result.Ok<IEnumerable<CategoryDto>>(new[] { CreateTestCategory() })));

		// Act
		var cut = Render<Categories>();
		await cut.InvokeAsync(() => Task.Delay(50));

		// Assert
		cut.Markup.Should().Contain("Show archived", "Checkbox label should indicate filtering archived items");
	}

	#endregion

	#region Page Structure Tests

	[Fact]
	public async Task Categories_DisplaysPageTitle()
	{
		// Arrange
		SetupAuthenticatedUser(isAdmin: true);
		CategoryService.GetCategoriesAsync(Arg.Any<bool>(), Arg.Any<CancellationToken>())
			.Returns(Task.FromResult(Result.Ok<IEnumerable<CategoryDto>>(new[] { CreateTestCategory() })));

		// Act
		var cut = Render<Categories>();
		await cut.InvokeAsync(() => Task.Delay(50));

		// Assert
		cut.Markup.Should().Contain("Categories", "Page should display 'Categories' title");
	}

	[Fact]
	public async Task Categories_DisplaysTableHeaders()
	{
		// Arrange
		SetupAuthenticatedUser(isAdmin: true);
		CategoryService.GetCategoriesAsync(Arg.Any<bool>(), Arg.Any<CancellationToken>())
			.Returns(Task.FromResult(Result.Ok<IEnumerable<CategoryDto>>(new[] { CreateTestCategory() })));

		// Act
		var cut = Render<Categories>();
		await cut.InvokeAsync(() => Task.Delay(50));

		// Assert
		cut.Markup.Should().Contain("Name", "Table should have Name header");
		cut.Markup.Should().Contain("Description", "Table should have Description header");
		cut.Markup.Should().Contain("Status", "Table should have Status header");
		cut.Markup.Should().Contain("Created", "Table should have Created header");
	}

	[Fact]
	public async Task Categories_DisplaysFormattedDates()
	{
		// Arrange
		SetupAuthenticatedUser(isAdmin: true);
		var category = new CategoryDto(
			Id: MongoDB.Bson.ObjectId.GenerateNewId(),
			CategoryName: "Test",
			CategoryDescription: "Test description",
			DateCreated: new DateTime(2025, 1, 15),
			DateModified: null,
			Archived: false,
			ArchivedBy: UserDto.Empty
		);
		CategoryService.GetCategoriesAsync(Arg.Any<bool>(), Arg.Any<CancellationToken>())
			.Returns(Task.FromResult(Result.Ok<IEnumerable<CategoryDto>>(new[] { category })));

		// Act
		var cut = Render<Categories>();
		await cut.InvokeAsync(() => Task.Delay(50));

		// Assert
		cut.Markup.Should().Contain("Jan 15, 2025", "Date should be formatted as 'MMM d, yyyy'");
	}

	#endregion
}
