// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     IssueServiceTests.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTrackerApp
// Project Name :  Web.Tests
// =======================================================

using Web.Services;

namespace Web.Tests.Services;

/// <summary>
///   Unit tests for IssueService facade operations.
///   Tests CRUD orchestration and MediatR integration.
/// </summary>
public sealed class IssueServiceTests
{
	private readonly IMediator _mediator;
	private readonly Domain.Abstractions.INotificationService _notificationService;
	private readonly IBulkOperationQueue _bulkQueue;
	private readonly IssueService _sut;

	public IssueServiceTests()
	{
		_mediator = Substitute.For<IMediator>();
		_notificationService = Substitute.For<Domain.Abstractions.INotificationService>();
		_bulkQueue = Substitute.For<IBulkOperationQueue>();
		_sut = new IssueService(_mediator, _notificationService, _bulkQueue);
	}

	#region GetIssuesAsync Tests

	[Fact]
	public async Task GetIssuesAsync_WithValidParams_ReturnsPagedIssues()
	{
		// Arrange
		var issues = new List<IssueDto>
		{
			CreateTestIssueDto("Issue 1"),
			CreateTestIssueDto("Issue 2")
		};
		var response = new PaginatedResponse<IssueDto>(issues, 2, 1, 10);
		_mediator.Send(Arg.Any<GetIssuesQuery>(), Arg.Any<CancellationToken>())
			.Returns(Result.Ok(response));

		// Act
		var result = await _sut.GetIssuesAsync(page: 1, pageSize: 10);

		// Assert
		result.Success.Should().BeTrue();
		result.Value.Should().NotBeNull();
		result.Value!.Items.Should().HaveCount(2);
	}

