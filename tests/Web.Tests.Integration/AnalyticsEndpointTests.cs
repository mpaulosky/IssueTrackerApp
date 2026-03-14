// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     AnalyticsEndpointTests.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTrackerApp
// Project Name :  Web.Tests.Integration
// =======================================================

using Domain.DTOs;
using Domain.DTOs.Analytics;
using Domain.Models;
using MongoDB.Bson;
using Persistence.MongoDb;
using Web.Services;

namespace Web.Tests.Integration;

/// <summary>
/// Integration tests for Analytics service endpoints against a real MongoDB instance.
/// Tests cover all analytics operations: summary, by-status, by-category, over-time,
/// resolution-times, top-contributors, and export functionality.
/// </summary>
[Collection("Integration")]
public sealed class AnalyticsEndpointTests : IntegrationTestBase
{
	public AnalyticsEndpointTests(CustomWebApplicationFactory factory) : base(factory)
	{
	}

	#region GET /api/analytics/summary Tests

	[Fact]
	public async Task GetAnalyticsSummary_EmptyDatabase_ReturnsZeroValues()
	{
		// Arrange
		var analyticsService = GetService<IAnalyticsService>();

		// Act
		var result = await analyticsService.GetAnalyticsSummaryAsync();

		// Assert
		result.Success.Should().BeTrue();
		result.Value.Should().NotBeNull();
		result.Value!.TotalIssues.Should().Be(0);
		result.Value.OpenIssues.Should().Be(0);
		result.Value.ClosedIssues.Should().Be(0);
		result.Value.AverageResolutionHours.Should().Be(0);
		result.Value.ByStatus.Should().BeEmpty();
		result.Value.ByCategory.Should().BeEmpty();
		result.Value.OverTime.Should().BeEmpty();
		result.Value.TopContributors.Should().BeEmpty();
	}

	[Fact]
	public async Task GetAnalyticsSummary_WithSeededData_ReturnsCorrectAggregations()
	{
		// Arrange
		var analyticsService = GetService<IAnalyticsService>();
		var (categories, statuses) = await SeedTestDataAsync();

		// Seed multiple issues with different statuses
		var openStatus = statuses.First(s => s.StatusName == "Open");
		var closedStatus = statuses.First(s => s.StatusName == "Closed");
		var bugCategory = categories.First(c => c.CategoryName == "Bug");

		await SeedIssuesAsync(bugCategory, openStatus, 3);
		await SeedIssuesAsync(bugCategory, closedStatus, 2);

		// Act
		var result = await analyticsService.GetAnalyticsSummaryAsync();

		// Assert
		result.Success.Should().BeTrue();
		result.Value.Should().NotBeNull();
		result.Value!.TotalIssues.Should().Be(5);
		result.Value.OpenIssues.Should().Be(3);
		result.Value.ClosedIssues.Should().Be(2);
	}

	[Fact]
	public async Task GetAnalyticsSummary_WithDateRange_FiltersCorrectly()
	{
		// Arrange
		var analyticsService = GetService<IAnalyticsService>();
		var (categories, statuses) = await SeedTestDataAsync();
		var category = categories.First();
		var status = statuses.First();

		// Seed issues
		await SeedIssuesAsync(category, status, 3);

		var startDate = DateTime.UtcNow.AddDays(-1);
		var endDate = DateTime.UtcNow.AddDays(1);

		// Act
		var result = await analyticsService.GetAnalyticsSummaryAsync(startDate, endDate);

		// Assert
		result.Success.Should().BeTrue();
		result.Value.Should().NotBeNull();
		result.Value!.TotalIssues.Should().Be(3);
	}

	#endregion

	#region GET /api/analytics/by-status Tests

	[Fact]
	public async Task GetIssuesByStatus_EmptyDatabase_ReturnsEmptyArray()
	{
		// Arrange
		var analyticsService = GetService<IAnalyticsService>();

		// Act
		var result = await analyticsService.GetIssuesByStatusAsync();

		// Assert
		result.Success.Should().BeTrue();
		result.Value.Should().NotBeNull();
		result.Value.Should().BeEmpty();
	}

