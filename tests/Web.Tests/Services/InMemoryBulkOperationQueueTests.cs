// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     InMemoryBulkOperationQueueTests.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTrackerApp
// Project Name :  Web.Tests
// =======================================================

using Microsoft.Extensions.Caching.Memory;
using Web.Services;

namespace Web.Tests.Services;

/// <summary>
/// Unit tests for <see cref="InMemoryBulkOperationQueue"/>.
/// Tests cover queue operations, status tracking, and concurrent access.
/// </summary>
public sealed class InMemoryBulkOperationQueueTests : IDisposable
{
	private readonly IMemoryCache _cache;
	private readonly ILogger<InMemoryBulkOperationQueue> _logger;
	private readonly InMemoryBulkOperationQueue _sut;

	public InMemoryBulkOperationQueueTests()
	{
		_cache = new MemoryCache(new MemoryCacheOptions());
		_logger = Substitute.For<ILogger<InMemoryBulkOperationQueue>>();
		_sut = new InMemoryBulkOperationQueue(_cache, _logger);
	}

	public void Dispose()
	{
		_cache.Dispose();
	}

	#region Queue Operations

	[Fact]
	public async Task QueueAsync_ReturnsOperationId()
	{
		// Arrange
		var command = new BulkUpdateStatusCommand(
			["issue-1", "issue-2"],
			new StatusDto(ObjectId.GenerateNewId(), "Closed", "", DateTime.UtcNow, null, false, UserDto.Empty),
			"user-1");

		// Act
		var operationId = await _sut.QueueAsync(command);

		// Assert
		operationId.Should().NotBeNullOrEmpty();
		operationId.Should().HaveLength(32); // GUID without hyphens
	}

	[Fact]
	public async Task QueueAsync_SetsInitialStatusToQueued()
	{
		// Arrange
		var command = new BulkUpdateStatusCommand(
			["issue-1"],
			new StatusDto(ObjectId.GenerateNewId(), "Open", "", DateTime.UtcNow, null, false, UserDto.Empty),
			"user-1");

		// Act
		var operationId = await _sut.QueueAsync(command);
		var status = await _sut.GetStatusAsync(operationId);

		// Assert
		status.Should().Be(BulkOperationStatus.Queued);
	}

	[Fact]
	public async Task QueueAsync_MultipleTimes_GeneratesUniqueIds()
	{
		// Arrange
		var command1 = new BulkUpdateStatusCommand(["issue-1"], CreateTestStatus(), "user-1");
		var command2 = new BulkUpdateStatusCommand(["issue-2"], CreateTestStatus(), "user-1");
		var command3 = new BulkUpdateStatusCommand(["issue-3"], CreateTestStatus(), "user-1");

		// Act
		var id1 = await _sut.QueueAsync(command1);
		var id2 = await _sut.QueueAsync(command2);
		var id3 = await _sut.QueueAsync(command3);

		// Assert
		id1.Should().NotBe(id2);
		id2.Should().NotBe(id3);
		id1.Should().NotBe(id3);
	}

	[Fact]
	public async Task DequeueAsync_ReturnsQueuedOperation()
	{
		// Arrange
		var command = new BulkUpdateStatusCommand(
			["issue-1", "issue-2"],
			CreateTestStatus(),
			"user-1");
		await _sut.QueueAsync(command);

		// Act
		var operation = await _sut.DequeueAsync();

		// Assert
		operation.Should().NotBeNull();
		operation!.Command.Should().BeEquivalentTo(command);
		operation.CommandType.Should().Be("BulkUpdateStatusCommand");
	}

	[Fact]
	public async Task DequeueAsync_ReturnsOperationsInOrder_FIFO()
	{
		// Arrange
		var command1 = new BulkUpdateStatusCommand(["issue-1"], CreateTestStatus(), "user-1");
		var command2 = new BulkUpdateStatusCommand(["issue-2"], CreateTestStatus(), "user-2");
		var command3 = new BulkUpdateStatusCommand(["issue-3"], CreateTestStatus(), "user-3");

		await _sut.QueueAsync(command1);
		await _sut.QueueAsync(command2);
		await _sut.QueueAsync(command3);

		// Act
		var op1 = await _sut.DequeueAsync();
		var op2 = await _sut.DequeueAsync();
		var op3 = await _sut.DequeueAsync();

		// Assert
		((BulkUpdateStatusCommand)op1!.Command).RequestedBy.Should().Be("user-1");
		((BulkUpdateStatusCommand)op2!.Command).RequestedBy.Should().Be("user-2");
		((BulkUpdateStatusCommand)op3!.Command).RequestedBy.Should().Be("user-3");
	}

