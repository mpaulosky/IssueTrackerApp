// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     IMongoDbContext.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : ArticlesSite
// Project Name :  Web
// =======================================================

using Shared
namespace ApiService.DataAccess;

/// <summary>
///   Interface for MongoDB context using a native driver
/// </summary>
public interface IMongoDbContext : IDisposable
{

	IMongoCollection<Shared.Features.Issue> Issues { get; }

	IMongoCollection<Category> Categories { get; }

	IMongoDatabase Database { get; }

}
