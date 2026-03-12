// =======================================================
// Copyright (c) 2025. All rights reserved.
// File Name :     Comment.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTrackerApp
// Project Name :  Domain
// =======================================================

namespace Domain.Models;

/// <summary>
///   Comment class
/// </summary>
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
	public ObjectId Id { get; set; } = ObjectId.Empty;

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
	///   Gets or sets the date modified.
	/// </summary>
	/// <value>
	///   The date modified.
	/// </value>
	[BsonElement("date_modified")]
	[BsonRepresentation(BsonType.DateTime)]
	public DateTime? DateModified { get; set; }

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
	public HashSet<string> UserVotes { get; set; } = [];

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
	[BsonElement("archived_by")]
	public UserDto ArchivedBy { get; set; } = UserDto.Empty;

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
