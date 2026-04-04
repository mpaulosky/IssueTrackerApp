// ============================================
// Copyright (c) 2026. All rights reserved.
// File Name :     FakeSeedData.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueManager
// Project Name :  Web
// =============================================

using System.Diagnostics.CodeAnalysis;

using Domain.Models;

using MongoDB.Bson;

namespace Web.Testing;

/// <summary>
/// Provides deterministic seed data for the Testing environment in-memory repositories.
/// Hard-coded ObjectIds ensure stable IDs across test runs.
/// </summary>
[ExcludeFromCodeCoverage]
internal static class FakeSeedData
{
	// --- Status IDs (deterministic, stable across runs) ---
	private static readonly ObjectId OpenId        = new("507f1f77bcf86cd799439011");
	private static readonly ObjectId InReviewId    = new("507f1f77bcf86cd799439012");
	private static readonly ObjectId ClosedId      = new("507f1f77bcf86cd799439013");
	private static readonly ObjectId DuplicateId   = new("507f1f77bcf86cd799439014");
	private static readonly ObjectId WrongId       = new("507f1f77bcf86cd799439015");

	// --- Category IDs ---
	private static readonly ObjectId BugId         = new("507f1f77bcf86cd799439021");
	private static readonly ObjectId FeatureId     = new("507f1f77bcf86cd799439022");
	private static readonly ObjectId ImprovementId = new("507f1f77bcf86cd799439023");

	// --- Issue IDs ---
	private static readonly ObjectId Issue1Id = new("507f1f77bcf86cd799439031");
	private static readonly ObjectId Issue2Id = new("507f1f77bcf86cd799439032");
	private static readonly ObjectId Issue3Id = new("507f1f77bcf86cd799439033");

	internal static IReadOnlyList<Status> Statuses =>
	[
		new() { Id = OpenId,        StatusName = "Open",        StatusDescription = "Issue is open",        Archived = false },
		new() { Id = InReviewId,    StatusName = "In Review",   StatusDescription = "Issue is in review",   Archived = false },
		new() { Id = ClosedId,      StatusName = "Closed",      StatusDescription = "Issue is closed",      Archived = false },
		new() { Id = DuplicateId,   StatusName = "Duplicate",   StatusDescription = "Issue is a duplicate", Archived = false },
		new() { Id = WrongId,       StatusName = "Wrong",       StatusDescription = "Issue is wrong",       Archived = false },
	];

	internal static IReadOnlyList<Category> Categories =>
	[
		new() { Id = BugId,         CategoryName = "Bug",         CategoryDescription = "Something is broken",    Archived = false },
		new() { Id = FeatureId,     CategoryName = "Feature",     CategoryDescription = "New functionality",       Archived = false },
		new() { Id = ImprovementId, CategoryName = "Improvement", CategoryDescription = "Enhancement to existing", Archived = false },
	];

	internal static IReadOnlyList<Issue> Issues =>
	[
		new()
		{
			Id          = Issue1Id,
			Title       = "Login page is broken",
			Description = "The login page returns 500 on form submit.",
			DateCreated = DateTime.UtcNow.AddDays(-10),
			Category    = new CategoryInfo { Id = BugId,       CategoryName = "Bug" },
			Status      = new StatusInfo   { Id = OpenId,      StatusName   = "Open" },
			Author      = new UserInfo     { Id = "auth0|test-user",  Name = "Test User",  Email = "user@test.com" },
		},
		new()
		{
			Id          = Issue2Id,
			Title       = "Add dark mode",
			Description = "Support dark mode across all pages.",
			DateCreated = DateTime.UtcNow.AddDays(-5),
			Category    = new CategoryInfo { Id = FeatureId,   CategoryName = "Feature" },
			Status      = new StatusInfo   { Id = OpenId,      StatusName   = "Open" },
			Author      = new UserInfo     { Id = "auth0|test-user",  Name = "Test User",  Email = "user@test.com" },
		},
		new()
		{
			Id          = Issue3Id,
			Title       = "Improve search performance",
			Description = "Search results are slow for large datasets.",
			DateCreated = DateTime.UtcNow.AddDays(-2),
			Category    = new CategoryInfo { Id = ImprovementId, CategoryName = "Improvement" },
			Status      = new StatusInfo   { Id = InReviewId,    StatusName   = "In Review" },
			Author      = new UserInfo     { Id = "auth0|test-admin", Name = "Test Admin", Email = "admin@test.com" },
		},
	];
}
