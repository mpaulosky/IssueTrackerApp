// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     NotificationServiceTests.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTrackerApp
// Project Name :  Web.Tests
// =======================================================

using Microsoft.AspNetCore.SignalR;
using Web.Hubs;
using Web.Services;

namespace Web.Tests.Services;

/// <summary>
///   Unit tests for NotificationService SignalR operations.
///   Tests real-time notification delivery to connected clients.
/// </summary>
public sealed class NotificationServiceTests
{
	private readonly IHubContext<IssueHub> _hubContext;
	private readonly IHubClients _hubClients;
	private readonly IClientProxy _allGroupProxy;
	private readonly IClientProxy _issueGroupProxy;
	private readonly ILogger<NotificationService> _logger;
	private readonly NotificationService _sut;

	public NotificationServiceTests()
	{
		_hubContext = Substitute.For<IHubContext<IssueHub>>();
		_hubClients = Substitute.For<IHubClients>();
		_allGroupProxy = Substitute.For<IClientProxy>();
		_issueGroupProxy = Substitute.For<IClientProxy>();
		_logger = Substitute.For<ILogger<NotificationService>>();

		_hubContext.Clients.Returns(_hubClients);
		_hubClients.Group("all").Returns(_allGroupProxy);

		_sut = new NotificationService(_hubContext, _logger);
	}

	#region NotifyIssueCreatedAsync Tests

