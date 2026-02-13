// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     Enum.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : ArticlesSite
// Project Name :  Shared
// =======================================================

namespace Shared.Enums;

/// <summary>
///   Defines user roles.
/// </summary>
public enum Roles
{
	/// <summary>
	///   Administrator role with full system access.
	/// </summary>
	Admin = 0,

	/// <summary>
	///   Author role with content creation privileges.
	/// </summary>
	Author = 10
}

/// <summary>
///   Defines blog article categories.
/// </summary>
public enum CategoryNames
{
	/// <summary>
	///   ASP.NET Core category.
	/// </summary>
	AspNetCore = 0,

	/// <summary>
	///   Blazor Server category.
	/// </summary>
	BlazorServer = 1,

	/// <summary>
	///   Blazor WebAssembly category.
	/// </summary>
	BlazorWasm = 2,

	/// <summary>
	///   Entity Framework Core category.
	/// </summary>
	EntityFrameworkCore = 3,

	/// <summary>
	///   .NET MAUI category.
	/// </summary>
	NetMaui = 4,

	/// <summary>
	///   Other category for miscellaneous topics.
	/// </summary>
	Other = 5
}
