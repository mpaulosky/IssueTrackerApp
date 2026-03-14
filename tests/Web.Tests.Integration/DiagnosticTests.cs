// Temporary diagnostic test to inspect MongoDB document structure
using Domain.Models;
using Microsoft.EntityFrameworkCore;
using MongoDB.Bson;
using MongoDB.Driver;
using Persistence.MongoDb;

namespace Web.Tests.Integration;

[Collection("Integration")]
public class DiagnosticTests : IntegrationTestBase
{
	public DiagnosticTests(CustomWebApplicationFactory factory)
		: base(factory)
	{
	}

	[Fact]
	public async Task BsonDocument_ShowStructure_AndEfCoreRoundTrip()
	{
		// Arrange - Seed via EF Core (same pattern as analytics tests)
		var (categories, statuses) = await SeedTestDataAsync();
		await SeedIssuesAsync(categories[0], statuses[0], 1);

		// Step 1: Discover actual collection name
		var client = new MongoClient(Factory.MongoConnectionString);
		var database = client.GetDatabase(Factory.DatabaseName);
		var collectionNames = await (await database.ListCollectionNamesAsync()).ToListAsync();
		var issueCollectionName = collectionNames.FirstOrDefault(n =>
			n.Contains("Issue", StringComparison.OrdinalIgnoreCase));

		Assert.NotNull(issueCollectionName);

		// Step 2: Read raw BSON and build structure dump
		var collection = database.GetCollection<BsonDocument>(issueCollectionName);
		var doc = await collection.Find(FilterDefinition<BsonDocument>.Empty).FirstOrDefaultAsync();
		Assert.NotNull(doc);

		var structure = new List<string>();
		foreach (var element in doc.Elements)
		{
			if (element.Value.BsonType == BsonType.Document)
			{
				var subdoc = element.Value.AsBsonDocument;
				var subfields = string.Join(", ",
					subdoc.Elements.Select(e => $"{e.Name}:{e.Value.BsonType}"));
				structure.Add($"  '{element.Name}'(Document): {{{subfields}}}");
			}
			else
			{
				structure.Add($"  '{element.Name}'({element.Value.BsonType}): {element.Value}");
			}
		}

		var structureDump = $"Collection: '{issueCollectionName}'\n" +
			$"All collections: [{string.Join(", ", collectionNames)}]\n" +
			$"Document fields:\n{string.Join("\n", structure)}";

		// Step 3: Try EF Core round-trip
		await using var context = Factory.CreateDbContext();
		try
		{
			var issues = await context.Issues.ToListAsync(CancellationToken.None);
			// If we get here, deserialization works! Pass the test.
			issues.Should().NotBeEmpty(
				$"EF Core round-trip succeeded but returned empty. {structureDump}");
		}
		catch (Exception ex)
		{
			// Force failure with BOTH the exception AND the document structure
			Assert.Fail(
				$"EF Core ToListAsync failed.\n" +
				$"Exception: {ex.GetType().Name}: {ex.Message}\n" +
				$"{structureDump}");
		}
	}
}