	[Fact]
	public async Task DequeueAsync_WithCancellation_ReturnsNull()
	{
		// Arrange
		var cts = new CancellationTokenSource();
		cts.Cancel();

		// Act
		var operation = await _sut.DequeueAsync(cts.Token);

		// Assert
		operation.Should().BeNull();
	}

	#endregion

	#region Status Tracking

	[Fact]
	public async Task UpdateStatusAsync_UpdatesStatus()
	{
		// Arrange
		var command = new BulkUpdateStatusCommand(["issue-1"], CreateTestStatus(), "user-1");
		var operationId = await _sut.QueueAsync(command);

		// Act
		await _sut.UpdateStatusAsync(operationId, BulkOperationStatus.Processing);
		var status = await _sut.GetStatusAsync(operationId);

		// Assert
		status.Should().Be(BulkOperationStatus.Processing);
	}

	[Fact]
	public async Task UpdateStatusAsync_ToCompleted_StoresResult()
	{
		// Arrange
		var command = new BulkUpdateStatusCommand(["issue-1", "issue-2"], CreateTestStatus(), "user-1");
		var operationId = await _sut.QueueAsync(command);
		var result = BulkOperationResult.Success(2, "undo-token");

		// Act
		await _sut.UpdateStatusAsync(operationId, BulkOperationStatus.Completed, result);
		var status = await _sut.GetStatusAsync(operationId);
		var storedResult = _sut.GetResult(operationId);

		// Assert
		status.Should().Be(BulkOperationStatus.Completed);
		storedResult.Should().NotBeNull();
		storedResult!.SuccessCount.Should().Be(2);
	}

	[Fact]
	public async Task UpdateStatusAsync_ToFailed_TracksFailure()
	{
		// Arrange
		var command = new BulkUpdateStatusCommand(["issue-1"], CreateTestStatus(), "user-1");
		var operationId = await _sut.QueueAsync(command);
		var errors = new List<BulkOperationError> { new("issue-1", "Database error") };
		var result = new BulkOperationResult(1, 0, 1, errors);

		// Act
		await _sut.UpdateStatusAsync(operationId, BulkOperationStatus.Failed, result);
		var status = await _sut.GetStatusAsync(operationId);

		// Assert
		status.Should().Be(BulkOperationStatus.Failed);
	}

	[Fact]
	public async Task GetStatusAsync_ForNonExistentOperation_ReturnsNull()
	{
		// Act
		var status = await _sut.GetStatusAsync("non-existent-id");

		// Assert
		status.Should().BeNull();
	}

	[Fact]
	public void GetResult_ForNonExistentOperation_ReturnsNull()
	{
		// Act
		var result = _sut.GetResult("non-existent-id");

		// Assert
		result.Should().BeNull();
	}

	#endregion

	#region Concurrent Access

	[Fact]
	public async Task Queue_ConcurrentOperations_AllGetProcessed()
	{
		// Arrange
		var tasks = new List<Task<string>>();
		for (var i = 0; i < 10; i++)
		{
			var command = new BulkUpdateStatusCommand([$"issue-{i}"], CreateTestStatus(), $"user-{i}");
			tasks.Add(_sut.QueueAsync(command));
		}

		// Act
		var operationIds = await Task.WhenAll(tasks);

		// Assert
		operationIds.Should().HaveCount(10);
		operationIds.Should().OnlyHaveUniqueItems();
	}

	[Fact]
	public async Task StatusUpdates_ConcurrentUpdates_AllSucceed()
	{
		// Arrange
		var operationIds = new List<string>();
		for (var i = 0; i < 5; i++)
		{
			var command = new BulkUpdateStatusCommand([$"issue-{i}"], CreateTestStatus(), $"user-{i}");
			operationIds.Add(await _sut.QueueAsync(command));
		}

		// Act - Concurrently update all statuses to Processing
		var updateTasks = operationIds.Select(id =>
			_sut.UpdateStatusAsync(id, BulkOperationStatus.Processing));
		await Task.WhenAll(updateTasks);

		// Assert
		foreach (var id in operationIds)
		{
			var status = await _sut.GetStatusAsync(id);
			status.Should().Be(BulkOperationStatus.Processing);
		}
	}

	#endregion

	#region Channel Reader

	[Fact]
	public void Reader_ReturnsChannelReader()
	{
		// Act
		var reader = _sut.Reader;

		// Assert
		reader.Should().NotBeNull();
	}

	[Fact]
	public async Task Reader_CanReadQueuedOperations()
	{
		// Arrange
		var command = new BulkUpdateStatusCommand(["issue-1"], CreateTestStatus(), "user-1");
		await _sut.QueueAsync(command);

		// Act
		var operation = await _sut.Reader.ReadAsync();

		// Assert
		operation.Should().NotBeNull();
		operation.CommandType.Should().Be("BulkUpdateStatusCommand");
	}

	#endregion

	#region Different Command Types

