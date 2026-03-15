// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     MockDbSetHelper.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTrackerApp
// Project Name :  Persistence.MongoDb.Tests
// =======================================================

using Microsoft.EntityFrameworkCore.Query;

namespace Persistence.MongoDb.Tests.Helpers;

/// <summary>
///   Helper class for creating mock DbSet instances for unit testing.
/// </summary>
public static class MockDbSetHelper
{
	/// <summary>
	///   Creates a mock DbSet with the provided data that supports async operations.
	/// </summary>
	/// <typeparam name="T">The entity type.</typeparam>
	/// <param name="data">The data to populate the DbSet with.</param>
	/// <returns>A mock DbSet instance.</returns>
	public static DbSet<T> CreateMockDbSet<T>(IEnumerable<T> data) where T : class
	{
		var queryableData = data.AsQueryable();
		var mockDbSet = Substitute.For<DbSet<T>, IQueryable<T>, IAsyncEnumerable<T>>();

		// Setup IQueryable interface
		((IQueryable<T>)mockDbSet).Provider.Returns(new TestAsyncQueryProvider<T>(queryableData.Provider));
		((IQueryable<T>)mockDbSet).Expression.Returns(queryableData.Expression);
		((IQueryable<T>)mockDbSet).ElementType.Returns(queryableData.ElementType);
		((IQueryable<T>)mockDbSet).GetEnumerator().Returns(queryableData.GetEnumerator());

		// Setup IAsyncEnumerable interface
		((IAsyncEnumerable<T>)mockDbSet).GetAsyncEnumerator(Arg.Any<CancellationToken>())
			.Returns(new TestAsyncEnumerator<T>(queryableData.GetEnumerator()));

		return mockDbSet;
	}

	/// <summary>
	///   Creates a mock DbSet with FindAsync support for MongoDB ObjectId.
	/// </summary>
	/// <typeparam name="T">The entity type.</typeparam>
	/// <param name="data">The data to populate the DbSet with.</param>
	/// <param name="keySelector">Function to extract the ObjectId from an entity.</param>
	/// <returns>A mock DbSet instance with FindAsync support.</returns>
	public static DbSet<T> CreateMockDbSetWithFind<T>(
		IEnumerable<T> data,
		Func<T, ObjectId> keySelector) where T : class
	{
		var mockDbSet = CreateMockDbSet(data);
		var dataList = data.ToList();

		// Setup FindAsync for ObjectId
		mockDbSet.FindAsync(Arg.Any<object[]>(), Arg.Any<CancellationToken>())
			.Returns(callInfo =>
			{
				var keyValues = callInfo.Arg<object[]>();
				if (keyValues.Length > 0 && keyValues[0] is ObjectId objectId)
				{
					return ValueTask.FromResult(dataList.FirstOrDefault(e => keySelector(e) == objectId));
				}
				return ValueTask.FromResult<T?>(null);
			});

		return mockDbSet;
	}

	/// <summary>
	///   Test async query provider for IQueryable support with EF Core async operations.
	/// </summary>
	private class TestAsyncQueryProvider<TEntity> : IAsyncQueryProvider
	{
		private readonly IQueryProvider _inner;

		internal TestAsyncQueryProvider(IQueryProvider inner)
		{
			_inner = inner;
		}

		public IQueryable CreateQuery(Expression expression)
		{
			return new TestAsyncEnumerable<TEntity>(expression);
		}

		public IQueryable<TElement> CreateQuery<TElement>(Expression expression)
		{
			return new TestAsyncEnumerable<TElement>(expression);
		}

		public object? Execute(Expression expression)
		{
			return _inner.Execute(expression);
		}

		public TResult Execute<TResult>(Expression expression)
		{
			return _inner.Execute<TResult>(expression);
		}

		public TResult ExecuteAsync<TResult>(Expression expression, CancellationToken cancellationToken = default)
		{
			var resultType = typeof(TResult);

			// Handle TResult that is Task<T>
			if (resultType.IsGenericType && resultType.GetGenericTypeDefinition() == typeof(Task<>))
			{
				var taskValueType = resultType.GetGenericArguments()[0];
				var executeMethod = typeof(IQueryProvider)
					.GetMethod(nameof(IQueryProvider.Execute), 1, new[] { typeof(Expression) })!
					.MakeGenericMethod(taskValueType);

				var result = executeMethod.Invoke(_inner, new object[] { expression });

				var taskFromResultMethod = typeof(Task)
					.GetMethod(nameof(Task.FromResult))!
					.MakeGenericMethod(taskValueType);

				return (TResult)taskFromResultMethod.Invoke(null, new[] { result })!;
			}

			// For other TResult types, execute synchronously and wrap in Task
			var syncResult = _inner.Execute(expression);
			return (TResult)(object)Task.FromResult(syncResult);
		}
	}

	/// <summary>
	///   Test async enumerable for IAsyncEnumerable support.
	/// </summary>
	private class TestAsyncEnumerable<T> : EnumerableQuery<T>, IAsyncEnumerable<T>, IQueryable<T>
	{
		public TestAsyncEnumerable(IEnumerable<T> enumerable)
			: base(enumerable)
		{
		}

		public TestAsyncEnumerable(Expression expression)
			: base(expression)
		{
		}

		public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
		{
			return new TestAsyncEnumerator<T>(this.AsEnumerable().GetEnumerator());
		}

		IQueryProvider IQueryable.Provider => new TestAsyncQueryProvider<T>(this);
	}

	/// <summary>
	///   Test async enumerator for async iteration support.
	/// </summary>
	private class TestAsyncEnumerator<T> : IAsyncEnumerator<T>
	{
		private readonly IEnumerator<T> _inner;

		public TestAsyncEnumerator(IEnumerator<T> inner)
		{
			_inner = inner;
		}

		public ValueTask<bool> MoveNextAsync()
		{
			return ValueTask.FromResult(_inner.MoveNext());
		}

		public T Current => _inner.Current;

		public ValueTask DisposeAsync()
		{
			_inner.Dispose();
			return ValueTask.CompletedTask;
		}
	}
}
