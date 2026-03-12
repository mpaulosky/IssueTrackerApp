var builder = DistributedApplication.CreateBuilder(args);

// Add MongoDB container
var mongodb = builder.AddMongoDB("mongodb")
	.WithMongoExpress()
	.AddDatabase("issuetracker-db");

// Add Redis container
var redis = builder.AddRedis("redis")
	.WithRedisCommander();

// Add Web project with service discovery and health checks
builder.AddProject<Projects.Web>("web")
	.WithReference(mongodb)
	.WithReference(redis)
	.WaitFor(mongodb)
	.WaitFor(redis)
	.WithHttpHealthCheck("/health");

builder.Build().Run();