	[Fact]
	public async Task QueueAsync_BulkAssignCommand_QueueSuccessfully()
	{
		// Arrange
		var command = new BulkAssignCommand(
			["issue-1", "issue-2"],
			new UserDto("user-456", "Jane", "jane@example.com"),
			"manager");

		// Act
		var operationId = await _sut.QueueAsync(command);
		var operation = await _sut.DequeueAsync();

		// Assert
		operationId.Should().NotBeNullOrEmpty();
		operation!.CommandType.Should().Be("BulkAssignCommand");
	}

	[Fact]
	public async Task QueueAsync_BulkDeleteCommand_QueuesSuccessfully()
	{
		// Arrange
		var command = new BulkDeleteCommand(
			["issue-1"],
			new UserDto("admin", "Admin", "admin@example.com"),
			"admin");

		// Act
		var operationId = await _sut.QueueAsync(command);
		var operation = await _sut.DequeueAsync();

		// Assert
		operationId.Should().NotBeNullOrEmpty();
		operation!.CommandType.Should().Be("BulkDeleteCommand");
	}

	[Fact]
	public async Task QueueAsync_BulkUpdateCategoryCommand_QueuesSuccessfully()
	{
		// Arrange
		var command = new BulkUpdateCategoryCommand(
			["issue-1", "issue-2"],
			new CategoryDto(ObjectId.GenerateNewId(), "Bug", "", DateTime.UtcNow, null, false, UserDto.Empty),
			"user-1");

		// Act
		var operationId = await _sut.QueueAsync(command);
		var operation = await _sut.DequeueAsync();

		// Assert
		operationId.Should().NotBeNullOrEmpty();
		operation!.CommandType.Should().Be("BulkUpdateCategoryCommand");
	}

	#endregion

	#region Helper Methods

	private static StatusDto CreateTestStatus()
	{
		return new StatusDto(
			ObjectId.GenerateNewId(),
			"Closed",
			"Closed status",
			DateTime.UtcNow,
			null,
			false,
			UserDto.Empty);
	}

	#endregion

	#region Edge Cases and Additional Coverage

	[Fact]
	public async Task QueueAsync_PreservesQueuedTimestamp()
	{
		// Arrange
		var beforeQueue = DateTime.UtcNow;
		var command = new BulkUpdateStatusCommand(["issue-1"], CreateTestStatus(), "user-1");

		// Act
		await _sut.QueueAsync(command);
		var afterQueue = DateTime.UtcNow;
		var operation = await _sut.DequeueAsync();

		// Assert
		operation!.QueuedAt.Should().BeOnOrAfter(beforeQueue);
		operation.QueuedAt.Should().BeOnOrBefore(afterQueue);
	}

	[Fact]
	public async Task QueueAsync_WithEmptyIssueIds_QueuedSuccessfully()
	{
		// Arrange
		var command = new BulkUpdateStatusCommand([], CreateTestStatus(), "user-1");

		// Act
		var operationId = await _sut.QueueAsync(command);
		var status = await _sut.GetStatusAsync(operationId);

		// Assert
		operationId.Should().NotBeNullOrEmpty();
		status.Should().Be(BulkOperationStatus.Queued);
	}

	[Fact]
	public async Task UpdateStatusAsync_MultipleTimesForSameOperation_UpdatesCorrectly()
	{
		// Arrange
		var command = new BulkUpdateStatusCommand(["issue-1"], CreateTestStatus(), "user-1");
		var operationId = await _sut.QueueAsync(command);

		// Act
		await _sut.UpdateStatusAsync(operationId, BulkOperationStatus.Processing);
		var status1 = await _sut.GetStatusAsync(operationId);

		await _sut.UpdateStatusAsync(operationId, BulkOperationStatus.Completed);
		var status2 = await _sut.GetStatusAsync(operationId);

		// Assert
		status1.Should().Be(BulkOperationStatus.Processing);
		status2.Should().Be(BulkOperationStatus.Completed);
	}

	[Fact]
	public async Task UpdateStatusAsync_WithNullResult_DoesNotStoreResult()
	{
		// Arrange
		var command = new BulkUpdateStatusCommand(["issue-1"], CreateTestStatus(), "user-1");
		var operationId = await _sut.QueueAsync(command);

		// Act
		await _sut.UpdateStatusAsync(operationId, BulkOperationStatus.Processing, null);
		var result = _sut.GetResult(operationId);

		// Assert
		result.Should().BeNull();
	}

