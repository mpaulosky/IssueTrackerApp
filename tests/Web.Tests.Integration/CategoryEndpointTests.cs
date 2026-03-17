// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     CategoryEndpointTests.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTrackerApp
// Project Name :  Web.Tests.Integration
// =======================================================

using Domain.DTOs;
using Domain.Models;

using MongoDB.Bson;

using Web.Endpoints;

namespace Web.Tests.Integration;

/// <summary>
///   Integration tests for Category API endpoints.
/// </summary>
[Collection("Integration")]
public sealed class CategoryEndpointTests : IntegrationTestBase
{
	public CategoryEndpointTests(CustomWebApplicationFactory factory) : base(factory)
	{
	}

	#region GET /api/categories - List All Categories

	[Fact]
	public async Task GetCategories_ReturnsEmptyList_WhenNoCategoriesExist()
	{
		// Arrange
		using var client = CreateAuthenticatedClient();

		// Act
		var response = await client.GetAsync("/api/categories");

		// Assert
		response.StatusCode.Should().Be(HttpStatusCode.OK);
		var categories = await response.Content.ReadFromJsonAsync<List<CategoryDto>>(JsonOptions);
		categories.Should().NotBeNull();
		categories.Should().BeEmpty();
	}

	[Fact]
	public async Task GetCategories_ReturnsAllCategories_WhenCategoriesExist()
	{
		// Arrange
		var seededCategories = await SeedCategoriesAsync();
		using var client = CreateAuthenticatedClient();

		// Act
		var response = await client.GetAsync("/api/categories");

		// Assert
		response.StatusCode.Should().Be(HttpStatusCode.OK);
		var categories = await response.Content.ReadFromJsonAsync<List<CategoryDto>>(JsonOptions);
		categories.Should().NotBeNull();
		categories.Should().HaveCount(seededCategories.Count);
		categories!.Select(c => c.CategoryName).Should()
			.Contain(seededCategories.Select(c => c.CategoryName));
	}

	[Fact]
	public async Task GetCategories_ExcludesArchivedByDefault()
	{
		// Arrange
		var seededCategories = await SeedCategoriesAsync();

		// Archive one category
		await using var context = CreateDbContext();
		var categoryToArchive = context.Categories.First();
		categoryToArchive.Archived = true;
		await context.SaveChangesAsync();

		using var client = CreateAuthenticatedClient();

		// Act
		var response = await client.GetAsync("/api/categories");

		// Assert
		response.StatusCode.Should().Be(HttpStatusCode.OK);
		var categories = await response.Content.ReadFromJsonAsync<List<CategoryDto>>(JsonOptions);
		categories.Should().NotBeNull();
		categories.Should().HaveCount(seededCategories.Count - 1);
		categories!.Should().NotContain(c => c.CategoryName == categoryToArchive.CategoryName);
	}

	[Fact]
	public async Task GetCategories_IncludesArchivedWhenRequested()
	{
		// Arrange
		var seededCategories = await SeedCategoriesAsync();

		// Archive one category
		await using var context = CreateDbContext();
		var categoryToArchive = context.Categories.First();
		categoryToArchive.Archived = true;
		await context.SaveChangesAsync();

		using var client = CreateAuthenticatedClient();

		// Act
		var response = await client.GetAsync("/api/categories?includeArchived=true");

		// Assert
		response.StatusCode.Should().Be(HttpStatusCode.OK);
		var categories = await response.Content.ReadFromJsonAsync<List<CategoryDto>>(JsonOptions);
		categories.Should().NotBeNull();
		categories.Should().HaveCount(seededCategories.Count);
	}

	#endregion

	#region GET /api/categories/{id} - Get Single Category

	[Fact]
	public async Task GetCategoryById_ReturnsCategory_WhenCategoryExists()
	{
		// Arrange
		var seededCategories = await SeedCategoriesAsync();
		var targetCategory = seededCategories.First();
		using var client = CreateAuthenticatedClient();

		// Act
		var response = await client.GetAsync($"/api/categories/{targetCategory.Id}");

		// Assert
		response.StatusCode.Should().Be(HttpStatusCode.OK);
		var category = await response.Content.ReadFromJsonAsync<CategoryDto>(JsonOptions);
		category.Should().NotBeNull();
		category!.CategoryName.Should().Be(targetCategory.CategoryName);
		category.CategoryDescription.Should().Be(targetCategory.CategoryDescription);
	}

