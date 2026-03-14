// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     ServiceCollectionExtensionsTests.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTrackerApp
// Project Name :  Persistence.MongoDb.Tests
// =======================================================

namespace Persistence.MongoDb.Tests;

/// <summary>
/// Unit tests for ServiceCollectionExtensions dependency injection registration.
/// </summary>
public sealed class ServiceCollectionExtensionsTests
{
	[Fact]
	public void AddMongoDbPersistence_Should_RegisterMongoDbSettings()
	{
		// Arrange
		var config = new ConfigurationBuilder()
			.AddInMemoryCollection(new Dictionary<string, string?>
			{
				["MongoDB:ConnectionString"] = "mongodb://localhost:27017",
				["MongoDB:DatabaseName"] = "test-db"
			})
			.Build();

		var services = new ServiceCollection();

		// Act
		services.AddMongoDbPersistence(config);
		var provider = services.BuildServiceProvider();

		// Assert - Verify we can resolve the settings
		var settings = provider.GetService<IOptions<MongoDbSettings>>();
		settings.Should().NotBeNull();
		settings!.Value.Should().NotBeNull();
	}

	[Fact]
	public void AddMongoDbPersistence_Should_RegisterSettingsValidator()
	{
		// Arrange
		var config = new ConfigurationBuilder()
			.AddInMemoryCollection(new Dictionary<string, string?>
			{
				["MongoDB:ConnectionString"] = "mongodb://localhost:27017",
				["MongoDB:DatabaseName"] = "test-db"
			})
			.Build();

		var services = new ServiceCollection();

		// Act
		services.AddMongoDbPersistence(config);

		// Assert
		services.Should().Contain(sd =>
			sd.ServiceType == typeof(IValidateOptions<MongoDbSettings>));
	}

	[Fact]
	public void AddMongoDbPersistence_Should_RegisterDbContext()
	{
		// Arrange
		var config = new ConfigurationBuilder()
			.AddInMemoryCollection(new Dictionary<string, string?>
			{
				["MongoDB:ConnectionString"] = "mongodb://localhost:27017",
				["MongoDB:DatabaseName"] = "test-db"
			})
			.Build();

		var services = new ServiceCollection();

		// Act
		services.AddMongoDbPersistence(config);

		// Assert
		services.Should().ContainSingle(sd => sd.ServiceType == typeof(IssueTrackerDbContext));
	}

	[Fact]
	public void AddMongoDbPersistence_Should_RegisterDbContextFactory()
	{
		// Arrange
		var config = new ConfigurationBuilder()
			.AddInMemoryCollection(new Dictionary<string, string?>
			{
				["MongoDB:ConnectionString"] = "mongodb://localhost:27017",
				["MongoDB:DatabaseName"] = "test-db"
			})
			.Build();

		var services = new ServiceCollection();

		// Act
		services.AddMongoDbPersistence(config);

		// Assert
		services.Should().ContainSingle(sd => sd.ServiceType == typeof(IDbContextFactory<IssueTrackerDbContext>));
	}

	[Fact]
	public void AddMongoDbPersistence_Should_RegisterGenericRepository()
	{
		// Arrange
		var config = new ConfigurationBuilder()
			.AddInMemoryCollection(new Dictionary<string, string?>
			{
				["MongoDB:ConnectionString"] = "mongodb://localhost:27017",
				["MongoDB:DatabaseName"] = "test-db"
			})
			.Build();

		var services = new ServiceCollection();

		// Act
		services.AddMongoDbPersistence(config);

		// Assert
		services.Should().ContainSingle(sd =>
			sd.ServiceType == typeof(IRepository<>) &&
			sd.ImplementationType == typeof(Repository<>) &&
			sd.Lifetime == ServiceLifetime.Scoped);
	}

	[Fact]
	public void AddMongoDbPersistence_Should_BindSettingsFromConfiguration()
	{
		// Arrange
		var config = new ConfigurationBuilder()
			.AddInMemoryCollection(new Dictionary<string, string?>
			{
				["MongoDB:ConnectionString"] = "mongodb://testserver:27017",
				["MongoDB:DatabaseName"] = "custom-db",
				["MongoDB:MaxConnectionPoolSize"] = "200",
				["MongoDB:ConnectionTimeoutSeconds"] = "60",
				["MongoDB:ServerSelectionTimeoutSeconds"] = "45",
				["MongoDB:MaxRetryAttempts"] = "5"
			})
			.Build();

		var services = new ServiceCollection();

		// Act
		services.AddMongoDbPersistence(config);
		var provider = services.BuildServiceProvider();
		var settings = provider.GetRequiredService<IOptions<MongoDbSettings>>().Value;

		// Assert
		settings.Should().NotBeNull();
		settings.ConnectionString.Should().Be("mongodb://testserver:27017");
		settings.DatabaseName.Should().Be("custom-db");
		settings.MaxConnectionPoolSize.Should().Be(200);
		settings.ConnectionTimeoutSeconds.Should().Be(60);
		settings.ServerSelectionTimeoutSeconds.Should().Be(45);
		settings.MaxRetryAttempts.Should().Be(5);
	}

	[Fact]
	public void AddMongoDbPersistence_Should_ReturnServiceCollection()
	{
		// Arrange
		var config = new ConfigurationBuilder()
			.AddInMemoryCollection(new Dictionary<string, string?>
			{
				["MongoDB:ConnectionString"] = "mongodb://localhost:27017",
				["MongoDB:DatabaseName"] = "test-db"
			})
			.Build();

		var services = new ServiceCollection();

		// Act
		var result = services.AddMongoDbPersistence(config);

		// Assert
		result.Should().BeSameAs(services);
	}

	[Fact]
	public void AddMongoDbPersistence_Should_ValidateSettingsOnStart()
	{
		// Arrange
		var config = new ConfigurationBuilder()
			.AddInMemoryCollection(new Dictionary<string, string?>
			{
				["MongoDB:ConnectionString"] = "",
				["MongoDB:DatabaseName"] = "test-db"
			})
			.Build();

		var services = new ServiceCollection();
		services.AddMongoDbPersistence(config);

		// Act - Build provider and trigger validation
		var act = () =>
		{
			var provider = services.BuildServiceProvider();
			var startupValidators = provider.GetServices<IStartupValidator>();
			foreach (var validator in startupValidators)
			{
				validator.Validate();
			}
		};

		// Assert
		act.Should().Throw<OptionsValidationException>()
			.WithMessage("*MongoDB connection string is not configured*");
	}
}
