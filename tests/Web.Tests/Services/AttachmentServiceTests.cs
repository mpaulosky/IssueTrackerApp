// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     AttachmentServiceTests.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTrackerApp
// Project Name :  Web.Tests
// =======================================================

using Web.Services;

namespace Web.Tests.Services;

/// <summary>
///   Unit tests for AttachmentService facade operations.
///   Tests file attachment CRUD orchestration and MediatR integration.
/// </summary>
public sealed class AttachmentServiceTests
{
	private readonly IMediator _mediator;
	private readonly ILogger<AttachmentService> _logger;
	private readonly AttachmentService _sut;

	public AttachmentServiceTests()
	{
		_mediator = Substitute.For<IMediator>();
		_logger = Substitute.For<ILogger<AttachmentService>>();
		_sut = new AttachmentService(_mediator, _logger);
	}

	#region Constructor Tests

	[Fact]
	public void Constructor_WithNullMediator_ThrowsArgumentNullException()
	{
		// Act
		var act = () => new AttachmentService(null!, _logger);

		// Assert
		act.Should().Throw<ArgumentNullException>()
			.WithParameterName("mediator");
	}

	[Fact]
	public void Constructor_WithNullLogger_ThrowsArgumentNullException()
	{
		// Act
		var act = () => new AttachmentService(_mediator, null!);

		// Assert
		act.Should().Throw<ArgumentNullException>()
			.WithParameterName("logger");
	}

	#endregion

	#region GetIssueAttachmentsAsync Tests

	[Fact]
	public async Task GetIssueAttachmentsAsync_WithValidIssueId_ReturnsAttachments()
	{
		// Arrange
		var issueId = "issue-123";
		var attachments = new List<AttachmentDto>
		{
			CreateTestAttachmentDto("file1.pdf"),
			CreateTestAttachmentDto("file2.png")
		};
		_mediator.Send(Arg.Any<GetIssueAttachmentsQuery>(), Arg.Any<CancellationToken>())
			.Returns(Result.Ok<IEnumerable<AttachmentDto>>(attachments));

		// Act
		var result = await _sut.GetIssueAttachmentsAsync(issueId);

		// Assert
		result.Success.Should().BeTrue();
		result.Value.Should().HaveCount(2);
	}

