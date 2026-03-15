// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     MongoDbSettingsTests.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTrackerApp
// Project Name :  Persistence.MongoDb.Tests
// =======================================================

namespace Persistence.MongoDb.Tests;

/// <summary>
/// Unit tests for MongoDbSettings configuration class and MongoDbSettingsValidator.
/// </summary>
public sealed class MongoDbSettingsTests
{
	[Fact]
	public void SectionName_Should_Be_MongoDB()
	{
		// Arrange & Act
		var sectionName = MongoDbSettings.SectionName;

		// Assert
		sectionName.Should().Be("MongoDB");
	}

	[Fact]
	public void Default_ConnectionString_Should_Be_Empty()
	{
		// Arrange & Act
		var settings = new MongoDbSettings();

		// Assert
		settings.ConnectionString.Should().BeEmpty();
	}

	[Fact]
	public void Default_DatabaseName_Should_Be_IssueTrackerDb()
	{
		// Arrange & Act
		var settings = new MongoDbSettings();

		// Assert
		settings.DatabaseName.Should().Be("issuetracker-db");
	}

	[Fact]
	public void Default_MaxConnectionPoolSize_Should_Be_100()
	{
		// Arrange & Act
		var settings = new MongoDbSettings();

		// Assert
		settings.MaxConnectionPoolSize.Should().Be(100);
	}

	[Fact]
	public void Default_ConnectionTimeoutSeconds_Should_Be_30()
	{
		// Arrange & Act
		var settings = new MongoDbSettings();

		// Assert
		settings.ConnectionTimeoutSeconds.Should().Be(30);
	}

	[Fact]
	public void Default_ServerSelectionTimeoutSeconds_Should_Be_30()
	{
		// Arrange & Act
		var settings = new MongoDbSettings();

		// Assert
		settings.ServerSelectionTimeoutSeconds.Should().Be(30);
	}

	[Fact]
	public void Default_MaxRetryAttempts_Should_Be_3()
	{
		// Arrange & Act
		var settings = new MongoDbSettings();

		// Assert
		settings.MaxRetryAttempts.Should().Be(3);
	}

	[Fact]
	public void Validate_WithValidSettings_Should_NotThrow()
	{
		// Arrange
		var settings = new MongoDbSettings
		{
			ConnectionString = "mongodb://localhost:27017",
			DatabaseName = "test-db"
		};

		// Act
		var act = () => settings.Validate();

		// Assert
		act.Should().NotThrow();
	}

	[Fact]
	public void Validate_WithEmptyConnectionString_Should_ThrowInvalidOperationException()
	{
		// Arrange
		var settings = new MongoDbSettings
		{
			ConnectionString = string.Empty,
			DatabaseName = "test-db"
		};

		// Act
		var act = () => settings.Validate();

		// Assert
		act.Should().Throw<InvalidOperationException>()
			.WithMessage("MongoDB connection string is not configured.");
	}

	[Fact]
	public void Validate_WithNullConnectionString_Should_ThrowInvalidOperationException()
	{
		// Arrange
		var settings = new MongoDbSettings
		{
			ConnectionString = null!,
			DatabaseName = "test-db"
		};

		// Act
		var act = () => settings.Validate();

		// Assert
		act.Should().Throw<InvalidOperationException>()
			.WithMessage("MongoDB connection string is not configured.");
	}

	[Fact]
	public void Validate_WithWhitespaceConnectionString_Should_ThrowInvalidOperationException()
	{
		// Arrange
		var settings = new MongoDbSettings
		{
			ConnectionString = "   ",
			DatabaseName = "test-db"
		};

		// Act
		var act = () => settings.Validate();

		// Assert
		act.Should().Throw<InvalidOperationException>()
			.WithMessage("MongoDB connection string is not configured.");
	}

	[Fact]
	public void Validate_WithEmptyDatabaseName_Should_ThrowInvalidOperationException()
	{
		// Arrange
		var settings = new MongoDbSettings
		{
			ConnectionString = "mongodb://localhost:27017",
			DatabaseName = string.Empty
		};

		// Act
		var act = () => settings.Validate();

		// Assert
		act.Should().Throw<InvalidOperationException>()
			.WithMessage("MongoDB database name is not configured.");
	}

	[Fact]
	public void Validate_WithNullDatabaseName_Should_ThrowInvalidOperationException()
	{
		// Arrange
		var settings = new MongoDbSettings
		{
			ConnectionString = "mongodb://localhost:27017",
			DatabaseName = null!
		};

		// Act
		var act = () => settings.Validate();

		// Assert
		act.Should().Throw<InvalidOperationException>()
			.WithMessage("MongoDB database name is not configured.");
	}

	[Fact]
	public void Validate_WithWhitespaceDatabaseName_Should_ThrowInvalidOperationException()
	{
		// Arrange
		var settings = new MongoDbSettings
		{
			ConnectionString = "mongodb://localhost:27017",
			DatabaseName = "   "
		};

		// Act
		var act = () => settings.Validate();

		// Assert
		act.Should().Throw<InvalidOperationException>()
			.WithMessage("MongoDB database name is not configured.");
	}

	[Fact]
	public void Validator_WithValidSettings_Should_AllowServiceProviderBuild()
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
		services.AddMongoDbPersistence(config);
		var provider = services.BuildServiceProvider();

		// Act
		var act = () => provider.GetRequiredService<IOptions<MongoDbSettings>>().Value;

		// Assert
		act.Should().NotThrow();
	}

	[Fact]
	public void Validator_WithInvalidSettings_Should_ThrowOptionsValidationException()
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

		// Act - Building the provider with validation enabled should throw
		var act = () =>
		{
			var provider = services.BuildServiceProvider();
			// Try to start the validators
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
