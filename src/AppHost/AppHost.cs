var builder = DistributedApplication.CreateBuilder(args);

// Reference MongoDB Atlas connection string from User Secrets (ConnectionStrings:mongodb)
var mongodb = builder.AddConnectionString("mongodb");

// Add Redis container
var redis = builder.AddRedis("redis");
if (builder.Environment.EnvironmentName == "Development")
{
	redis = redis.WithRedisCommander();
}

// Add Auth0 Management API parameters (M2M credentials for admin user management)
var auth0MgmtClientId = builder.AddParameter("auth0-mgmt-client-id", secret: true);
var auth0MgmtClientSecret = builder.AddParameter("auth0-mgmt-client-secret", secret: true);

// Add Web project with service discovery and health checks
builder.AddProject<Projects.Web>("web")
	.WithReference(mongodb)
	.WithReference(redis)
	.WaitFor(redis)
	.WithHttpHealthCheck("/health")
	.WithEnvironment("Auth0Management__ClientId", auth0MgmtClientId)
	.WithEnvironment("Auth0Management__ClientSecret", auth0MgmtClientSecret);

builder.Build().Run();
