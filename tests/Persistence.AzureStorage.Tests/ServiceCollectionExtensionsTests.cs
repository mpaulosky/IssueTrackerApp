// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     ServiceCollectionExtensionsTests.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTrackerApp
// Project Name :  Persistence.AzureStorage.Tests
// =======================================================

namespace Persistence.AzureStorage.Tests;

/// <summary>
///   Unit tests for ServiceCollectionExtensions dependency injection configuration.
/// </summary>
public sealed class ServiceCollectionExtensionsTests
{
	[Fact]
	public void AddAzureBlobStorage_WithValidConnectionString_ShouldRegisterBlobServiceClientAsSingleton()
	{
		// Arrange
		var services = new ServiceCollection();
		var configuration = new ConfigurationBuilder()
			.AddInMemoryCollection(new Dictionary<string, string?>
			{
				["BlobStorage:ConnectionString"] = "DefaultEndpointsProtocol=https;AccountName=test;AccountKey=dGVzdA==;EndpointSuffix=core.windows.net",
				["BlobStorage:ContainerName"] = "my-container",
				["BlobStorage:ThumbnailContainerName"] = "my-thumbnails"
			})
			.Build();

		// Act
		services.AddAzureBlobStorage(configuration);

		// Assert
		var serviceProvider = services.BuildServiceProvider();
		var blobServiceClient = serviceProvider.GetService<BlobServiceClient>();
		blobServiceClient.Should().NotBeNull();

		// Verify singleton by getting service twice
		var blobServiceClient2 = serviceProvider.GetService<BlobServiceClient>();
		blobServiceClient.Should().BeSameAs(blobServiceClient2);
	}

	[Fact]
	public void AddAzureBlobStorage_WithValidConnectionString_ShouldRegisterFileStorageServiceAsScoped()
	{
		// Arrange
		var services = new ServiceCollection();
		// Add a logger factory to satisfy BlobStorageService's ILogger dependency
		services.AddSingleton<ILoggerFactory, NullLoggerFactory>();
		services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));

		var configuration = new ConfigurationBuilder()
			.AddInMemoryCollection(new Dictionary<string, string?>
			{
				["BlobStorage:ConnectionString"] = "DefaultEndpointsProtocol=https;AccountName=test;AccountKey=dGVzdA==;EndpointSuffix=core.windows.net",
				["BlobStorage:ContainerName"] = "my-container"
			})
			.Build();

		// Act
		services.AddAzureBlobStorage(configuration);

		// Assert
		var serviceProvider = services.BuildServiceProvider();
		using var scope = serviceProvider.CreateScope();
		var fileStorageService = scope.ServiceProvider.GetService<IFileStorageService>();
		fileStorageService.Should().NotBeNull();
		fileStorageService.Should().BeOfType<BlobStorageService>();
	}

	[Fact]
	public void AddAzureBlobStorage_ShouldConfigureBlobStorageSettingsFromConfiguration()
	{
		// Arrange
		var services = new ServiceCollection();
		var configuration = new ConfigurationBuilder()
			.AddInMemoryCollection(new Dictionary<string, string?>
			{
				["BlobStorage:ConnectionString"] = "DefaultEndpointsProtocol=https;AccountName=custom;AccountKey=Y3VzdG9t;EndpointSuffix=core.windows.net",
				["BlobStorage:ContainerName"] = "custom-container",
				["BlobStorage:ThumbnailContainerName"] = "custom-thumbnails"
			})
			.Build();

		// Act
		services.AddAzureBlobStorage(configuration);

		// Assert
		var serviceProvider = services.BuildServiceProvider();
		var settings = serviceProvider.GetService<IOptions<BlobStorageSettings>>();
		settings.Should().NotBeNull();
		settings!.Value.ConnectionString.Should().Be("DefaultEndpointsProtocol=https;AccountName=custom;AccountKey=Y3VzdG9t;EndpointSuffix=core.windows.net");
		settings.Value.ContainerName.Should().Be("custom-container");
		settings.Value.ThumbnailContainerName.Should().Be("custom-thumbnails");
	}

	[Fact]
	public void AddAzureBlobStorage_WithEmptyConnectionString_ShouldNotRegisterBlobServiceClient()
	{
		// Arrange
		var services = new ServiceCollection();
		var configuration = new ConfigurationBuilder()
			.AddInMemoryCollection(new Dictionary<string, string?>
			{
				["BlobStorage:ConnectionString"] = "",
				["BlobStorage:ContainerName"] = "my-container"
			})
			.Build();

		// Act
		services.AddAzureBlobStorage(configuration);

		// Assert
		var serviceProvider = services.BuildServiceProvider();
		var blobServiceClient = serviceProvider.GetService<BlobServiceClient>();
		blobServiceClient.Should().BeNull();
	}

	[Fact]
	public void AddAzureBlobStorage_WithEmptyConnectionString_ShouldNotRegisterFileStorageService()
	{
		// Arrange
		var services = new ServiceCollection();
		var configuration = new ConfigurationBuilder()
			.AddInMemoryCollection(new Dictionary<string, string?>
			{
				["BlobStorage:ConnectionString"] = "",
				["BlobStorage:ContainerName"] = "my-container"
			})
			.Build();

		// Act
		services.AddAzureBlobStorage(configuration);

		// Assert
		var serviceProvider = services.BuildServiceProvider();
		var fileStorageService = serviceProvider.GetService<IFileStorageService>();
		fileStorageService.Should().BeNull();
	}

	[Fact]
	public void AddAzureBlobStorage_WithNullConnectionString_ShouldNotRegisterBlobServiceClient()
	{
		// Arrange
		var services = new ServiceCollection();
		var configuration = new ConfigurationBuilder()
			.AddInMemoryCollection(new Dictionary<string, string?>
			{
				["BlobStorage:ContainerName"] = "my-container"
				// ConnectionString intentionally omitted (null)
			})
			.Build();

		// Act
		services.AddAzureBlobStorage(configuration);

		// Assert
		var serviceProvider = services.BuildServiceProvider();
		var blobServiceClient = serviceProvider.GetService<BlobServiceClient>();
		blobServiceClient.Should().BeNull();
	}

	[Fact]
	public void AddAzureBlobStorage_WithNullConnectionString_ShouldNotRegisterFileStorageService()
	{
		// Arrange
		var services = new ServiceCollection();
		var configuration = new ConfigurationBuilder()
			.AddInMemoryCollection(new Dictionary<string, string?>
			{
				["BlobStorage:ContainerName"] = "my-container"
				// ConnectionString intentionally omitted (null)
			})
			.Build();

		// Act
		services.AddAzureBlobStorage(configuration);

		// Assert
		var serviceProvider = services.BuildServiceProvider();
		var fileStorageService = serviceProvider.GetService<IFileStorageService>();
		fileStorageService.Should().BeNull();
	}

	[Fact]
	public void AddAzureBlobStorage_ShouldReturnServiceCollection()
	{
		// Arrange
		var services = new ServiceCollection();
		var configuration = new ConfigurationBuilder()
			.AddInMemoryCollection(new Dictionary<string, string?>
			{
				["BlobStorage:ConnectionString"] = "DefaultEndpointsProtocol=https;AccountName=test;AccountKey=dGVzdA==;EndpointSuffix=core.windows.net"
			})
			.Build();

		// Act
		var result = services.AddAzureBlobStorage(configuration);

		// Assert
		result.Should().BeSameAs(services);
	}
}
