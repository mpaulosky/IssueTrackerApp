// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     DashboardServiceTests.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTrackerApp
// Project Name :  Web.Tests
// =======================================================

using Domain.Features.Dashboard.Queries;
using Web.Services;

namespace Web.Tests.Services;

/// <summary>
///   Unit tests for DashboardService facade operations.
///   Tests dashboard data retrieval and MediatR integration.
/// </summary>
public sealed class DashboardServiceTests
{
	private readonly IMediator _mediator;
	private readonly DashboardService _sut;

	public DashboardServiceTests()
	{
		_mediator = Substitute.For<IMediator>();
		_sut = new DashboardService(_mediator);
	}

	#region GetUserDashboardAsync Tests

	[Fact]
	public async Task GetUserDashboardAsync_WithValidUserId_ReturnsDashboardData()
	{
		// Arrange
		var userId = "user-123";
		var dashboardDto = CreateTestDashboardDto();
		_mediator.Send(Arg.Any<GetUserDashboardQuery>(), Arg.Any<CancellationToken>())
			.Returns(Result.Ok(dashboardDto));

		// Act
		var result = await _sut.GetUserDashboardAsync(userId);

		// Assert
		result.Success.Should().BeTrue();
		result.Value.Should().NotBeNull();
		result.Value!.TotalIssues.Should().Be(10);
		result.Value.OpenIssues.Should().Be(5);
		result.Value.ResolvedIssues.Should().Be(3);
	}

	[Fact]
	public async Task GetUserDashboardAsync_SendsCorrectQueryToMediator()
	{
		// Arrange
		var userId = "user-456";
		var dashboardDto = CreateTestDashboardDto();
		_mediator.Send(Arg.Any<GetUserDashboardQuery>(), Arg.Any<CancellationToken>())
			.Returns(Result.Ok(dashboardDto));

		// Act
		await _sut.GetUserDashboardAsync(userId, CancellationToken.None);

		// Assert
		await _mediator.Received(1).Send(
			Arg.Is<GetUserDashboardQuery>(q => q.UserId == userId),
			Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task GetUserDashboardAsync_WhenMediatorFails_ReturnsFailure()
	{
		// Arrange
		var userId = "user-789";
		_mediator.Send(Arg.Any<GetUserDashboardQuery>(), Arg.Any<CancellationToken>())
			.Returns(Result.Fail<UserDashboardDto>("Database error"));

		// Act
		var result = await _sut.GetUserDashboardAsync(userId);

		// Assert
		result.Success.Should().BeFalse();
		result.Error.Should().Be("Database error");
	}

	[Fact]
	public async Task GetUserDashboardAsync_WithNullUserId_SendsQueryWithNullUserId()
	{
		// Arrange
		string? userId = null;
		var dashboardDto = UserDashboardDto.Empty;
		_mediator.Send(Arg.Any<GetUserDashboardQuery>(), Arg.Any<CancellationToken>())
			.Returns(Result.Ok(dashboardDto));

		// Act
		await _sut.GetUserDashboardAsync(userId!, CancellationToken.None);

		// Assert
		await _mediator.Received(1).Send(
			Arg.Is<GetUserDashboardQuery>(q => q.UserId == null),
			Arg.Any<CancellationToken>());
	}

	[Fact]
	public async Task GetUserDashboardAsync_PassesCancellationTokenToMediator()
	{
		// Arrange
		var userId = "user-abc";
		var dashboardDto = CreateTestDashboardDto();
		using var cts = new CancellationTokenSource();
		var cancellationToken = cts.Token;
		_mediator.Send(Arg.Any<GetUserDashboardQuery>(), Arg.Any<CancellationToken>())
			.Returns(Result.Ok(dashboardDto));

		// Act
		await _sut.GetUserDashboardAsync(userId, cancellationToken);

		// Assert
		await _mediator.Received(1).Send(
			Arg.Any<GetUserDashboardQuery>(),
			cancellationToken);
	}

	#endregion

	#region Helper Methods

	private static UserDashboardDto CreateTestDashboardDto()
	{
		var recentIssues = new List<IssueDto>
		{
			CreateTestIssueDto("Recent Issue 1"),
			CreateTestIssueDto("Recent Issue 2")
		};

		return new UserDashboardDto(
			TotalIssues: 10,
			OpenIssues: 5,
			ResolvedIssues: 3,
			ThisWeekIssues: 2,
			RecentIssues: recentIssues);
	}

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
