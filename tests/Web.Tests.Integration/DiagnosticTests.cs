// Temporary diagnostic test to inspect MongoDB document structure
using MongoDB.Bson;
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
		var collection = database.GetCollection<BsonDocument>("Issues");

		var documents = await collection.Find(FilterDefinition<BsonDocument>.Empty).ToListAsync();
		_output.WriteLine($"\nTotal documents in Issues collection: {documents.Count}");

		foreach (var doc in documents)
		{
			_output.WriteLine($"\n=== Raw BsonDocument ===");
			_output.WriteLine(doc.ToJson(new MongoDB.Bson.IO.JsonWriterSettings { Indent = true }));

			_output.WriteLine($"\n=== Top-level field names ===");
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

		// Also try reading back via EF Core to see what error occurs
		try
		{
			await using var context = Factory.CreateDbContext();
			var efIssues = await context.Issues.ToListAsync();
			_output.WriteLine($"\nEF Core read back: {efIssues.Count} issues");
			foreach (var efIssue in efIssues)
			{
				_output.WriteLine($"  Issue: {efIssue.Title}, Status: {efIssue.Status?.StatusName}");
			}
		}
		catch (Exception ex)
		{
			_output.WriteLine($"\nEF Core read error: {ex.Message}");
			_output.WriteLine($"Inner: {ex.InnerException?.Message}");
		}
	}
}
