// =======================================================
// Copyright (c) 2025. All rights reserved.
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTrackerApp
// Project Name :  Web.Tests.Bunit
// =======================================================

namespace Web.Tests.Bunit.Components.Issues;

/// <summary>
///   Tests for AttachmentCard component.
/// </summary>
public class AttachmentCardTests : BunitTestBase
{
	#region Helper Methods

	/// <summary>
	///   Creates an AttachmentDto for use in tests.
	/// </summary>
	private static AttachmentDto CreateAttachment(
		string contentType = "image/png",
		string? thumbnailUrl = null,
		string blobUrl = "https://example.com/blob/file.png",
		string fileName = "test-file.png",
		long fileSize = 1024,
		DateTime? uploadedAt = null)
	{
		return new AttachmentDto(
			Id: Guid.NewGuid().ToString(),
			IssueId: Guid.NewGuid().ToString(),
			FileName: fileName,
			ContentType: contentType,
			FileSize: fileSize,
			BlobUrl: blobUrl,
			ThumbnailUrl: thumbnailUrl,
			UploadedBy: new UserDto("user-1", "Jane Doe", "jane@example.com"),
			UploadedAt: uploadedAt ?? new DateTime(2025, 1, 15)
		);
	}

	#endregion

	#region Image Rendering Tests

	[Fact]
	public void AttachmentCard_ImageWithThumbnail_RendersImgWithThumbnailUrl()
	{
		// Arrange
		var attachment = CreateAttachment(
			contentType: "image/png",
			thumbnailUrl: "https://example.com/thumb/file.png",
			blobUrl: "https://example.com/blob/file.png"
		);

		// Act
		var cut = Render<AttachmentCard>(p => p
			.Add(c => c.Attachment, attachment)
		);

		// Assert
		var img = cut.Find("img");
		img.GetAttribute("src").Should().Be("https://example.com/thumb/file.png");
	}

	[Fact]
	public void AttachmentCard_ImageWithoutThumbnail_RendersImgWithBlobUrl()
	{
		// Arrange
		var attachment = CreateAttachment(
			contentType: "image/jpeg",
			thumbnailUrl: null,
			blobUrl: "https://example.com/blob/photo.jpg"
		);

		// Act
		var cut = Render<AttachmentCard>(p => p
			.Add(c => c.Attachment, attachment)
		);

		// Assert
		var img = cut.Find("img");
		img.GetAttribute("src").Should().Be("https://example.com/blob/photo.jpg");
	}

	[Fact]
	public void AttachmentCard_ImageWithEmptyThumbnail_FallsBackToBlobUrl()
	{
		// Arrange
		var attachment = CreateAttachment(
			contentType: "image/gif",
			thumbnailUrl: string.Empty,
			blobUrl: "https://example.com/blob/anim.gif"
		);

		// Act
		var cut = Render<AttachmentCard>(p => p
			.Add(c => c.Attachment, attachment)
		);

		// Assert
		var img = cut.Find("img");
		img.GetAttribute("src").Should().Be("https://example.com/blob/anim.gif");
	}

	#endregion

	#region Document Icon Tests

	[Fact]
	public void AttachmentCard_PdfContentType_ShowsPdfLabel()
	{
		// Arrange
		var attachment = CreateAttachment(contentType: "application/pdf", fileName: "report.pdf");

		// Act
		var cut = Render<AttachmentCard>(p => p
			.Add(c => c.Attachment, attachment)
		);

		// Assert
		cut.Markup.Should().Contain("PDF");
		cut.FindAll("img").Should().BeEmpty();
	}

	[Fact]
	public void AttachmentCard_MarkdownContentType_ShowsMdLabel()
	{
		// Arrange
		var attachment = CreateAttachment(contentType: "text/markdown", fileName: "README.md");

		// Act
		var cut = Render<AttachmentCard>(p => p
			.Add(c => c.Attachment, attachment)
		);

		// Assert
		cut.Markup.Should().Contain("MD");
		cut.FindAll("img").Should().BeEmpty();
	}

	[Fact]
	public void AttachmentCard_UnknownContentType_ShowsGenericTxtLabel()
	{
		// Arrange
		var attachment = CreateAttachment(contentType: "text/plain", fileName: "notes.txt");

		// Act
		var cut = Render<AttachmentCard>(p => p
			.Add(c => c.Attachment, attachment)
		);

		// Assert
		cut.Markup.Should().Contain("TXT");
		cut.FindAll("img").Should().BeEmpty();
	}

