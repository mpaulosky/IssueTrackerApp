// Temporary diagnostic test to inspect MongoDB document structure
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Driver;
using Xunit.Abstractions;

namespace Web.Tests.Integration;

[Collection("Integration")]
public class DiagnosticTests : IntegrationTestBase
{
	private readonly ITestOutputHelper _output;

	public DiagnosticTests(CustomWebApplicationFactory factory, ITestOutputHelper output)
		: base(factory)
	{
		_output = output;
	}

	[Fact]
	public async Task Diagnostic_InspectIssueBsonDocument()
	{
		// Seed data via EF Core
		var (categories, statuses) = await SeedTestDataAsync();
		var issue = await SeedIssueAsync(categories[0], statuses[0]);

		_output.WriteLine($"Seeded Issue ID: {issue.Id}");
		_output.WriteLine($"Issue.Status type: {issue.Status?.GetType().Name}");
		_output.WriteLine($"Issue.Status.StatusName: {issue.Status?.StatusName}");

		// Read raw BsonDocument via MongoDB driver
		var client = new MongoClient(Factory.MongoConnectionString);
		var database = client.GetDatabase(Factory.DatabaseName);

		// List all collection names first
		var collectionNames = new List<string>();
		using (var cursor = await database.ListCollectionNamesAsync())
		{
			while (await cursor.MoveNextAsync())
			{
				collectionNames.AddRange(cursor.Current);
			}
		}
		_output.WriteLine($"\n=== Collections in database ===");
		foreach (var name in collectionNames)
		{
			_output.WriteLine($"  Collection: '{name}'");
		}

		// Try the collection EF Core uses (could be "Issues" or "Issue")
		foreach (var collName in new[] { "Issues", "Issue", "issues", "issue" })
		{
			var collection = database.GetCollection<BsonDocument>(collName);
			var count = await collection.CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty);
			if (count == 0) continue;

			_output.WriteLine($"\n=== Collection '{collName}' has {count} documents ===");

			using var cursor = await collection.FindAsync(FilterDefinition<BsonDocument>.Empty);
			var documents = new List<BsonDocument>();
			while (await cursor.MoveNextAsync())
			{
				documents.AddRange(cursor.Current);
			}

			foreach (var doc in documents)
			{
				_output.WriteLine($"\n--- Raw BsonDocument ---");
				_output.WriteLine(doc.ToJson(new JsonWriterSettings { Indent = true }));

				_output.WriteLine($"\n--- Top-level field names ---");
				foreach (var element in doc.Elements)
				{
					_output.WriteLine($"  '{element.Name}' : {element.Value.BsonType}");
					if (element.Value.BsonType == BsonType.Document)
					{
						var subdoc = element.Value.AsBsonDocument;
						foreach (var sub in subdoc.Elements)
						{
							_output.WriteLine($"    '{sub.Name}' : {sub.Value.BsonType}");
						}
					}
				}
			}
		}

		// Also try reading back via EF Core to see what error occurs
		try
		{
			await using var context = Factory.CreateDbContext();
			var efIssues = new List<Domain.Models.Issue>();
			await foreach (var item in context.Issues.AsAsyncEnumerable())
			{
				efIssues.Add(item);
			}
			_output.WriteLine($"\nEF Core read back: {efIssues.Count} issues");
			foreach (var efIssue in efIssues)
			{
				_output.WriteLine($"  Issue: {efIssue.Title}, Status: {efIssue.Status?.StatusName}");
			}
		}
		catch (Exception ex)
		{
			_output.WriteLine($"\nEF Core read error: {ex.GetType().Name}: {ex.Message}");
			if (ex.InnerException is not null)
			{
				_output.WriteLine($"Inner: {ex.InnerException.GetType().Name}: {ex.InnerException.Message}");
			}
			_output.WriteLine($"Stack: {ex.StackTrace}");
		}
	}
}
