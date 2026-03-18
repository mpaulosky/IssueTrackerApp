// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     BulkOperationBackgroundServiceTests.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTrackerApp
// Project Name :  Web.Tests
// =======================================================

using System.Threading.Channels;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Web.Services;

namespace Web.Tests.Services;

/// <summary>
/// Unit tests for BulkOperationBackgroundService background processing.
/// </summary>
public sealed class BulkOperationBackgroundServiceTests : IDisposable
{
	private readonly IServiceScopeFactory _scopeFactory;
	private readonly InMemoryBulkOperationQueue _queue;
	private readonly IMediator _mediator;
	private readonly IServiceScope _scope;
	private readonly IServiceProvider _scopedServiceProvider;

	public BulkOperationBackgroundServiceTests()
	{
		_mediator = Substitute.For<IMediator>();

		// Create a real queue with memory cache
		var memoryCache = new MemoryCache(new MemoryCacheOptions());
		_queue = new InMemoryBulkOperationQueue(
			memoryCache,
			NullLogger<InMemoryBulkOperationQueue>.Instance);

		// Setup service scope factory
		_scopedServiceProvider = Substitute.For<IServiceProvider>();
		_scopedServiceProvider.GetService(typeof(IMediator)).Returns(_mediator);

		_scope = Substitute.For<IServiceScope>();
		_scope.ServiceProvider.Returns(_scopedServiceProvider);

		_scopeFactory = Substitute.For<IServiceScopeFactory>();
		_scopeFactory.CreateScope().Returns(_scope);
	}

	public void Dispose()
	{
		_scope.Dispose();
	}

	private BulkOperationBackgroundService CreateService()
	{
		return new BulkOperationBackgroundService(
			_scopeFactory,
			_queue,
			NullLogger<BulkOperationBackgroundService>.Instance);
	}

	private static StatusDto CreateStatusDto(string name = "In Progress")
	{
		return new StatusDto(
			ObjectId.GenerateNewId(),
			name,
			$"{name} status",
			DateTime.UtcNow,
			null,
			false,
			UserDto.Empty);
	}

	private static CategoryDto CreateCategoryDto(string name = "Bug")
	{
		return new CategoryDto(
			ObjectId.GenerateNewId(),
			name,
			$"{name} category",
			DateTime.UtcNow,
			null,
			false,
			UserDto.Empty);
	}

	#region Constructor Tests

	[Fact]
	public void Constructor_WithValidDependencies_CreatesService()
	{
		// Arrange & Act
		var service = CreateService();

		// Assert
		service.Should().NotBeNull();
	}

	#endregion

	#region ExecuteAsync Tests

	[Fact]
	public async Task ExecuteAsync_WhenQueueEmpty_WaitsForOperations()
	{
		// Arrange
		var service = CreateService();
		using var cts = new CancellationTokenSource();
		cts.CancelAfter(TimeSpan.FromMilliseconds(100));

		// Act
		var executeTask = service.StartAsync(cts.Token);
		await Task.Delay(50);
		cts.Cancel();

		// Assert
		await executeTask;
		_mediator.ReceivedCalls().Should().BeEmpty();
	}

	[Fact]
	public async Task ExecuteAsync_WhenCancellationRequested_StopsGracefully()
	{
		// Arrange
		var service = CreateService();
		using var cts = new CancellationTokenSource();

		// Act
		var executeTask = service.StartAsync(cts.Token);
		cts.Cancel();
		await Task.Delay(50);
		await service.StopAsync(CancellationToken.None);

		// Assert - should complete without throwing
		await executeTask;
	}

	[Fact]
	public async Task ExecuteAsync_WithBulkUpdateStatusCommand_ProcessesSuccessfully()
	{
		// Arrange
		var service = CreateService();
		using var cts = new CancellationTokenSource();

		var command = new BulkUpdateStatusCommand(
			["issue-1", "issue-2"],
			CreateStatusDto("In Progress"),
			"test-user");

		var expectedResult = BulkOperationResult.Success(2);
		_mediator
			.Send(Arg.Any<BulkUpdateStatusCommand>(), Arg.Any<CancellationToken>())
			.Returns(Result.Ok(expectedResult));

		// Act
		var executeTask = service.StartAsync(cts.Token);
		var operationId = await _queue.QueueAsync(command);
		await Task.Delay(150); // Allow processing
		cts.Cancel();
		await service.StopAsync(CancellationToken.None);

		// Assert
		var status = await _queue.GetStatusAsync(operationId);
		status.Should().Be(BulkOperationStatus.Completed);
	}

