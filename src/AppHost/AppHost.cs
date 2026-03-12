var builder = DistributedApplication.CreateBuilder(args);

// Add MongoDB container
var mongoServer = builder.AddMongoDB("mongodb");
if (builder.Environment.EnvironmentName == "Development")
{
	mongoServer = mongoServer.WithMongoExpress();
}
var mongodb = mongoServer.AddDatabase("issuetracker-db");

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
	.WaitFor(mongodb)
	.WaitFor(redis)
	.WithHttpHealthCheck("/health");

builder.Build().Run();
