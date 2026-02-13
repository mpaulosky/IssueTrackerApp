// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     DatabaseService.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : ArticleSite
// Project Name :  AppHost
// =======================================================

namespace AppHost;

/// <summary>
///   Extension methods for adding and configuring MongoDB resources with Aspire 9.4.0 features.
/// </summary>
public static class DatabaseService
{
	/// <summary>
	///   Adds MongoDB services to the distributed application builder, including resource tagging, grouping, and improved
	///   seeding logic.
	/// </summary>
	/// <param name="builder">The distributed application builder.</param>
	/// <returns>The MongoDB database resource builder.</returns>
	public static IResourceBuilder<MongoDBDatabaseResource> AddMongoDbServices(
		this IDistributedApplicationBuilder builder)
	{

		var mongoDbConnection = builder.AddParameter("mongoDb-connection", secret: true);
		var databaseName = builder.AddParameter("mongoDb-database", secret: true);

		// Use a valid resource name, not the connection string
		var server = builder.AddMongoDB(Server)
				.WithLifetime(ContainerLifetime.Persistent)
				.WithDataVolume($"{Server}-data", isReadOnly: false)
				.WithEnvironment("MONGODB-CONNECTION-STRING", mongoDbConnection)
				.WithEnvironment("MONGODB-DATABASE-NAME", databaseName)
				.WithMongoExpress();

		var database = server.AddDatabase(DatabaseName);

		return database;
	}
}
