// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     AzuriteCollection.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTrackerApp
// Project Name :  Persistence.AzureStorage.Tests.Integration
// =======================================================

namespace Persistence.AzureStorage.Tests.Integration;

/// <summary>
///   xUnit collection definition for tests using the Azurite fixture.
/// </summary>
[CollectionDefinition("Azurite")]
public class AzuriteCollection : ICollectionFixture<AzuriteFixture>
{
}
