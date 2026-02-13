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
///   Provides helper methods for resolving MongoDB collection names.
/// </summary>
public static class CollectionNames
{
	/// <summary>
	///   Gets the MongoDB collection name for the specified entity type.
	/// </summary>
	/// <param name="entityName">The entity type name.</param>
	/// <returns>The MongoDB collection name.</returns>
	/// <exception cref="ArgumentException">Thrown when an invalid entity name is provided.</exception>
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
