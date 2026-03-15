// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     MongoDbCollection.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTrackerApp
// Project Name :  Persistence.MongoDb.Tests.Integration
// =======================================================

namespace Persistence.MongoDb.Tests.Integration;

/// <summary>
///   xUnit collection definition for shared MongoDb test fixture.
/// </summary>
[CollectionDefinition("MongoDb")]
public class MongoDbCollection : ICollectionFixture<MongoDbFixture>
{
}
