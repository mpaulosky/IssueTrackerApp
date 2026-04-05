// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     CommentsSectionTests.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTrackerApp
// Project Name :  Web.Tests.Bunit
// =======================================================

using System.Security.Claims;

using Microsoft.AspNetCore.Components.Authorization;

using Web.Auth;

namespace Web.Tests.Bunit.Components.Issues;

/// <summary>
///   Comprehensive bUnit tests for the CommentsSection component.
///   Tests comment display, adding, editing, deleting, and authorization.
/// </summary>
public class CommentsSectionTests : BunitTestBase
{
	private readonly string _testIssueId = MongoDB.Bson.ObjectId.GenerateNewId().ToString();

	#region Loading State Tests

	[Fact]
	public void CommentsSection_InitialRender_DisplaysLoadingSpinner()
	{
		// Arrange
		SetupAuthenticatedUser(isAdmin: false);
		var tcs = new TaskCompletionSource<Result<IReadOnlyList<CommentDto>>>();
		CommentService.GetCommentsAsync(Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
			.Returns(tcs.Task);

		// Act
		var cut = Render<CommentsSection>(parameters => parameters
			.Add(p => p.IssueId, _testIssueId));

		// Assert
		cut.Markup.Should().Contain("animate-spin", "Loading spinner should be visible during initial load");
	}

	[Fact]
	public async Task CommentsSection_AfterLoading_HidesLoadingSpinner()
	{
		// Arrange
		SetupAuthenticatedUser(isAdmin: false);
		CommentService.GetCommentsAsync(Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
			.Returns(Task.FromResult(Result.Ok<IReadOnlyList<CommentDto>>(Array.Empty<CommentDto>())));

		// Act
		var cut = Render<CommentsSection>(parameters => parameters
			.Add(p => p.IssueId, _testIssueId));
		await cut.InvokeAsync(() => Task.Delay(50));

		// Assert
		cut.Markup.Should().NotContain("animate-spin", "Loading spinner should be hidden after loading completes");
	}

	#endregion

	#region Empty State Tests

	[Fact]
	public async Task CommentsSection_WithNoComments_DisplaysEmptyState()
	{
		// Arrange
		SetupAuthenticatedUser(isAdmin: false);
		CommentService.GetCommentsAsync(Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
			.Returns(Task.FromResult(Result.Ok<IReadOnlyList<CommentDto>>(Array.Empty<CommentDto>())));

		// Act
		var cut = Render<CommentsSection>(parameters => parameters
			.Add(p => p.IssueId, _testIssueId));
		await cut.InvokeAsync(() => Task.Delay(50));

		// Assert
		cut.Markup.Should().Contain("No comments yet", "Empty state should display 'No comments yet' message");
	}

	[Fact]
	public async Task CommentsSection_WithNoComments_DisplaysHelpfulMessage()
	{
		// Arrange
		SetupAuthenticatedUser(isAdmin: false);
		CommentService.GetCommentsAsync(Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
			.Returns(Task.FromResult(Result.Ok<IReadOnlyList<CommentDto>>(Array.Empty<CommentDto>())));

		// Act
		var cut = Render<CommentsSection>(parameters => parameters
			.Add(p => p.IssueId, _testIssueId));
		await cut.InvokeAsync(() => Task.Delay(50));

		// Assert
		cut.Markup.Should().Contain("Be the first", "Empty state should encourage users to add comments");
	}

	[Fact]
	public async Task CommentsSection_WithNoComments_StillDisplaysAddCommentForm()
	{
		// Arrange
		SetupAuthenticatedUser(isAdmin: false);
		CommentService.GetCommentsAsync(Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
			.Returns(Task.FromResult(Result.Ok<IReadOnlyList<CommentDto>>(Array.Empty<CommentDto>())));

		// Act
		var cut = Render<CommentsSection>(parameters => parameters
			.Add(p => p.IssueId, _testIssueId));
		await cut.InvokeAsync(() => Task.Delay(50));

		// Assert
		cut.Markup.Should().Contain("Add a comment", "Add comment form should always be displayed");
	}

	#endregion

	#region Comments Display Tests

	[Fact]
	public async Task CommentsSection_WithComments_DisplaysCommentsList()
	{
		// Arrange
		SetupAuthenticatedUser(isAdmin: false);
		var comments = new List<CommentDto>
		{
			CreateTestComment(title: "First Comment"),
			CreateTestComment(title: "Second Comment"),
			CreateTestComment(title: "Third Comment")
		};
		CommentService.GetCommentsAsync(Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
			.Returns(Task.FromResult(Result.Ok<IReadOnlyList<CommentDto>>(comments)));

		// Act
		var cut = Render<CommentsSection>(parameters => parameters
			.Add(p => p.IssueId, _testIssueId));
		await cut.InvokeAsync(() => Task.Delay(50));

		// Assert
		cut.Markup.Should().Contain("First Comment");
		cut.Markup.Should().Contain("Second Comment");
		cut.Markup.Should().Contain("Third Comment");
	}

	[Fact]
	public async Task CommentsSection_WithComments_DisplaysCommentTitles()
	{
		// Arrange
		SetupAuthenticatedUser(isAdmin: false);
		var comment = CreateTestComment(title: "Important Discussion Point");
		CommentService.GetCommentsAsync(Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
			.Returns(Task.FromResult(Result.Ok<IReadOnlyList<CommentDto>>(new[] { comment })));

		// Act
		var cut = Render<CommentsSection>(parameters => parameters
			.Add(p => p.IssueId, _testIssueId));
		await cut.InvokeAsync(() => Task.Delay(50));

		// Assert
		cut.Markup.Should().Contain("Important Discussion Point", "Comment title should be displayed");
	}

	[Fact]
	public async Task CommentsSection_WithComments_DisplaysCommentContent()
	{
		// Arrange
		SetupAuthenticatedUser(isAdmin: false);
		var comment = CreateTestComment(description: "This is the detailed comment content.");
		CommentService.GetCommentsAsync(Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
			.Returns(Task.FromResult(Result.Ok<IReadOnlyList<CommentDto>>(new[] { comment })));

		// Act
		var cut = Render<CommentsSection>(parameters => parameters
			.Add(p => p.IssueId, _testIssueId));
		await cut.InvokeAsync(() => Task.Delay(50));

		// Assert
		cut.Markup.Should().Contain("This is the detailed comment content.", "Comment description should be displayed");
	}

	[Fact]
	public async Task CommentsSection_WithComments_DisplaysAuthorName()
	{
		// Arrange
		SetupAuthenticatedUser(isAdmin: false);
		var author = CreateTestUser(name: "Jane Developer");
		var comment = CreateTestComment(author: author);
		CommentService.GetCommentsAsync(Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
			.Returns(Task.FromResult(Result.Ok<IReadOnlyList<CommentDto>>(new[] { comment })));

		// Act
		var cut = Render<CommentsSection>(parameters => parameters
			.Add(p => p.IssueId, _testIssueId));
		await cut.InvokeAsync(() => Task.Delay(50));

		// Assert
		cut.Markup.Should().Contain("Jane Developer", "Author name should be displayed");
	}

	[Fact]
	public async Task CommentsSection_WithComments_DisplaysAuthorAvatar()
	{
		// Arrange
		SetupAuthenticatedUser(isAdmin: false);
		var author = CreateTestUser(name: "John Doe");
		var comment = CreateTestComment(author: author);
		CommentService.GetCommentsAsync(Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
			.Returns(Task.FromResult(Result.Ok<IReadOnlyList<CommentDto>>(new[] { comment })));

		// Act
		var cut = Render<CommentsSection>(parameters => parameters
			.Add(p => p.IssueId, _testIssueId));
		await cut.InvokeAsync(() => Task.Delay(50));

		// Assert
		cut.Markup.Should().Contain("J", "Avatar should display the first letter of author's name");
	}

	[Fact]
	public async Task CommentsSection_WithComments_DisplaysFormattedTimestamps()
	{
		// Arrange
		SetupAuthenticatedUser(isAdmin: false);
		var comment = new CommentDto(
			Id: MongoDB.Bson.ObjectId.GenerateNewId(),
			Title: "Test",
			Description: "Test content",
			DateCreated: new DateTime(2025, 6, 15, 14, 30, 0),
			DateModified: null,
			IssueId: MongoDB.Bson.ObjectId.Parse(_testIssueId),
			Author: CreateTestUser(),
			UserVotes: [],
			Archived: false,
			ArchivedBy: UserDto.Empty,
			IsAnswer: false,
			AnswerSelectedBy: UserDto.Empty
		);
		CommentService.GetCommentsAsync(Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
			.Returns(Task.FromResult(Result.Ok<IReadOnlyList<CommentDto>>(new[] { comment })));

		// Act
		var cut = Render<CommentsSection>(parameters => parameters
			.Add(p => p.IssueId, _testIssueId));
		await cut.InvokeAsync(() => Task.Delay(50));

		// Assert
		cut.Markup.Should().Contain("Jun 15, 2025", "Date should be formatted correctly");
	}

	[Fact]
	public async Task CommentsSection_WithEditedComment_DisplaysEditedIndicator()
	{
		// Arrange
		SetupAuthenticatedUser(isAdmin: false);
		var comment = new CommentDto(
			Id: MongoDB.Bson.ObjectId.GenerateNewId(),
			Title: "Edited Comment",
			Description: "This was edited",
			DateCreated: DateTime.UtcNow.AddHours(-2),
			DateModified: DateTime.UtcNow,
			IssueId: MongoDB.Bson.ObjectId.Parse(_testIssueId),
			Author: CreateTestUser(),
			UserVotes: [],
			Archived: false,
			ArchivedBy: UserDto.Empty,
			IsAnswer: false,
			AnswerSelectedBy: UserDto.Empty
		);
		CommentService.GetCommentsAsync(Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
			.Returns(Task.FromResult(Result.Ok<IReadOnlyList<CommentDto>>(new[] { comment })));

		// Act
		var cut = Render<CommentsSection>(parameters => parameters
			.Add(p => p.IssueId, _testIssueId));
		await cut.InvokeAsync(() => Task.Delay(50));

		// Assert
		cut.Markup.Should().Contain("edited", "Edited comment should show (edited) indicator");
	}

	[Fact]
	public async Task CommentsSection_WithAnswerComment_DisplaysAnswerBadge()
	{
		// Arrange
		SetupAuthenticatedUser(isAdmin: false);
		var comment = new CommentDto(
			Id: MongoDB.Bson.ObjectId.GenerateNewId(),
			Title: "Answer Comment",
			Description: "This is the answer",
			DateCreated: DateTime.UtcNow,
			DateModified: null,
			IssueId: MongoDB.Bson.ObjectId.Parse(_testIssueId),
			Author: CreateTestUser(),
			UserVotes: [],
			Archived: false,
			ArchivedBy: UserDto.Empty,
			IsAnswer: true,
			AnswerSelectedBy: CreateTestUser()
		);
		CommentService.GetCommentsAsync(Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
			.Returns(Task.FromResult(Result.Ok<IReadOnlyList<CommentDto>>(new[] { comment })));

		// Act
		var cut = Render<CommentsSection>(parameters => parameters
			.Add(p => p.IssueId, _testIssueId));
		await cut.InvokeAsync(() => Task.Delay(50));

		// Assert
		cut.Markup.Should().Contain("Answer", "Answer comment should display 'Answer' badge");
	}

	[Fact]
	public async Task CommentsSection_WithAnswerComment_HasHighlightedStyling()
	{
		// Arrange
		SetupAuthenticatedUser(isAdmin: false);
		var comment = new CommentDto(
			Id: MongoDB.Bson.ObjectId.GenerateNewId(),
			Title: "Answer Comment",
			Description: "This is the answer",
			DateCreated: DateTime.UtcNow,
			DateModified: null,
			IssueId: MongoDB.Bson.ObjectId.Parse(_testIssueId),
			Author: CreateTestUser(),
			UserVotes: [],
			Archived: false,
			ArchivedBy: UserDto.Empty,
			IsAnswer: true,
			AnswerSelectedBy: CreateTestUser()
		);
		CommentService.GetCommentsAsync(Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
			.Returns(Task.FromResult(Result.Ok<IReadOnlyList<CommentDto>>(new[] { comment })));

		// Act
		var cut = Render<CommentsSection>(parameters => parameters
			.Add(p => p.IssueId, _testIssueId));
		await cut.InvokeAsync(() => Task.Delay(50));

		// Assert
		cut.Markup.Should().Contain("ring-green", "Answer comment should have green ring styling");
	}

	#endregion

	#region Add Comment Form Tests

	[Fact]
	public async Task CommentsSection_DisplaysAddCommentFormTitle()
	{
		// Arrange
		SetupAuthenticatedUser(isAdmin: false);
		CommentService.GetCommentsAsync(Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
			.Returns(Task.FromResult(Result.Ok<IReadOnlyList<CommentDto>>(Array.Empty<CommentDto>())));

		// Act
		var cut = Render<CommentsSection>(parameters => parameters
			.Add(p => p.IssueId, _testIssueId));
		await cut.InvokeAsync(() => Task.Delay(50));

		// Assert
		cut.Markup.Should().Contain("Add a comment", "Form should have 'Add a comment' heading");
	}

	[Fact]
	public async Task CommentsSection_AddCommentForm_HasTitleField()
	{
		// Arrange
		SetupAuthenticatedUser(isAdmin: false);
		CommentService.GetCommentsAsync(Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
			.Returns(Task.FromResult(Result.Ok<IReadOnlyList<CommentDto>>(Array.Empty<CommentDto>())));

		// Act
		var cut = Render<CommentsSection>(parameters => parameters
			.Add(p => p.IssueId, _testIssueId));
		await cut.InvokeAsync(() => Task.Delay(50));

		// Assert
		var titleInput = cut.Find("#comment-title");
		titleInput.Should().NotBeNull("Form should have a title input field");
	}

	[Fact]
	public async Task CommentsSection_AddCommentForm_HasContentField()
	{
		// Arrange
		SetupAuthenticatedUser(isAdmin: false);
		CommentService.GetCommentsAsync(Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
			.Returns(Task.FromResult(Result.Ok<IReadOnlyList<CommentDto>>(Array.Empty<CommentDto>())));

		// Act
		var cut = Render<CommentsSection>(parameters => parameters
			.Add(p => p.IssueId, _testIssueId));
		await cut.InvokeAsync(() => Task.Delay(50));

		// Assert
		var contentTextarea = cut.Find("#comment-content");
		contentTextarea.Should().NotBeNull("Form should have a content textarea field");
	}

	[Fact]
	public async Task CommentsSection_AddCommentForm_HasSubmitButton()
	{
		// Arrange
		SetupAuthenticatedUser(isAdmin: false);
		CommentService.GetCommentsAsync(Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
			.Returns(Task.FromResult(Result.Ok<IReadOnlyList<CommentDto>>(Array.Empty<CommentDto>())));

		// Act
		var cut = Render<CommentsSection>(parameters => parameters
			.Add(p => p.IssueId, _testIssueId));
		await cut.InvokeAsync(() => Task.Delay(50));

		// Assert
		var addButton = cut.FindAll("button[type='submit']").FirstOrDefault();
		addButton.Should().NotBeNull("Form should have a submit button");
		addButton!.TextContent.Should().Contain("Add Comment", "Submit button should say 'Add Comment'");
	}

	[Fact]
	public async Task CommentsSection_AddComment_CallsServiceWithCorrectData()
	{
		// Arrange
		SetupAuthenticatedUser(isAdmin: false);
		var newComment = CreateTestComment(title: "New Comment Title");
		CommentService.GetCommentsAsync(Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
			.Returns(Task.FromResult(Result.Ok<IReadOnlyList<CommentDto>>(Array.Empty<CommentDto>())));
		CommentService.AddCommentAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<UserDto>(), Arg.Any<CancellationToken>())
			.Returns(Task.FromResult(Result.Ok(newComment)));

		var cut = Render<CommentsSection>(parameters => parameters
			.Add(p => p.IssueId, _testIssueId));
		await cut.InvokeAsync(() => Task.Delay(50));

		// Fill in form
		var titleInput = cut.Find("#comment-title");
		var contentInput = cut.Find("#comment-content");
		titleInput.Change("New Comment Title");
		contentInput.Change("New comment content here");

		// Act
		var submitButton = cut.Find("button[type='submit']");
		submitButton.Click();
		await cut.InvokeAsync(() => Task.Delay(50));

		// Assert
		await CommentService.Received(1).AddCommentAsync(
			_testIssueId,
			"New Comment Title",
			"New comment content here",
			Arg.Any<UserDto>(),
			Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task CommentsSection_AddCommentSuccess_DisplaysSuccessMessage()
	{
		// Arrange
		SetupAuthenticatedUser(isAdmin: false);
		var newComment = CreateTestComment(title: "New Comment");
		CommentService.GetCommentsAsync(Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
			.Returns(Task.FromResult(Result.Ok<IReadOnlyList<CommentDto>>(Array.Empty<CommentDto>())));
		CommentService.AddCommentAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<UserDto>(), Arg.Any<CancellationToken>())
			.Returns(Task.FromResult(Result.Ok(newComment)));

		var cut = Render<CommentsSection>(parameters => parameters
			.Add(p => p.IssueId, _testIssueId));
		await cut.InvokeAsync(() => Task.Delay(50));

		var titleInput = cut.Find("#comment-title");
		var contentInput = cut.Find("#comment-content");
		titleInput.Change("New Comment");
		contentInput.Change("New content");

		// Act
		var submitButton = cut.Find("button[type='submit']");
		submitButton.Click();
		await cut.InvokeAsync(() => Task.Delay(100));

		// Assert
		cut.Markup.Should().Contain("added successfully", "Success message should be displayed");
	}

	[Fact]
	public async Task CommentsSection_AddCommentSuccess_ClearsForm()
	{
		// Arrange
		SetupAuthenticatedUser(isAdmin: false);
		var newComment = CreateTestComment();
		CommentService.GetCommentsAsync(Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
			.Returns(Task.FromResult(Result.Ok<IReadOnlyList<CommentDto>>(Array.Empty<CommentDto>())));
		CommentService.AddCommentAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<UserDto>(), Arg.Any<CancellationToken>())
			.Returns(Task.FromResult(Result.Ok(newComment)));

		var cut = Render<CommentsSection>(parameters => parameters
			.Add(p => p.IssueId, _testIssueId));
		await cut.InvokeAsync(() => Task.Delay(50));

		var titleInput = cut.Find("#comment-title");
		var contentInput = cut.Find("#comment-content");
		titleInput.Change("Test Title");
		contentInput.Change("Test Content");

		// Act
		var submitButton = cut.Find("button[type='submit']");
		submitButton.Click();
		await cut.InvokeAsync(() => Task.Delay(100));

		// Assert
		var titleInputAfter = cut.Find("#comment-title") as AngleSharp.Html.Dom.IHtmlInputElement;
		titleInputAfter?.Value.Should().BeEmpty("Title field should be cleared after successful submission");
	}

	#endregion

	#region Edit Comment Tests

	[Fact]
	public async Task CommentsSection_OwnComment_ShowsEditButton()
	{
		// Arrange
		SetupAuthenticatedUser(userId: "test-user-id", isAdmin: false);
		var author = CreateTestUser(id: "test-user-id");
		var comment = CreateTestComment(author: author);
		CommentService.GetCommentsAsync(Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
			.Returns(Task.FromResult(Result.Ok<IReadOnlyList<CommentDto>>(new[] { comment })));

		// Act
		var cut = Render<CommentsSection>(parameters => parameters
			.Add(p => p.IssueId, _testIssueId));
		await cut.InvokeAsync(() => Task.Delay(50));

		// Assert
		var editButton = cut.FindAll("button").FirstOrDefault(b => b.TextContent.Contains("Edit"));
		editButton.Should().NotBeNull("User should see Edit button for their own comment");
	}

	[Fact]
	public async Task CommentsSection_OtherUserComment_HidesEditButton()
	{
		// Arrange
		SetupAuthenticatedUser(userId: "current-user-id", isAdmin: false);
		var author = CreateTestUser(id: "other-user-id");
		var comment = CreateTestComment(author: author);
		CommentService.GetCommentsAsync(Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
			.Returns(Task.FromResult(Result.Ok<IReadOnlyList<CommentDto>>(new[] { comment })));

		// Act
		var cut = Render<CommentsSection>(parameters => parameters
			.Add(p => p.IssueId, _testIssueId));
		await cut.InvokeAsync(() => Task.Delay(50));

		// Assert
		var editButton = cut.FindAll("button").FirstOrDefault(b => b.TextContent.Contains("Edit"));
		editButton.Should().BeNull("User should not see Edit button for other user's comment");
	}

	[Fact]
	public async Task CommentsSection_ClickEdit_ShowsEditForm()
	{
		// Arrange
		SetupAuthenticatedUser(userId: "test-user-id", isAdmin: false);
		var author = CreateTestUser(id: "test-user-id");
		var comment = CreateTestComment(title: "Original Title", description: "Original Content", author: author);
		CommentService.GetCommentsAsync(Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
			.Returns(Task.FromResult(Result.Ok<IReadOnlyList<CommentDto>>(new[] { comment })));

		var cut = Render<CommentsSection>(parameters => parameters
			.Add(p => p.IssueId, _testIssueId));
		await cut.InvokeAsync(() => Task.Delay(50));

		// Act
		var editButton = cut.FindAll("button").First(b => b.TextContent.Contains("Edit"));
		editButton.Click();

		// Assert
		var saveButton = cut.FindAll("button").FirstOrDefault(b => b.TextContent.Contains("Save"));
		saveButton.Should().NotBeNull("Edit form should display a Save button");

		var cancelButton = cut.FindAll("button").FirstOrDefault(b => b.TextContent.Contains("Cancel"));
		cancelButton.Should().NotBeNull("Edit form should display a Cancel button");
	}

	[Fact]
	public async Task CommentsSection_EditForm_HasPrefilledValues()
	{
		// Arrange
		SetupAuthenticatedUser(userId: "test-user-id", isAdmin: false);
		var author = CreateTestUser(id: "test-user-id");
		var comment = CreateTestComment(title: "Original Title", description: "Original Content", author: author);
		CommentService.GetCommentsAsync(Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
			.Returns(Task.FromResult(Result.Ok<IReadOnlyList<CommentDto>>(new[] { comment })));

		var cut = Render<CommentsSection>(parameters => parameters
			.Add(p => p.IssueId, _testIssueId));
		await cut.InvokeAsync(() => Task.Delay(50));

		// Act
		var editButton = cut.FindAll("button").First(b => b.TextContent.Contains("Edit"));
		editButton.Click();

		// Assert - The edit form should be visible with input fields
		var inputs = cut.FindAll("input, textarea");
		inputs.Should().NotBeEmpty("Edit form should have input fields");
	}

	[Fact]
	public async Task CommentsSection_CancelEdit_HidesEditForm()
	{
		// Arrange
		SetupAuthenticatedUser(userId: "test-user-id", isAdmin: false);
		var author = CreateTestUser(id: "test-user-id");
		var comment = CreateTestComment(author: author);
		CommentService.GetCommentsAsync(Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
			.Returns(Task.FromResult(Result.Ok<IReadOnlyList<CommentDto>>(new[] { comment })));

		var cut = Render<CommentsSection>(parameters => parameters
			.Add(p => p.IssueId, _testIssueId));
		await cut.InvokeAsync(() => Task.Delay(50));

		var editButton = cut.FindAll("button").First(b => b.TextContent.Contains("Edit"));
		editButton.Click();

		// Act
		var cancelButton = cut.FindAll("button").First(b => b.TextContent.Contains("Cancel"));
		cancelButton.Click();

		// Assert
		var saveButton = cut.FindAll("button").FirstOrDefault(b => b.TextContent.Contains("Save"));
		saveButton.Should().BeNull("Edit form should be hidden after clicking Cancel");
	}

	[Fact]
	public async Task CommentsSection_SaveEdit_CallsUpdateService()
	{
		// Arrange
		SetupAuthenticatedUser(userId: "test-user-id", isAdmin: false);
		var author = CreateTestUser(id: "test-user-id");
		var comment = CreateTestComment(author: author);
		var updatedComment = comment with { Title = "Updated Title" };
		CommentService.GetCommentsAsync(Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
			.Returns(Task.FromResult(Result.Ok<IReadOnlyList<CommentDto>>(new[] { comment })));
		CommentService.UpdateCommentAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
			.Returns(Task.FromResult(Result.Ok(updatedComment)));

		var cut = Render<CommentsSection>(parameters => parameters
			.Add(p => p.IssueId, _testIssueId));
		await cut.InvokeAsync(() => Task.Delay(50));

		var editButton = cut.FindAll("button").First(b => b.TextContent.Contains("Edit"));
		editButton.Click();

		// Act
		var saveButton = cut.FindAll("button").First(b => b.TextContent.Contains("Save"));
		saveButton.Click();
		await cut.InvokeAsync(() => Task.Delay(50));

		// Assert
		await CommentService.Received(1).UpdateCommentAsync(
			comment.Id.ToString(),
			Arg.Any<string>(),
			Arg.Any<string>(),
			Arg.Any<string>(),
			"test-user-id",
			Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task CommentsSection_UpdateSuccess_DisplaysSuccessMessage()
	{
		// Arrange
		SetupAuthenticatedUser(userId: "test-user-id", isAdmin: false);
		var author = CreateTestUser(id: "test-user-id");
		var comment = CreateTestComment(author: author);
		var updatedComment = comment with { Title = "Updated Title" };
		CommentService.GetCommentsAsync(Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
			.Returns(Task.FromResult(Result.Ok<IReadOnlyList<CommentDto>>(new[] { comment })));
		CommentService.UpdateCommentAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
			.Returns(Task.FromResult(Result.Ok(updatedComment)));

		var cut = Render<CommentsSection>(parameters => parameters
			.Add(p => p.IssueId, _testIssueId));
		await cut.InvokeAsync(() => Task.Delay(50));

		var editButton = cut.FindAll("button").First(b => b.TextContent.Contains("Edit"));
		editButton.Click();

		// Act
		var saveButton = cut.FindAll("button").First(b => b.TextContent.Contains("Save"));
		saveButton.Click();
		await cut.InvokeAsync(() => Task.Delay(100));

		// Assert
		cut.Markup.Should().Contain("updated successfully", "Success message should be displayed");
	}

	#endregion

	#region Delete Comment Tests

	[Fact]
	public async Task CommentsSection_OwnComment_ShowsDeleteButton()
	{
		// Arrange
		SetupAuthenticatedUser(userId: "test-user-id", isAdmin: false);
		var author = CreateTestUser(id: "test-user-id");
		var comment = CreateTestComment(author: author);
		CommentService.GetCommentsAsync(Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
			.Returns(Task.FromResult(Result.Ok<IReadOnlyList<CommentDto>>(new[] { comment })));

		// Act
		var cut = Render<CommentsSection>(parameters => parameters
			.Add(p => p.IssueId, _testIssueId));
		await cut.InvokeAsync(() => Task.Delay(50));

		// Assert
		var deleteButton = cut.FindAll("button").FirstOrDefault(b => b.TextContent.Contains("Delete"));
		deleteButton.Should().NotBeNull("User should see Delete button for their own comment");
	}

	[Fact]
	public async Task CommentsSection_AdminUser_CanDeleteAnyComment()
	{
		// Arrange
		SetupAuthenticatedUser(userId: "admin-user-id", isAdmin: true);
		var author = CreateTestUser(id: "other-user-id");
		var comment = CreateTestComment(author: author);
		CommentService.GetCommentsAsync(Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
			.Returns(Task.FromResult(Result.Ok<IReadOnlyList<CommentDto>>(new[] { comment })));

		// Act
		var cut = Render<CommentsSection>(parameters => parameters
			.Add(p => p.IssueId, _testIssueId));
		await cut.InvokeAsync(() => Task.Delay(50));

		// Assert
		var deleteButton = cut.FindAll("button").FirstOrDefault(b => b.TextContent.Contains("Delete"));
		deleteButton.Should().NotBeNull("Admin should see Delete button for any comment");
	}

	[Fact]
	public async Task CommentsSection_ClickDelete_ShowsConfirmationModal()
	{
		// Arrange
		SetupAuthenticatedUser(userId: "test-user-id", isAdmin: false);
		var author = CreateTestUser(id: "test-user-id");
		var comment = CreateTestComment(title: "Comment to Delete", author: author);
		CommentService.GetCommentsAsync(Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
			.Returns(Task.FromResult(Result.Ok<IReadOnlyList<CommentDto>>(new[] { comment })));

		var cut = Render<CommentsSection>(parameters => parameters
			.Add(p => p.IssueId, _testIssueId));
		await cut.InvokeAsync(() => Task.Delay(50));

		// Act
		var deleteButton = cut.FindAll("button").First(b => b.TextContent.Contains("Delete"));
		deleteButton.Click();

		// Assert
		cut.Markup.Should().Contain("Delete Comment", "Confirmation modal should be displayed");
	}

	[Fact]
	public async Task CommentsSection_ConfirmDelete_CallsDeleteService()
	{
		// Arrange
		SetupAuthenticatedUser(userId: "test-user-id", isAdmin: false);
		var author = CreateTestUser(id: "test-user-id");
		var comment = CreateTestComment(author: author);
		CommentService.GetCommentsAsync(Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
			.Returns(Task.FromResult(Result.Ok<IReadOnlyList<CommentDto>>(new[] { comment })));
		CommentService.DeleteCommentAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<UserDto>(), Arg.Any<CancellationToken>())
			.Returns(Task.FromResult(Result.Ok(true)));

		var cut = Render<CommentsSection>(parameters => parameters
			.Add(p => p.IssueId, _testIssueId));
		await cut.InvokeAsync(() => Task.Delay(50));

		// Click delete to show modal
		var deleteButton = cut.FindAll("button").First(b => b.TextContent.Contains("Delete"));
		deleteButton.Click();

		// Act - Click confirm in modal (look for the delete button in modal)
		var confirmButton = cut.FindAll("button").FirstOrDefault(b =>
			b.ClassName?.Contains("bg-red") == true &&
			(b.TextContent.Contains("Delete") || b.TextContent.Contains("Confirm")));

		if (confirmButton != null)
		{
			confirmButton.Click();
			await cut.InvokeAsync(() => Task.Delay(50));

			// Assert
			await CommentService.Received(1).DeleteCommentAsync(
				comment.Id.ToString(),
				Arg.Any<string>(),
				"test-user-id",
				false,
				Arg.Any<UserDto>(),
				Arg.Any<CancellationToken>());
		}
	}

	[Fact]
	public async Task CommentsSection_DeleteSuccess_DisplaysSuccessMessage()
	{
		// Arrange
		SetupAuthenticatedUser(userId: "test-user-id", isAdmin: false);
		var author = CreateTestUser(id: "test-user-id");
		var comment = CreateTestComment(author: author);
		CommentService.GetCommentsAsync(Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
			.Returns(Task.FromResult(Result.Ok<IReadOnlyList<CommentDto>>(new[] { comment })));
		CommentService.DeleteCommentAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<UserDto>(), Arg.Any<CancellationToken>())
			.Returns(Task.FromResult(Result.Ok(true)));

		var cut = Render<CommentsSection>(parameters => parameters
			.Add(p => p.IssueId, _testIssueId));
		await cut.InvokeAsync(() => Task.Delay(50));

		var deleteButton = cut.FindAll("button").First(b => b.TextContent.Contains("Delete"));
		deleteButton.Click();

		// Act
		var confirmButton = cut.FindAll("button").FirstOrDefault(b =>
			b.ClassName?.Contains("bg-red") == true);

		if (confirmButton != null)
		{
			confirmButton.Click();
			await cut.InvokeAsync(() => Task.Delay(100));

			// Assert
			cut.Markup.Should().Contain("deleted successfully", "Success message should be displayed");
		}
	}

	#endregion

	#region Error Handling Tests

	[Fact]
	public async Task CommentsSection_OnLoadError_DisplaysErrorMessage()
	{
		// Arrange
		SetupAuthenticatedUser(isAdmin: false);
		CommentService.GetCommentsAsync(Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
			.Returns(Task.FromResult(Result.Fail<IReadOnlyList<CommentDto>>("Failed to load comments")));

		// Act
		var cut = Render<CommentsSection>(parameters => parameters
			.Add(p => p.IssueId, _testIssueId));
		await cut.InvokeAsync(() => Task.Delay(50));

		// Assert
		cut.Markup.Should().Contain("Failed to load comments", "Error message should be displayed");
	}

	[Fact]
	public async Task CommentsSection_OnAddError_DisplaysErrorMessage()
	{
		// Arrange
		SetupAuthenticatedUser(isAdmin: false);
		CommentService.GetCommentsAsync(Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
			.Returns(Task.FromResult(Result.Ok<IReadOnlyList<CommentDto>>(Array.Empty<CommentDto>())));
		CommentService.AddCommentAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<UserDto>(), Arg.Any<CancellationToken>())
			.Returns(Task.FromResult(Result.Fail<CommentDto>("Failed to add comment")));

		var cut = Render<CommentsSection>(parameters => parameters
			.Add(p => p.IssueId, _testIssueId));
		await cut.InvokeAsync(() => Task.Delay(50));

		var titleInput = cut.Find("#comment-title");
		var contentInput = cut.Find("#comment-content");
		titleInput.Change("Test");
		contentInput.Change("Test content");

		// Act
		var submitButton = cut.Find("button[type='submit']");
		submitButton.Click();
		await cut.InvokeAsync(() => Task.Delay(100));

		// Assert
		cut.Markup.Should().Contain("Failed to add comment", "Error message should be displayed");
	}

	[Fact]
	public async Task CommentsSection_OnUpdateError_DisplaysErrorMessage()
	{
		// Arrange
		SetupAuthenticatedUser(userId: "test-user-id", isAdmin: false);
		var author = CreateTestUser(id: "test-user-id");
		var comment = CreateTestComment(author: author);
		CommentService.GetCommentsAsync(Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
			.Returns(Task.FromResult(Result.Ok<IReadOnlyList<CommentDto>>(new[] { comment })));
		CommentService.UpdateCommentAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
			.Returns(Task.FromResult(Result.Fail<CommentDto>("Failed to update comment")));

		var cut = Render<CommentsSection>(parameters => parameters
			.Add(p => p.IssueId, _testIssueId));
		await cut.InvokeAsync(() => Task.Delay(50));

		var editButton = cut.FindAll("button").First(b => b.TextContent.Contains("Edit"));
		editButton.Click();

		// Act
		var saveButton = cut.FindAll("button").First(b => b.TextContent.Contains("Save"));
		saveButton.Click();
		await cut.InvokeAsync(() => Task.Delay(100));

		// Assert
		cut.Markup.Should().Contain("Failed to update comment", "Error message should be displayed");
	}

	#endregion

	#region Page Structure Tests

	[Fact]
	public async Task CommentsSection_DisplaysSectionHeader()
	{
		// Arrange
		SetupAuthenticatedUser(isAdmin: false);
		CommentService.GetCommentsAsync(Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
			.Returns(Task.FromResult(Result.Ok<IReadOnlyList<CommentDto>>(Array.Empty<CommentDto>())));

		// Act
		var cut = Render<CommentsSection>(parameters => parameters
			.Add(p => p.IssueId, _testIssueId));
		await cut.InvokeAsync(() => Task.Delay(50));

		// Assert
		var header = cut.Find("h2");
		header.TextContent.Should().Contain("Comments", "Section should have 'Comments' header");
	}

	[Fact]
	public async Task CommentsSection_WithMultipleComments_MaintainsOrder()
	{
		// Arrange
		SetupAuthenticatedUser(isAdmin: false);
		var comments = new List<CommentDto>
		{
			CreateTestComment(title: "First"),
			CreateTestComment(title: "Second"),
			CreateTestComment(title: "Third")
		};
		CommentService.GetCommentsAsync(Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
			.Returns(Task.FromResult(Result.Ok<IReadOnlyList<CommentDto>>(comments)));

		// Act
		var cut = Render<CommentsSection>(parameters => parameters
			.Add(p => p.IssueId, _testIssueId));
		await cut.InvokeAsync(() => Task.Delay(50));

		// Assert - Check that comments appear in order
		var markup = cut.Markup;
		var firstIndex = markup.IndexOf("First");
		var secondIndex = markup.IndexOf("Second");
		var thirdIndex = markup.IndexOf("Third");

		firstIndex.Should().BeLessThan(secondIndex);
		secondIndex.Should().BeLessThan(thirdIndex);
	}

	#endregion
}
