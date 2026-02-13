using AppHost;

var builder = DistributedApplication.CreateBuilder(args);

// Configure resources
var redisCache = builder.AddRedisServices();
var mongoDb = builder.AddMongoDbServices();

// Web project with health check and resource dependencies
builder.AddProject<Projects.Web>("web")
		// Ensure the app binds to HTTP on port5057 to match Playwright tests
		.WithEnvironment("ASPNETCORE_URLS", "http://localhost:5057")
		.WithExternalHttpEndpoints()
		.WithHttpHealthCheck("/health")
		.WithReference(redisCache).WaitFor(redisCache)
		.WithReference(mongoDb).WaitFor(mongoDb);

builder.Build().Run();
