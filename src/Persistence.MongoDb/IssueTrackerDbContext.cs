using Microsoft.Extensions.Options;
using Persistence.MongoDb.Configurations;

namespace Persistence.MongoDb;

/// <summary>
/// Database context for IssueTracker application using MongoDB.
/// </summary>
public sealed class IssueTrackerDbContext : DbContext
{
	private readonly MongoDbSettings _settings;

	public IssueTrackerDbContext(
		DbContextOptions<IssueTrackerDbContext> options,
		IOptions<MongoDbSettings> settings) : base(options)
	{
		_settings = settings.Value;
	}

	protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
	{
		if (!optionsBuilder.IsConfigured)
		{
			optionsBuilder.UseMongoDB(
				_settings.ConnectionString,
				_settings.DatabaseName);
		}
	}

	protected override void OnModelCreating(ModelBuilder modelBuilder)
	{
		base.OnModelCreating(modelBuilder);

		// Apply all configurations from the assembly
		modelBuilder.ApplyConfigurationsFromAssembly(typeof(IssueTrackerDbContext).Assembly);
	}

	/// <summary>
	/// Ensures the database and collections are created with proper indexes.
	/// </summary>
	public async Task InitializeDatabaseAsync(CancellationToken cancellationToken = default)
	{
		try
		{
			// Ensure database is created
			await Database.EnsureCreatedAsync(cancellationToken);
		}
		catch (Exception ex)
		{
			throw new InvalidOperationException("Failed to initialize database.", ex);
		}
	}
}
