// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     Constants.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : ArticlesSite
// Project Name :  Shared
// =======================================================

namespace Shared.Constants;

public static class Constants
{

	public const string ArticleDatabase = "articlesdb";

	public const string ArticleConnect = "mongodb";

	public const string AdminPolicy = "AdminOnly";

	public const string Cache = "Cache";

	// Backwards-compatible service/resource names moved from the legacy Services class
	public const string Server = "Server";

	// Tests historically expect a `Database` constant with value "articlesdb".
	public const string Database = "articlesdb";

	public const string DatabaseName = "articlesdb";

	public const string DefaultCorsPolicy = "DefaultPolicy";

	public const string OutputCache = "output-cache";

	public const string ServerName = "articlesite-server";

	public const string RedisCache = "RedisCache";

	public const string UserDatabase = "usersDb";

	public const string Website = "WebApp";

	public const string ApiService = "api-service";

	public const string CategoryCacheName = "CategoryData";

	public const string ArticleCacheName = "ArticleData";

}
