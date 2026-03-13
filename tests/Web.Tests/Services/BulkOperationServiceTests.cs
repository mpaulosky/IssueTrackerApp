// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     BulkOperationServiceTests.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTrackerApp
// Project Name :  Web.Tests
// =======================================================

using System.Text;
using Web.Services;

namespace Web.Tests.Services;

/// <summary>
/// Unit tests for <see cref="BulkOperationService"/>.
/// Tests cover batch processing, progress tracking, partial failures, 
/// undo operations, and concurrent operation handling.
/// </summary>
public sealed class BulkOperationServiceTests
{
	private readonly IMediator _mediator;
	private readonly IBulkOperationQueue _bulkQueue;
	private readonly ILogger<BulkOperationService> _logger;
	private readonly BulkOperationService _sut;

	public BulkOperationServiceTests()
	{
		_mediator = Substitute.For<IMediator>();
		_bulkQueue = Substitute.For<IBulkOperationQueue>();
		_logger = Substitute.For<ILogger<BulkOperationService>>();
		_sut = new BulkOperationService(_mediator, _bulkQueue, _logger);
	}

	#region Batch Processing with Queue Mechanism

	[Fact]
	public async Task BulkUpdateStatusAsync_SendsCommandToMediator()
	{
		// Arrange
		var issueIds = new List<string> { "issue-1", "issue-2", "issue-3" };
		var status = CreateTestStatus("Closed");
		var requestedBy = "user-123";

		var expectedResult = BulkOperationResult.Success(3, "undo-token");
		_mediator.Send(Arg.Any<BulkUpdateStatusCommand>(), Arg.Any<CancellationToken>())
			.Returns(Result.Ok(expectedResult));

		// Act
		var result = await _sut.BulkUpdateStatusAsync(issueIds, status, requestedBy);

		// Assert
		result.Success.Should().BeTrue();
		result.Value.Should().NotBeNull();
		result.Value!.SuccessCount.Should().Be(3);

		await _mediator.Received(1).Send(
			Arg.Is<BulkUpdateStatusCommand>(c =>
				c.IssueIds.Count == 3 &&
				c.NewStatus.StatusName == "Closed" &&
				c.RequestedBy == requestedBy),
			Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task BulkUpdateCategoryAsync_SendsCommandToMediator()
	{
		// Arrange
		var issueIds = new List<string> { "issue-1", "issue-2" };
		var category = CreateTestCategory("Bug");
		var requestedBy = "admin-user";

		var expectedResult = BulkOperationResult.Success(2, "undo-token");
		_mediator.Send(Arg.Any<BulkUpdateCategoryCommand>(), Arg.Any<CancellationToken>())
			.Returns(Result.Ok(expectedResult));

		// Act
		var result = await _sut.BulkUpdateCategoryAsync(issueIds, category, requestedBy);

		// Assert
		result.Success.Should().BeTrue();
		result.Value!.SuccessCount.Should().Be(2);

		await _mediator.Received(1).Send(
			Arg.Is<BulkUpdateCategoryCommand>(c =>
				c.IssueIds.Count == 2 &&
				c.NewCategory.CategoryName == "Bug"),
			Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task BulkAssignAsync_SendsCommandToMediator()
	{
		// Arrange
		var issueIds = new List<string> { "issue-1", "issue-2", "issue-3", "issue-4" };
		var assignee = new UserDto("user-456", "Jane Developer", "jane@example.com");
		var requestedBy = "manager-user";

		var expectedResult = BulkOperationResult.Success(4, "undo-token");
		_mediator.Send(Arg.Any<BulkAssignCommand>(), Arg.Any<CancellationToken>())
			.Returns(Result.Ok(expectedResult));

		// Act
		var result = await _sut.BulkAssignAsync(issueIds, assignee, requestedBy);

		// Assert
		result.Success.Should().BeTrue();
		result.Value!.SuccessCount.Should().Be(4);

		await _mediator.Received(1).Send(
			Arg.Is<BulkAssignCommand>(c =>
				c.IssueIds.Count == 4 &&
				c.Assignee.Id == "user-456"),
			Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task BulkDeleteAsync_SendsCommandToMediator()
	{
		// Arrange
		var issueIds = new List<string> { "issue-1", "issue-2" };
		var archivedBy = new UserDto("admin-user", "Admin", "admin@example.com");
		var requestedBy = "admin-user";

		var expectedResult = BulkOperationResult.Success(2, "undo-token");
		_mediator.Send(Arg.Any<BulkDeleteCommand>(), Arg.Any<CancellationToken>())
			.Returns(Result.Ok(expectedResult));

		// Act
		var result = await _sut.BulkDeleteAsync(issueIds, archivedBy, requestedBy);

		// Assert
		result.Success.Should().BeTrue();
		result.Value!.SuccessCount.Should().Be(2);

		await _mediator.Received(1).Send(
			Arg.Is<BulkDeleteCommand>(c =>
				c.IssueIds.Count == 2 &&
				c.DeletedBy.Id == "admin-user"),
			Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task BulkExportAsync_SendsCommandToMediator()
	{
		// Arrange
		var issueIds = new List<string> { "issue-1", "issue-2", "issue-3" };
		var requestedBy = "user-export";

		var exportResult = new BulkExportResult(
			Encoding.UTF8.GetBytes("csv-content-here"),
			"issues_export.csv",
			3,
			[]);
		_mediator.Send(Arg.Any<BulkExportCommand>(), Arg.Any<CancellationToken>())
			.Returns(Result.Ok(exportResult));

		// Act
		var result = await _sut.BulkExportAsync(issueIds, requestedBy);

		// Assert
		result.Success.Should().BeTrue();
		result.Value!.TotalExported.Should().Be(3);

		await _mediator.Received(1).Send(
			Arg.Is<BulkExportCommand>(c => c.IssueIds.Count == 3),
			Arg.Any<CancellationToken>());
	}

	#endregion

	#region Progress Tracking During Bulk Operations

	[Fact]
	public async Task BulkUpdateStatusAsync_ReportsProgress_WhenProgressProvided()
	{
		// Arrange
		var issueIds = new List<string> { "issue-1", "issue-2", "issue-3" };
		var status = CreateTestStatus("InProgress");
		var requestedBy = "user-123";

		var expectedResult = new BulkOperationResult(3, 3, 0, [], "undo-token");
		_mediator.Send(Arg.Any<BulkUpdateStatusCommand>(), Arg.Any<CancellationToken>())
			.Returns(Result.Ok(expectedResult));

		var progress = Substitute.For<IProgress<BulkOperationProgress>>();

		// Act
		var result = await _sut.BulkUpdateStatusAsync(issueIds, status, requestedBy, progress);

		// Assert
		result.Success.Should().BeTrue();
		progress.Received(1).Report(Arg.Is<BulkOperationProgress>(p =>
			p.TotalCount == 3 &&
			p.ProcessedCount == 3 &&
			p.SuccessCount == 3 &&
			p.FailureCount == 0));
	}

	[Fact]
	public async Task BulkUpdateCategoryAsync_ReportsProgress_WhenProgressProvided()
	{
		// Arrange
		var issueIds = new List<string> { "issue-1", "issue-2" };
		var category = CreateTestCategory("Feature");
		var requestedBy = "user-123";

		var expectedResult = new BulkOperationResult(2, 1, 1, [new("issue-2", "Not found")], null);
		_mediator.Send(Arg.Any<BulkUpdateCategoryCommand>(), Arg.Any<CancellationToken>())
			.Returns(Result.Ok(expectedResult));

		var progressReports = new List<BulkOperationProgress>();
		var progress = new Progress<BulkOperationProgress>(p => progressReports.Add(p));

		// Act
		var result = await _sut.BulkUpdateCategoryAsync(issueIds, category, requestedBy, progress);

		// Assert
		result.Success.Should().BeTrue();
		
		// Wait a bit for async progress reporting
		await Task.Delay(50);
		
		progressReports.Should().HaveCount(1);
		progressReports[0].SuccessCount.Should().Be(1);
		progressReports[0].FailureCount.Should().Be(1);
	}

	[Fact]
	public void BulkOperationProgress_CalculatesPercentageCorrectly()
	{
		// Arrange
		var progress = new BulkOperationProgress
		{
			TotalCount = 10,
			ProcessedCount = 5,
			SuccessCount = 4,
			FailureCount = 1
		};

		// Assert
		progress.Percentage.Should().Be(50.0);
	}

	[Fact]
	public void BulkOperationProgress_ReturnsZeroPercentage_WhenTotalIsZero()
	{
		// Arrange
		var progress = new BulkOperationProgress
		{
			TotalCount = 0,
			ProcessedCount = 0,
			SuccessCount = 0,
			FailureCount = 0
		};

		// Assert
		progress.Percentage.Should().Be(0);
	}

	[Fact]
	public async Task BulkAssignAsync_ReportsProgress_WhenProgressProvided()
	{
		// Arrange
		var issueIds = new List<string> { "issue-1", "issue-2", "issue-3", "issue-4" };
		var assignee = new UserDto("user-456", "Jane", "jane@example.com");
		var requestedBy = "manager";

		var expectedResult = new BulkOperationResult(4, 3, 1, [new("issue-4", "Permission denied")]);
		_mediator.Send(Arg.Any<BulkAssignCommand>(), Arg.Any<CancellationToken>())
			.Returns(Result.Ok(expectedResult));

		var progress = Substitute.For<IProgress<BulkOperationProgress>>();

		// Act
		var result = await _sut.BulkAssignAsync(issueIds, assignee, requestedBy, progress);

		// Assert
		progress.Received(1).Report(Arg.Is<BulkOperationProgress>(p =>
			p.TotalCount == 4 &&
			p.ProcessedCount == 4 &&
			p.SuccessCount == 3 &&
			p.FailureCount == 1));
	}

	[Fact]
	public async Task BulkDeleteAsync_ReportsProgress_WhenProgressProvided()
	{
		// Arrange
		var issueIds = new List<string> { "issue-1", "issue-2" };
		var archivedBy = new UserDto("admin", "Admin", "admin@example.com");
		var requestedBy = "admin";

		var expectedResult = new BulkOperationResult(2, 2, 0, [], "undo-token");
		_mediator.Send(Arg.Any<BulkDeleteCommand>(), Arg.Any<CancellationToken>())
			.Returns(Result.Ok(expectedResult));

		var progress = Substitute.For<IProgress<BulkOperationProgress>>();

		// Act
		var result = await _sut.BulkDeleteAsync(issueIds, archivedBy, requestedBy, progress);

		// Assert
		progress.Received(1).Report(Arg.Is<BulkOperationProgress>(p =>
			p.SuccessCount == 2 && p.FailureCount == 0));
	}

	#endregion

	#region Partial Failure Handling

	[Fact]
	public async Task BulkUpdateStatusAsync_ReturnsPartialSuccess_WhenSomeFail()
	{
		// Arrange
		var issueIds = new List<string> { "issue-1", "issue-2", "issue-3" };
		var status = CreateTestStatus("Closed");
		var requestedBy = "user-123";

		var errors = new List<BulkOperationError>
		{
			new("issue-2", "Issue not found"),
			new("issue-3", "Permission denied")
		};
		var expectedResult = new BulkOperationResult(3, 1, 2, errors, "undo-token");

		_mediator.Send(Arg.Any<BulkUpdateStatusCommand>(), Arg.Any<CancellationToken>())
			.Returns(Result.Ok(expectedResult));

		// Act
		var result = await _sut.BulkUpdateStatusAsync(issueIds, status, requestedBy);

		// Assert
		result.Success.Should().BeTrue();
		result.Value.Should().NotBeNull();
		result.Value!.SuccessCount.Should().Be(1);
		result.Value!.FailureCount.Should().Be(2);
		result.Value!.IsFullSuccess.Should().BeFalse();
		result.Value!.Errors.Should().HaveCount(2);
		result.Value!.Errors.Should().Contain(e => e.IssueId == "issue-2");
		result.Value!.Errors.Should().Contain(e => e.IssueId == "issue-3");
	}

	[Fact]
	public async Task BulkUpdateCategoryAsync_ReturnsAllFailures_WhenNoneSucceed()
	{
		// Arrange
		var issueIds = new List<string> { "issue-1", "issue-2" };
		var category = CreateTestCategory("Bug");
		var requestedBy = "user-123";

		var errors = new List<BulkOperationError>
		{
			new("issue-1", "Issue locked"),
			new("issue-2", "Issue archived")
		};
		var expectedResult = new BulkOperationResult(2, 0, 2, errors);

		_mediator.Send(Arg.Any<BulkUpdateCategoryCommand>(), Arg.Any<CancellationToken>())
			.Returns(Result.Ok(expectedResult));

		// Act
		var result = await _sut.BulkUpdateCategoryAsync(issueIds, category, requestedBy);

		// Assert
		result.Success.Should().BeTrue(); // Operation completed, just all items failed
		result.Value!.SuccessCount.Should().Be(0);
		result.Value!.FailureCount.Should().Be(2);
		result.Value!.IsFullSuccess.Should().BeFalse();
	}

	[Fact]
	public async Task BulkAssignAsync_CapturesErrorDetails_ForEachFailedItem()
	{
		// Arrange
		var issueIds = new List<string> { "issue-1", "issue-2", "issue-3" };
		var assignee = new UserDto("user-456", "Jane", "jane@example.com");
		var requestedBy = "manager";

		var errors = new List<BulkOperationError>
		{
			new("issue-1", "Issue is already closed"),
			new("issue-3", "User does not have permission to assign this issue")
		};
		var expectedResult = new BulkOperationResult(3, 1, 2, errors, "undo-token");

		_mediator.Send(Arg.Any<BulkAssignCommand>(), Arg.Any<CancellationToken>())
			.Returns(Result.Ok(expectedResult));

		// Act
		var result = await _sut.BulkAssignAsync(issueIds, assignee, requestedBy);

		// Assert
		result.Value!.Errors.Should().HaveCount(2);
		result.Value!.Errors.First().ErrorMessage.Should().Contain("closed");
	}

	[Fact]
	public async Task BulkDeleteAsync_HandlesPartialFailure_WithUndoToken()
	{
		// Arrange
		var issueIds = new List<string> { "issue-1", "issue-2", "issue-3" };
		var archivedBy = new UserDto("admin", "Admin", "admin@example.com");
		var requestedBy = "admin";

		var errors = new List<BulkOperationError>
		{
			new("issue-3", "Issue has pending comments")
		};
		var expectedResult = new BulkOperationResult(3, 2, 1, errors, "partial-undo-token");

		_mediator.Send(Arg.Any<BulkDeleteCommand>(), Arg.Any<CancellationToken>())
			.Returns(Result.Ok(expectedResult));

		// Act
		var result = await _sut.BulkDeleteAsync(issueIds, archivedBy, requestedBy);

		// Assert
		result.Success.Should().BeTrue();
		result.Value!.SuccessCount.Should().Be(2);
		result.Value!.FailureCount.Should().Be(1);
		result.Value!.UndoToken.Should().Be("partial-undo-token");
	}

	[Fact]
	public async Task BulkUpdateStatusAsync_WhenMediatorFails_ReturnsFailure()
	{
		// Arrange
		var issueIds = new List<string> { "issue-1" };
		var status = CreateTestStatus("Closed");
		var requestedBy = "user-123";

		_mediator.Send(Arg.Any<BulkUpdateStatusCommand>(), Arg.Any<CancellationToken>())
			.Returns(Result.Fail<BulkOperationResult>("Database connection failed"));

		// Act
		var result = await _sut.BulkUpdateStatusAsync(issueIds, status, requestedBy);

		// Assert
		result.Success.Should().BeFalse();
		result.Error.Should().Contain("Database connection failed");
	}

	#endregion

	#region Undo Operation State Management

	[Fact]
	public async Task UndoLastOperationAsync_SendsUndoCommand()
	{
		// Arrange
		var undoToken = "undo-token-123";
		var requestedBy = "user-123";

		var expectedResult = BulkOperationResult.Success(3);
		_mediator.Send(Arg.Any<UndoBulkOperationCommand>(), Arg.Any<CancellationToken>())
			.Returns(Result.Ok(expectedResult));

		// Act
		var result = await _sut.UndoLastOperationAsync(undoToken, requestedBy);

		// Assert
		result.Success.Should().BeTrue();
		result.Value!.SuccessCount.Should().Be(3);

		await _mediator.Received(1).Send(
			Arg.Is<UndoBulkOperationCommand>(c =>
				c.UndoToken == undoToken &&
				c.RequestedBy == requestedBy),
			Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task UndoLastOperationAsync_WhenTokenExpired_ReturnsFailure()
	{
		// Arrange
		var undoToken = "expired-token";
		var requestedBy = "user-123";

		_mediator.Send(Arg.Any<UndoBulkOperationCommand>(), Arg.Any<CancellationToken>())
			.Returns(Result.Fail<BulkOperationResult>("Undo token not found or expired"));

		// Act
		var result = await _sut.UndoLastOperationAsync(undoToken, requestedBy);

		// Assert
		result.Success.Should().BeFalse();
		result.Error.Should().Contain("expired");
	}

	[Fact]
	public async Task UndoLastOperationAsync_WhenWrongUser_ReturnsFailure()
	{
		// Arrange
		var undoToken = "valid-token";
		var requestedBy = "different-user";

		_mediator.Send(Arg.Any<UndoBulkOperationCommand>(), Arg.Any<CancellationToken>())
			.Returns(Result.Fail<BulkOperationResult>("Undo token not found or expired"));

		// Act
		var result = await _sut.UndoLastOperationAsync(undoToken, requestedBy);

		// Assert
		result.Success.Should().BeFalse();
	}

	[Fact]
	public async Task BulkOperation_ReturnsUndoToken_ForSuccessfulOperation()
	{
		// Arrange
		var issueIds = new List<string> { "issue-1", "issue-2" };
		var status = CreateTestStatus("InProgress");
		var requestedBy = "user-123";

		var expectedResult = new BulkOperationResult(2, 2, 0, [], "undo-abc123");
		_mediator.Send(Arg.Any<BulkUpdateStatusCommand>(), Arg.Any<CancellationToken>())
			.Returns(Result.Ok(expectedResult));

		// Act
		var result = await _sut.BulkUpdateStatusAsync(issueIds, status, requestedBy);

		// Assert
		result.Success.Should().BeTrue();
		result.Value!.UndoToken.Should().Be("undo-abc123");
		result.Value!.UndoToken.Should().NotBeNullOrEmpty();
	}

	#endregion

	#region Concurrent Bulk Operation Handling

	[Fact]
	public async Task GetOperationStatusAsync_ReturnsStatus_FromQueue()
	{
		// Arrange
		var operationId = "operation-123";
		_bulkQueue.GetStatusAsync(operationId, Arg.Any<CancellationToken>())
			.Returns(BulkOperationStatus.Processing);

		// Act
		var status = await _sut.GetOperationStatusAsync(operationId);

		// Assert
		status.Should().Be(BulkOperationStatus.Processing);
		await _bulkQueue.Received(1).GetStatusAsync(operationId, Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task GetOperationStatusAsync_ReturnsNull_WhenOperationNotFound()
	{
		// Arrange
		var operationId = "non-existent-operation";
		_bulkQueue.GetStatusAsync(operationId, Arg.Any<CancellationToken>())
			.Returns((BulkOperationStatus?)null);

		// Act
		var status = await _sut.GetOperationStatusAsync(operationId);

		// Assert
		status.Should().BeNull();
	}

	[Fact]
	public async Task GetOperationStatusAsync_ReturnsCompleted_WhenOperationDone()
	{
		// Arrange
		var operationId = "completed-operation";
		_bulkQueue.GetStatusAsync(operationId, Arg.Any<CancellationToken>())
			.Returns(BulkOperationStatus.Completed);

		// Act
		var status = await _sut.GetOperationStatusAsync(operationId);

		// Assert
		status.Should().Be(BulkOperationStatus.Completed);
	}

	[Fact]
	public async Task GetOperationStatusAsync_ReturnsFailed_WhenOperationFailed()
	{
		// Arrange
		var operationId = "failed-operation";
		_bulkQueue.GetStatusAsync(operationId, Arg.Any<CancellationToken>())
			.Returns(BulkOperationStatus.Failed);

		// Act
		var status = await _sut.GetOperationStatusAsync(operationId);

		// Assert
		status.Should().Be(BulkOperationStatus.Failed);
	}

	[Fact]
	public async Task GetOperationStatusAsync_ReturnsQueued_WhenOperationPending()
	{
		// Arrange
		var operationId = "queued-operation";
		_bulkQueue.GetStatusAsync(operationId, Arg.Any<CancellationToken>())
			.Returns(BulkOperationStatus.Queued);

		// Act
		var status = await _sut.GetOperationStatusAsync(operationId);

		// Assert
		status.Should().Be(BulkOperationStatus.Queued);
	}

	[Fact]
	public async Task ConcurrentBulkOperations_CanRunInParallel()
	{
		// Arrange
		var issueIds1 = new List<string> { "issue-1", "issue-2" };
		var issueIds2 = new List<string> { "issue-3", "issue-4" };
		var status = CreateTestStatus("Closed");
		var requestedBy = "user-123";

		var result1 = BulkOperationResult.Success(2, "token-1");
		var result2 = BulkOperationResult.Success(2, "token-2");

		_mediator.Send(Arg.Any<BulkUpdateStatusCommand>(), Arg.Any<CancellationToken>())
			.Returns(
				Result.Ok(result1),
				Result.Ok(result2));

		// Act - Run two bulk operations concurrently
		var task1 = _sut.BulkUpdateStatusAsync(issueIds1, status, requestedBy);
		var task2 = _sut.BulkUpdateStatusAsync(issueIds2, status, requestedBy);

		var results = await Task.WhenAll(task1, task2);

		// Assert
		results.Should().HaveCount(2);
		results.All(r => r.Success).Should().BeTrue();
		await _mediator.Received(2).Send(
			Arg.Any<BulkUpdateStatusCommand>(),
			Arg.Any<CancellationToken>());
	}

	#endregion

	#region Edge Cases - Empty Batches

	[Fact]
	public async Task BulkUpdateStatusAsync_WithEmptyList_SendsCommandWithEmptyList()
	{
		// Arrange
		var issueIds = new List<string>();
		var status = CreateTestStatus("Closed");
		var requestedBy = "user-123";

		var expectedResult = BulkOperationResult.Success(0);
		_mediator.Send(Arg.Any<BulkUpdateStatusCommand>(), Arg.Any<CancellationToken>())
			.Returns(Result.Ok(expectedResult));

		// Act
		var result = await _sut.BulkUpdateStatusAsync(issueIds, status, requestedBy);

		// Assert
		result.Success.Should().BeTrue();
		result.Value!.TotalRequested.Should().Be(0);
		result.Value!.SuccessCount.Should().Be(0);
	}

	[Fact]
	public async Task BulkUpdateCategoryAsync_WithEmptyList_HandlesGracefully()
	{
		// Arrange
		var issueIds = new List<string>();
		var category = CreateTestCategory("Bug");
		var requestedBy = "user-123";

		var expectedResult = BulkOperationResult.Success(0);
		_mediator.Send(Arg.Any<BulkUpdateCategoryCommand>(), Arg.Any<CancellationToken>())
			.Returns(Result.Ok(expectedResult));

		// Act
		var result = await _sut.BulkUpdateCategoryAsync(issueIds, category, requestedBy);

		// Assert
		result.Success.Should().BeTrue();
		result.Value!.IsFullSuccess.Should().BeTrue();
	}

	[Fact]
	public async Task BulkAssignAsync_WithEmptyList_ReturnsSuccessWithZeroCount()
	{
		// Arrange
		var issueIds = new List<string>();
		var assignee = new UserDto("user-456", "Jane", "jane@example.com");
		var requestedBy = "manager";

		var expectedResult = BulkOperationResult.Success(0);
		_mediator.Send(Arg.Any<BulkAssignCommand>(), Arg.Any<CancellationToken>())
			.Returns(Result.Ok(expectedResult));

		// Act
		var result = await _sut.BulkAssignAsync(issueIds, assignee, requestedBy);

		// Assert
		result.Success.Should().BeTrue();
		result.Value!.TotalRequested.Should().Be(0);
	}

	[Fact]
	public async Task BulkDeleteAsync_WithEmptyList_ReturnsSuccessWithZeroCount()
	{
		// Arrange
		var issueIds = new List<string>();
		var archivedBy = new UserDto("admin", "Admin", "admin@example.com");
		var requestedBy = "admin";

		var expectedResult = BulkOperationResult.Success(0);
		_mediator.Send(Arg.Any<BulkDeleteCommand>(), Arg.Any<CancellationToken>())
			.Returns(Result.Ok(expectedResult));

		// Act
		var result = await _sut.BulkDeleteAsync(issueIds, archivedBy, requestedBy);

		// Assert
		result.Success.Should().BeTrue();
		result.Value!.TotalRequested.Should().Be(0);
	}

	[Fact]
	public async Task BulkExportAsync_WithEmptyList_ReturnsEmptyExport()
	{
		// Arrange
		var issueIds = new List<string>();
		var requestedBy = "user-export";

		var exportResult = new BulkExportResult([], "empty_export.csv", 0, []);
		_mediator.Send(Arg.Any<BulkExportCommand>(), Arg.Any<CancellationToken>())
			.Returns(Result.Ok(exportResult));

		// Act
		var result = await _sut.BulkExportAsync(issueIds, requestedBy);

		// Assert
		result.Success.Should().BeTrue();
		result.Value!.TotalExported.Should().Be(0);
	}

	#endregion

	#region Edge Cases - Large Batches

	[Fact]
	public async Task BulkUpdateStatusAsync_WithLargeBatch_ProcessesAllItems()
	{
		// Arrange
		var issueIds = Enumerable.Range(1, 500).Select(i => $"issue-{i}").ToList();
		var status = CreateTestStatus("Closed");
		var requestedBy = "user-123";

		var expectedResult = BulkOperationResult.Queued(500, "operation-large-batch");
		_mediator.Send(Arg.Any<BulkUpdateStatusCommand>(), Arg.Any<CancellationToken>())
			.Returns(Result.Ok(expectedResult));

		// Act
		var result = await _sut.BulkUpdateStatusAsync(issueIds, status, requestedBy);

		// Assert
		result.Success.Should().BeTrue();
		result.Value!.TotalRequested.Should().Be(500);
	}

	[Fact]
	public async Task BulkUpdateStatusAsync_LargeBatch_ShouldNotExceedTimeout()
	{
		// Arrange
		var issueIds = Enumerable.Range(1, 100).Select(i => $"issue-{i}").ToList();
		var status = CreateTestStatus("InProgress");
		var requestedBy = "user-123";

		var expectedResult = BulkOperationResult.Success(100, "undo-large");
		_mediator.Send(Arg.Any<BulkUpdateStatusCommand>(), Arg.Any<CancellationToken>())
			.Returns(async _ =>
			{
				await Task.Delay(10); // Simulate some processing time
				return Result.Ok(expectedResult);
			});

		using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

		// Act
		var result = await _sut.BulkUpdateStatusAsync(issueIds, status, requestedBy, cancellationToken: cts.Token);

		// Assert
		result.Success.Should().BeTrue();
	}

	#endregion

	#region Cancellation Token Handling

	[Fact]
	public async Task BulkUpdateStatusAsync_RespectsCancellationToken()
	{
		// Arrange
		var issueIds = new List<string> { "issue-1", "issue-2" };
		var status = CreateTestStatus("Closed");
		var requestedBy = "user-123";
		var cts = new CancellationTokenSource();
		cts.Cancel();

		_mediator.Send(Arg.Any<BulkUpdateStatusCommand>(), Arg.Any<CancellationToken>())
			.Returns<Task<Result<BulkOperationResult>>>(Task.FromException<Result<BulkOperationResult>>(new OperationCanceledException()));

		// Act & Assert
		await Assert.ThrowsAsync<OperationCanceledException>(() =>
			_sut.BulkUpdateStatusAsync(issueIds, status, requestedBy, cancellationToken: cts.Token));
	}

	[Fact]
	public async Task GetOperationStatusAsync_RespectsCancellationToken()
	{
		// Arrange
		var operationId = "operation-123";
		var cts = new CancellationTokenSource();
		cts.Cancel();

		_bulkQueue.GetStatusAsync(operationId, Arg.Any<CancellationToken>())
			.Returns<Task<BulkOperationStatus?>>(Task.FromException<BulkOperationStatus?>(new OperationCanceledException()));

		// Act & Assert
		await Assert.ThrowsAsync<OperationCanceledException>(() =>
			_sut.GetOperationStatusAsync(operationId, cts.Token));
	}

	#endregion

	#region Helper Methods

	private static StatusDto CreateTestStatus(string statusName)
	{
		return new StatusDto(
			ObjectId.GenerateNewId(),
			statusName,
			$"{statusName} status description",
			DateTime.UtcNow,
			null,
			false,
			UserDto.Empty);
	}

	private static CategoryDto CreateTestCategory(string categoryName)
	{
		return new CategoryDto(
			ObjectId.GenerateNewId(),
			categoryName,
			$"{categoryName} category description",
			DateTime.UtcNow,
			null,
			false,
			UserDto.Empty);
	}

	#endregion
}
