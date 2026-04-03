// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     ObjectIdJsonConverterTests.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTrackerApp
// Project Name :  Web.Tests
// =======================================================

using System.Text.Json;

using MongoDB.Bson;

using Web.Helpers;

namespace Web.Tests.Helpers;

/// <summary>
///   Unit tests for <see cref="ObjectIdJsonConverter" />.
///   Verifies round-trip serialization, invalid-input fallback, and non-string token handling.
/// </summary>
public sealed class ObjectIdJsonConverterTests
{
	// -----------------------------------------------------------------------
	// Shared setup
	// -----------------------------------------------------------------------

	/// <summary>Serializer options pre-loaded with the converter under test.</summary>
	private static readonly JsonSerializerOptions Options = new()
	{
		PropertyNameCaseInsensitive = true,
		Converters = { new ObjectIdJsonConverter() }
	};

	/// <summary>A well-known 24-hex-char ObjectId string used across tests.</summary>
	private const string KnownIdString = "507f1f77bcf86cd799439011";

	// -----------------------------------------------------------------------
	// Read – happy path
	// -----------------------------------------------------------------------

	[Fact]
	public void Read_ReturnsObjectId_WhenValidString()
	{
		// Arrange
		var json = $"{{\"id\":\"{KnownIdString}\"}}";

		// Act
		var dto = JsonSerializer.Deserialize<IdWrapper>(json, Options);

		// Assert
		dto.Should().NotBeNull();
		dto!.Id.Should().Be(ObjectId.Parse(KnownIdString));
	}

	// -----------------------------------------------------------------------
	// Read – invalid string
	// -----------------------------------------------------------------------

	[Fact]
	public void Read_ReturnsEmptyObjectId_WhenInvalidString()
	{
		// Arrange – "not-a-valid-id" cannot be parsed as an ObjectId
		var json = "{\"id\":\"not-a-valid-id\"}";

		// Act
		var dto = JsonSerializer.Deserialize<IdWrapper>(json, Options);

		// Assert
		dto.Should().NotBeNull();
		dto!.Id.Should().Be(ObjectId.Empty);
	}

	// -----------------------------------------------------------------------
	// Read – non-string token
	// -----------------------------------------------------------------------

	[Fact]
	public void Read_ReturnsEmptyObjectId_WhenNonStringToken()
	{
		// Arrange – send a JSON number instead of a string
		var json = "{\"id\":12345}";

		// Act
		var dto = JsonSerializer.Deserialize<IdWrapper>(json, Options);

		// Assert
		dto.Should().NotBeNull();
		dto!.Id.Should().Be(ObjectId.Empty);
	}

	// -----------------------------------------------------------------------
	// Write
	// -----------------------------------------------------------------------

	[Fact]
	public void Write_WritesStringRepresentation()
	{
		// Arrange
		var objectId = ObjectId.Parse(KnownIdString);
		var wrapper = new IdWrapper { Id = objectId };

		// Act
		var json = JsonSerializer.Serialize(wrapper, Options);

		// Assert
		json.Should().Contain($"\"{KnownIdString}\"");
	}

	[Fact]
	public void Write_ProducesExactly24CharHexString()
	{
		// Arrange
		var objectId = ObjectId.GenerateNewId();
		var wrapper = new IdWrapper { Id = objectId };

		// Act
		var json = JsonSerializer.Serialize(wrapper, Options);
		var dto = JsonSerializer.Deserialize<IdWrapper>(json, Options);

		// Assert – the written string should be the 24-char hex representation
		dto.Should().NotBeNull();
		dto!.Id.ToString().Should().HaveLength(24);
		dto.Id.Should().Be(objectId);
	}

	// -----------------------------------------------------------------------
	// Round-trip
	// -----------------------------------------------------------------------

	[Fact]
	public void RoundTrip_SerializeDeserialize_PreservesObjectId()
	{
		// Arrange
		var original = ObjectId.GenerateNewId();
		var wrapper = new IdWrapper { Id = original };

		// Act
		var json = JsonSerializer.Serialize(wrapper, Options);
		var restored = JsonSerializer.Deserialize<IdWrapper>(json, Options);

		// Assert
		restored.Should().NotBeNull();
		restored!.Id.Should().Be(original);
	}

	// -----------------------------------------------------------------------
	// Private helper DTO
	// -----------------------------------------------------------------------

	/// <summary>Minimal DTO used to drive serializer round-trip tests.</summary>
	private sealed class IdWrapper
	{
		public ObjectId Id { get; set; }
	}
}
