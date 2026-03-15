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
using Persistence.MongoDb.Repositories;

namespace Persistence.MongoDb.Tests;

/// <summary>
///   Tests for Repository constructor.
/// </summary>
public class RepositoryConstructorTests
{

	[Fact]
	public void Constructor_WithNullContext_Should_ThrowArgumentNullException()
	{
		// Arrange
		var logger = Substitute.For<ILogger<Repository<Category>>>();

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
		var mockContext = Substitute.For<IIssueTrackerDbContext>();

		// Act
		Action act = () => new Repository<Category>(mockContext, null!);

		// Assert
		act.Should().Throw<ArgumentNullException>()
			.WithParameterName("logger");
	}

	[Fact]
	public void Constructor_WithValidArguments_Should_NotThrow()
	{
		// Arrange
		var mockContext = Substitute.For<IIssueTrackerDbContext>();
		var logger = Substitute.For<ILogger<Repository<Category>>>();

		// Act
		Action act = () => new Repository<Category>(mockContext, logger);

		// Assert
		act.Should().NotThrow();
	}

	[Fact]
	public void Constructor_Should_SetContextProperty()
	{
		// Arrange
		var mockContext = Substitute.For<IIssueTrackerDbContext>();
		var logger = Substitute.For<ILogger<Repository<Category>>>();

		// Act
		var repository = new TestableRepository<Category>(mockContext, logger);

		// Assert
		repository.ExposedContext.Should().BeSameAs(mockContext);
	}

	[Fact]
	public void Constructor_Should_SetDbSetProperty()
	{
		// Arrange
		var mockContext = Substitute.For<IIssueTrackerDbContext>();
		var mockDbSet = Substitute.For<DbSet<Category>>();
		mockContext.Set<Category>().Returns(mockDbSet);
		var logger = Substitute.For<ILogger<Repository<Category>>>();

		// Act
		var repository = new TestableRepository<Category>(mockContext, logger);

		// Assert
		repository.ExposedDbSet.Should().NotBeNull();
		repository.ExposedDbSet.Should().BeSameAs(mockDbSet);
	}

	[Fact]
	public void Constructor_Should_SetLoggerProperty()
	{
		// Arrange
		var mockContext = Substitute.For<IIssueTrackerDbContext>();
		var logger = Substitute.For<ILogger<Repository<Category>>>();

		// Act
		var repository = new TestableRepository<Category>(mockContext, logger);

		// Assert
		repository.ExposedLogger.Should().BeSameAs(logger);
	}

	/// <summary>
	///   Testable repository that exposes protected fields for testing.
	/// </summary>
	private class TestableRepository<TEntity> : Repository<TEntity> where TEntity : class
	{
		public TestableRepository(IIssueTrackerDbContext context, ILogger<Repository<TEntity>> logger)
			: base(context, logger)
		{
		}

		public IIssueTrackerDbContext ExposedContext => Context;
		public DbSet<TEntity> ExposedDbSet => DbSet;
		public ILogger<Repository<TEntity>> ExposedLogger => Logger;
	}
}