	[Fact]
	public async Task NotifyIssueCreatedAsync_WithValidIssue_SendsToAllGroup()
	{
		// Arrange
		var issue = CreateTestIssueDto("New Bug Report");

		// Act
		await _sut.NotifyIssueCreatedAsync(issue, CancellationToken.None);

		// Assert
		await _allGroupProxy.Received(1).SendCoreAsync(
			"IssueCreated",
			Arg.Is<object?[]>(args => args.Length == 1),
			Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task NotifyIssueCreatedAsync_WithValidIssue_LogsInformation()
	{
		// Arrange
		var issue = CreateTestIssueDto("New Bug Report");

		// Act
		await _sut.NotifyIssueCreatedAsync(issue, CancellationToken.None);

		// Assert
		_logger.Received(1).Log(
			LogLevel.Information,
			Arg.Any<EventId>(),
			Arg.Is<object>(o => o.ToString()!.Contains("new issue created")),
			Arg.Any<Exception?>(),
			Arg.Any<Func<object, Exception?, string>>());
	}

	#endregion

	#region NotifyIssueUpdatedAsync Tests

	[Fact]
	public async Task NotifyIssueUpdatedAsync_WithValidIssue_SendsToIssueSpecificGroup()
	{
		// Arrange
		var issueId = ObjectId.GenerateNewId();
		var issue = CreateTestIssueDtoWithId(issueId, "Updated Issue");
		_hubClients.Group($"issue-{issueId}").Returns(_issueGroupProxy);

		// Act
		await _sut.NotifyIssueUpdatedAsync(issue, CancellationToken.None);

		// Assert
		await _issueGroupProxy.Received(1).SendCoreAsync(
			"IssueUpdated",
			Arg.Is<object?[]>(args => args.Length == 1),
			Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task NotifyIssueUpdatedAsync_WithValidIssue_SendsToAllGroup()
	{
		// Arrange
		var issueId = ObjectId.GenerateNewId();
		var issue = CreateTestIssueDtoWithId(issueId, "Updated Issue");
		_hubClients.Group($"issue-{issueId}").Returns(_issueGroupProxy);

		// Act
		await _sut.NotifyIssueUpdatedAsync(issue, CancellationToken.None);

		// Assert
		await _allGroupProxy.Received(1).SendCoreAsync(
			"IssueUpdated",
			Arg.Is<object?[]>(args => args.Length == 1),
			Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task NotifyIssueUpdatedAsync_WithValidIssue_LogsInformation()
	{
		// Arrange
		var issueId = ObjectId.GenerateNewId();
		var issue = CreateTestIssueDtoWithId(issueId, "Updated Issue");
		_hubClients.Group($"issue-{issueId}").Returns(_issueGroupProxy);

		// Act
		await _sut.NotifyIssueUpdatedAsync(issue, CancellationToken.None);

		// Assert
		_logger.Received(1).Log(
			LogLevel.Information,
			Arg.Any<EventId>(),
			Arg.Is<object>(o => o.ToString()!.Contains("issue updated")),
			Arg.Any<Exception?>(),
			Arg.Any<Func<object, Exception?, string>>());
	}

	#endregion

	#region NotifyCommentAddedAsync Tests

	[Fact]
	public async Task NotifyCommentAddedAsync_WithValidData_SendsToIssueSpecificGroup()
	{
		// Arrange
		var issueId = ObjectId.GenerateNewId();
		var comment = CreateTestCommentDto("This is a test comment");
		_hubClients.Group($"issue-{issueId}").Returns(_issueGroupProxy);

		// Act
		await _sut.NotifyCommentAddedAsync(issueId, "Test Issue", "owner@test.com", comment, CancellationToken.None);

		// Assert
		await _issueGroupProxy.Received(1).SendCoreAsync(
			"CommentAdded",
			Arg.Is<object?[]>(args => args.Length == 1),
			Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task NotifyCommentAddedAsync_WithValidData_LogsInformation()
	{
		// Arrange
		var issueId = ObjectId.GenerateNewId();
		var comment = CreateTestCommentDto("This is a test comment");
		_hubClients.Group($"issue-{issueId}").Returns(_issueGroupProxy);

		// Act
		await _sut.NotifyCommentAddedAsync(issueId, "Test Issue", "owner@test.com", comment, CancellationToken.None);

		// Assert
		_logger.Received(1).Log(
			LogLevel.Information,
			Arg.Any<EventId>(),
			Arg.Is<object>(o => o.ToString()!.Contains("comment added")),
			Arg.Any<Exception?>(),
			Arg.Any<Func<object, Exception?, string>>());
	}

	#endregion

	#region NotifyIssueAssignedAsync Tests

	[Fact]
	public async Task NotifyIssueAssignedAsync_WithValidData_SendsToIssueSpecificGroup()
	{
		// Arrange
		var issueId = ObjectId.GenerateNewId();
		_hubClients.Group($"issue-{issueId}").Returns(_issueGroupProxy);

		// Act
		await _sut.NotifyIssueAssignedAsync(issueId, "Test Issue", "assignee@test.com", CancellationToken.None);

		// Assert
		await _issueGroupProxy.Received(1).SendCoreAsync(
			"IssueAssigned",
			Arg.Is<object?[]>(args => args.Length == 1),
			Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task NotifyIssueAssignedAsync_WithValidData_SendsToAllGroup()
	{
		// Arrange
		var issueId = ObjectId.GenerateNewId();
		_hubClients.Group($"issue-{issueId}").Returns(_issueGroupProxy);

		// Act
		await _sut.NotifyIssueAssignedAsync(issueId, "Test Issue", "assignee@test.com", CancellationToken.None);

		// Assert
		await _allGroupProxy.Received(1).SendCoreAsync(
			"IssueAssigned",
			Arg.Is<object?[]>(args => args.Length == 1),
			Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task NotifyIssueAssignedAsync_WithValidData_LogsInformation()
	{
		// Arrange
		var issueId = ObjectId.GenerateNewId();
		_hubClients.Group($"issue-{issueId}").Returns(_issueGroupProxy);

		// Act
		await _sut.NotifyIssueAssignedAsync(issueId, "Test Issue", "assignee@test.com", CancellationToken.None);

		// Assert
		_logger.Received(1).Log(
			LogLevel.Information,
			Arg.Any<EventId>(),
			Arg.Is<object>(o => o.ToString()!.Contains("issue assigned")),
			Arg.Any<Exception?>(),
			Arg.Any<Func<object, Exception?, string>>());
	}

	#endregion

	#region Helper Methods

	private static IssueDto CreateTestIssueDto(string title)
	{
		return new IssueDto(
			ObjectId.GenerateNewId(),
			title,
			"Test Description",
			DateTime.UtcNow,
			null,
			CreateTestUserDto(),
			CreateTestCategoryDto(),
			CreateTestStatusDto(),
			false,
			UserDto.Empty,
			false,
			false,
			UserDto.Empty);
	}

	private static IssueDto CreateTestIssueDtoWithId(ObjectId id, string title)
	{
		return new IssueDto(
			id,
			title,
			"Test Description",
			DateTime.UtcNow,
			null,
			CreateTestUserDto(),
			CreateTestCategoryDto(),
			CreateTestStatusDto(),
			false,
			UserDto.Empty,
			false,
			false,
			UserDto.Empty);
	}

	private static CommentDto CreateTestCommentDto(string content)
	{
		return new CommentDto(
			ObjectId.GenerateNewId(),
			"Comment Title",
			content,
			DateTime.UtcNow,
			null,
			ObjectId.GenerateNewId(),
			CreateTestUserDto(),
			[],
			false,
			UserDto.Empty,
			false,
			UserDto.Empty);
	}

	private static CategoryDto CreateTestCategoryDto()
	{
		return new CategoryDto(
			ObjectId.GenerateNewId(),
			"Test Category",
			"Category Description",
			DateTime.UtcNow,
			null,
			false,
			UserDto.Empty);
	}

	private static StatusDto CreateTestStatusDto()
	{
		return new StatusDto(
			ObjectId.GenerateNewId(),
			"Open",
			"Open status",
			DateTime.UtcNow,
			null,
			false,
			UserDto.Empty);
	}

	private static UserDto CreateTestUserDto()
	{
		return new UserDto("user1", "Test User", "test@example.com");
	}

	#endregion
}
