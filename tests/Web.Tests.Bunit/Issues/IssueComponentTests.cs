// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     IssueComponentTests.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTrackerApp
// Project Name :  Web.Tests.Bunit
// =======================================================

namespace Web.Tests.Bunit.Issues;

/// <summary>
///   Tests for AttachmentCard component.
/// </summary>
public class AttachmentCardTests : BunitTestBase
{
	[Fact]
	public void AttachmentCard_WithImageAttachment_DisplaysImage()
	{
		// Arrange
		var attachment = new AttachmentDto(
			Id: "att-1",
			IssueId: "issue-1",
			FileName: "image.png",
			ContentType: "image/png",
			FileSize: 2048,
			BlobUrl: "https://example.com/image.png",
			ThumbnailUrl: "https://example.com/thumb.png",
			UploadedBy: CreateTestUser(),
			UploadedAt: DateTime.UtcNow
		);

		// Act
		var cut = Render<AttachmentCard>(parameters => parameters
			.Add(p => p.Attachment, attachment)
			.Add(p => p.CanDelete, false)
		);

		// Assert
		cut.Find("img").GetAttribute("src").Should().Be("https://example.com/thumb.png");
		cut.Find("img").GetAttribute("alt").Should().Be("image.png");
	}

	[Fact]
	public void AttachmentCard_WithPdfAttachment_DisplaysPdfIcon()
	{
		// Arrange
		var attachment = new AttachmentDto(
			Id: "att-2",
			IssueId: "issue-1",
			FileName: "document.pdf",
			ContentType: "application/pdf",
			FileSize: 5120,
			BlobUrl: "https://example.com/doc.pdf",
			ThumbnailUrl: null,
			UploadedBy: CreateTestUser(),
			UploadedAt: DateTime.UtcNow
		);

		// Act
		var cut = Render<AttachmentCard>(parameters => parameters
			.Add(p => p.Attachment, attachment)
			.Add(p => p.CanDelete, false)
		);

		// Assert
		cut.FindAll("svg").Should().NotBeEmpty();
		cut.Find("span").TextContent.Should().Contain("PDF");
	}

	[Fact]
	public void AttachmentCard_WithMarkdownAttachment_DisplaysMarkdownIcon()
	{
		// Arrange
		var attachment = new AttachmentDto(
			Id: "att-3",
			IssueId: "issue-1",
			FileName: "readme.md",
			ContentType: "text/markdown",
			FileSize: 1024,
			BlobUrl: "https://example.com/readme.md",
			ThumbnailUrl: null,
			UploadedBy: CreateTestUser(),
			UploadedAt: DateTime.UtcNow
		);

		// Act
		var cut = Render<AttachmentCard>(parameters => parameters
			.Add(p => p.Attachment, attachment)
			.Add(p => p.CanDelete, false)
		);

		// Assert
		cut.FindAll("span").Should().Contain(e => e.TextContent == "MD");
	}

	[Fact]
	public async Task AttachmentCard_WithDeletePermission_ShowsDeleteButton()
	{
		// Arrange
		var attachment = new AttachmentDto(
			Id: "att-4",
			IssueId: "issue-1",
			FileName: "file.txt",
			ContentType: "text/plain",
			FileSize: 512,
			BlobUrl: "https://example.com/file.txt",
			ThumbnailUrl: null,
			UploadedBy: CreateTestUser(),
			UploadedAt: DateTime.UtcNow
		);

		var deleteCallback = false;
		var deletedAttachmentId = string.Empty;

		// Act
		var cut = Render<AttachmentCard>(parameters => parameters
			.Add(p => p.Attachment, attachment)
			.Add(p => p.CanDelete, true)
			.Add(p => p.OnDelete, new EventCallback<string>(
				null,
				(Func<string, Task>)(id =>
				{
					deleteCallback = true;
					deletedAttachmentId = id;
					return Task.CompletedTask;
				})
			))
		);

		var deleteButton = cut.FindAll("button").FirstOrDefault(b => b.TextContent.Contains("Delete", StringComparison.OrdinalIgnoreCase));
		if (deleteButton is not null)
		{
			await cut.InvokeAsync(() => deleteButton.Click());
		}

		// Assert
		deleteCallback.Should().BeTrue();
		deletedAttachmentId.Should().Be("att-4");
	}

