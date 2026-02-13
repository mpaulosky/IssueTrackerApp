// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     CollectionNames.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : ArticlesSite
// Project Name :  Shared
// =======================================================

namespace Shared.Helpers;

/// <summary>
///   CollectionNames class
/// </summary>
public static class CollectionNames
{

	/// <summary>
	///   GetCollectionName method
	/// </summary>
	/// <param name="entityName">string</param>
	/// <returns>string collection name</returns>
	public static string GetCollectionName(string? entityName)
	{

		return entityName switch
		{
			"Category" => "categories",
			"Comment" => "comments",
			"Issue" => "issues",
			"Status" => "statuses",
			"User" => "users",
			_ => throw new ArgumentException($"Invalid entity name provided: {entityName}", nameof(entityName))
		};

	}

}
