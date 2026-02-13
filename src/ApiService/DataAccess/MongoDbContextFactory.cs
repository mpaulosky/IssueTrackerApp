// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     MongoDbContextFactory.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : ArticlesSite
// Project Name :  Web
// =======================================================

namespace ApiService.DataAccess;

/// <summary>
///   A factory for <see cref="MongoDbContext" /> that can create instances
///   with proper configuration.
/// </summary>
public sealed class MongoDbContextFactory : IMongoDbContextFactory
{

	public IMongoDbContext CreateDbContext()
	{
		// Build a minimal configuration that mirrors how the app resolves the connection string.
		IConfigurationBuilder builder = new ConfigurationBuilder()
				.SetBasePath(Directory.GetCurrentDirectory())
				.AddJsonFile("appsettings.json", true)
				.AddJsonFile("appsettings.Development.json", true)
				.AddEnvironmentVariables();

		IConfigurationRoot configuration = builder.Build();

		string? connectionString = configuration["DefaultConnection"]
															?? configuration.GetConnectionString("DefaultConnection")
															?? configuration["ConnectionStrings:DefaultConnection"]
															?? Environment.GetEnvironmentVariable("DefaultConnection");

		if (string.IsNullOrWhiteSpace(connectionString))
		{
			// For development, use a default connection string
			connectionString = "mongodb://localhost:27017";
		}

		string? databaseName = configuration["DatabaseName"]
													?? configuration["ConnectionStrings:DatabaseName"]
													?? Environment.GetEnvironmentVariable("DatabaseName")
													?? "ArticleSiteDb";

		// Create MongoDB client from connection string
		MongoClient mongoClient = new(connectionString);

		return new MongoDbContext(mongoClient, databaseName);
	}

	public IMongoDbContext CreateDbContext(string[] args)
	{
		// For compatibility with tools that may pass args, delegate to the parameterless method
		return CreateDbContext();
	}

}