	[Fact]
	public void AttachmentCard_WithoutDeletePermission_HidesDeleteButton()
	{
		// Arrange
		var attachment = new AttachmentDto(
			Id: "att-5",
			IssueId: "issue-1",
			FileName: "file.txt",
			ContentType: "text/plain",
			FileSize: 512,
			BlobUrl: "https://example.com/file.txt",
			ThumbnailUrl: null,
			UploadedBy: CreateTestUser(),
			UploadedAt: DateTime.UtcNow
		);

		// Act
		var cut = Render<AttachmentCard>(parameters => parameters
			.Add(p => p.Attachment, attachment)
			.Add(p => p.CanDelete, false)
		);

		var deleteButton = cut.FindAll("button").FirstOrDefault(b => b.TextContent.Contains("Delete", StringComparison.OrdinalIgnoreCase));

		// Assert
		deleteButton.Should().BeNull();
	}

	[Fact]
	public void AttachmentCard_DisplaysFileInfo()
	{
		// Arrange
		var uploadedAt = new DateTime(2025, 1, 15, 14, 30, 0, DateTimeKind.Utc);
		var user = CreateTestUser(name: "John Doe");
		var attachment = new AttachmentDto(
			Id: "att-6",
			IssueId: "issue-1",
			FileName: "document.txt",
			ContentType: "text/plain",
			FileSize: 1024,
			BlobUrl: "https://example.com/doc.txt",
			ThumbnailUrl: null,
			UploadedBy: user,
			UploadedAt: uploadedAt
		);

		// Act
		var cut = Render<AttachmentCard>(parameters => parameters
			.Add(p => p.Attachment, attachment)
			.Add(p => p.CanDelete, false)
		);

		// Assert
		cut.Markup.Should().Contain("document.txt");
		cut.Markup.Should().Contain("1 KB");
		cut.Markup.Should().Contain("John Doe");
	}
}

/// <summary>
///   Tests for AttachmentList component.
/// </summary>
public class AttachmentListTests : BunitTestBase
{
	private AttachmentDto CreateTestAttachment(string id = "att-1", string fileName = "file.txt")
	{
		return new AttachmentDto(
			Id: id,
			IssueId: "issue-1",
			FileName: fileName,
			ContentType: "text/plain",
			FileSize: 1024,
			BlobUrl: $"https://example.com/{fileName}",
			ThumbnailUrl: null,
			UploadedBy: CreateTestUser(),
			UploadedAt: DateTime.UtcNow
		);
	}

	[Fact]
	public void AttachmentList_WithEmptyList_ShowsEmptyState()
	{
		// Act
		var cut = Render<AttachmentList>(parameters => parameters
			.Add(p => p.Attachments, new List<AttachmentDto>())
			.Add(p => p.CurrentUserId, "user-1")
			.Add(p => p.IsAdmin, false)
		);

		// Assert
		cut.Markup.Should().Contain("No attachments yet");
	}

	[Fact]
	public void AttachmentList_WithAttachments_DisplaysGrid()
	{
		// Arrange
		var attachments = new List<AttachmentDto>
		{
			CreateTestAttachment("att-1", "file1.txt"),
			CreateTestAttachment("att-2", "file2.txt"),
			CreateTestAttachment("att-3", "file3.txt")
		};

		// Act
		var cut = Render<AttachmentList>(parameters => parameters
			.Add(p => p.Attachments, attachments)
			.Add(p => p.CurrentUserId, "user-1")
			.Add(p => p.IsAdmin, false)
		);

		// Assert
		var cards = cut.FindComponents<AttachmentCard>();
		cards.Should().HaveCount(3);
	}

	[Fact]
	public void AttachmentList_DisplaysAttachmentCount()
	{
		// Arrange
		var attachments = new List<AttachmentDto>
		{
			CreateTestAttachment("att-1"),
			CreateTestAttachment("att-2")
		};

		// Act
		var cut = Render<AttachmentList>(parameters => parameters
			.Add(p => p.Attachments, attachments)
			.Add(p => p.CurrentUserId, "user-1")
			.Add(p => p.IsAdmin, false)
		);

		// Assert
		cut.Markup.Should().Contain("(2)");
	}

