// ============================================
// Copyright (c) 2026. All rights reserved.
// File Name :     CacheIntegrationTests.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueManager
// Project Name :  Web.Tests.Integration
// =============================================

using System.Text;
using System.Text.Json;

using Domain.Abstractions;
using Domain.DTOs;
using Domain.Models;

using Microsoft.Extensions.Caching.Distributed;

using MongoDB.Bson;
using MongoDB.Driver;

using Web.Endpoints;
using Web.Features;
using Web.Services;

namespace Web.Tests.Integration;

/// <summary>
///   Integration tests that verify cache warm/cold/invalidation behaviour
///   for the full stack: HTTP request → service → cache → MongoDB.
///   Issue #237.
/// </summary>
[Collection("Integration")]
public sealed class CacheIntegrationTests : IntegrationTestBase
{
	public CacheIntegrationTests(CustomWebApplicationFactory factory) : base(factory)
	{
	}

	// ── helpers ───────────────────────────────────────────────────────────────

	private IDistributedCache GetDistributedCache() =>
		Factory.Services.GetRequiredService<IDistributedCache>();

	private static bool KeyExistsInCache(IDistributedCache cache, string key)
	{
		// IDistributedCache.Get returns null when the key is absent
		var bytes = cache.Get(key);
		return bytes is not null;
	}

	// ── #1 — GetCategories warm / cold ────────────────────────────────────────

	/// <summary>
	///   Calls GET /api/categories twice.
	///   After the first call the DB records are deleted.  The second call must still
	///   return the original data — proving it came from cache, not MongoDB.
	/// </summary>
	[Fact]
	public async Task GetCategories_SecondRequest_HitsCacheNotDatabase()
	{
		// Arrange
		var seeded = await SeedCategoriesAsync();
		using var client = CreateAuthenticatedClient();
		var cache = GetDistributedCache();

		// Act — first request warms the cache
		var resp1 = await client.GetAsync("/api/categories");
		resp1.StatusCode.Should().Be(HttpStatusCode.OK);
		var data1 = await resp1.Content.ReadFromJsonAsync<List<CategoryDto>>(JsonOptions);

		// Assert — cache key was populated
		const string cacheKey = "categories_list_False";
		KeyExistsInCache(cache, cacheKey).Should().BeTrue(
			"CategoryService should have written the list to the distributed cache after the first request");

		// Arrange — delete all categories from MongoDB so a DB re-hit returns nothing
		var mongoClient = new MongoClient(Factory.MongoConnectionString);
		var db = mongoClient.GetDatabase(Factory.DatabaseName);
		await db.GetCollection<MongoDB.Bson.BsonDocument>("Categories")
			.DeleteManyAsync(MongoDB.Driver.FilterDefinition<MongoDB.Bson.BsonDocument>.Empty);

		// Act — second request should serve from cache (not empty DB)
		var resp2 = await client.GetAsync("/api/categories");
		resp2.StatusCode.Should().Be(HttpStatusCode.OK);
		var data2 = await resp2.Content.ReadFromJsonAsync<List<CategoryDto>>(JsonOptions);

		// Assert — cached data returned even though DB is now empty
		data2.Should().NotBeNull();
		data2!.Count.Should().Be(data1!.Count,
			"the second request must serve cached data — if it re-queried the DB it would return 0 rows");
		data2.Select(c => c.CategoryName).Should()
			.BeEquivalentTo(data1.Select(c => c.CategoryName));
	}

	// ── #2 — CreateCategory invalidates cache ────────────────────────────────

	/// <summary>
	///   Warms the categories cache, POSTs a new category, then GETs again.
	///   Asserts the new category appears in the response (cache was invalidated).
	/// </summary>
	[Fact]
	public async Task CreateCategory_InvalidatesCache()
	{
		// Arrange — warm the cache with seed data
		await SeedCategoriesAsync();
		using var userClient = CreateAuthenticatedClient("User");
		using var adminClient = CreateAuthenticatedClient("Admin");

		var warmResp = await userClient.GetAsync("/api/categories");
		warmResp.StatusCode.Should().Be(HttpStatusCode.OK);
		var warmData = await warmResp.Content.ReadFromJsonAsync<List<CategoryDto>>(JsonOptions);

		// Act — create a new category (should invalidate the cache)
		var newCategory = new CreateCategoryRequest("Sprint5NewCategory", "Added in Sprint 5 test");
		var createResp = await adminClient.PostAsJsonAsync("/api/categories", newCategory);
		createResp.StatusCode.Should().Be(HttpStatusCode.Created);

		// Act — re-fetch categories
		var afterResp = await userClient.GetAsync("/api/categories");
		afterResp.StatusCode.Should().Be(HttpStatusCode.OK);
		var afterData = await afterResp.Content.ReadFromJsonAsync<List<CategoryDto>>(JsonOptions);

		// Assert — new category present (stale cache was not served)
		afterData.Should().NotBeNull();
		afterData!.Count.Should().Be(warmData!.Count + 1);
		afterData.Should().Contain(c => c.CategoryName == "Sprint5NewCategory");
	}

	// ── #3 — GetIssues second request returns cached page ────────────────────

