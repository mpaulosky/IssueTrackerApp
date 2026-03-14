// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     UpdateStatusCommandHandlerTests.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTrackerApp
// Project Name :  Domain.Tests
// =======================================================

using Domain.Features.Statuses.Commands;

namespace Domain.Tests.Features.Statuses.Commands;

/// <summary>
///   Unit tests for UpdateStatusCommandHandler.
/// </summary>
public class UpdateStatusCommandHandlerTests
{
	private readonly IRepository<Status> _repository;
	private readonly ILogger<UpdateStatusCommandHandler> _logger;
	private readonly UpdateStatusCommandHandler _handler;

	public UpdateStatusCommandHandlerTests()
	{
		_repository = Substitute.For<IRepository<Status>>();
		_logger = new NullLogger<UpdateStatusCommandHandler>();
		_handler = new UpdateStatusCommandHandler(_repository, _logger);
	}

	/// <summary>
	///   Verifies that updating an existing status returns success.
	/// </summary>
	[Fact]
	public async Task UpdateStatus_WhenExists_ReturnsSuccess()
	{
		// Arrange
		var statusId = ObjectId.GenerateNewId();
		var existingStatus = new Status
		{
			Id = statusId,
			StatusName = "Old Name",
			StatusDescription = "Old Description",
			DateCreated = DateTime.UtcNow.AddDays(-1),
			Archived = false,
			ArchivedBy = UserInfo.Empty
		};

		var command = new UpdateStatusCommand(statusId.ToString(), "New Name", "New Description");

		_repository.GetByIdAsync(statusId.ToString(), Arg.Any<CancellationToken>())
			.Returns(Result.Ok(existingStatus));

		_repository.FirstOrDefaultAsync(
				Arg.Any<Expression<Func<Status, bool>>>(),
				Arg.Any<CancellationToken>())
			.Returns(Result.Ok<Status?>(null));

		_repository.UpdateAsync(Arg.Any<Status>(), Arg.Any<CancellationToken>())
			.Returns(callInfo =>
			{
				var status = callInfo.Arg<Status>();
				return Result.Ok(status);
			});

		// Act
		var result = await _handler.Handle(command, CancellationToken.None);

		// Assert
		result.Success.Should().BeTrue();
		result.Value.Should().NotBeNull();
		result.Value!.StatusName.Should().Be("New Name");
		result.Value.StatusDescription.Should().Be("New Description");
	}

	/// <summary>
	///   Verifies that updating a non-existent status returns NotFound error.
	/// </summary>
	[Fact]
	public async Task UpdateStatus_WhenNotFound_ReturnsNotFoundError()
	{
		// Arrange
		var nonExistentId = ObjectId.GenerateNewId().ToString();
		var command = new UpdateStatusCommand(nonExistentId, "Name", "Description");

		_repository.GetByIdAsync(nonExistentId, Arg.Any<CancellationToken>())
			.Returns(Result.Fail<Status>("Status not found", ResultErrorCode.NotFound));

		// Act
		var result = await _handler.Handle(command, CancellationToken.None);

		// Assert
		result.Failure.Should().BeTrue();
		result.ErrorCode.Should().Be(ResultErrorCode.NotFound);
		result.Error.Should().Contain("not found");
	}
}