	#endregion

	#region File Info Display Tests

	[Fact]
	public void AttachmentCard_Always_ShowsFileName()
	{
		// Arrange
		var attachment = CreateAttachment(fileName: "important-document.pdf", contentType: "application/pdf");

		// Act
		var cut = Render<AttachmentCard>(p => p
			.Add(c => c.Attachment, attachment)
		);

		// Assert
		cut.Markup.Should().Contain("important-document.pdf");
	}

	[Fact]
	public void AttachmentCard_Always_ShowsFileSizeFormatted()
	{
		// Arrange — 1024 bytes → "1 KB"
		var attachment = CreateAttachment(fileSize: 1024, contentType: "text/plain");

		// Act
		var cut = Render<AttachmentCard>(p => p
			.Add(c => c.Attachment, attachment)
		);

		// Assert
		cut.Markup.Should().Contain("1 KB");
	}

	[Fact]
	public void AttachmentCard_Always_ShowsUploadedAtFormatted()
	{
		// Arrange — date renders as "Jan 15, 2025"
		var attachment = CreateAttachment(uploadedAt: new DateTime(2025, 1, 15));

		// Act
		var cut = Render<AttachmentCard>(p => p
			.Add(c => c.Attachment, attachment)
		);

		// Assert
		cut.Markup.Should().Contain("Jan 15, 2025");
	}

	[Fact]
	public void AttachmentCard_Always_ShowsUploaderName()
	{
		// Arrange
		var uploader = new UserDto("u-99", "Alice Smith", "alice@example.com");
		var attachment = new AttachmentDto(
			Id: Guid.NewGuid().ToString(),
			IssueId: Guid.NewGuid().ToString(),
			FileName: "file.txt",
			ContentType: "text/plain",
			FileSize: 512,
			BlobUrl: "https://example.com/file.txt",
			ThumbnailUrl: null,
			UploadedBy: uploader,
			UploadedAt: DateTime.UtcNow
		);

		// Act
		var cut = Render<AttachmentCard>(p => p
			.Add(c => c.Attachment, attachment)
		);

		// Assert
		cut.Markup.Should().Contain("Alice Smith");
	}

	#endregion

	#region Delete Button Tests

	[Fact]
	public void AttachmentCard_CanDeleteTrue_ShowsDeleteButton()
	{
		// Arrange
		var attachment = CreateAttachment();

		// Act
		var cut = Render<AttachmentCard>(p => p
			.Add(c => c.Attachment, attachment)
			.Add(c => c.CanDelete, true)
		);

		// Assert — delete button has title="Delete"
		var deleteBtn = cut.Find("button[title='Delete']");
		deleteBtn.Should().NotBeNull();
	}

	[Fact]
	public void AttachmentCard_CanDeleteFalse_HidesDeleteButton()
	{
		// Arrange
		var attachment = CreateAttachment();

		// Act
		var cut = Render<AttachmentCard>(p => p
			.Add(c => c.Attachment, attachment)
			.Add(c => c.CanDelete, false)
		);

		// Assert
		cut.FindAll("button[title='Delete']").Should().BeEmpty();
	}

	[Fact]
	public void AttachmentCard_DeleteButtonClicked_FiresOnDeleteWithAttachmentId()
	{
		// Arrange
		var attachmentId = Guid.NewGuid().ToString();
		var attachment = new AttachmentDto(
			Id: attachmentId,
			IssueId: Guid.NewGuid().ToString(),
			FileName: "delete-me.pdf",
			ContentType: "application/pdf",
			FileSize: 2048,
			BlobUrl: "https://example.com/delete-me.pdf",
			ThumbnailUrl: null,
			UploadedBy: CreateTestUser(),
			UploadedAt: DateTime.UtcNow
		);
		string? receivedId = null;

		// Act
		var cut = Render<AttachmentCard>(p => p
			.Add(c => c.Attachment, attachment)
			.Add(c => c.CanDelete, true)
			.Add(c => c.OnDelete, EventCallback.Factory.Create<string>(this, id => receivedId = id))
		);
		cut.Find("button[title='Delete']").Click();

		// Assert
		receivedId.Should().Be(attachmentId);
	}

	#endregion
}