	[Fact]
	public async Task GetIssuesByStatus_WithSeededData_ReturnsCorrectGrouping()
	{
		// Arrange
		var analyticsService = GetService<IAnalyticsService>();
		var (categories, statuses) = await SeedTestDataAsync();

		var openStatus = statuses.First(s => s.StatusName == "Open");
		var inProgressStatus = statuses.First(s => s.StatusName == "In Progress");
		var closedStatus = statuses.First(s => s.StatusName == "Closed");
		var category = categories.First();

		await SeedIssuesAsync(category, openStatus, 5);
		await SeedIssuesAsync(category, inProgressStatus, 3);
		await SeedIssuesAsync(category, closedStatus, 2);

		// Act
		var result = await analyticsService.GetIssuesByStatusAsync();

		// Assert
		result.Success.Should().BeTrue();
		result.Value.Should().NotBeNull();
		result.Value.Should().HaveCount(3);
		result.Value.Should().Contain(x => x.Status == "Open" && x.Count == 5);
		result.Value.Should().Contain(x => x.Status == "In Progress" && x.Count == 3);
		result.Value.Should().Contain(x => x.Status == "Closed" && x.Count == 2);
	}

	[Fact]
	public async Task GetIssuesByStatus_WithDateFilter_ReturnsFilteredResults()
	{
		// Arrange
		var analyticsService = GetService<IAnalyticsService>();
		var (categories, statuses) = await SeedTestDataAsync();
		var category = categories.First();
		var status = statuses.First();

		await SeedIssuesAsync(category, status, 4);

		var startDate = DateTime.UtcNow.AddDays(-1);
		var endDate = DateTime.UtcNow.AddDays(1);

		// Act
		var result = await analyticsService.GetIssuesByStatusAsync(startDate, endDate);

		// Assert
		result.Success.Should().BeTrue();
		result.Value.Should().NotBeNull();
		result.Value.Should().HaveCountGreaterThanOrEqualTo(1);
	}

	#endregion

	#region GET /api/analytics/by-category Tests

	[Fact]
	public async Task GetIssuesByCategory_EmptyDatabase_ReturnsEmptyArray()
	{
		// Arrange
		var analyticsService = GetService<IAnalyticsService>();

		// Act
		var result = await analyticsService.GetIssuesByCategoryAsync();

		// Assert
		result.Success.Should().BeTrue();
		result.Value.Should().NotBeNull();
		result.Value.Should().BeEmpty();
	}

	[Fact]
	public async Task GetIssuesByCategory_WithSeededData_ReturnsCorrectGrouping()
	{
		// Arrange
		var analyticsService = GetService<IAnalyticsService>();
		var (categories, statuses) = await SeedTestDataAsync();

		var bugCategory = categories.First(c => c.CategoryName == "Bug");
		var featureCategory = categories.First(c => c.CategoryName == "Feature");
		var enhancementCategory = categories.First(c => c.CategoryName == "Enhancement");
		var status = statuses.First();

		await SeedIssuesAsync(bugCategory, status, 10);
		await SeedIssuesAsync(featureCategory, status, 6);
		await SeedIssuesAsync(enhancementCategory, status, 4);

		// Act
		var result = await analyticsService.GetIssuesByCategoryAsync();

		// Assert
		result.Success.Should().BeTrue();
		result.Value.Should().NotBeNull();
		result.Value.Should().HaveCount(3);
		result.Value.Should().Contain(x => x.Category == "Bug" && x.Count == 10);
		result.Value.Should().Contain(x => x.Category == "Feature" && x.Count == 6);
		result.Value.Should().Contain(x => x.Category == "Enhancement" && x.Count == 4);
	}

	[Fact]
	public async Task GetIssuesByCategory_WithDateFilter_ReturnsFilteredResults()
	{
		// Arrange
		var analyticsService = GetService<IAnalyticsService>();
		var (categories, statuses) = await SeedTestDataAsync();
		var category = categories.First();
		var status = statuses.First();

		await SeedIssuesAsync(category, status, 5);

		var startDate = DateTime.UtcNow.AddDays(-7);
		var endDate = DateTime.UtcNow.AddDays(1);

		// Act
		var result = await analyticsService.GetIssuesByCategoryAsync(startDate, endDate);

		// Assert
		result.Success.Should().BeTrue();
		result.Value.Should().NotBeNull();
		result.Value.Should().HaveCountGreaterThanOrEqualTo(1);
	}

	#endregion

	#region GET /api/analytics/over-time Tests

	[Fact]
	public async Task GetIssuesOverTime_EmptyDatabase_ReturnsEmptyArray()
	{
		// Arrange
		var analyticsService = GetService<IAnalyticsService>();

		// Act
		var result = await analyticsService.GetIssuesOverTimeAsync();

		// Assert
		result.Success.Should().BeTrue();
		result.Value.Should().NotBeNull();
		result.Value.Should().BeEmpty();
	}

