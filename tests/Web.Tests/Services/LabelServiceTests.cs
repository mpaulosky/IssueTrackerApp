// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     LabelServiceTests.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTrackerApp
// Project Name :  Web.Tests
// =======================================================

using Domain.Models;

using Microsoft.Extensions.Logging.Abstractions;

using Web.Services;

namespace Web.Tests.Services;

/// <summary>
///   Unit tests for LabelService suggestion logic.
/// </summary>
public sealed class LabelServiceTests
{
	private readonly IRepository<Issue> _repository;
	private readonly LabelService _sut;

	public LabelServiceTests()
	{
		_repository = Substitute.For<IRepository<Issue>>();
		_sut = new LabelService(_repository, new NullLogger<LabelService>());
	}

	[Fact]
	public async Task GetSuggestionsAsync_WithMatchingPrefix_ReturnsSuggestions()
	{
		// Arrange
		var issues = new List<Issue>
		{
			new() { Labels = ["bug", "bugfix", "feature"] },
			new() { Labels = ["bug", "backend"] }
		};

		_repository.GetAllAsync(Arg.Any<CancellationToken>())
			.Returns(Result.Ok<IEnumerable<Issue>>(issues));

		// Act
		var result = await _sut.GetSuggestionsAsync("bu");

		// Assert
		result.Should().NotBeEmpty();
		result.Should().OnlyContain(l => l.StartsWith("bu", StringComparison.OrdinalIgnoreCase));
	}

	[Fact]
	public async Task GetSuggestionsAsync_WithEmptyPrefix_ReturnsAllLabels()
	{
		// Arrange
		var issues = new List<Issue>
		{
			new() { Labels = ["alpha", "beta"] },
			new() { Labels = ["gamma"] }
		};

		_repository.GetAllAsync(Arg.Any<CancellationToken>())
			.Returns(Result.Ok<IEnumerable<Issue>>(issues));

		// Act
		var result = await _sut.GetSuggestionsAsync(string.Empty);

		// Assert
		result.Should().HaveCount(3);
		result.Should().Contain("alpha");
		result.Should().Contain("beta");
		result.Should().Contain("gamma");
	}

	[Fact]
	public async Task GetSuggestionsAsync_WhenRepositoryReturnsNoIssues_ReturnsEmpty()
	{
		// Arrange
		_repository.GetAllAsync(Arg.Any<CancellationToken>())
			.Returns(Result.Ok<IEnumerable<Issue>>([]));

		// Act
		var result = await _sut.GetSuggestionsAsync("bug");

		// Assert
		result.Should().BeEmpty();
	}

	[Fact]
	public async Task GetSuggestionsAsync_RespectsMaxResults()
	{
		// Arrange
		var labels = Enumerable.Range(1, 20).Select(i => $"bug-{i:D2}").ToList();
		var issues = new List<Issue>
		{
			new() { Labels = labels }
		};

		_repository.GetAllAsync(Arg.Any<CancellationToken>())
			.Returns(Result.Ok<IEnumerable<Issue>>(issues));

		// Act
		var result = await _sut.GetSuggestionsAsync("bug", maxResults: 5);

		// Assert
		result.Should().HaveCount(5);
	}
}
