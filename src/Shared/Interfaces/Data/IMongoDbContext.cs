// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     IMongoDbContext.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTrackerApp
// Project Name :  Shared
// =======================================================

using MongoDB.Driver;

using Shared.Models;

namespace Shared.Interfaces.Data;

/// <summary>
///   Interface for MongoDB context using a native driver
/// </summary>
public interface IMongoDbContext : IDisposable
{

	IMongoCollection<Issue> Issues { get; }

	IMongoCollection<Category> Categories { get; }

	IMongoCollection<Comment> Comments { get; }

	IMongoCollection<Status> Statuses { get; }

	IMongoCollection<User> Users { get; }

	IMongoDatabase Database { get; }

}