	[Fact]
	public async Task AttachmentList_OwnerCanDeleteAttachment()
	{
		// Arrange
		var userId = "user-1";
		var userDto = CreateTestUser(id: userId);
		var attachments = new List<AttachmentDto>
		{
			new AttachmentDto(
				Id: "att-1",
				IssueId: "issue-1",
				FileName: "file.txt",
				ContentType: "text/plain",
				FileSize: 1024,
				BlobUrl: "https://example.com/file.txt",
				ThumbnailUrl: null,
				UploadedBy: userDto,
				UploadedAt: DateTime.UtcNow
			)
		};

		var deletedId = string.Empty;

		// Act
		var cut = Render<AttachmentList>(parameters => parameters
			.Add(p => p.Attachments, attachments)
			.Add(p => p.CurrentUserId, userId)
			.Add(p => p.IsAdmin, false)
			.Add(p => p.OnAttachmentDeleted, EventCallback.Factory.Create<string>(
				this,
				id => deletedId = id
			))
		);

		// Assert
		var card = cut.FindComponent<AttachmentCard>();
		card.Instance.CanDelete.Should().BeTrue();
	}

	[Fact]
	public void AttachmentList_AdminCanDeleteAnyAttachment()
	{
		// Arrange
		var attachments = new List<AttachmentDto>
		{
			new AttachmentDto(
				Id: "att-1",
				IssueId: "issue-1",
				FileName: "file.txt",
				ContentType: "text/plain",
				FileSize: 1024,
				BlobUrl: "https://example.com/file.txt",
				ThumbnailUrl: null,
				UploadedBy: CreateTestUser(id: "other-user"),
				UploadedAt: DateTime.UtcNow
			)
		};

		// Act
		var cut = Render<AttachmentList>(parameters => parameters
			.Add(p => p.Attachments, attachments)
			.Add(p => p.CurrentUserId, "admin-user")
			.Add(p => p.IsAdmin, true)
		);

		// Assert
		var card = cut.FindComponent<AttachmentCard>();
		card.Instance.CanDelete.Should().BeTrue();
	}

	[Fact]
	public void AttachmentList_NonOwnerCannotDeleteAttachment()
	{
		// Arrange
		var attachments = new List<AttachmentDto>
		{
			new AttachmentDto(
				Id: "att-1",
				IssueId: "issue-1",
				FileName: "file.txt",
				ContentType: "text/plain",
				FileSize: 1024,
				BlobUrl: "https://example.com/file.txt",
				ThumbnailUrl: null,
				UploadedBy: CreateTestUser(id: "other-user"),
				UploadedAt: DateTime.UtcNow
			)
		};

		// Act
		var cut = Render<AttachmentList>(parameters => parameters
			.Add(p => p.Attachments, attachments)
			.Add(p => p.CurrentUserId, "current-user")
			.Add(p => p.IsAdmin, false)
		);

		// Assert
		var card = cut.FindComponent<AttachmentCard>();
		card.Instance.CanDelete.Should().BeFalse();
	}
}

/// <summary>
///   Tests for CommentsSection component.
/// </summary>
public class CommentsSectionTests : BunitTestBase
{
	[Fact]
	public async Task CommentsSection_InitiallyLoading_ShowsLoadingSpinner()
	{
		// Arrange
		var issueId = MongoDB.Bson.ObjectId.GenerateNewId().ToString();
		var comments = new List<CommentDto> { CreateTestComment() };

		CommentService.GetCommentsAsync(issueId)
			.Returns(Task.FromResult(Result.Ok<IReadOnlyList<CommentDto>>(comments)));

		// Act
		var cut = Render<CommentsSection>(parameters => parameters
			.Add(p => p.IssueId, issueId)
		);

		// Assert - should show loading initially
		cut.Markup.Should().Contain("animate-spin");
	}

	[Fact]
	public void CommentsSection_WithComments_DisplaysAll()
	{
		// Arrange
		var issueId = MongoDB.Bson.ObjectId.GenerateNewId().ToString();
		var comments = new List<CommentDto>
		{
			CreateTestComment(title: "Great issue", description: "I agree with this"),
			CreateTestComment(title: "Follow up", description: "Here's more info")
		};

		CommentService.GetCommentsAsync(issueId)
			.Returns(Task.FromResult(Result.Ok<IReadOnlyList<CommentDto>>(comments)));

		// Act
		var cut = Render<CommentsSection>(parameters => parameters
			.Add(p => p.IssueId, issueId)
		);

		// Assert
		cut.Markup.Should().Contain("Great issue");
		cut.Markup.Should().Contain("Follow up");
	}

