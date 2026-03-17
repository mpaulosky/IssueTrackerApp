// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     BulkExportCommandHandlerTests.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTrackerApp
// Project Name :  Domain.Tests
// =======================================================

using System.Text;

using Domain.Abstractions;
using Domain.Features.Issues.Commands.Bulk;

using Microsoft.Extensions.Logging;

using MongoDB.Bson;

namespace Domain.Tests.Features.Issues.Bulk;

/// <summary>
/// Unit tests for BulkExportCommandHandler.
/// </summary>
public sealed class BulkExportCommandHandlerTests
{
	private readonly IRepository<Issue> _repository;
	private readonly ILogger<BulkExportCommandHandler> _logger;
	private readonly BulkExportCommandHandler _sut;

	public BulkExportCommandHandlerTests()
	{
		_repository = Substitute.For<IRepository<Issue>>();
		_logger = Substitute.For<ILogger<BulkExportCommandHandler>>();
		_sut = new BulkExportCommandHandler(_repository, _logger);
	}

	[Fact]
	public async Task BulkExport_ReturnsExportData()
	{
		// Arrange
		var issueIds = new List<string> { "issue1", "issue2", "issue3" };
		var command = new BulkExportCommand(issueIds, "user1");

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
			CategoryDescription = "Bug reports",
			DateCreated = DateTime.UtcNow,
			DateModified = null,
			Archived = false,
			ArchivedBy = UserInfo.Empty
		};

		var author = new UserInfo { Id = "user1", Name = "John Doe", Email = "john@example.com" };

		var issues = issueIds.Select((id, index) => new Issue
		{
			Id = ObjectId.GenerateNewId(),
			Title = $"Issue Title {index + 1}",
			Description = $"Issue Description {index + 1}",
			Status = status,
			Category = category,
			Author = author,
			Archived = false,
			DateCreated = DateTime.UtcNow.AddDays(-(index + 1)),
			DateModified = index == 0 ? DateTime.UtcNow : null
		}).ToList();

		for (var i = 0; i < issueIds.Count; i++)
		{
			_repository.GetByIdAsync(issueIds[i], Arg.Any<CancellationToken>())
				.Returns(Result.Ok(issues[i]));
		}

		// Act
		var result = await _sut.Handle(command, CancellationToken.None);

		// Assert
		result.Success.Should().BeTrue();
		result.Value.Should().NotBeNull();
		result.Value!.TotalExported.Should().Be(3);
		result.Value!.Errors.Should().BeEmpty();
		result.Value!.FileName.Should().Contain("issues_export_");
		result.Value!.FileName.Should().EndWith(".csv");
		result.Value!.CsvContent.Should().NotBeEmpty();

		var csvContent = Encoding.UTF8.GetString(result.Value!.CsvContent);
		csvContent.Should().Contain("Id,Title,Description,Status,Category,Author,DateCreated,DateModified,Archived");
		csvContent.Should().Contain("Issue Title 1");
		csvContent.Should().Contain("Issue Title 2");
		csvContent.Should().Contain("Issue Title 3");
		csvContent.Should().Contain("Open");
		csvContent.Should().Contain("Bug");
		csvContent.Should().Contain("John Doe");
	}

	[Fact]
	public async Task BulkExport_HandlesPartialFailures()
	{
		// Arrange
		var issueIds = new List<string> { "issue1", "issue2-not-found" };
		var command = new BulkExportCommand(issueIds, "user1");

		var validIssue = new Issue
		{
			Id = ObjectId.GenerateNewId(),
			Title = "Valid Issue",
			Description = "Valid Description",
			Status = StatusInfo.Empty,
			Category = CategoryInfo.Empty,
			Author = UserInfo.Empty,
			DateCreated = DateTime.UtcNow
		};

		_repository.GetByIdAsync("issue1", Arg.Any<CancellationToken>())
			.Returns(Result.Ok(validIssue));

		_repository.GetByIdAsync("issue2-not-found", Arg.Any<CancellationToken>())
			.Returns(Result.Fail<Issue>("Issue not found"));

		// Act
		var result = await _sut.Handle(command, CancellationToken.None);

		// Assert
		result.Success.Should().BeTrue();
		result.Value.Should().NotBeNull();
		result.Value!.TotalExported.Should().Be(1);
		result.Value!.Errors.Should().HaveCount(1);
		result.Value!.Errors.First().IssueId.Should().Be("issue2-not-found");
	}
}