	[Fact]
	public async Task GetIssueAttachmentsAsync_SendsCorrectQuery()
	{
		// Arrange
		var issueId = "issue-123";
		_mediator.Send(Arg.Any<GetIssueAttachmentsQuery>(), Arg.Any<CancellationToken>())
			.Returns(Result.Ok<IEnumerable<AttachmentDto>>(new List<AttachmentDto>()));

		// Act
		await _sut.GetIssueAttachmentsAsync(issueId);

		// Assert
		await _mediator.Received(1).Send(
			Arg.Is<GetIssueAttachmentsQuery>(q => q.IssueId == issueId),
			Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task GetIssueAttachmentsAsync_WhenMediatorFails_ReturnsFailure()
	{
		// Arrange
		_mediator.Send(Arg.Any<GetIssueAttachmentsQuery>(), Arg.Any<CancellationToken>())
			.Returns(Result.Fail<IEnumerable<AttachmentDto>>("Issue not found"));

		// Act
		var result = await _sut.GetIssueAttachmentsAsync("invalid-id");

		// Assert
		result.Success.Should().BeFalse();
		result.Error.Should().Contain("Issue not found");
	}

	[Fact]
	public async Task GetIssueAttachmentsAsync_WithNoAttachments_ReturnsEmptyList()
	{
		// Arrange
		_mediator.Send(Arg.Any<GetIssueAttachmentsQuery>(), Arg.Any<CancellationToken>())
			.Returns(Result.Ok<IEnumerable<AttachmentDto>>(new List<AttachmentDto>()));

		// Act
		var result = await _sut.GetIssueAttachmentsAsync("issue-with-no-attachments");

		// Assert
		result.Success.Should().BeTrue();
		result.Value.Should().BeEmpty();
	}

	[Fact]
	public async Task GetIssueAttachmentsAsync_WhenExceptionThrown_ReturnsFailureAndLogs()
	{
		// Arrange
		var issueId = "issue-123";
		_mediator.When(x => x.Send(Arg.Any<GetIssueAttachmentsQuery>(), Arg.Any<CancellationToken>()))
			.Do(_ => throw new InvalidOperationException("Database connection failed"));

		// Act
		var result = await _sut.GetIssueAttachmentsAsync(issueId);

		// Assert
		result.Success.Should().BeFalse();
		result.Error.Should().Contain("Failed to retrieve attachments");
	}

	#endregion

	#region AddAttachmentAsync Tests

	[Fact]
	public async Task AddAttachmentAsync_WithValidData_ReturnsCreatedAttachment()
	{
		// Arrange
		var issueId = "issue-123";
		using var stream = new MemoryStream(new byte[] { 1, 2, 3 });
		var uploadedBy = CreateTestUserDto();
		var createdAttachment = CreateTestAttachmentDto("newfile.pdf");

		_mediator.Send(Arg.Any<AddAttachmentCommand>(), Arg.Any<CancellationToken>())
			.Returns(Result.Ok(createdAttachment));

		// Act
		var result = await _sut.AddAttachmentAsync(issueId, stream, "newfile.pdf", "application/pdf", 1024, uploadedBy);

		// Assert
		result.Success.Should().BeTrue();
		result.Value!.FileName.Should().Be("newfile.pdf");
	}

	[Fact]
	public async Task AddAttachmentAsync_SendsCorrectCommand()
	{
		// Arrange
		var issueId = "issue-123";
		using var stream = new MemoryStream(new byte[] { 1, 2, 3 });
		var uploadedBy = CreateTestUserDto();
		var createdAttachment = CreateTestAttachmentDto("test.pdf");
		_mediator.Send(Arg.Any<AddAttachmentCommand>(), Arg.Any<CancellationToken>())
			.Returns(Result.Ok(createdAttachment));

		// Act
		await _sut.AddAttachmentAsync(issueId, stream, "test.pdf", "application/pdf", 2048, uploadedBy);

		// Assert
		await _mediator.Received(1).Send(
			Arg.Is<AddAttachmentCommand>(c =>
				c.IssueId == issueId &&
				c.FileName == "test.pdf" &&
				c.ContentType == "application/pdf" &&
				c.FileSize == 2048 &&
				c.UploadedBy == uploadedBy),
			Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task AddAttachmentAsync_WhenValidationFails_ReturnsFailure()
	{
		// Arrange
		using var stream = new MemoryStream();
		_mediator.Send(Arg.Any<AddAttachmentCommand>(), Arg.Any<CancellationToken>())
			.Returns(Result.Fail<AttachmentDto>("File size exceeds maximum allowed"));

		// Act
		var result = await _sut.AddAttachmentAsync("issue-123", stream, "huge.zip", "application/zip", 100_000_000, CreateTestUserDto());

		// Assert
		result.Success.Should().BeFalse();
		result.Error.Should().Contain("exceeds");
	}

	[Fact]
	public async Task AddAttachmentAsync_WhenIssueNotFound_ReturnsFailure()
	{
		// Arrange
		using var stream = new MemoryStream(new byte[] { 1, 2, 3 });
		_mediator.Send(Arg.Any<AddAttachmentCommand>(), Arg.Any<CancellationToken>())
			.Returns(Result.Fail<AttachmentDto>("Issue not found"));

		// Act
		var result = await _sut.AddAttachmentAsync("invalid-issue", stream, "file.pdf", "application/pdf", 1024, CreateTestUserDto());

		// Assert
		result.Success.Should().BeFalse();
		result.Error.Should().Contain("not found");
	}

	[Fact]
	public async Task AddAttachmentAsync_WhenExceptionThrown_ReturnsFailureAndLogs()
	{
		// Arrange
		using var stream = new MemoryStream(new byte[] { 1, 2, 3 });
		_mediator.When(x => x.Send(Arg.Any<AddAttachmentCommand>(), Arg.Any<CancellationToken>()))
			.Do(_ => throw new IOException("Storage unavailable"));

		// Act
		var result = await _sut.AddAttachmentAsync("issue-123", stream, "file.pdf", "application/pdf", 1024, CreateTestUserDto());

		// Assert
		result.Success.Should().BeFalse();
		result.Error.Should().Contain("Failed to add attachment");
	}

	[Fact]
	public async Task AddAttachmentAsync_WithDifferentContentTypes_Works()
	{
		// Arrange
		using var stream = new MemoryStream(new byte[] { 1, 2, 3 });
		var createdAttachment = CreateTestAttachmentDto("image.png");
		_mediator.Send(Arg.Any<AddAttachmentCommand>(), Arg.Any<CancellationToken>())
			.Returns(Result.Ok(createdAttachment));

		// Act
		var result = await _sut.AddAttachmentAsync("issue-123", stream, "image.png", "image/png", 5000, CreateTestUserDto());

		// Assert
		result.Success.Should().BeTrue();
	}

	#endregion

	#region DeleteAttachmentAsync Tests

	[Fact]
	public async Task DeleteAttachmentAsync_WithValidId_ReturnsSuccess()
	{
		// Arrange
		_mediator.Send(Arg.Any<DeleteAttachmentCommand>(), Arg.Any<CancellationToken>())
			.Returns(Result.Ok(true));

		// Act
		var result = await _sut.DeleteAttachmentAsync("attachment-123", "user1", false);

		// Assert
		result.Success.Should().BeTrue();
		result.Value.Should().BeTrue();
	}

	[Fact]
	public async Task DeleteAttachmentAsync_WhenNotFound_ReturnsFailure()
	{
		// Arrange
		_mediator.Send(Arg.Any<DeleteAttachmentCommand>(), Arg.Any<CancellationToken>())
			.Returns(Result.Fail<bool>("Attachment not found"));

		// Act
		var result = await _sut.DeleteAttachmentAsync("invalid-id", "user1", false);

		// Assert
		result.Success.Should().BeFalse();
		result.Error.Should().Contain("not found");
	}

	[Fact]
	public async Task DeleteAttachmentAsync_WhenUnauthorized_ReturnsFailure()
	{
		// Arrange
		_mediator.Send(Arg.Any<DeleteAttachmentCommand>(), Arg.Any<CancellationToken>())
			.Returns(Result.Fail<bool>("Unauthorized: Only the uploader or admin can delete this attachment"));

		// Act
		var result = await _sut.DeleteAttachmentAsync("attachment-123", "different-user", false);

		// Assert
		result.Success.Should().BeFalse();
		result.Error.Should().Contain("Unauthorized");
	}

	[Fact]
	public async Task DeleteAttachmentAsync_AsAdmin_SendsCorrectCommand()
	{
		// Arrange
		_mediator.Send(Arg.Any<DeleteAttachmentCommand>(), Arg.Any<CancellationToken>())
			.Returns(Result.Ok(true));

		// Act
		await _sut.DeleteAttachmentAsync("attachment-123", "admin-user", true);

		// Assert
		await _mediator.Received(1).Send(
			Arg.Is<DeleteAttachmentCommand>(c =>
				c.AttachmentId == "attachment-123" &&
				c.UserId == "admin-user" &&
				c.IsAdmin == true),
			Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task DeleteAttachmentAsync_AsOwner_SendsCorrectCommand()
	{
		// Arrange
		_mediator.Send(Arg.Any<DeleteAttachmentCommand>(), Arg.Any<CancellationToken>())
			.Returns(Result.Ok(true));

		// Act
		await _sut.DeleteAttachmentAsync("attachment-123", "user1", false);

		// Assert
		await _mediator.Received(1).Send(
			Arg.Is<DeleteAttachmentCommand>(c =>
				c.AttachmentId == "attachment-123" &&
				c.UserId == "user1" &&
				c.IsAdmin == false),
			Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task DeleteAttachmentAsync_WhenExceptionThrown_ReturnsFailureAndLogs()
	{
		// Arrange
		_mediator.When(x => x.Send(Arg.Any<DeleteAttachmentCommand>(), Arg.Any<CancellationToken>()))
			.Do(_ => throw new InvalidOperationException("Storage error"));

		// Act
		var result = await _sut.DeleteAttachmentAsync("attachment-123", "user1", false);

		// Assert
		result.Success.Should().BeFalse();
		result.Error.Should().Contain("Failed to delete attachment");
	}

	#endregion

	#region Helper Methods

	private static AttachmentDto CreateTestAttachmentDto(string fileName)
	{
		return new AttachmentDto(
			ObjectId.GenerateNewId().ToString(),
			ObjectId.GenerateNewId().ToString(),
			fileName,
			GetContentType(fileName),
			1024,
			$"https://storage.example.com/{fileName}",
			null,
			CreateTestUserDto(),
			DateTime.UtcNow);
	}

	private static string GetContentType(string fileName)
	{
		var ext = Path.GetExtension(fileName).ToLowerInvariant();
		return ext switch
		{
			".pdf" => "application/pdf",
			".png" => "image/png",
			".jpg" or ".jpeg" => "image/jpeg",
			".gif" => "image/gif",
			".zip" => "application/zip",
			_ => "application/octet-stream"
		};
	}

	private static UserDto CreateTestUserDto()
	{
		return new UserDto("user1", "Test User", "test@example.com");
	}

	#endregion
}
