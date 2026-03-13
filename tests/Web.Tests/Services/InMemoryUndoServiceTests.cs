// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     InMemoryUndoServiceTests.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTrackerApp
// Project Name :  Web.Tests
// =======================================================

using Microsoft.Extensions.Caching.Memory;
using Web.Services;

namespace Web.Tests.Services;

/// <summary>
/// Unit tests for <see cref="InMemoryUndoService"/>.
/// Tests cover undo data storage, retrieval, expiration, and user validation.
/// </summary>
public sealed class InMemoryUndoServiceTests : IDisposable
{
	private readonly IMemoryCache _cache;
	private readonly ILogger<InMemoryUndoService> _logger;
	private readonly InMemoryUndoService _sut;

	public InMemoryUndoServiceTests()
	{
		_cache = new MemoryCache(new MemoryCacheOptions());
		_logger = Substitute.For<ILogger<InMemoryUndoService>>();
		_sut = new InMemoryUndoService(_cache, _logger);
	}

	public void Dispose()
	{
		_cache.Dispose();
	}

	#region Store Undo Data

	[Fact]
	public async Task StoreUndoDataAsync_ReturnsToken()
	{
		// Arrange
		var requestedBy = "user-123";
		var snapshots = CreateTestSnapshots();

		// Act
		var token = await _sut.StoreUndoDataAsync(requestedBy, snapshots);

		// Assert
		token.Should().NotBeNullOrEmpty();
		token.Should().HaveLength(32); // GUID without hyphens
	}

	[Fact]
	public async Task StoreUndoDataAsync_MultipleStores_ReturnsUniqueTokens()
	{
		// Arrange
		var requestedBy = "user-123";
		var snapshots1 = CreateTestSnapshots();
		var snapshots2 = CreateTestSnapshots();

		// Act
		var token1 = await _sut.StoreUndoDataAsync(requestedBy, snapshots1);
		var token2 = await _sut.StoreUndoDataAsync(requestedBy, snapshots2);

		// Assert
		token1.Should().NotBe(token2);
	}

	[Fact]
	public async Task StoreUndoDataAsync_StoresAllSnapshots()
	{
		// Arrange
		var requestedBy = "user-123";
		var snapshots = new List<IssueUndoSnapshot>
		{
			new("issue-1", BulkOperationType.StatusUpdate, new StatusUpdateSnapshot(CreateTestStatus())),
			new("issue-2", BulkOperationType.StatusUpdate, new StatusUpdateSnapshot(CreateTestStatus())),
			new("issue-3", BulkOperationType.StatusUpdate, new StatusUpdateSnapshot(CreateTestStatus()))
		};

		// Act
		var token = await _sut.StoreUndoDataAsync(requestedBy, snapshots);
		var undoData = await _sut.GetUndoDataAsync(token, requestedBy);

		// Assert
		undoData.Should().NotBeNull();
		undoData!.Snapshots.Should().HaveCount(3);
	}

	[Fact]
	public async Task StoreUndoDataAsync_EmptySnapshots_StoresSuccessfully()
	{
		// Arrange
		var requestedBy = "user-123";
		var snapshots = new List<IssueUndoSnapshot>();

		// Act
		var token = await _sut.StoreUndoDataAsync(requestedBy, snapshots);
		var undoData = await _sut.GetUndoDataAsync(token, requestedBy);

		// Assert
		token.Should().NotBeNullOrEmpty();
		undoData.Should().NotBeNull();
		undoData!.Snapshots.Should().BeEmpty();
	}

	#endregion

	#region Retrieve Undo Data

	[Fact]
	public async Task GetUndoDataAsync_ReturnsStoredData()
	{
		// Arrange
		var requestedBy = "user-123";
		var snapshots = CreateTestSnapshots();
		var token = await _sut.StoreUndoDataAsync(requestedBy, snapshots);

		// Act
		var undoData = await _sut.GetUndoDataAsync(token, requestedBy);

		// Assert
		undoData.Should().NotBeNull();
		undoData!.RequestedBy.Should().Be(requestedBy);
		undoData.Snapshots.Should().BeEquivalentTo(snapshots);
	}

	[Fact]
	public async Task GetUndoDataAsync_NonExistentToken_ReturnsNull()
	{
		// Arrange
		var token = "non-existent-token";
		var requestedBy = "user-123";

		// Act
		var undoData = await _sut.GetUndoDataAsync(token, requestedBy);

		// Assert
		undoData.Should().BeNull();
	}

	[Fact]
	public async Task GetUndoDataAsync_WrongUser_ReturnsNull()
	{
		// Arrange
		var originalUser = "user-123";
		var differentUser = "user-456";
		var snapshots = CreateTestSnapshots();
		var token = await _sut.StoreUndoDataAsync(originalUser, snapshots);

		// Act
		var undoData = await _sut.GetUndoDataAsync(token, differentUser);

		// Assert
		undoData.Should().BeNull();
	}

