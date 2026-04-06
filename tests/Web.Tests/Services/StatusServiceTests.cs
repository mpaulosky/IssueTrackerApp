// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     StatusServiceTests.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTrackerApp
// Project Name :  Web.Tests
// =======================================================

using Domain.Features.Statuses.Commands;
using Domain.Features.Statuses.Queries;

using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

using Web.Services;

namespace Web.Tests.Services;

/// <summary>
///   Unit tests for StatusService facade operations.
///   Tests CRUD orchestration and MediatR integration.
/// </summary>
public sealed class StatusServiceTests
{
	private readonly IMediator _mediator;
	private readonly StatusService _sut;

	public StatusServiceTests()
	{
		_mediator = Substitute.For<IMediator>();
		var cache = new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions()));
		var cacheLogger = Substitute.For<ILogger<DistributedCacheHelper>>();
		var cacheHelper = new DistributedCacheHelper(cache, cacheLogger);
		_sut = new StatusService(_mediator, cacheHelper);
	}

	#region GetStatusesAsync Tests

	[Fact]
	public async Task GetStatusesAsync_WithDefaultParams_ReturnsStatuses()
	{
		// Arrange
		var statuses = new List<StatusDto>
		{
			CreateTestStatusDto("Open"),
			CreateTestStatusDto("In Progress")
		};
		_mediator.Send(Arg.Any<GetStatusesQuery>(), Arg.Any<CancellationToken>())
			.Returns(Result.Ok<IEnumerable<StatusDto>>(statuses));

		// Act
		var result = await _sut.GetStatusesAsync();

		// Assert
		result.Success.Should().BeTrue();
		result.Value.Should().NotBeNull();
		result.Value!.Should().HaveCount(2);
	}

	[Fact]
	public async Task GetStatusesAsync_WithIncludeArchivedTrue_SendsCorrectQuery()
	{
		// Arrange
		var statuses = new List<StatusDto>();
		_mediator.Send(Arg.Any<GetStatusesQuery>(), Arg.Any<CancellationToken>())
			.Returns(Result.Ok<IEnumerable<StatusDto>>(statuses));

		// Act
		await _sut.GetStatusesAsync(includeArchived: true, CancellationToken.None);

		// Assert
		await _mediator.Received(1).Send(
			Arg.Is<GetStatusesQuery>(q => q.IncludeArchived == true),
			Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task GetStatusesAsync_WhenMediatorFails_ReturnsFailure()
	{
		// Arrange
		_mediator.Send(Arg.Any<GetStatusesQuery>(), Arg.Any<CancellationToken>())
			.Returns(Result.Fail<IEnumerable<StatusDto>>("Database error"));

		// Act
		var result = await _sut.GetStatusesAsync();

		// Assert
		result.Success.Should().BeFalse();
		result.Error.Should().Be("Database error");
	}

	#endregion

	#region GetStatusByIdAsync Tests

	[Fact]
	public async Task GetStatusByIdAsync_WithValidId_ReturnsStatus()
	{
		// Arrange
		var statusDto = CreateTestStatusDto("Open");
		_mediator.Send(Arg.Any<GetStatusByIdQuery>(), Arg.Any<CancellationToken>())
			.Returns(Result.Ok(statusDto));

		// Act
		var result = await _sut.GetStatusByIdAsync("test-id");

		// Assert
		result.Success.Should().BeTrue();
		result.Value.Should().NotBeNull();
		result.Value!.StatusName.Should().Be("Open");
	}

	[Fact]
	public async Task GetStatusByIdAsync_WithInvalidId_ReturnsFailure()
	{
		// Arrange
		_mediator.Send(Arg.Any<GetStatusByIdQuery>(), Arg.Any<CancellationToken>())
			.Returns(Result.Fail<StatusDto>("Status not found"));

		// Act
		var result = await _sut.GetStatusByIdAsync("invalid-id");

		// Assert
		result.Success.Should().BeFalse();
		result.Error.Should().Contain("not found");
	}

	#endregion

	#region CreateStatusAsync Tests

	[Fact]
	public async Task CreateStatusAsync_WithValidData_ReturnsCreatedStatus()
	{
		// Arrange
		var createdStatus = CreateTestStatusDto("New Status");
		_mediator.Send(Arg.Any<CreateStatusCommand>(), Arg.Any<CancellationToken>())
			.Returns(Result.Ok(createdStatus));

		// Act
		var result = await _sut.CreateStatusAsync("New Status", "New status description");

		// Assert
		result.Success.Should().BeTrue();
		result.Value!.StatusName.Should().Be("New Status");
	}

	[Fact]
	public async Task CreateStatusAsync_SendsCorrectCommand()
	{
		// Arrange
		var createdStatus = CreateTestStatusDto("Test Status");
		_mediator.Send(Arg.Any<CreateStatusCommand>(), Arg.Any<CancellationToken>())
			.Returns(Result.Ok(createdStatus));

		// Act
		await _sut.CreateStatusAsync("Test Status", "Test Description", CancellationToken.None);

		// Assert
		await _mediator.Received(1).Send(
			Arg.Is<CreateStatusCommand>(c =>
				c.StatusName == "Test Status" &&
				c.StatusDescription == "Test Description"),
			Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task CreateStatusAsync_WhenValidationFails_ReturnsFailure()
	{
		// Arrange
		_mediator.Send(Arg.Any<CreateStatusCommand>(), Arg.Any<CancellationToken>())
			.Returns(Result.Fail<StatusDto>("Status name is required"));

		// Act
		var result = await _sut.CreateStatusAsync("", "Description");

		// Assert
		result.Success.Should().BeFalse();
		result.Error.Should().Contain("required");
	}

	#endregion

	#region UpdateStatusAsync Tests

	[Fact]
	public async Task UpdateStatusAsync_WithValidData_ReturnsUpdatedStatus()
	{
		// Arrange
		var updatedStatus = CreateTestStatusDto("Updated Status");
		_mediator.Send(Arg.Any<UpdateStatusCommand>(), Arg.Any<CancellationToken>())
			.Returns(Result.Ok(updatedStatus));

		// Act
		var result = await _sut.UpdateStatusAsync("id", "Updated Status", "Updated description");

		// Assert
		result.Success.Should().BeTrue();
		result.Value!.StatusName.Should().Be("Updated Status");
	}

	[Fact]
	public async Task UpdateStatusAsync_SendsCorrectCommand()
	{
		// Arrange
		var updatedStatus = CreateTestStatusDto("Test Status");
		_mediator.Send(Arg.Any<UpdateStatusCommand>(), Arg.Any<CancellationToken>())
			.Returns(Result.Ok(updatedStatus));

		// Act
		await _sut.UpdateStatusAsync("status-id", "Test Status", "Test Description", CancellationToken.None);

		// Assert
		await _mediator.Received(1).Send(
			Arg.Is<UpdateStatusCommand>(c =>
				c.Id == "status-id" &&
				c.StatusName == "Test Status" &&
				c.StatusDescription == "Test Description"),
			Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task UpdateStatusAsync_WhenNotFound_ReturnsFailure()
	{
		// Arrange
		_mediator.Send(Arg.Any<UpdateStatusCommand>(), Arg.Any<CancellationToken>())
			.Returns(Result.Fail<StatusDto>("Status not found"));

		// Act
		var result = await _sut.UpdateStatusAsync("invalid-id", "Title", "Desc");

		// Assert
		result.Success.Should().BeFalse();
		result.Error.Should().Contain("not found");
	}

	#endregion

	#region ArchiveStatusAsync Tests

	[Fact]
	public async Task ArchiveStatusAsync_WhenArchiving_ReturnsArchivedStatus()
	{
		// Arrange
		var archivedStatus = CreateTestStatusDto("Archived Status", archived: true);
		_mediator.Send(Arg.Any<ArchiveStatusCommand>(), Arg.Any<CancellationToken>())
			.Returns(Result.Ok(archivedStatus));

		// Act
		var result = await _sut.ArchiveStatusAsync("id", archive: true, CreateTestUserDto());

		// Assert
		result.Success.Should().BeTrue();
		result.Value!.Archived.Should().BeTrue();
	}

	[Fact]
	public async Task ArchiveStatusAsync_WhenUnarchiving_ReturnsUnarchivedStatus()
	{
		// Arrange
		var unarchivedStatus = CreateTestStatusDto("Active Status", archived: false);
		_mediator.Send(Arg.Any<ArchiveStatusCommand>(), Arg.Any<CancellationToken>())
			.Returns(Result.Ok(unarchivedStatus));

		// Act
		var result = await _sut.ArchiveStatusAsync("id", archive: false, CreateTestUserDto());

		// Assert
		result.Success.Should().BeTrue();
		result.Value!.Archived.Should().BeFalse();
	}

	[Fact]
	public async Task ArchiveStatusAsync_SendsCorrectCommand()
	{
		// Arrange
		var archivedStatus = CreateTestStatusDto("Test Status", archived: true);
		var user = CreateTestUserDto();
		_mediator.Send(Arg.Any<ArchiveStatusCommand>(), Arg.Any<CancellationToken>())
			.Returns(Result.Ok(archivedStatus));

		// Act
		await _sut.ArchiveStatusAsync("status-id", archive: true, user, CancellationToken.None);

		// Assert
		await _mediator.Received(1).Send(
			Arg.Is<ArchiveStatusCommand>(c =>
				c.Id == "status-id" &&
				c.Archive == true &&
				c.ArchivedBy.Id == user.Id),
			Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task ArchiveStatusAsync_WhenNotFound_ReturnsFailure()
	{
		// Arrange
		_mediator.Send(Arg.Any<ArchiveStatusCommand>(), Arg.Any<CancellationToken>())
			.Returns(Result.Fail<StatusDto>("Status not found"));

		// Act
		var result = await _sut.ArchiveStatusAsync("invalid-id", archive: true, CreateTestUserDto());

		// Assert
		result.Success.Should().BeFalse();
		result.Error.Should().Contain("not found");
	}

	#endregion

	#region Helper Methods

	private static StatusDto CreateTestStatusDto(string name, bool archived = false)
	{
		return new StatusDto(
			ObjectId.GenerateNewId(),
			name,
			$"{name} description",
			DateTime.UtcNow,
			null,
			archived,
			archived ? CreateTestUserDto() : UserDto.Empty);
	}

	private static UserDto CreateTestUserDto()
	{
		return new UserDto("user1", "Test User", "test@example.com");
	}

	#endregion
}
