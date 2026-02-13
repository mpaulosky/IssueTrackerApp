// ============================================
// Copyright (c) 2023. All rights reserved.
// File Name :     MongoHealthCheck.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTracker
// Project Name :  IssueTracker.UI
// =============================================

using Microsoft.Extensions.Diagnostics.HealthChecks;

using MongoDB.Bson;
using MongoDB.Driver;

namespace Web.Helpers;

/// <summary>
///   Provides health check functionality for MongoDB database connections.
/// </summary>
public class MongoHealthCheck : IHealthCheck
{
	private readonly IMongoDbContextFactory _factory;

	/// <summary>
	///   Initializes a new instance of the <see cref="MongoHealthCheck" /> class.
	/// </summary>
	/// <param name="factory">The MongoDB context factory.</param>
	public MongoHealthCheck(IMongoDbContextFactory factory)
	{
		_factory = factory;
	}

	/// <summary>
	///   Runs the health check asynchronously.
	/// </summary>
	/// <param name="context">The health check context.</param>
	/// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
	/// <returns>
	///   A task representing the asynchronous operation. The task result contains the health check result.
	/// </returns>
	public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context,
		CancellationToken cancellationToken = default)
	{
		bool healthCheckResult = await CheckMongoDbConnection();

		return healthCheckResult
			? HealthCheckResult.Healthy("MongoDB health check success")
			: HealthCheckResult.Unhealthy("MongoDB health check failure");
	}

	/// <summary>
	///   Checks the MongoDB connection.
	/// </summary>
	/// <returns>
	///   <see langword="true" /> if the MongoDB connection is successful; otherwise, <see langword="false" />.
	/// </returns>
	private async Task<bool> CheckMongoDbConnection()
	{
		try
		{
			await _factory.Database.RunCommandAsync((Command<BsonDocument>)"{ping:1}");
		}
		catch (Exception)
		{
			return false;
		}

		return true;
	}
}