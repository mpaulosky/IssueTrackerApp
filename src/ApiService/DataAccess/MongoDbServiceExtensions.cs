// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     MongoDbServiceExtensions.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : ArticlesSite
// Project Name :  Web
// =======================================================

// Removed redundant usings: these namespaces are included in Web/GlobalUsings.cs

namespace ApiService.DataAccess;

/// <summary>
///   Provides extension methods for registering MongoDB-related services, context factories, and repositories
///   in a Blazor/ASP.NET Core application using Aspire service discovery and Aspire.MongoDB.Driver.
/// </summary>
public static class MongoDbServiceExtensions
{

	/// <summary>
	///   Adds MongoDB services to the application's dependency injection container using Aspire service discovery and
	///   Aspire.MongoDB.Driver.
	///   Registers IMongoClient, IMongoDatabase, MongoDbContext, and related repositories and handlers.
	/// </summary>
	/// <param name="builder">The <see cref="WebApplicationBuilder" /> to configure.</param>
	/// <returns>The configured <see cref="WebApplicationBuilder" /> for chaining.</returns>
	public static void AddMongoDb(this WebApplicationBuilder builder)
	{
		IServiceCollection services = builder.Services;
		var configuration = builder.Configuration;

		// Get MongoDB connection string from configuration or environment variables.
		// Support both "MongoDb:ConnectionString" and legacy "ConnectionStrings:articlesdb" keys so tests and environments work.
		string? connectionString = configuration["MongoDb:ConnectionString"] ?? configuration["ConnectionStrings:articlesdb"];
		connectionString ??= Environment.GetEnvironmentVariable("MONGODB_CONNECTION_STRING");
		if (string.IsNullOrWhiteSpace(connectionString))
		{
			throw new InvalidOperationException("MongoDB connection string not found.");
		}

		// Support both "MongoDb:Database" and "MongoDb:DatabaseName" keys for compatibility with tests and config sources
		string databaseName = configuration["MongoDb:Database"] ?? configuration["MongoDb:DatabaseName"] ??
													Environment.GetEnvironmentVariable("MONGODB_DATABASE_NAME") ?? "articlesdb";

		// Register IMongoClient and IMongoDatabase manually
		services.AddSingleton<IMongoClient>(_ => new MongoClient(connectionString));

		// Use a factory pattern for the database to ensure the database name is resolved at runtime, not at registration time
		services.AddScoped(sp =>
		{
			IMongoClient client = sp.GetRequiredService<IMongoClient>();
			// Re-read database name in case it changed after registration
			var runtimeDatabaseName = sp.GetRequiredService<IConfiguration>()["MongoDb:Database"]
				?? sp.GetRequiredService<IConfiguration>()["MongoDb:DatabaseName"]
				?? Environment.GetEnvironmentVariable("MONGODB_DATABASE_NAME")
				?? "articlesdb";
			return client.GetDatabase(runtimeDatabaseName);
		});

		services.AddScoped<IMongoDbContext>(sp =>
		{
			IMongoClient client = sp.GetRequiredService<IMongoClient>();
			IMongoDatabase database = sp.GetRequiredService<IMongoDatabase>();

			return new MongoDbContext(client, database.DatabaseNamespace.DatabaseName);
		});

		services.AddScoped<IMongoDbContextFactory>(sp =>
		{
			IMongoDbContext context = sp.GetRequiredService<IMongoDbContext>();

			return new RuntimeMongoDbContextFactory(context);
		});

		RegisterRepositoriesAndHandlers(services);

	}

	/// <summary>
	///   Registers MongoDB repositories and CQRS handlers for articles and categories.
	/// </summary>
	/// <param name="services">The <see cref="IServiceCollection" /> to register services with.</param>
	private static void RegisterRepositoriesAndHandlers(IServiceCollection services)
	{
		// Register repositories
		services.AddScoped<IArticleRepository, ArticleRepository>();
		services.AddScoped<ICategoryRepository, CategoryRepository>();

		// Register validators
		services.AddScoped<IValidator<CategoryDto>, Web.Components.Features.Categories.Validators.CategoryDtoValidator>();
		services.AddScoped<IValidator<ArticleDto>, Web.Components.Features.Articles.Validators.ArticleDtoValidator>();
		services.AddScoped<IValidator<Category>, Web.Components.Features.Categories.Validators.CategoryValidator>();
		services.AddScoped<IValidator<Article>, Web.Components.Features.Articles.Validators.ArticleValidator>();

		// Article Handlers
		services.AddScoped<GetArticles.IGetArticlesHandler, GetArticles.Handler>();
		services.AddScoped<GetArticle.IGetArticleHandler, GetArticle.Handler>();
		services.AddScoped<CreateArticle.ICreateArticleHandler, CreateArticle.Handler>();
		services.AddScoped<EditArticle.IEditArticleHandler, EditArticle.Handler>();

		// Category Handlers
		services.AddScoped<EditCategory.IEditCategoryHandler, EditCategory.Handler>();
		services.AddScoped<GetCategory.IGetCategoryHandler, GetCategory.Handler>();
		services.AddScoped<CreateCategory.ICreateCategoryHandler, CreateCategory.Handler>();
		services.AddScoped<GetCategories.IGetCategoriesHandler, GetCategories.Handler>();
	}

	/// <summary>
	///   Runtime adapter that wraps the DI-resolved <see cref="IMongoDbContext" /> for use with
	///   <see cref="IMongoDbContextFactory" />.
	/// </summary>
	private sealed class RuntimeMongoDbContextFactory : IMongoDbContextFactory
	{

		private readonly IMongoDbContext _context;

		/// <summary>
		///   Initializes a new instance of the <see cref="RuntimeMongoDbContextFactory" /> class.
		/// </summary>
		/// <param name="context">The DI-resolved <see cref="IMongoDbContext" /> instance.</param>
		public RuntimeMongoDbContextFactory(IMongoDbContext context)
		{
			_context = context;
		}

		/// <summary>
		///   Returns the DI-resolved <see cref="IMongoDbContext" /> instance.
		/// </summary>
		/// <returns>The <see cref="IMongoDbContext" /> instance.</returns>
		public IMongoDbContext CreateDbContext()
		{
			// Return the context directly
			return _context;
		}

	}

}
