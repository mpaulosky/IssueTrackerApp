// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     CreatePageTests.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTrackerApp
// Project Name :  Web.Tests.Bunit
// =======================================================

using System.Security.Claims;

using Microsoft.AspNetCore.Components.Authorization;

using Web.Components.Pages.Issues;

namespace Web.Tests.Bunit.Pages.Issues;

/// <summary>
///   Comprehensive tests for the Create Issue page component.
/// </summary>
public class CreatePageTests : BunitTestBase
{
	#region Initial Render Tests

	[Fact]
	public async Task Create_RendersPageTitle()
	{
		// Arrange
		var categories = new List<CategoryDto> { CreateTestCategory() };
		LookupService.GetCategoriesAsync(Arg.Any<CancellationToken>())
			.Returns(Result.Ok<IEnumerable<CategoryDto>>(categories));

		// Act
		var cut = Render<Create>();
		await cut.InvokeAsync(() => Task.Delay(50));

		// Assert
		cut.Find("h1").TextContent.Should().Contain("Create New Issue");
	}

	[Fact]
	public async Task Create_RendersSubtitle()
	{
		// Arrange
		var categories = new List<CategoryDto> { CreateTestCategory() };
		LookupService.GetCategoriesAsync(Arg.Any<CancellationToken>())
			.Returns(Result.Ok<IEnumerable<CategoryDto>>(categories));

		// Act
		var cut = Render<Create>();
		await cut.InvokeAsync(() => Task.Delay(50));

		// Assert
		cut.Markup.Should().Contain("Fill out the form below to create a new issue");
	}

	[Fact]
	public async Task Create_RendersBreadcrumb()
	{
		// Arrange
		var categories = new List<CategoryDto> { CreateTestCategory() };
		LookupService.GetCategoriesAsync(Arg.Any<CancellationToken>())
			.Returns(Result.Ok<IEnumerable<CategoryDto>>(categories));

		// Act
		var cut = Render<Create>();
		await cut.InvokeAsync(() => Task.Delay(50));

		// Assert
		var breadcrumb = cut.Find("nav[aria-label='Breadcrumb']");
		breadcrumb.Should().NotBeNull();
		breadcrumb.TextContent.Should().Contain("Issues");
		breadcrumb.TextContent.Should().Contain("Create");
	}

	#endregion

	#region Form Field Tests

	[Fact]
	public async Task Create_RendersTitleField()
	{
		// Arrange
		var categories = new List<CategoryDto> { CreateTestCategory() };
		LookupService.GetCategoriesAsync(Arg.Any<CancellationToken>())
			.Returns(Result.Ok<IEnumerable<CategoryDto>>(categories));

		// Act
		var cut = Render<Create>();
		await cut.InvokeAsync(() => Task.Delay(50));

		// Assert
		var titleInput = cut.Find("input#title");
		titleInput.Should().NotBeNull();
		titleInput.GetAttribute("placeholder").Should().Contain("descriptive title");
	}

	[Fact]
	public async Task Create_RendersTitleLabel()
	{
		// Arrange
		var categories = new List<CategoryDto> { CreateTestCategory() };
		LookupService.GetCategoriesAsync(Arg.Any<CancellationToken>())
			.Returns(Result.Ok<IEnumerable<CategoryDto>>(categories));

		// Act
		var cut = Render<Create>();
		await cut.InvokeAsync(() => Task.Delay(50));

		// Assert
		var titleLabel = cut.Find("label[for='title']");
		titleLabel.Should().NotBeNull();
		titleLabel.TextContent.Should().Contain("Title");
		titleLabel.InnerHtml.Should().Contain("text-red-500"); // Required indicator
	}

