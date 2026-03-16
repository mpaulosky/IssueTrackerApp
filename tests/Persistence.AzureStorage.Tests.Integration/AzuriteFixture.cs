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
	private readonly AzuriteContainer? _container;
	private readonly bool _useExternalAzurite;

	public AzuriteFixture()
	{
		// Check if an external Azurite (e.g., CI environment) is available
		var envConnString = Environment.GetEnvironmentVariable("AZURE_STORAGE_CONNECTION_STRING");
		if (!string.IsNullOrEmpty(envConnString))
		{
			_useExternalAzurite = true;
			_container = null;
		}
		else
		{
			_useExternalAzurite = false;
			_container = new AzuriteBuilder("mcr.microsoft.com/azure-storage/azurite:latest")
				.Build();
		}
	}

	public string ConnectionString
	{
		get
		{
			// Use external Azurite from CI environment if available
			var envConnString = Environment.GetEnvironmentVariable("AZURE_STORAGE_CONNECTION_STRING");
			if (!string.IsNullOrEmpty(envConnString))
			{
				return envConnString;
			}
			return _container!.GetConnectionString();
		}
	}

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

	public async Task InitializeAsync()
	{
		if (!_useExternalAzurite && _container != null)
		{
			await _container.StartAsync();
		}
	}

	public async Task DisposeAsync()
	{
		if (!_useExternalAzurite && _container != null)
		{
			await _container.DisposeAsync();
		}
	}
}
