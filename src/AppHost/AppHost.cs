var builder = DistributedApplication.CreateBuilder(args);

// Add MongoDB container
var mongodb = builder.AddMongoDB("mongodb")
	.WithMongoExpress()
	.AddDatabase("issuetracker-db");

// Add Redis container
var redis = builder.AddRedis("redis");

// Add Web project
builder.AddProject<Projects.Web>("web")
	.WithReference(mongodb)
	.WithReference(redis)
	.WaitFor(mongodb)
	.WaitFor(redis);

builder.Build().Run();