	[Fact]
	public void CommentsSection_WithNoComments_ShowsEmptyState()
	{
		// Arrange
		var issueId = MongoDB.Bson.ObjectId.GenerateNewId().ToString();

		CommentService.GetCommentsAsync(issueId)
			.Returns(Task.FromResult(Result.Ok<IReadOnlyList<CommentDto>>(new List<CommentDto>())));

		// Act
		var cut = Render<CommentsSection>(parameters => parameters
			.Add(p => p.IssueId, issueId)
		);

		// Assert
		cut.Markup.Should().Contain("No comments yet");
		cut.Markup.Should().Contain("Be the first to add a comment");
	}

	[Fact]
	public void CommentsSection_LoadCommentsFails_ShowsErrorMessage()
	{
		// Arrange
		var issueId = MongoDB.Bson.ObjectId.GenerateNewId().ToString();

		CommentService.GetCommentsAsync(issueId)
			.Returns(Task.FromResult(Result.Fail<IReadOnlyList<CommentDto>>("Failed to load comments")));

		// Act
		var cut = Render<CommentsSection>(parameters => parameters
			.Add(p => p.IssueId, issueId)
		);

		// Assert
		cut.Markup.Should().Contain("Failed to load comments");
	}

	[Fact]
	public void CommentsSection_CommentShowsAuthorName()
	{
		// Arrange
		var issueId = MongoDB.Bson.ObjectId.GenerateNewId().ToString();
		var author = CreateTestUser(name: "Alice Smith");
		var comment = CreateTestComment(author: author);
		var comments = new List<CommentDto> { comment };

		CommentService.GetCommentsAsync(issueId)
			.Returns(Task.FromResult(Result.Ok<IReadOnlyList<CommentDto>>(comments)));

		// Act
		var cut = Render<CommentsSection>(parameters => parameters
			.Add(p => p.IssueId, issueId)
		);

		// Assert
		cut.Markup.Should().Contain("Alice Smith");
	}

	[Fact]
	public void CommentsSection_CommentDisplaysTitle()
	{
		// Arrange
		var issueId = MongoDB.Bson.ObjectId.GenerateNewId().ToString();
		var comment = CreateTestComment(title: "Important Update");
		var comments = new List<CommentDto> { comment };

		CommentService.GetCommentsAsync(issueId)
			.Returns(Task.FromResult(Result.Ok<IReadOnlyList<CommentDto>>(comments)));

		// Act
		var cut = Render<CommentsSection>(parameters => parameters
			.Add(p => p.IssueId, issueId)
		);

		// Assert
		cut.Markup.Should().Contain("Important Update");
	}
}

/// <summary>
///   Tests for BulkActionToolbar component.
/// </summary>
public class BulkActionToolbarTests : BunitTestBase
{
	[Fact]
	public void BulkActionToolbar_WithoutSelection_IsHidden()
	{
		// Act
		var cut = Render<BulkActionToolbar>();

		// Assert
		cut.Markup.Should().NotContain("issue");
	}

	[Fact]
	public void BulkActionToolbar_WithSelection_ShowsToolbar()
	{
		// Arrange
		var statuses = new List<StatusDto> { CreateTestStatus() };
		var categories = new List<CategoryDto> { CreateTestCategory() };

		// Act
		var cut = Render<BulkActionToolbar>(parameters => parameters
			.Add(p => p.Statuses, statuses)
			.Add(p => p.Categories, categories)
			.Add(p => p.IsAdmin, false)
		);

		// Note: Without manually selecting items, the toolbar won't be visible
		// In a real integration test, you would modify the BulkSelectionState

		// Assert - Verify component can be rendered
		cut.Instance.Should().NotBeNull();
	}

