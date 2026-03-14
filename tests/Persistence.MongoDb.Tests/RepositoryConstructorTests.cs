// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     RepositoryConstructorTests.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTrackerApp
// Project Name :  Persistence.MongoDb.Tests
// =======================================================

using Domain.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Persistence.MongoDb.Configurations;
using Persistence.MongoDb.Repositories;

namespace Persistence.MongoDb.Tests;

/// <summary>
///   Tests for Repository constructor.
/// </summary>
public class RepositoryConstructorTests
{
	/// <summary>
	///   Helper method to create a test IssueTrackerDbContext.
	/// </summary>
	private static IssueTrackerDbContext CreateTestContext()
	{
		var options = new DbContextOptionsBuilder<IssueTrackerDbContext>()
			.UseMongoDB("mongodb://localhost:27017", "test-db")
			.Options;

		var settings = Options.Create(new MongoDbSettings
		{
			ConnectionString = "mongodb://localhost:27017",
			DatabaseName = "test-db"
		});

		return new IssueTrackerDbContext(options, settings);
	}

	/// <summary>
	///   Helper method to create a test logger.
	/// </summary>
	private static ILogger<Repository<Category>> CreateTestLogger()
	{
		return Substitute.For<ILogger<Repository<Category>>>();
	}

	[Fact]
	public void Constructor_WithNullContext_Should_ThrowArgumentNullException()
	{
		// Arrange
		var logger = CreateTestLogger();

		// Act
		Action act = () => new Repository<Category>(null!, logger);

		// Assert
		act.Should().Throw<ArgumentNullException>()
			.WithParameterName("context");
	}

	[Fact]
	public void Constructor_WithNullLogger_Should_ThrowArgumentNullException()
	{
		// Arrange
		using var context = CreateTestContext();

		// Act
		Action act = () => new Repository<Category>(context, null!);

		// Assert
		act.Should().Throw<ArgumentNullException>()
			.WithParameterName("logger");
	}

	[Fact]
	public void Constructor_WithValidArguments_Should_NotThrow()
	{
		// Arrange
		using var context = CreateTestContext();
		var logger = CreateTestLogger();

		// Act
		Action act = () => new Repository<Category>(context, logger);

		// Assert
		act.Should().NotThrow();
	}

	[Fact]
	public void Constructor_Should_SetContextProperty()
	{
		// Arrange
		using var context = CreateTestContext();
		var logger = CreateTestLogger();

		// Act
		var repository = new TestableRepository<Category>(context, logger);

		// Assert
		repository.ExposedContext.Should().BeSameAs(context);
	}

	[Fact]
	public void Constructor_Should_SetDbSetProperty()
	{
		// Arrange
		using var context = CreateTestContext();
		var logger = CreateTestLogger();

		// Act
		var repository = new TestableRepository<Category>(context, logger);

		// Assert
		repository.ExposedDbSet.Should().NotBeNull();
		repository.ExposedDbSet.Should().BeSameAs(context.Set<Category>());
	}

	[Fact]
	public void Constructor_Should_SetLoggerProperty()
	{
		// Arrange
		using var context = CreateTestContext();
		var logger = CreateTestLogger();

		// Act
		var repository = new TestableRepository<Category>(context, logger);

		// Assert
		repository.ExposedLogger.Should().BeSameAs(logger);
	}

	/// <summary>
	///   Testable repository that exposes protected fields for testing.
	/// </summary>
	private class TestableRepository<TEntity> : Repository<TEntity> where TEntity : class
	{
		public TestableRepository(IssueTrackerDbContext context, ILogger<Repository<TEntity>> logger)
			: base(context, logger)
		{
		}

		public IssueTrackerDbContext ExposedContext => Context;
		public DbSet<TEntity> ExposedDbSet => DbSet;
		public ILogger<Repository<TEntity>> ExposedLogger => Logger;
	}
}
