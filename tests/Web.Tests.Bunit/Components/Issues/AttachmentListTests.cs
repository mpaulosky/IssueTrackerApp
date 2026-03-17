// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     AttachmentListTests.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTrackerApp
// Project Name :  Web.Tests.Bunit
// =======================================================

namespace Web.Tests.Bunit.Components.Issues;

/// <summary>
///   Tests for AttachmentList component.
/// </summary>
public class AttachmentListTests : BunitTestBase
{
	#region Helper Methods

	/// <summary>
	///   Creates a test AttachmentDto.
	/// </summary>
	private static AttachmentDto CreateTestAttachment(
		string? id = null,
		string? fileName = null,
		string contentType = "image/png",
		long fileSize = 1024,
		UserDto? uploadedBy = null)
	{
		return new AttachmentDto(
			Id: id ?? Guid.NewGuid().ToString(),
			IssueId: Guid.NewGuid().ToString(),
			FileName: fileName ?? "test-file.png",
			ContentType: contentType,
			FileSize: fileSize,
			BlobUrl: "https://example.com/blob/test-file.png",
			ThumbnailUrl: contentType.StartsWith("image/") ? "https://example.com/thumb/test-file.png" : null,
			UploadedBy: uploadedBy ?? CreateTestUser(),
			UploadedAt: DateTime.UtcNow
		);
	}

	/// <summary>
	///   Creates a list of test attachments.
	/// </summary>
	private static List<AttachmentDto> CreateTestAttachments(int count)
	{
		return Enumerable.Range(1, count)
			.Select(i => CreateTestAttachment(
				id: $"attachment-{i}",
				fileName: $"file-{i}.png"
			))
			.ToList();
	}

	#endregion

	#region Render Tests - Empty State

	[Fact]
	public void AttachmentList_WithEmptyAttachments_DisplaysEmptyState()
	{
		// Arrange
		var attachments = new List<AttachmentDto>();

		// Act
		var cut = Render<AttachmentList>(parameters => parameters
			.Add(p => p.Attachments, attachments)
			.Add(p => p.CurrentUserId, "test-user-id")
			.Add(p => p.IsAdmin, false)
		);

		// Assert
		cut.Markup.Should().Contain("No attachments yet");
		cut.Markup.Should().Contain("border-dashed");
	}

	[Fact]
	public void AttachmentList_WithNullAttachments_DisplaysEmptyState()
	{
		// Arrange & Act
		var cut = Render<AttachmentList>(parameters => parameters
			.Add(p => p.Attachments, null)
			.Add(p => p.CurrentUserId, "test-user-id")
			.Add(p => p.IsAdmin, false)
		);

		// Assert
		cut.Markup.Should().Contain("No attachments yet");
	}

	#endregion

	#region Render Tests - With Attachments

	[Fact]
	public void AttachmentList_WithSingleAttachment_RendersAttachmentCard()
	{
		// Arrange
		var attachment = CreateTestAttachment(fileName: "document.pdf", contentType: "application/pdf");
		var attachments = new List<AttachmentDto> { attachment };

		// Act
		var cut = Render<AttachmentList>(parameters => parameters
			.Add(p => p.Attachments, attachments)
			.Add(p => p.CurrentUserId, "test-user-id")
			.Add(p => p.IsAdmin, false)
		);

		// Assert
		cut.Markup.Should().NotContain("No attachments yet");
		cut.Markup.Should().Contain("document.pdf");
		cut.Markup.Should().Contain("(1)");
	}

	[Fact]
	public void AttachmentList_WithMultipleAttachments_RendersAllCards()
	{
		// Arrange
		var attachments = CreateTestAttachments(3);

		// Act
		var cut = Render<AttachmentList>(parameters => parameters
			.Add(p => p.Attachments, attachments)
			.Add(p => p.CurrentUserId, "test-user-id")
			.Add(p => p.IsAdmin, false)
		);

		// Assert
		cut.Markup.Should().Contain("file-1.png");
		cut.Markup.Should().Contain("file-2.png");
		cut.Markup.Should().Contain("file-3.png");
		cut.Markup.Should().Contain("(3)");
	}

	[Fact]
	public void AttachmentList_DisplaysAttachmentCount_InHeader()
	{
		// Arrange
		var attachments = CreateTestAttachments(5);

		// Act
		var cut = Render<AttachmentList>(parameters => parameters
			.Add(p => p.Attachments, attachments)
			.Add(p => p.CurrentUserId, "test-user-id")
			.Add(p => p.IsAdmin, false)
		);

		// Assert
		cut.Markup.Should().Contain("Attachments");
		cut.Markup.Should().Contain("(5)");
	}

