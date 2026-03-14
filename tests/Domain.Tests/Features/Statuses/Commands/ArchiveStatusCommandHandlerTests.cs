// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     ArchiveStatusCommandHandlerTests.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTrackerApp
// Project Name :  Domain.Tests
// =======================================================

using Domain.Features.Statuses.Commands;

namespace Domain.Tests.Features.Statuses.Commands;

/// <summary>
///   Unit tests for ArchiveStatusCommandHandler.
/// </summary>
public class ArchiveStatusCommandHandlerTests
{
	private readonly IRepository<Status> _repository;
	private readonly ILogger<ArchiveStatusCommandHandler> _logger;
	private readonly ArchiveStatusCommandHandler _handler;

	public ArchiveStatusCommandHandlerTests()
	{
		_repository = Substitute.For<IRepository<Status>>();
		_logger = new NullLogger<ArchiveStatusCommandHandler>();
		_handler = new ArchiveStatusCommandHandler(_repository, _logger);
	}

	/// <summary>
	///   Verifies that archiving a status sets the Archived flag to true.
	/// </summary>
	[Fact]
	public async Task ArchiveStatus_SetsArchivedFlag()
	{
		// Arrange
		var statusId = ObjectId.GenerateNewId();
		var existingStatus = new Status
		{
			Id = statusId,
			StatusName = "Test Status",
			StatusDescription = "Test Description",
			DateCreated = DateTime.UtcNow.AddDays(-1),
			Archived = false,
			ArchivedBy = UserInfo.Empty
		};

		var archivedByUser = new UserInfo { Id = "user-123", Name = "Test User", Email = "test@example.com" };
		var archivedByUserDto = new UserDto(archivedByUser);
		var command = new ArchiveStatusCommand(statusId.ToString(), true, archivedByUserDto);

		_repository.GetByIdAsync(statusId.ToString(), Arg.Any<CancellationToken>())
			.Returns(Result.Ok(existingStatus));

		Status? capturedStatus = null;
		_repository.UpdateAsync(Arg.Any<Status>(), Arg.Any<CancellationToken>())
			.Returns(callInfo =>
			{
				capturedStatus = callInfo.Arg<Status>();
				return Result.Ok(capturedStatus);
			});

		// Act
		var result = await _handler.Handle(command, CancellationToken.None);

		// Assert
		result.Success.Should().BeTrue();
		capturedStatus.Should().NotBeNull();
		capturedStatus!.Archived.Should().BeTrue();
		capturedStatus.ArchivedBy.Should().BeEquivalentTo(archivedByUser);
	}
}
