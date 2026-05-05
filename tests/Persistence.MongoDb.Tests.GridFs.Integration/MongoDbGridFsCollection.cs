// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     MongoDbGridFsCollection.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTrackerApp
// Project Name :  Persistence.MongoDb.Tests.GridFs.Integration
// =======================================================

namespace Persistence.MongoDb.Tests.GridFs.Integration;

[CollectionDefinition("GridFsIntegration")]
public sealed class MongoDbGridFsCollection : ICollectionFixture<MongoDbGridFsFixture> { }
