// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     Constants.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : ArticlesSite
// Project Name :  Shared
// =======================================================

namespace Shared.Constants;

/// <summary>
///   Contains application-wide constant values.
/// </summary>
public static class Constants
{
	/// <summary>
	///   The name of the article database.
	/// </summary>
	public const string ArticleDatabase = "articlesdb";

	/// <summary>
	///   The MongoDB connection resource name.
	/// </summary>
	public const string ArticleConnect = "mongodb";

	/// <summary>
	///   The name of the admin-only authorization policy.
	/// </summary>
	public const string AdminPolicy = "AdminOnly";

	/// <summary>
	///   The name of the cache resource.
	/// </summary>
	public const string Cache = "Cache";

	/// <summary>
	///   The backwards-compatible server resource name.
	/// </summary>
	public const string Server = "Server";

	/// <summary>
	///   The backwards-compatible database name.
	/// </summary>
	public const string Database = "articlesdb";

	/// <summary>
	///   The name of the database.
	/// </summary>
	public const string DatabaseName = "articlesdb";

	/// <summary>
	///   The name of the default CORS policy.
	/// </summary>
	public const string DefaultCorsPolicy = "DefaultPolicy";

	/// <summary>
	///   The name of the output cache resource.
	/// </summary>
	public const string OutputCache = "output-cache";

	/// <summary>
	///   The name of the article site server.
	/// </summary>
	public const string ServerName = "articlesite-server";

	/// <summary>
	///   The name of the Redis cache resource.
	/// </summary>
	public const string RedisCache = "RedisCache";

	/// <summary>
	///   The name of the user database.
	/// </summary>
	public const string UserDatabase = "usersDb";

	/// <summary>
	///   The name of the web application resource.
	/// </summary>
	public const string Website = "WebApp";

	/// <summary>
	///   The name of the API service resource.
	/// </summary>
	public const string ApiService = "api-service";

	/// <summary>
	///   The name of the category cache.
	/// </summary>
	public const string CategoryCacheName = "CategoryData";

	/// <summary>
	///   The name of the article cache.
	/// </summary>
	public const string ArticleCacheName = "ArticleData";
}