	[Fact]
	public async Task Create_RendersDescriptionField()
	{
		// Arrange
		var categories = new List<CategoryDto> { CreateTestCategory() };
		LookupService.GetCategoriesAsync(Arg.Any<CancellationToken>())
			.Returns(Result.Ok<IEnumerable<CategoryDto>>(categories));

		// Act
		var cut = Render<Create>();
		await cut.InvokeAsync(() => Task.Delay(50));

		// Assert
		var descriptionTextArea = cut.Find("textarea#description");
		descriptionTextArea.Should().NotBeNull();
		descriptionTextArea.GetAttribute("placeholder").Should().Contain("Describe the issue in detail");
		descriptionTextArea.GetAttribute("rows").Should().Be("6");
	}

	[Fact]
	public async Task Create_RendersCategoryDropdown()
	{
		// Arrange
		var categories = new List<CategoryDto> { CreateTestCategory() };
		LookupService.GetCategoriesAsync(Arg.Any<CancellationToken>())
			.Returns(Result.Ok<IEnumerable<CategoryDto>>(categories));

		// Act
		var cut = Render<Create>();
		await cut.InvokeAsync(() => Task.Delay(50));

		// Assert
		var categorySelect = cut.Find("select#category");
		categorySelect.Should().NotBeNull();
	}

	[Fact]
	public async Task Create_CategoryDropdownHasDefaultOption()
	{
		// Arrange
		var categories = new List<CategoryDto> { CreateTestCategory() };
		LookupService.GetCategoriesAsync(Arg.Any<CancellationToken>())
			.Returns(Result.Ok<IEnumerable<CategoryDto>>(categories));

		// Act
		var cut = Render<Create>();
		await cut.InvokeAsync(() => Task.Delay(50));

		// Assert
		var categorySelect = cut.Find("select#category");
		var defaultOption = categorySelect.QuerySelector("option[value='']");
		defaultOption.Should().NotBeNull();
		defaultOption!.TextContent.Should().Contain("Select a category");
	}

	#endregion

	#region Category Population Tests

	[Fact]
	public async Task Create_LoadsCategoriesOnInit()
	{
		// Arrange
		var categories = new List<CategoryDto>
		{
			CreateTestCategory(name: "Bug"),
			CreateTestCategory(name: "Feature"),
			CreateTestCategory(name: "Enhancement")
		};
		LookupService.GetCategoriesAsync(Arg.Any<CancellationToken>())
			.Returns(Result.Ok<IEnumerable<CategoryDto>>(categories));

		// Act
		var cut = Render<Create>();
		await cut.InvokeAsync(() => Task.Delay(50));

		// Assert
		var categorySelect = cut.Find("select#category");
		categorySelect.TextContent.Should().Contain("Bug");
		categorySelect.TextContent.Should().Contain("Feature");
		categorySelect.TextContent.Should().Contain("Enhancement");
	}

	[Fact]
	public async Task Create_WhenCategoriesFailToLoad_HandlesGracefully()
	{
		// Arrange
		LookupService.GetCategoriesAsync(Arg.Any<CancellationToken>())
			.Returns(Result.Fail<IEnumerable<CategoryDto>>("Failed to load categories"));

		// Act
		var cut = Render<Create>();
		await cut.InvokeAsync(() => Task.Delay(50));

		// Assert - Should still render form without categories
		var categorySelect = cut.Find("select#category");
		categorySelect.Should().NotBeNull();
		var options = categorySelect.QuerySelectorAll("option");
		options.Length.Should().Be(1); // Only default "Select a category" option
	}

	#endregion

	#region Form Actions Tests

	[Fact]
	public async Task Create_RendersCancelButton()
	{
		// Arrange
		var categories = new List<CategoryDto> { CreateTestCategory() };
		LookupService.GetCategoriesAsync(Arg.Any<CancellationToken>())
			.Returns(Result.Ok<IEnumerable<CategoryDto>>(categories));

		// Act
		var cut = Render<Create>();
		await cut.InvokeAsync(() => Task.Delay(50));

		// Assert
		var cancelLink = cut.FindAll("a[href='/issues']")
			.FirstOrDefault(a => a.TextContent.Contains("Cancel"));
		cancelLink.Should().NotBeNull();
	}

