// ============================================
// Copyright (c) 2023. All rights reserved.
// File Name :     DatabaseSettings.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTracker
// Project Name :  IssueTracker.CoreBusiness
// =============================================

namespace Shared.Features.Models;

/// <summary>
///   Represents configuration settings for a database connection.
/// </summary>
public class DatabaseSettings : IDatabaseSettings
{
	/// <summary>
	///   Initializes a new instance of the <see cref="DatabaseSettings" /> class.
	/// </summary>
	public DatabaseSettings()
	{
	}

	/// <summary>
	///   Initializes a new instance of the <see cref="DatabaseSettings" /> class.
	/// </summary>
	/// <param name="connectionStrings">The connection string.</param>
	/// <param name="databaseName">The database name.</param>
	public DatabaseSettings(string connectionStrings, string databaseName)
	{
		ConnectionStrings = connectionStrings;
		DatabaseName = databaseName;
	}

	/// <summary>
	///   Gets or sets the connection string.
	/// </summary>
	/// <value>
	///   The connection string.
	/// </value>
	public string ConnectionStrings { get; set; } = null!;

	/// <summary>
	///   Gets or sets the database name.
	/// </summary>
	/// <value>
	///   The database name.
	/// </value>
	public string DatabaseName { get; set; } = null!;
}
