// ============================================
// Copyright (c) 2025. All rights reserved.
// File Name :     IDatabaseSettings.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTrackerApp
// Project Name :  Shared
// =============================================

namespace Shared.Features.Models;

/// <summary>
///   IDatabaseSettings interface
/// </summary>
public interface IDatabaseSettings
{
	/// <summary>
	///   Gets or sets the connection strings.
	/// </summary>
	/// <value>
	///   The connection strings.
	/// </value>
	string ConnectionStrings { get; set; }

	/// <summary>
	///   Gets or sets the database name.
	/// </summary>
	/// <value>
	///   The database name.
	/// </value>
	string DatabaseName { get; set; }
}
