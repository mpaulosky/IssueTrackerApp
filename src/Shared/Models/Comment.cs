// ============================================
// Copyright (c) 2023. All rights reserved.
// File Name :     Comment.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTracker
// Project Name :  IssueTracker.CoreBusiness
// =============================================

using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

using Shared.Abstractions;
using Shared.Models.DTOs;

namespace Shared.Models;

/// <summary>
///   Represents a comment on an issue.
/// </summary>
[Serializable]
public class Comment : Entity
{

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
	///   Gets or sets the issue.
	/// </summary>
	/// <value>
	///   The issue.
	/// </value>
	public IssueDto Issue { get; set; } = IssueDto.Empty;

	/// <summary>
	///   Gets or sets the author.
	/// </summary>
	/// <value>
	///   The author.
	/// </value>
	public UserDto Author { get; set; } = UserDto.Empty;

	/// <summary>
	///   Gets or sets the user votes.
	/// </summary>
	/// <value>
	///   The user votes.
	/// </value>
	public HashSet<string> UserVotes { get; init; } = new();

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
	public UserDto AnswerSelectedBy { get; set; } = UserDto.Empty;

}
