using Persistence.MongoDb.Configurations;
using Persistence.MongoDb.Repositories;

namespace Persistence.MongoDb;

/// <summary>
/// Extension methods for registering MongoDB persistence services.
/// </summary>
public static class ServiceCollectionExtensions
{
	/// <summary>
	/// Adds MongoDB persistence services to the service collection.
	/// </summary>
	public static IServiceCollection AddMongoDbPersistence(
		this IServiceCollection services,
		IConfiguration configuration)
	{
		// Register and validate MongoDB settings
		services.AddOptions<MongoDbSettings>()
			.Bind(configuration.GetSection(MongoDbSettings.SectionName))
			.ValidateOnStart();

		services.AddSingleton<IValidateOptions<MongoDbSettings>, MongoDbSettingsValidator>();

		// Register DbContext with MongoDB provider
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
		services.AddDbContextFactory<IssueTrackerDbContext>((serviceProvider, options) =>
		{
			var settings = serviceProvider.GetRequiredService<IOptions<MongoDbSettings>>().Value;

			options.UseMongoDB(
				settings.ConnectionString,
				settings.DatabaseName);
		});

		// Register generic repository
		services.AddScoped(typeof(IRepository<>), typeof(Repository<>));

		return services;
	}

	/// <summary>
	/// Initializes the MongoDB database (creates database and applies indexes).
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
