namespace Persistence.MongoDb.Configurations;

/// <summary>
/// MongoDB connection and database settings.
/// </summary>
public sealed class MongoDbSettings
{
	public const string SectionName = "MongoDB";

	/// <summary>
	/// MongoDB connection string.
	/// </summary>
	public string ConnectionString { get; init; } = string.Empty;

	/// <summary>
	/// Database name.
	/// </summary>
	public string DatabaseName { get; init; } = "issuetrackerdb";

	/// <summary>
	/// Maximum connection pool size.
	/// </summary>
	public int MaxConnectionPoolSize { get; init; } = 100;

	/// <summary>
	/// Connection timeout in seconds.
	/// </summary>
	public int ConnectionTimeoutSeconds { get; init; } = 30;

	/// <summary>
	/// Server selection timeout in seconds.
	/// </summary>
	public int ServerSelectionTimeoutSeconds { get; init; } = 30;

	/// <summary>
	/// Maximum retry attempts for transient failures.
	/// </summary>
	public int MaxRetryAttempts { get; init; } = 3;

	/// <summary>
	/// Validates the settings.
	/// </summary>
	public void Validate()
	{
		if (string.IsNullOrWhiteSpace(ConnectionString))
		{
			throw new InvalidOperationException("MongoDB connection string is not configured.");
		}

		if (string.IsNullOrWhiteSpace(DatabaseName))
		{
			throw new InvalidOperationException("MongoDB database name is not configured.");
		}
	}
}