	[Fact]
	public async Task Create_RendersSubmitButton()
	{
		// Arrange
		var categories = new List<CategoryDto> { CreateTestCategory() };
		LookupService.GetCategoriesAsync(Arg.Any<CancellationToken>())
			.Returns(Result.Ok<IEnumerable<CategoryDto>>(categories));

		// Act
		var cut = Render<Create>();
		await cut.InvokeAsync(() => Task.Delay(50));

		// Assert
		var submitButton = cut.Find("button[type='submit']");
		submitButton.Should().NotBeNull();
		submitButton.TextContent.Should().Contain("Create Issue");
	}

	[Fact]
	public async Task Create_SubmitButtonNotDisabledInitially()
	{
		// Arrange
		var categories = new List<CategoryDto> { CreateTestCategory() };
		LookupService.GetCategoriesAsync(Arg.Any<CancellationToken>())
			.Returns(Result.Ok<IEnumerable<CategoryDto>>(categories));

		// Act
		var cut = Render<Create>();
		await cut.InvokeAsync(() => Task.Delay(50));

		// Assert
		var submitButton = cut.Find("button[type='submit']");
		submitButton.HasAttribute("disabled").Should().BeFalse();
	}

	#endregion

	#region Validation Tests

	[Fact]
	public async Task Create_WhenTitleEmpty_ShowsValidationError()
	{
		// Arrange
		var categories = new List<CategoryDto> { CreateTestCategory() };
		LookupService.GetCategoriesAsync(Arg.Any<CancellationToken>())
			.Returns(Result.Ok<IEnumerable<CategoryDto>>(categories));

		var cut = Render<Create>();
		await cut.InvokeAsync(() => Task.Delay(50));

		// Act - Submit form without filling fields
		var form = cut.Find("form");
		await cut.InvokeAsync(() => form.Submit());

		// Assert
		cut.Markup.Should().Contain("Title is required");
	}

	[Fact]
	public async Task Create_WhenDescriptionEmpty_ShowsValidationError()
	{
		// Arrange
		var categories = new List<CategoryDto> { CreateTestCategory() };
		LookupService.GetCategoriesAsync(Arg.Any<CancellationToken>())
			.Returns(Result.Ok<IEnumerable<CategoryDto>>(categories));

		var cut = Render<Create>();
		await cut.InvokeAsync(() => Task.Delay(50));

		// Fill title but not description
		var titleInput = cut.Find("input#title");
		await cut.InvokeAsync(() => titleInput.Change("Test Issue Title"));

		// Act
		var form = cut.Find("form");
		await cut.InvokeAsync(() => form.Submit());

		// Assert
		cut.Markup.Should().Contain("Description is required");
	}

	[Fact]
	public async Task Create_WhenCategoryNotSelected_ShowsValidationError()
	{
		// Arrange
		var categories = new List<CategoryDto> { CreateTestCategory() };
		LookupService.GetCategoriesAsync(Arg.Any<CancellationToken>())
			.Returns(Result.Ok<IEnumerable<CategoryDto>>(categories));

		var cut = Render<Create>();
		await cut.InvokeAsync(() => Task.Delay(50));

		// Fill title and description but not category
		var titleInput = cut.Find("input#title");
		await cut.InvokeAsync(() => titleInput.Change("Test Issue Title"));

		var descriptionInput = cut.Find("textarea#description");
		await cut.InvokeAsync(() => descriptionInput.Change("This is a detailed description of the issue."));

		// Act
		var form = cut.Find("form");
		await cut.InvokeAsync(() => form.Submit());

		// Assert
		cut.Markup.Should().Contain("Category is required");
	}

