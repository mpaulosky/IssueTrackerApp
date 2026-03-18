var builder = DistributedApplication.CreateBuilder(args);

// Reference MongoDB Atlas connection string from User Secrets (ConnectionStrings:mongodb)
var mongodb = builder.AddConnectionString("mongodb");

// Add Redis container
var redis = builder.AddRedis("redis");
if (builder.Environment.EnvironmentName == "Development")
{
	redis = redis.WithRedisCommander();
}

// Add Web project with service discovery and health checks
builder.AddProject<Projects.Web>("web")
	.WithReference(mongodb)
	.WithReference(redis)
	.WaitFor(redis)
	.WithHttpHealthCheck("/health");

builder.Build().Run();
