using Domain.Features.Admin.Abstractions;

using Persistence.MongoDb.Configurations;
using Persistence.MongoDb.Repositories;
using Persistence.MongoDb.Services;

namespace Persistence.MongoDb;

/// <summary>
/// Extension methods for registering MongoDB persistence services.
/// </summary>
public static class ServiceCollectionExtensions
{
	private const string LocalhostDefault = "mongodb://localhost:27017";

	/// <summary>
	/// Adds MongoDB persistence services to the service collection.
	/// </summary>
	public static IServiceCollection AddMongoDbPersistence(
		this IServiceCollection services,
		IConfiguration configuration)
	{
		// Fallback: when MongoDB:ConnectionString is empty or the localhost default,
		// use ConnectionStrings:mongodb (injected by Aspire or stored in user secrets).
		var mongoSection = configuration.GetSection(MongoDbSettings.SectionName);
		var configuredConnectionString = mongoSection[nameof(MongoDbSettings.ConnectionString)];

		if (string.IsNullOrWhiteSpace(configuredConnectionString)
			|| configuredConnectionString.Equals(LocalhostDefault, StringComparison.OrdinalIgnoreCase))
		{
			var fallback = configuration.GetConnectionString("mongodb");
			if (!string.IsNullOrWhiteSpace(fallback))
			{
				mongoSection[nameof(MongoDbSettings.ConnectionString)] = fallback;
			}
		}

		// Register and validate MongoDB settings
		services.AddOptions<MongoDbSettings>()
			.Bind(configuration.GetSection(MongoDbSettings.SectionName))
			.ValidateOnStart();

		services.AddSingleton<IValidateOptions<MongoDbSettings>, MongoDbSettingsValidator>();

		// Register DbContext with a MongoDB provider
		services.AddDbContext<IssueTrackerDbContext>((serviceProvider, options) =>
		{
			var settings = serviceProvider.GetRequiredService<IOptions<MongoDbSettings>>().Value;

			options.UseMongoDB(
				settings.ConnectionString,
				settings.DatabaseName);
		});

		// Register the interface so Repository<T> can resolve IIssueTrackerDbContext
		services.AddScoped<IIssueTrackerDbContext>(sp => sp.GetRequiredService<IssueTrackerDbContext>());

		// Register DbContext factory for scenarios requiring multiple contexts
		// Use scoped lifetime to match the scoped DbContextOptions registered by AddDbContext
		services.AddDbContextFactory<IssueTrackerDbContext>((serviceProvider, options) =>
		{
			var settings = serviceProvider.GetRequiredService<IOptions<MongoDbSettings>>().Value;

			options.UseMongoDB(
				settings.ConnectionString,
				settings.DatabaseName);
		}, lifetime: ServiceLifetime.Scoped);

		// Register a generic repository
		services.AddScoped(typeof(IRepository<>), typeof(Repository<>));

		// Register audit log writer service
		services.AddScoped<IAuditLogWriterService, AuditLogWriterService>();

		// Register audit log repository
		services.AddScoped<IAuditLogRepository, AuditLogRepository>();

		return services;
	}

	/// <summary>
	/// Initializes the MongoDB database (creates a database and applies indexes).
	/// </summary>
	public static async Task InitializeMongoDbAsync(this IServiceProvider serviceProvider)
	{
		using var scope = serviceProvider.CreateScope();
		var context = scope.ServiceProvider.GetRequiredService<IssueTrackerDbContext>();
		await context.InitializeDatabaseAsync();
	}
}

/// <summary>
/// Validates MongoDB settings on application startup.
/// </summary>
internal sealed class MongoDbSettingsValidator : IValidateOptions<MongoDbSettings>
{
	public ValidateOptionsResult Validate(string? name, MongoDbSettings options)
	{
		try
		{
			options.Validate();
			return ValidateOptionsResult.Success;
		}
		catch (Exception ex)
		{
			return ValidateOptionsResult.Fail(ex.Message);
		}
	}
}