	[Fact]
	public async Task Create_WhenTitleTooShort_ShowsValidationError()
	{
		// Arrange
		var categories = new List<CategoryDto> { CreateTestCategory() };
		LookupService.GetCategoriesAsync(Arg.Any<CancellationToken>())
			.Returns(Result.Ok<IEnumerable<CategoryDto>>(categories));

		var cut = Render<Create>();
		await cut.InvokeAsync(() => Task.Delay(50));

		// Fill with short title
		var titleInput = cut.Find("input#title");
		await cut.InvokeAsync(() => titleInput.Change("Hi")); // Less than 5 chars

		var descriptionInput = cut.Find("textarea#description");
		await cut.InvokeAsync(() => descriptionInput.Change("This is a detailed description of the issue."));

		// Act
		var form = cut.Find("form");
		await cut.InvokeAsync(() => form.Submit());

		// Assert
		cut.Markup.Should().Contain("between 5 and 200 characters");
	}

	[Fact]
	public async Task Create_WhenDescriptionTooShort_ShowsValidationError()
	{
		// Arrange
		var categories = new List<CategoryDto> { CreateTestCategory() };
		LookupService.GetCategoriesAsync(Arg.Any<CancellationToken>())
			.Returns(Result.Ok<IEnumerable<CategoryDto>>(categories));

		var cut = Render<Create>();
		await cut.InvokeAsync(() => Task.Delay(50));

		var titleInput = cut.Find("input#title");
		await cut.InvokeAsync(() => titleInput.Change("Valid Title Here"));

		// Fill with short description
		var descriptionInput = cut.Find("textarea#description");
		await cut.InvokeAsync(() => descriptionInput.Change("Short")); // Less than 10 chars

		// Act
		var form = cut.Find("form");
		await cut.InvokeAsync(() => form.Submit());

		// Assert
		cut.Markup.Should().Contain("between 10 and 5000 characters");
	}

	#endregion

	#region Successful Submission Tests

