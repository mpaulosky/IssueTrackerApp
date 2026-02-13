// ============================================
// Copyright (c) 2023. All rights reserved.
// File Name :     CreateCommentDto.cs
// Company :       mpaulosky
// Author :        Matthew Paulosky
// Solution Name : IssueTracker
// Project Name :  IssueTracker.UI
// =============================================

namespace Web.Models;

public class CreateCommentDto
{
	[Required] [MaxLength(75)] public string? Title { get; set; }

	[Required] [MaxLength(500)] public string? Description { get; set; }
}