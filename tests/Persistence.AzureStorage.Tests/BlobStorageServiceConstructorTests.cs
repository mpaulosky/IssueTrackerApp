// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     BlobStorageServiceConstructorTests.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTrackerApp
// Project Name :  Persistence.AzureStorage.Tests
// =======================================================

namespace Persistence.AzureStorage.Tests;

/// <summary>
///   Unit tests for BlobStorageService constructor validation.
/// </summary>
public sealed class BlobStorageServiceConstructorTests
{
	[Fact]
	public void Constructor_WhenBlobServiceClientIsNull_ShouldThrowArgumentNullException()
	{
		// Arrange
		BlobServiceClient? blobServiceClient = null;
		var settings = Options.Create(new BlobStorageSettings());
		var logger = Substitute.For<ILogger<BlobStorageService>>();

		// Act
		Action act = () => new BlobStorageService(blobServiceClient!, settings, logger);

		// Assert
		act.Should().Throw<ArgumentNullException>()
			.WithParameterName(nameof(blobServiceClient));
	}

	[Fact]
	public void Constructor_WhenSettingsIsNull_ShouldThrowArgumentNullException()
	{
		// Arrange
		var blobServiceClient = Substitute.For<BlobServiceClient>();
		IOptions<BlobStorageSettings>? settings = null;
		var logger = Substitute.For<ILogger<BlobStorageService>>();

		// Act
		Action act = () => new BlobStorageService(blobServiceClient, settings!, logger);

		// Assert
		act.Should().Throw<ArgumentNullException>()
			.WithParameterName("settings");
	}

	[Fact]
	public void Constructor_WhenLoggerIsNull_ShouldThrowArgumentNullException()
	{
		// Arrange
		var blobServiceClient = Substitute.For<BlobServiceClient>();
		var settings = Options.Create(new BlobStorageSettings());
		ILogger<BlobStorageService>? logger = null;

		// Act
		Action act = () => new BlobStorageService(blobServiceClient, settings, logger!);

		// Assert
		act.Should().Throw<ArgumentNullException>()
			.WithParameterName(nameof(logger));
	}

	[Fact]
	public void Constructor_WhenAllDependenciesAreValid_ShouldCreateInstance()
	{
		// Arrange
		var blobServiceClient = Substitute.For<BlobServiceClient>();
		var settings = Options.Create(new BlobStorageSettings
		{
			ConnectionString = "DefaultEndpointsProtocol=https;AccountName=test;AccountKey=dGVzdA==",
			ContainerName = "test-container",
			ThumbnailContainerName = "test-thumbnails"
		});
		var logger = Substitute.For<ILogger<BlobStorageService>>();

		// Act
		var service = new BlobStorageService(blobServiceClient, settings, logger);

		// Assert
		service.Should().NotBeNull();
		service.Should().BeAssignableTo<IFileStorageService>();
	}
}