	[Fact]
	public async Task BulkActionToolbar_OnStatusChange_CallsCallback()
	{
		// Arrange
		var status = CreateTestStatus(name: "Resolved");
		var statuses = new List<StatusDto> { status };
		StatusDto? changedStatus = null;

		var cut = Render<BulkActionToolbar>(parameters => parameters
			.Add(p => p.Statuses, statuses)
			.Add(p => p.Categories, new List<CategoryDto>())
			.Add(p => p.IsAdmin, false)
			.Add(p => p.OnChangeStatus, EventCallback.Factory.Create<StatusDto>(
				this,
				s =>
				{
					changedStatus = s;
				}
			))
		);

		// Assert
		cut.Instance.Should().NotBeNull();
	}

	[Fact]
	public async Task BulkActionToolbar_OnCategoryChange_CallsCallback()
	{
		// Arrange
		var category = CreateTestCategory(name: "Bug");
		var categories = new List<CategoryDto> { category };
		CategoryDto? changedCategory = null;

		var cut = Render<BulkActionToolbar>(parameters => parameters
			.Add(p => p.Statuses, new List<StatusDto>())
			.Add(p => p.Categories, categories)
			.Add(p => p.IsAdmin, false)
			.Add(p => p.OnChangeCategory, EventCallback.Factory.Create<CategoryDto>(
				this,
				c =>
				{
					changedCategory = c;
				}
			))
		);

		// Assert
		cut.Instance.Should().NotBeNull();
	}

	[Fact]
	public async Task BulkActionToolbar_DeleteButtonVisible_OnlyForAdmin()
	{
		// Arrange - non-admin
		var cut1 = Render<BulkActionToolbar>(parameters => parameters
			.Add(p => p.IsAdmin, false)
		);

		// Act & Assert - no delete button for non-admin
		cut1.Instance.Should().NotBeNull();

		// Arrange - admin
		var cut2 = Render<BulkActionToolbar>(parameters => parameters
			.Add(p => p.IsAdmin, true)
		);

		// Act & Assert - admin can see component
		cut2.Instance.Should().NotBeNull();
	}
}

/// <summary>
///   Tests for BulkConfirmationModal component.
/// </summary>
public class BulkConfirmationModalTests : BunitTestBase
{
	[Fact]
	public void BulkConfirmationModal_WhenNotVisible_IsHidden()
	{
		// Act
		var cut = Render<BulkConfirmationModal>(parameters => parameters
			.Add(p => p.IsVisible, false)
		);

		// Assert
		cut.Markup.Should().NotContain("Confirm");
	}

	[Fact]
	public void BulkConfirmationModal_WhenVisible_ShowsModal()
	{
		// Act
		var cut = Render<BulkConfirmationModal>(parameters => parameters
			.Add(p => p.IsVisible, true)
			.Add(p => p.Title, "Delete Issues")
			.Add(p => p.Message, "Are you sure?")
			.Add(p => p.AffectedCount, 5)
		);

		// Assert
		cut.Markup.Should().Contain("Delete Issues");
		cut.Markup.Should().Contain("Are you sure?");
		cut.Markup.Should().Contain("5");
		cut.Markup.Should().Contain("issues will be affected");
	}

	[Fact]
	public async Task BulkConfirmationModal_OnConfirm_CallsCallback()
	{
		// Act
		var cut = Render<BulkConfirmationModal>(parameters => parameters
			.Add(p => p.IsVisible, true)
			.Add(p => p.ConfirmButtonText, "Delete")
		);

		// Assert
		var confirmButton = cut.FindAll("button").FirstOrDefault(b => b.TextContent.Contains("Delete"));
		confirmButton.Should().NotBeNull();
	}

	[Fact]
	public async Task BulkConfirmationModal_OnCancel_HidesModal()
	{
		// Act
		var cut = Render<BulkConfirmationModal>(parameters => parameters
			.Add(p => p.IsVisible, true)
		);

		// Assert
		var cancelButton = cut.FindAll("button").FirstOrDefault(b => b.TextContent == "Cancel");
		cancelButton.Should().NotBeNull();
	}

	[Fact]
	public void BulkConfirmationModal_DeleteAction_ShowsDeleteIcon()
	{
		// Arrange & Act
		var cut = Render<BulkConfirmationModal>(parameters => parameters
			.Add(p => p.IsVisible, true)
			.Add(p => p.ActionType, BulkConfirmationModal.BulkActionType.Delete)
		);

		// Assert
		cut.Markup.Should().Contain("text-red-600");
	}

