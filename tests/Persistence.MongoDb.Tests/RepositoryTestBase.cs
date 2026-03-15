// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     RepositoryTestBase.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTrackerApp
// Project Name :  Persistence.MongoDb.Tests
// =======================================================

using Persistence.MongoDb.Tests.Helpers;

namespace Persistence.MongoDb.Tests;

/// <summary>
///   Base test class for Repository unit tests with common setup and mock configurations.
/// </summary>
/// <typeparam name="TEntity">The entity type being tested.</typeparam>
public abstract class RepositoryTestBase<TEntity> where TEntity : class
{
	/// <summary>
	///   Gets the mock database context.
	/// </summary>
	protected IIssueTrackerDbContext MockContext { get; private set; } = null!;

	/// <summary>
	///   Gets the mock DbSet for the entity.
	/// </summary>
	protected DbSet<TEntity> MockDbSet { get; private set; } = null!;

	/// <summary>
	///   Gets the mock logger.
	/// </summary>
	protected ILogger<Repository<TEntity>> MockLogger { get; private set; } = null!;

	/// <summary>
	///   Gets the System Under Test (Repository instance).
	/// </summary>
	protected Repository<TEntity> Sut { get; private set; } = null!;

	/// <summary>
	///   Initializes the test base with common mocks.
	/// </summary>
	protected RepositoryTestBase()
	{
		MockContext = Substitute.For<IIssueTrackerDbContext>();
		MockLogger = Substitute.For<ILogger<Repository<TEntity>>>();
	}

	/// <summary>
	///   Sets up an empty DbSet for the entity.
	/// </summary>
	protected void SetupEmptyDbSet()
	{
		SetupDbSetWithData(Enumerable.Empty<TEntity>());
	}

	/// <summary>
	///   Sets up the DbSet with the provided data.
	/// </summary>
	/// <param name="data">The data to populate the DbSet with.</param>
	protected void SetupDbSetWithData(IEnumerable<TEntity> data)
	{
		MockDbSet = MockDbSetHelper.CreateMockDbSet(data);
		MockContext.Set<TEntity>().Returns(MockDbSet);
		Sut = new Repository<TEntity>(MockContext, MockLogger);
	}

	/// <summary>
	///   Sets up the DbSet with FindAsync support for entities with ObjectId.
	/// </summary>
	/// <param name="data">The data to populate the DbSet with.</param>
	/// <param name="keySelector">Function to extract the ObjectId from an entity.</param>
	protected void SetupDbSetWithFind(IEnumerable<TEntity> data, Func<TEntity, ObjectId> keySelector)
	{
		MockDbSet = MockDbSetHelper.CreateMockDbSetWithFind(data, keySelector);
		MockContext.Set<TEntity>().Returns(MockDbSet);
		Sut = new Repository<TEntity>(MockContext, MockLogger);
	}

	/// <summary>
	///   Sets up the DbSet to throw an exception when accessed.
	/// </summary>
	/// <param name="ex">The exception to throw.</param>
	protected void SetupDbSetToThrow(Exception ex)
	{
		MockDbSet = Substitute.For<DbSet<TEntity>>();
		MockDbSet.When(x => x.GetAsyncEnumerator(Arg.Any<CancellationToken>()))
			.Do(_ => throw ex);
		MockContext.Set<TEntity>().Returns(MockDbSet);
		Sut = new Repository<TEntity>(MockContext, MockLogger);
	}

	/// <summary>
	///   Sets up SaveChangesAsync to complete successfully.
	/// </summary>
	/// <param name="affectedRows">Number of rows affected (default: 1).</param>
	protected void SetupSaveChangesAsync(int affectedRows = 1)
	{
		MockContext.SaveChangesAsync(Arg.Any<CancellationToken>())
			.Returns(Task.FromResult(affectedRows));
	}

	/// <summary>
	///   Sets up SaveChangesAsync to throw an exception.
	/// </summary>
	/// <param name="ex">The exception to throw.</param>
	protected void SetupSaveChangesToThrow(Exception ex)
	{
		MockContext.SaveChangesAsync(Arg.Any<CancellationToken>())
			.Returns<Task<int>>(_ => throw ex);
	}

	/// <summary>
	///   Verifies that SaveChangesAsync was called exactly once.
	/// </summary>
	protected void VerifySaveChangesCalledOnce()
	{
		MockContext.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
	}

	/// <summary>
	///   Verifies that SaveChangesAsync was never called.
	/// </summary>
	protected void VerifySaveChangesNotCalled()
	{
		MockContext.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
	}

	/// <summary>
	///   Verifies that the logger logged an error.
	/// </summary>
	protected void VerifyErrorLogged()
	{
		MockLogger.Received().Log(
			LogLevel.Error,
			Arg.Any<EventId>(),
			Arg.Any<object>(),
			Arg.Any<Exception>(),
			Arg.Any<Func<object, Exception?, string>>());
	}

	/// <summary>
	///   Verifies that the logger logged information.
	/// </summary>
	protected void VerifyInformationLogged()
	{
		MockLogger.Received().Log(
			LogLevel.Information,
			Arg.Any<EventId>(),
			Arg.Any<object>(),
			Arg.Any<Exception?>(),
			Arg.Any<Func<object, Exception?, string>>());
	}
}