	[Fact]
	public async Task Create_WhenFormValidAndSubmitted_CallsIssueService()
	{
		// Arrange
		var category = CreateTestCategory(name: "Bug");
		var categories = new List<CategoryDto> { category };
		LookupService.GetCategoriesAsync(Arg.Any<CancellationToken>())
			.Returns(Result.Ok<IEnumerable<CategoryDto>>(categories));

		var createdIssue = CreateTestIssue(title: "Test Issue");
		IssueService.CreateIssueAsync(
				Arg.Any<string>(),
				Arg.Any<string>(),
				Arg.Any<CategoryDto>(),
				Arg.Any<UserDto>(),
				Arg.Any<CancellationToken>())
			.Returns(Result.Ok(createdIssue));

		var cut = Render<Create>();
		await cut.InvokeAsync(() => Task.Delay(50));

		// Fill form
		var titleInput = cut.Find("input#title");
		await cut.InvokeAsync(() => titleInput.Change("Valid Test Issue Title"));

		var descriptionInput = cut.Find("textarea#description");
		await cut.InvokeAsync(() => descriptionInput.Change("This is a valid description that is long enough."));

		var categorySelect = cut.Find("select#category");
		await cut.InvokeAsync(() => categorySelect.Change(category.Id.ToString()));

		// Act
		var form = cut.Find("form");
		await cut.InvokeAsync(() => form.Submit());

		// Assert
		await IssueService.Received(1).CreateIssueAsync(
			"Valid Test Issue Title",
			"This is a valid description that is long enough.",
			Arg.Is<CategoryDto>(c => c.CategoryName == "Bug"),
			Arg.Any<UserDto>(),
			Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task Create_WhenSubmissionSucceeds_NavigatesToIssueDetails()
	{
		// Arrange
		var category = CreateTestCategory(name: "Bug");
		var categories = new List<CategoryDto> { category };
		LookupService.GetCategoriesAsync(Arg.Any<CancellationToken>())
			.Returns(Result.Ok<IEnumerable<CategoryDto>>(categories));

		var createdIssue = CreateTestIssue(title: "Test Issue");
		IssueService.CreateIssueAsync(
				Arg.Any<string>(),
				Arg.Any<string>(),
				Arg.Any<CategoryDto>(),
				Arg.Any<UserDto>(),
				Arg.Any<CancellationToken>())
			.Returns(Result.Ok(createdIssue));

		var cut = Render<Create>();
		await cut.InvokeAsync(() => Task.Delay(50));

		// Fill form
		var titleInput = cut.Find("input#title");
		await cut.InvokeAsync(() => titleInput.Change("Valid Test Issue Title"));

		var descriptionInput = cut.Find("textarea#description");
		await cut.InvokeAsync(() => descriptionInput.Change("This is a valid description that is long enough."));

		var categorySelect = cut.Find("select#category");
		await cut.InvokeAsync(() => categorySelect.Change(category.Id.ToString()));

		// Act
		var form = cut.Find("form");
		await cut.InvokeAsync(() => form.Submit());

		// Assert
		var navManager = Services.GetRequiredService<NavigationManager>();
		navManager.Uri.Should().Contain($"/issues/{createdIssue.Id}");
	}

	#endregion

	#region Submission Error Tests

	[Fact]
	public async Task Create_WhenSubmissionFails_ShowsErrorMessage()
	{
		// Arrange
		var category = CreateTestCategory(name: "Bug");
		var categories = new List<CategoryDto> { category };
		LookupService.GetCategoriesAsync(Arg.Any<CancellationToken>())
			.Returns(Result.Ok<IEnumerable<CategoryDto>>(categories));

		IssueService.CreateIssueAsync(
				Arg.Any<string>(),
				Arg.Any<string>(),
				Arg.Any<CategoryDto>(),
				Arg.Any<UserDto>(),
				Arg.Any<CancellationToken>())
			.Returns(Result.Fail<IssueDto>("Database connection failed"));

		var cut = Render<Create>();
		await cut.InvokeAsync(() => Task.Delay(50));

		// Fill form
		var titleInput = cut.Find("input#title");
		await cut.InvokeAsync(() => titleInput.Change("Valid Test Issue Title"));

		var descriptionInput = cut.Find("textarea#description");
		await cut.InvokeAsync(() => descriptionInput.Change("This is a valid description that is long enough."));

		var categorySelect = cut.Find("select#category");
		await cut.InvokeAsync(() => categorySelect.Change(category.Id.ToString()));

		// Act
		var form = cut.Find("form");
		await cut.InvokeAsync(() => form.Submit());

		// Assert
		cut.Markup.Should().Contain("Error");
		cut.Markup.Should().Contain("Database connection failed");
		cut.Markup.Should().Contain("bg-red-50");
	}

	[Fact]
	public async Task Create_WhenExceptionThrown_ShowsErrorMessage()
	{
		// Arrange
		var category = CreateTestCategory(name: "Bug");
		var categories = new List<CategoryDto> { category };
		LookupService.GetCategoriesAsync(Arg.Any<CancellationToken>())
			.Returns(Result.Ok<IEnumerable<CategoryDto>>(categories));

		IssueService.CreateIssueAsync(
				Arg.Any<string>(),
				Arg.Any<string>(),
				Arg.Any<CategoryDto>(),
				Arg.Any<UserDto>(),
				Arg.Any<CancellationToken>())
			.Returns<Result<IssueDto>>(_ => throw new Exception("Unexpected error occurred"));

		var cut = Render<Create>();
		await cut.InvokeAsync(() => Task.Delay(50));

		// Fill form
		var titleInput = cut.Find("input#title");
		await cut.InvokeAsync(() => titleInput.Change("Valid Test Issue Title"));

		var descriptionInput = cut.Find("textarea#description");
		await cut.InvokeAsync(() => descriptionInput.Change("This is a valid description that is long enough."));

		var categorySelect = cut.Find("select#category");
		await cut.InvokeAsync(() => categorySelect.Change(category.Id.ToString()));

		// Act
		var form = cut.Find("form");
		await cut.InvokeAsync(() => form.Submit());

		// Assert
		cut.Markup.Should().Contain("Unexpected error occurred");
	}

	#endregion

	#region Submit Button State Tests

	[Fact]
	public async Task Create_WhenSubmitting_ShowsLoadingState()
	{
		// Arrange
		var category = CreateTestCategory(name: "Bug");
		var categories = new List<CategoryDto> { category };
		LookupService.GetCategoriesAsync(Arg.Any<CancellationToken>())
			.Returns(Result.Ok<IEnumerable<CategoryDto>>(categories));

		// Use TaskCompletionSource to control when the service call completes
		var tcs = new TaskCompletionSource<Result<IssueDto>>();
		IssueService.CreateIssueAsync(
				Arg.Any<string>(),
				Arg.Any<string>(),
				Arg.Any<CategoryDto>(),
				Arg.Any<UserDto>(),
				Arg.Any<CancellationToken>())
			.Returns(tcs.Task);

		var cut = Render<Create>();
		await cut.InvokeAsync(() => Task.Delay(50));

		// Fill form
		var titleInput = cut.Find("input#title");
		await cut.InvokeAsync(() => titleInput.Change("Valid Test Issue Title"));

		var descriptionInput = cut.Find("textarea#description");
		await cut.InvokeAsync(() => descriptionInput.Change("This is a valid description that is long enough."));

		var categorySelect = cut.Find("select#category");
		await cut.InvokeAsync(() => categorySelect.Change(category.Id.ToString()));

		// Act - Start submission (don't await)
		var form = cut.Find("form");
		var submitTask = cut.InvokeAsync(() => form.Submit());

		// Assert - During submission
		var submitButton = cut.Find("button[type='submit']");
		submitButton.HasAttribute("disabled").Should().BeTrue();
		cut.Markup.Should().Contain("Creating...");
		cut.Markup.Should().Contain("animate-spin");

		// Cleanup
		tcs.SetResult(Result.Ok(CreateTestIssue()));
		await submitTask;
	}

	[Fact]
	public async Task Create_AfterSubmission_RestoresButtonState()
	{
		// Arrange
		var category = CreateTestCategory(name: "Bug");
		var categories = new List<CategoryDto> { category };
		LookupService.GetCategoriesAsync(Arg.Any<CancellationToken>())
			.Returns(Result.Ok<IEnumerable<CategoryDto>>(categories));

		IssueService.CreateIssueAsync(
				Arg.Any<string>(),
				Arg.Any<string>(),
				Arg.Any<CategoryDto>(),
				Arg.Any<UserDto>(),
				Arg.Any<CancellationToken>())
			.Returns(Result.Fail<IssueDto>("Error"));

		var cut = Render<Create>();
		await cut.InvokeAsync(() => Task.Delay(50));

		// Fill form
		var titleInput = cut.Find("input#title");
		await cut.InvokeAsync(() => titleInput.Change("Valid Test Issue Title"));

		var descriptionInput = cut.Find("textarea#description");
		await cut.InvokeAsync(() => descriptionInput.Change("This is a valid description that is long enough."));

		var categorySelect = cut.Find("select#category");
		await cut.InvokeAsync(() => categorySelect.Change(category.Id.ToString()));

		// Act
		var form = cut.Find("form");
		await cut.InvokeAsync(() => form.Submit());

		// Assert - After failed submission, button should be re-enabled
		var submitButton = cut.Find("button[type='submit']");
		submitButton.HasAttribute("disabled").Should().BeFalse();
		cut.Markup.Should().NotContain("Creating...");
	}

	#endregion

	#region Form Styling Tests

	[Fact]
	public async Task Create_HasProperFormStyling()
	{
		// Arrange
		var categories = new List<CategoryDto> { CreateTestCategory() };
		LookupService.GetCategoriesAsync(Arg.Any<CancellationToken>())
			.Returns(Result.Ok<IEnumerable<CategoryDto>>(categories));

		// Act
		var cut = Render<Create>();
		await cut.InvokeAsync(() => Task.Delay(50));

		// Assert
		cut.Markup.Should().Contain("max-w-2xl");
		cut.Markup.Should().Contain("shadow");
		cut.Markup.Should().Contain("rounded-lg");
	}

	[Fact]
	public async Task Create_FormActionsHaveProperstyling()
	{
		// Arrange
		var categories = new List<CategoryDto> { CreateTestCategory() };
		LookupService.GetCategoriesAsync(Arg.Any<CancellationToken>())
			.Returns(Result.Ok<IEnumerable<CategoryDto>>(categories));

		// Act
		var cut = Render<Create>();
		await cut.InvokeAsync(() => Task.Delay(50));

		// Assert
		cut.Markup.Should().Contain("bg-gray-50");
		cut.Markup.Should().Contain("flex");
		cut.Markup.Should().Contain("justify-end");
	}

	#endregion

	#region Duplicate Submission Prevention Tests

	[Fact]
	public async Task Create_WhenAlreadySubmitting_IgnoresAdditionalSubmits()
	{
		// Arrange
		var category = CreateTestCategory(name: "Bug");
		var categories = new List<CategoryDto> { category };
		LookupService.GetCategoriesAsync(Arg.Any<CancellationToken>())
			.Returns(Result.Ok<IEnumerable<CategoryDto>>(categories));

		var callCount = 0;
		var tcs = new TaskCompletionSource<Result<IssueDto>>();
		IssueService.CreateIssueAsync(
				Arg.Any<string>(),
				Arg.Any<string>(),
				Arg.Any<CategoryDto>(),
				Arg.Any<UserDto>(),
				Arg.Any<CancellationToken>())
			.Returns(_ =>
			{
				callCount++;
				return tcs.Task;
			});

		var cut = Render<Create>();
		await cut.InvokeAsync(() => Task.Delay(50));

		// Fill form
		var titleInput = cut.Find("input#title");
		await cut.InvokeAsync(() => titleInput.Change("Valid Test Issue Title"));

		var descriptionInput = cut.Find("textarea#description");
		await cut.InvokeAsync(() => descriptionInput.Change("This is a valid description that is long enough."));

		var categorySelect = cut.Find("select#category");
		await cut.InvokeAsync(() => categorySelect.Change(category.Id.ToString()));

		// Act - Try to submit multiple times
		var form = cut.Find("form");
		var submitTask1 = cut.InvokeAsync(() => form.Submit());
		var submitTask2 = cut.InvokeAsync(() => form.Submit());
		var submitTask3 = cut.InvokeAsync(() => form.Submit());

		// Complete the submission
		tcs.SetResult(Result.Ok(CreateTestIssue()));
		await Task.WhenAll(submitTask1, submitTask2, submitTask3);

		// Assert - Service should only be called once
		callCount.Should().Be(1);
	}

	#endregion

	#region Invalid Category Selection Tests

	[Fact]
	public async Task Create_WhenInvalidCategorySelected_ShowsError()
	{
		// Arrange
		var category = CreateTestCategory(name: "Bug");
		var categories = new List<CategoryDto> { category };
		LookupService.GetCategoriesAsync(Arg.Any<CancellationToken>())
			.Returns(Result.Ok<IEnumerable<CategoryDto>>(categories));

		var cut = Render<Create>();
		await cut.InvokeAsync(() => Task.Delay(50));

		// Fill form with valid title and description
		var titleInput = cut.Find("input#title");
		await cut.InvokeAsync(() => titleInput.Change("Valid Test Issue Title"));

		var descriptionInput = cut.Find("textarea#description");
		await cut.InvokeAsync(() => descriptionInput.Change("This is a valid description that is long enough."));

		// Select an invalid category (non-existent ID)
		var categorySelect = cut.Find("select#category");
		await cut.InvokeAsync(() => categorySelect.Change("invalid-category-id"));

		// Act
		var form = cut.Find("form");
		await cut.InvokeAsync(() => form.Submit());

		// Assert
		cut.Markup.Should().Contain("Please select a valid category");
	}

	#endregion
}