	[Fact]
	public async Task GetCategoryById_ReturnsNotFound_WhenCategoryDoesNotExist()
	{
		// Arrange
		var nonExistentId = ObjectId.GenerateNewId().ToString();
		using var client = CreateAuthenticatedClient();

		// Act
		var response = await client.GetAsync($"/api/categories/{nonExistentId}");

		// Assert
		response.StatusCode.Should().Be(HttpStatusCode.NotFound);
	}

	[Fact]
	public async Task GetCategoryById_ReturnsNotFound_WhenIdIsInvalid()
	{
		// Arrange
		using var client = CreateAuthenticatedClient();

		// Act
		var response = await client.GetAsync("/api/categories/invalid-id");

		// Assert
		response.StatusCode.Should().Be(HttpStatusCode.NotFound);
	}

	#endregion

	#region POST /api/categories - Create Category

	[Fact]
	public async Task CreateCategory_ReturnsCreated_WhenValidRequest()
	{
		// Arrange
		using var client = CreateAuthenticatedClient("Admin");
		var request = new CreateCategoryRequest("New Category", "A new test category description");

		// Act
		var response = await client.PostAsJsonAsync("/api/categories", request);

		// Assert
		response.StatusCode.Should().Be(HttpStatusCode.Created);
		var category = await response.Content.ReadFromJsonAsync<CategoryDto>(JsonOptions);
		category.Should().NotBeNull();
		category!.CategoryName.Should().Be(request.CategoryName);
		category.CategoryDescription.Should().Be(request.CategoryDescription);
		category.Archived.Should().BeFalse();
		response.Headers.Location.Should().NotBeNull();
	}

	[Fact]
	public async Task CreateCategory_ReturnsBadRequest_WhenNameIsEmpty()
	{
		// Arrange
		using var client = CreateAuthenticatedClient("Admin");
		var request = new CreateCategoryRequest("", "Description");

		// Act
		var response = await client.PostAsJsonAsync("/api/categories", request);

		// Assert
		response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
	}

	[Fact]
	public async Task CreateCategory_ReturnsBadRequest_WhenDescriptionIsEmpty()
	{
		// Arrange
		using var client = CreateAuthenticatedClient("Admin");
		var request = new CreateCategoryRequest("Valid Name", "");

		// Act
		var response = await client.PostAsJsonAsync("/api/categories", request);

		// Assert
		response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
	}

	[Fact]
	public async Task CreateCategory_ReturnsConflict_WhenDuplicateNameExists()
	{
		// Arrange
		var seededCategories = await SeedCategoriesAsync();
		var existingName = seededCategories.First().CategoryName;
		using var client = CreateAuthenticatedClient("Admin");
		var request = new CreateCategoryRequest(existingName, "Different description");

		// Act
		var response = await client.PostAsJsonAsync("/api/categories", request);

		// Assert
		response.StatusCode.Should().Be(HttpStatusCode.Conflict);
	}

	[Fact]
	public async Task CreateCategory_ReturnsConflict_WhenDuplicateNameExistsWithDifferentCase()
	{
		// Arrange
		var seededCategories = await SeedCategoriesAsync();
		var existingName = seededCategories.First().CategoryName.ToUpper();
		using var client = CreateAuthenticatedClient("Admin");
		var request = new CreateCategoryRequest(existingName, "Different description");

		// Act
		var response = await client.PostAsJsonAsync("/api/categories", request);

		// Assert
		response.StatusCode.Should().Be(HttpStatusCode.Conflict);
	}

	[Fact]
	public async Task CreateCategory_RequiresAdminRole()
	{
		// Arrange
		using var client = CreateAuthenticatedClient("User"); // Non-admin
		var request = new CreateCategoryRequest("New Category", "Description for new category");

		// Act
		var response = await client.PostAsJsonAsync("/api/categories", request);

		// Assert
		response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
	}

	#endregion

	#region PUT /api/categories/{id} - Update Category

	[Fact]
	public async Task UpdateCategory_ReturnsOk_WhenValidRequest()
	{
		// Arrange
		var seededCategories = await SeedCategoriesAsync();
		var targetCategory = seededCategories.First();
		using var client = CreateAuthenticatedClient("Admin");
		var request = new UpdateCategoryRequest("Updated Name", "Updated description text");

		// Act
		var response = await client.PutAsJsonAsync($"/api/categories/{targetCategory.Id}", request);

		// Assert
		response.StatusCode.Should().Be(HttpStatusCode.OK);
		var category = await response.Content.ReadFromJsonAsync<CategoryDto>(JsonOptions);
		category.Should().NotBeNull();
		category!.CategoryName.Should().Be(request.CategoryName);
		category.CategoryDescription.Should().Be(request.CategoryDescription);
		category.DateModified.Should().NotBeNull();
	}