	[Fact]
	public async Task GetUndoDataAsync_CaseInsensitiveUserMatch()
	{
		// Arrange
		var requestedBy = "User@Example.com";
		var snapshots = CreateTestSnapshots();
		var token = await _sut.StoreUndoDataAsync(requestedBy, snapshots);

		// Act - Try with different casing
		var undoData = await _sut.GetUndoDataAsync(token, "user@example.com");

		// Assert
		undoData.Should().NotBeNull();
	}

	[Fact]
	public async Task GetUndoDataAsync_SameUserUppercase_ReturnsData()
	{
		// Arrange
		var requestedBy = "user-123";
		var snapshots = CreateTestSnapshots();
		var token = await _sut.StoreUndoDataAsync(requestedBy, snapshots);

		// Act
		var undoData = await _sut.GetUndoDataAsync(token, "USER-123");

		// Assert
		undoData.Should().NotBeNull();
	}

	#endregion

	#region Invalidate Undo Token

	[Fact]
	public async Task InvalidateUndoTokenAsync_RemovesData()
	{
		// Arrange
		var requestedBy = "user-123";
		var snapshots = CreateTestSnapshots();
		var token = await _sut.StoreUndoDataAsync(requestedBy, snapshots);

		// Act
		await _sut.InvalidateUndoTokenAsync(token);
		var undoData = await _sut.GetUndoDataAsync(token, requestedBy);

		// Assert
		undoData.Should().BeNull();
	}

	[Fact]
	public async Task InvalidateUndoTokenAsync_NonExistentToken_DoesNotThrow()
	{
		// Arrange
		var token = "non-existent-token";

		// Act
		var act = () => _sut.InvalidateUndoTokenAsync(token);

		// Assert
		await act.Should().NotThrowAsync();
	}

	[Fact]
	public async Task InvalidateUndoTokenAsync_OnlyRemovesSpecificToken()
	{
		// Arrange
		var requestedBy = "user-123";
		var snapshots1 = CreateTestSnapshots();
		var snapshots2 = CreateTestSnapshots();
		var token1 = await _sut.StoreUndoDataAsync(requestedBy, snapshots1);
		var token2 = await _sut.StoreUndoDataAsync(requestedBy, snapshots2);

		// Act
		await _sut.InvalidateUndoTokenAsync(token1);
		var undoData1 = await _sut.GetUndoDataAsync(token1, requestedBy);
		var undoData2 = await _sut.GetUndoDataAsync(token2, requestedBy);

		// Assert
		undoData1.Should().BeNull();
		undoData2.Should().NotBeNull();
	}

	#endregion

	#region Different Operation Types

	[Fact]
	public async Task StoreUndoDataAsync_StatusUpdateSnapshot_StoresCorrectly()
	{
		// Arrange
		var requestedBy = "user-123";
		var previousStatus = CreateTestStatus();
		var snapshots = new List<IssueUndoSnapshot>
		{
			new("issue-1", BulkOperationType.StatusUpdate, new StatusUpdateSnapshot(previousStatus))
		};

		// Act
		var token = await _sut.StoreUndoDataAsync(requestedBy, snapshots);
		var undoData = await _sut.GetUndoDataAsync(token, requestedBy);

		// Assert
		undoData!.Snapshots.First().OperationType.Should().Be(BulkOperationType.StatusUpdate);
		undoData.Snapshots.First().PreviousState.Should().BeOfType<StatusUpdateSnapshot>();
	}

	[Fact]
	public async Task StoreUndoDataAsync_CategoryUpdateSnapshot_StoresCorrectly()
	{
		// Arrange
		var requestedBy = "user-123";
		var previousCategory = CreateTestCategory();
		var snapshots = new List<IssueUndoSnapshot>
		{
			new("issue-1", BulkOperationType.CategoryUpdate, new CategoryUpdateSnapshot(previousCategory))
		};

		// Act
		var token = await _sut.StoreUndoDataAsync(requestedBy, snapshots);
		var undoData = await _sut.GetUndoDataAsync(token, requestedBy);

		// Assert
		undoData!.Snapshots.First().OperationType.Should().Be(BulkOperationType.CategoryUpdate);
		undoData.Snapshots.First().PreviousState.Should().BeOfType<CategoryUpdateSnapshot>();
	}

	[Fact]
	public async Task StoreUndoDataAsync_AssignmentSnapshot_StoresCorrectly()
	{
		// Arrange
		var requestedBy = "user-123";
		var previousAssignee = new UserDto("user-456", "Jane", "jane@example.com");
		var snapshots = new List<IssueUndoSnapshot>
		{
			new("issue-1", BulkOperationType.Assignment, new AssignmentSnapshot(previousAssignee))
		};

		// Act
		var token = await _sut.StoreUndoDataAsync(requestedBy, snapshots);
		var undoData = await _sut.GetUndoDataAsync(token, requestedBy);

		// Assert
		undoData!.Snapshots.First().OperationType.Should().Be(BulkOperationType.Assignment);
		undoData.Snapshots.First().PreviousState.Should().BeOfType<AssignmentSnapshot>();
	}