	#endregion

	#region Loading State Tests

	[Fact]
	public async Task AttachmentList_WhenLoadingStateSet_DisplaysLoadingSpinner()
	{
		// Arrange
		var attachments = new List<AttachmentDto>();
		var cut = Render<AttachmentList>(parameters => parameters
			.Add(p => p.Attachments, attachments)
			.Add(p => p.CurrentUserId, "test-user-id")
			.Add(p => p.IsAdmin, false)
		);

		// Act
		await cut.InvokeAsync(() => cut.Instance.SetLoading(true));

		// Assert
		cut.Markup.Should().Contain("animate-spin");
		cut.Markup.Should().NotContain("No attachments yet");
	}

	[Fact]
	public async Task AttachmentList_WhenLoadingCleared_HidesSpinner()
	{
		// Arrange
		var attachments = new List<AttachmentDto>();
		var cut = Render<AttachmentList>(parameters => parameters
			.Add(p => p.Attachments, attachments)
			.Add(p => p.CurrentUserId, "test-user-id")
			.Add(p => p.IsAdmin, false)
		);

		// Act
		await cut.InvokeAsync(() => cut.Instance.SetLoading(true));
		await cut.InvokeAsync(() => cut.Instance.SetLoading(false));

		// Assert
		cut.Markup.Should().NotContain("animate-spin");
		cut.Markup.Should().Contain("No attachments yet");
	}

	#endregion

	#region Error State Tests

	[Fact]
	public async Task AttachmentList_WhenErrorSet_DisplaysErrorMessage()
	{
		// Arrange
		var attachments = new List<AttachmentDto>();
		var cut = Render<AttachmentList>(parameters => parameters
			.Add(p => p.Attachments, attachments)
			.Add(p => p.CurrentUserId, "test-user-id")
			.Add(p => p.IsAdmin, false)
		);

		// Act
		await cut.InvokeAsync(() => cut.Instance.SetError("Failed to load attachments"));

		// Assert
		cut.Markup.Should().Contain("Failed to load attachments");
		cut.Markup.Should().Contain("bg-red-50");
		cut.Markup.Should().NotContain("No attachments yet");
	}

	[Fact]
	public async Task AttachmentList_WhenErrorCleared_HidesErrorMessage()
	{
		// Arrange
		var attachments = new List<AttachmentDto>();
		var cut = Render<AttachmentList>(parameters => parameters
			.Add(p => p.Attachments, attachments)
			.Add(p => p.CurrentUserId, "test-user-id")
			.Add(p => p.IsAdmin, false)
		);

		// Act
		await cut.InvokeAsync(() => cut.Instance.SetError("Failed to load attachments"));
		await cut.InvokeAsync(() => cut.Instance.ClearError());

		// Assert
		cut.Markup.Should().NotContain("Failed to load attachments");
		cut.Markup.Should().Contain("No attachments yet");
	}

	#endregion

	#region Delete Permission Tests

	[Fact]
	public void AttachmentList_WhenUserIsAdmin_CanDeleteAnyAttachment()
	{
		// Arrange
		var otherUser = CreateTestUser(id: "other-user-id", name: "Other User");
		var attachment = CreateTestAttachment(uploadedBy: otherUser);
		var attachments = new List<AttachmentDto> { attachment };

		// Act
		var cut = Render<AttachmentList>(parameters => parameters
			.Add(p => p.Attachments, attachments)
			.Add(p => p.CurrentUserId, "admin-user-id")
			.Add(p => p.IsAdmin, true)
		);

		// Assert - Admin should see delete button for other user's attachment
		var attachmentCard = cut.FindComponent<AttachmentCard>();
		attachmentCard.Instance.CanDelete.Should().BeTrue();
	}

	[Fact]
	public void AttachmentList_WhenUserIsOwner_CanDeleteOwnAttachment()
	{
		// Arrange
		var currentUser = CreateTestUser(id: "current-user-id", name: "Current User");
		var attachment = CreateTestAttachment(uploadedBy: currentUser);
		var attachments = new List<AttachmentDto> { attachment };

		// Act
		var cut = Render<AttachmentList>(parameters => parameters
			.Add(p => p.Attachments, attachments)
			.Add(p => p.CurrentUserId, "current-user-id")
			.Add(p => p.IsAdmin, false)
		);

		// Assert
		var attachmentCard = cut.FindComponent<AttachmentCard>();
		attachmentCard.Instance.CanDelete.Should().BeTrue();
	}

