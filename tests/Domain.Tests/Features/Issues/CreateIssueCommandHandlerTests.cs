// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     CreateIssueCommandHandlerTests.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTrackerApp
// Project Name :  Domain.Tests
// =======================================================

using System.Linq.Expressions;

using Domain.Abstractions;
using Domain.Features.Issues.Commands;

using Microsoft.Extensions.Logging;

using MongoDB.Bson;

namespace Domain.Tests.Features.Issues;

/// <summary>
///   Unit tests for the <see cref="CreateIssueCommandHandler" /> class.
/// </summary>
public sealed class CreateIssueCommandHandlerTests
{
	private readonly IRepository<Issue> _issueRepository;
	private readonly IRepository<Status> _statusRepository;
	private readonly ILogger<CreateIssueCommandHandler> _logger;
	private readonly CreateIssueCommandHandler _handler;

	public CreateIssueCommandHandlerTests()
	{
		_issueRepository = Substitute.For<IRepository<Issue>>();
		_statusRepository = Substitute.For<IRepository<Status>>();
		_logger = Substitute.For<ILogger<CreateIssueCommandHandler>>();
		_handler = new CreateIssueCommandHandler(_issueRepository, _statusRepository, _logger);

		// Default: status lookup returns null (triggers fallback)
		_statusRepository.FirstOrDefaultAsync(Arg.Any<Expression<Func<Status, bool>>>(), Arg.Any<CancellationToken>())
			.Returns(Result.Ok<Status?>(null));
	}