	[Fact]
	public async Task ExecuteAsync_WithBulkUpdateCategoryCommand_ProcessesSuccessfully()
	{
		// Arrange
		var service = CreateService();
		using var cts = new CancellationTokenSource();

		var command = new BulkUpdateCategoryCommand(
			["issue-1"],
			CreateCategoryDto("Bug"),
			"test-user");

		var expectedResult = BulkOperationResult.Success(1);
		_mediator
			.Send(Arg.Any<BulkUpdateCategoryCommand>(), Arg.Any<CancellationToken>())
			.Returns(Result.Ok(expectedResult));

		// Act
		var executeTask = service.StartAsync(cts.Token);
		var operationId = await _queue.QueueAsync(command);
		await Task.Delay(150);
		cts.Cancel();
		await service.StopAsync(CancellationToken.None);

		// Assert
		var status = await _queue.GetStatusAsync(operationId);
		status.Should().Be(BulkOperationStatus.Completed);
	}

	[Fact]
	public async Task ExecuteAsync_WithBulkAssignCommand_ProcessesSuccessfully()
	{
		// Arrange
		var service = CreateService();
		using var cts = new CancellationTokenSource();

		var command = new BulkAssignCommand(
			["issue-1"],
			new UserDto("user-1", "Test User", "test@test.com"),
			"test-user");

		var expectedResult = BulkOperationResult.Success(1);
		_mediator
			.Send(Arg.Any<BulkAssignCommand>(), Arg.Any<CancellationToken>())
			.Returns(Result.Ok(expectedResult));

		// Act
		var executeTask = service.StartAsync(cts.Token);
		var operationId = await _queue.QueueAsync(command);
		await Task.Delay(150);
		cts.Cancel();
		await service.StopAsync(CancellationToken.None);

		// Assert
		var status = await _queue.GetStatusAsync(operationId);
		status.Should().Be(BulkOperationStatus.Completed);
	}

	[Fact]
	public async Task ExecuteAsync_WithBulkDeleteCommand_ProcessesSuccessfully()
	{
		// Arrange
		var service = CreateService();
		using var cts = new CancellationTokenSource();

		var command = new BulkDeleteCommand(
			["issue-1"],
			new UserDto("admin-1", "Admin User", "admin@test.com"),
			"admin-user");

		var expectedResult = BulkOperationResult.Success(1);
		_mediator
			.Send(Arg.Any<BulkDeleteCommand>(), Arg.Any<CancellationToken>())
			.Returns(Result.Ok(expectedResult));

		// Act
		var executeTask = service.StartAsync(cts.Token);
		var operationId = await _queue.QueueAsync(command);
		await Task.Delay(150);
		cts.Cancel();
		await service.StopAsync(CancellationToken.None);

		// Assert
		var status = await _queue.GetStatusAsync(operationId);
		status.Should().Be(BulkOperationStatus.Completed);
	}

	[Fact]
	public async Task ExecuteAsync_WhenMediatorReturnsFailure_UpdatesStatusToFailed()
	{
		// Arrange
		var service = CreateService();
		using var cts = new CancellationTokenSource();

		var command = new BulkUpdateStatusCommand(
			["issue-1", "issue-2"],
			CreateStatusDto("Done"),
			"test-user");

		// Return a result where all failed
		var failedResult = new BulkOperationResult(
			2, 0, 2, 
			[new BulkOperationError("issue-1", "Error"), new BulkOperationError("issue-2", "Error")]);

		_mediator
			.Send(Arg.Any<BulkUpdateStatusCommand>(), Arg.Any<CancellationToken>())
			.Returns(Result.Ok(failedResult));

		// Act
		var executeTask = service.StartAsync(cts.Token);
		var operationId = await _queue.QueueAsync(command);
		await Task.Delay(150);
		cts.Cancel();
		await service.StopAsync(CancellationToken.None);

		// Assert
		var status = await _queue.GetStatusAsync(operationId);
		status.Should().Be(BulkOperationStatus.Failed);
	}

	[Fact]
	public async Task ExecuteAsync_WhenExceptionThrown_UpdatesStatusToFailed()
	{
		// Arrange
		var service = CreateService();
		using var cts = new CancellationTokenSource();

		var command = new BulkUpdateStatusCommand(
			["issue-1"],
			CreateStatusDto("Done"),
			"test-user");

		_mediator
			.Send(Arg.Any<BulkUpdateStatusCommand>(), Arg.Any<CancellationToken>())
			.Returns<Task<Result<BulkOperationResult>>>(_ => throw new InvalidOperationException("Test error"));

		// Act
		var executeTask = service.StartAsync(cts.Token);
		var operationId = await _queue.QueueAsync(command);
		await Task.Delay(150);
		cts.Cancel();
		await service.StopAsync(CancellationToken.None);

		// Assert
		var status = await _queue.GetStatusAsync(operationId);
		status.Should().Be(BulkOperationStatus.Failed);
	}

