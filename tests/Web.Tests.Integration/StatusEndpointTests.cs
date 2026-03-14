// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     StatusEndpointTests.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTrackerApp
// Project Name :  Web.Tests.Integration
// =======================================================

using System.Net.Http.Json;
using Domain.DTOs;
using Domain.Models;
using MongoDB.Bson;
using Web.Endpoints;

namespace Web.Tests.Integration;

/// <summary>
///   Integration tests for the Status API endpoints.
/// </summary>
[Collection("Integration")]
public sealed class StatusEndpointTests : IntegrationTestBase
{

	public StatusEndpointTests(CustomWebApplicationFactory factory) : base(factory)
	{
	}

	#region GET /api/statuses Tests

	[Fact]
	public async Task GetAllStatuses_ReturnsEmptyList_WhenNoStatusesExist()
	{
		// Arrange
		using var client = CreateAuthenticatedClient();

		// Act
		var response = await client.GetAsync("/api/statuses");

		// Assert
		response.StatusCode.Should().Be(HttpStatusCode.OK);

		var statuses = await response.Content.ReadFromJsonAsync<List<StatusDto>>(JsonOptions);

		statuses.Should().NotBeNull();
		statuses.Should().BeEmpty();
	}

	[Fact]
	public async Task GetAllStatuses_ReturnsAllStatuses_WhenStatusesExist()
	{
		// Arrange
		var seededStatuses = await SeedStatusesAsync();
		using var client = CreateAuthenticatedClient();

		// Act
		var response = await client.GetAsync("/api/statuses");

		// Assert
		response.StatusCode.Should().Be(HttpStatusCode.OK);

		var statuses = await response.Content.ReadFromJsonAsync<List<StatusDto>>(JsonOptions);

		statuses.Should().NotBeNull();
		statuses.Should().HaveCount(seededStatuses.Count);
	}

	[Fact]
	public async Task GetAllStatuses_ExcludesArchivedStatuses_WhenIncludeArchivedIsFalse()
	{
		// Arrange
		await SeedStatusesAsync();
		await SeedArchivedStatusAsync();
		using var client = CreateAuthenticatedClient();

		// Act
		var response = await client.GetAsync("/api/statuses?includeArchived=false");

		// Assert
		response.StatusCode.Should().Be(HttpStatusCode.OK);

		var statuses = await response.Content.ReadFromJsonAsync<List<StatusDto>>(JsonOptions);

		statuses.Should().NotBeNull();
		statuses!.All(s => !s.Archived).Should().BeTrue();
	}

	[Fact]
	public async Task GetAllStatuses_IncludesArchivedStatuses_WhenIncludeArchivedIsTrue()
	{
		// Arrange
		var seededStatuses = await SeedStatusesAsync();
		var archivedStatus = await SeedArchivedStatusAsync();
		using var client = CreateAuthenticatedClient();

		// Act
		var response = await client.GetAsync("/api/statuses?includeArchived=true");

		// Assert
		response.StatusCode.Should().Be(HttpStatusCode.OK);

		var statuses = await response.Content.ReadFromJsonAsync<List<StatusDto>>(JsonOptions);

		statuses.Should().NotBeNull();
		statuses.Should().HaveCount(seededStatuses.Count + 1);
		statuses!.Any(s => s.Id == archivedStatus.Id).Should().BeTrue();
	}

	[Fact]
	public async Task GetAllStatuses_ReturnsStatusesInAlphabeticalOrder()
	{
		// Arrange
		await SeedStatusesAsync();
		using var client = CreateAuthenticatedClient();

		// Act
		var response = await client.GetAsync("/api/statuses");

		// Assert
		response.StatusCode.Should().Be(HttpStatusCode.OK);

		var statuses = await response.Content.ReadFromJsonAsync<List<StatusDto>>(JsonOptions);

		statuses.Should().NotBeNull();
		statuses.Should().BeInAscendingOrder(s => s.StatusName);
	}