	[Fact]
	public async Task GetIssuesOverTime_WithSeededData_ReturnsTimeSeriesData()
	{
		// Arrange
		var analyticsService = GetService<IAnalyticsService>();
		var (categories, statuses) = await SeedTestDataAsync();
		var category = categories.First();
		var status = statuses.First();

		await SeedIssuesAsync(category, status, 5);

		// Act
		var result = await analyticsService.GetIssuesOverTimeAsync();

		// Assert
		result.Success.Should().BeTrue();
		result.Value.Should().NotBeNull();
		result.Value.Should().HaveCountGreaterThanOrEqualTo(1);
		result.Value.Should().AllSatisfy(x =>
		{
			x.Date.Should().BeBefore(DateTime.UtcNow.AddDays(1));
			x.Created.Should().BeGreaterThanOrEqualTo(0);
			x.Closed.Should().BeGreaterThanOrEqualTo(0);
		});
	}

	[Fact]
	public async Task GetIssuesOverTime_WithDateRange_FiltersCorrectly()
	{
		// Arrange
		var analyticsService = GetService<IAnalyticsService>();
		var (categories, statuses) = await SeedTestDataAsync();
		var category = categories.First();
		var status = statuses.First();

		await SeedIssuesAsync(category, status, 3);

		var startDate = DateTime.UtcNow.AddDays(-30);
		var endDate = DateTime.UtcNow.AddDays(1);

		// Act
		var result = await analyticsService.GetIssuesOverTimeAsync(startDate, endDate);

		// Assert
		result.Success.Should().BeTrue();
		result.Value.Should().NotBeNull();
		result.Value.Should().AllSatisfy(x =>
		{
			x.Date.Should().BeOnOrAfter(startDate.Date);
			x.Date.Should().BeOnOrBefore(endDate.Date);
		});
	}

	#endregion

	#region GET /api/analytics/resolution-times Tests

	[Fact]
	public async Task GetResolutionTimes_EmptyDatabase_ReturnsEmptyArray()
	{
		// Arrange
		var analyticsService = GetService<IAnalyticsService>();

		// Act
		var result = await analyticsService.GetResolutionTimesAsync();

		// Assert
		result.Success.Should().BeTrue();
		result.Value.Should().NotBeNull();
		result.Value.Should().BeEmpty();
	}

	[Fact]
	public async Task GetResolutionTimes_WithClosedIssues_ReturnsAverageResolutionByCategory()
	{
		// Arrange
		var analyticsService = GetService<IAnalyticsService>();
		var (categories, statuses) = await SeedTestDataAsync();

		var bugCategory = categories.First(c => c.CategoryName == "Bug");
		var closedStatus = statuses.First(s => s.StatusName == "Closed");

		// Seed closed issues
		await SeedIssuesWithResolutionAsync(bugCategory, closedStatus, 3);

		// Act
		var result = await analyticsService.GetResolutionTimesAsync();

		// Assert
		result.Success.Should().BeTrue();
		result.Value.Should().NotBeNull();
		// Only categories with closed issues should appear
		result.Value.Should().AllSatisfy(x =>
		{
			x.Category.Should().NotBeNullOrEmpty();
			x.AverageHours.Should().BeGreaterThanOrEqualTo(0);
		});
	}

	[Fact]
	public async Task GetResolutionTimes_WithDateFilter_ReturnsFilteredResults()
	{
		// Arrange
		var analyticsService = GetService<IAnalyticsService>();
		var (categories, statuses) = await SeedTestDataAsync();

		var category = categories.First();
		var closedStatus = statuses.First(s => s.StatusName == "Closed");

		await SeedIssuesWithResolutionAsync(category, closedStatus, 2);

		var startDate = DateTime.UtcNow.AddDays(-7);
		var endDate = DateTime.UtcNow.AddDays(1);

		// Act
		var result = await analyticsService.GetResolutionTimesAsync(startDate, endDate);

		// Assert
		result.Success.Should().BeTrue();
		result.Value.Should().NotBeNull();
	}

	#endregion

	#region GET /api/analytics/top-contributors Tests

	[Fact]
	public async Task GetTopContributors_EmptyDatabase_ReturnsEmptyArray()
	{
		// Arrange
		var analyticsService = GetService<IAnalyticsService>();

		// Act
		var result = await analyticsService.GetTopContributorsAsync();

		// Assert
		result.Success.Should().BeTrue();
		result.Value.Should().NotBeNull();
		result.Value.Should().BeEmpty();
	}