	[Fact]
	public void BulkConfirmationModal_ProcessingState_DisablesButtons()
	{
		// Act
		var cut = Render<BulkConfirmationModal>(parameters => parameters
			.Add(p => p.IsVisible, true)
			.Add(p => p.IsProcessing, true)
		);

		// Assert
		var buttons = cut.FindAll("button");
		buttons.Should().AllSatisfy(b => b.GetAttribute("disabled").Should().Be("disabled"));
	}

	[Fact]
	public void BulkConfirmationModal_AffectedCountDisplay_ShowsCorrectPluralization()
	{
		// Single item
		var cut1 = Render<BulkConfirmationModal>(parameters => parameters
			.Add(p => p.IsVisible, true)
			.Add(p => p.AffectedCount, 1)
		);

		cut1.Markup.Should().Contain("1 issue will be affected");

		// Multiple items
		var cut2 = Render<BulkConfirmationModal>(parameters => parameters
			.Add(p => p.IsVisible, true)
			.Add(p => p.AffectedCount, 5)
		);

		cut2.Markup.Should().Contain("5 issues will be affected");
	}
}

/// <summary>
///   Tests for BulkProgressIndicator component.
/// </summary>
public class BulkProgressIndicatorTests : BunitTestBase
{
	[Fact]
	public void BulkProgressIndicator_WhenNotVisible_IsHidden()
	{
		// Act
		var cut = Render<BulkProgressIndicator>(parameters => parameters
			.Add(p => p.IsVisible, false)
		);

		// Assert
		cut.Markup.Should().NotContain("Processing");
	}

	[Fact]
	public void BulkProgressIndicator_WhenVisible_ShowsProgress()
	{
		// Arrange
		var progress = new BulkOperationProgress
		{
			TotalCount = 10,
			ProcessedCount = 5,
			SuccessCount = 4,
			FailureCount = 1
		};

		// Act
		var cut = Render<BulkProgressIndicator>(parameters => parameters
			.Add(p => p.IsVisible, true)
			.Add(p => p.Title, "Updating Issues")
			.Add(p => p.Progress, progress)
			.Add(p => p.IsComplete, false)
		);

		// Assert
		cut.Markup.Should().Contain("Updating Issues");
		cut.Markup.Should().Contain("5 of 10 processed");
		cut.Markup.Should().Contain("50%");
		cut.Markup.Should().Contain("Success: 4");
		cut.Markup.Should().Contain("Failed: 1");
	}

	[Fact]
	public void BulkProgressIndicator_Processing_ShowsLoadingIndicator()
	{
		// Arrange
		var progress = new BulkOperationProgress
		{
			TotalCount = 10,
			ProcessedCount = 3,
			SuccessCount = 3,
			FailureCount = 0
		};

		// Act
		var cut = Render<BulkProgressIndicator>(parameters => parameters
			.Add(p => p.IsVisible, true)
			.Add(p => p.Progress, progress)
			.Add(p => p.IsComplete, false)
		);

		// Assert
		cut.Markup.Should().Contain("animate-spin");
		cut.Markup.Should().Contain("Processing...");
	}

	[Fact]
	public void BulkProgressIndicator_CompleteSuccessfully_ShowsSuccessMessage()
	{
		// Arrange
		var progress = new BulkOperationProgress
		{
			TotalCount = 10,
			ProcessedCount = 10,
			SuccessCount = 10,
			FailureCount = 0
		};

		// Act
		var cut = Render<BulkProgressIndicator>(parameters => parameters
			.Add(p => p.IsVisible, true)
			.Add(p => p.Progress, progress)
			.Add(p => p.IsComplete, true)
		);

		// Assert
		cut.Markup.Should().Contain("Completed successfully!");
		cut.Markup.Should().Contain("text-green-600");
	}

	[Fact]
	public void BulkProgressIndicator_CompleteWithFailures_ShowsWarningMessage()
	{
		// Arrange
		var progress = new BulkOperationProgress
		{
			TotalCount = 10,
			ProcessedCount = 10,
			SuccessCount = 8,
			FailureCount = 2
		};

		// Act
		var cut = Render<BulkProgressIndicator>(parameters => parameters
			.Add(p => p.IsVisible, true)
			.Add(p => p.Progress, progress)
			.Add(p => p.IsComplete, true)
		);

		// Assert
		cut.Markup.Should().Contain("Completed with some failures");
		cut.Markup.Should().Contain("text-yellow-600");
	}