	[Fact]
	public async Task UpdateCategory_ReturnsNotFound_WhenCategoryDoesNotExist()
	{
		// Arrange
		var nonExistentId = ObjectId.GenerateNewId().ToString();
		using var client = CreateAuthenticatedClient("Admin");
		var request = new UpdateCategoryRequest("Updated Name", "Updated description text");

		// Act
		var response = await client.PutAsJsonAsync($"/api/categories/{nonExistentId}", request);

		// Assert
		response.StatusCode.Should().Be(HttpStatusCode.NotFound);
	}

	[Fact]
	public async Task UpdateCategory_ReturnsBadRequest_WhenNameIsEmpty()
	{
		// Arrange
		var seededCategories = await SeedCategoriesAsync();
		var targetCategory = seededCategories.First();
		using var client = CreateAuthenticatedClient("Admin");
		var request = new UpdateCategoryRequest("", "Valid description");

		// Act
		var response = await client.PutAsJsonAsync($"/api/categories/{targetCategory.Id}", request);

		// Assert
		response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
	}

	[Fact]
	public async Task UpdateCategory_ReturnsBadRequest_WhenDescriptionIsEmpty()
	{
		// Arrange
		var seededCategories = await SeedCategoriesAsync();
		var targetCategory = seededCategories.First();
		using var client = CreateAuthenticatedClient("Admin");
		var request = new UpdateCategoryRequest("Valid Name", "");

		// Act
		var response = await client.PutAsJsonAsync($"/api/categories/{targetCategory.Id}", request);

		// Assert
		response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
	}

	[Fact]
	public async Task UpdateCategory_ReturnsConflict_WhenDuplicateNameExists()
	{
		// Arrange
		var seededCategories = await SeedCategoriesAsync();
		var targetCategory = seededCategories.First();
		var otherCategory = seededCategories.Skip(1).First();
		using var client = CreateAuthenticatedClient("Admin");
		var request = new UpdateCategoryRequest(otherCategory.CategoryName, "Updated description text");

		// Act
		var response = await client.PutAsJsonAsync($"/api/categories/{targetCategory.Id}", request);

		// Assert
		response.StatusCode.Should().Be(HttpStatusCode.Conflict);
	}

	[Fact]
	public async Task UpdateCategory_AllowsSameNameForSameCategory()
	{
		// Arrange
		var seededCategories = await SeedCategoriesAsync();
		var targetCategory = seededCategories.First();
		using var client = CreateAuthenticatedClient("Admin");
		var request = new UpdateCategoryRequest(targetCategory.CategoryName, "Updated description only");

		// Act
		var response = await client.PutAsJsonAsync($"/api/categories/{targetCategory.Id}", request);

		// Assert
		response.StatusCode.Should().Be(HttpStatusCode.OK);
	}

	[Fact]
	public async Task UpdateCategory_RequiresAdminRole()
	{
		// Arrange
		var seededCategories = await SeedCategoriesAsync();
		var targetCategory = seededCategories.First();
		using var client = CreateAuthenticatedClient("User"); // Non-admin
		var request = new UpdateCategoryRequest("Updated Name", "Updated description text");

		// Act
		var response = await client.PutAsJsonAsync($"/api/categories/{targetCategory.Id}", request);

		// Assert
		response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
	}

	#endregion

	#region DELETE /api/categories/{id} - Archive Category

	[Fact]
	public async Task ArchiveCategory_ReturnsOk_WhenCategoryExists()
	{
		// Arrange
		var seededCategories = await SeedCategoriesAsync();
		var targetCategory = seededCategories.First();
		using var client = CreateAuthenticatedClient("Admin");

		// Act
		var response = await client.DeleteAsync($"/api/categories/{targetCategory.Id}");

		// Assert
		response.StatusCode.Should().Be(HttpStatusCode.OK);
		var category = await response.Content.ReadFromJsonAsync<CategoryDto>(JsonOptions);
		category.Should().NotBeNull();
		category!.Archived.Should().BeTrue();
	}