	#endregion

	#region GET /api/statuses/{id} Tests

	[Fact]
	public async Task GetStatusById_ReturnsStatus_WhenStatusExists()
	{
		// Arrange
		var seededStatuses = await SeedStatusesAsync();
		var targetStatus = seededStatuses.First();
		using var client = CreateAuthenticatedClient();

		// Act
		var response = await client.GetAsync($"/api/statuses/{targetStatus.Id}");

		// Assert
		response.StatusCode.Should().Be(HttpStatusCode.OK);

		var status = await response.Content.ReadFromJsonAsync<StatusDto>(JsonOptions);

		status.Should().NotBeNull();
		status!.Id.Should().Be(targetStatus.Id);
		status.StatusName.Should().Be(targetStatus.StatusName);
		status.StatusDescription.Should().Be(targetStatus.StatusDescription);
	}

	[Fact]
	public async Task GetStatusById_ReturnsNotFound_WhenStatusDoesNotExist()
	{
		// Arrange
		var nonExistentId = ObjectId.GenerateNewId().ToString();
		using var client = CreateAuthenticatedClient();

		// Act
		var response = await client.GetAsync($"/api/statuses/{nonExistentId}");

		// Assert
		response.StatusCode.Should().Be(HttpStatusCode.NotFound);
	}

	[Fact]
	public async Task GetStatusById_ReturnsBadRequest_WhenIdIsEmpty()
	{
		// Arrange
		using var client = CreateAuthenticatedClient();

		// Act
		var response = await client.GetAsync("/api/statuses/%20");

		// Assert
		response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
	}

	#endregion

	#region POST /api/statuses Tests

	[Fact]
	public async Task CreateStatus_ReturnsCreated_WhenValidRequest()
	{
		// Arrange
		using var client = CreateAuthenticatedClient();
		var request = new CreateStatusRequest("New Status", "A new status description");

		// Act
		var response = await client.PostAsJsonAsync("/api/statuses", request);

		// Assert
		response.StatusCode.Should().Be(HttpStatusCode.Created);

		var createdStatus = await response.Content.ReadFromJsonAsync<StatusDto>(JsonOptions);

		createdStatus.Should().NotBeNull();
		createdStatus!.StatusName.Should().Be(request.StatusName);
		createdStatus.StatusDescription.Should().Be(request.StatusDescription);
		createdStatus.Archived.Should().BeFalse();

		// Verify Location header
		response.Headers.Location.Should().NotBeNull();
		response.Headers.Location!.ToString().Should().Contain("/api/statuses/");
	}

	[Fact]
	public async Task CreateStatus_ReturnsBadRequest_WhenNameIsEmpty()
	{
		// Arrange
		using var client = CreateAuthenticatedClient();
		var request = new CreateStatusRequest("", "A valid description text");

		// Act
		var response = await client.PostAsJsonAsync("/api/statuses", request);

		// Assert
		response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
	}

	[Fact]
	public async Task CreateStatus_ReturnsBadRequest_WhenDescriptionIsEmpty()
	{
		// Arrange
		using var client = CreateAuthenticatedClient();
		var request = new CreateStatusRequest("Valid Name", "");

		// Act
		var response = await client.PostAsJsonAsync("/api/statuses", request);

		// Assert
		response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
	}

	[Fact]
	public async Task CreateStatus_ReturnsBadRequest_WhenNameIsTooShort()
	{
		// Arrange
		using var client = CreateAuthenticatedClient();
		var request = new CreateStatusRequest("A", "A valid description text");

		// Act
		var response = await client.PostAsJsonAsync("/api/statuses", request);

		// Assert
		response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
	}

	[Fact]
	public async Task CreateStatus_ReturnsBadRequest_WhenDescriptionIsTooShort()
	{
		// Arrange
		using var client = CreateAuthenticatedClient();
		var request = new CreateStatusRequest("Valid Name", "Hi");

		// Act
		var response = await client.PostAsJsonAsync("/api/statuses", request);

		// Assert
		response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
	}

