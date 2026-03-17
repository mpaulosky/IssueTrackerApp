// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     ExportAnalyticsQueryHandlerTests.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTrackerApp
// Project Name :  Domain.Tests
// =======================================================

using System.Linq.Expressions;
using System.Text;

using Domain.Abstractions;
using Domain.Features.Analytics.Queries;

using Microsoft.Extensions.Logging;

using MongoDB.Bson;

namespace Domain.Tests.Features.Analytics;

/// <summary>
/// Unit tests for ExportAnalyticsQueryHandler.
/// </summary>
public sealed class ExportAnalyticsQueryHandlerTests
{
	private readonly IRepository<Issue> _repository;
	private readonly ILogger<ExportAnalyticsQueryHandler> _logger;
	private readonly ExportAnalyticsQueryHandler _sut;

	public ExportAnalyticsQueryHandlerTests()
	{
		_repository = Substitute.For<IRepository<Issue>>();
		_logger = Substitute.For<ILogger<ExportAnalyticsQueryHandler>>();
		_sut = new ExportAnalyticsQueryHandler(_repository, _logger);
	}

	[Fact]
	public async Task ExportAnalytics_ReturnsCsvData()
	{
		// Arrange
		var query = new ExportAnalyticsQuery(null, null);

		var status = new StatusInfo
		{
			Id = ObjectId.GenerateNewId(),
			StatusName = "Open",
			StatusDescription = "Open status",
			DateCreated = DateTime.UtcNow,
			DateModified = null,
			Archived = false,
			ArchivedBy = UserInfo.Empty
		};

		var category = new CategoryInfo
		{
			Id = ObjectId.GenerateNewId(),
			CategoryName = "Bug",
			CategoryDescription = "Bug category",
			DateCreated = DateTime.UtcNow,
			DateModified = null,
			Archived = false,
			ArchivedBy = UserInfo.Empty
		};

		var author = new UserInfo { Id = "user1", Name = "John Doe", Email = "john@example.com" };

		var issues = new List<Issue>
		{
			new()
			{
				Id = ObjectId.GenerateNewId(),
				Title = "Test Issue 1",
				Description = "Test Description 1",
				Status = status,
				Category = category,
				Author = author,
				DateCreated = DateTime.UtcNow.AddDays(-5),
				DateModified = DateTime.UtcNow.AddDays(-3)
			},
			new()
			{
				Id = ObjectId.GenerateNewId(),
				Title = "Test Issue 2",
				Description = "Test Description 2",
				Status = status,
				Category = category,
				Author = author,
				DateCreated = DateTime.UtcNow.AddDays(-2),
				DateModified = null
			}
		};

		_repository.FindAsync(Arg.Any<Expression<Func<Issue, bool>>>(), Arg.Any<CancellationToken>())
			.Returns(Result.Ok<IEnumerable<Issue>>(issues));

		// Act
		var result = await _sut.Handle(query, CancellationToken.None);

		// Assert
		result.Success.Should().BeTrue();
		result.Value.Should().NotBeNull();
		result.Value.Should().NotBeEmpty();

		var csvContent = Encoding.UTF8.GetString(result.Value!);
		csvContent.Should().Contain("ID,Title,Status,Category,Author,Created,Modified,ResolutionHours");
		csvContent.Should().Contain("Test Issue 1");
		csvContent.Should().Contain("Test Issue 2");
		csvContent.Should().Contain("Open");
		csvContent.Should().Contain("Bug");
		csvContent.Should().Contain("John Doe");
	}
}
