// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     GetIssueAttachmentsQueryHandlerTests.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTrackerApp
// Project Name :  Domain.Tests
// =======================================================

namespace Domain.Tests.Features.Attachments;

/// <summary>
///   Unit tests for GetIssueAttachmentsQueryHandler.
/// </summary>
public sealed class GetIssueAttachmentsQueryHandlerTests
{
	private readonly IRepository<Attachment> _repository;
	private readonly ILogger<GetIssueAttachmentsQueryHandler> _logger;
	private readonly GetIssueAttachmentsQueryHandler _sut;

	public GetIssueAttachmentsQueryHandlerTests()
	{
		_repository = Substitute.For<IRepository<Attachment>>();
		_logger = Substitute.For<ILogger<GetIssueAttachmentsQueryHandler>>();
		_sut = new GetIssueAttachmentsQueryHandler(_repository, _logger);
	}

	[Fact]
	public async Task GetAttachments_ReturnsAttachmentsForIssue()
	{
		// Arrange
		var issueId = ObjectId.GenerateNewId();
		var uploader = new UserDto("user-123", "Test User", "test@example.com");

		var attachments = new List<Attachment>
		{
			new()
			{
				Id = ObjectId.GenerateNewId(),
				IssueId = issueId,
				FileName = "document1.pdf",
				ContentType = "application/pdf",
				FileSize = 1024,
				BlobUrl = "https://storage.example.com/attachments/document1.pdf",
				ThumbnailUrl = null,
				UploadedBy = uploader,
				UploadedAt = DateTime.UtcNow.AddMinutes(-30)
			},
			new()
			{
				Id = ObjectId.GenerateNewId(),
				IssueId = issueId,
				FileName = "image1.png",
				ContentType = "image/png",
				FileSize = 2048,
				BlobUrl = "https://storage.example.com/attachments/image1.png",
				ThumbnailUrl = "https://storage.example.com/attachments/image1-thumb.png",
				UploadedBy = uploader,
				UploadedAt = DateTime.UtcNow.AddMinutes(-15)
			},
			new()
			{
				Id = ObjectId.GenerateNewId(),
				IssueId = issueId,
				FileName = "report.pdf",
				ContentType = "application/pdf",
				FileSize = 4096,
				BlobUrl = "https://storage.example.com/attachments/report.pdf",
				ThumbnailUrl = null,
				UploadedBy = uploader,
				UploadedAt = DateTime.UtcNow
			}
		};

		var query = new GetIssueAttachmentsQuery(issueId.ToString());

		_repository.FindAsync(Arg.Any<Expression<Func<Attachment, bool>>>(), Arg.Any<CancellationToken>())
			.Returns(Result.Ok<IEnumerable<Attachment>>(attachments));

		// Act
		var result = await _sut.Handle(query, CancellationToken.None);

		// Assert
		result.Success.Should().BeTrue();
		result.Value.Should().NotBeNull();
		result.Value!.Should().HaveCount(3);
	}

	[Fact]
	public async Task GetAttachments_OrdersByUploadedAtDescending()
	{
		// Arrange
		var issueId = ObjectId.GenerateNewId();
		var uploader = new UserDto("user-123", "Test User", "test@example.com");

		var oldestDate = DateTime.UtcNow.AddHours(-3);
		var middleDate = DateTime.UtcNow.AddHours(-2);
		var newestDate = DateTime.UtcNow.AddHours(-1);

		var attachments = new List<Attachment>
		{
			new()
			{
				Id = ObjectId.GenerateNewId(),
				IssueId = issueId,
				FileName = "oldest.pdf",
				ContentType = "application/pdf",
				FileSize = 1024,
				BlobUrl = "https://storage.example.com/oldest.pdf",
				UploadedBy = uploader,
				UploadedAt = oldestDate
			},
			new()
			{
				Id = ObjectId.GenerateNewId(),
				IssueId = issueId,
				FileName = "newest.pdf",
				ContentType = "application/pdf",
				FileSize = 1024,
				BlobUrl = "https://storage.example.com/newest.pdf",
				UploadedBy = uploader,
				UploadedAt = newestDate
			},
			new()
			{
				Id = ObjectId.GenerateNewId(),
				IssueId = issueId,
				FileName = "middle.pdf",
				ContentType = "application/pdf",
				FileSize = 1024,
				BlobUrl = "https://storage.example.com/middle.pdf",
				UploadedBy = uploader,
				UploadedAt = middleDate
			}
		};

		var query = new GetIssueAttachmentsQuery(issueId.ToString());

		_repository.FindAsync(Arg.Any<Expression<Func<Attachment, bool>>>(), Arg.Any<CancellationToken>())
			.Returns(Result.Ok<IEnumerable<Attachment>>(attachments));

		// Act
		var result = await _sut.Handle(query, CancellationToken.None);

		// Assert
		result.Success.Should().BeTrue();
		result.Value.Should().NotBeNull();

		var resultList = result.Value!.ToList();
		resultList[0].FileName.Should().Be("newest.pdf");
		resultList[1].FileName.Should().Be("middle.pdf");
		resultList[2].FileName.Should().Be("oldest.pdf");
	}

	[Fact]
	public async Task GetAttachments_WhenNoAttachments_ReturnsEmptyList()
	{
		// Arrange
		var issueId = ObjectId.GenerateNewId();
		var query = new GetIssueAttachmentsQuery(issueId.ToString());

		_repository.FindAsync(Arg.Any<Expression<Func<Attachment, bool>>>(), Arg.Any<CancellationToken>())
			.Returns(Result.Ok<IEnumerable<Attachment>>([]));

		// Act
		var result = await _sut.Handle(query, CancellationToken.None);

		// Assert
		result.Success.Should().BeTrue();
		result.Value.Should().NotBeNull();
		result.Value!.Should().BeEmpty();
	}

	[Fact]
	public async Task GetAttachments_WhenRepositoryFails_ReturnsError()
	{
		// Arrange
		var issueId = ObjectId.GenerateNewId();
		var query = new GetIssueAttachmentsQuery(issueId.ToString());

		_repository.FindAsync(Arg.Any<Expression<Func<Attachment, bool>>>(), Arg.Any<CancellationToken>())
			.Returns(Result.Fail<IEnumerable<Attachment>>("Database connection failed"));

		// Act
		var result = await _sut.Handle(query, CancellationToken.None);

		// Assert
		result.Failure.Should().BeTrue();
		result.Error.Should().Contain("Database connection failed");
	}
}