	[Fact]
	public async Task GetTopContributors_WithSeededData_ReturnsContributorStats()
	{
		// Arrange
		var analyticsService = GetService<IAnalyticsService>();
		var (categories, statuses) = await SeedTestDataAsync();

		var category = categories.First();
		var closedStatus = statuses.First(s => s.StatusName == "Closed");

		// Seed issues with the test user as author
		await SeedIssuesAsync(category, closedStatus, 5);

		// Act
		var result = await analyticsService.GetTopContributorsAsync(topCount: 10);

		// Assert
		result.Success.Should().BeTrue();
		result.Value.Should().NotBeNull();
		// Should contain contributors based on closed issues
	}

	[Fact]
	public async Task GetTopContributors_WithTopCount_ReturnsLimitedResults()
	{
		// Arrange
		var analyticsService = GetService<IAnalyticsService>();
		var (categories, statuses) = await SeedTestDataAsync();

		var category = categories.First();
		var status = statuses.First();

		await SeedIssuesWithMultipleAuthorsAsync(category, status, 10);

		// Act
		var result = await analyticsService.GetTopContributorsAsync(topCount: 3);

		// Assert
		result.Success.Should().BeTrue();
		result.Value.Should().NotBeNull();
		result.Value!.Count.Should().BeLessThanOrEqualTo(3);
	}

	[Fact]
	public async Task GetTopContributors_WithDateFilter_ReturnsFilteredResults()
	{
		// Arrange
		var analyticsService = GetService<IAnalyticsService>();
		var (categories, statuses) = await SeedTestDataAsync();

		var category = categories.First();
		var closedStatus = statuses.First(s => s.StatusName == "Closed");

		await SeedIssuesAsync(category, closedStatus, 4);

		var startDate = DateTime.UtcNow.AddDays(-30);
		var endDate = DateTime.UtcNow.AddDays(1);

		// Act
		var result = await analyticsService.GetTopContributorsAsync(startDate, endDate, topCount: 5);

		// Assert
		result.Success.Should().BeTrue();
		result.Value.Should().NotBeNull();
	}

	#endregion

	#region GET /api/analytics/export Tests

	[Fact]
	public async Task ExportAnalytics_EmptyDatabase_ReturnsValidCsvWithHeaders()
	{
		// Arrange
		var analyticsService = GetService<IAnalyticsService>();

		// Act
		var result = await analyticsService.ExportAnalyticsAsync();

		// Assert
		result.Success.Should().BeTrue();
		result.Value.Should().NotBeNull();
		result.Value.Should().NotBeEmpty();

		// Verify it's valid CSV (at least has headers)
		var csvContent = System.Text.Encoding.UTF8.GetString(result.Value!);
		csvContent.Should().NotBeNullOrEmpty();
	}

	[Fact]
	public async Task ExportAnalytics_WithSeededData_ReturnsPopulatedCsv()
	{
		// Arrange
		var analyticsService = GetService<IAnalyticsService>();
		var (categories, statuses) = await SeedTestDataAsync();

		var category = categories.First();
		var status = statuses.First();
		await SeedIssuesAsync(category, status, 5);

		// Act
		var result = await analyticsService.ExportAnalyticsAsync();

		// Assert
		result.Success.Should().BeTrue();
		result.Value.Should().NotBeNull();
		result.Value!.Length.Should().BeGreaterThan(0);

		var csvContent = System.Text.Encoding.UTF8.GetString(result.Value);
		csvContent.Should().NotBeNullOrEmpty();
	}

	[Fact]
	public async Task ExportAnalytics_WithDateRange_ReturnsFilteredExport()
	{
		// Arrange
		var analyticsService = GetService<IAnalyticsService>();
		var (categories, statuses) = await SeedTestDataAsync();

		var category = categories.First();
		var status = statuses.First();
		await SeedIssuesAsync(category, status, 3);

		var startDate = DateTime.UtcNow.AddDays(-7);
		var endDate = DateTime.UtcNow.AddDays(1);

		// Act
		var result = await analyticsService.ExportAnalyticsAsync(startDate, endDate);

		// Assert
		result.Success.Should().BeTrue();
		result.Value.Should().NotBeNull();
		result.Value!.Length.Should().BeGreaterThan(0);
	}

	#endregion

	#region Edge Cases and Error Handling Tests

	[Fact]
	public async Task GetAnalyticsSummary_FutureDateRange_ReturnsEmptyResults()
	{
		// Arrange
		var analyticsService = GetService<IAnalyticsService>();
		var (categories, statuses) = await SeedTestDataAsync();
		var category = categories.First();
		var status = statuses.First();

		await SeedIssuesAsync(category, status, 5);

		// Use future date range
		var startDate = DateTime.UtcNow.AddYears(1);
		var endDate = DateTime.UtcNow.AddYears(2);

		// Act
		var result = await analyticsService.GetAnalyticsSummaryAsync(startDate, endDate);

		// Assert
		result.Success.Should().BeTrue();
		result.Value!.TotalIssues.Should().Be(0);
	}

