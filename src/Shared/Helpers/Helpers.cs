// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     Helpers.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : ArticlesSite
// Project Name :  Shared
// =======================================================

namespace Shared.Helpers;

public static partial class Helpers
{

	private static readonly DateTimeOffset StaticDate = new(2025, 1, 1, 8, 0, 0, TimeSpan.Zero);

	/// <summary>
	///   Gets a static date for testing purposes.
	/// </summary>
	/// <returns>A static date of January 1, 2025, at 08:00 AM.</returns>
	public static DateTimeOffset GetStaticDate()
	{
		return StaticDate;
	}

	/// <summary>
	///   Converts a string to a URL-friendly slug.
	/// </summary>
	/// <param name="item">The string to convert to a slug.</param>
	/// <returns>A URL-encoded slug.</returns>
	[System.Diagnostics.CodeAnalysis.SuppressMessage("Globalization", "CA1308:Normalize strings to uppercase",
			Justification = "URL slugs are conventionally lowercase for SEO and readability")]
	public static string GenerateSlug(this string item)
	{

		if (string.IsNullOrWhiteSpace(item))
		{
			return string.Empty;
		}

		// Lowercase, replace non-alphanumeric sequences with a single underscore, collapse multiple underscores
		string slug = item.ToLowerInvariant();

		// Replace any sequence of non-alphanumeric characters with underscore
		slug = Regex.Replace(slug, "[^a-z0-9]+", "_");

		// Collapse multiple underscores into one
		slug = Regex.Replace(slug, "_+", "_");

		// Trim leading/trailing underscores
		slug = slug.Trim('_');

		// Add trailing underscore only if:
		// 1) the last non-whitespace character in the original string is non-alphanumeric, AND
		// 2) there exists at least one other non-alphanumeric (non-space) character earlier in the string.
		if (!string.IsNullOrEmpty(item) && !string.IsNullOrEmpty(slug) && !slug.EndsWith("_"))
		{
			int last = item.Length - 1;
			while (last >= 0 && char.IsWhiteSpace(item[last])) last--;

			if (last >= 0 && !char.IsLetterOrDigit(item[last]))
			{
				bool hasInternalPunctuation = false;
				for (int j = 0; j < last; j++)
				{
					char ch = item[j];
					if (!char.IsLetterOrDigit(ch) && !char.IsWhiteSpace(ch))
					{
						hasInternalPunctuation = true;
						break;
					}
				}

				if (hasInternalPunctuation)
				{
					slug += "_";
				}
			}
		}

		return slug;

	}

	[GeneratedRegex(@"[^a-z0-9]+")] private static partial Regex MyRegex();

	/// <summary>
	///   Gets a random category name from predefined categories.
	/// </summary>
	/// <returns>A random category name.</returns>
	[System.Diagnostics.CodeAnalysis.SuppressMessage("Security", "CA5394:Do not use insecure randomness",
			Justification = "Used only for test data generation, not security-sensitive operations")]
	public static string GetRandomCategoryName()
	{

		List<string> categories =
		[
				MyCategories.First,
				MyCategories.Second,
				MyCategories.Third,
				MyCategories.Fourth,
				MyCategories.Fifth,
				MyCategories.Sixth,
				MyCategories.Seventh,
				MyCategories.Eighth,
				MyCategories.Ninth
		];

		return categories[Random.Shared.Next(categories.Count)];

	}

}