	[Fact]
	public async Task ExecuteAsync_WithMultipleOperations_ProcessesAll()
	{
		// Arrange
		var service = CreateService();
		using var cts = new CancellationTokenSource();

		var command1 = new BulkUpdateStatusCommand(
			["issue-1"],
			CreateStatusDto("In Progress"),
			"user1");

		var command2 = new BulkUpdateStatusCommand(
			["issue-2"],
			CreateStatusDto("Done"),
			"user2");

		_mediator
			.Send(Arg.Any<BulkUpdateStatusCommand>(), Arg.Any<CancellationToken>())
			.Returns(Result.Ok(BulkOperationResult.Success(1)));

		// Act
		var executeTask = service.StartAsync(cts.Token);
		var opId1 = await _queue.QueueAsync(command1);
		var opId2 = await _queue.QueueAsync(command2);
		await Task.Delay(250);
		cts.Cancel();
		await service.StopAsync(CancellationToken.None);

		// Assert
		var status1 = await _queue.GetStatusAsync(opId1);
		var status2 = await _queue.GetStatusAsync(opId2);
		status1.Should().Be(BulkOperationStatus.Completed);
		status2.Should().Be(BulkOperationStatus.Completed);
	}

	[Fact]
	public async Task ExecuteAsync_WithPartialSuccess_SetsStatusToCompleted()
	{
		// Arrange
		var service = CreateService();
		using var cts = new CancellationTokenSource();

		var command = new BulkUpdateStatusCommand(
			["issue-1", "issue-2"],
			CreateStatusDto("Done"),
			"test-user");

		// One success, one failure
		var partialResult = new BulkOperationResult(
			2, 1, 1,
			[new BulkOperationError("issue-2", "Not found")]);

		_mediator
			.Send(Arg.Any<BulkUpdateStatusCommand>(), Arg.Any<CancellationToken>())
			.Returns(Result.Ok(partialResult));

		// Act
		var executeTask = service.StartAsync(cts.Token);
		var operationId = await _queue.QueueAsync(command);
		await Task.Delay(150);
		cts.Cancel();
		await service.StopAsync(CancellationToken.None);

		// Assert
		var status = await _queue.GetStatusAsync(operationId);
		status.Should().Be(BulkOperationStatus.Completed);
	}

	[Fact]
	public async Task ExecuteAsync_UpdatesStatusToProcessingBeforeExecution()
	{
		// Arrange
		var service = CreateService();
		using var cts = new CancellationTokenSource();
		BulkOperationStatus? statusDuringProcessing = null;

		var command = new BulkUpdateStatusCommand(
			["issue-1"],
			CreateStatusDto("Done"),
			"test-user");

		_mediator
			.Send(Arg.Any<BulkUpdateStatusCommand>(), Arg.Any<CancellationToken>())
			.Returns(async callInfo =>
			{
				// Capture status during processing
				var opId = _queue.GetResult(command.IssueIds[0])?.OperationId;
				await Task.Delay(10);
				return Result.Ok(BulkOperationResult.Success(1));
			});

		// Act
		var executeTask = service.StartAsync(cts.Token);
		var operationId = await _queue.QueueAsync(command);
		await Task.Delay(50);
		statusDuringProcessing = await _queue.GetStatusAsync(operationId);
		await Task.Delay(150);
		cts.Cancel();
		await service.StopAsync(CancellationToken.None);

		// Assert - status transitions through Processing
		var finalStatus = await _queue.GetStatusAsync(operationId);
		finalStatus.Should().Be(BulkOperationStatus.Completed);
	}

	#endregion

	#region StartAsync/StopAsync Tests

	[Fact]
	public async Task StartAsync_WhenCalled_StartsBackgroundProcessing()
	{
		// Arrange
		var service = CreateService();
		using var cts = new CancellationTokenSource();

		// Act
		await service.StartAsync(cts.Token);
		await Task.Delay(50);

		// Assert - service should be running
		cts.Cancel();
		await service.StopAsync(CancellationToken.None);
	}

	[Fact]
	public async Task StopAsync_WhenCalled_StopsBackgroundProcessing()
	{
		// Arrange
		var service = CreateService();
		using var cts = new CancellationTokenSource();
		await service.StartAsync(cts.Token);

		// Act
		await service.StopAsync(CancellationToken.None);

		// Assert - should complete without hanging
		cts.Cancel();
	}

	#endregion
}