	/// <summary>
	///   GETs paginated issues twice, asserts the cache key for the page
	///   exists after the first call.
	/// </summary>
	[Fact]
	public async Task GetIssues_SecondRequest_ReturnsCachedPage()
	{
		// Arrange
		var (categories, statuses) = await SeedTestDataAsync();
		await SeedIssuesAsync(categories[0], statuses[0], 3);

		var issueService = GetService<IIssueService>();
		var cache = GetDistributedCache();

		// Act — first request warms cache
		var result1 = await issueService.GetIssuesAsync(page: 1, pageSize: 10);
		result1.Success.Should().BeTrue();

		// Assert — a versioned cache key now exists
		var cacheHelper = GetService<DistributedCacheHelper>();
		var version = await cacheHelper.GetVersionAsync("issues_version");
		var expectedKey = $"issues_list_{version}_1_10|||False";
		KeyExistsInCache(cache, expectedKey).Should().BeTrue(
			"IssueService should populate a versioned cache key after the first GetIssuesAsync call");

		// Act — second request
		var result2 = await issueService.GetIssuesAsync(page: 1, pageSize: 10);
		result2.Success.Should().BeTrue();

		// Assert — both pages return the same total
		result2.Value!.Total.Should().Be(result1.Value!.Total);
		result2.Value.Items.Count.Should().Be(result1.Value.Items.Count);
	}

	// ── #4 — CreateIssue bumps issues_version ────────────────────────────────

	/// <summary>
	///   Warms the issues cache, POSTs a new issue, then verifies
	///   the <c>issues_version</c> key was bumped in <see cref="IDistributedCache" />.
	/// </summary>
	[Fact]
	public async Task CreateIssue_BumpsIssueVersion()
	{
		// Arrange
		var (categories, statuses) = await SeedTestDataAsync();
		await SeedIssuesAsync(categories[0], statuses[0], 2);

		var issueService = GetService<IIssueService>();
		var cacheHelper  = GetService<DistributedCacheHelper>();

		// Warm the cache (writes issues_version if absent)
		await issueService.GetIssuesAsync(page: 1, pageSize: 10);

		var versionBefore = await cacheHelper.GetVersionAsync("issues_version");

		// Act — create a new issue via service (triggers BumpVersionAsync internally)
		var author = new UserDto(
			TestAuthHandler.TestUserId,
			TestAuthHandler.TestUserName,
			TestAuthHandler.TestUserEmail);
		var category = new CategoryDto(
			categories[0].Id,
			categories[0].CategoryName,
			categories[0].CategoryDescription,
			categories[0].DateCreated,
			categories[0].DateModified,
			categories[0].Archived,
			UserDto.Empty);

		var createResult = await issueService.CreateIssueAsync(
			"Cache-Bump Test Issue",
			"Version bump validation",
			category,
			author);
		createResult.Success.Should().BeTrue();

		// Assert — version counter was incremented
		var versionAfter = await cacheHelper.GetVersionAsync("issues_version");
		versionAfter.Should().BeGreaterThan(versionBefore,
			"creating an issue must bump the issues_version cache key so stale paginated pages are abandoned");
	}

	// ── #5 — GetComments second request hits cache ────────────────────────────

	/// <summary>
	///   GETs comments for an issue twice; asserts the cache key is present
	///   after the first call.
	/// </summary>
	[Fact]
	public async Task GetComments_SecondRequest_HitsCacheNotDatabase()
	{
		// Arrange
		var (categories, statuses) = await SeedTestDataAsync();
		var issue = await SeedIssueAsync(categories[0], statuses[0]);

		// Seed a comment directly
		await using var ctx = CreateDbContext();
		var comment = new Comment
		{
			Id          = ObjectId.GenerateNewId(),
			Title       = "Integration cache comment",
			Description = "Written to test cache behaviour",
			IssueId     = issue.Id,
			Author = new UserInfo
			{
				Id    = TestAuthHandler.TestUserId,
				Name  = TestAuthHandler.TestUserName,
				Email = TestAuthHandler.TestUserEmail
			}
		};
		ctx.Comments.Add(comment);
		await ctx.SaveChangesAsync();

		var cache = GetDistributedCache();
		using var client = CreateAuthenticatedClient();

		// Act — first GET warms the cache
		var issueIdStr = issue.Id.ToString();
		var resp1 = await client.GetAsync($"/api/issues/{issueIdStr}/comments");
		resp1.StatusCode.Should().Be(HttpStatusCode.OK);

		// Assert — cache key was populated
		var expectedKey = $"comments_issue_{issue.Id}";
		KeyExistsInCache(cache, expectedKey).Should().BeTrue(
			"CommentService should cache comments after the first request");

		// Act — second GET
		var resp2 = await client.GetAsync($"/api/issues/{issueIdStr}/comments");
		resp2.StatusCode.Should().Be(HttpStatusCode.OK);
		var data2 = await resp2.Content.ReadFromJsonAsync<List<CommentDto>>(JsonOptions);

		// Assert — same data returned
		data2.Should().NotBeNull();
		data2!.Should().ContainSingle(c => c.Title == "Integration cache comment");
	}
}