	[Fact]
	public async Task GetIssuesByStatus_PastDateRange_ReturnsEmptyResults()
	{
		// Arrange
		var analyticsService = GetService<IAnalyticsService>();
		var (categories, statuses) = await SeedTestDataAsync();
		var category = categories.First();
		var status = statuses.First();

		await SeedIssuesAsync(category, status, 5);

		// Use past date range before any issues were created
		var startDate = DateTime.UtcNow.AddYears(-10);
		var endDate = DateTime.UtcNow.AddYears(-9);

		// Act
		var result = await analyticsService.GetIssuesByStatusAsync(startDate, endDate);

		// Assert
		result.Success.Should().BeTrue();
		result.Value.Should().BeEmpty();
	}

	[Fact]
	public async Task GetAnalyticsSummary_ConcurrentRequests_HandlesCorrectly()
	{
		// Arrange
		var analyticsService = GetService<IAnalyticsService>();
		var (categories, statuses) = await SeedTestDataAsync();
		var category = categories.First();
		var status = statuses.First();

		await SeedIssuesAsync(category, status, 10);

		// Act - Execute multiple concurrent requests
		var tasks = Enumerable.Range(0, 5)
			.Select(_ => analyticsService.GetAnalyticsSummaryAsync())
			.ToList();

		var results = await Task.WhenAll(tasks);

		// Assert - All should succeed with consistent data
		results.Should().AllSatisfy(r =>
		{
			r.Success.Should().BeTrue();
			r.Value!.TotalIssues.Should().Be(10);
		});
	}

	#endregion

	#region Helper Methods

	/// <summary>
	/// Seeds issues with simulated resolution (DateModified set for closed status).
	/// </summary>
	private async Task SeedIssuesWithResolutionAsync(
		Category category,
		Status status,
		int count)
	{
		await using var context = Factory.CreateDbContext();

		var author = new UserInfo
		{
			Id = TestAuthHandler.TestUserId,
			Name = TestAuthHandler.TestUserName,
			Email = TestAuthHandler.TestUserEmail
		};

		var categoryInfo = new CategoryInfo
		{
			Id = category.Id,
			CategoryName = category.CategoryName,
			CategoryDescription = category.CategoryDescription
		};
		var statusInfo = new StatusInfo
		{
			Id = status.Id,
			StatusName = status.StatusName,
			StatusDescription = status.StatusDescription
		};

		var issues = new List<Issue>();
		for (var i = 1; i <= count; i++)
		{
			var createdDate = DateTime.UtcNow.AddHours(-24 * i);
			issues.Add(new Issue
			{
				Id = ObjectId.GenerateNewId(),
				Title = $"Resolved Issue {i}",
				Description = $"Resolved issue description {i}",
				Category = categoryInfo,
				Status = statusInfo,
				Author = author,
				DateCreated = createdDate,
				DateModified = DateTime.UtcNow.AddHours(-12 * i) // Simulates resolution time
			});
		}

		context.Issues.AddRange(issues);
		await context.SaveChangesAsync();
	}

	/// <summary>
	/// Seeds issues with multiple different authors for contributor testing.
	/// </summary>
	private async Task SeedIssuesWithMultipleAuthorsAsync(
		Category category,
		Status status,
		int count)
	{
		await using var context = Factory.CreateDbContext();

		var categoryInfo = new CategoryInfo
		{
			Id = category.Id,
			CategoryName = category.CategoryName,
			CategoryDescription = category.CategoryDescription
		};
		var statusInfo = new StatusInfo
		{
			Id = status.Id,
			StatusName = status.StatusName,
			StatusDescription = status.StatusDescription
		};

		var issues = new List<Issue>();
		for (var i = 1; i <= count; i++)
		{
			var author = new UserInfo
			{
				Id = $"auth0|user-{i}",
				Name = $"User {i}",
				Email = $"user{i}@example.com"
			};

			issues.Add(new Issue
			{
				Id = ObjectId.GenerateNewId(),
				Title = $"Multi-Author Issue {i}",
				Description = $"Issue created by User {i}",
				Category = categoryInfo,
				Status = statusInfo,
				Author = author
			});
		}

		context.Issues.AddRange(issues);
		await context.SaveChangesAsync();
	}

	#endregion
}
