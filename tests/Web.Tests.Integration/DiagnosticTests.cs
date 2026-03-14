// Temporary diagnostic test to inspect MongoDB document structure
using System.Linq.Expressions;
using Domain.Models;
using MongoDB.Bson;
using MongoDB.Bson.IO;
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
	public async Task BsonDocument_ShouldContain_StatusField()
	{
		// Arrange - Seed via EF Core (same pattern as analytics tests)
		var (categories, statuses) = await SeedTestDataAsync();
		await SeedIssuesAsync(categories[0], statuses[0], 3);

		// Act - Read raw BSON via MongoDB driver
		var client = new MongoClient(Factory.MongoConnectionString);
		var database = client.GetDatabase(Factory.DatabaseName);
		var collection = database.GetCollection<BsonDocument>("Issue");
		var docs = await collection.Find(FilterDefinition<BsonDocument>.Empty).ToListAsync();

		// Assert - Document should have Status field
		docs.Should().NotBeEmpty("Issues collection should contain seeded documents");

		var firstDoc = docs[0];
		var fieldNames = firstDoc.Elements.Select(e => e.Name).ToList();

		// This assertion will SHOW the actual field names in the error message
		firstDoc.Contains("Status").Should().BeTrue(
			$"Issue document should contain 'Status' field. " +
			$"Actual top-level fields: [{string.Join(", ", fieldNames)}]");
	}

	[Fact]
	public async Task EfCore_ToListAsync_ShouldDeserializeIssues()
	{
		// Arrange - Seed via EF Core
		var (categories, statuses) = await SeedTestDataAsync();
		await SeedIssuesAsync(categories[0], statuses[0], 3);

		// Act - Read back via EF Core ToListAsync (same as Repository.FindAsync)
		await using var context = Factory.CreateDbContext();
		var act = async () => await context.Issues.ToListAsync();

		// Assert
		var issues = await act.Should().NotThrowAsync(
			"EF Core should be able to deserialize Issues written by EF Core");
		issues.Subject.Should().HaveCount(3);
	}

	[Fact]
	public async Task EfCore_WhereToListAsync_ShouldDeserializeIssues()
	{
		// Arrange - Seed via EF Core (exact pattern analytics uses)
		var (categories, statuses) = await SeedTestDataAsync();
		await SeedIssuesAsync(categories[0], statuses[0], 3);

		var startDate = DateTime.UtcNow.AddDays(-1);
		var endDate = DateTime.UtcNow.AddDays(1);

		// Act - Read back via EF Core Where + ToListAsync (same as Repository.FindAsync)
		await using var context = Factory.CreateDbContext();
		var act = async () => await context.Issues
			.Where(i => i.DateCreated >= startDate && i.DateCreated <= endDate)
			.ToListAsync();

		// Assert
		var issues = await act.Should().NotThrowAsync(
			"EF Core Where+ToListAsync should deserialize Issues. " +
			"This is the exact pattern Repository.FindAsync uses.");
		issues.Subject.Should().HaveCount(3);
	}

	[Fact]
	public async Task BsonDocument_ShowFullStructure_OnFailure()
	{
		// Arrange - Seed via EF Core
		var (categories, statuses) = await SeedTestDataAsync();
		await SeedIssuesAsync(categories[0], statuses[0], 1);

		// Act - Read raw BSON
		var client = new MongoClient(Factory.MongoConnectionString);
		var database = client.GetDatabase(Factory.DatabaseName);

		// Try both "Issue" and "Issues" collection names
		var collectionNames = await (await database.ListCollectionNamesAsync()).ToListAsync();
		var issueCollectionName = collectionNames.FirstOrDefault(n =>
			n.Equals("Issue", StringComparison.OrdinalIgnoreCase) ||
			n.Equals("Issues", StringComparison.OrdinalIgnoreCase));

		issueCollectionName.Should().NotBeNull(
			$"Should find an Issue/Issues collection. Available collections: [{string.Join(", ", collectionNames)}]");

		var collection = database.GetCollection<BsonDocument>(issueCollectionName!);
		var doc = await collection.Find(FilterDefinition<BsonDocument>.Empty).FirstOrDefaultAsync();
		doc.Should().NotBeNull("Should have at least one seeded document");

		// Build a detailed description of the document structure
		var structure = new List<string>();
		foreach (var element in doc!.Elements)
		{
			if (element.Value.BsonType == BsonType.Document)
			{
				var subdoc = element.Value.AsBsonDocument;
				var subfields = string.Join(", ", subdoc.Elements.Select(e => $"{e.Name}:{e.Value.BsonType}"));
				structure.Add($"'{element.Name}'(Document): {{{subfields}}}");
			}
			else
			{
				structure.Add($"'{element.Name}'({element.Value.BsonType})");
			}
		}

		// This will always pass but shows the structure in test output
		// If Status field is missing, BsonDocument_ShouldContain_StatusField will catch it
		doc.ElementCount.Should().BeGreaterThan(0,
			$"Document structure:\n{string.Join("\n", structure)}");
	}
}
