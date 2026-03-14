// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     ObjectIdJsonConverter.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTrackerApp
// Project Name :  Web
// =======================================================

using System.Text.Json;
using System.Text.Json.Serialization;

using MongoDB.Bson;

namespace Web.Helpers;

/// <summary>
///   JSON converter for MongoDB ObjectId to serialize as string and deserialize from string.
/// </summary>
public sealed class ObjectIdJsonConverter : JsonConverter<ObjectId>
{
	public override ObjectId Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		if (reader.TokenType == JsonTokenType.String)
		{
			var value = reader.GetString();
			return ObjectId.TryParse(value, out var objectId) ? objectId : ObjectId.Empty;
		}

		return ObjectId.Empty;
	}

	public override void Write(Utf8JsonWriter writer, ObjectId value, JsonSerializerOptions options)
	{
		writer.WriteStringValue(value.ToString());
	}
}