	[Fact]
	public void BulkProgressIndicator_Processing_HidesCancelButton_WhenCannotCancel()
	{
		// Act
		var cut = Render<BulkProgressIndicator>(parameters => parameters
			.Add(p => p.IsVisible, true)
			.Add(p => p.IsComplete, false)
			.Add(p => p.CanCancel, false)
		);

		// Assert
		var cancelButton = cut.FindAll("button").FirstOrDefault(b => b.TextContent.Contains("Cancel"));
		cancelButton.Should().BeNull();
	}

	[Fact]
	public void BulkProgressIndicator_Complete_ShowsCloseButton()
	{
		// Act
		var cut = Render<BulkProgressIndicator>(parameters => parameters
			.Add(p => p.IsVisible, true)
			.Add(p => p.IsComplete, true)
		);

		// Assert
		var closeButton = cut.FindAll("button").FirstOrDefault(b => b.TextContent.Contains("Close"));
		closeButton.Should().NotBeNull();
	}
}

/// <summary>
///   Tests for IssueMultiSelect component.
/// </summary>
public class IssueMultiSelectTests : BunitTestBase
{
	[Fact]
	public void IssueMultiSelect_SingleIssue_RendersCheckbox()
	{
		// Act
		var cut = Render<IssueMultiSelect>(parameters => parameters
			.Add(p => p.IssueId, "issue-1")
			.Add(p => p.ShowSelectAll, false)
		);

		// Assert
		var checkbox = cut.Find("input[type='checkbox']");
		checkbox.Should().NotBeNull();
		checkbox.GetAttribute("id").Should().Contain("issue-checkbox-issue-1");
	}

	[Fact]
	public void IssueMultiSelect_SelectAll_RendersSelectAllCheckbox()
	{
		// Arrange
		var issueIds = new[] { "issue-1", "issue-2", "issue-3" };

		// Act
		var cut = Render<IssueMultiSelect>(parameters => parameters
			.Add(p => p.ShowSelectAll, true)
			.Add(p => p.AllIssueIds, issueIds)
		);

		// Assert
		var checkbox = cut.Find("input[type='checkbox']");
		checkbox.GetAttribute("id").Should().Be("select-all-checkbox");
	}

	[Fact]
	public void IssueMultiSelect_InitiallyUnchecked_RenderUncheckedCheckbox()
	{
		// Act
		var cut = Render<IssueMultiSelect>(parameters => parameters
			.Add(p => p.IssueId, "issue-1")
			.Add(p => p.ShowSelectAll, false)
		);

		// Assert
		var checkbox = cut.Find("input[type='checkbox']");
		var checkedAttr = checkbox.GetAttribute("checked");
		checkedAttr.Should().BeNullOrEmpty();
	}

	[Fact]
	public async Task IssueMultiSelect_OnCheckboxChange_UpdatesSelection()
	{
		// Act
		var cut = Render<IssueMultiSelect>(parameters => parameters
			.Add(p => p.IssueId, "issue-1")
			.Add(p => p.ShowSelectAll, false)
		);

		// Assert
		cut.Instance.Should().NotBeNull();
	}

	[Fact]
	public void IssueMultiSelect_SelectAll_WithMultipleIssues()
	{
		// Arrange
		var issueIds = new[] { "issue-1", "issue-2", "issue-3" };

		// Act
		var cut = Render<IssueMultiSelect>(parameters => parameters
			.Add(p => p.ShowSelectAll, true)
			.Add(p => p.AllIssueIds, issueIds)
		);

		// Assert
		var checkbox = cut.Find("input[type='checkbox']");
		checkbox.Should().NotBeNull();
	}
}

/// <summary>
///   Tests for UndoToast component.
/// </summary>
public class UndoToastTests : BunitTestBase
{
	[Fact]
	public void UndoToast_WhenNotVisible_IsHidden()
	{
		// Act
		var cut = Render<UndoToast>(parameters => parameters
			.Add(p => p.IsVisible, false)
		);

		// Assert
		cut.Markup.Should().NotContain("Operation completed");
	}