	[Fact]
	public async Task ArchiveCategory_ReturnsNotFound_WhenCategoryDoesNotExist()
	{
		// Arrange
		var nonExistentId = ObjectId.GenerateNewId().ToString();
		using var client = CreateAuthenticatedClient("Admin");

		// Act
		var response = await client.DeleteAsync($"/api/categories/{nonExistentId}");

		// Assert
		response.StatusCode.Should().Be(HttpStatusCode.NotFound);
	}

	[Fact]
	public async Task ArchiveCategory_RequiresAdminRole()
	{
		// Arrange
		var seededCategories = await SeedCategoriesAsync();
		var targetCategory = seededCategories.First();
		using var client = CreateAuthenticatedClient("User"); // Non-admin

		// Act
		var response = await client.DeleteAsync($"/api/categories/{targetCategory.Id}");

		// Assert
		response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
	}

	[Fact]
	public async Task ArchiveCategory_SetsArchivedByInformation()
	{
		// Arrange
		var seededCategories = await SeedCategoriesAsync();
		var targetCategory = seededCategories.First();
		using var client = CreateAuthenticatedClient("Admin");

		// Act
		var response = await client.DeleteAsync($"/api/categories/{targetCategory.Id}");

		// Assert
		response.StatusCode.Should().Be(HttpStatusCode.OK);
		var category = await response.Content.ReadFromJsonAsync<CategoryDto>(JsonOptions);
		category.Should().NotBeNull();
		category!.ArchivedBy.Should().NotBeNull();
		category.ArchivedBy!.Id.Should().NotBeEmpty();
	}

	[Fact]
	public async Task ArchiveCategory_ExcludesCategoryFromDefaultList()
	{
		// Arrange
		var seededCategories = await SeedCategoriesAsync();
		var targetCategory = seededCategories.First();
		using var adminClient = CreateAuthenticatedClient("Admin");

		// Act - Archive the category
		var archiveResponse = await adminClient.DeleteAsync($"/api/categories/{targetCategory.Id}");
		archiveResponse.StatusCode.Should().Be(HttpStatusCode.OK);

		// Act - Get all categories
		var listResponse = await adminClient.GetAsync("/api/categories");

		// Assert
		listResponse.StatusCode.Should().Be(HttpStatusCode.OK);
		var categories = await listResponse.Content.ReadFromJsonAsync<List<CategoryDto>>(JsonOptions);
		categories.Should().NotBeNull();
		categories!.Should().NotContain(c => c.Id.ToString() == targetCategory.Id.ToString());
	}

	#endregion

	#region Anonymous Access Tests

	[Fact]
	public async Task GetCategories_AllowsAnonymousAccess()
	{
		// Arrange
		await SeedCategoriesAsync();
		using var client = CreateAnonymousClient();

		// Act
		var response = await client.GetAsync("/api/categories");

		// Assert
		response.StatusCode.Should().Be(HttpStatusCode.OK);
	}

	[Fact]
	public async Task GetCategoryById_AllowsAnonymousAccess()
	{
		// Arrange
		var seededCategories = await SeedCategoriesAsync();
		var targetCategory = seededCategories.First();
		using var client = CreateAnonymousClient();

		// Act
		var response = await client.GetAsync($"/api/categories/{targetCategory.Id}");

		// Assert
		response.StatusCode.Should().Be(HttpStatusCode.OK);
	}

	[Fact]
	public async Task CreateCategory_DeniesAnonymousAccess()
	{
		// Arrange
		using var client = CreateAnonymousClient();
		var request = new CreateCategoryRequest("New Category", "Description for new category");

		// Act
		var response = await client.PostAsJsonAsync("/api/categories", request);

		// Assert
		response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
	}

	[Fact]
	public async Task UpdateCategory_DeniesAnonymousAccess()
	{
		// Arrange
		var seededCategories = await SeedCategoriesAsync();
		var targetCategory = seededCategories.First();
		using var client = CreateAnonymousClient();
		var request = new UpdateCategoryRequest("Updated Name", "Updated description text");

		// Act
		var response = await client.PutAsJsonAsync($"/api/categories/{targetCategory.Id}", request);

		// Assert
		response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
	}

	[Fact]
	public async Task ArchiveCategory_DeniesAnonymousAccess()
	{
		// Arrange
		var seededCategories = await SeedCategoriesAsync();
		var targetCategory = seededCategories.First();
		using var client = CreateAnonymousClient();

		// Act
		var response = await client.DeleteAsync($"/api/categories/{targetCategory.Id}");

		// Assert
		response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
	}

	#endregion
}