	[Fact]
	public async Task CreateIssue_WithValidData_ReturnsSuccessResult()
	{
		// Arrange
		var category = new CategoryDto(
			ObjectId.GenerateNewId(),
			"Bug",
			"Bug category",
			DateTime.UtcNow,
			null,
			false,
			UserDto.Empty);

		var author = new UserDto("user-123", "John Doe", "john@example.com");

		var command = new CreateIssueCommand("Test Issue", "Test Description", category, author);

		_issueRepository.AddAsync(Arg.Any<Issue>(), Arg.Any<CancellationToken>())
			.Returns(callInfo =>
			{
				var issue = callInfo.Arg<Issue>();
				return Result.Ok(issue);
			});

		// Act
		var result = await _handler.Handle(command, CancellationToken.None);

		// Assert
		result.Success.Should().BeTrue();
		result.Value.Should().NotBeNull();
		result.Value!.Title.Should().Be("Test Issue");
		result.Value.Description.Should().Be("Test Description");
		result.Value.Author.Should().Be(author);
		result.Value.Category.Should().Be(category);

		await _issueRepository.Received(1).AddAsync(Arg.Any<Issue>(), Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task CreateIssue_WithNullTitle_StillProcessesCommand()
	{
		// Arrange - Testing that the handler accepts whatever is passed (validation happens elsewhere)
		var category = CategoryDto.Empty;
		var author = UserDto.Empty;

		var command = new CreateIssueCommand(null!, "Description", category, author);

		_issueRepository.AddAsync(Arg.Any<Issue>(), Arg.Any<CancellationToken>())
			.Returns(callInfo =>
			{
				var issue = callInfo.Arg<Issue>();
				return Result.Ok(issue);
			});

		// Act
		var result = await _handler.Handle(command, CancellationToken.None);

		// Assert
		result.Success.Should().BeTrue();
		result.Value.Should().NotBeNull();
		result.Value!.Title.Should().BeNull();

		await _issueRepository.Received(1).AddAsync(Arg.Any<Issue>(), Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task CreateIssue_ShouldSetCreatedAtTimestamp()
	{
		// Arrange
		var beforeTest = DateTime.UtcNow;

		var category = new CategoryDto(
			ObjectId.GenerateNewId(),
			"Feature",
			"Feature category",
			DateTime.UtcNow,
			null,
			false,
			UserDto.Empty);

		var author = new UserDto("user-456", "Jane Doe", "jane@example.com");

		var command = new CreateIssueCommand("Timestamped Issue", "Description", category, author);

		Issue? capturedIssue = null;
		_issueRepository.AddAsync(Arg.Any<Issue>(), Arg.Any<CancellationToken>())
			.Returns(callInfo =>
			{
				capturedIssue = callInfo.Arg<Issue>();
				return Result.Ok(capturedIssue);
			});

		// Act
		var result = await _handler.Handle(command, CancellationToken.None);

		var afterTest = DateTime.UtcNow;

		// Assert
		result.Success.Should().BeTrue();
		capturedIssue.Should().NotBeNull();
		capturedIssue!.DateCreated.Should().BeOnOrAfter(beforeTest);
		capturedIssue.DateCreated.Should().BeOnOrBefore(afterTest);
	}

	[Fact]
	public async Task CreateIssue_WhenRepositoryFails_ReturnsFailure()
	{
		// Arrange
		var category = CategoryDto.Empty;
		var author = UserDto.Empty;

		var command = new CreateIssueCommand("Test Issue", "Description", category, author);

		_issueRepository.AddAsync(Arg.Any<Issue>(), Arg.Any<CancellationToken>())
			.Returns(Result.Fail<Issue>("Database error", ResultErrorCode.Conflict));

		// Act
		var result = await _handler.Handle(command, CancellationToken.None);

		// Assert
		result.Failure.Should().BeTrue();
		result.Error.Should().Contain("error");
	}

	[Fact]
	public async Task CreateIssue_ShouldSetDefaultStatusToOpen()
	{
		// Arrange
		var category = CategoryDto.Empty;
		var author = new UserDto("user-789", "Test User", "test@example.com");

		var command = new CreateIssueCommand("Test Issue", "Description", category, author);

		Issue? capturedIssue = null;
		_issueRepository.AddAsync(Arg.Any<Issue>(), Arg.Any<CancellationToken>())
			.Returns(callInfo =>
			{
				capturedIssue = callInfo.Arg<Issue>();
				return Result.Ok(capturedIssue);
			});

		// Act
		var result = await _handler.Handle(command, CancellationToken.None);

		// Assert
		result.Success.Should().BeTrue();
		capturedIssue.Should().NotBeNull();
		capturedIssue!.Status.StatusName.Should().Be("Open");
	}

	[Fact]
	public async Task CreateIssue_ShouldSetArchivedToFalse()
	{
		// Arrange
		var category = CategoryDto.Empty;
		var author = UserDto.Empty;

		var command = new CreateIssueCommand("Test Issue", "Description", category, author);

		Issue? capturedIssue = null;
		_issueRepository.AddAsync(Arg.Any<Issue>(), Arg.Any<CancellationToken>())
			.Returns(callInfo =>
			{
				capturedIssue = callInfo.Arg<Issue>();
				return Result.Ok(capturedIssue);
			});

		// Act
		var result = await _handler.Handle(command, CancellationToken.None);

		// Assert
		result.Success.Should().BeTrue();
		capturedIssue.Should().NotBeNull();
		capturedIssue!.Archived.Should().BeFalse();
		capturedIssue.ApprovedForRelease.Should().BeFalse();
		capturedIssue.Rejected.Should().BeFalse();
	}

	[Fact]
	public async Task CreateIssue_WhenStatusExistsInDb_UsesDbStatus()
	{
		// Arrange
		var dbStatusId = ObjectId.GenerateNewId();
		var dbStatus = new Status
		{
			Id = dbStatusId,
			StatusName = "Open",
			StatusDescription = "Issue is open",
			DateCreated = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
			Archived = false,
			ArchivedBy = UserInfo.Empty
		};

		_statusRepository.FirstOrDefaultAsync(Arg.Any<Expression<Func<Status, bool>>>(), Arg.Any<CancellationToken>())
			.Returns(Result.Ok<Status?>(dbStatus));

		var command = new CreateIssueCommand("Test Issue", "Description", CategoryDto.Empty, UserDto.Empty);

		Issue? capturedIssue = null;
		_issueRepository.AddAsync(Arg.Any<Issue>(), Arg.Any<CancellationToken>())
			.Returns(callInfo =>
			{
				capturedIssue = callInfo.Arg<Issue>();
				return Result.Ok(capturedIssue);
			});

		// Act
		var result = await _handler.Handle(command, CancellationToken.None);

		// Assert
		result.Success.Should().BeTrue();
		capturedIssue.Should().NotBeNull();
		capturedIssue!.Status.Id.Should().Be(dbStatusId);
		capturedIssue.Status.StatusName.Should().Be("Open");
		capturedIssue.Status.StatusDescription.Should().Be("Issue is open");
	}

	[Fact]
	public async Task CreateIssue_WhenStatusLookupFails_UsesFallbackStatus()
	{
		// Arrange
		_statusRepository.FirstOrDefaultAsync(Arg.Any<Expression<Func<Status, bool>>>(), Arg.Any<CancellationToken>())
			.Returns(Result.Fail<Status?>("Database error"));

		var command = new CreateIssueCommand("Test Issue", "Description", CategoryDto.Empty, UserDto.Empty);

		Issue? capturedIssue = null;
		_issueRepository.AddAsync(Arg.Any<Issue>(), Arg.Any<CancellationToken>())
			.Returns(callInfo =>
			{
				capturedIssue = callInfo.Arg<Issue>();
				return Result.Ok(capturedIssue);
			});

		// Act
		var result = await _handler.Handle(command, CancellationToken.None);

		// Assert
		result.Success.Should().BeTrue();
		capturedIssue.Should().NotBeNull();
		capturedIssue!.Status.Id.Should().Be(ObjectId.Empty);
		capturedIssue.Status.StatusName.Should().Be("Open");
	}
}