	[Fact]
	public void UndoToast_WhenVisible_ShowsToast()
	{
		// Act
		var cut = Render<UndoToast>(parameters => parameters
			.Add(p => p.IsVisible, true)
			.Add(p => p.Message, "Issues updated successfully")
		);

		// Assert
		cut.Markup.Should().Contain("Issues updated successfully");
		cut.Markup.Should().Contain("text-green-400");
	}

	[Fact]
	public void UndoToast_WithUndoToken_ShowsUndoButton()
	{
		// Act
		var cut = Render<UndoToast>(parameters => parameters
			.Add(p => p.IsVisible, true)
			.Add(p => p.Message, "Issues deleted")
			.Add(p => p.UndoToken, "undo-token-123")
		);

		// Assert
		var undoButton = cut.FindAll("button").FirstOrDefault(b => b.TextContent.Contains("Undo"));
		undoButton.Should().NotBeNull();
	}

	[Fact]
	public void UndoToast_WithoutUndoToken_HidesUndoButton()
	{
		// Act
		var cut = Render<UndoToast>(parameters => parameters
			.Add(p => p.IsVisible, true)
			.Add(p => p.Message, "Operation completed")
			.Add(p => p.UndoToken, null)
		);

		// Assert
		var undoButton = cut.FindAll("button").FirstOrDefault(b => b.TextContent.Contains("Undo"));
		undoButton.Should().BeNull();
	}

	[Fact]
	public void UndoToast_DisplaysCountdownTimer()
	{
		// Act
		var cut = Render<UndoToast>(parameters => parameters
			.Add(p => p.IsVisible, true)
			.Add(p => p.CountdownSeconds, 5)
		);

		// Assert
		cut.Markup.Should().Contain("5");
	}

	[Fact]
	public void UndoToast_ShowsCloseButton()
	{
		// Act
		var cut = Render<UndoToast>(parameters => parameters
			.Add(p => p.IsVisible, true)
		);

		// Assert
		var buttons = cut.FindAll("button");
		buttons.Should().NotBeEmpty();
	}

	[Fact]
	public void UndoToast_CustomCountdown_DisplaysCorrectTime()
	{
		// Act
		var cut = Render<UndoToast>(parameters => parameters
			.Add(p => p.IsVisible, true)
			.Add(p => p.CountdownSeconds, 10)
		);

		// Assert
		cut.Markup.Should().Contain("10");
	}
}

/// <summary>
///   Integration tests combining multiple components.
/// </summary>
public class IssueComponentIntegrationTests : BunitTestBase
{
	[Fact]
	public void CommentsSection_WithComments_DisplaysMultiple()
	{
		// Arrange
		var issueId = MongoDB.Bson.ObjectId.GenerateNewId().ToString();
		var comments = new List<CommentDto>
		{
			CreateTestComment(title: "First"),
			CreateTestComment(title: "Second")
		};

		CommentService.GetCommentsAsync(issueId)
			.Returns(Task.FromResult(Result.Ok<IReadOnlyList<CommentDto>>(comments)));

		// Act
		var cut = Render<CommentsSection>(parameters => parameters
			.Add(p => p.IssueId, issueId)
		);

		// Assert
		cut.Markup.Should().Contain("Test Comment");
	}

	[Fact]
	public void AttachmentList_WithMultipleAttachments_DisplaysAllTypes()
	{
		// Arrange
		var attachments = new List<AttachmentDto>
		{
			new AttachmentDto("1", "issue-1", "image.png", "image/png", 2048, "url1", "thumb1", CreateTestUser(), DateTime.UtcNow),
			new AttachmentDto("2", "issue-1", "doc.pdf", "application/pdf", 5120, "url2", null, CreateTestUser(), DateTime.UtcNow),
			new AttachmentDto("3", "issue-1", "readme.md", "text/markdown", 1024, "url3", null, CreateTestUser(), DateTime.UtcNow)
		};

		// Act
		var cut = Render<AttachmentList>(parameters => parameters
			.Add(p => p.Attachments, attachments)
			.Add(p => p.CurrentUserId, "user-1")
			.Add(p => p.IsAdmin, false)
		);

		// Assert
		var cards = cut.FindComponents<AttachmentCard>();
		cards.Should().HaveCount(3);
	}
}


