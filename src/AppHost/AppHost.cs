var builder = DistributedApplication.CreateBuilder(args);

var isTesting = string.Equals(builder.Environment.EnvironmentName, "Testing", StringComparison.OrdinalIgnoreCase)
	|| string.Equals(Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"), "Testing", StringComparison.OrdinalIgnoreCase);

// Reference MongoDB Atlas connection string from User Secrets (ConnectionStrings:mongodb)
var mongodb = !isTesting ? builder.AddConnectionString("mongodb") : null;

// Add Redis container
var redis = builder.AddRedis("redis");
if (builder.Environment.EnvironmentName == "Development")
{
	redis = redis.WithRedisCommander();
}

// Add Auth0 Management API parameters (M2M credentials for admin user management)
// Names use camelCase (no hyphens) so they map cleanly to Parameters__auth0MgmtClientId env vars in CI.
var auth0MgmtClientId = !isTesting ? builder.AddParameter("auth0MgmtClientId", secret: true) : null;
var auth0MgmtClientSecret = !isTesting ? builder.AddParameter("auth0MgmtClientSecret", secret: true) : null;

// Add Web project with service discovery and health checks
var web = builder.AddProject<Projects.Web>("web")
	.WithReference(redis)
	.WaitFor(redis);

if (mongodb is not null)
{
	web.WithReference(mongodb);
}

if (isTesting)
{
	// Avoid missing Aspire parameter failures in tests where Auth0 M2M creds are intentionally absent.
	web.WithEnvironment("Auth0Management__ClientId", "test-client-id")
		.WithEnvironment("Auth0Management__ClientSecret", "test-client-secret");
}
else
{
	web.WithEnvironment("Auth0Management__ClientId", auth0MgmtClientId!)
		.WithEnvironment("Auth0Management__ClientSecret", auth0MgmtClientSecret!);
}

if (!isTesting)
{
	web.WithHttpHealthCheck("/health");
}

builder.Build().Run();