	[Fact]
	public async Task UpdateStatusAsync_ReplacesExistingResult()
	{
		// Arrange
		var command = new BulkUpdateStatusCommand(["issue-1", "issue-2"], CreateTestStatus(), "user-1");
		var operationId = await _sut.QueueAsync(command);
		var firstResult = BulkOperationResult.Success(1, "undo-1");
		var secondResult = BulkOperationResult.Success(2, "undo-2");

		// Act
		await _sut.UpdateStatusAsync(operationId, BulkOperationStatus.Processing, firstResult);
		await _sut.UpdateStatusAsync(operationId, BulkOperationStatus.Completed, secondResult);
		var storedResult = _sut.GetResult(operationId);

		// Assert
		storedResult!.SuccessCount.Should().Be(2);
		storedResult.UndoToken.Should().Be("undo-2");
	}

	[Fact]
	public async Task GetResult_AfterPartialFailure_ReturnsErrorDetails()
	{
		// Arrange
		var command = new BulkUpdateStatusCommand(["issue-1", "issue-2", "issue-3"], CreateTestStatus(), "user-1");
		var operationId = await _sut.QueueAsync(command);
		var errors = new List<BulkOperationError>
		{
			new("issue-2", "Not found"),
			new("issue-3", "Access denied")
		};
		var result = new BulkOperationResult(3, 1, 2, errors);

		// Act
		await _sut.UpdateStatusAsync(operationId, BulkOperationStatus.Failed, result);
		var storedResult = _sut.GetResult(operationId);

		// Assert
		storedResult.Should().NotBeNull();
		storedResult!.TotalRequested.Should().Be(3);
		storedResult.SuccessCount.Should().Be(1);
		storedResult.FailureCount.Should().Be(2);
		storedResult.Errors.Should().HaveCount(2);
	}

	[Fact]
	public async Task Reader_WaitForReadAsync_ReturnsTrue_WhenItemsAvailable()
	{
		// Arrange
		var command = new BulkUpdateStatusCommand(["issue-1"], CreateTestStatus(), "user-1");
		await _sut.QueueAsync(command);

		// Act
		var canRead = await _sut.Reader.WaitToReadAsync();

		// Assert
		canRead.Should().BeTrue();
	}

	[Fact]
	public async Task Reader_CanReadMultipleQueuedItems()
	{
		// Arrange
		var command1 = new BulkUpdateStatusCommand(["issue-1"], CreateTestStatus(), "user-1");
		var command2 = new BulkUpdateStatusCommand(["issue-2"], CreateTestStatus(), "user-2");

		// Act
		await _sut.QueueAsync(command1);
		await _sut.QueueAsync(command2);

		// Dequeue to verify both items exist
		var op1 = await _sut.Reader.ReadAsync();
		var op2 = await _sut.Reader.ReadAsync();

		// Assert
		op1.Should().NotBeNull();
		op2.Should().NotBeNull();
		op1.CommandType.Should().Be("BulkUpdateStatusCommand");
		op2.CommandType.Should().Be("BulkUpdateStatusCommand");
	}

	[Fact]
	public async Task DequeueAsync_AfterMultipleQueueAndDequeue_MaintainsOrder()
	{
		// Arrange
		var command1 = new BulkUpdateStatusCommand(["a"], CreateTestStatus(), "first");
		var command2 = new BulkUpdateStatusCommand(["b"], CreateTestStatus(), "second");

		await _sut.QueueAsync(command1);
		var op1 = await _sut.DequeueAsync();

		await _sut.QueueAsync(command2);
		var op2 = await _sut.DequeueAsync();

		// Assert
		((BulkUpdateStatusCommand)op1!.Command).RequestedBy.Should().Be("first");
		((BulkUpdateStatusCommand)op2!.Command).RequestedBy.Should().Be("second");
	}

	[Fact]
	public async Task QueueAsync_WithLargeIssueList_HandlesCorrectly()
	{
		// Arrange
		var issueIds = Enumerable.Range(1, 1000).Select(i => $"issue-{i}").ToList();
		var command = new BulkUpdateStatusCommand(issueIds, CreateTestStatus(), "bulk-user");

		// Act
		var operationId = await _sut.QueueAsync(command);
		var operation = await _sut.DequeueAsync();

		// Assert
		operationId.Should().NotBeNullOrEmpty();
		var dequeued = (BulkUpdateStatusCommand)operation!.Command;
		dequeued.IssueIds.Should().HaveCount(1000);
	}

	[Fact]
	public void GetResult_WithDifferentOperationIds_ReturnsCorrectResults()
	{
		// Arrange
		var result1 = BulkOperationResult.Success(5, "token-1");
		var result2 = BulkOperationResult.Success(10, "token-2");

		// Act
		_sut.UpdateStatusAsync("op-1", BulkOperationStatus.Completed, result1);
		_sut.UpdateStatusAsync("op-2", BulkOperationStatus.Completed, result2);

		// Assert
		var stored1 = _sut.GetResult("op-1");
		var stored2 = _sut.GetResult("op-2");

		stored1!.SuccessCount.Should().Be(5);
		stored2!.SuccessCount.Should().Be(10);
	}

	#endregion
}