	[Fact]
	public async Task CreateStatus_ReturnsBadRequest_WhenNameExceedsMaxLength()
	{
		// Arrange
		using var client = CreateAuthenticatedClient();
		var longName = new string('A', 101);
		var request = new CreateStatusRequest(longName, "A valid description text");

		// Act
		var response = await client.PostAsJsonAsync("/api/statuses", request);

		// Assert
		response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
	}

	[Fact]
	public async Task CreateStatus_ReturnsConflict_WhenDuplicateNameExists()
	{
		// Arrange
		var seededStatuses = await SeedStatusesAsync();
		using var client = CreateAuthenticatedClient();
		var request = new CreateStatusRequest(
			seededStatuses.First().StatusName,
			"A different description");

		// Act
		var response = await client.PostAsJsonAsync("/api/statuses", request);

		// Assert
		response.StatusCode.Should().Be(HttpStatusCode.Conflict);
	}

	[Fact]
	public async Task CreateStatus_RequiresAuthentication()
	{
		// Arrange
		using var client = CreateAnonymousClient();
		var request = new CreateStatusRequest("New Status", "A new status description");

		// Act
		var response = await client.PostAsJsonAsync("/api/statuses", request);

		// Assert
		response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
	}

	#endregion

	#region PUT /api/statuses/{id} Tests

	[Fact]
	public async Task UpdateStatus_ReturnsOk_WhenValidRequest()
	{
		// Arrange
		var seededStatuses = await SeedStatusesAsync();
		var targetStatus = seededStatuses.First();
		using var client = CreateAuthenticatedClient();
		var request = new UpdateStatusRequest("Updated Name", "Updated description text");

		// Act
		var response = await client.PutAsJsonAsync($"/api/statuses/{targetStatus.Id}", request);

		// Assert
		response.StatusCode.Should().Be(HttpStatusCode.OK);

		var updatedStatus = await response.Content.ReadFromJsonAsync<StatusDto>(JsonOptions);

		updatedStatus.Should().NotBeNull();
		updatedStatus!.Id.Should().Be(targetStatus.Id);
		updatedStatus.StatusName.Should().Be(request.StatusName);
		updatedStatus.StatusDescription.Should().Be(request.StatusDescription);
		updatedStatus.DateModified.Should().NotBeNull();
	}

	[Fact]
	public async Task UpdateStatus_ReturnsNotFound_WhenStatusDoesNotExist()
	{
		// Arrange
		var nonExistentId = ObjectId.GenerateNewId().ToString();
		using var client = CreateAuthenticatedClient();
		var request = new UpdateStatusRequest("Updated Name", "Updated description text");

		// Act
		var response = await client.PutAsJsonAsync($"/api/statuses/{nonExistentId}", request);

		// Assert
		response.StatusCode.Should().Be(HttpStatusCode.NotFound);
	}

	[Fact]
	public async Task UpdateStatus_ReturnsBadRequest_WhenNameIsEmpty()
	{
		// Arrange
		var seededStatuses = await SeedStatusesAsync();
		var targetStatus = seededStatuses.First();
		using var client = CreateAuthenticatedClient();
		var request = new UpdateStatusRequest("", "Valid description text");

		// Act
		var response = await client.PutAsJsonAsync($"/api/statuses/{targetStatus.Id}", request);

		// Assert
		response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
	}

	[Fact]
	public async Task UpdateStatus_ReturnsConflict_WhenDuplicateNameExists()
	{
		// Arrange
		var seededStatuses = await SeedStatusesAsync();
		var firstStatus = seededStatuses[0];
		var secondStatus = seededStatuses[1];
		using var client = CreateAuthenticatedClient();
		var request = new UpdateStatusRequest(
			secondStatus.StatusName,
			"Updated description text");

		// Act
		var response = await client.PutAsJsonAsync($"/api/statuses/{firstStatus.Id}", request);

		// Assert
		response.StatusCode.Should().Be(HttpStatusCode.Conflict);
	}

