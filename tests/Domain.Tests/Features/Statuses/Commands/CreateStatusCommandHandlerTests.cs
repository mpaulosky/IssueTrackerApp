// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     CreateStatusCommandHandlerTests.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTrackerApp
// Project Name :  Domain.Tests
// =======================================================

using Domain.Features.Statuses.Commands;

namespace Domain.Tests.Features.Statuses.Commands;

/// <summary>
///   Unit tests for CreateStatusCommandHandler.
/// </summary>
public class CreateStatusCommandHandlerTests
{
	private readonly IRepository<Status> _repository;
	private readonly ILogger<CreateStatusCommandHandler> _logger;
	private readonly CreateStatusCommandHandler _handler;

	public CreateStatusCommandHandlerTests()
	{
		_repository = Substitute.For<IRepository<Status>>();
		_logger = new NullLogger<CreateStatusCommandHandler>();
		_handler = new CreateStatusCommandHandler(_repository, _logger);
	}

	/// <summary>
	///   Verifies that creating a status with valid data returns success.
	/// </summary>
	[Fact]
	public async Task CreateStatus_WithValidData_ReturnsSuccess()
	{
		// Arrange
		var command = new CreateStatusCommand("Test Status", "Test Description");

		_repository.FirstOrDefaultAsync(
				Arg.Any<Expression<Func<Status, bool>>>(),
				Arg.Any<CancellationToken>())
			.Returns(Result.Ok<Status?>(null));

		_repository.AddAsync(Arg.Any<Status>(), Arg.Any<CancellationToken>())
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
		result.Value!.StatusName.Should().Be("Test Status");
		result.Value.StatusDescription.Should().Be("Test Description");
	}
}
