var builder = DistributedApplication.CreateBuilder(args);

var isTesting = string.Equals(builder.Environment.EnvironmentName, "Testing", StringComparison.OrdinalIgnoreCase)
	|| string.Equals(Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"), "Testing", StringComparison.OrdinalIgnoreCase);

var web = builder.AddProject<Projects.Web>("web")
	.WithEnvironment("Auth0Management__ClientId", "test-client-id")
	.WithEnvironment("Auth0Management__ClientSecret", "test-client-secret");

if (!isTesting)
{
	// Reference MongoDB Atlas connection string from User Secrets (ConnectionStrings:mongodb)
	var mongodb = builder.AddConnectionString("mongodb");

	// Add Redis container
	var redis = builder.AddRedis("redis");
	if (builder.Environment.EnvironmentName == "Development")
	{
		redis = redis.WithRedisCommander();
	}

	// Add Auth0 Management API parameters (M2M credentials for admin user management)
	// Names use camelCase (no hyphens) so they map cleanly to Parameters__auth0MgmtClientId env vars in CI.
	var auth0MgmtClientId = builder.AddParameter("auth0MgmtClientId", secret: true);
	var auth0MgmtClientSecret = builder.AddParameter("auth0MgmtClientSecret", secret: true);

	web.WithReference(mongodb)
		.WithReference(redis)
		.WaitFor(redis)
		.WithEnvironment("Auth0Management__ClientId", auth0MgmtClientId)
		.WithEnvironment("Auth0Management__ClientSecret", auth0MgmtClientSecret);

	web.WithHttpHealthCheck("/health");
}

builder.Build().Run();