	[Fact]
	public async Task UpdateStatus_RequiresAuthentication()
	{
		// Arrange
		var seededStatuses = await SeedStatusesAsync();
		var targetStatus = seededStatuses.First();
		using var client = CreateAnonymousClient();
		var request = new UpdateStatusRequest("Updated Name", "Updated description text");

		// Act
		var response = await client.PutAsJsonAsync($"/api/statuses/{targetStatus.Id}", request);

		// Assert
		response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
	}

	#endregion

	#region DELETE /api/statuses/{id} Tests

	[Fact]
	public async Task ArchiveStatus_ReturnsOk_WhenStatusExists()
	{
		// Arrange
		var seededStatuses = await SeedStatusesAsync();
		var targetStatus = seededStatuses.First();
		using var client = CreateAuthenticatedClient();

		// Act
		var response = await client.DeleteAsync($"/api/statuses/{targetStatus.Id}");

		// Assert
		response.StatusCode.Should().Be(HttpStatusCode.OK);

		var archivedStatus = await response.Content.ReadFromJsonAsync<StatusDto>(JsonOptions);

		archivedStatus.Should().NotBeNull();
		archivedStatus!.Id.Should().Be(targetStatus.Id);
		archivedStatus.Archived.Should().BeTrue();
		archivedStatus.ArchivedBy.Should().NotBe(UserDto.Empty);
	}

	[Fact]
	public async Task ArchiveStatus_ReturnsNotFound_WhenStatusDoesNotExist()
	{
		// Arrange
		var nonExistentId = ObjectId.GenerateNewId().ToString();
		using var client = CreateAuthenticatedClient();

		// Act
		var response = await client.DeleteAsync($"/api/statuses/{nonExistentId}");

		// Assert
		response.StatusCode.Should().Be(HttpStatusCode.NotFound);
	}

	[Fact]
	public async Task ArchiveStatus_RequiresAuthentication()
	{
		// Arrange
		var seededStatuses = await SeedStatusesAsync();
		var targetStatus = seededStatuses.First();
		using var client = CreateAnonymousClient();

		// Act
		var response = await client.DeleteAsync($"/api/statuses/{targetStatus.Id}");

		// Assert
		response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
	}

	[Fact]
	public async Task ArchiveStatus_SetsArchivedByToCurrentUser()
	{
		// Arrange
		var seededStatuses = await SeedStatusesAsync();
		var targetStatus = seededStatuses.First();
		using var client = CreateAuthenticatedClient();

		// Act
		var response = await client.DeleteAsync($"/api/statuses/{targetStatus.Id}");

		// Assert
		response.StatusCode.Should().Be(HttpStatusCode.OK);

		var archivedStatus = await response.Content.ReadFromJsonAsync<StatusDto>(JsonOptions);

		archivedStatus.Should().NotBeNull();
		archivedStatus!.ArchivedBy.Id.Should().Be(TestAuthHandler.TestUserId);
	}

	#endregion

	#region Helper Methods

	/// <summary>
	///   Seeds an archived status into the database.
	/// </summary>
	private async Task<Status> SeedArchivedStatusAsync()
	{
		await using var context = Factory.CreateDbContext();

		var status = new Status
		{
			Id = ObjectId.GenerateNewId(),
			StatusName = "Archived Status",
			StatusDescription = "An archived status for testing",
			Archived = true,
			ArchivedBy = new UserInfo
			{
				Id = TestAuthHandler.TestUserId,
				Name = TestAuthHandler.TestUserName,
				Email = TestAuthHandler.TestUserEmail
			}
		};

		context.Statuses.Add(status);
		await context.SaveChangesAsync();

		return status;
	}

	#endregion
}