	[Fact]
	public void AttachmentList_WhenUserIsNotOwnerOrAdmin_CannotDeleteAttachment()
	{
		// Arrange
		var otherUser = CreateTestUser(id: "other-user-id", name: "Other User");
		var attachment = CreateTestAttachment(uploadedBy: otherUser);
		var attachments = new List<AttachmentDto> { attachment };

		// Act
		var cut = Render<AttachmentList>(parameters => parameters
			.Add(p => p.Attachments, attachments)
			.Add(p => p.CurrentUserId, "current-user-id")
			.Add(p => p.IsAdmin, false)
		);

		// Assert
		var attachmentCard = cut.FindComponent<AttachmentCard>();
		attachmentCard.Instance.CanDelete.Should().BeFalse();
	}

	#endregion

	#region Delete Callback Tests

	[Fact]
	public async Task AttachmentList_WhenDeleteConfirmed_InvokesOnAttachmentDeleted()
	{
		// Arrange
		var currentUser = CreateTestUser(id: "current-user-id");
		var attachment = CreateTestAttachment(id: "attachment-to-delete", uploadedBy: currentUser);
		var attachments = new List<AttachmentDto> { attachment };

		string? deletedAttachmentId = null;
		var cut = Render<AttachmentList>(parameters => parameters
			.Add(p => p.Attachments, attachments)
			.Add(p => p.CurrentUserId, "current-user-id")
			.Add(p => p.IsAdmin, false)
			.Add(p => p.OnAttachmentDeleted, EventCallback.Factory.Create<string>(this, id => deletedAttachmentId = id))
		);

		// Act - Find and click delete button on attachment card
		var attachmentCard = cut.FindComponent<AttachmentCard>();
		await cut.InvokeAsync(() => attachmentCard.Instance.OnDelete.InvokeAsync("attachment-to-delete"));

		// Assert - Modal should appear
		cut.Markup.Should().Contain("Delete Attachment");

		// Act - Confirm delete
		var confirmButton = cut.FindAll("button").FirstOrDefault(b => b.TextContent.Contains("Delete") && !b.TextContent.Contains("Deleting"));
		confirmButton.Should().NotBeNull();
		await cut.InvokeAsync(() => confirmButton!.Click());

		// Assert
		deletedAttachmentId.Should().Be("attachment-to-delete");
	}

	[Fact]
	public async Task AttachmentList_WhenDeleteCancelled_DoesNotInvokeCallback()
	{
		// Arrange
		var currentUser = CreateTestUser(id: "current-user-id");
		var attachment = CreateTestAttachment(id: "attachment-to-delete", uploadedBy: currentUser);
		var attachments = new List<AttachmentDto> { attachment };

		string? deletedAttachmentId = null;
		var cut = Render<AttachmentList>(parameters => parameters
			.Add(p => p.Attachments, attachments)
			.Add(p => p.CurrentUserId, "current-user-id")
			.Add(p => p.IsAdmin, false)
			.Add(p => p.OnAttachmentDeleted, EventCallback.Factory.Create<string>(this, id => deletedAttachmentId = id))
		);

		// Act - Trigger delete modal
		var attachmentCard = cut.FindComponent<AttachmentCard>();
		await cut.InvokeAsync(() => attachmentCard.Instance.OnDelete.InvokeAsync("attachment-to-delete"));

		// Act - Cancel delete
		var cancelButton = cut.FindAll("button").FirstOrDefault(b => b.TextContent.Contains("Cancel"));
		cancelButton.Should().NotBeNull();
		await cut.InvokeAsync(() => cancelButton!.Click());

		// Assert
		deletedAttachmentId.Should().BeNull();
	}

	#endregion

	#region Aria Attributes Tests

	[Fact]
	public void AttachmentList_HasProperHeadingStructure()
	{
		// Arrange
		var attachments = CreateTestAttachments(2);

		// Act
		var cut = Render<AttachmentList>(parameters => parameters
			.Add(p => p.Attachments, attachments)
			.Add(p => p.CurrentUserId, "test-user-id")
			.Add(p => p.IsAdmin, false)
		);

		// Assert
		var heading = cut.Find("h3");
		heading.Should().NotBeNull();
		heading.TextContent.Should().Contain("Attachments");
	}