	[Fact]
	public async Task GetIssuesAsync_WithFilters_SendsCorrectQuery()
	{
		// Arrange
		var response = new PaginatedResponse<IssueDto>(new List<IssueDto>(), 0, 1, 10);
		_mediator.Send(Arg.Any<GetIssuesQuery>(), Arg.Any<CancellationToken>())
			.Returns(Result.Ok(response));

		// Act
		await _sut.GetIssuesAsync(2, 25, "Open", "Bug", true, CancellationToken.None);

		// Assert
		await _mediator.Received(1).Send(
			Arg.Is<GetIssuesQuery>(q =>
				q.Page == 2 &&
				q.PageSize == 25 &&
				q.StatusFilter == "Open" &&
				q.CategoryFilter == "Bug" &&
				q.IncludeArchived == true),
			Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task GetIssuesAsync_WhenMediatorFails_ReturnsFailure()
	{
		// Arrange
		_mediator.Send(Arg.Any<GetIssuesQuery>(), Arg.Any<CancellationToken>())
			.Returns(Result.Fail<PaginatedResponse<IssueDto>>("Database error"));

		// Act
		var result = await _sut.GetIssuesAsync();

		// Assert
		result.Success.Should().BeFalse();
		result.Error.Should().Be("Database error");
	}

	#endregion

	#region GetIssueByIdAsync Tests

	[Fact]
	public async Task GetIssueByIdAsync_WithValidId_ReturnsIssue()
	{
		// Arrange
		var issueDto = CreateTestIssueDto("Test Issue");
		_mediator.Send(Arg.Any<GetIssueByIdQuery>(), Arg.Any<CancellationToken>())
			.Returns(Result.Ok(issueDto));

		// Act
		var result = await _sut.GetIssueByIdAsync("test-id");

		// Assert
		result.Success.Should().BeTrue();
		result.Value.Should().NotBeNull();
		result.Value!.Title.Should().Be("Test Issue");
	}

	[Fact]
	public async Task GetIssueByIdAsync_WithInvalidId_ReturnsFailure()
	{
		// Arrange
		_mediator.Send(Arg.Any<GetIssueByIdQuery>(), Arg.Any<CancellationToken>())
			.Returns(Result.Fail<IssueDto>("Issue not found"));

		// Act
		var result = await _sut.GetIssueByIdAsync("invalid-id");

		// Assert
		result.Success.Should().BeFalse();
		result.Error.Should().Contain("not found");
	}

	#endregion

	#region CreateIssueAsync Tests

	[Fact]
	public async Task CreateIssueAsync_WithValidData_ReturnsCreatedIssue()
	{
		// Arrange
		var category = CreateTestCategoryDto();
		var author = CreateTestUserDto();
		var createdIssue = CreateTestIssueDto("New Issue");

		_mediator.Send(Arg.Any<CreateIssueCommand>(), Arg.Any<CancellationToken>())
			.Returns(Result.Ok(createdIssue));

		// Act
		var result = await _sut.CreateIssueAsync("New Issue", "Description", category, author);

		// Assert
		result.Success.Should().BeTrue();
		result.Value!.Title.Should().Be("New Issue");
	}

	[Fact]
	public async Task CreateIssueAsync_WhenSuccessful_NotifiesClients()
	{
		// Arrange
		var createdIssue = CreateTestIssueDto("New Issue");
		_mediator.Send(Arg.Any<CreateIssueCommand>(), Arg.Any<CancellationToken>())
			.Returns(Result.Ok(createdIssue));

		// Act
		await _sut.CreateIssueAsync("New Issue", "Description", CreateTestCategoryDto(), CreateTestUserDto());

		// Assert
		await _notificationService.Received(1).NotifyIssueCreatedAsync(
			Arg.Is<IssueDto>(i => i.Title == "New Issue"),
			Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task CreateIssueAsync_WhenFails_DoesNotNotifyClients()
	{
		// Arrange
		_mediator.Send(Arg.Any<CreateIssueCommand>(), Arg.Any<CancellationToken>())
			.Returns(Result.Fail<IssueDto>("Validation failed"));

		// Act
		await _sut.CreateIssueAsync("", "Description", CreateTestCategoryDto(), CreateTestUserDto());

		// Assert
		await _notificationService.DidNotReceive().NotifyIssueCreatedAsync(
			Arg.Any<IssueDto>(),
			Arg.Any<CancellationToken>());
	}

	#endregion

	#region UpdateIssueAsync Tests

	[Fact]
	public async Task UpdateIssueAsync_WithValidData_ReturnsUpdatedIssue()
	{
		// Arrange
		var updatedIssue = CreateTestIssueDto("Updated Issue");
		_mediator.Send(Arg.Any<UpdateIssueCommand>(), Arg.Any<CancellationToken>())
			.Returns(Result.Ok(updatedIssue));

		// Act
		var result = await _sut.UpdateIssueAsync("id", "Updated Issue", "New Description", CreateTestCategoryDto());

		// Assert
		result.Success.Should().BeTrue();
		result.Value!.Title.Should().Be("Updated Issue");
	}

	[Fact]
	public async Task UpdateIssueAsync_WhenSuccessful_NotifiesClients()
	{
		// Arrange
		var updatedIssue = CreateTestIssueDto("Updated Issue");
		_mediator.Send(Arg.Any<UpdateIssueCommand>(), Arg.Any<CancellationToken>())
			.Returns(Result.Ok(updatedIssue));

		// Act
		await _sut.UpdateIssueAsync("id", "Updated Issue", "New Description", CreateTestCategoryDto());

		// Assert
		await _notificationService.Received(1).NotifyIssueUpdatedAsync(
			Arg.Is<IssueDto>(i => i.Title == "Updated Issue"),
			Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task UpdateIssueAsync_WhenNotFound_ReturnsFailure()
	{
		// Arrange
		_mediator.Send(Arg.Any<UpdateIssueCommand>(), Arg.Any<CancellationToken>())
			.Returns(Result.Fail<IssueDto>("Issue not found"));

		// Act
		var result = await _sut.UpdateIssueAsync("invalid-id", "Title", "Desc", CreateTestCategoryDto());

		// Assert
		result.Success.Should().BeFalse();
		result.Error.Should().Contain("not found");
	}

	#endregion

	#region DeleteIssueAsync Tests

	[Fact]
	public async Task DeleteIssueAsync_WithValidId_ReturnsSuccess()
	{
		// Arrange
		_mediator.Send(Arg.Any<DeleteIssueCommand>(), Arg.Any<CancellationToken>())
			.Returns(Result.Ok(true));

		// Act
		var result = await _sut.DeleteIssueAsync("id", CreateTestUserDto());

		// Assert
		result.Success.Should().BeTrue();
		result.Value.Should().BeTrue();
	}

	[Fact]
	public async Task DeleteIssueAsync_WhenNotFound_ReturnsFailure()
	{
		// Arrange
		_mediator.Send(Arg.Any<DeleteIssueCommand>(), Arg.Any<CancellationToken>())
			.Returns(Result.Fail<bool>("Issue not found"));

		// Act
		var result = await _sut.DeleteIssueAsync("invalid-id", CreateTestUserDto());

		// Assert
		result.Success.Should().BeFalse();
	}

	#endregion

	#region ChangeIssueStatusAsync Tests

	[Fact]
	public async Task ChangeIssueStatusAsync_WithValidData_ReturnsUpdatedIssue()
	{
		// Arrange
		var updatedIssue = CreateTestIssueDto("Test Issue");
		_mediator.Send(Arg.Any<ChangeIssueStatusCommand>(), Arg.Any<CancellationToken>())
			.Returns(Result.Ok(updatedIssue));

		// Act
		var result = await _sut.ChangeIssueStatusAsync("id", CreateTestStatusDto());

		// Assert
		result.Success.Should().BeTrue();
	}

	[Fact]
	public async Task ChangeIssueStatusAsync_WhenSuccessful_NotifiesClients()
	{
		// Arrange
		var updatedIssue = CreateTestIssueDto("Test Issue");
		_mediator.Send(Arg.Any<ChangeIssueStatusCommand>(), Arg.Any<CancellationToken>())
			.Returns(Result.Ok(updatedIssue));

		// Act
		await _sut.ChangeIssueStatusAsync("id", CreateTestStatusDto());

		// Assert
		await _notificationService.Received(1).NotifyIssueUpdatedAsync(
			Arg.Any<IssueDto>(),
			Arg.Any<CancellationToken>());
	}

	#endregion

	#region SearchIssuesAsync Tests

	[Fact]
	public async Task SearchIssuesAsync_WithRequest_SendsCorrectQuery()
	{
		// Arrange
		var request = new IssueSearchRequest { SearchText = "bug", Page = 1, PageSize = 10 };
		var pagedResult = PagedResult<IssueDto>.Create(new List<IssueDto>(), 0, 1, 10);
		_mediator.Send(Arg.Any<SearchIssuesQuery>(), Arg.Any<CancellationToken>())
			.Returns(Result.Ok(pagedResult));

		// Act
		var result = await _sut.SearchIssuesAsync(request);

		// Assert
		result.Success.Should().BeTrue();
		await _mediator.Received(1).Send(
			Arg.Is<SearchIssuesQuery>(q => q.Request.SearchText == "bug"),
			Arg.Any<CancellationToken>());
	}

	#endregion

	#region BulkUpdateStatusAsync Tests

	[Fact]
	public async Task BulkUpdateStatusAsync_WithValidIds_ReturnsResult()
	{
		// Arrange
		var issueIds = new List<string> { "id1", "id2" };
		var bulkResult = BulkOperationResult.Success(2, "token");
		_mediator.Send(Arg.Any<BulkUpdateStatusCommand>(), Arg.Any<CancellationToken>())
			.Returns(Result.Ok(bulkResult));

		// Act
		var result = await _sut.BulkUpdateStatusAsync(issueIds, CreateTestStatusDto(), "user1");

		// Assert
		result.Success.Should().BeTrue();
		result.Value!.SuccessCount.Should().Be(2);
	}

	#endregion

	#region BulkUpdateCategoryAsync Tests

	[Fact]
	public async Task BulkUpdateCategoryAsync_WithValidIds_ReturnsResult()
	{
		// Arrange
		var issueIds = new List<string> { "id1", "id2" };
		var bulkResult = BulkOperationResult.Success(2, "token");
		_mediator.Send(Arg.Any<BulkUpdateCategoryCommand>(), Arg.Any<CancellationToken>())
			.Returns(Result.Ok(bulkResult));

		// Act
		var result = await _sut.BulkUpdateCategoryAsync(issueIds, CreateTestCategoryDto(), "user1");

		// Assert
		result.Success.Should().BeTrue();
	}

	#endregion

	#region BulkAssignAsync Tests

	[Fact]
	public async Task BulkAssignAsync_WithValidIds_ReturnsResult()
	{
		// Arrange
		var issueIds = new List<string> { "id1", "id2" };
		var bulkResult = BulkOperationResult.Success(2, "token");
		_mediator.Send(Arg.Any<BulkAssignCommand>(), Arg.Any<CancellationToken>())
			.Returns(Result.Ok(bulkResult));

		// Act
		var result = await _sut.BulkAssignAsync(issueIds, CreateTestUserDto(), "user1");

		// Assert
		result.Success.Should().BeTrue();
	}

	#endregion

	#region BulkDeleteAsync Tests

	[Fact]
	public async Task BulkDeleteAsync_WithValidIds_ReturnsResult()
	{
		// Arrange
		var issueIds = new List<string> { "id1", "id2" };
		var bulkResult = BulkOperationResult.Success(2, "token");
		_mediator.Send(Arg.Any<BulkDeleteCommand>(), Arg.Any<CancellationToken>())
			.Returns(Result.Ok(bulkResult));

		// Act
		var result = await _sut.BulkDeleteAsync(issueIds, CreateTestUserDto(), "user1");

		// Assert
		result.Success.Should().BeTrue();
	}

	#endregion

	#region BulkExportAsync Tests

	[Fact]
	public async Task BulkExportAsync_WithValidIds_ReturnsCsvData()
	{
		// Arrange
		var issueIds = new List<string> { "id1", "id2" };
		var exportResult = new BulkExportResult(new byte[] { 1, 2, 3 }, "export.csv", 2, new List<BulkOperationError>());
		_mediator.Send(Arg.Any<BulkExportCommand>(), Arg.Any<CancellationToken>())
			.Returns(Result.Ok(exportResult));

		// Act
		var result = await _sut.BulkExportAsync(issueIds, "user1");

		// Assert
		result.Success.Should().BeTrue();
		result.Value!.FileName.Should().Be("export.csv");
	}

	#endregion

	#region UndoLastBulkOperationAsync Tests

	[Fact]
	public async Task UndoLastBulkOperationAsync_WithValidToken_ReturnsResult()
	{
		// Arrange
		var bulkResult = BulkOperationResult.Success(2);
		_mediator.Send(Arg.Any<UndoBulkOperationCommand>(), Arg.Any<CancellationToken>())
			.Returns(Result.Ok(bulkResult));

		// Act
		var result = await _sut.UndoLastBulkOperationAsync("undo-token", "user1");

		// Assert
		result.Success.Should().BeTrue();
	}

	#endregion

	#region GetBulkOperationStatusAsync Tests

	[Fact]
	public async Task GetBulkOperationStatusAsync_WithValidId_ReturnsStatus()
	{
		// Arrange
		var status = BulkOperationStatus.Completed;
		_bulkQueue.GetStatusAsync("op1", Arg.Any<CancellationToken>())
			.Returns(status);

		// Act
		var result = await _sut.GetBulkOperationStatusAsync("op1");

		// Assert
		result.Should().NotBeNull();
		result!.Value.Should().Be(BulkOperationStatus.Completed);
	}

	[Fact]
	public async Task GetBulkOperationStatusAsync_WithInvalidId_ReturnsNull()
	{
		// Arrange
		_bulkQueue.GetStatusAsync("invalid", Arg.Any<CancellationToken>())
			.Returns((BulkOperationStatus?)null);

		// Act
		var result = await _sut.GetBulkOperationStatusAsync("invalid");

		// Assert
		result.Should().BeNull();
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
			UserDto.Empty,
			0,
			[],
			[]);
	}
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
