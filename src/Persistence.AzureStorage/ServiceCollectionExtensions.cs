// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     ServiceCollectionExtensions.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTrackerApp
// Project Name :  Persistence.AzureStorage
// =======================================================

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Persistence.AzureStorage;

/// <summary>
///   Extension methods for registering Azure Storage services.
/// </summary>
public static class ServiceCollectionExtensions
{
	/// <summary>
	///   Adds Azure Blob Storage services to the service collection.
	/// </summary>
	public static IServiceCollection AddAzureBlobStorage(
		this IServiceCollection services,
		IConfiguration configuration)
	{
		// Configure settings
		services.Configure<BlobStorageSettings>(
			configuration.GetSection(BlobStorageSettings.SECTION_NAME));

		// Register BlobServiceClient
		var connectionString = configuration[$"{BlobStorageSettings.SECTION_NAME}:ConnectionString"];

		if (!string.IsNullOrEmpty(connectionString))
		{
			services.AddSingleton(new BlobServiceClient(connectionString));
			services.AddScoped<IFileStorageService, BlobStorageService>();
		}

		return services;
	}
}
