// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     AzuriteFixture.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTrackerApp
// Project Name :  Persistence.AzureStorage.Tests.Integration
// =======================================================

namespace Persistence.AzureStorage.Tests.Integration;

/// <summary>
///   xUnit fixture that starts an Azurite Docker container for integration tests.
/// </summary>
public sealed class AzuriteFixture : IAsyncLifetime
{
	private readonly AzuriteContainer _container;

	public AzuriteFixture()
	{
		_container = new AzuriteBuilder("mcr.microsoft.com/azure-storage/azurite:latest")
			.Build();
	}

	public string ConnectionString => _container.GetConnectionString();

	public BlobServiceClient CreateBlobServiceClient() => new(ConnectionString);

	public BlobStorageService CreateBlobStorageService(
		string containerName = "issue-attachments",
		string thumbnailContainerName = "issue-attachments-thumbnails")
	{
		var settings = Options.Create(new BlobStorageSettings
		{
			ConnectionString = ConnectionString,
			ContainerName = containerName,
			ThumbnailContainerName = thumbnailContainerName
		});

		return new BlobStorageService(
			CreateBlobServiceClient(),
			settings,
			NullLogger<BlobStorageService>.Instance);
	}

	public async Task InitializeAsync() => await _container.StartAsync();

	public async Task DisposeAsync() => await _container.DisposeAsync();
}
