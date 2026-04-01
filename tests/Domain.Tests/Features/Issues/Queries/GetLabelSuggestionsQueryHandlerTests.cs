// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     GetLabelSuggestionsQueryHandlerTests.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTrackerApp
// Project Name :  Domain.Tests
// =======================================================

using Domain.Features.Issues;
using Domain.Features.Issues.Queries;

namespace Domain.Tests.Features.Issues.Queries;

/// <summary>
///   Unit tests for GetLabelSuggestionsQueryHandler.
/// </summary>
public sealed class GetLabelSuggestionsQueryHandlerTests
{
	private readonly ILabelService _labelService;
	private readonly GetLabelSuggestionsQueryHandler _handler;

	public GetLabelSuggestionsQueryHandlerTests()
	{
		_labelService = Substitute.For<ILabelService>();
		_handler = new GetLabelSuggestionsQueryHandler(
			_labelService,
			new NullLogger<GetLabelSuggestionsQueryHandler>());
	}

	[Fact]
	public async Task Handle_WithMatchingLabels_ReturnsSortedDistinctResults()
	{
		// Arrange
		IReadOnlyList<string> suggestions = ["bug", "bug-fix", "buggy"];
		_labelService.GetSuggestionsAsync("bug", Arg.Any<int>(), Arg.Any<CancellationToken>())
			.Returns(suggestions);

		var query = new GetLabelSuggestionsQuery("bug");

		// Act
		var result = await _handler.Handle(query, CancellationToken.None);

		// Assert
		result.Success.Should().BeTrue();
		result.Value.Should().NotBeNull();
		result.Value!.Should().BeEquivalentTo(["bug", "bug-fix", "buggy"]);
	}

	[Fact]
	public async Task Handle_WithNoMatches_ReturnsEmptyList()
	{
		// Arrange
		IReadOnlyList<string> empty = [];
		_labelService.GetSuggestionsAsync("xyz", Arg.Any<int>(), Arg.Any<CancellationToken>())
			.Returns(empty);

		var query = new GetLabelSuggestionsQuery("xyz");

		// Act
		var result = await _handler.Handle(query, CancellationToken.None);

		// Assert
		result.Success.Should().BeTrue();
		result.Value.Should().NotBeNull();
		result.Value!.Should().BeEmpty();
	}

	[Fact]
	public async Task Handle_WithEmptyPrefix_ReturnsValidationFailure()
	{
		// Arrange
		var query = new GetLabelSuggestionsQuery(string.Empty);

		// Act
		var result = await _handler.Handle(query, CancellationToken.None);

		// Assert
		result.Failure.Should().BeTrue();
		result.ErrorCode.Should().Be(ResultErrorCode.Validation);
		await _labelService.DidNotReceive()
			.GetSuggestionsAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<CancellationToken>());
	}
}