	[Fact]
	public void AttachmentList_DeleteModal_HasProperAriaAttributes()
	{
		// Arrange
		var currentUser = CreateTestUser(id: "current-user-id");
		var attachment = CreateTestAttachment(uploadedBy: currentUser);
		var attachments = new List<AttachmentDto> { attachment };

		var cut = Render<AttachmentList>(parameters => parameters
			.Add(p => p.Attachments, attachments)
			.Add(p => p.CurrentUserId, "current-user-id")
			.Add(p => p.IsAdmin, false)
		);

		// Act - Open delete modal
		var attachmentCard = cut.FindComponent<AttachmentCard>();
		cut.InvokeAsync(() => attachmentCard.Instance.OnDelete.InvokeAsync(attachment.Id));

		// Assert
		var modal = cut.Find("div[role='dialog']");
		modal.Should().NotBeNull();
		modal.GetAttribute("aria-modal").Should().Be("true");
		modal.GetAttribute("aria-labelledby").Should().Be("modal-title");
	}

	#endregion

	#region Grid Layout Tests

	[Fact]
	public void AttachmentList_WithAttachments_RendersGridLayout()
	{
		// Arrange
		var attachments = CreateTestAttachments(4);

		// Act
		var cut = Render<AttachmentList>(parameters => parameters
			.Add(p => p.Attachments, attachments)
			.Add(p => p.CurrentUserId, "test-user-id")
			.Add(p => p.IsAdmin, false)
		);

		// Assert
		var gridDiv = cut.Find("div.grid");
		gridDiv.Should().NotBeNull();
		gridDiv.ClassList.Should().Contain("grid-cols-2");
	}

	#endregion

	#region Attachment Type Tests

	[Fact]
	public void AttachmentList_WithImageAttachment_DisplaysImagePreview()
	{
		// Arrange
		var attachment = CreateTestAttachment(fileName: "photo.jpg", contentType: "image/jpeg");
		var attachments = new List<AttachmentDto> { attachment };

		// Act
		var cut = Render<AttachmentList>(parameters => parameters
			.Add(p => p.Attachments, attachments)
			.Add(p => p.CurrentUserId, "test-user-id")
			.Add(p => p.IsAdmin, false)
		);

		// Assert
		cut.Markup.Should().Contain("photo.jpg");
		cut.Markup.Should().Contain("img");
	}

	[Fact]
	public void AttachmentList_WithPdfAttachment_DisplaysPdfIcon()
	{
		// Arrange
		var attachment = CreateTestAttachment(fileName: "document.pdf", contentType: "application/pdf");
		var attachments = new List<AttachmentDto> { attachment };

		// Act
		var cut = Render<AttachmentList>(parameters => parameters
			.Add(p => p.Attachments, attachments)
			.Add(p => p.CurrentUserId, "test-user-id")
			.Add(p => p.IsAdmin, false)
		);

		// Assert
		cut.Markup.Should().Contain("document.pdf");
		cut.Markup.Should().Contain("PDF");
	}

	[Fact]
	public void AttachmentList_WithMarkdownAttachment_DisplaysMarkdownIcon()
	{
		// Arrange
		var attachment = CreateTestAttachment(fileName: "readme.md", contentType: "text/markdown");
		var attachments = new List<AttachmentDto> { attachment };

		// Act
		var cut = Render<AttachmentList>(parameters => parameters
			.Add(p => p.Attachments, attachments)
			.Add(p => p.CurrentUserId, "test-user-id")
			.Add(p => p.IsAdmin, false)
		);

		// Assert
		cut.Markup.Should().Contain("readme.md");
		cut.Markup.Should().Contain("MD");
	}

	#endregion

	#region Error Callback Tests

	[Fact]
	public async Task AttachmentList_WhenDeleteFails_InvokesOnError()
	{
		// Arrange
		var currentUser = CreateTestUser(id: "current-user-id");
		var attachment = CreateTestAttachment(uploadedBy: currentUser);
		var attachments = new List<AttachmentDto> { attachment };

		string? errorMessage = null;
		var cut = Render<AttachmentList>(parameters => parameters
			.Add(p => p.Attachments, attachments)
			.Add(p => p.CurrentUserId, "current-user-id")
			.Add(p => p.IsAdmin, false)
			.Add(p => p.OnAttachmentDeleted, EventCallback.Factory.Create<string>(this,
				_ => throw new Exception("Delete failed")))
			.Add(p => p.OnError, EventCallback.Factory.Create<string>(this, e => errorMessage = e))
		);

		// Act - Trigger delete
		var attachmentCard = cut.FindComponent<AttachmentCard>();
		await cut.InvokeAsync(() => attachmentCard.Instance.OnDelete.InvokeAsync(attachment.Id));

		// Confirm delete
		var confirmButton = cut.FindAll("button").FirstOrDefault(b => b.TextContent.Contains("Delete") && !b.TextContent.Contains("Deleting"));
		await cut.InvokeAsync(() => confirmButton!.Click());

		// Assert
		errorMessage.Should().Contain("Failed to delete attachment");
	}

	#endregion
}
