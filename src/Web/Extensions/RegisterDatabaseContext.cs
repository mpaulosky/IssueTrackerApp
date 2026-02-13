// ============================================
// Copyright (c) 2023. All rights reserved.
// File Name :     RegisterDatabaseContext.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTracker
// Project Name :  IssueTracker.UI
// =============================================

namespace IssueTracker.UI.Extensions;

/// <summary>
///   IServiceCollectionExtensions
/// </summary>
public static partial class ServiceCollectionExtensions
{
	/// <summary>
	///   RegisterDatabaseContext
	/// </summary>
	/// <param name="services">IServiceCollection</param>
	/// <returns>IServiceCollection</returns>
	public static IServiceCollection RegisterDatabaseContext(this IServiceCollection services)
	{
		services.AddSingleton<IMongoDbContextFactory, MongoDbContextFactory>();

		return services;
	}
}