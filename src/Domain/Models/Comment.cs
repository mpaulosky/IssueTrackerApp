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
	public ObjectId Id { get; set; } = ObjectId.Empty;

	/// <summary>
	///   Gets or sets the title.
	/// </summary>
	/// <value>
	///   The title.
	/// </value>
	public string Title { get; set; } = string.Empty;

	/// <summary>
	///   Gets or sets the description.
	/// </summary>
	/// <value>
	///   The description.
	/// </value>
	public string Description { get; init; } = string.Empty;

	/// <summary>
	///   Gets or sets the date created.
	/// </summary>
	/// <value>
	///   The date created.
	/// </value>
	public DateTime DateCreated { get; init; } = DateTime.UtcNow;

	/// <summary>
	///   Gets or sets the date modified.
	/// </summary>
	/// <value>
	///   The date modified.
	/// </value>
	public DateTime? DateModified { get; set; }

	/// <summary>
	///   Gets or sets the issue identifier.
	/// </summary>
	/// <value>
	///   The issue identifier.
	/// </value>
	public ObjectId IssueId { get; set; } = ObjectId.Empty;

	/// <summary>
	///   Gets or sets the author.
	/// </summary>
	/// <value>
	///   The author.
	/// </value>
	public UserInfo Author { get; set; } = UserInfo.Empty;

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
	public bool Archived { get; set; }

	/// <summary>
	///   Gets or sets who archived the record.
	/// </summary>
	/// <value>
	///   Who archived the record.
	/// </value>
	public UserInfo ArchivedBy { get; set; } = UserInfo.Empty;

	/// <summary>
	///   Gets or sets that this comment is the selected answer to the associated Issue.
	/// </summary>
	/// <value>
	///   <c>true</c> if is the answer; otherwise, <c>false</c>.
	/// </value>
	public bool IsAnswer { get; set; }

	/// <summary>
	///   Gets or sets the user that selected this comment as the answer to the associated Issue.
	/// </summary>
	/// <value>
	///   Who selected this comment as the answer to the associated Issue.
	/// </value>
	public UserInfo AnswerSelectedBy { get; set; } = UserInfo.Empty;
}
