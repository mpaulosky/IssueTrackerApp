// ============================================
// Copyright (c) 2023. All rights reserved.
// File Name :     CommentModel.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTracker
// Project Name :  IssueTracker.CoreBusiness
// =============================================

using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

using Shared.Models.DTOs;

namespace Shared.Models;

[Serializable]
public class Comment
{
	/// <summary>
	///   Gets or sets the identifier.
	/// </summary>
	/// <value>
	///   The identifier.
	/// </value>
	[BsonId]
	[BsonRepresentation(BsonType.ObjectId)]
	public string Id { get; set; } = string.Empty;

	/// <summary>
	///   Gets or sets the title.
	/// </summary>
	/// <value>
	///   The title.
	/// </value>
	[BsonElement("comment_title")]
	[BsonRepresentation(BsonType.String)]
	public string Title { get; set; } = string.Empty;

	/// <summary>
	///   Gets or sets the description.
	/// </summary>
	/// <value>
	///   The description.
	/// </value>
	[BsonElement("comment_description")]
	[BsonRepresentation(BsonType.String)]
	public string Description { get; init; } = string.Empty;

	/// <summary>
	///   Gets or sets the date created.
	/// </summary>
	/// <value>
	///   The date created.
	/// </value>
	[BsonElement("date_created")]
	[BsonRepresentation(BsonType.DateTime)]
	public DateTime DateCreated { get; init; } = DateTime.UtcNow;

	/// <summary>
	///   Gets or sets the issue.
	/// </summary>
	/// <value>
	///   The issue.
	/// </value>
	public BasicIssueModel Issue { get; set; } = new();

	/// <summary>
	///   Gets or sets the author.
	/// </summary>
	/// <value>
	///   The author.
	/// </value>
	public BasicUserModel Author { get; set; } = new();

	/// <summary>
	///   Gets or sets the user votes.
	/// </summary>
	/// <value>
	///   The user votes.
	/// </value>
	public HashSet<string> UserVotes { get; init; } = new();

	/// <summary>
	///   Gets or sets a value indicating whether this <see cref="Comment" /> is archived.
	/// </summary>
	/// <value>
	///   <c>true</c> if archived; otherwise, <c>false</c>.
	/// </value>
	[BsonElement("archived")]
	[BsonRepresentation(BsonType.Boolean)]
	public bool Archived { get; set; }

	/// <summary>
	///   Gets or sets who archived the record.
	/// </summary>
	/// <value>
	///   Who archived the record.
	/// </value>
	public BasicUserModel ArchivedBy { get; set; } = new();

	/// <summary>
	///   Gets or sets that this comment is the selected answer to the associated Issue.
	/// </summary>
	/// <value>
	///   <c>true</c> if is the answer; otherwise, <c>false</c>.
	/// </value>
	[BsonElement("is_answer")]
	[BsonRepresentation(BsonType.Boolean)]
	public bool IsAnswer { get; set; }

	/// <summary>
	///   Gets or sets the user that selected this comment as the answer to the associated Issue.
	/// </summary>
	/// <value>
	///   Who selected this comment as the answer to the associated Issue.
	/// </value>
	public BasicUserModel AnswerSelectedBy { get; set; } = new();
}