	[Fact]
	public async Task StoreUndoDataAsync_DeleteSnapshot_StoresCorrectly()
	{
		// Arrange
		var requestedBy = "admin";
		var archivedBy = new UserDto("admin", "Admin", "admin@example.com");
		var snapshots = new List<IssueUndoSnapshot>
		{
			new("issue-1", BulkOperationType.Delete, new DeleteSnapshot(false, archivedBy))
		};

		// Act
		var token = await _sut.StoreUndoDataAsync(requestedBy, snapshots);
		var undoData = await _sut.GetUndoDataAsync(token, requestedBy);

		// Assert
		undoData!.Snapshots.First().OperationType.Should().Be(BulkOperationType.Delete);
		undoData.Snapshots.First().PreviousState.Should().BeOfType<DeleteSnapshot>();
	}

	[Fact]
	public async Task StoreUndoDataAsync_MixedOperationTypes_StoresCorrectly()
	{
		// Arrange
		var requestedBy = "user-123";
		var snapshots = new List<IssueUndoSnapshot>
		{
			new("issue-1", BulkOperationType.StatusUpdate, new StatusUpdateSnapshot(CreateTestStatus())),
			new("issue-2", BulkOperationType.CategoryUpdate, new CategoryUpdateSnapshot(CreateTestCategory())),
			new("issue-3", BulkOperationType.Assignment, new AssignmentSnapshot(UserDto.Empty)),
			new("issue-4", BulkOperationType.Delete, new DeleteSnapshot(false, UserDto.Empty))
		};

		// Act
		var token = await _sut.StoreUndoDataAsync(requestedBy, snapshots);
		var undoData = await _sut.GetUndoDataAsync(token, requestedBy);

		// Assert
		undoData!.Snapshots.Should().HaveCount(4);
		undoData.Snapshots.Should().Contain(s => s.OperationType == BulkOperationType.StatusUpdate);
		undoData.Snapshots.Should().Contain(s => s.OperationType == BulkOperationType.CategoryUpdate);
		undoData.Snapshots.Should().Contain(s => s.OperationType == BulkOperationType.Assignment);
		undoData.Snapshots.Should().Contain(s => s.OperationType == BulkOperationType.Delete);
	}

	#endregion

	#region CreatedAt Timestamp

	[Fact]
	public async Task StoreUndoDataAsync_SetsCreatedAtTimestamp()
	{
		// Arrange
		var requestedBy = "user-123";
		var snapshots = CreateTestSnapshots();
		var beforeStore = DateTime.UtcNow;

		// Act
		var token = await _sut.StoreUndoDataAsync(requestedBy, snapshots);
		var undoData = await _sut.GetUndoDataAsync(token, requestedBy);
		var afterStore = DateTime.UtcNow;

		// Assert
		undoData!.CreatedAt.Should().BeOnOrAfter(beforeStore);
		undoData.CreatedAt.Should().BeOnOrBefore(afterStore);
	}

	#endregion

	#region Concurrent Access

	[Fact]
	public async Task StoreAndRetrieve_ConcurrentAccess_AllSucceed()
	{
		// Arrange
		var tasks = new List<Task<(string Token, string User)>>();
		for (var i = 0; i < 10; i++)
		{
			var user = $"user-{i}";
			tasks.Add(Task.Run(async () =>
			{
				var snapshots = CreateTestSnapshots();
				var token = await _sut.StoreUndoDataAsync(user, snapshots);
				return (token, user);
			}));
		}

		// Act
		var results = await Task.WhenAll(tasks);

		// Assert
		results.Should().HaveCount(10);
		foreach (var (token, user) in results)
		{
			var undoData = await _sut.GetUndoDataAsync(token, user);
			undoData.Should().NotBeNull();
		}
	}

	#endregion

	#region Large Data Sets

	[Fact]
	public async Task StoreUndoDataAsync_LargeSnapshotList_StoresSuccessfully()
	{
		// Arrange
		var requestedBy = "user-123";
		var snapshots = Enumerable.Range(1, 100)
			.Select(i => new IssueUndoSnapshot(
				$"issue-{i}",
				BulkOperationType.StatusUpdate,
				new StatusUpdateSnapshot(CreateTestStatus())))
			.ToList();

		// Act
		var token = await _sut.StoreUndoDataAsync(requestedBy, snapshots);
		var undoData = await _sut.GetUndoDataAsync(token, requestedBy);

		// Assert
		undoData.Should().NotBeNull();
		undoData!.Snapshots.Should().HaveCount(100);
	}

	#endregion

	#region Helper Methods

	private static List<IssueUndoSnapshot> CreateTestSnapshots()
	{
		return
		[
			new IssueUndoSnapshot(
				"issue-1",
				BulkOperationType.StatusUpdate,
				new StatusUpdateSnapshot(CreateTestStatus()))
		];
	}

	private static StatusDto CreateTestStatus()
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

	private static CategoryDto CreateTestCategory()
	{
		return new CategoryDto(
			ObjectId.GenerateNewId(),
			"Bug",
			"Bug category",
			DateTime.UtcNow,
			null,
			false,
			UserDto.Empty);
	}

	#endregion
}
